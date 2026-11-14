using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Asancode.Tests.Infrastructure;
using DataLayer.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Asancode.Tests;

public sealed class MediaDropzoneUploadBoundaryTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    private const string DropzoneRoute = "/UserPanel/Teacher/DropzoneTarget";

    [Fact]
    public async Task Anonymous_user_cannot_upload_an_episode_archive()
    {
        var scenario = await SeedScenarioAsync();
        using var client = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        using var form = CreateArchiveForm();
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_who_does_not_own_the_course_cannot_upload_an_archive()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OtherTeacherUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.OtherTeacherCourseId);

        using var form = CreateArchiveForm();
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await DeleteLegacyUploadIfCreatedAsync(response);
        }
    }

    [Fact]
    public async Task Upload_endpoint_rejects_get_requests()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);

        using var response = await client.GetAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Upload_endpoint_rejects_post_without_antiforgery_token()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);

        using var form = CreateArchiveForm();
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await DeleteLegacyUploadIfCreatedAsync(response);
        }
    }

    [Fact]
    public async Task Course_owner_can_upload_an_archive_with_a_valid_antiforgery_token()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.CourseId);

        using var form = CreateArchiveForm();
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("Success", payload.RootElement.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(
                payload.RootElement.GetProperty("data").GetString()));
        }
        finally
        {
            await DeleteLegacyUploadIfCreatedAsync(response);
        }
    }

    [Fact]
    public async Task Upload_endpoint_rejects_empty_archive_files()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.CourseId);

        using var form = CreateArchiveForm(Array.Empty<byte>());
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_endpoint_rejects_more_than_one_archive_file()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.CourseId);

        using var form = CreateArchiveForm();
        var secondFile = new ByteArrayContent(CreateValidTestZipArchive());
        secondFile.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(secondFile, "files", "episode-2.zip");

        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_endpoint_rejects_files_larger_than_the_configured_archive_limit()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.CourseId);

        using var form = CreateArchiveForm(new byte[2049]);
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }
        finally
        {
            await DeleteLegacyUploadIfCreatedAsync(response);
        }
    }

    [Fact]
    public async Task Upload_endpoint_rejects_file_extensions_outside_the_archive_allowlist()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.CourseId);

        using var form = CreateArchiveForm(fileName: "episode.exe");
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_endpoint_rejects_missing_course()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.OwnerUserName);
        await AddAntiforgeryHeaderAsync(client, scenario.CourseId);

        using var form = CreateArchiveForm();
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId=2147483000",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Browser_html_404_requests_keep_the_legacy_error_redirect()
    {
        using var client = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        using var response = await client.GetAsync(
            "/no-such-route-for-media-test",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/Error404", response.Headers.Location?.OriginalString);
    }

    private async Task<MediaTeacherCourseScenario> SeedScenarioAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();

        return await MediaTestDataSeeder.SeedTeacherCourseAsync(
            dbContext,
            TestContext.Current.CancellationToken);
    }

    private HttpClient CreateAuthenticatedClient(string userName)
    {
        var client = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        client.DefaultRequestHeaders.Add(
            MediaTestAuthenticationHandler.UserHeaderName,
            userName);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client, int tokenCourseId)
    {
        // Generate a token from the real authenticated Teacher page so it is
        // bound to the same claims principal that posts the upload.
        using var pageResponse = await client.GetAsync(
            $"/add-eppisode/{tokenCourseId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var html = await pageResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        var tokenInput = Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .FirstOrDefault(tag => Regex.IsMatch(
                tag,
                "\\bname\\s*=\\s*[\"']__RequestVerificationToken[\"']",
                RegexOptions.IgnoreCase));

        Assert.False(string.IsNullOrWhiteSpace(tokenInput));

        var tokenMatch = Regex.Match(
            tokenInput!,
            "\\bvalue\\s*=\\s*[\"'](?<token>[^\"']+)[\"']",
            RegexOptions.IgnoreCase);
        Assert.True(tokenMatch.Success);

        client.DefaultRequestHeaders.Remove("RequestVerificationToken");
        client.DefaultRequestHeaders.Add(
            "RequestVerificationToken",
            tokenMatch.Groups["token"].Value);
    }

    private static MultipartFormDataContent CreateArchiveForm(
        byte[]? fileBytes = null,
        string fileName = "episode.zip")
    {
        fileBytes ??= CreateValidTestZipArchive();

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "files", fileName);
        return form;
    }

    private static byte[] CreateValidTestZipArchive()
    {
        using var archiveBytes = new MemoryStream();

        using (var archive = new ZipArchive(
                   archiveBytes,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            var entry = archive.CreateEntry("episode.mp4");
            using var entryStream = entry.Open();
            entryStream.Write(new byte[]
            {
                0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p',
                (byte)'i', (byte)'s', (byte)'o', (byte)'m', 0, 0, 2, 0,
                (byte)'i', (byte)'s', (byte)'o', (byte)'m',
                (byte)'i', (byte)'s', (byte)'o', (byte)'2'
            });
        }

        return archiveBytes.ToArray();
    }

    private static async Task DeleteLegacyUploadIfCreatedAsync(HttpResponseMessage response)
    {
        try
        {
            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);

            if (!payload.RootElement.TryGetProperty("status", out var status) ||
                status.GetString() != "Success" ||
                !payload.RootElement.TryGetProperty("data", out var data))
            {
                return;
            }

            var fileName = data.GetString();
            if (string.IsNullOrWhiteSpace(fileName) ||
                !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            {
                return;
            }

            var legacyPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Episode",
                fileName);

            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch (JsonException)
        {
            // A rejected request has no JSON upload receipt and needs no cleanup.
        }
    }
}
