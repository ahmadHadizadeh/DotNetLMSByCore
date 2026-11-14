using System.Net;
using Asancode.Tests.Infrastructure;
using DataLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Asancode.Tests;

public sealed class MediaSqlServerInfrastructureTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    [Fact]
    public async Task Test_host_uses_sql_server_and_applies_project_migrations()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();

        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", dbContext.Database.ProviderName);
        Assert.True(await dbContext.Database.CanConnectAsync(cancellationToken));
        Assert.Contains(
            "20260218105211_mig1_FirstDb",
            await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken));

        Assert.Equal(0, await dbContext.CourseEpisode.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task Teacher_can_manage_episodes_only_for_a_course_they_own()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        MediaTeacherCourseScenario scenario;

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
            scenario = await MediaTestDataSeeder.SeedTeacherCourseAsync(
                dbContext,
                cancellationToken);
        }

        using var ownerClient = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        ownerClient.DefaultRequestHeaders.Add(
            MediaTestAuthenticationHandler.UserHeaderName,
            scenario.OwnerUserName);

        using var otherTeacherClient = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        otherTeacherClient.DefaultRequestHeaders.Add(
            MediaTestAuthenticationHandler.UserHeaderName,
            scenario.OtherTeacherUserName);

        using var ownerResponse = await ownerClient.GetAsync(
            $"/add-eppisode/{scenario.CourseId}",
            cancellationToken);
        using var otherTeacherResponse = await otherTeacherClient.GetAsync(
            $"/add-eppisode/{scenario.CourseId}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, otherTeacherResponse.StatusCode);
        Assert.Equal("/teacher-course", otherTeacherResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Admin_episode_create_page_is_mapped_and_requires_authentication()
    {
        using var client = fixture.Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        using var response = await client.GetAsync(
            "/Admin/Course/CreateEpisode/1",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login", response.Headers.Location?.OriginalString);
    }
}
