namespace Grb.Building.Tests.UploadProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.NetTopology;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
    using FluentAssertions;
    using Processor.Upload.Zip;
    using Processor.Upload.Zip.Translators;
    using Processor.Upload.Zip.Validators;
    using Xunit;

    /// <summary>
    /// Drives the real Lambert 2008 delivery GRB produced (<c>gebouw_ALL_LB08.zip</c>) through the two
    /// places the reference system matters: the validator that classifies it, and the translator that
    /// records the answer on the geometry. See ADR 0003.
    /// </summary>
    /// <remarks>
    /// Only the shape entry is read. That archive's dbase entry was exported with a different schema than
    /// <see cref="GrbDbaseSchema"/> requires - GRBIDN 10 rather than 9, GVDV/GVDE/GRID 254 rather than
    /// 10/10/128 - so it is refused by <c>ZipArchiveDbaseEntryValidator</c> before any of this is reached,
    /// for reasons that have nothing to do with the coordinates. The shape entry itself is well-formed and
    /// carries genuine Lambert 2008 coordinates, which is what is under test here.
    /// </remarks>
    public class Lambert2008ArchiveTests
    {
        [Fact]
        public void ThenTheArchiveIsAcceptedAsLambert2008()
            => Validate().Should().BeEmpty();

        [Fact]
        public void ThenEveryTranslatedGeometryCarriesLambert2008()
        {
            var records = ReadShapeRecords();
            var jobRecords = records.ToDictionary(
                x => x.Header.RecordNumber,
                _ => new JobRecord { VersionDate = DateTimeOffset.Now });

            new GrbShapeRecordsTranslator().Translate(records.GetEnumerator(), jobRecords);

            jobRecords.Values.Should().NotBeEmpty();
            jobRecords.Values.Should().OnlyContain(x => x.Geometry.SRID == SystemReferenceId.SridLambert2008);
        }

        /// <summary>The Lambert 72 delivery still classifies as Lambert 72 - this changes nothing for it.</summary>
        [Fact]
        public void ThenTheLambert72ArchiveIsUnaffected()
            => ReadShapeRecords("gebouw_ALL.zip")
                .Select(x => GeometryTranslator.ToGeometryPolygon((x.Content as PolygonShapeContent)!.Shape))
                .Should().OnlyContain(x => GrbGeometryReferenceSystem.Of(x) == SystemReferenceId.SridLambert72);

        private static IDictionary<RecordNumber, List<ValidationErrorType>> Validate()
        {
            using var stream = OpenShapeEntry("gebouw_ALL_LB08.zip", out var archive, out var entryStream);
            using var _ = archive;
            using var __ = entryStream;
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            using var records = ShapeFileHeader.Read(reader).CreateShapeRecordEnumerator(reader);

            return new GrbShapeRecordsValidator().Validate(ZipArchiveConstants.SHP_FILENAME, records);
        }

        private static List<ShapeRecord> ReadShapeRecords(string fileName = "gebouw_ALL_LB08.zip")
        {
            using var stream = OpenShapeEntry(fileName, out var archive, out var entryStream);
            using var _ = archive;
            using var __ = entryStream;
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            using var records = ShapeFileHeader.Read(reader).CreateShapeRecordEnumerator(reader);

            var result = new List<ShapeRecord>();
            while (records.MoveNext())
            {
                result.Add(records.Current);
            }

            return result;
        }

        private static Stream OpenShapeEntry(string fileName, out ZipArchive archive, out Stream fileStream)
        {
            fileStream = new FileStream(
                $"{AppContext.BaseDirectory}/UploadProcessor/{fileName}", FileMode.Open, FileAccess.Read);
            archive = new ZipArchive(fileStream, ZipArchiveMode.Read, false);

            return archive.GetEntry(ZipArchiveConstants.SHP_FILENAME)!.Open();
        }
    }
}
