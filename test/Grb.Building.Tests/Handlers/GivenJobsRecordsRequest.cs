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
    using NetTopologySuite.Geometries;
    using Xunit;

    public class GivenJobsRecordsRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _buildingGrbContext;

        public GivenJobsRecordsRequest()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new WithUniqueInteger());
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
        }

        [Theory]
        [InlineData(JobStatus.Prepared)]
        [InlineData(JobStatus.Processing)]
        [InlineData(JobStatus.Completed)]
        [InlineData(JobStatus.Error)]
        public async Task ThenReturnJobsRecordsForJob(JobStatus jobStatus)
        {
            var job = new Job(DateTimeOffset.Now, jobStatus, _fixture.Create<Guid>());
            var anotherJob = new Job(DateTimeOffset.Now, jobStatus, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            _buildingGrbContext.Jobs.Add(anotherJob);
            var jobRecordOne = CreateJobRecord(job.Id);
            var jobRecordTwo = CreateJobRecord(job.Id);
            var jobRecordThree = CreateJobRecord(job.Id);
            _buildingGrbContext.JobRecords.Add(jobRecordOne);
            _buildingGrbContext.JobRecords.Add(jobRecordTwo);
            _buildingGrbContext.JobRecords.Add(jobRecordThree);
            CreateJobRecord(anotherJob.Id);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new JobResultsRequestHandler(_buildingGrbContext);
            var response = await handler.Handle(new JobRecordsRequest(job.Id), CancellationToken.None);

            response.JobRecords.Should().HaveCount(3);
            response.JobRecords.Should().ContainSingle(x =>
                x.JobId == jobRecordOne.JobId
                && x.JobRecordId == jobRecordOne.Id
                && x.RecordNumber == jobRecordOne.RecordNumber
                && x.Status == jobRecordOne.Status
                && x.ErrorMessage == jobRecordOne.ErrorMessage);
            response.JobRecords.Should().ContainSingle(x =>
                x.JobId == jobRecordTwo.JobId
                && x.JobRecordId == jobRecordTwo.Id
                && x.RecordNumber == jobRecordTwo.RecordNumber
                && x.Status == jobRecordTwo.Status
                && x.ErrorMessage == jobRecordTwo.ErrorMessage);
            response.JobRecords.Should().ContainSingle(x =>
                x.JobId == jobRecordThree.JobId
                && x.JobRecordId == jobRecordThree.Id
                && x.RecordNumber == jobRecordThree.RecordNumber
                && x.Status == jobRecordThree.Status
                && x.ErrorMessage == jobRecordThree.ErrorMessage);
        }

        [Theory]
        [InlineData(JobStatus.Created)]
        [InlineData(JobStatus.Preparing)]
        public async Task WithInvalidJobStatus_ThenThrowsApiException(JobStatus jobStatus)
        {
            var job = new Job(DateTimeOffset.Now, jobStatus, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(job);
            var jobRecord = CreateJobRecord(job.Id);
            _buildingGrbContext.JobRecords.Add(jobRecord);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var handler = new JobResultsRequestHandler(_buildingGrbContext);
            var act = async () => await handler.Handle(new JobRecordsRequest(job.Id), CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status400BadRequest
                    && x.Message == $"Upload job '{job.Id}' heeft nog geen bescikbare job records.");
        }

        [Fact]
        public async Task WithUnexistingJob_ThenThrowsApiException()
        {
            var jobId = _fixture.Create<Guid>();

            var handler = new JobResultsRequestHandler(_buildingGrbContext);
            var act = async () => await handler.Handle(new JobRecordsRequest(jobId), CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == "Onbestaande upload job.");
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
