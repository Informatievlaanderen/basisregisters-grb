namespace Grb.Building.Api.Abstractions.Requests
{
    using System;
    using System.Collections.Generic;
    using Infrastructure.Query;
    using MediatR;
    using Responses;

    public record GetJobRecordsRequest(
            Pagination Pagination,
            List<JobRecordStatus> JobRecordStatuses,
            Guid JobId)
        : IRequest<GetJobRecordsResponse>;
}
