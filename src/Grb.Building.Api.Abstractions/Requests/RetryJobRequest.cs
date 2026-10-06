namespace Grb.Building.Api.Abstractions.Requests
{
    using System;
    using MediatR;

    public sealed record RetryJobRequest(Guid JobId) : IRequest;
}
