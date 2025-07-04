namespace Grb
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using NetTopologySuite.Geometries;

    public sealed class JobRecord
    {
        public const int DefaultNewBuildingGrId = -9;

        public long Id { get; set; }
        public Guid JobId { get; set; }

        public int RecordNumber { get; set; }

        public long Idn { get; set; }
        public int IdnVersion { get; set; }
        public DateTimeOffset VersionDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        public GrbObject GrbObject { get; set; }
        public GrbObjectType GrbObjectType { get; set; }
        public GrbEventType EventType { get; set; }
        public int GrId { get; set; }
        public Polygon Geometry { get; set; } = null!;
        public decimal? Overlap { get; set; }

        public Guid? TicketId { get; set; }
        public JobRecordStatus Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int? BuildingPersistentLocalId { get; set; }

        public void ResolveError()
        {
            if (Status != JobRecordStatus.Error)
            {
                return;
            }

            // We're setting the status to resolved, but we are not deleting the ErrorMessage.
            Status = JobRecordStatus.ErrorResolved;
        }
    }

    public sealed class JobRecordConfiguration : IEntityTypeConfiguration<JobRecord>
    {
        public const string TableName = "JobRecords";
        public const string ArchiveTableName = "JobRecordsArchive";

        public void Configure(EntityTypeBuilder<JobRecord> builder)
        {
            builder.ToTable(TableName, BuildingGrbContext.Schema)
                .HasKey(x => x.Id)
                .IsClustered();

            builder.Property(x => x.Id)
                .UseIdentityColumn();

            builder.Property(x => x.JobId);

            builder.Property(x => x.RecordNumber);

            builder.Property(x => x.Idn);
            builder.Property(x => x.IdnVersion);
            builder.Property(x => x.VersionDate);
            builder.Property(x => x.EndDate);

            builder.Property(x => x.GrbObject);
            builder.Property(x => x.GrbObjectType);
            builder.Property(x => x.EventType);
            builder.Property(x => x.GrId);
            builder.Property(x => x.Geometry)
                .HasColumnType("sys.geometry");

            builder.Property(x => x.Overlap)
                .HasPrecision(8, 5);

            builder.Property(x => x.TicketId);
            builder.Property(x => x.Status);
            builder.Property(x => x.ErrorCode);
            builder.Property(x => x.ErrorMessage);
            builder.Property(x => x.BuildingPersistentLocalId);

            builder.HasIndex(x => x.JobId);

            builder.HasIndex(x => new { x.GrbObject, x.Idn, x.IdnVersion, x.EventType, x.GrId })
                .HasFilter("[EventType] = 1 AND [GrId] = -9");
        }
    }
}
