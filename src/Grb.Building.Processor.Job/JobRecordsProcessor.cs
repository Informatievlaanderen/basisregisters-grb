﻿namespace Grb.Building.Processor.Job
{
    using System;
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
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly IBackOfficeApiProxy _backOfficeApiProxy;

        public JobRecordsProcessor(
            BuildingGrbContext buildingGrbContext,
            IBackOfficeApiProxy backOfficeApiProxy)
        {
            _buildingGrbContext = buildingGrbContext;
            _backOfficeApiProxy = backOfficeApiProxy;
        }

        public async Task Process(Guid jobId, CancellationToken ct)
        {
            var jobRecords = await _buildingGrbContext.JobRecords
                .Where(x => x.JobId == jobId && x.Status == JobRecordStatus.Created)
                .OrderBy(x => x.RecordNumber)
                .ToListAsync(ct);

            foreach (var jobRecord in jobRecords)
            {
                BackOfficeApiResult backOfficeApiResult;

                switch (jobRecord.EventType)
                {
                    case GrbEventType.DefineBuilding:
                        backOfficeApiResult = await _backOfficeApiProxy.RealizeAndMeasureUnplannedBuilding(
                            new RealizeAndMeasureUnplannedBuildingRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
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
                            jobRecord.GrId, new ChangeBuildingMeasurementRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
                        break;
                    case GrbEventType.CorrectBuildingMeasurement:
                        backOfficeApiResult = await _backOfficeApiProxy.CorrectBuildingMeasurement(
                            jobRecord.GrId, new CorrectBuildingMeasurementRequest { GrbData = GrbDataMapper.Map(jobRecord) }, ct);
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
                }

                await _buildingGrbContext.SaveChangesAsync(ct);
            }
        }
    }
}
