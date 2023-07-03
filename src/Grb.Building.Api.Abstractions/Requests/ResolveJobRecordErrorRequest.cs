namespace Grb.Building.Api.Abstractions.Requests
{
    using System;
    using MediatR;

    public sealed record ResolveJobRecordErrorRequest(Guid JobId, long JobRecordId) : IRequest;
}
