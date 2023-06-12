namespace Grb.Building.Api.Uploads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    public sealed record JobRecordsRequest(Guid JobId) : IRequest<JobRecordsResponse>;

    public sealed record JobRecordsResponse(IEnumerable<JobRecordResponse> JobRecords);

    public sealed record JobRecordResponse(
        long JobRecordId,
        Guid JobId,
        int RecordNumber,
        JobRecordStatus Status,
        string? ErrorMessage);

    public sealed class JobResultsRequestHandler
        : IRequestHandler<JobRecordsRequest, JobRecordsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;

        public JobResultsRequestHandler(
            BuildingGrbContext buildingGrbContext)
        {
            _buildingGrbContext = buildingGrbContext;
        }

        public async Task<JobRecordsResponse> Handle(
            JobRecordsRequest request,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
            }

            // For job status
            //  Cancelled (cancellation can only occur when job was not yet being prepared)
            //  Completed (job records are archived)
            // we are returning an empty list.
            if (job.Status is JobStatus.Created or JobStatus.Preparing)
            {
                throw new ApiException($"Upload job '{request.JobId}' heeft nog geen bescikbare job records.", StatusCodes.Status400BadRequest);
            }

            var jobResults = await _buildingGrbContext.JobRecords
                .Where(x => x.JobId == request.JobId)
                .OrderBy(x => x.RecordNumber)
                .Select(x => new JobRecordResponse(
                    x.Id,
                    x.JobId,
                    x.RecordNumber,
                    x.Status,
                    x.ErrorMessage))
                .ToListAsync(cancellationToken);

            return new JobRecordsResponse(jobResults);
        }
    }
}
