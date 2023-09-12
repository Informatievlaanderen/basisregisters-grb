namespace Grb.Building.Processor.Job
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Notifications;
    using TicketingService.Abstractions;

    public sealed class JobProcessor : BackgroundService
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly IJobRecordsProcessor _jobRecordsProcessor;
        private readonly IJobRecordsMonitor _jobRecordsMonitor;
        private readonly ITicketing _ticketing;
        private readonly IJobResultUploader _jobResultUploader;
        private readonly IJobRecordsArchiver _jobRecordsArchiver;
        private readonly GrbApiOptions _grbApiOptions;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly INotificationService _notificationService;
        private readonly ILogger<JobProcessor> _logger;

        public JobProcessor(
            BuildingGrbContext buildingGrbContext,
            IJobRecordsProcessor jobRecordsProcessor,
            IJobRecordsMonitor jobRecordsMonitor,
            IJobResultUploader jobResultUploader,
            IJobRecordsArchiver jobRecordsArchiver,
            ITicketing ticketing,
            IOptions<GrbApiOptions> grbApiOptions,
            IHostApplicationLifetime hostApplicationLifetime,
            INotificationService notificationService,
            ILoggerFactory loggerFactory)
        {
            _buildingGrbContext = buildingGrbContext;
            _jobRecordsProcessor = jobRecordsProcessor;
            _jobRecordsMonitor = jobRecordsMonitor;
            _ticketing = ticketing;
            _jobResultUploader = jobResultUploader;
            _jobRecordsArchiver = jobRecordsArchiver;
            _grbApiOptions = grbApiOptions.Value;
            _hostApplicationLifetime = hostApplicationLifetime;
            _notificationService = notificationService;
            _logger = loggerFactory.CreateLogger<JobProcessor>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxLifeTimeJob = 65;

            _logger.LogInformation("JobProcessor started");

            var inactiveJobStatuses = new[] {JobStatus.Completed, JobStatus.Cancelled};
            var jobsToProcess = await _buildingGrbContext.Jobs
                .Where(x => !inactiveJobStatuses.Contains(x.Status))
                .OrderBy(x => x.Created)
                .ToListAsync(stoppingToken);

            foreach (var job in jobsToProcess)
            {
                if (job.Status == JobStatus.Created)
                {
                    if (job.IsExpired(TimeSpan.FromMinutes(maxLifeTimeJob)))
                    {
                        await CancelJob(job, stoppingToken);
                        continue;
                    }

                    break;
                }

                if (job.Status is JobStatus.Preparing or JobStatus.Error)
                {
                    _logger.LogWarning("Job '{jobId}' cannot be processed because it has status '{jobStatus}'.", job.Id,
                        job.Status);
                    break;
                }

                await ProcessJob(job, stoppingToken);

                if (job.Status is JobStatus.Error)
                {
                    break;
                }
            }

            _hostApplicationLifetime.StopApplication();

            await Task.FromResult(stoppingToken);
        }

        private async Task ProcessJob(Grb.Job job, CancellationToken stoppingToken)
        {
            await UpdateJobStatus(job, JobStatus.Processing, stoppingToken);

            await _jobRecordsProcessor.Process(job.Id, stoppingToken);
            await _jobRecordsMonitor.Monitor(job.Id, stoppingToken);

            var jobRecordErrors = await _buildingGrbContext.JobRecords
                .Where(x =>
                    x.JobId == job.Id
                    && x.Status == JobRecordStatus.Error)
                .ToListAsync(stoppingToken);

            if (jobRecordErrors.Any())
            {
                var jobTicketError = new TicketError
                {
                    Errors = jobRecordErrors.Select(x =>
                        new TicketError(
                            $"{x.ErrorMessage!} Record nummer: {x.RecordNumber}, GRID: {x.GrId}",
                            x.ErrorCode ?? string.Empty)).ToList()
                };

                await _ticketing.Error(job.TicketId!.Value, jobTicketError, stoppingToken);

                await _notificationService.PublishToTopicAsync(new NotificationMessage(
                    nameof(Job),
                    $"JobRecordErrors, Job: {job.Id} has {jobRecordErrors.Count} errors.",
                    "Building Import Job Processor",
                    NotificationSeverity.Danger));

                await UpdateJobStatus(job, JobStatus.Error, stoppingToken);

                return;
            }

            await _jobResultUploader.UploadJobResults(job.Id, stoppingToken);

            await _ticketing.Complete(
                job.TicketId!.Value,
                new TicketResult(new
                {
                    JobResultLocation = new Uri(new Uri(_grbApiOptions.PublicApiUrl),
                        $"/v2/gebouwen/uploads/jobs/{job.Id:D}/results").ToString()
                }),
                stoppingToken);
            await UpdateJobStatus(job, JobStatus.Completed, stoppingToken);

            var numberOfWarnings = _buildingGrbContext.JobRecords
                .Count(x =>
                    x.JobId == job.Id
                    && x.Status == JobRecordStatus.Warning);

            await _notificationService.PublishToTopicAsync(
                new NotificationMessage(
                    nameof(Job),
                    numberOfWarnings > 0
                        ? $"JobCompleted, Job {job.Id} is completed with {numberOfWarnings} warnings."
                        : $"JobCompleted, Job {job.Id} is completed.",
                    "Building Import Job Processor",
                    NotificationSeverity.Good));

            await _jobRecordsArchiver.Archive(job.Id, stoppingToken);
        }

        private async Task CancelJob(Grb.Job job, CancellationToken stoppingToken)
        {
            await _ticketing.Complete(
                job.TicketId!.Value,
                new TicketResult(new {JobStatus = "Cancelled"}),
                stoppingToken);

            await UpdateJobStatus(job, JobStatus.Cancelled, stoppingToken);

            await _notificationService.PublishToTopicAsync(
                new NotificationMessage(
                    nameof(Job),
                    $"JobCancelled, Job {job.Id} is cancelled. (Expired)",
                    "Building Import Job Processor",
                    NotificationSeverity.Low));

            _logger.LogWarning("Cancelled expired job '{jobId}'.", job.Id);
        }

        private async Task UpdateJobStatus(Grb.Job job, JobStatus jobStatus, CancellationToken stoppingToken)
        {
            job.UpdateStatus(jobStatus);
            await _buildingGrbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
