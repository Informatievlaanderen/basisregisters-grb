namespace Grb.Building.Api.Abstractions.Responses
{
    using System;

    public record GetJobRecordsResponse(JobRecordResponse[] JobRecords, Uri? NextPage);
}
