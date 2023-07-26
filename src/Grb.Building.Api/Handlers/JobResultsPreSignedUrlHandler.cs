namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Abstractions.Responses;
    using Amazon.S3.Model;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Infrastructure;
    using Infrastructure.Options;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public sealed class JobResultsPreSignedUrlHandler
        : IRequestHandler<JobResultsPreSignedUrlRequest, JobResultsPreSignedUrlResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly BucketOptions _bucketOptions;
        private readonly IAmazonS3Extended _s3Extended;

        public JobResultsPreSignedUrlHandler(
            BuildingGrbContext buildingGrbContext,
            IOptions<BucketOptions> bucketOptions,
            IAmazonS3Extended s3Extended)
        {
            _buildingGrbContext = buildingGrbContext;
            _bucketOptions = bucketOptions.Value;
            _s3Extended = s3Extended;
        }

        public async Task<JobResultsPreSignedUrlResponse> Handle(
            JobResultsPreSignedUrlRequest request,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
            }

            if (job.Status is JobStatus.Cancelled or JobStatus.Error)
            {
                throw new ApiException(
                    $"De status van de upload job '{request.JobId}' is {job.Status.ToString().ToLower()}, hierdoor zijn er voor deze job geen resultaten beschikbaar.",
                    StatusCodes.Status400BadRequest);
            }

            if (job.Status != JobStatus.Completed)
            {
                throw new ApiException(
                    $"Upload job '{request.JobId}' wordt verwerkt en resultaten zijn nog niet beschikbaar.",
                    StatusCodes.Status400BadRequest);
            }

            var urlString = _s3Extended.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketOptions.BucketName,
                Key = Job.JobResultsBlobName(request.JobId),
                Expires = DateTime.UtcNow.AddHours(_bucketOptions.UrlExpirationInMinutes)
            });

            return new JobResultsPreSignedUrlResponse(
                request.JobId,
                urlString);
        }
    }
}
