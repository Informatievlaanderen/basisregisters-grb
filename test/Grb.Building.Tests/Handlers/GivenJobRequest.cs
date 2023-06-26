namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Handlers;
    using Api.Infrastructure;
    using AutoFixture;
    using AutoFixtures;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Moq;
    using NetTopologySuite.Geometries;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenJobRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _buildingGrbContext;
        private readonly Mock<ITicketingUrl> _ticketingUrl;

        public GivenJobRequest()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new WithUniqueInteger());
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
            _ticketingUrl = new Mock<ITicketingUrl>();
            _ticketingUrl
                .Setup(x => x.For(It.IsAny<Guid>()))
                .Returns<Guid>(ticketId => new Uri(GetTicketUrl(ticketId)));
        }

        private static string GetTicketUrl(Guid ticketId)
        {
            return $"https://api.basisregisters.vlaanderen.be/v2/tickets/{ticketId}";
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

            var mockPagedUriGenerator = new Mock<IPagedUriGenerator>();

            var handler = new GetJobByIdHandler(_buildingGrbContext, _ticketingUrl.Object, mockPagedUriGenerator.Object);
            var response = await handler.Handle(new GetJobByIdRequest(job.Id), CancellationToken.None);

            response.Id.Should().Be(job.Id);
            response.Created.Should().Be(job.Created);
            response.Status.Should().Be(job.Status);
            response.TicketUrl.Should().Be(GetTicketUrl(job.TicketId!.Value));
        }

        [Fact]
        public Task WithUnexistingJob_ThenThrowsApiException()
        {
            var jobId = _fixture.Create<Guid>();

            var mockPagedUriGenerator = new Mock<IPagedUriGenerator>();
            var handler = new GetJobByIdHandler(_buildingGrbContext, _ticketingUrl.Object, mockPagedUriGenerator.Object);
            var act = async () => await handler.Handle(new GetJobByIdRequest(jobId), CancellationToken.None);

            act.Should()
                .ThrowAsync<ApiException>()
                .Result
                .Where(x =>
                    x.StatusCode == StatusCodes.Status404NotFound
                    && x.Message == "Onbestaande upload job.");
            return Task.CompletedTask;
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
