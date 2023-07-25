namespace Grb.Building.Processor.Job
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api.Extract;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using Be.Vlaanderen.Basisregisters.GrAr.Extracts;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public interface IJobResultUploader
    {
        Task UploadJobResults(Guid jobId, CancellationToken ct);
    }

    public class JobResultUploader : IJobResultUploader
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly IBlobClient _blobClient;
        private readonly string _buildingReadUri;

        public JobResultUploader(BuildingGrbContext buildingGrbContext, IBlobClient blobClient, string buildingReadUri)
        {
            _buildingGrbContext = buildingGrbContext;
            _blobClient = blobClient;
            _buildingReadUri = buildingReadUri;
        }

        public async Task UploadJobResults(Guid jobId, CancellationToken ct)
        {
            var jobResultsZipArchive = await CreateResultFile(jobId, ct);

            using var archiveStream = new MemoryStream();
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                await using (var entryStream = archive.CreateEntry($"{jobId:D}.dbf").Open())
                    jobResultsZipArchive.WriteTo(entryStream, ct);
            }

            archiveStream.Seek(0, SeekOrigin.Begin);

            var metadata = Metadata.None.Add(
                new KeyValuePair<MetadataKey, string>(new MetadataKey("filename"), jobResultsZipArchive.Name));

            await _blobClient.CreateBlobAsync(
                new BlobName(Grb.Job.JobResultsBlobName(jobId)),
                metadata,
                ContentType.Parse("application/zip"),
                archiveStream,
                ct);
        }

        private async Task<ExtractFile> CreateResultFile(Guid jobId, CancellationToken ct)
        {
            var jobResults = await GetJobResults(jobId, ct);

            byte[] TransformRecord(JobResult jobResult)
            {
                var item = new JobResultDbaseRecord
                {
                    Idn = {Value = jobResult.GrbIdn},
                    GrbObject = {Value = (int)jobResult.GrbObject},
                    GrId =  {Value = $"{_buildingReadUri.TrimEnd('/')}/{jobResult.BuildingPersistentLocalId}"}
                };

                return item.ToBytes(DbfFileWriter<JobResultDbaseRecord>.Encoding);
            }

            return ExtractBuilder.CreateDbfFile<JobResult, JobResultDbaseRecord>(
                "IdnGrResults",
                new JobResultDbaseSchema(),
                jobResults,
                jobResults.Count,
                TransformRecord);
        }

        private async Task<IEnumerable<JobResult>> GetJobResults(Guid jobId, CancellationToken ct)
        {
            var jobRecords = await _buildingGrbContext.JobRecords
                .AsNoTracking()
                .Where(x =>
                    x.JobId == jobId
                    && (x.Status == JobRecordStatus.Completed || x.Status == JobRecordStatus.Warning)
                    && x.EventType == GrbEventType.DefineBuilding)
                .ToListAsync(ct);

            return jobRecords
                .Select(jobRecord => new JobResult
                {
                    JobId = jobRecord.JobId,
                    BuildingPersistentLocalId = jobRecord.BuildingPersistentLocalId ?? jobRecord.GrId,
                    GrbIdn = (int)jobRecord.Idn,
                    IsBuildingCreated = jobRecord.EventType == GrbEventType.DefineBuilding,
                    GrbObject = jobRecord.GrbObject
                })
                .ToList();
        }
    }
}
