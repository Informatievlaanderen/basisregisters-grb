namespace Grb.Building.Tests.JobResultUploader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.Unicode;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Oslo.Extensions;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using FluentAssertions;
    using Moq;
    using Processor.Job;
    using Processor.Upload.Zip;
    using Processor.Upload.Zip.Translators;
    using Xunit;
    using Polygon = NetTopologySuite.Geometries.Polygon;

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
                new JobRecord
                {
                    JobId = job.Id, GrId = 123, Status = JobRecordStatus.Completed,
                    EventType = GrbEventType.DefineBuilding, Geometry = Polygon.Empty
                },
                new JobRecord
                {
                    JobId = job.Id, GrId = 456, Status = JobRecordStatus.Completed,
                    EventType = GrbEventType.DefineBuilding, Geometry = Polygon.Empty
                });

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
            var sut = new JobResultUploader(buildingGrbContext, blobClient.Object, "https://basisregisters/");
            await sut.UploadJobResults(job.Id, CancellationToken.None);

            // Assert
            resultStream.Seek(0, SeekOrigin.Begin);
            using var zipArchive = new ZipArchive(resultStream);
            zipArchive.Entries.Should().ContainSingle();
            var result = zipArchive.Entries.First();

            using var stream = result.Open();
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var header = DbaseFileHeader.Read(reader, new DbaseFileHeaderReadBehavior(true));

            using var enumerator = header.CreateDbaseRecordEnumerator<JobResultDbaseRecord>(reader);
            var records = Translate(enumerator);
            records.Count.Should().Be(2);
            records.First().GrId.Value.Should().BeEquivalentTo("https://basisregisters/123");
        }

        public List<JobResultDbaseRecord> Translate(IDbaseRecordEnumerator<JobResultDbaseRecord> records)
        {
            var jobRecords = new List<JobResultDbaseRecord>();

            while (records.MoveNext())
            {
                var record = records.Current;
                jobRecords.Add(record);
            }

            return jobRecords;
        }
    }
}
