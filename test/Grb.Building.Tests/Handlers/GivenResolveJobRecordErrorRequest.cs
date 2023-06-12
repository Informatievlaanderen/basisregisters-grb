namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Uploads;
    using AutoFixture;
    using AutoFixtures;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using NetTopologySuite.Geometries;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenResolveJobRecordErrorRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _buildingGrbContext;
        private readonly Mock<ITicketing> _ticketing;

        public GivenResolveJobRecordErrorRequest()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new WithUniqueInteger());
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
            _ticketing = new Mock<ITicketing>();
        }

        [Fact]
        public async Task WithMultipleJobRecordsInError_ThenJobRecordStatusBecomesErrorResolved()
        {
            var job = new Job(DateTimeOffset.Now, JobStatus.Error, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            var jobRecordOne = CreateJobRecord(job.Id, JobRecordStatus.Error);
            var jobRecordTwo = CreateJobRecord(job.Id, JobRecordStatus.Error);
            _buildingGrbContext.JobRecords.Add(jobRecordOne);
            _buildingGrbContext.JobRecords.Add(jobRecordTwo);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new ResolveJobRecordErrorHandler(_buildingGrbContext, _ticketing.Object);
            await handler.Handle(new ResolveJobRecordErrorRequest(job.Id, jobRecordOne.Id), CancellationToken.None);

            job.Status.Should().Be(JobStatus.Error);
            jobRecordOne.Status.Should().Be(JobRecordStatus.ErrorResolved);
            jobRecordOne.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            jobRecordTwo.Status.Should().Be(JobRecordStatus.Error);

            _ticketing.Verify(x => x.Pending(job.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithSingleJobRecordsInError_ThenJobContinuesProcessing()
        {
            var job = new Job(DateTimeOffset.Now, JobStatus.Error, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            var jobRecordOne = CreateJobRecord(job.Id, JobRecordStatus.Error);
            var jobRecordTwo = CreateJobRecord(job.Id, JobRecordStatus.ErrorResolved);
            _buildingGrbContext.JobRecords.Add(jobRecordOne);
            _buildingGrbContext.JobRecords.Add(jobRecordTwo);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new ResolveJobRecordErrorHandler(_buildingGrbContext, _ticketing.Object);
            await handler.Handle(new ResolveJobRecordErrorRequest(job.Id, jobRecordOne.Id), CancellationToken.None);

            job.Status.Should().Be(JobStatus.Processing);
            jobRecordOne.Status.Should().Be(JobRecordStatus.ErrorResolved);
            jobRecordOne.ErrorMessage.Should().NotBeNullOrWhiteSpace();

            _ticketing.Verify(x => x.Pending(job.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(JobRecordStatus.Created)]
        [InlineData(JobRecordStatus.Pending)]
        [InlineData(JobRecordStatus.Warning)]
        [InlineData(JobRecordStatus.Completed)]
        public async Task WithJobRecordsNotInError_ThenNothing(JobRecordStatus jobRecordStatus)
        {
            var job = new Job(DateTimeOffset.Now, JobStatus.Processing, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            var jobRecordOne = CreateJobRecord(job.Id, jobRecordStatus);
            var jobRecordTwo = CreateJobRecord(job.Id, jobRecordStatus);
            _buildingGrbContext.JobRecords.Add(jobRecordOne);
            _buildingGrbContext.JobRecords.Add(jobRecordTwo);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new ResolveJobRecordErrorHandler(_buildingGrbContext, _ticketing.Object);
            await handler.Handle(new ResolveJobRecordErrorRequest(job.Id, jobRecordOne.Id), CancellationToken.None);

            job.Status.Should().Be(JobStatus.Processing);
            jobRecordOne.Status.Should().Be(jobRecordStatus);

            _ticketing.Verify(x => x.Pending(job.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public Task WithUnexistingJob_ThenThrowsApiException()
        {
            var jobId = _fixture.Create<Guid>();
            var jobRecordId = _fixture.Create<long>();

            var handler = new ResolveJobRecordErrorHandler(_buildingGrbContext, _ticketing.Object);
            var act = async () => await handler.Handle(
                new ResolveJobRecordErrorRequest(jobId, jobRecordId), CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == "Onbestaande upload job.");
            return Task.CompletedTask;
        }

        [Fact]
        public async Task WithUnexistingJobRecord_ThenThrowsApiException()
        {
            var job = new Job(DateTimeOffset.Now, JobStatus.Processing, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);
            var jobRecordId = _fixture.Create<long>();

            var handler = new ResolveJobRecordErrorHandler(_buildingGrbContext, _ticketing.Object);
            var act = async () => await handler.Handle(
                new ResolveJobRecordErrorRequest(job.Id, jobRecordId), CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == "Onbestaande upload job record.");
        }

        private JobRecord CreateJobRecord(Guid jobId, JobRecordStatus jobRecordStatus)
        {
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
