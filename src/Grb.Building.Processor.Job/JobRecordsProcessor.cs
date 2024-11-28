namespace Grb.Building.Processor.Job
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.GrAr.Common;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Oslo.Extensions;
    using BuildingRegistry.Api.BackOffice.Abstractions.Building.Requests;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NodaTime;

    public interface IJobRecordsProcessor
    {
        Task Process(Guid jobId, bool skipOfficeHours = false, CancellationToken ct = default);
    }

    public sealed class JobRecordsProcessor : IJobRecordsProcessor
    {
        private readonly IDbContextFactory<BuildingGrbContext> _buildingGrbContextFactory;
        private readonly IBackOfficeApiProxy _backOfficeApiProxy;
        private readonly IClock _clock;
        private readonly OutsideOfficeHoursOptions _outsideOfficeHoursOptions;
        private readonly ILogger<JobRecordsProcessor> _logger;

        public JobRecordsProcessor(
            IDbContextFactory<BuildingGrbContext> buildingGrbContextFactory,
            IBackOfficeApiProxy backOfficeApiProxy,
            IClock clock,
            IOptions<OutsideOfficeHoursOptions> processWindowOptions,
            ILoggerFactory loggerFactory)
        {
            _buildingGrbContextFactory = buildingGrbContextFactory;
            _backOfficeApiProxy = backOfficeApiProxy;
            _clock = clock;
            _outsideOfficeHoursOptions = processWindowOptions.Value;
            _logger = loggerFactory.CreateLogger<JobRecordsProcessor>();
        }

        public async Task Process(Guid jobId, bool skipOfficeHours = false, CancellationToken ct = default)
        {
            await using var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(ct);
            var jobRecords = (await buildingGrbContext.JobRecords
                .Where(x => x.JobId == jobId && x.Status == JobRecordStatus.Created)
                .Select(x => new { x.Id, x.GrId, x.RecordNumber })
                .ToListAsync(ct))
                .OrderBy(x => x.RecordNumber)
                .ToList();

            if (jobRecords.Count == 0)
            {
                return;
            }

            var maxUsedBuildingRegistryId = jobRecords.Max(x => x.GrId);

            var batches = jobRecords.SplitBySize(2500).ToList();

            foreach (var batch in batches)
            {
                var maxDegreeOfParallelism = IsOutsideOfOfficeHours() || skipOfficeHours ? 10 : 1;
                _logger.LogInformation(
                    "Processing batch of {numberOfRecords} records with {maxDegreeOfParallelism} threads.",
                    batch.Count,
                    maxDegreeOfParallelism);

                var groupedJobRecords = batch
                    .GroupBy(x => x.GrId != -9 ? x.GrId : maxUsedBuildingRegistryId + x.RecordNumber)
                    .ToDictionary(
                        x => x.Key,
                        x => x
                            .OrderBy(record => record.RecordNumber)
                            .Select(record => record.Id)
                            .ToList());

                await Parallel.ForEachAsync(groupedJobRecords.Keys,
                    new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = ct },
                    async (key, innerCt) =>
                    {
                        var records = groupedJobRecords[key];
                        await ProcessJobRecords(records, innerCt);
                    });
            }
        }

        private bool IsOutsideOfOfficeHours()
        {
            var localTime = _clock.GetCurrentInstant().ToBelgianDateTimeOffset();

            return localTime.Hour >= _outsideOfficeHoursOptions.FromHour || localTime.Hour < _outsideOfficeHoursOptions.UntilHour;
        }

        private async Task ProcessJobRecords(List<long> jobRecordIds, CancellationToken ct)
        {
            foreach (var jobRecordId in jobRecordIds)
            {
                await using var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(ct);
                var jobRecord = await buildingGrbContext.JobRecords.FindAsync([jobRecordId], cancellationToken: ct);

                BackOfficeApiResult backOfficeApiResult;

                switch (jobRecord!.EventType)
                {
                    case GrbEventType.DefineBuilding:
                        backOfficeApiResult = await _backOfficeApiProxy.RealizeAndMeasureUnplannedBuilding(
                            new RealizeAndMeasureUnplannedBuildingRequest { GrbData = GrbDataMapper.Map(jobRecord) },
                            ct);
                        break;
                    case GrbEventType.DemolishBuilding:
                        backOfficeApiResult = await _backOfficeApiProxy.DemolishBuilding(
                            jobRecord.GrId, new DemolishBuildingRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
                        break;
                    case GrbEventType.MeasureBuilding:
                        backOfficeApiResult = await _backOfficeApiProxy.MeasureBuilding(
                            jobRecord.GrId, new MeasureBuildingRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
                        break;
                    case GrbEventType.ChangeBuildingMeasurement:
                        backOfficeApiResult = await _backOfficeApiProxy.ChangeBuildingMeasurement(
                            jobRecord.GrId,
                            new ChangeBuildingMeasurementRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
                        break;
                    case GrbEventType.CorrectBuildingMeasurement:
                        backOfficeApiResult = await _backOfficeApiProxy.CorrectBuildingMeasurement(
                            jobRecord.GrId,
                            new CorrectBuildingMeasurementRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
                        break;
                    case GrbEventType.Unknown:
                    default:
                        throw new NotImplementedException($"Unsupported JobRecord EventType: {jobRecord.EventType}");
                }

                if (backOfficeApiResult.IsSuccess)
                {
                    jobRecord.TicketId = Guid.Parse(backOfficeApiResult.TicketUrl!.AsIdentifier().Map(x => x));
                    jobRecord.Status = JobRecordStatus.Pending;
                }
                else
                {
                    var evaluation = ErrorWarningEvaluator.Evaluate(backOfficeApiResult.ValidationErrors!.ToList());
                    jobRecord.Status = evaluation.jobRecordStatus;
                    jobRecord.ErrorMessage = evaluation.message;
                    jobRecord.ErrorCode = evaluation.code;
                }

                await buildingGrbContext.SaveChangesAsync(ct);
            }
        }
    }
}
