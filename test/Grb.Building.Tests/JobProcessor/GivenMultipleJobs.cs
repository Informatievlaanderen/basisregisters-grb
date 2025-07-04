﻿namespace Grb.Building.Tests.JobProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    public class GivenMultipleJobs
    {
        [Fact]
        public async Task WithFirstJobNotPrepared_ThenNothing()
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
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = "https://api-vlaanderen.be"}),
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var job1 = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Created);
            buildingGrbContext.Jobs.Add(job1);
            var job2 = new Job(DateTimeOffset.Now.AddMinutes(-9), JobStatus.Prepared);
            buildingGrbContext.Jobs.Add(job2);
            await buildingGrbContext.SaveChangesAsync();

            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask!;

            jobRecordsProcessor.Verify(x => x.Process(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()), Times.Never);
            jobRecordsMonitor.Verify(x => x.Monitor(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

            mockJobRecordsArchiver.Verify(x => x.Archive(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            mockJobResultsUploader.Verify(x => x.UploadJobResults(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);

            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task WithOnlyFirstJobPrepared_ThenProcessFirstJobOnly()
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
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = "https://api-vlaanderen.be"}),
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var firstJob = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Prepared, ticketId: Guid.NewGuid()) { Id = Guid.NewGuid() };
            var secondJob = new Job(DateTimeOffset.Now.AddMinutes(-9), JobStatus.Created) { Id = Guid.NewGuid() };
            buildingGrbContext.Jobs.Add(firstJob);
            buildingGrbContext.Jobs.Add(secondJob);

            var jobRecordOfFirstJob = CreateJobRecord(firstJob.Id, 1);
            var jobRecordOfSecondJob = CreateJobRecord(secondJob.Id, 2);
            buildingGrbContext.JobRecords.Add(jobRecordOfFirstJob);
            buildingGrbContext.JobRecords.Add(jobRecordOfSecondJob);

            await buildingGrbContext.SaveChangesAsync();

            //act
            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask!;

            //assert
            jobRecordsProcessor.Verify(x =>
                x.Process(It.Is<Guid>(y => y == firstJob.Id), false, It.IsAny<CancellationToken>()), Times.Once);
            jobRecordsProcessor.Verify(x =>
                x.Process(It.Is<Guid>(y => y == secondJob.Id), false, It.IsAny<CancellationToken>()), Times.Never);
            firstJob.Status.Should().Be(JobStatus.Completed);
            firstJob.LastChanged.Should().BeAfter(firstJob.Created);
            jobRecordsMonitor.Verify(x =>
                x.Monitor(It.Is<Guid>(y => y == firstJob.Id), It.IsAny<CancellationToken>()), Times.Once);
            jobRecordsMonitor.Verify(x =>
                x.Monitor(It.Is<Guid>(y => y == secondJob.Id), It.IsAny<CancellationToken>()), Times.Never);
            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);
            mockJobResultsUploader.Verify(x => x.UploadJobResults(firstJob.Id, It.IsAny<CancellationToken>()), Times.Once);
            mockJobRecordsArchiver.Verify(x => x.Archive(firstJob.Id, It.IsAny<CancellationToken>()), Times.Once);
            mockJobResultsUploader.Verify(x => x.UploadJobResults(secondJob.Id, It.IsAny<CancellationToken>()), Times.Never);
            mockJobRecordsArchiver.Verify(x => x.Archive(secondJob.Id, It.IsAny<CancellationToken>()), Times.Never);
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);
        }

        [Fact]
        public async Task WithAllJobsPrepared_ThenProcessAllJobsInOrder()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var jobRecordExecutionSequence = new List<Guid>();
            var jobRecordsProcessor = new Mock<IJobRecordsProcessor>();
            jobRecordsProcessor
                .Setup(x => x.Process(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, bool, CancellationToken>((jobId, _, _) => jobRecordExecutionSequence.Add(jobId));
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
                new OptionsWrapper<GrbApiOptions>(new GrbApiOptions { PublicApiUrl = "https://api-vlaanderen.be"}),
                hostApplicationLifetime.Object,
                notificationsService.Object,
                new NullLoggerFactory());

            var firstJob = new Job(DateTimeOffset.Now.AddMinutes(-10), JobStatus.Prepared, ticketId: Guid.NewGuid()) { Id = Guid.NewGuid() };
            var secondJob = new Job(DateTimeOffset.Now.AddMinutes(-9), JobStatus.Prepared, ticketId: Guid.NewGuid()) { Id = Guid.NewGuid() };
            buildingGrbContext.Jobs.Add(firstJob);
            buildingGrbContext.Jobs.Add(secondJob);

            var jobRecordOfFirstJob = CreateJobRecord(firstJob.Id, 1);
            var jobRecordOfSecondJob = CreateJobRecord(secondJob.Id, 2);
            buildingGrbContext.JobRecords.Add(jobRecordOfFirstJob);
            buildingGrbContext.JobRecords.Add(jobRecordOfSecondJob);

            await buildingGrbContext.SaveChangesAsync();

            //act
            await jobProcessor.StartAsync(CancellationToken.None);
            await jobProcessor.ExecuteTask!;

            //assert
            jobRecordsProcessor.Verify(x =>
                x.Process(It.Is<Guid>(y => y == firstJob.Id), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            jobRecordsProcessor.Verify(x =>
                x.Process(It.Is<Guid>(y => y == secondJob.Id), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
            jobRecordExecutionSequence.First().Should().Be(firstJob.Id);
            jobRecordExecutionSequence.Last().Should().Be(secondJob.Id);
            firstJob.Status.Should().Be(JobStatus.Completed);
            firstJob.LastChanged.Should().BeAfter(firstJob.Created);
            secondJob.Status.Should().Be(JobStatus.Completed);
            secondJob.LastChanged.Should().BeAfter(firstJob.Created);
            jobRecordsMonitor.Verify(x =>
                x.Monitor(It.Is<Guid>(y => y == firstJob.Id), It.IsAny<CancellationToken>()), Times.Once);
            jobRecordsMonitor.Verify(x =>
                x.Monitor(It.Is<Guid>(y => y == secondJob.Id), It.IsAny<CancellationToken>()), Times.Once);
            mockJobResultsUploader.Verify(x => x.UploadJobResults(firstJob.Id, It.IsAny<CancellationToken>()));
            mockJobRecordsArchiver.Verify(x => x.Archive(firstJob.Id, It.IsAny<CancellationToken>()));
            mockJobResultsUploader.Verify(x => x.UploadJobResults(secondJob.Id, It.IsAny<CancellationToken>()));
            mockJobRecordsArchiver.Verify(x => x.Archive(secondJob.Id, It.IsAny<CancellationToken>()));
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Exactly(2));
            hostApplicationLifetime.Verify(x => x.StopApplication(), Times.Once);

        }

        private JobRecord CreateJobRecord(Guid jobId, int id)
        {
            return new JobRecord
            {
                JobId = jobId,
                Status = JobRecordStatus.Created,
                EventType = GrbEventType.DefineBuilding,
                Geometry = (Polygon)GeometryHelper.ValidPolygon,
                GrbObject = GrbObject.ArtWork,
                GrbObjectType = GrbObjectType.MainBuilding,
                GrId = 1,
                Id = id,
                Idn = 3
            };
        }
    }
}
