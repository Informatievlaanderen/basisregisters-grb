﻿namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using FluentAssertions;
    using Grb.Building.Api.Uploads;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    public class GivenCancelJobRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _fakeBuildingGrbContext;

        public GivenCancelJobRequest()
        {
            _fixture = new Fixture();
            _fakeBuildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
        }

        [Fact]
        public void WithNotExistingJobId_ThenReturnsNotFound()
        {
            var jobId = _fixture.Create<Guid>();
            var request = new CancelJobRequest(jobId);
            var handler = new CancelJobHandler(_fakeBuildingGrbContext);

            var act = () => handler.Handle(request, CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == $"Upload job with id {jobId} not found.");
        }

        [Fact]
        public void WithNonCreatedJob_ThenReturnsBadRequest()
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(JobStatus.Prepared);
            _fakeBuildingGrbContext.Jobs.Add(job);

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_fakeBuildingGrbContext);

            // Act
            var act = () => handler.Handle(request, CancellationToken.None);

            // Assert
            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status400BadRequest
                    && x.Message == $"Upload job with id {job.Id} is being processed and cannot be cancelled.");
        }

        [Fact]
        public async Task ThenJobIsCancelled()
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(JobStatus.Created);
            _fakeBuildingGrbContext.Jobs.Add(job);

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_fakeBuildingGrbContext);

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            job.Status.Should().Be(JobStatus.Cancelled);
        }
    }
}
