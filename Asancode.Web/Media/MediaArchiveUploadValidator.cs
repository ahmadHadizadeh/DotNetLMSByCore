using Microsoft.AspNetCore.Http;
using System.IO;

namespace Asancode.Web.Media;

public static class MediaArchiveUploadValidator
{
    private static readonly byte[] ZipLocalFileHeader = { 0x50, 0x4B, 0x03, 0x04 };
    private static readonly byte[] ZipEndOfCentralDirectory = { 0x50, 0x4B, 0x05, 0x06 };
    private static readonly byte[] ZipSpannedArchive = { 0x50, 0x4B, 0x07, 0x08 };
    private static readonly byte[] Rar4Signature = { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 };
    private static readonly byte[] Rar5Signature = { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 };

    public static async Task<int?> GetFailureStatusCodeAsync(
        IFormFile? file,
        long maximumArchiveBytes,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            return StatusCodes.Status400BadRequest;
        }

        if (maximumArchiveBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumArchiveBytes));
        }

        if (file.Length > maximumArchiveBytes)
        {
            return StatusCodes.Status413PayloadTooLarge;
        }

        var fileName = file.FileName;
        if (string.IsNullOrWhiteSpace(fileName) ||
            Path.IsPathRooted(fileName) ||
            fileName.IndexOfAny(new[] { '/', '\\' }) >= 0)
        {
            return StatusCodes.Status400BadRequest;
        }

        var extension = Path.GetExtension(fileName);
        var isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
        var isRar = extension.Equals(".rar", StringComparison.OrdinalIgnoreCase);
        if (!isZip && !isRar)
        {
            return StatusCodes.Status400BadRequest;
        }

        var header = new byte[8];
        var bytesRead = 0;
        await using (var stream = file.OpenReadStream())
        {
            while (bytesRead < header.Length)
            {
                var read = await stream.ReadAsync(
                    header.AsMemory(bytesRead, header.Length - bytesRead),
                    cancellationToken);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }
        }

        var hasValidSignature = isZip
            ? Matches(header, bytesRead, ZipLocalFileHeader) ||
              Matches(header, bytesRead, ZipEndOfCentralDirectory) ||
              Matches(header, bytesRead, ZipSpannedArchive)
            : Matches(header, bytesRead, Rar4Signature) ||
              Matches(header, bytesRead, Rar5Signature);

        return hasValidSignature ? null : StatusCodes.Status400BadRequest;
    }

    private static bool Matches(byte[] header, int bytesRead, byte[] signature)
    {
        if (bytesRead < signature.Length)
        {
            return false;
        }

        for (var index = 0; index < signature.Length; index++)
        {
            if (header[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }
}
