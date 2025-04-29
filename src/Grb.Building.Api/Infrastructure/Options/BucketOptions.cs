namespace Grb.Building.Api.Infrastructure.Options
{
    public sealed class BucketOptions
    {
        public const string ConfigKey = "Bucket";

        public required string BucketName { get; set; }
        public int UrlExpirationInMinutes { get; set; }
    }
}
