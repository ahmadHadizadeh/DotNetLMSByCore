using Microsoft.AspNetCore.Http;

namespace Core.Security;

public static class ImageValidator
{
    public static bool IsImage(this IFormFile file) =>
        ImageUploadProcessor.TryProcess(file, ImageUploadProcessor.DefaultMainMaxWidth,
            ImageUploadProcessor.DefaultThumbnailMaxWidth, out _);
}