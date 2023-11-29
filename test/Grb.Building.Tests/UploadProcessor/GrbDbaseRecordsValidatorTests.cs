namespace Grb.Building.Tests.UploadProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture;
    using FluentAssertions;
    using Processor.Upload.Zip;
    using Processor.Upload.Zip.Exceptions;
    using Processor.Upload.Zip.Validators;
    using Xunit;

    public class GrbDbaseRecordsValidatorTests
    {
        [Fact]
        public void WhenRecordsNull_ThrowException()
        {
            var func = () => new GrbDbaseRecordsValidator().Validate("dummy", null);
            func.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WhenRecordsEmpty_ThrowException()
        {
            var func = () =>
                new GrbDbaseRecordsValidator().Validate("dummy", new List<GrbDbaseRecord>().GetEnumerator());
            func.Should().Throw<NoDbaseRecordsException>();
        }

        private GrbDbaseRecord GetCreateValidRecord(){
            var record = new GrbDbaseRecord();
            record.IDN.Value = 1;
            record.IDNV.Value = 1;
            record.GVDV.Value = DateTime.Now.ToString("yyyy-MM-dd");
            record.GVDE.Value = DateTime.Now.ToString("yyyy-MM-dd");
            record.EventType.Value = (int) GrbEventType.Unknown;
            record.GRBOBJECT.Value = (int) GrbObject.Unknown;
            record.GRID.Value = "https://data.vlaanderen.be/id/gebouw/11111111";
            record.TPC.Value = (int) GrbObjectType.MainBuilding;
            return record;
        }

        [Fact]
        public void WhenRecordEventTypeIsNull_ThenValidationError()
        {
            var recordWithEventTypeNull = GetCreateValidRecord();
            recordWithEventTypeNull.EventType.Reset();

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithEventTypeNull
                }.GetEnumerator());

            var v = validationResult.FirstOrDefault();
            v.Should().NotBeNull();
            v.Value.Should().Contain(ValidationErrorType.UnknownEventType);
        }

        [Fact]
        public void WhenRecordEventTypeIsInvalid_ThenValidationError()
        {
            var recordWithEventTypeNull = GetCreateValidRecord();
            recordWithEventTypeNull.EventType.Value = 11;

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithEventTypeNull
                }.GetEnumerator());

            var v = validationResult.FirstOrDefault();
            v.Should().NotBeNull();
            v.Value.Should().Contain(ValidationErrorType.UnknownEventType);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("1")]
        [InlineData("data.vlaanderen.be/id/gebouw/123")]
        [InlineData("https://data.vlaanderen.be/id/gebouw/")]
        [InlineData("https://data.vlaanderen.be/id/gebouw/-1")]
        [InlineData("https://data.vlaanderen.be/id/gebouw/-9")]
        public void WhenRecordGridIsInvalid_ThenValidationError(string grid)
        {
            var recordWithInvalidGrId = GetCreateValidRecord();
            recordWithInvalidGrId.GRID.Value = grid;

            Assert.Throws<InvalidGrIdException>(() => new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithInvalidGrId
                }.GetEnumerator()));
        }

        [Theory]
        [InlineData("-9")]
        [InlineData("https://data.vlaanderen.be/id/gebouw/123")]
        [InlineData("https://data.vlaanderen.be/id/gebouw/123/456")]
        public void WhenRecordGridIsValid_ThenNoValidationError(string grid)
        {
            var recordWithValidGrId = GetCreateValidRecord();
            recordWithValidGrId.GRID.Value = grid;

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithValidGrId
                }.GetEnumerator());

            validationResult.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData("-123456789")]
        [InlineData("19-10-2023")]
        [InlineData("10-19-2023")]
        [InlineData("2023-19-10")]
        public void WhenRecordVersionDateIsInvalid_ThenValidationError(string versionDate)
        {
            var recordWithInvalidVersionDate = GetCreateValidRecord();
            recordWithInvalidVersionDate.GVDV.Value = versionDate;

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithInvalidVersionDate
                }.GetEnumerator());

            var v = validationResult.FirstOrDefault();
            v.Should().NotBeNull();
            v.Value.Should().Contain(ValidationErrorType.InvalidVersionDate);
        }

        [Fact]
        public void WhenRecordVersionDateIsValid_ThenNoValidationError()
        {
            var recordWithValidVersionDate = GetCreateValidRecord();
            recordWithValidVersionDate.GVDV.Value = new Fixture().Create<DateTime>().ToString("yyyy-MM-dd");

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithValidVersionDate
                }.GetEnumerator());

            validationResult.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData("-123456789")]
        [InlineData("19-10-2023")]
        [InlineData("10-19-2023")]
        [InlineData("2023-19-10")]
        public void WhenRecordEndDateIsInvalid_ThenValidationError(string endDate)
        {
            var recordWithInvalidEndDate = GetCreateValidRecord();
            recordWithInvalidEndDate.GVDE.Value = endDate;

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithInvalidEndDate
                }.GetEnumerator());

            var v = validationResult.FirstOrDefault();
            v.Should().NotBeNull();
            v.Value.Should().Contain(ValidationErrorType.InvalidEndDate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("2023-10-19")]
        public void WhenRecordEndDateIsValid_ThenNoValidationError(string? endDate)
        {
            var recordWithValidEndDate = GetCreateValidRecord();
            recordWithValidEndDate.GVDE.Value = endDate;

            var validationResult =  new GrbDbaseRecordsValidator().Validate(
                "dummy",
                new List<GrbDbaseRecord>
                {
                    recordWithValidEndDate
                }.GetEnumerator());

            validationResult.Should().BeNullOrEmpty();
        }
    }
}
