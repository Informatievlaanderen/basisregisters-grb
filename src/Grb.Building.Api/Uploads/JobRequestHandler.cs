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
    using TicketingService.Abstractions;

    public sealed record JobRequest(Guid JobId) : IRequest<JobResponse>;

    public sealed record JobResponse(
        Guid JobId,
        DateTimeOffset Created,
        JobStatus Status,
        string? TicketUrl,
        IEnumerable<JobRecordResponse> JobRecords);

    public sealed record JobRecordResponse(
        long JobRecordId,
        Guid JobId,
        int RecordNumber,
        JobRecordStatus Status,
        string? ErrorMessage);

    public sealed class JobRequestHandler
        : IRequestHandler<JobRequest, JobResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;

        public JobRequestHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketingUrl ticketingUrl)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
        }

        public async Task<JobResponse> Handle(
            JobRequest request,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
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

            return new JobResponse(
                job.Id,
                job.Created,
                job.Status,
                job.TicketId.HasValue ? _ticketingUrl.For(job.TicketId.Value).ToString() : null,
                jobResults);
        }
    }
}
