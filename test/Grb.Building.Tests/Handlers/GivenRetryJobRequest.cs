namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Abstractions.Requests;
    using Api.Handlers;
    using AutoFixture;
    using AutoFixtures;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using NetTopologySuite.Geometries;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenRetryJobRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _buildingGrbContext;
        private readonly Mock<ITicketing> _ticketing;
        private readonly RetryJobHandler _handler;

        public GivenRetryJobRequest()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new WithUniqueInteger());
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
            _ticketing = new Mock<ITicketing>();
            _handler = new RetryJobHandler(_buildingGrbContext, _ticketing.Object);
        }

        [Fact]
        public async Task WithNotExistingJobId_ThenReturnsNotFound()
        {
            var act = () => _handler.Handle(new RetryJobRequest(_fixture.Create<Guid>()), CancellationToken.None);

            await act.Should()
                .ThrowAsync<ApiException>()
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == "Onbestaande upload job.");
        }

        [Theory]
        [InlineData(JobStatus.Created)]
        [InlineData(JobStatus.Preparing)]
        [InlineData(JobStatus.Prepared)]
        [InlineData(JobStatus.Processing)]
        [InlineData(JobStatus.Completed)]
        [InlineData(JobStatus.Cancelled)]
        public async Task WithJobNotInError_ThenReturnsBadRequest(JobStatus jobStatus)
        {
            var job = await AddJob(jobStatus);

            var act = () => _handler.Handle(new RetryJobRequest(job.Id), CancellationToken.None);

            await act.Should()
                .ThrowAsync<ApiException>()
                .Where(x =>
                    x.StatusCode == StatusCodes.Status400BadRequest
                    && x.Message == $"De status van de upload job '{job.Id}' is {jobStatus.ToString().ToLower()}, hierdoor kan deze job niet opnieuw verwerkt worden.");
            job.Status.Should().Be(jobStatus);
            _ticketing.Verify(x => x.Pending(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WithJobInErrorWithoutJobRecords_ThenJobIsCreatedAgain()
        {
            var job = await AddJob(JobStatus.Error);
            var lastChanged = job.LastChanged;

            await _handler.Handle(new RetryJobRequest(job.Id), CancellationToken.None);

            job.Status.Should().Be(JobStatus.Created);
            job.LastChanged.Should().BeAfter(lastChanged);
            _ticketing.Verify(x => x.Pending(job.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithJobInErrorWithJobRecordErrors_ThenErroredRecordsAreRetriedAndJobIsProcessing()
        {
            var job = await AddJob(JobStatus.Error);
            var errorRecord = AddJobRecord(job.Id, JobRecordStatus.Error);
            var completedRecord = AddJobRecord(job.Id, JobRecordStatus.Completed);
            var warningRecord = AddJobRecord(job.Id, JobRecordStatus.Warning);
            var resolvedRecord = AddJobRecord(job.Id, JobRecordStatus.ErrorResolved);
            await _buildingGrbContext.SaveChangesAsync();

            await _handler.Handle(new RetryJobRequest(job.Id), CancellationToken.None);

            job.Status.Should().Be(JobStatus.Processing);
            errorRecord.Status.Should().Be(JobRecordStatus.Created);
            errorRecord.TicketId.Should().BeNull();
            errorRecord.ErrorCode.Should().BeNull();
            errorRecord.ErrorMessage.Should().BeNull();
            completedRecord.Status.Should().Be(JobRecordStatus.Completed);
            warningRecord.Status.Should().Be(JobRecordStatus.Warning);
            resolvedRecord.Status.Should().Be(JobRecordStatus.ErrorResolved);
            _ticketing.Verify(x => x.Pending(job.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task WithJobInErrorWithJobRecordsButNoneInError_ThenJobIsPrepared()
        {
            var job = await AddJob(JobStatus.Error);
            var record = AddJobRecord(job.Id, JobRecordStatus.Created);
            await _buildingGrbContext.SaveChangesAsync();

            await _handler.Handle(new RetryJobRequest(job.Id), CancellationToken.None);

            job.Status.Should().Be(JobStatus.Prepared);
            record.Status.Should().Be(JobRecordStatus.Created);
            _ticketing.Verify(x => x.Pending(job.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task<Job> AddJob(JobStatus status)
        {
            var job = new Job(DateTimeOffset.Now.AddHours(-2), status, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync();
            return job;
        }

        private JobRecord AddJobRecord(Guid jobId, JobRecordStatus status)
        {
            var jobRecord = new JobRecord
            {
                Id = _fixture.Create<int>(),
                JobId = jobId,
                RecordNumber = _fixture.Create<int>(),
                Status = status,
                ErrorCode = status == JobRecordStatus.Error ? _fixture.Create<string>() : null,
                ErrorMessage = status == JobRecordStatus.Error ? _fixture.Create<string>() : null,
                EventType = GrbEventType.DefineBuilding,
                Geometry = (Polygon)GeometryHelper.ValidPolygon,
                GrbObject = GrbObject.BuildingAtGroundLevel,
                GrbObjectType = GrbObjectType.MainBuilding,
                GrId = 1,
                Idn = 3,
                TicketId = _fixture.Create<Guid>()
            };
            _buildingGrbContext.JobRecords.Add(jobRecord);
            return jobRecord;
        }
    }
}
