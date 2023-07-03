namespace Grb.Building.Api.Abstractions.Responses
{
    using System;

    public record GetJobsResponse(JobResponse[] Jobs, Uri NextPage);
}
