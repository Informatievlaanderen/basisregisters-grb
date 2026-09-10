namespace Grb.Building.Tests
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.NetTopology;
    using FluentAssertions;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Utilities;
    using Processor.Job;
    using Xunit;

    /// <summary>
    /// The srsName on the outgoing GML is the only thing telling building-registry what the coordinates
    /// mean: it converts from the system the srsName declares to the one its event store holds. A fixed
    /// srsName is silently wrong the moment GRB delivers anything else. See ADR 0003.
    /// </summary>
    public class GrbDataMapperTests
    {
        [Fact]
        public void GivenLambert72Geometry_ThenSrsNameIsLambert72()
            => SrsNameOf(GeometryHelper.ValidPolygon, SystemReferenceId.SridLambert72)
                .Should().Be("https://www.opengis.net/def/crs/EPSG/0/31370");

        [Fact]
        public void GivenLambert2008Geometry_ThenSrsNameIsLambert2008()
            => SrsNameOf(GeometryHelper.ValidPolygon, SystemReferenceId.SridLambert2008)
                .Should().Be("https://www.opengis.net/def/crs/EPSG/0/3812");

        /// <summary>
        /// Every job record written before the upload started stamping an SRID holds one of these, and they
        /// are Lambert 72.
        /// </summary>
        [Fact]
        public void GivenGeometryWithoutSrid_ThenSrsNameIsLambert72()
            => SrsNameOf(GeometryHelper.ValidPolygon, 0)
                .Should().Be("https://www.opengis.net/def/crs/EPSG/0/31370");

        /// <summary>The reference system is a label here: the mapper re-expresses nothing.</summary>
        [Theory]
        [InlineData(SystemReferenceId.SridLambert72)]
        [InlineData(SystemReferenceId.SridLambert2008)]
        [InlineData(0)]
        public void ThenTheCoordinatesAreUnchanged(int srid)
        {
            var polygon = (Polygon)GeometryHelper.ValidPolygon;
            var gml = GrbDataMapper.Map(JobRecordWith(polygon, srid)).GeometriePolygoon;

            var posList = XDocument.Parse(gml!)
                .Descendants(XName.Get("posList", "http://www.opengis.net/gml/3.2"))
                .Single()
                .Value;

            foreach (var coordinate in polygon.ExteriorRing.Coordinates)
            {
                posList.Should().Contain(coordinate.X.ToString(Global.GetNfi()));
                posList.Should().Contain(coordinate.Y.ToString(Global.GetNfi()));
            }
        }

        private static string SrsNameOf(Geometry geometry, int srid)
        {
            var gml = GrbDataMapper.Map(JobRecordWith((Polygon)geometry, srid)).GeometriePolygoon;

            return XDocument.Parse(gml!).Root!.Attribute("srsName")!.Value;
        }

        private static JobRecord JobRecordWith(Polygon polygon, int srid)
        {
            var geometry = (Polygon)polygon.Copy();
            geometry.SRID = srid;

            return new JobRecord
            {
                JobId = Guid.NewGuid(),
                Status = JobRecordStatus.Created,
                EventType = GrbEventType.ChangeBuildingMeasurement,
                Geometry = geometry,
                GrbObject = GrbObject.ArtWork,
                GrbObjectType = GrbObjectType.MainBuilding,
                GrId = 1,
                Id = 2,
                Idn = 3,
                VersionDate = DateTimeOffset.Now
            };
        }
    }
}
