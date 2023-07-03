namespace Grb.Building.Api.Abstractions.Responses
{
    using System;

    public sealed record JobResultsPreSignedUrlResponse(Guid JobId, string GetUrl);
}
