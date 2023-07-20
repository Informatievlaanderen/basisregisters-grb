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
    using NetTopologySuite.Geometries;
    using Notifications;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenPreparedJob
    {
        [Fact]
        public async Task ThenProcessJob()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var jobRecordsProcessor = new Mock<IJobRecordsProcessor>();
            var jobRecordsMonitor = new Mock<IJobRecordsMonitor>();
            var ticketing = new Mock<ITicketing>();
            var jobResultsUploader = new Mock<IJobResultUploader>();
            var jobRecordsArchiver = new Mock<IJobRecordsArchiver>();
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();
            var notificationsService = new Mock<INotificationService>();

            var grbApiBaseUrl = "https://api-vlaanderen.be";
            var jobProcessor = new JobProcessor(
                buildingGrbContext,
                jobRecordsProcessor.Object,
                jobRecordsMonitor.Object,
                jobResultsUploader.Object,
                jobRecordsArchiver.Object,
                ticketing.Object,
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = grbApiBaseUrl }),
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var job = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Prepared, ticketId: Guid.NewGuid());
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

        [Fact]
        public async Task WhenJobRecordErrors_ThenJobError()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var jobRecordsProcessor = new Mock<IJobRecordsProcessor>();
            var jobRecordsMonitor = new Mock<IJobRecordsMonitor>();
            var jobResultsUploader = new Mock<IJobResultUploader>();
            var jobRecordsArchiver = new Mock<IJobRecordsArchiver>();
            var ticketing = new Mock<ITicketing>();
            var notificationsService = new Mock<INotificationService>();
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var jobProcessor = new JobProcessor(
                buildingGrbContext,
                jobRecordsProcessor.Object,
                jobRecordsMonitor.Object,
                jobResultsUploader.Object,
                jobRecordsArchiver.Object,
                ticketing.Object,
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions
                    { PublicApiUrl = "https://api-vlaanderen.be" }),
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var job = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Prepared, ticketId: Guid.NewGuid());
            buildingGrbContext.Jobs.Add(job);
            var jobRecord1 = new JobRecord
            {
                Id = 123456,
                JobId = job.Id,
                Status = JobRecordStatus.Error,
                ErrorCode = "Code1",
                ErrorMessage = "Error1",
                Geometry = (Polygon)GeometryHelper.ValidPolygon,
            };
            var jobRecord2 = new JobRecord
            {
                Id = 654321,
                JobId = job.Id,
                Status = JobRecordStatus.Error,
                ErrorCode = "Code2",
                ErrorMessage = "Error2",
                Geometry = (Polygon)GeometryHelper.ValidPolygon,
            };
            buildingGrbContext.JobRecords.AddRange(new[]
            {
                jobRecord1,
                jobRecord2
            });
            await buildingGrbContext.SaveChangesAsync();

            //act
            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask;

            //assert
            var jobEntity = buildingGrbContext.Jobs.FirstOrDefault(x => x.Id == job.Id);
            jobEntity.Should().NotBeNull();
            jobEntity!.Status.Should().Be(JobStatus.Error);

            jobRecordsProcessor.Verify(x => x.Process(job.Id, It.IsAny<CancellationToken>()), Times.Once);
            jobRecordsMonitor.Verify(x => x.Monitor(job.Id, It.IsAny<CancellationToken>()), Times.Once);

            ticketing.Verify(x => x.Error(
                    job.TicketId!.Value,
                    It.Is<TicketError>(y =>
                        y.Errors!.Contains(new TicketError(jobRecord1.ErrorMessage, jobRecord1.ErrorCode))
                        && y.Errors!.Contains(new TicketError(jobRecord2.ErrorMessage, jobRecord2.ErrorCode))),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);

            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
        }
    }
}
