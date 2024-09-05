namespace Grb.Building.Api.Abstractions.Requests
{
    using System;
    using System.Collections.Generic;
    using Infrastructure.Query;
    using MediatR;
    using Responses;

    public record GetJobsRequest(
        Pagination Pagination,
        List<JobStatus> JobStatuses,
        DateTime? FromDate,
        DateTime? ToDate) : IRequest<GetJobsResponse>;
}
