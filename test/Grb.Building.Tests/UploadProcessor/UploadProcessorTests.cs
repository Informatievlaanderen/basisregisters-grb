namespace Grb.Building.Tests.UploadProcessor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Amazon.ECS;
    using Amazon.ECS.Model;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using FluentAssertions;
    using Grb.Building.Processor.Upload;
    using Grb.Building.Processor.Upload.Zip;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Notifications;
    using TicketingService.Abstractions;
    using Xunit;
    using Task = System.Threading.Tasks.Task;

    public class UploadProcessorTests
    {
        private readonly FakeBuildingGrbContext _buildingGrbContext;

        public UploadProcessorTests()
        {
            _buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();
        }

        [Fact]
        public async Task FlowTest()
        {
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(
                    blobName,
                    null,
                    ContentType.Parse("X-multipart/abc"),
                    _ => Task.FromResult((Stream)new FileStream(
                        $"{AppContext.BaseDirectory}/UploadProcessor/gebouw_ALL.zip", FileMode.Open,
                        FileAccess.Read))));

            mockAmazonClient
                .Setup(x => x.RunTaskAsync(It.IsAny<RunTaskRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RunTaskResponse { HttpStatusCode = HttpStatusCode.OK });

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions { Subnets = "subnet-1234", SecurityGroups = "sg-5678" }));

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockTicketing.Verify(x => x.Pending(ticketId, It.IsAny<CancellationToken>()), Times.Once);

            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().HaveCount(10);
            _buildingGrbContext.Jobs.First().Status.Should().Be(JobStatus.Prepared);

            mockAmazonClient.Verify(x => x.RunTaskAsync(It.IsAny<RunTaskRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);
            mockIHostApplicationLifeTime.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task WhenBlobNotFound()
        {
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(CancellationToken.None);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            mockTicketing.Verify(x => x.Pending(ticketId, It.IsAny<CancellationToken>()), Times.Never);
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);
        }

        [Fact]
        public async Task WhenBlobObjectIsNull_ThenLogErrorAndContinue()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BlobObject?)null);

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();
            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);
        }

        [Fact]
        public async Task WhenZipArchiveValidationProblems_ThenTicketError()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var zipFileStream = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_dbf_missing.zip",
                FileMode.Open, FileAccess.Read);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(blobName, null, ContentType.Parse("X-multipart/abc"),
                    _ => Task.FromResult((Stream)zipFileStream)));

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            mockTicketing.Verify(x => x.Error(
                ticketId,
                It.Is<TicketError>(x =>
                    x.Errors.First().ErrorCode == "RequiredFileMissing"
                    && x.Errors.First().ErrorMessage == $"Er ontbreekt een verplichte file in de zip: GEBOUW_ALL.DBF."),
                It.IsAny<CancellationToken>()), Times.Once);

            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();

            _buildingGrbContext.Jobs.First().Status.Should().Be(JobStatus.Error);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);
        }

        [Fact]
        public async Task WhenDbaseRecordHasMissingShapeRecord_ThenTicketError()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var zipFileStream = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_shape_missing.zip",
                FileMode.Open, FileAccess.Read);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(blobName, null, ContentType.Parse("X-multipart/abc"),
                    _ => Task.FromResult((Stream)zipFileStream)));

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            mockTicketing.Verify(x => x.Error(
                ticketId,
                It.Is<TicketError>(x =>
                    x.ErrorCode == "OntbrekendeGeometriePolygoonShapeFile"
                    && x.ErrorMessage ==
                    $"In de meegegeven shape file hebben niet alle gebouwen een geometriePolygoon. Record nummers: 2"),
                It.IsAny<CancellationToken>()), Times.Once);

            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();

            _buildingGrbContext.Jobs.First().Status.Should().Be(JobStatus.Error);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);
        }

        [Fact]
        public async Task WhenDbaseRecordHasInvalidGrId_ThenTicketError()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var zipFileStream = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_grid_invalid.zip",
                FileMode.Open, FileAccess.Read);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(blobName, null, ContentType.Parse("X-multipart/abc"),
                    _ => Task.FromResult((Stream)zipFileStream)));

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            mockTicketing.Verify(x => x.Error(
                ticketId,
                It.Is<TicketError>(x =>
                    x.Errors.Any(y =>
                        y.ErrorCode == "GebouwIdOngeldig" &&
                        y.ErrorMessage == "De meegegeven waarde in de kolom 'GRID' is ongeldig. Record nummer: 1, GRID: invalid puri")),
                It.IsAny<CancellationToken>()), Times.Once);

            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();

            _buildingGrbContext.Jobs.First().Status.Should().Be(JobStatus.Error);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);
        }

        [Fact]
        public async Task WhenDbaseRecordHasInvalidVersionDate_ThenTicketError()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var zipFileStream = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_versiondate_invalid.zip",
                FileMode.Open, FileAccess.Read);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(blobName, null, ContentType.Parse("X-multipart/abc"),
                    _ => Task.FromResult((Stream)zipFileStream)));

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            mockTicketing.Verify(x => x.Error(
                ticketId,
                It.Is<TicketError>(ticketError =>
                    ticketError.Errors.Any(y =>
                        y.ErrorCode == "InvalidVersionDate" &&
                        y.ErrorMessage == "Record number(s):1,2")),
                It.IsAny<CancellationToken>()), Times.Once);

            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();

            _buildingGrbContext.Jobs.First().Status.Should().Be(JobStatus.Error);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);
        }

        [Fact]
        public async Task WhenDbaseRecordHasInvalidEndDate_ThenTicketError()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var zipFileStream = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_enddate_invalid.zip",
                FileMode.Open, FileAccess.Read);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(blobName, null, ContentType.Parse("X-multipart/abc"),
                    _ => Task.FromResult((Stream)zipFileStream)));

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            mockTicketing.Verify(x => x.Error(
                ticketId,
                It.Is<TicketError>(ticketError =>
                    ticketError.Errors.Any(y =>
                        y.ErrorCode == "InvalidEndDate" &&
                        y.ErrorMessage == "Record number(s):1,2")),
                It.IsAny<CancellationToken>()), Times.Once);

            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();

            _buildingGrbContext.Jobs.First().Status.Should().Be(JobStatus.Error);

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Once);

        }

        [Fact]
        public async Task WhenBlobNotFoundException_ThenLogAndContinue()
        {
            var ct = CancellationToken.None;
            var mockTicketing = new Mock<ITicketing>();
            var mockIBlobClient = new Mock<IBlobClient>();
            var mockAmazonClient = new Mock<IAmazonECS>();
            var mockIHostApplicationLifeTime = new Mock<IHostApplicationLifetime>();

            var ticketId = Guid.NewGuid();
            var job = new Job(DateTimeOffset.Now, JobStatus.Created, ticketId);

            _buildingGrbContext.Jobs.Add(job);
            await _buildingGrbContext.SaveChangesAsync(ct);

            var blobName = new BlobName(job.ReceivedBlobName);

            mockIBlobClient
                .Setup(x => x.BlobExistsAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            mockIBlobClient
                .Setup(x => x.GetBlobAsync(blobName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BlobObject(blobName, null, ContentType.Parse("X-multipart/abc"),
                    _ => throw new BlobNotFoundException(blobName)));

            var notificationsService = new Mock<INotificationService>();

            var sut = new UploadProcessor(
                _buildingGrbContext,
                mockTicketing.Object,
                mockIBlobClient.Object,
                mockAmazonClient.Object,
                new NullLoggerFactory(),
                mockIHostApplicationLifeTime.Object,
                notificationsService.Object,
                Options.Create(new EcsTaskOptions()));

            // Act
            await sut.StartAsync(ct);

            // Assert
            var jobRecords = _buildingGrbContext.JobRecords.Where(x => x.JobId == job.Id);
            jobRecords.Should().BeEmpty();

            notificationsService.Verify(x => x.PublishToTopicAsync(It.IsAny<NotificationMessage>()), Times.Never);
        }
    }
}
