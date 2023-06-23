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

    public record GetJobsRequest(Pagination Pagination, List<JobStatus> JobStatuses) : IRequest<GetJobsResponse>;
    public record GetJobsResponse(JobResult[] Jobs, Uri NextPage);

    public record JobResult(
        Guid Id,
        Uri? TicketUrl,
        JobStatus Status,
        string BlobName,
        DateTimeOffset Created,
        DateTimeOffset LastChanged,
        Uri GetJobRecords
    );

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
            {
                query = query.Where(x => request.JobStatuses.Contains(x.Status));
            }

            var result = await query
                .OrderBy(x => x.Created)
                .Skip(request.Pagination.Offset.Value)
                .Take(request.Pagination.Limit!.Value)
                .ToListAsync(cancellationToken);

            return new GetJobsResponse(
                result.Select(x =>
                    new JobResult
                    (
                        Id : x.Id,
                        TicketUrl : x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status : x.Status,
                        BlobName : x.ReceivedBlobName,
                        Created : x.Created,
                        LastChanged : x.LastChanged,
                        GetJobRecords : _pagedUriGenerator.FirstPage($"/v2/uploads/jobs/{x.Id}/jobrecords")
                    )
                ).ToArray(),
                _pagedUriGenerator.NextPage(query, request.Pagination, "v2/uploads/jobs"));
        }
    }
}

