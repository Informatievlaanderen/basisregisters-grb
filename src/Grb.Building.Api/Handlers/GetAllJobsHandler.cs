namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure;
    using Infrastructure.Query;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public record GetAllJobsRequest(Pagination Pagination, List<JobStatus> JobStatuses) : IRequest<GetAllJobsResponse>;
    public record GetAllJobsResponse(object[] Jobs, Uri NextPage);

    public class GetAllJobsHandler : IRequestHandler<GetAllJobsRequest, GetAllJobsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;
        private readonly IPagedUriGenerator _pagedUriGenerator;

        public GetAllJobsHandler(BuildingGrbContext buildingGrbContext, ITicketingUrl ticketingUrl, IPagedUriGenerator pagedUriGenerator)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
            _pagedUriGenerator = pagedUriGenerator;
        }

        public async Task<GetAllJobsResponse> Handle(GetAllJobsRequest request, CancellationToken cancellationToken)
        {
            var query = _buildingGrbContext.Jobs.AsQueryable();

            if (request.JobStatuses.Any())
            {
                query = query.Where(x => request.JobStatuses.Contains(x.Status));
            }

            var result = await query.Skip(request.Pagination.Offset.Value)
                .Take(request.Pagination.Limit!.Value)
                .OrderBy(x => x.Created)
                .ToListAsync(cancellationToken);

            return new GetAllJobsResponse(
                result.Select(x =>
                    new
                    {
                        Id = x.Id,
                        TicketUrl = x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status = x.Status,
                        BlobName = x.ReceivedBlobName,
                        Created = x.Created,
                        LastChanged = x.LastChanged,
                        GetJobRecords = _pagedUriGenerator.FirstPage($"/v2/uploads/jobs/{x.Id}/jobrecords")
                    }
                ).ToArray(),
                _pagedUriGenerator.NextPage(query, request.Pagination, "v2/uploads/jobs"));
        }
    }
}

