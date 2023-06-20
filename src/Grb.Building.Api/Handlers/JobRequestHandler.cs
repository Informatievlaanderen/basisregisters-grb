namespace Grb.Building.Api.Handlers
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
    public record JobResponse(Guid Id, Uri? TicketUrl, JobStatus Status, DateTimeOffset Created, string? BlobName, Uri GetJobRecords);

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

            return new JobResponse(
                job.Id,
                TicketUrl: job.TicketId.HasValue ? _ticketingUrl.For(job.TicketId.Value) : null,
                job.Status,
                job.Created,
                job.ReceivedBlobName,
                new Uri($"http://localhost:6018/v2/uploads/jobs/{job.Id}/jobrecords?offset=0"));
        }
    }
}
