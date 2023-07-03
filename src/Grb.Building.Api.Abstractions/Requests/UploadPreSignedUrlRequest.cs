namespace Grb.Building.Api.Abstractions.Requests
{
    using MediatR;
    using Responses;

    public sealed record UploadPreSignedUrlRequest : IRequest<UploadPreSignedUrlResponse>;
}
