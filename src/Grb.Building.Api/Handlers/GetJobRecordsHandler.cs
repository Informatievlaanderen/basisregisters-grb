namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Abstractions.Responses;
    using Infrastructure;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    public class GetJobRecordsHandler : IRequestHandler<GetJobRecordsRequest, GetJobRecordsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;
        private readonly IPagedUriGenerator _pagedUriGenerator;

        public GetJobRecordsHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketingUrl ticketingUrl,
            IPagedUriGenerator pagedUriGenerator)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
            _pagedUriGenerator = pagedUriGenerator;
        }

        public async Task<GetJobRecordsResponse> Handle(GetJobRecordsRequest request,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.Jobs.FindAsync(request.JobId);

            if (job is null)
            {
                return new GetJobRecordsResponse(Array.Empty<JobRecordResponse>(), null);
            }

            var query = job.Status == JobStatus.Completed
                ? _buildingGrbContext.GetJobRecordsArchive(request.JobId)
                : _buildingGrbContext.JobRecords.Where(x => x.JobId == request.JobId);

            if (request.JobRecordStatuses.Any())
            {
                query = query.Where(x => request.JobRecordStatuses.Contains(x.Status));
            }

            var result = await query
                .OrderBy(x => x.RecordNumber)
                .Skip(request.Pagination.Offset.Value)
                .Take(request.Pagination.Limit!.Value)
                .ToListAsync(cancellationToken);

            return new GetJobRecordsResponse(
                result.Select(x =>
                    new JobRecordResponse
                    (
                        RecordNumber: x.RecordNumber,
                        Id: x.Id,
                        GrId: x.GrId,
                        TicketUrl: x.TicketId.HasValue ? _ticketingUrl.For(x.TicketId.Value) : null,
                        Status: x.Status,
                        ErrorMessage: x.ErrorMessage,
                        VersionDate: x.VersionDate
                    )
                ).ToArray(),
                _pagedUriGenerator.NextPage(query, request.Pagination, $"v2/uploads/jobs/{request.JobId}/jobrecords"));
        }
    }
}
