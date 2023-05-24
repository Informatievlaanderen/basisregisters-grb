﻿namespace Grb.Building.Api.Uploads
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3.Model;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Grb;
    using Infrastructure;
    using Infrastructure.Options;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public sealed record DownloadJobResultsPreSignedUrlRequest
        (Guid JobId) : IRequest<DownloadJobResultsPreSignedUrlResponse>;

    public sealed record DownloadJobResultsPreSignedUrlResponse(Guid JobId, string GetUrl);

    public sealed class DownloadJobResultsPreSignedUrlHandler
        : IRequestHandler<DownloadJobResultsPreSignedUrlRequest, DownloadJobResultsPreSignedUrlResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly BucketOptions _bucketOptions;
        private readonly IAmazonS3Extended _s3Extended;

        public DownloadJobResultsPreSignedUrlHandler(
            BuildingGrbContext buildingGrbContext,
            IOptions<BucketOptions> bucketOptions,
            IAmazonS3Extended s3Extended)
        {
            _buildingGrbContext = buildingGrbContext;
            _bucketOptions = bucketOptions.Value;
            _s3Extended = s3Extended;
        }

        public async Task<DownloadJobResultsPreSignedUrlResponse> Handle(
            DownloadJobResultsPreSignedUrlRequest request,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException($"Upload job with id {request.JobId} not found.", StatusCodes.Status404NotFound);
            }

            if (job.Status != JobStatus.Completed)
            {
                throw new ApiException(
                    $"Job with id {request.JobId} has not yet completed.",
                    StatusCodes.Status400BadRequest);
            }

            var urlString = _s3Extended.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketOptions.BucketName,
                Key = Job.JobResultsBlobName(request.JobId),
                Expires = DateTime.UtcNow.AddHours(_bucketOptions.UrlExpirationInMinutes)
            });

            return new DownloadJobResultsPreSignedUrlResponse(
                request.JobId,
                urlString);
        }
    }
}
