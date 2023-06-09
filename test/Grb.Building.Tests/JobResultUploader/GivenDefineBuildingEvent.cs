﻿namespace Grb.Building.Tests.JobResultUploader
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using FluentAssertions;
    using Moq;
    using NetTopologySuite.Geometries;
    using Processor.Job;
    using Xunit;

    public class GivenDefineBuildingEvent
    {
        public GivenDefineBuildingEvent()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public async Task ThenJobResultsZipShouldHaveSingleEntry()
        {
            // Arrange
            var buildingGrbContext = new FakeBuildingGrbContextFactory().CreateDbContext();

            var job = new Job(DateTimeOffset.Now, JobStatus.Completed, Guid.NewGuid());
            buildingGrbContext.Jobs.Add(job);
            buildingGrbContext.JobRecords.AddRange(
                new JobRecord {JobId = job.Id, GrId = 123, Status = JobRecordStatus.Completed, EventType = GrbEventType.DefineBuilding, Geometry = Polygon.Empty},
                new JobRecord {JobId = job.Id, GrId = 456, Status = JobRecordStatus.Completed, EventType = GrbEventType.DefineBuilding, Geometry = Polygon.Empty});

            await buildingGrbContext.SaveChangesAsync();

            await using Stream resultStream = new MemoryStream();

            var blobClient = new Mock<IBlobClient>();
            blobClient.Setup(x => x.CreateBlobAsync(
                It.IsAny<BlobName>(),
                It.IsAny<Metadata>(),
                It.IsAny<ContentType>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()
            )).Callback<BlobName, Metadata, ContentType, Stream, CancellationToken>((_, _, _, stream, _) =>
            {
                stream.CopyTo(resultStream);
            });

            // Act
            var sut = new JobResultUploader(buildingGrbContext, blobClient.Object);
            await sut.UploadJobResults(job.Id, CancellationToken.None);

            // Assert
            resultStream.Seek(0, SeekOrigin.Begin);
            using var zipArchive = new ZipArchive(resultStream);
            zipArchive.Entries.Should().ContainSingle();

            // await using var fileStream = File.Create($"{AppContext.BaseDirectory}/Grb/UploadProcessor/jobresults.zip");
            // await resultStream.CopyToAsync(fileStream);
        }
    }
}
