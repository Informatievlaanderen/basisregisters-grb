namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Abstractions.Requests;
    using Api.Handlers;
    using AutoFixture;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using NetTopologySuite.Geometries;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenCancelJobRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _buildingGrbContext;

        public GivenCancelJobRequest()
        {
            _fixture = new Fixture();
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
        }

        [Fact]
        public void WithNotExistingJobId_ThenReturnsNotFound()
        {
            var jobId = _fixture.Create<Guid>();
            var request = new CancelJobRequest(jobId);
            var handler = new CancelJobHandler(_buildingGrbContext, Mock.Of<ITicketing>());

            var act = () => handler.Handle(request, CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == "Onbestaande upload job.");
        }

        [Theory]
        [InlineData(JobStatus.Preparing)]
        [InlineData(JobStatus.Prepared)]
        [InlineData(JobStatus.Processing)]
        [InlineData(JobStatus.Completed)]
        public void WithJobBeingProcessedOrCompleted_ThenReturnsBadRequest(JobStatus jobStatus)
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(jobStatus);
            _buildingGrbContext.Jobs.Add(job);

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_buildingGrbContext, Mock.Of<ITicketing>());

            // Act
            var act = () => handler.Handle(request, CancellationToken.None);

            // Assert
            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status400BadRequest
                    && x.Message == $"De status van de upload job '{job.Id}' is {job.Status.ToString().ToLower()}, hierdoor kan deze job niet geannuleerd worden.");
        }

        [Fact]
        public async Task WithJobInStatusCreated_ThenJobIsCancelled()
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(JobStatus.Created);
            _buildingGrbContext.Jobs.Add(job);

            var ticketing = new Mock<ITicketing>();
            ticketing
                .Setup(x => x.Get(job.TicketId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Ticket(
                        job.TicketId!.Value,
                        TicketStatus.Created,
                        new Dictionary<string, string>()
                        ));

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_buildingGrbContext, ticketing.Object);

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            job.Status.Should().Be(JobStatus.Cancelled);
            ticketing.Verify(x => x.Complete(
                job.TicketId!.Value,
                new TicketResult(new { JobStatus = "Cancelled" }),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WithJobInStatusErrorAndWithoutJobRecords_ThenJobIsCancelled()
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(JobStatus.Error);
            _buildingGrbContext.Jobs.Add(job);

            var ticketing = new Mock<ITicketing>();
            ticketing
                .Setup(x => x.Get(job.TicketId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Ticket(
                        job.TicketId!.Value,
                        TicketStatus.Created,
                        new Dictionary<string, string>()
                    ));

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_buildingGrbContext, ticketing.Object);

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            job.Status.Should().Be(JobStatus.Cancelled);
            ticketing.Verify(x => x.Complete(
                job.TicketId!.Value,
                new TicketResult(new { JobStatus = "Cancelled" }),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WithJobInStatusErrorAndWithoutJobRecordErrors_ThenJobIsCancelledAndErrorsInsertedIntoTicket()
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(JobStatus.Error);
            _buildingGrbContext.Jobs.Add(job);

            var expectedErrors = new TicketError("Something went wrong", "ErrorCode");
            var ticketing = new Mock<ITicketing>();
            ticketing
                .Setup(x => x.Get(job.TicketId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Ticket(
                        job.TicketId!.Value,
                        TicketStatus.Error,
                        new Dictionary<string, string>(),
                        new TicketResult(expectedErrors)
                    ));

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_buildingGrbContext, ticketing.Object);

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            job.Status.Should().Be(JobStatus.Cancelled);
            ticketing.Verify(x => x.Complete(
                job.TicketId!.Value,
                new TicketResult(new { JobStatus = "Cancelled", Error = expectedErrors}),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WithJobInStatusErrorAndWithJobRecords_ThenReturnsBadRequest()
        {
            // Arrange
            var job = _fixture.Create<Job>();
            job.UpdateStatus(JobStatus.Error);
            _buildingGrbContext.Jobs.Add(job);
            var jobRecord = CreateJobRecord(job.Id);
            _buildingGrbContext.JobRecords.Add(jobRecord);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var ticketing = new Mock<ITicketing>();

            var request = new CancelJobRequest(job.Id);
            var handler = new CancelJobHandler(_buildingGrbContext, ticketing.Object);

            // Act
            var act = () => handler.Handle(request, CancellationToken.None);

            // Assert
            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status400BadRequest
                    && x.Message == $"De status van de upload job '{job.Id}' is {job.Status.ToString().ToLower()}, hierdoor kan deze job niet geannuleerd worden.");
        }

        private JobRecord CreateJobRecord(Guid jobId)
        {
            var jobRecordStatus = _fixture.Create<JobRecordStatus>();

            return new JobRecord
            {
                Id = _fixture.Create<int>(),
                JobId = jobId,
                RecordNumber = _fixture.Create<int>(),
                Status = jobRecordStatus,
                ErrorMessage = jobRecordStatus == JobRecordStatus.Error ? _fixture.Create<string>() : null,
                EventType = GrbEventType.DefineBuilding,
                Geometry = (Polygon) GeometryHelper.ValidPolygon,
                GrbObject = GrbObject.BuildingAtGroundLevel,
                GrbObjectType = GrbObjectType.MainBuilding,
                GrId = 1,
                Idn = 3,
                TicketId = _fixture.Create<Guid>()
            };
        }
    }
}
