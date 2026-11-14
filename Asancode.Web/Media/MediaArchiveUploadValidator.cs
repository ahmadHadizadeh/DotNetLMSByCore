using Microsoft.AspNetCore.Http;
using System.IO;

namespace Asancode.Web.Media;

public static class MediaArchiveUploadValidator
{
    public static bool TryValidate(
        IFormFile? file,
        long maximumArchiveBytes,
        out int failureStatusCode)
    {
        if (file is null || file.Length <= 0)
        {
            failureStatusCode = StatusCodes.Status400BadRequest;
            return false;
        }

        if (file.Length > maximumArchiveBytes)
        {
            failureStatusCode = StatusCodes.Status413PayloadTooLarge;
            return false;
        }

        var fileName = file.FileName;
        if (string.IsNullOrWhiteSpace(fileName) ||
            Path.IsPathRooted(fileName) ||
            fileName.IndexOfAny(new[] { '/', '\\' }) >= 0)
        {
            failureStatusCode = StatusCodes.Status400BadRequest;
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".rar", StringComparison.OrdinalIgnoreCase))
        {
            failureStatusCode = StatusCodes.Status400BadRequest;
            return false;
        }

        failureStatusCode = StatusCodes.Status200OK;
        return true;
    }
}
