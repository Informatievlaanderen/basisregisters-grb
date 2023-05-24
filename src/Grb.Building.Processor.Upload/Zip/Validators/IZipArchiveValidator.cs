namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System.IO.Compression;

    public interface IZipArchiveValidator
    {
        ZipArchiveProblems Validate(ZipArchive archive);
    }
}
