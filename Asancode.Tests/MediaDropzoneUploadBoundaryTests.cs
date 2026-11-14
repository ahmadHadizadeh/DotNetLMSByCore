using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
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
        await AddAntiforgeryHeaderAsync(client);

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
        await AddAntiforgeryHeaderAsync(client);

        using var form = CreateArchiveForm();
        using var response = await client.PostAsync(
            $"{DropzoneRoute}?courseId={scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        try
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(
                "Success",
                payload.RootElement.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(
                payload.RootElement.GetProperty("data").GetString()));
        }
        finally
        {
            await DeleteLegacyUploadIfCreatedAsync(response);
        }
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

        return client;
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client)
    {
        using var response = await client.GetAsync(
            "/__test/antiforgery",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(),
            cancellationToken: TestContext.Current.CancellationToken);

        var requestToken = payload.RootElement.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));

        client.DefaultRequestHeaders.Remove("RequestVerificationToken");
        client.DefaultRequestHeaders.Add("RequestVerificationToken", requestToken);
    }

    private static MultipartFormDataContent CreateArchiveForm()
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

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(archiveBytes.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "files", "episode.zip");
        return form;
    }

    private static async Task DeleteLegacyUploadIfCreatedAsync(HttpResponseMessage response)
    {
        try
        {
            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());

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
