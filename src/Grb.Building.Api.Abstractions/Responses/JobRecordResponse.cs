namespace Grb.Building.Api.Abstractions.Responses
{
    using System;

    public record JobRecordResponse(
        int RecordNumber,
        long Id,
        int GrId,
        Uri? TicketUrl,
        JobRecordStatus Status,
        string? ErrorMessage,
        DateTimeOffset VersionDate
    );
}
