namespace Grb.Building.Api.Abstractions.Responses
{
    using System;
    using System.Collections.Generic;

    public sealed record UploadPreSignedUrlResponse(Guid JobId, string UploadUrl, Dictionary<string, string> UploadUrlFormData, string TicketUrl);
}
