namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Query;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public record GetAllJobsRequest(Pagination Pagination, List<JobStatus> JobStatuses) : IRequest<GetAllJobsResponse>;

    //public record GetAllJobsResponse(JobResponse[] Jobs, Uri NextPage);

    public record GetAllJobsResponse(object[] Jobs, Uri NextPage);

    public class GetAllJobsHandler : IRequestHandler<GetAllJobsRequest, GetAllJobsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;

        public GetAllJobsHandler(BuildingGrbContext buildingGrbContext, ITicketingUrl ticketingUrl)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
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

            var hasNextPage = query
                .Skip(request.Pagination.NextPageOffset)
                .Take(1).Any();

            return new GetAllJobsResponse(
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
                ).ToArray(),
                hasNextPage
                    ? new Uri($"http://localhost:6018/v2/uploads/jobs?offset={request.Pagination.NextPageOffset}")
                    : null);
        }
    }
}
