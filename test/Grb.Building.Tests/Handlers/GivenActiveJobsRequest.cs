namespace Grb.Building.Tests.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Api.Handlers;
    using Api.Infrastructure;
    using AutoFixture;
    using FluentAssertions;
    using Moq;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenActiveJobsRequest
    {
        private readonly Fixture _fixture;
        private readonly FakeBuildingGrbContext _buildingGrbContext;
        private readonly Mock<ITicketingUrl> _ticketingUrl;

        public GivenActiveJobsRequest()
        {
            _fixture = new Fixture();
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

        [Fact]
        public async Task ThenReturnActiveJobs()
        {
            var jobInStatusCreated = new Job(DateTimeOffset.Now, JobStatus.Created, _fixture.Create<Guid>());
            var jobInStatusPreparing = new Job(DateTimeOffset.Now, JobStatus.Preparing, _fixture.Create<Guid>());
            var jobInStatusPrepared = new Job(DateTimeOffset.Now, JobStatus.Prepared, _fixture.Create<Guid>());
            var jobInStatusProcessing = new Job(DateTimeOffset.Now, JobStatus.Processing, _fixture.Create<Guid>());
            var jobInStatusError = new Job(DateTimeOffset.Now, JobStatus.Error, _fixture.Create<Guid>());
            var jobInStatusCancelled = new Job(DateTimeOffset.Now, JobStatus.Cancelled, _fixture.Create<Guid>());
            var jobInStatusCompleted = new Job(DateTimeOffset.Now, JobStatus.Completed, _fixture.Create<Guid>());
            _buildingGrbContext.Jobs.Add(jobInStatusCreated);
            _buildingGrbContext.Jobs.Add(jobInStatusPreparing);
            _buildingGrbContext.Jobs.Add(jobInStatusPrepared);
            _buildingGrbContext.Jobs.Add(jobInStatusProcessing);
            _buildingGrbContext.Jobs.Add(jobInStatusError);
            _buildingGrbContext.Jobs.Add(jobInStatusCancelled);
            _buildingGrbContext.Jobs.Add(jobInStatusCompleted);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var mockPagedUriGenerator = new Mock<IPagedUriGenerator>();

            var handler = new GetActiveJobsHandler(_buildingGrbContext, _ticketingUrl.Object, mockPagedUriGenerator.Object);
            var response = await handler.Handle(new GetActiveJobsRequest(), CancellationToken.None);

            response.Jobs.Should().HaveCount(5);
            // TODO: because use of anonymous object[] cannot assert here
            // response.Jobs.Should().ContainSingle(x =>
            //     x.Id == jobInStatusCreated.Id
            //     && x.Created == jobInStatusCreated.Created
            //     && x.Status == jobInStatusCreated.Status
            //     && x.TicketUrl == GetTicketUrl(jobInStatusCreated.TicketId!.Value));
            // response.Jobs.Should().ContainSingle(x =>
            //     x.JobId == jobInStatusPreparing.Id
            //     && x.Created == jobInStatusPreparing.Created
            //     && x.Status == jobInStatusPreparing.Status
            //     && x.TicketUrl == GetTicketUrl(jobInStatusPreparing.TicketId!.Value));
            // response.Jobs.Should().ContainSingle(x =>
            //     x.JobId == jobInStatusPrepared.Id
            //     && x.Created == jobInStatusPrepared.Created
            //     && x.Status == jobInStatusPrepared.Status
            //     && x.TicketUrl == GetTicketUrl(jobInStatusPrepared.TicketId!.Value));
            // response.Jobs.Should().ContainSingle(x =>
            //     x.JobId == jobInStatusProcessing.Id
            //     && x.Created == jobInStatusProcessing.Created
            //     && x.Status == jobInStatusProcessing.Status
            //     && x.TicketUrl == GetTicketUrl(jobInStatusProcessing.TicketId!.Value));
            // response.Jobs.Should().ContainSingle(x =>
            //     x.JobId == jobInStatusError.Id
            //     && x.Created == jobInStatusError.Created
            //     && x.Status == jobInStatusError.Status
            //     && x.TicketUrl == GetTicketUrl(jobInStatusError.TicketId!.Value));
        }
    }
}
