namespace Grb
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    /// <summary>
    /// A new building (DefineBuilding with the default GRID) that may be uploaded again even though an earlier job already contained it.
    /// </summary>
    public sealed class DuplicateNewBuildingExemption
    {
        public long Id { get; set; }
        public long Idn { get; set; }
        public int IdnVersion { get; set; }
        public GrbObject GrbObject { get; set; }
        public DateTimeOffset Created { get; set; }

        private DuplicateNewBuildingExemption() { }

        public DuplicateNewBuildingExemption(long idn, int idnVersion, GrbObject grbObject, DateTimeOffset created)
        {
            Idn = idn;
            IdnVersion = idnVersion;
            GrbObject = grbObject;
            Created = created;
        }
    }

    public sealed class DuplicateNewBuildingExemptionConfiguration : IEntityTypeConfiguration<DuplicateNewBuildingExemption>
    {
        public const string TableName = "DuplicateNewBuildingExemptions";

        public void Configure(EntityTypeBuilder<DuplicateNewBuildingExemption> builder)
        {
            builder.ToTable(TableName, BuildingGrbContext.Schema)
                .HasKey(x => x.Id)
                .IsClustered();

            builder.Property(x => x.Id)
                .UseIdentityColumn();

            builder.Property(x => x.Idn);
            builder.Property(x => x.IdnVersion);
            builder.Property(x => x.GrbObject);
            builder.Property(x => x.Created);

            builder.HasIndex(x => new { x.Idn, x.IdnVersion, x.GrbObject })
                .IsUnique();
        }
    }
}
