using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Asancode.Tests.Infrastructure;
using DataLayer.Context;
using DataLayer.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Asancode.Tests;

public sealed class AdminMediaUploadBoundaryTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    private const string CreatePage = "/Admin/Course/CreateEpisode";
    private const string EditPage = "/Admin/Course/EditEpisode";

    [Fact]
    public async Task Teacher_without_admin_permissions_cannot_open_admin_episode_page()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.TeacherUserName);

        using var response = await client.GetAsync(
            $"{CreatePage}/{scenario.CourseId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Login?", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task User_with_both_admin_permissions_can_open_episode_create_page()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.AdminUserName);

        using var response = await client.GetAsync(
            $"{CreatePage}/{scenario.CourseId}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_episode_create_rejects_post_without_antiforgery_token()
    {
        var scenario = await SeedScenarioAsync();
        using var client = CreateAuthenticatedClient(scenario.AdminUserName);
        await GetPageAsync(client, $"{CreatePage}/{scenario.CourseId}");

        using var form = CreateCreateForm(
            scenario.CourseId,
            $"csrf-{Guid.NewGuid():N}",
            "csrf-test.zip");

        using var response = await client.PostAsync(
            $"{CreatePage}/{scenario.CourseId}",
            form,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_upload_rejects_archive_larger_than_configured_limit()
    {
        var scenario = await SeedScenarioAsync();
        var title = $"oversize-{Guid.NewGuid():N}";
        var fileName = $"oversize-{Guid.NewGuid():N}.zip";
        using var client = CreateAuthenticatedClient(scenario.AdminUserName);
        await AddAntiforgeryHeaderAsync(client, $"{CreatePage}/{scenario.CourseId}");

        using var form = CreateCreateForm(
            scenario.CourseId,
            title,
            fileName,
            new byte[2049]);

        try
        {
            using var response = await client.PostAsync(
                $"{CreatePage}/{scenario.CourseId}",
                form,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }
        finally
        {
            await CleanupEpisodeAsync(scenario.CourseId, title, fileName);
        }
    }

    [Fact]
    public async Task Admin_upload_rejects_file_names_containing_path_segments()
    {
        var scenario = await SeedScenarioAsync();
        var title = $"traversal-{Guid.NewGuid():N}";
        var leafName = $"admin-path-{Guid.NewGuid():N}.zip";
        var suppliedFileName = $"../../{leafName}";
        using var client = CreateAuthenticatedClient(scenario.AdminUserName);
        await AddAntiforgeryHeaderAsync(client, $"{CreatePage}/{scenario.CourseId}");

        using var form = CreateCreateForm(
            scenario.CourseId,
            title,
            suppliedFileName);

        try
        {
            using var response = await client.PostAsync(
                $"{CreatePage}/{scenario.CourseId}",
                form,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await CleanupEpisodeAsync(scenario.CourseId, title, suppliedFileName);
        }
    }

    [Fact]
    public async Task Admin_upload_rejects_file_extensions_outside_archive_allowlist()
    {
        var scenario = await SeedScenarioAsync();
        var title = $"extension-{Guid.NewGuid():N}";
        var fileName = $"admin-invalid-{Guid.NewGuid():N}.exe";
        using var client = CreateAuthenticatedClient(scenario.AdminUserName);
        await AddAntiforgeryHeaderAsync(client, $"{CreatePage}/{scenario.CourseId}");

        using var form = CreateCreateForm(scenario.CourseId, title, fileName);

        try
        {
            using var response = await client.PostAsync(
                $"{CreatePage}/{scenario.CourseId}",
                form,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await CleanupEpisodeAsync(scenario.CourseId, title, fileName);
        }
    }

    [Fact]
    public async Task Admin_can_upload_an_archive_and_episode_record_is_persisted()
    {
        var scenario = await SeedScenarioAsync();
        var title = $"valid-{Guid.NewGuid():N}";
        var fileName = $"admin-valid-{Guid.NewGuid():N}.zip";
        using var client = CreateAuthenticatedClient(scenario.AdminUserName);
        await AddAntiforgeryHeaderAsync(client, $"{CreatePage}/{scenario.CourseId}");

        using var form = CreateCreateForm(scenario.CourseId, title, fileName);

        try
        {
            using var response = await client.PostAsync(
                $"{CreatePage}/{scenario.CourseId}",
                form,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(
                $"/Admin/Course/IndexEpisode/{scenario.CourseId}",
                response.Headers.Location?.OriginalString);

            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
            var episode = await dbContext.CourseEpisode.SingleAsync(
                item => item.CourseId == scenario.CourseId && item.EpisodeTitle == title,
                TestContext.Current.CancellationToken);

            Assert.False(string.IsNullOrWhiteSpace(episode.EpisodeFileName));
            Assert.Equal(
                Path.GetFileName(episode.EpisodeFileName),
                episode.EpisodeFileName);
            Assert.Equal(".zip", Path.GetExtension(episode.EpisodeFileName));
            Assert.NotEqual(fileName, episode.EpisodeFileName);
        }
        finally
        {
            await CleanupEpisodeAsync(scenario.CourseId, title, fileName);
        }
    }

    [Fact]
    public async Task Admin_can_edit_episode_metadata_without_replacing_its_media_file()
    {
        var scenario = await SeedScenarioAsync();
        var episodeTitle = $"edit-original-{Guid.NewGuid():N}";
        var updatedTitle = $"edit-updated-{Guid.NewGuid():N}";
        var fileName = $"existing-media-{Guid.NewGuid():N}.zip";
        var episodeId = await SeedEpisodeAsync(scenario.CourseId, episodeTitle, fileName);
        var filePath = GetEpisodeFilePath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, CreateTestArchive(), TestContext.Current.CancellationToken);

        using var client = CreateAuthenticatedClient(scenario.AdminUserName);
        await AddAntiforgeryHeaderAsync(client, $"{EditPage}/{episodeId}");

        using var form = CreateEditForm(scenario.CourseId, episodeId, updatedTitle, fileName);

        try
        {
            using var response = await client.PostAsync(
                $"{EditPage}/{episodeId}",
                form,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(
                $"/Admin/Course/IndexEpisode/{scenario.CourseId}",
                response.Headers.Location?.OriginalString);

            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
            var episode = await dbContext.CourseEpisode.SingleAsync(
                item => item.EpisodeId == episodeId,
                TestContext.Current.CancellationToken);

            Assert.Equal(updatedTitle, episode.EpisodeTitle);
            Assert.Equal(fileName, episode.EpisodeFileName);
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            await CleanupEpisodeByIdAsync(episodeId, filePath);
        }
    }

    private async Task<AdminMediaScenario> SeedScenarioAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var teacherScenario = await MediaTestDataSeeder.SeedTeacherCourseAsync(
            dbContext,
            TestContext.Current.CancellationToken);
        var adminUserName = await MediaTestDataSeeder.SeedAdminUserAsync(
            dbContext,
            TestContext.Current.CancellationToken);

        return new AdminMediaScenario(
            teacherScenario.OwnerUserName,
            adminUserName,
            teacherScenario.CourseId);
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
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/html"));

        return client;
    }

    private static async Task GetPageAsync(HttpClient client, string pageUrl)
    {
        using var response = await client.GetAsync(
            pageUrl,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task AddAntiforgeryHeaderAsync(HttpClient client, string pageUrl)
    {
        using var response = await client.GetAsync(
            pageUrl,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync(
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

    private static MultipartFormDataContent CreateCreateForm(
        int courseId,
        string title,
        string fileName,
        byte[]? fileBytes = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(courseId.ToString()), "CourseEpisode.CourseId");
        form.Add(new StringContent(title), "CourseEpisode.EpisodeTitle");
        form.Add(new StringContent("00:12:00"), "CourseEpisode.EpisodeTime");
        form.Add(new StringContent("true"), "CourseEpisode.IsFree");

        var fileContent = new ByteArrayContent(fileBytes ?? CreateTestArchive());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "fileEpisode", fileName);
        return form;
    }

    private static MultipartFormDataContent CreateEditForm(
        int courseId,
        int episodeId,
        string title,
        string currentFileName)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(courseId.ToString()), "CourseEpisode.CourseId");
        form.Add(new StringContent(episodeId.ToString()), "CourseEpisode.EpisodeId");
        form.Add(new StringContent(title), "CourseEpisode.EpisodeTitle");
        form.Add(new StringContent("00:15:00"), "CourseEpisode.EpisodeTime");
        form.Add(new StringContent("true"), "CourseEpisode.IsFree");
        form.Add(new StringContent(currentFileName), "CourseEpisode.EpisodeFileName");
        return form;
    }

    private static byte[] CreateTestArchive()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("episode.mp4");
            using var stream = entry.Open();
            stream.Write(new byte[]
            {
                0, 0, 0, 24, (byte)'f', (byte)'t', (byte)'y', (byte)'p',
                (byte)'i', (byte)'s', (byte)'o', (byte)'m', 0, 0, 2, 0,
                (byte)'i', (byte)'s', (byte)'o', (byte)'m',
                (byte)'i', (byte)'s', (byte)'o', (byte)'2'
            });
        }

        return output.ToArray();
    }

    private async Task<int> SeedEpisodeAsync(int courseId, string title, string fileName)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var episode = new CourseEpisode
        {
            CourseId = courseId,
            EpisodeTitle = title,
            EpisodeTime = TimeSpan.FromMinutes(10),
            EpisodeFileName = fileName,
            IsFree = true,
            IsDelete = false
        };

        dbContext.CourseEpisode.Add(episode);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return episode.EpisodeId;
    }

    private async Task CleanupEpisodeAsync(int courseId, string title, string fileName)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var episode = await dbContext.CourseEpisode.IgnoreQueryFilters().SingleOrDefaultAsync(
            item => item.CourseId == courseId && item.EpisodeTitle == title,
            TestContext.Current.CancellationToken);

        if (episode is not null)
        {
            dbContext.CourseEpisode.Remove(episode);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        DeleteFileWithinRepository(GetEpisodeFilePath(fileName));
    }

    private async Task CleanupEpisodeByIdAsync(int episodeId, string filePath)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var episode = await dbContext.CourseEpisode.IgnoreQueryFilters().SingleOrDefaultAsync(
            item => item.EpisodeId == episodeId,
            TestContext.Current.CancellationToken);

        if (episode is not null)
        {
            dbContext.CourseEpisode.Remove(episode);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        DeleteFileWithinRepository(filePath);
    }

    private static string GetEpisodeFilePath(string fileName) =>
        Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "Episode",
            fileName));

    private static void DeleteFileWithinRepository(string path)
    {
        var root = Path.GetFullPath(Directory.GetCurrentDirectory())
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (path.StartsWith(root, StringComparison.Ordinal) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed record AdminMediaScenario(
        string TeacherUserName,
        string AdminUserName,
        int CourseId);
}
