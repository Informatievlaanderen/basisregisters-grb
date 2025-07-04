﻿namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3.Model;
    using Api.Abstractions.Requests;
    using Api.Handlers;
    using Api.Infrastructure;
    using Api.Infrastructure.Options;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Moq;
    using Xunit;

    public class GivenJobResultPreSignedUrlRequest
    {
        private readonly FakeBuildingGrbContext _fakeBuildingGrbContext;
        private readonly BucketOptions _bucketOptions;
        private readonly Mock<IAmazonS3Extended> _s3Extended;
        private readonly JobResultsPreSignedUrlHandler _handler;

        public GivenJobResultPreSignedUrlRequest()
        {
            _fakeBuildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
            _s3Extended = new Mock<IAmazonS3Extended>();
            _bucketOptions = new BucketOptions { BucketName = "Test", UrlExpirationInMinutes = 60 };
            _handler = new JobResultsPreSignedUrlHandler(
                _fakeBuildingGrbContext,
                Options.Create(_bucketOptions),
                _s3Extended.Object);
        }

        [Fact]
        public async Task ThenReturnsPresignedUrlResponse()
        {
            var job = new Job(DateTimeOffset.Now, JobStatus.Completed) { Id = Guid.NewGuid() };
            _fakeBuildingGrbContext.Jobs.Add(job);
            await _fakeBuildingGrbContext.SaveChangesAsync();

            const string expectedPresignedUrl = "https://presignedurl.com";

            _s3Extended
                .Setup(x => x.GetPreSignedURL(It.Is<GetPreSignedUrlRequest>(
                    x => x.BucketName == _bucketOptions.BucketName && x.Key == $"jobresults/{job.Id:D}")))
                .Returns(expectedPresignedUrl);

            var request = new JobResultsPreSignedUrlRequest(job.Id);
            var result = await _handler.Handle(request, CancellationToken.None);

            result.JobId.Should().Be(job.Id);
            result.GetUrl.Should().Be(expectedPresignedUrl);
        }

        [Fact]
        public async Task WithNonExistingJob_ThenThrowsApiException()
        {
            var request = new JobResultsPreSignedUrlRequest(Guid.NewGuid());
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            await act
                .Should()
                .ThrowAsync<ApiException>()
                .Where(x =>
                    x.Message.Contains("Onbestaande upload job.")
                    && x.StatusCode == StatusCodes.Status404NotFound);
        }

        [Theory]
        [InlineData(JobStatus.Cancelled)]
        [InlineData(JobStatus.Error)]
        public async Task WithCancelledOrErrorJob_ThenThrowsApiException(JobStatus jobStatus)
        {
            var job = new Job(DateTimeOffset.Now, jobStatus) { Id = Guid.NewGuid() };
            _fakeBuildingGrbContext.Jobs.Add(job);
            _fakeBuildingGrbContext.SaveChanges();

            var request = new JobResultsPreSignedUrlRequest(job.Id);
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            await act
                .Should()
                .ThrowAsync<ApiException>()
                .Where(x =>
                    x.Message.Contains($"De status van de upload job '{job.Id}' is {job.Status.ToString().ToLower()}, hierdoor zijn er voor deze job geen resultaten beschikbaar.")
                    && x.StatusCode == StatusCodes.Status400BadRequest);
        }

        [Theory]
        [InlineData(JobStatus.Created)]
        [InlineData(JobStatus.Preparing)]
        [InlineData(JobStatus.Prepared)]
        [InlineData(JobStatus.Processing)]
        public async Task WithUncompletedJob_ThenThrowsApiException(JobStatus jobStatus)
        {
            var job = new Job(DateTimeOffset.Now, jobStatus) { Id = Guid.NewGuid() };
            _fakeBuildingGrbContext.Jobs.Add(job);
            _fakeBuildingGrbContext.SaveChanges();

            var request = new JobResultsPreSignedUrlRequest(job.Id);
            var act = async () => await _handler.Handle(request, CancellationToken.None);

            await act
                .Should()
                .ThrowAsync<ApiException>()
                .Where(x =>
                    x.Message.Contains($"Upload job '{job.Id}' wordt verwerkt en resultaten zijn nog niet beschikbaar.")
                    && x.StatusCode == StatusCodes.Status400BadRequest);
        }
    }
}
