﻿namespace Grb.Building.Api.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public sealed class ResolveJobRecordErrorHandler : IRequestHandler<ResolveJobRecordErrorRequest>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketing _ticketing;

        public ResolveJobRecordErrorHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketing ticketing)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketing = ticketing;
        }

        public async Task Handle(
            ResolveJobRecordErrorRequest request,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
            }

            var jobRecord = await _buildingGrbContext.FindJobRecord(request.JobRecordId, cancellationToken);

            if (jobRecord is null)
            {
                throw new ApiException("Onbestaande upload job record.", StatusCodes.Status404NotFound);
            }

            jobRecord.ResolveError();

            if (job.IsInError() && await CanJobContinueProcessing(job, request.JobRecordId, cancellationToken))
            {
                await ContinueProcessingJob(job, cancellationToken);
            }

            await _buildingGrbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> CanJobContinueProcessing(Job job, long jobRecordId, CancellationToken cancellationToken)
        {
            return !await _buildingGrbContext.JobRecords
                .AnyAsync(x =>
                    x.JobId == job.Id
                    && x.Id != jobRecordId
                    && x.Status == JobRecordStatus.Error, cancellationToken: cancellationToken);
        }

        private async Task ContinueProcessingJob(Job job, CancellationToken cancellationToken)
        {
            job.UpdateStatus(JobStatus.Processing);
            await _ticketing.Pending(job.TicketId!.Value, cancellationToken);
        }
    }
}
