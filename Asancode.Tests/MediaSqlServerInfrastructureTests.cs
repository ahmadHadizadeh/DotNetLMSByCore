using System.Net;
using Asancode.Tests.Infrastructure;
using DataLayer.Context;
using DataLayer.Entities.User;
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

        // The test authentication scheme challenges with 401; production cookie auth redirects.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    [Fact]
    public async Task Wallet_type_id_is_the_actual_foreign_key_for_transaction_types_one_and_two()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var uniqueKey = Guid.NewGuid().ToString("N");
        var user = CreateRelationshipTestUser($"wallet-{uniqueKey}");

        dbContext.Users.Add(user);
        dbContext.WalletTypes.AddRange(
            new WalletType { TypeId = 1, TypeTitle = "Deposit" },
            new WalletType { TypeId = 2, TypeTitle = "Withdrawal" });
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Wallets.AddRange(
            new Wallet
            {
                TypeId = 1,
                UserId = user.UserId,
                Amount = 100,
                Description = "SQL Server relationship test deposit",
                IsPay = true,
                CreateDate = DateTime.UtcNow
            },
            new Wallet
            {
                TypeId = 2,
                UserId = user.UserId,
                Amount = 40,
                Description = "SQL Server relationship test withdrawal",
                IsPay = true,
                CreateDate = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        var persistedTypeIds = await dbContext.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == user.UserId)
            .OrderBy(wallet => wallet.TypeId)
            .Select(wallet => wallet.TypeId)
            .ToListAsync(cancellationToken);

        Assert.Equal(new[] { 1, 2 }, persistedTypeIds);

        var walletTypeForeignKey = dbContext.Model
            .FindEntityType(typeof(Wallet))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(WalletType));

        Assert.Equal(nameof(Wallet.TypeId), Assert.Single(walletTypeForeignKey.Properties).Name);
        Assert.False(Assert.Single(walletTypeForeignKey.Properties).IsShadowProperty());
        Assert.Null(dbContext.Model.FindEntityType(typeof(Wallet))!.FindProperty("WalletTypesTypeId"));
    }

    [Fact]
    public async Task Plan_user_id_is_the_actual_foreign_key_and_persists_without_a_shadow_key()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var uniqueKey = Guid.NewGuid().ToString("N");
        var user = CreateRelationshipTestUser($"plan-{uniqueKey}");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var plan = new Plan
        {
            UserId = user.UserId,
            PlanLevel = $"SQL Server relationship test {uniqueKey}",
            PlanPrice = 100,
            Description = "Test Plan FK using the scalar UserId property",
            IsDelete = false
        };

        dbContext.Plans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        var persistedPlan = await dbContext.Plans
            .AsNoTracking()
            .Include(existingPlan => existingPlan.Users)
            .SingleAsync(existingPlan => existingPlan.PlanId == plan.PlanId, cancellationToken);

        Assert.Equal(user.UserId, persistedPlan.UserId);
        Assert.Equal(user.UserId, persistedPlan.Users.UserId);

        var planUserForeignKey = dbContext.Model
            .FindEntityType(typeof(Plan))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Users));

        Assert.Equal(nameof(Plan.UserId), Assert.Single(planUserForeignKey.Properties).Name);
        Assert.False(Assert.Single(planUserForeignKey.Properties).IsShadowProperty());
        Assert.Null(dbContext.Model.FindEntityType(typeof(Plan))!.FindProperty("UsersUserId"));
    }

    private static Users CreateRelationshipTestUser(string userName) =>
        new()
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            Password = "test-only-not-a-real-password",
            UserImage = string.Empty,
            ActiveCode = string.Empty,
            RegisterDate = DateTime.UtcNow,
            IsActive = true,
            IsDelete = false
        };

}
