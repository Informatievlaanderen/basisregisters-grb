namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public sealed record GetActiveJobsRequest() : IRequest<GetActiveJobResponse>;

   // public sealed record ActiveJobsResponse(IEnumerable<ActiveJobResponse> Jobs);

    public sealed record GetActiveJobResponse(object[] Jobs);

    public sealed class ActiveJobsHandler
        : IRequestHandler<GetActiveJobsRequest, GetActiveJobResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;

        public ActiveJobsHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketingUrl ticketingUrl)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
        }

        public async Task<GetActiveJobResponse> Handle(
            GetActiveJobsRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _buildingGrbContext
                .Jobs
                .Where(x => x.Status != JobStatus.Cancelled && x.Status != JobStatus.Completed)
                .ToListAsync(cancellationToken);

            return new GetActiveJobResponse(
                result.ConvertAll(x =>
                    new
                    {
                        Id = x.Id,
                        TicketUrl = x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status = x.Status,
                        BlobName = x.ReceivedBlobName,
                        Created = x.Created,
                        LastChanged = x.LastChanged,
                        GetJobRecords = new Uri($"http://localhost:6018/v2/uploads/jobs/{x.Id}/jobrecords?offset=0")
                    }
                ).ToArray());
        }
    }
}
