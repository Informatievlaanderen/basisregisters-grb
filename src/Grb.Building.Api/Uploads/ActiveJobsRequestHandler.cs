namespace Grb.Building.Api.Uploads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public sealed record ActiveJobsRequest() : IRequest<ActiveJobsResponse>;

    public sealed record ActiveJobsResponse(IEnumerable<JobResponse> Jobs);

    public sealed record JobResponse(
        Guid JobId,
        DateTimeOffset Created,
        JobStatus Status,
        string? TicketUrl);

    public sealed class ActiveJobsRequestHandler
        : IRequestHandler<ActiveJobsRequest, ActiveJobsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;

        public ActiveJobsRequestHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketingUrl ticketingUrl)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
        }

        public async Task<ActiveJobsResponse> Handle(
            ActiveJobsRequest request,
            CancellationToken cancellationToken)
        {
            var jobs = await _buildingGrbContext
                .Jobs
                .Where(x => x.Status != JobStatus.Cancelled && x.Status != JobStatus.Completed)
                .Select(x => new JobResponse(
                    x.Id,
                    x.Created,
                    x.Status,
                    x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value).ToString() : null))
                .ToListAsync(cancellationToken);

            return new ActiveJobsResponse(jobs);
        }
    }
}
