﻿// <auto-generated />
using System;
using Grb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;

#nullable disable

namespace Grb.Migrations
{
    [DbContext(typeof(BuildingGrbContext))]
    partial class BuildingGrbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Grb.Job", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("ForceProcessing")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("LastChanged")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<Guid?>("TicketId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Status");

                    b.ToTable("Jobs", "BuildingRegistryGrb");
                });

            modelBuilder.Entity("Grb.JobRecord", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<int?>("BuildingPersistentLocalId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("EndDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("ErrorCode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EventType")
                        .HasColumnType("int");

                    b.Property<Polygon>("Geometry")
                        .IsRequired()
                        .HasColumnType("sys.geometry");

                    b.Property<int>("GrId")
                        .HasColumnType("int");

                    b.Property<int>("GrbObject")
                        .HasColumnType("int");

                    b.Property<int>("GrbObjectType")
                        .HasColumnType("int");

                    b.Property<long>("Idn")
                        .HasColumnType("bigint");

                    b.Property<int>("IdnVersion")
                        .HasColumnType("int");

                    b.Property<Guid>("JobId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal?>("Overlap")
                        .HasPrecision(8, 5)
                        .HasColumnType("decimal(8,5)");

                    b.Property<int>("RecordNumber")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<Guid?>("TicketId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("VersionDate")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.HasIndex("JobId");

                    b.HasIndex("GrbObject", "Idn", "IdnVersion", "EventType", "GrId")
                        .HasFilter("[EventType] = 1 AND [GrId] = -9");

                    b.ToTable("JobRecords", "BuildingRegistryGrb");
                });
#pragma warning restore 612, 618
        }
    }
}
