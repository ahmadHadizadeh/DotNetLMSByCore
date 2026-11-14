namespace Asancode.Web.Options;

public sealed class MediaUploadOptions
{
    public const string SectionName = "MediaUpload";
    public const long DefaultMaxArchiveBytes = 100L * 1024 * 1024;

    public long MaxArchiveBytes { get; set; } = DefaultMaxArchiveBytes;
}
