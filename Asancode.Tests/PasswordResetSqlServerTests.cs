using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Asancode.Tests.Infrastructure;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Asancode.Tests;

public sealed class PasswordResetSqlServerTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    private const string InitialPassword = "Initial-Password-2026!";

    [Fact]
    public async Task Issued_token_is_random_hashed_and_expires_after_six_hours()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var user = await AddUserAsync(dbContext, scope.ServiceProvider.GetRequiredService<UserPasswordHasher>());
        var originalActiveCode = user.ActiveCode;
        var beforeIssue = DateTime.UtcNow;

        var token = userService.CreatePasswordResetToken(user.UserId);

        var afterIssue = DateTime.UtcNow;
        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);

        Assert.Equal(43, token.Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))), persisted.PasswordResetTokenHash);
        Assert.NotEqual(token, persisted.PasswordResetTokenHash);
        Assert.NotNull(persisted.PasswordResetTokenExpiresAt);
        Assert.InRange(persisted.PasswordResetTokenExpiresAt.Value, beforeIssue.AddHours(6), afterIssue.AddHours(6));
        Assert.Equal(originalActiveCode, persisted.ActiveCode);
        Assert.True(userService.IsPasswordResetTokenValid(token));
    }

    [Fact]
    public async Task Expired_token_is_rejected_without_changing_password()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var hasher = scope.ServiceProvider.GetRequiredService<UserPasswordHasher>();
        var user = await AddUserAsync(dbContext, hasher);
        var token = userService.CreatePasswordResetToken(user.UserId);
        var originalPasswordHash = user.Password;

        await dbContext.Users.Where(item => item.UserId == user.UserId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.PasswordResetTokenExpiresAt, DateTime.UtcNow.AddMinutes(-1)),
                TestContext.Current.CancellationToken);

        Assert.False(userService.IsPasswordResetTokenValid(token));
        Assert.False(userService.TryResetPassword(token, "Replacement-Password-2026!"));

        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);
        Assert.Equal(originalPasswordHash, persisted.Password);
    }

    [Fact]
    public async Task Invalid_token_is_rejected_without_changing_password()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var user = await AddUserAsync(dbContext, scope.ServiceProvider.GetRequiredService<UserPasswordHasher>());
        var originalPasswordHash = user.Password;
        const string invalidToken = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

        Assert.False(userService.IsPasswordResetTokenValid(invalidToken));
        Assert.False(userService.TryResetPassword(invalidToken, "Replacement-Password-2026!"));

        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);
        Assert.Equal(originalPasswordHash, persisted.Password);
    }

    [Fact]
    public async Task Valid_token_changes_password_and_cannot_be_reused()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var hasher = scope.ServiceProvider.GetRequiredService<UserPasswordHasher>();
        var user = await AddUserAsync(dbContext, hasher);
        var token = userService.CreatePasswordResetToken(user.UserId);
        const string newPassword = "Replacement-Password-2026!";

        Assert.True(userService.TryResetPassword(token, newPassword));
        Assert.False(userService.IsPasswordResetTokenValid(token));
        Assert.False(userService.TryResetPassword(token, "Second-Replacement-2026!"));

        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyPassword(persisted, persisted.Password, newPassword));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyPassword(persisted, persisted.Password, InitialPassword));
        Assert.Null(persisted.PasswordResetTokenHash);
        Assert.Null(persisted.PasswordResetTokenExpiresAt);
        Assert.Equal(user.ActiveCode, persisted.ActiveCode);
    }

    [Fact]
    public async Task Concurrent_reset_requests_can_consume_the_same_token_only_once()
    {
        int userId;
        string token;

        await using (var seedScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var dbContext = seedScope.ServiceProvider.GetRequiredService<AsanwebContext>();
            var service = seedScope.ServiceProvider.GetRequiredService<IUserService>();
            var user = await AddUserAsync(dbContext, seedScope.ServiceProvider.GetRequiredService<UserPasswordHasher>());
            userId = user.UserId;
            token = service.CreatePasswordResetToken(userId);
        }

        await using var firstScope = fixture.Factory.Services.CreateAsyncScope();
        await using var secondScope = fixture.Factory.Services.CreateAsyncScope();
        var firstService = firstScope.ServiceProvider.GetRequiredService<IUserService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<IUserService>();

        var results = await Task.WhenAll(
            Task.Run(() => firstService.TryResetPassword(token, "Concurrent-Password-2026-A!")),
            Task.Run(() => secondService.TryResetPassword(token, "Concurrent-Password-2026-B!")));

        Assert.Equal(1, results.Count(result => result));

        await using var verifyScope = fixture.Factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var persisted = await verifyContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == userId, TestContext.Current.CancellationToken);
        var hasher = verifyScope.ServiceProvider.GetRequiredService<UserPasswordHasher>();

        Assert.Null(persisted.PasswordResetTokenHash);
        Assert.Null(persisted.PasswordResetTokenExpiresAt);
        Assert.True(
            hasher.VerifyPassword(persisted, persisted.Password, "Concurrent-Password-2026-A!") != PasswordVerificationResult.Failed
            || hasher.VerifyPassword(persisted, persisted.Password, "Concurrent-Password-2026-B!") != PasswordVerificationResult.Failed);
    }

    private static async Task<Users> AddUserAsync(AsanwebContext dbContext, UserPasswordHasher hasher)
    {
        var id = Guid.NewGuid().ToString("N");
        var user = new Users
        {
            UserName = $"password-reset-{id}",
            Email = $"password-reset-{id}@example.test",
            Password = "pending",
            UserImage = "Defult.jpg",
            ActiveCode = Guid.NewGuid().ToString("N"),
            RegisterDate = DateTime.UtcNow,
            IsActive = true,
            IsDelete = false
        };

        user.Password = hasher.HashPassword(user, InitialPassword);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }
}
