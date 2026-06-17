namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Abstractions.Responses;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Infrastructure.Options;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    public sealed class JobResultsPreSignedUrlHandler
        : IRequestHandler<JobResultsPreSignedUrlRequest, JobResultsPreSignedUrlResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly BucketOptions _bucketOptions;
        private readonly IAmazonS3 _s3;

        public JobResultsPreSignedUrlHandler(
            BuildingGrbContext buildingGrbContext,
            IOptions<BucketOptions> bucketOptions,
            IAmazonS3 s3)
        {
            _buildingGrbContext = buildingGrbContext;
            _bucketOptions = bucketOptions.Value;
            _s3 = s3;
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
                    StatusCodes.Status400BadRequest); }

            var urlString = await _s3.GetPreSignedURLAsync(
                new GetPreSignedUrlRequest
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
