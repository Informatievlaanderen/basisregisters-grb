namespace Grb.Building.Api.Abstractions.Responses
{
    using System;

    public record JobResponse(
        Guid Id,
        Uri? TicketUrl,
        JobStatus Status,
        DateTimeOffset Created,
        DateTimeOffset LastChanged,
        Uri GetJobRecords
    );
}
