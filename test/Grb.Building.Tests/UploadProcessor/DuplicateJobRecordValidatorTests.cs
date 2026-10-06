namespace Grb.Building.Tests.UploadProcessor
{
    using System;
    using FluentAssertions;
    using NetTopologySuite.Geometries;
    using Processor.Upload.Zip.Validators;
    using Xunit;

    public sealed class DuplicateJobRecordValidatorTests
    {
        private const int Idn = 47280;
        private const int IdnVersion = 1;
        private const GrbObject Object = GrbObject.CompositeBuilding;

        private readonly FakeBuildingGrbContext _context;
        private readonly DuplicateJobRecordValidator _sut;

        public DuplicateJobRecordValidatorTests()
        {
            _context = new FakeBuildingGrbContextFactory().CreateDbContext();
            _sut = new DuplicateJobRecordValidator(_context);

            _context.JobRecords.Add(new JobRecord
            {
                JobId = Guid.NewGuid(),
                Idn = Idn,
                IdnVersion = IdnVersion,
                GrbObject = Object,
                EventType = GrbEventType.DefineBuilding,
                GrId = JobRecord.DefaultNewBuildingGrId,
                Geometry = (Polygon)GeometryHelper.ValidPolygon
            });
            _context.SaveChanges();
        }

        [Fact]
        public void WithDuplicateAndNoExemption_ThenHasDuplicate()
        {
            _sut.HasDuplicateNewBuilding(Idn, IdnVersion, Object).Should().BeTrue();
        }

        [Fact]
        public void WithDuplicateAndExemption_ThenHasNoDuplicate()
        {
            _context.DuplicateNewBuildingExemptions.Add(
                new DuplicateNewBuildingExemption(Idn, IdnVersion, Object, DateTimeOffset.Now));
            _context.SaveChanges();

            _sut.HasDuplicateNewBuilding(Idn, IdnVersion, Object).Should().BeFalse();
        }

        [Theory]
        [InlineData(Idn + 1, IdnVersion, Object)]
        [InlineData(Idn, IdnVersion + 1, Object)]
        [InlineData(Idn, IdnVersion, GrbObject.BuildingAtGroundLevel)]
        public void WithDuplicateAndExemptionForAnotherRecord_ThenHasDuplicate(int idn, int idnVersion, GrbObject grbObject)
        {
            _context.DuplicateNewBuildingExemptions.Add(
                new DuplicateNewBuildingExemption(idn, idnVersion, grbObject, DateTimeOffset.Now));
            _context.SaveChanges();

            _sut.HasDuplicateNewBuilding(Idn, IdnVersion, Object).Should().BeTrue();
        }
    }
}
