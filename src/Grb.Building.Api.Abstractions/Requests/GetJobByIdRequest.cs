namespace Grb.Building.Api.Abstractions.Requests
{
    using System;
    using MediatR;
    using Responses;

    public sealed record GetJobByIdRequest(Guid JobId) : IRequest<JobResponse>;
}
