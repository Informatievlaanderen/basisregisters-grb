namespace Grb.Building.Processor.Job
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Oslo.Extensions;
    using BuildingRegistry.Api.BackOffice.Abstractions.Building.Requests;
    using Microsoft.EntityFrameworkCore;

    public interface IJobRecordsProcessor
    {
        Task Process(Guid jobId, CancellationToken ct);
    }

    public sealed class JobRecordsProcessor : IJobRecordsProcessor
    {
        private readonly IDbContextFactory<BuildingGrbContext> _buildingGrbContextFactory;
        private readonly IBackOfficeApiProxy _backOfficeApiProxy;

        public JobRecordsProcessor(
            IDbContextFactory<BuildingGrbContext> buildingGrbContextFactory,
            IBackOfficeApiProxy backOfficeApiProxy)
        {
            _buildingGrbContextFactory = buildingGrbContextFactory;
            _backOfficeApiProxy = backOfficeApiProxy;
        }

        public async Task Process(Guid jobId, CancellationToken ct)
        {
            await using var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(ct);
            var jobRecords = await buildingGrbContext.JobRecords
                .Where(x => x.JobId == jobId && x.Status == JobRecordStatus.Created)
                .Select(x => new { x.Id, x.GrId, x.RecordNumber })
                .ToListAsync(ct);

            // filter out jobrecords grid == -9
            var createJobRecords = jobRecords.Where(x => x.GrId == -9)
                .Select(x => x.Id)
                .ToList();

            // filter out jobrecords GrId != -9 and group them by GrId in concurrent dictionary
            var updateJobRecords = jobRecords.Where(x => x.GrId != -9)
                .GroupBy(x => x.GrId)
                .ToDictionary(x => x.Key, x => x
                    .OrderBy(record => record.RecordNumber)
                    .Select(record => record.Id)
                    .ToList());

            var maxJobRecords = createJobRecords.Count + updateJobRecords.Count;

            var maxDegreeOfParallelism = 10.0;

            var percentageCreatedJobRecords = createJobRecords.Count / maxJobRecords;
            var percentageUpdatedJobRecords = updateJobRecords.Count / maxJobRecords;

            var maxDegreeOfParallelismOfCreated = Math.Max((int) Math.Round(maxDegreeOfParallelism * percentageCreatedJobRecords), 1);
            var maxDegreeOfParallelismOfUpdated = Math.Max((int) Math.Round(maxDegreeOfParallelism * percentageUpdatedJobRecords), 1);

            var createTask = Parallel.ForEachAsync(createJobRecords,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelismOfCreated, CancellationToken = ct },
                async (record, innerCt) =>
                {
                    await ProcessJobRecords(new List<long> { record }, innerCt);
                });

            var updateTask = Parallel.ForEachAsync(updateJobRecords.Keys,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelismOfUpdated, CancellationToken = ct },
                async (key, innerCt) =>
                {
                    var records = updateJobRecords[key];
                    await ProcessJobRecords(records, innerCt);
                });

            Task.WaitAll(new[] { createTask, updateTask }, cancellationToken: ct);
        }

        private async Task ProcessJobRecords(List<long> jobRecordIds, CancellationToken ct)
        {
            foreach (var jobRecordId in jobRecordIds)
            {
                await using var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(ct);
                var jobRecord =
                    await buildingGrbContext.JobRecords.FindAsync(new object?[] { jobRecordId }, cancellationToken: ct);

                BackOfficeApiResult backOfficeApiResult;

                switch (jobRecord.EventType)
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
