using Microsoft.AspNetCore.Http;
using SkiaSharp;

namespace Core.Security;

public sealed record ProcessedImage(byte[] MainImage, byte[] Thumbnail);

public static class ImageUploadProcessor
{
    public const long MaximumFileBytes = 5 * 1024 * 1024;
    public const int MaximumDimension = 8192;
    public const long MaximumPixelCount = 16_000_000;
    public const int DefaultMainMaxWidth = 1920;
    public const int DefaultThumbnailMaxWidth = 600;

    public static bool TryProcess(IFormFile? file, int mainMaxWidth, int thumbnailMaxWidth,
        out ProcessedImage? processedImage)
    {
        processedImage = null;
        if (file is null || file.Length <= 0 || file.Length > MaximumFileBytes ||
            mainMaxWidth <= 0 || thumbnailMaxWidth <= 0)
            return false;

        try
        {
            using var input = new MemoryStream();
            file.CopyTo(input);
            if (input.Length <= 0 || input.Length > MaximumFileBytes)
                return false;

            input.Position = 0;
            using var codec = SKCodec.Create(input);
            if (codec is null ||
                codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or
                    SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
                return false;

            var sourceInfo = codec.Info;
            var pixelCount = (long)sourceInfo.Width * sourceInfo.Height;
            if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0 ||
                sourceInfo.Width > MaximumDimension ||
                sourceInfo.Height > MaximumDimension ||
                pixelCount > MaximumPixelCount)
                return false;

            var bitmapInfo = new SKImageInfo(
                sourceInfo.Width,
                sourceInfo.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Premul);
            using var sourceBitmap = new SKBitmap(bitmapInfo);
            var decodeResult = codec.GetPixels(sourceBitmap.Info, sourceBitmap.GetPixels());
            if (decodeResult != SKCodecResult.Success)
                return false;

            var main = EncodeJpeg(sourceBitmap, mainMaxWidth);
            var thumbnail = EncodeJpeg(sourceBitmap, thumbnailMaxWidth);
            if (main.Length == 0 || thumbnail.Length == 0)
                return false;

            processedImage = new ProcessedImage(main, thumbnail);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }

    public static string SaveImagePair(ProcessedImage image, string mainDirectory, string thumbnailDirectory)
    {
        ArgumentNullException.ThrowIfNull(image);
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var mainPath = Path.Combine(mainDirectory, fileName);
        var thumbnailPath = Path.Combine(thumbnailDirectory, fileName);
        try
        {
            Directory.CreateDirectory(mainDirectory);
            Directory.CreateDirectory(thumbnailDirectory);
            WriteNewFile(mainPath, image.MainImage);
            WriteNewFile(thumbnailPath, image.Thumbnail);
            return fileName;
        }
        catch
        {
            DeleteCreatedFile(mainPath);
            DeleteCreatedFile(thumbnailPath);
            throw;
        }
    }

    public static string SaveImage(ProcessedImage image, string directory)
    {
        ArgumentNullException.ThrowIfNull(image);
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var path = Path.Combine(directory, fileName);
        try
        {
            Directory.CreateDirectory(directory);
            WriteNewFile(path, image.MainImage);
            return fileName;
        }
        catch
        {
            DeleteCreatedFile(path);
            throw;
        }
    }

    public static void DeleteImageIfSafe(string directory, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or ".." ||
            Path.IsPathRooted(fileName) || fileName.IndexOfAny(new[] { '/', '\\' }) >= 0 ||
            !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            return;

        var fullDirectory = Path.GetFullPath(directory);
        var root = fullDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(fullDirectory, fileName));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (fullPath.StartsWith(root, comparison) && File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static byte[] EncodeJpeg(SKBitmap source, int maximumWidth)
    {
        var scale = Math.Min(1d, maximumWidth / (double)source.Width);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var destination = new SKBitmap(info);
        using (var canvas = new SKCanvas(destination))
        using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(source, SKRect.Create(0, 0, width, height), paint);
            canvas.Flush();
        }
        using var image = SKImage.FromBitmap(destination);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded?.ToArray() ?? throw new InvalidDataException("Image encoding failed.");
    }

    private static void WriteNewFile(string path, byte[] content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(content);
        stream.Flush(flushToDisk: true);
    }

    private static void DeleteCreatedFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}