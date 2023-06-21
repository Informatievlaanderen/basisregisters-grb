namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public sealed record GetActiveJobsRequest() : IRequest<GetActiveJobsResponse>;

    public sealed record GetActiveJobsResponse(object[] Jobs);

    public sealed class GetActiveJobsHandler
        : IRequestHandler<GetActiveJobsRequest, GetActiveJobsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;
        private readonly IPagedUriGenerator _pagedUriGenerator;

        public GetActiveJobsHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketingUrl ticketingUrl,
            IPagedUriGenerator pagedUriGenerator)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
            _pagedUriGenerator = pagedUriGenerator;
        }

        public async Task<GetActiveJobsResponse> Handle(
            GetActiveJobsRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _buildingGrbContext
                .Jobs
                .Where(x => x.Status != JobStatus.Cancelled && x.Status != JobStatus.Completed)
                .ToListAsync(cancellationToken);

            return new GetActiveJobsResponse(
                result.ConvertAll(x =>
                    new
                    {
                        Id = x.Id,
                        TicketUrl = x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status = x.Status,
                        BlobName = x.ReceivedBlobName,
                        Created = x.Created,
                        LastChanged = x.LastChanged,
                        GetJobRecords = _pagedUriGenerator.FirstPage($"v2/uploads/jobs/{x.Id}/jobrecords")
                    }
                ).ToArray());
        }
    }
}
