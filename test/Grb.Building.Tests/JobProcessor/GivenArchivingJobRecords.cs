namespace Grb.Building.Tests.JobProcessor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using NetTopologySuite.Geometries;
    using Notifications;
    using Processor.Job;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenArchivingJobRecords
    {
        private readonly FakeBuildingGrbContext _buildingGrbContext;
        private readonly Mock<IJobRecordsArchiver> _jobRecordsArchiver;
        private readonly Mock<INotificationService> _notificationsService;
        private readonly Mock<IHostApplicationLifetime> _hostApplicationLifetime;
        private readonly JobProcessor _jobProcessor;

        public GivenArchivingJobRecords()
        {
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
            _jobRecordsArchiver = new Mock<IJobRecordsArchiver>();
            _notificationsService = new Mock<INotificationService>();
            _hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            _jobProcessor = new JobProcessor(
                _buildingGrbContext,
                Mock.Of<IJobRecordsProcessor>(),
                Mock.Of<IJobRecordsMonitor>(),
                Mock.Of<IJobResultUploader>(),
                _jobRecordsArchiver.Object,
                Mock.Of<ITicketing>(),
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = "https://api-vlaanderen.be" }),
                _hostApplicationLifetime.Object,
                _notificationsService.Object,
                new NullLoggerFactory());
        }

        [Fact]
        public async Task WithCompletedJobThatStillHasRecords_ThenRecordsAreArchived()
        {
            var completedJobWithRecords = AddJob(JobStatus.Completed);
            AddJobRecord(completedJobWithRecords.Id);
            AddJobRecord(completedJobWithRecords.Id);
            var completedJobWithoutRecords = AddJob(JobStatus.Completed);
            var erroredJobWithRecords = AddJob(JobStatus.Error);
            AddJobRecord(erroredJobWithRecords.Id);
            await _buildingGrbContext.SaveChangesAsync();

            await _jobProcessor.StartAsync(CancellationToken.None);
            await _jobProcessor.ExecuteTask!;

            _jobRecordsArchiver.Verify(x => x.Archive(completedJobWithRecords.Id, It.IsAny<CancellationToken>()), Times.Once);
            _jobRecordsArchiver.Verify(x => x.Archive(completedJobWithoutRecords.Id, It.IsAny<CancellationToken>()), Times.Never);
            _jobRecordsArchiver.Verify(x => x.Archive(erroredJobWithRecords.Id, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task WhenArchivingFails_ThenJobIsStillCompletedAndFailureIsNotified()
        {
            var job = AddJob(JobStatus.Prepared);
            await _buildingGrbContext.SaveChangesAsync();

            _jobRecordsArchiver
                .Setup(x => x.Archive(job.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Simulated archive failure."));

            await _jobProcessor.StartAsync(CancellationToken.None);
            await _jobProcessor.ExecuteTask!;

            _jobProcessor.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
            job.Status.Should().Be(JobStatus.Completed);
            _notificationsService.Verify(x => x.PublishToTopicAsync(
                It.Is<NotificationMessage>(y => y.BasisregistersError.StartsWith("ArchiveFailed"))), Times.Once);
            _notificationsService.Verify(x => x.PublishToTopicAsync(
                It.Is<NotificationMessage>(y => y.BasisregistersError.StartsWith("JobCompleted"))), Times.Once);
            _hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
        }

        private Job AddJob(JobStatus status)
        {
            var job = new Job(DateTimeOffset.Now.AddMinutes(-10), status, ticketId: Guid.NewGuid());
            _buildingGrbContext.Jobs.Add(job);
            return job;
        }

        private void AddJobRecord(Guid jobId)
        {
            _buildingGrbContext.JobRecords.Add(new JobRecord
            {
                JobId = jobId,
                Status = JobRecordStatus.Completed,
                Geometry = (Polygon)GeometryHelper.ValidPolygon
            });
        }
    }
}
