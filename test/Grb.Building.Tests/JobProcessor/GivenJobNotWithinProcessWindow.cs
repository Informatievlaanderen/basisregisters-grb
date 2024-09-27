namespace Grb.Building.Tests.JobProcessor
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Grb.Building.Processor.Job;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using NodaTime;
    using Notifications;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenJobNotWithinProcessWindow
    {
        [Theory]
        [InlineData(7)]
        [InlineData(10)]
        [InlineData(18)]
        public async Task ThenNothing(int currentHour)
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var jobRecordsProcessor = new Mock<IJobRecordsProcessor>();
            var jobRecordsMonitor = new Mock<IJobRecordsMonitor>();
            var jobResultsUploader = new Mock<IJobResultUploader>();
            var jobRecordsArchiver = new Mock<IJobRecordsArchiver>();
            var notificationsService = new Mock<INotificationService>();
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var clock = new Mock<IClock>();
            clock
                .Setup(x => x.GetCurrentInstant())
                .Returns(Instant.FromDateTimeOffset(new DateTimeOffset(2020, 1, 1, currentHour, 0, 0, TimeSpan.FromHours(1))));

            var jobProcessor = new JobProcessor(
                buildingGrbContext,
                jobRecordsProcessor.Object,
                jobRecordsMonitor.Object,
                jobResultsUploader.Object,
                jobRecordsArchiver.Object,
                Mock.Of<ITicketing>(),
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = "https://api-vlaanderen.be"}),
                new OptionsWrapper<ProcessWindowOptions>(new ProcessWindowOptions { FromHour = 19, UntilHour = 7 }),
                clock.Object,
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var job = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Prepared);
            buildingGrbContext.Jobs.Add(job);
            await buildingGrbContext.SaveChangesAsync();

            //act
            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask;

            //assert
            jobRecordsProcessor.Verify(x => x.Process(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            jobRecordsMonitor.Verify(x => x.Monitor(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            jobResultsUploader.Verify(x => x.UploadJobResults(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            jobRecordsArchiver.Verify(x => x.Archive(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);
            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
        }

        [Theory]
        [InlineData(7)]
        // [InlineData(10)]
        // [InlineData(18)]
        public async Task WithForceProcessing_ThenProcessJob(int currentHour)
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var jobRecordsProcessor = new Mock<IJobRecordsProcessor>();
            var jobRecordsMonitor = new Mock<IJobRecordsMonitor>();
            var ticketing = new Mock<ITicketing>();
            var jobResultsUploader = new Mock<IJobResultUploader>();
            var jobRecordsArchiver = new Mock<IJobRecordsArchiver>();
            var notificationsService = new Mock<INotificationService>();
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var clock = new Mock<IClock>();
            clock
                .Setup(x => x.GetCurrentInstant())
                .Returns(Instant.FromDateTimeOffset(new DateTimeOffset(2020, 1, 1, currentHour, 0, 0, TimeSpan.FromHours(1))));

            var grbApiBaseUrl = "https://api-vlaanderen.be";
            var jobProcessor = new JobProcessor(
                buildingGrbContext,
                jobRecordsProcessor.Object,
                jobRecordsMonitor.Object,
                jobResultsUploader.Object,
                jobRecordsArchiver.Object,
                ticketing.Object,
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = grbApiBaseUrl }),
                new OptionsWrapper<ProcessWindowOptions>(new ProcessWindowOptions { FromHour = 19, UntilHour = 7 }),
                clock.Object,
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var job = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Prepared, ticketId: Guid.NewGuid()) { ForceProcessing = true };
            buildingGrbContext.Jobs.Add(job);
            await buildingGrbContext.SaveChangesAsync();

            //act
            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask;

            //assert
            var jobEntity = buildingGrbContext.Jobs.FirstOrDefault(x => x.Id == job.Id);
            jobEntity.Should().NotBeNull();
            jobEntity!.Status.Should().Be(JobStatus.Completed);

            jobRecordsProcessor.Verify(x => x.Process(job.Id, It.IsAny<CancellationToken>()), Times.Once);
            jobRecordsMonitor.Verify(x => x.Monitor(job.Id, It.IsAny<CancellationToken>()), Times.Once);
            jobResultsUploader.Verify(x => x.UploadJobResults(job.Id, It.IsAny<CancellationToken>()));

            var expectedTicketResultAsJson = new TicketResult(new
            {
                JobResultLocation = new Uri(new Uri(grbApiBaseUrl), $"/v2/gebouwen/uploads/jobs/{job.Id:D}/results").ToString()
            }).ResultAsJson;

            ticketing.Verify(x => x.Complete(
                job.TicketId!.Value,
                It.Is<TicketResult>(y => y.ResultAsJson == expectedTicketResultAsJson),
                It.IsAny<CancellationToken>()));

            jobRecordsArchiver.Verify(x => x.Archive(job.Id, It.IsAny<CancellationToken>()));
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);
            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
        }
    }
}
