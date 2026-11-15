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

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void TryProcess_accepts_each_supported_source_format_and_returns_jpeg(
        SKEncodedImageFormat sourceFormat)
    {
        var file = CreateImageFile("source-image.bin", "application/octet-stream", sourceFormat);

        var accepted = ImageUploadProcessor.TryProcess(
            file,
            ImageUploadProcessor.DefaultMainMaxWidth,
            ImageUploadProcessor.DefaultThumbnailMaxWidth,
            out var result);

        Assert.True(accepted);
        Assert.NotNull(result);
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, result!.MainImage[..3]);
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, result.Thumbnail[..3]);
    }

    [Fact]
    public void TryProcess_resizes_main_and_thumbnail_without_changing_aspect_ratio()
    {
        var file = CreateImageFile(
            "wide.png",
            "image/png",
            SKEncodedImageFormat.Png,
            width: 2400,
            height: 1200);

        var accepted = ImageUploadProcessor.TryProcess(
            file,
            mainMaxWidth: 1920,
            thumbnailMaxWidth: 600,
            out var result);

        Assert.True(accepted);
        Assert.NotNull(result);

        var mainInfo = GetEncodedImageInfo(result!.MainImage);
        var thumbnailInfo = GetEncodedImageInfo(result.Thumbnail);

        Assert.Equal(1920, mainInfo.Width);
        Assert.Equal(960, mainInfo.Height);
        Assert.Equal(600, thumbnailInfo.Width);
        Assert.Equal(300, thumbnailInfo.Height);
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

    private static FormFile CreateImageFile(
        string fileName,
        string contentType,
        SKEncodedImageFormat format = SKEncodedImageFormat.Png,
        int width = 4,
        int height = 3)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height));
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, 100);
        var bytes = encoded?.ToArray()
            ?? throw new InvalidOperationException($"Could not encode the {format} test image.");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static SKImageInfo GetEncodedImageInfo(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidOperationException("The processed image could not be decoded.");
        return codec.Info;
    }
}
