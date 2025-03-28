﻿namespace Grb.Building.Processor.Upload
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.ECS;
    using Amazon.ECS.Model;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Notifications;
    using TicketingService.Abstractions;
    using Zip;
    using Zip.Exceptions;
    using Zip.Translators;
    using Zip.Validators;
    using Task = System.Threading.Tasks.Task;

    public sealed class UploadProcessor : BackgroundService
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly IDuplicateJobRecordValidator _duplicateJobRecordValidator;
        private readonly ITicketing _ticketing;
        private readonly IBlobClient _blobClient;
        private readonly IAmazonECS _amazonEcs;
        private readonly ILogger<UploadProcessor> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly INotificationService _notificationService;
        private readonly EcsTaskOptions _ecsTaskOptions;

        public UploadProcessor(
            BuildingGrbContext buildingGrbContext,
            IDuplicateJobRecordValidator duplicateJobRecordValidator,
            ITicketing ticketing,
            IBlobClient blobClient,
            IAmazonECS amazonEcs,
            ILoggerFactory loggerFactory,
            IHostApplicationLifetime hostApplicationLifetime,
            INotificationService notificationService,
            IOptions<EcsTaskOptions> ecsTaskOptions)
        {
            _buildingGrbContext = buildingGrbContext;
            _duplicateJobRecordValidator = duplicateJobRecordValidator;
            _ticketing = ticketing;
            _blobClient = blobClient;
            _amazonEcs = amazonEcs;
            _logger = loggerFactory.CreateLogger<UploadProcessor>();
            _hostApplicationLifetime = hostApplicationLifetime;
            _notificationService = notificationService;
            _ecsTaskOptions = ecsTaskOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Check created jobs
            var jobs = await _buildingGrbContext.Jobs
                .Where(x => x.Status == JobStatus.Created || x.Status == JobStatus.Preparing)
                .OrderBy(x => x.Created)
                .ToListAsync(stoppingToken);

            if (!jobs.Any())
            {
                _hostApplicationLifetime.StopApplication();
                return;
            }

            foreach (var job in jobs)
            {
                try
                {
                    await using var stream = await GetZipArchiveStream(job, stoppingToken);

                    if (stream == null)
                    {
                        continue;
                    }

                    // If so, update ticket status and job status => preparing
                    await _ticketing.Pending(job.TicketId!.Value, stoppingToken);
                    await UpdateJobStatus(job, JobStatus.Preparing, stoppingToken);

                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);

                    var archiveValidator = new ZipArchiveValidator(GrbArchiveEntryStructure);

                    var problems = archiveValidator.Validate(archive);
                    if (problems.Any())
                    {
                        var ticketingErrors = problems.Select(x =>
                                x.Parameters.Any()
                                    ? new TicketError("Record number(s):" + string.Join(',', x.Parameters.Select(y => y.Value)), x.Message)
                                    : new TicketError(x.Message, x.Code))
                            .ToList();

                        var ticketError = new TicketError
                        {
                            Errors = ticketingErrors
                        };
                        await _ticketing.Error(job.TicketId!.Value, ticketError, stoppingToken);
                        await UpdateJobStatus(job, JobStatus.Error, stoppingToken);

                        await _notificationService.PublishToTopicAsync(new NotificationMessage(
                            nameof(Upload),
                            $"Job '{job.Id}' placed in error due to validation problems.",
                            "Building Import Upload Processor",
                            NotificationSeverity.Danger));

                        continue;
                    }

                    var archiveTranslator = new ZipArchiveTranslator(Encoding.UTF8);
                    var jobRecords = archiveTranslator.Translate(archive).ToList();
                    jobRecords.ForEach(x => x.JobId = job.Id);

                    await _buildingGrbContext.JobRecords.AddRangeAsync(jobRecords, stoppingToken);
                    await _buildingGrbContext.SaveChangesAsync(stoppingToken);

                    await UpdateJobStatus(job, JobStatus.Prepared, stoppingToken);
                }
                catch (DbRecordsWithMissingShapeException ex)
                {
                    var errorMessage = $"In de meegegeven shape file hebben niet alle gebouwen een geometriePolygoon. Record nummers: {string.Join(',', ex.RecordNumbers)}";
                    await _ticketing.Error(job.TicketId!.Value, new TicketError(errorMessage, "OntbrekendeGeometriePolygoonShapeFile"), stoppingToken);
                    await UpdateJobStatus(job, JobStatus.Error, stoppingToken);

                    await _notificationService.PublishToTopicAsync(new NotificationMessage(
                        nameof(Upload),
                        errorMessage,
                        "Building Import Upload Processor",
                        NotificationSeverity.Danger));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected exception for job '{job.Id}'");

                    await _ticketing.Error(job.TicketId!.Value, new TicketError($"Onverwachte fout bij de verwerking van het zip-bestand.", "OnverwachteFout"), stoppingToken);
                    await UpdateJobStatus(job, JobStatus.Error, stoppingToken);

                    await _notificationService.PublishToTopicAsync(new NotificationMessage(
                        nameof(Upload),
                        $"Unexpected exception for job '{job.Id}'.",
                        "Building Import Upload Processor",
                        NotificationSeverity.Danger));
                }
            }

            if (jobs.Any(x => x.Status == JobStatus.Prepared))
            {
                await StartJobProcessor(stoppingToken);
            }

            _hostApplicationLifetime.StopApplication();
        }

        private async Task UpdateJobStatus(Job job, JobStatus status, CancellationToken stoppingToken)
        {
            job.UpdateStatus(status);
            await _buildingGrbContext.SaveChangesAsync(stoppingToken);
        }

        private async Task StartJobProcessor(CancellationToken stoppingToken)
        {
            var taskResponse = await _amazonEcs.RunTaskAsync(
                new RunTaskRequest
                {
                    Cluster = _ecsTaskOptions.Cluster,
                    TaskDefinition = _ecsTaskOptions.TaskDefinition,
                    LaunchType = LaunchType.FARGATE,
                    Count = 1,
                    NetworkConfiguration = new NetworkConfiguration
                    {
                        AwsvpcConfiguration = new AwsVpcConfiguration
                        {
                            Subnets = _ecsTaskOptions.Subnets.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                            SecurityGroups = _ecsTaskOptions.SecurityGroups.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                            AssignPublicIp = AssignPublicIp.DISABLED
                        }
                    }
                },
                stoppingToken);

            if (taskResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"Starting ECS Task return HttpStatusCode: {taskResponse.HttpStatusCode.ToString()}");
            }

            string FailureToString(Failure failure) => $"Reason: {failure.Reason}{Environment.NewLine}Failure: {failure.Detail}";

            if (taskResponse.Failures.Any())
            {
                foreach (var failure in taskResponse.Failures)
                {
                    _logger.LogError(FailureToString(failure));
                }
            }
        }

        private async Task<Stream?> GetZipArchiveStream(Job job, CancellationToken stoppingToken)
        {
            var blobName = new BlobName(job.ReceivedBlobName);

            if (!await _blobClient.BlobExistsAsync(blobName, stoppingToken))
            {
                return null;
            }

            var blobObject = await _blobClient.GetBlobAsync(blobName, stoppingToken);
            if (blobObject is null)
            {
                _logger.LogError($"No blob found with name: {job.ReceivedBlobName}");
                return null;
            }

            try
            {
                return await blobObject.OpenAsync(stoppingToken);
            }
            catch (BlobNotFoundException)
            {
                _logger.LogError($"No blob found with name: {job.ReceivedBlobName}");
                return null;
            }
        }

        public Dictionary<string, IZipArchiveEntryValidator> GrbArchiveEntryStructure =>
            new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    new ZipArchiveDbaseEntryValidator<GrbDbaseRecord>(
                        Encoding.UTF8,
                        new DbaseFileHeaderReadBehavior(true),
                        new GrbDbaseSchema(),
                        new GrbDbaseRecordsValidator(_duplicateJobRecordValidator))
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            };
    }
}
