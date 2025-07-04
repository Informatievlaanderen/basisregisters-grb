﻿namespace Grb.Building.Tests.JobProcessor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Notifications;
    using Processor.Job;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenCancelledJob
    {
        [Fact]
        public async Task ThenNothing()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var jobRecordsProcessor = new Mock<IJobRecordsProcessor>();
            var jobRecordsMonitor = new Mock<IJobRecordsMonitor>();
            var mockJobResultsUploader = new Mock<IJobResultUploader>();
            var mockJobRecordsArchiver = new Mock<IJobRecordsArchiver>();
            var notificationsService = new Mock<INotificationService>();
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var jobProcessor = new JobProcessor(
                buildingGrbContext,
                jobRecordsProcessor.Object,
                jobRecordsMonitor.Object,
                mockJobResultsUploader.Object,
                mockJobRecordsArchiver.Object,
                Mock.Of<ITicketing>(),
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = "https://api-vlaanderen.be" }),
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var job = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Cancelled);
            buildingGrbContext.Jobs.Add(job);
            await buildingGrbContext.SaveChangesAsync();

            //act
            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask!;

            //assert
            jobRecordsProcessor.Verify(x => x.Process(job.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
            jobRecordsMonitor.Verify(x => x.Monitor(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            mockJobResultsUploader.Verify(x => x.UploadJobResults(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            mockJobRecordsArchiver.Verify(x => x.Archive(job.Id, It.IsAny<CancellationToken>()), Times.Never);
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);
            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
        }
    }
}
