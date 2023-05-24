namespace Grb.Building.Tests.UploadProcessor
{
    using Xunit;

    [CollectionDefinition(COLLECTION)]
    public class ZipArchiveCollectionFixture : ICollectionFixture<ZipArchiveFixture>
    {
        public const string COLLECTION = "ZipArchiveOpener";
    }
}
