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

    public record GetAllJobRecordsRequest(
        Pagination Pagination,
        List<JobRecordStatus> JobRecordStatuses,
        Guid JobId) : IRequest<GetAllJobRecordsResponse>;

    public record GetAllJobRecordsResponse(object[] JobRecords, Uri NextPage);

    public class GetAllJobRecordsHandler : IRequestHandler<GetAllJobRecordsRequest, GetAllJobRecordsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;

        public GetAllJobRecordsHandler(BuildingGrbContext buildingGrbContext, ITicketingUrl ticketingUrl)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
        }

        public async Task<GetAllJobRecordsResponse> Handle(GetAllJobRecordsRequest request,
            CancellationToken cancellationToken)
        {
            var query = _buildingGrbContext.JobRecords
                .Where(x => x.JobId == request.JobId);

            if (request.JobRecordStatuses.Any())
            {
                query = query.Where(x => request.JobRecordStatuses.Contains(x.Status));
            }

            var result = await query
                .Skip(request.Pagination.Offset.Value)
                .Take(request.Pagination.Limit!.Value)
                .OrderBy(x => x.RecordNumber)
                .ToListAsync(cancellationToken);

            var hasNextPage = query
                .Skip(request.Pagination.NextPageOffset)
                .Take(1).Any();

            return new GetAllJobRecordsResponse(
                result.ConvertAll(x =>
                    new
                    {
                        RecordNumber = x.RecordNumber,
                        Id = x.Id,
                        GrId = x.GrId,
                        TicketId = x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status = x.Status,
                        ErrorMessage = x.ErrorMessage,
                        VersionDate = x.VersionDate
                    }
                ).ToArray(),
                hasNextPage
                    ? new Uri($"http://localhost:6018/v2/uploads/jobs/{request.JobId}/jobrecords?offset={request.Pagination.NextPageOffset}")
                    : null);
        }
    }
}
