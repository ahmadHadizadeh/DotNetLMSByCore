using Core.Security;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
using Xunit;

namespace Asancode.Tests;

public sealed class ImageUploadProcessorTests
{
    [Fact]
    public void TryProcess_accepts_real_image_bytes_and_reencodes_to_jpeg()
    {
        var file = CreateImageFile("payload.txt", "text/plain");
        var accepted = ImageUploadProcessor.TryProcess(file,
            ImageUploadProcessor.DefaultMainMaxWidth,
            ImageUploadProcessor.DefaultThumbnailMaxWidth, out var result);

        Assert.True(accepted);
        Assert.NotNull(result);
        Assert.True(result!.MainImage.Length > 3);
        Assert.True(result.Thumbnail.Length > 3);
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, result.MainImage[..3]);
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, result.Thumbnail[..3]);
    }

    [Fact]
    public void TryProcess_rejects_non_image_payload_even_when_extension_is_png()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("not an image");
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "image", "spoofed.png");
        var accepted = ImageUploadProcessor.TryProcess(file,
            ImageUploadProcessor.DefaultMainMaxWidth,
            ImageUploadProcessor.DefaultThumbnailMaxWidth, out var result);
        Assert.False(accepted);
        Assert.Null(result);
    }

    [Fact]
    public void TryProcess_rejects_uploads_over_the_size_limit()
    {
        var bytes = new byte[(int)ImageUploadProcessor.MaximumFileBytes + 1];
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "image", "large.png");
        var accepted = ImageUploadProcessor.TryProcess(file,
            ImageUploadProcessor.DefaultMainMaxWidth,
            ImageUploadProcessor.DefaultThumbnailMaxWidth, out var result);
        Assert.False(accepted);
        Assert.Null(result);
    }

    [Fact]
    public void TryProcess_rejects_images_exceeding_dimension_limit()
    {
        using var bitmap = new SKBitmap(
            new SKImageInfo(ImageUploadProcessor.MaximumDimension + 1, 1));
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = encoded!.ToArray();
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "image", "oversized-dimensions.png");

        var accepted = ImageUploadProcessor.TryProcess(
            file,
            ImageUploadProcessor.DefaultMainMaxWidth,
            ImageUploadProcessor.DefaultThumbnailMaxWidth,
            out var result);

        Assert.False(accepted);
        Assert.Null(result);
    }

    [Fact]
    public void DeleteImageIfSafe_does_not_delete_files_outside_media_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"image-upload-{Guid.NewGuid():N}");
        var mediaDirectory = Path.Combine(root, "media");
        Directory.CreateDirectory(mediaDirectory);
        var outsideFile = Path.Combine(root, "keep.txt");
        File.WriteAllText(outsideFile, "must remain");
        try
        {
            ImageUploadProcessor.DeleteImageIfSafe(mediaDirectory, "../keep.txt");
            Assert.True(File.Exists(outsideFile));
            Assert.Equal("must remain", File.ReadAllText(outsideFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static FormFile CreateImageFile(string fileName, string contentType)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(4, 3));
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = encoded!.ToArray();
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}