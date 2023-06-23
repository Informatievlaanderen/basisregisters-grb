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

    public record GetJobRecordsRequest(
        Pagination Pagination,
        List<JobRecordStatus> JobRecordStatuses,
        Guid JobId)
            : IRequest<GetJobRecordsResponse>;

    public record JobRecordResult(
        int RecordNumber,
        long Id,
        int GrId,
        Uri? TicketUrl,
        JobRecordStatus Status,
        string? ErrorMessage,
        DateTimeOffset VersionDate
    );

    public record GetJobRecordsResponse(JobRecordResult[] JobRecords, Uri NextPage);

    public class GetJobRecordsHandler : IRequestHandler<GetJobRecordsRequest, GetJobRecordsResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;
        private readonly IPagedUriGenerator _pagedUriGenerator;

        public GetJobRecordsHandler(BuildingGrbContext buildingGrbContext, ITicketingUrl ticketingUrl,
            IPagedUriGenerator pagedUriGenerator)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
            _pagedUriGenerator = pagedUriGenerator;
        }

        public async Task<GetJobRecordsResponse> Handle(GetJobRecordsRequest request,
            CancellationToken cancellationToken)
        {
            var query = _buildingGrbContext.JobRecords
                .Where(x => x.JobId == request.JobId);

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
                    new JobRecordResult
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
