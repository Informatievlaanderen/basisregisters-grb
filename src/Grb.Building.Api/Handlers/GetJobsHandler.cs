namespace Grb.Building.Api.Handlers
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Abstractions.Responses;
    using Infrastructure;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public class GetJobsHandler : IRequestHandler<GetJobsRequest, GetJobsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;
        private readonly IPagedUriGenerator _pagedUriGenerator;

        public GetJobsHandler(BuildingGrbContext buildingGrbContext, ITicketingUrl ticketingUrl, IPagedUriGenerator pagedUriGenerator)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
            _pagedUriGenerator = pagedUriGenerator;
        }

        public async Task<GetJobsResponse> Handle(GetJobsRequest request, CancellationToken cancellationToken)
        {
            var query = _buildingGrbContext.Jobs.AsQueryable();

            if (request.JobStatuses.Any())
                query = query.Where(x => request.JobStatuses.Contains(x.Status));

            if (request.FromDate.HasValue)
                query = query.Where(x => x.Created >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(x => x.Created <= request.ToDate.Value);

            var result = await query
                .OrderByDescending(x => x.Created)
                .Skip(request.Pagination.Offset.Value)
                .Take(request.Pagination.Limit!.Value + 1)
                .ToListAsync(cancellationToken);

            return new GetJobsResponse(
                result
                    .Take(request.Pagination.Limit!.Value)
                    .Select(x =>
                    new JobResponse
                    (
                        Id : x.Id,
                        TicketUrl : x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status : x.Status,
                        Created : x.Created,
                        LastChanged : x.LastChanged,
                        GetJobRecords : _pagedUriGenerator.FirstPage($"/v2/uploads/jobs/{x.Id}/jobrecords")
                    )
                ).ToArray(),
                _pagedUriGenerator.NextPage(result, request.Pagination, "v2/uploads/jobs"));
        }
    }
}

