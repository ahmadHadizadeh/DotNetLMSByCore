using System.IO.Compression;
using Asancode.Web.Media;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Asancode.Tests;

public sealed class MediaArchiveUploadValidatorTests
{
    [Fact]
    public async Task Zip_extension_with_valid_local_file_signature_is_accepted()
    {
        var header = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };

        Assert.Null(await ValidateAsync("lesson.zip", header));
    }

    [Fact]
    public async Task Empty_zip_archive_is_accepted()
    {
        using var output = new MemoryStream();
        using (new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
        }

        Assert.Null(await ValidateAsync("lesson.zip", output.ToArray()));
    }

    [Fact]
    public async Task Rar4_extension_with_valid_signature_is_accepted()
    {
        var header = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00, 0x00 };

        Assert.Null(await ValidateAsync("lesson.rar", header));
    }

    [Fact]
    public async Task Rar5_extension_with_valid_signature_is_accepted()
    {
        var header = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 };

        Assert.Null(await ValidateAsync("lesson.rar", header));
    }

    [Theory]
    [InlineData("lesson.zip")]
    [InlineData("lesson.rar")]
    public async Task Archive_extension_with_arbitrary_content_is_rejected(string fileName)
    {
        var content = System.Text.Encoding.UTF8.GetBytes("this is not an archive");

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            await ValidateAsync(fileName, content));
    }

    [Fact]
    public async Task Zip_extension_with_rar_signature_is_rejected()
    {
        var rarHeader = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00, 0x00 };

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            await ValidateAsync("lesson.zip", rarHeader));
    }

    [Fact]
    public async Task Rar_extension_with_zip_signature_is_rejected()
    {
        var zipHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            await ValidateAsync("lesson.rar", zipHeader));
    }

    private static async Task<int?> ValidateAsync(string fileName, byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        var file = new FormFile(stream, 0, stream.Length, "files", fileName);

        return await MediaArchiveUploadValidator.GetFailureStatusCodeAsync(
            file,
            maximumArchiveBytes: 2048);
    }
}
