namespace Grb.Building.Tests.UploadProcessor
{
    using System.Collections.Generic;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.GrAr.CrsTransform;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
    using FluentAssertions;
    using Processor.Upload.Zip.Exceptions;
    using Processor.Upload.Zip.Validators;
    using Xunit;

    public class GrbShapeRecordsValidatorTests
    {
        [Fact]
        public void WithPoint_ThenValidationErrorTypeIsGeometryIsNotPolygon()
        {
            var validator = new GrbShapeRecordsValidator();
            var records = new List<ShapeRecord>()
            {
                new ShapeRecord(new ShapeRecordHeader(new RecordNumber(1), new WordLength(10)),
                    new PointShapeContent(new Point(5.1, 2.2)))
            };

            // Act
            var result = validator.Validate("dummy", records.GetEnumerator());

            // Assert
            var validationErrorTypes = result[new RecordNumber(1)];
            validationErrorTypes.Should().NotBeNullOrEmpty();
            validationErrorTypes.First().Should().Be(ValidationErrorType.GeometryIsNotPolygon);
        }

        [Fact]
        public void WithValidPolygon_ThenNoValidationErrorTypes()
            => Validate(Lambert72Polygon).Should().BeNullOrEmpty();

        /// <summary>
        /// A GRB delivery declares no reference system - no <c>.prj</c>, and the uploaded filename does not
        /// survive the pre-signed upload - so it is decided from the coordinates. See ADR 0003.
        /// </summary>
        [Fact]
        public void WithAllRecordsInLambert72_ThenNoValidationErrorTypes()
            => Validate(Lambert72Polygon, Lambert72Polygon).Should().BeEmpty();

        [Fact]
        public void WithAllRecordsInLambert2008_ThenNoValidationErrorTypes()
            => Validate(Lambert2008Polygon, Lambert2008Polygon).Should().BeEmpty();

        /// <summary>
        /// In neither envelope, so there is no saying what the coordinates mean - refused rather than
        /// guessed at.
        /// </summary>
        [Fact]
        public void WithGeometryOutsideFlanders_ThenValidationErrorTypeIsGeometryOutsideFlanders()
        {
            var result = Validate(Lambert72Polygon, OutsideFlandersPolygon);

            result[new RecordNumber(2)].Should().Contain(ValidationErrorType.GeometryOutsideFlanders);
            result.Should().NotContainKey(new RecordNumber(1));
        }

        /// <summary>
        /// GRB converts as a whole, so a mix means something went wrong upstream. The minority records are
        /// reported, so the operator is pointed at the few that disagree rather than at the whole file.
        /// </summary>
        [Fact]
        public void WithMixedReferenceSystems_ThenTheMinorityRecordsAreReported()
        {
            var result = Validate(Lambert72Polygon, Lambert72Polygon, Lambert2008Polygon);

            result[new RecordNumber(3)].Should().Contain(ValidationErrorType.MixedReferenceSystems);
            result.Should().NotContainKey(new RecordNumber(1));
            result.Should().NotContainKey(new RecordNumber(2));
        }

        [Fact]
        public void WithAnEvenMix_ThenOneSideIsStillReported()
            => Validate(Lambert72Polygon, Lambert2008Polygon)
                .SelectMany(x => x.Value)
                .Should().Contain(ValidationErrorType.MixedReferenceSystems);

        /// <summary>The premise the classification rests on: a polygon falls in at most one envelope.</summary>
        [Fact]
        public void TheTwoFlandersEnvelopesDoNotOverlap()
        {
            Lambert72Polygon.IsInsideFlandersUsingLambert72().Should().BeTrue();
            Lambert72Polygon.IsInsideFlandersUsingLambert08().Should().BeFalse();

            Lambert2008Polygon.IsInsideFlandersUsingLambert08().Should().BeTrue();
            Lambert2008Polygon.IsInsideFlandersUsingLambert72().Should().BeFalse();
        }

        private static NetTopologySuite.Geometries.Polygon Lambert72Polygon
            => (NetTopologySuite.Geometries.Polygon)GeometryHelper.ValidPolygon;

        private static NetTopologySuite.Geometries.Polygon Lambert2008Polygon
            => Lambert72Polygon.TransformFromLambert72To08();

        /// <summary>Somewhere in the North Sea: in neither envelope.</summary>
        private static NetTopologySuite.Geometries.Polygon OutsideFlandersPolygon
            => (NetTopologySuite.Geometries.Polygon)new NetTopologySuite.IO.WKTReader()
                .Read("POLYGON ((10 10, 20 10, 20 20, 10 20, 10 10))");

        private static IDictionary<RecordNumber, List<ValidationErrorType>> Validate(
            params NetTopologySuite.Geometries.Polygon[] polygons)
        {
            var records = polygons
                .Select((polygon, index) => new PolygonShapeContent(GeometryTranslator
                        .FromGeometryPolygon(polygon))
                    .RecordAs(new RecordNumber(index + 1)))
                .ToList();

            return new GrbShapeRecordsValidator().Validate("dummy", records.GetEnumerator());
        }

        [Fact]
        public void WithNoShapeRecords_ThenException()
        {
            // Arrange
            var validator = new GrbShapeRecordsValidator();
            var records = new List<ShapeRecord>();

            // Act
            var func = () => validator.Validate("dummy", records.GetEnumerator());

            // Assert
            func.Should().Throw<NoShapeRecordsException>("dummy");
        }
    }
}
