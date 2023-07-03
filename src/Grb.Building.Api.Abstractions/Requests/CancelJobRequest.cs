namespace Grb.Building.Api.Abstractions.Requests
{
    using System;
    using MediatR;

    public sealed record CancelJobRequest(Guid JobId) : IRequest;
}
