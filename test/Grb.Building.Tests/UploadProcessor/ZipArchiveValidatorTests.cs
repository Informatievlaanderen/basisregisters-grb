namespace Grb.Building.Tests.UploadProcessor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using FluentAssertions;
    using Moq;
    using Processor.Upload;
    using Processor.Upload.Zip;
    using Processor.Upload.Zip.Core;
    using Processor.Upload.Zip.Exceptions;
    using Processor.Upload.Zip.Validators;
    using Xunit;

    [Collection(ZipArchiveCollectionFixture.COLLECTION)]
    public class ZipArchiveValidatorTests
    {
        private readonly ZipArchiveFixture _fixture;

        public ZipArchiveValidatorTests(ZipArchiveFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void WhenMissingFile_ThenValidationError()
        {
            using var zipFile = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_dbf_missing.zip", FileMode.Open, FileAccess.Read);
            using var archive = new ZipArchive(zipFile, ZipArchiveMode.Read, false);

            // Act
            var sut = new ZipArchiveValidator(new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    new ZipArchiveDbaseEntryValidator<GrbDbaseRecord>(
                        Encoding.UTF8,
                        new DbaseFileHeaderReadBehavior(true),
                        new GrbDbaseSchema(),
                        new GrbDbaseRecordsValidator(new DuplicateJobRecordValidator(new FakeBuildingGrbContextFactory().CreateDbContext())))
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            });
            var zipArchiveProblems = sut.Validate(archive);

            // Assert
            var fileProblem = zipArchiveProblems.FirstOrDefault(x => x is FileError error && error.Code == "RequiredFileMissing");
            fileProblem.Should().NotBeNull();
            fileProblem.Message.Should().Be($"Er ontbreekt een verplichte file in de zip: GEBOUW_ALL.DBF.");
        }

        [Fact]
        public void WhenShapeHeaderFormatException_ThenProblem()
        {
            var zipArchiveRecordEntryValidator = new Mock<IZipArchiveDbaseEntryValidator>();
            zipArchiveRecordEntryValidator
                .Setup(x => x.Validate(It.IsAny<ZipArchiveEntry>()))
                .Throws(new ShapeHeaderFormatException(ZipArchiveConstants.DBF_FILENAME, new Exception()));

            // Act
            var sut = new ZipArchiveValidator(new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    zipArchiveRecordEntryValidator.Object
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            });
            var zipArchiveProblems = sut.Validate(_fixture.ZipArchive);

            // Assert
            var fileProblem = zipArchiveProblems.FirstOrDefault(x => x is FileError error && error.Code == ZipArchiveConstants.DBF_FILENAME);
            fileProblem.Should().NotBeNull();
            fileProblem.Message.Should().Be("ShapeHeaderFormatException");
        }

        [Fact]
        public void WhenDbaseHeaderFormatException_ThenProblem()
        {
            var zipArchiveRecordEntryValidator = new Mock<IZipArchiveDbaseEntryValidator>();
            zipArchiveRecordEntryValidator
                .Setup(x => x.Validate(It.IsAny<ZipArchiveEntry>()))
                .Throws(new DbaseHeaderFormatException(ZipArchiveConstants.DBF_FILENAME, new Exception()));

            // Act
            var sut = new ZipArchiveValidator(new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    zipArchiveRecordEntryValidator.Object
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            });
            var zipArchiveProblems = sut.Validate(_fixture.ZipArchive);

            // Assert
            var fileProblem = zipArchiveProblems.FirstOrDefault(x => x is FileError error && error.Code == ZipArchiveConstants.DBF_FILENAME);
            fileProblem.Should().NotBeNull();
            fileProblem.Message.Should().Be("DbaseHeaderFormatException");
        }

        [Fact]
        public void WhenDbaseHeaderSchemaMismatchException_ThenProblem()
        {
            var zipArchiveRecordEntryValidator = new Mock<IZipArchiveDbaseEntryValidator>();
            zipArchiveRecordEntryValidator
                .Setup(x => x.Validate(It.IsAny<ZipArchiveEntry>()))
                .Throws(new DbaseHeaderSchemaMismatchException(ZipArchiveConstants.DBF_FILENAME));

            // Act
            var sut = new ZipArchiveValidator(new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    zipArchiveRecordEntryValidator.Object
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            });
            var zipArchiveProblems = sut.Validate(_fixture.ZipArchive);

            // Assert
            var fileProblem = zipArchiveProblems.FirstOrDefault(x => x is FileError error && error.Code == "DbaseHeaderSchemaMismatchException");
            fileProblem.Should().NotBeNull();
            fileProblem.Message.Should().Be("De kolomnamen komen niet overeen met de verwachte kolomstructuur.");
            fileProblem.Code.Should().Be("DbaseHeaderSchemaMismatchException");
        }

        [Fact]
        public void WhenNoDbaseRecordsException_ThenProblem()
        {
            var zipArchiveRecordEntryValidator = new Mock<IZipArchiveDbaseEntryValidator>();
            zipArchiveRecordEntryValidator
                .Setup(x => x.Validate(It.IsAny<ZipArchiveEntry>()))
                .Throws(new NoDbaseRecordsException(ZipArchiveConstants.DBF_FILENAME));

            // Act
            var sut = new ZipArchiveValidator(new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    zipArchiveRecordEntryValidator.Object
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            });
            var zipArchiveProblems = sut.Validate(_fixture.ZipArchive);

            // Assert
            var fileProblem = zipArchiveProblems.FirstOrDefault(x => x is FileError error && error.Code == "DbaseRecordFileLeeg");
            fileProblem.Should().NotBeNull();
            fileProblem.Message.Should().Be($"De meegegeven dbase record file ({ZipArchiveConstants.DBF_FILENAME}) is leeg.");
            fileProblem.Code.Should().Be("DbaseRecordFileLeeg");
        }

        [Fact]
        public void WhenNoDbaseShapeRecordsException_ThenProblem()
        {
            var zipArchiveRecordEntryValidator = new Mock<IZipArchiveDbaseEntryValidator>();
            zipArchiveRecordEntryValidator
                .Setup(x => x.Validate(It.IsAny<ZipArchiveEntry>()))
                .Throws(new NoShapeRecordsException(ZipArchiveConstants.SHP_FILENAME));

            // Act
            var sut = new ZipArchiveValidator(new Dictionary<string, IZipArchiveEntryValidator>(StringComparer.InvariantCultureIgnoreCase)
            {
                {
                    ZipArchiveConstants.DBF_FILENAME,
                    zipArchiveRecordEntryValidator.Object
                },
                {
                    ZipArchiveConstants.SHP_FILENAME,
                    new ZipArchiveShapeEntryValidator(Encoding.UTF8, new GrbShapeRecordsValidator())
                }
            });
            var zipArchiveProblems = sut.Validate(_fixture.ZipArchive);

            // Assert
            var fileProblem = zipArchiveProblems.FirstOrDefault(x => x is FileError error && error.Code == "ShapefileLeeg");
            fileProblem.Should().NotBeNull();
            fileProblem.Message.Should().Be($"De meegegeven shapefile ({ZipArchiveConstants.SHP_FILENAME}) is leeg.");
            fileProblem.Code.Should().Be("ShapefileLeeg");
        }
    }
}
