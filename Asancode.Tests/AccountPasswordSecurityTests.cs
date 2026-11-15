using Asancode.Tests.Infrastructure;
using Core.DTO;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Asancode.Tests;

public sealed class AccountPasswordSecurityTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    private readonly PasswordHasher<Users> _identityHasher = new();

    [Fact]
    public void Legacy_md5_encoder_preserves_the_existing_ascii_hash_format()
    {
        Assert.Equal(
            "5F-4D-CC-3B-5A-A7-65-D6-1D-83-27-DE-B8-82-CF-99",
            PasswordEncript.EncriptPassword("password"));
    }

    [Fact]
    public async Task LoginUser_rehashes_a_valid_legacy_password_after_successful_login()
    {
        const string password = "Legacy-Password-2026!";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var user = CreateUser(PasswordEncript.EncriptPassword(password));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalHash = user.Password;

        var authenticated = scope.ServiceProvider.GetRequiredService<IUserService>().LoginUser(new LoginViewModel
        {
            Email = user.Email,
            Password = password
        });

        Assert.NotNull(authenticated);
        var persisted = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(u => u.UserId == user.UserId, TestContext.Current.CancellationToken);

        Assert.NotEqual(originalHash, persisted.Password);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            _identityHasher.VerifyHashedPassword(persisted, persisted.Password, password));
    }

    [Fact]
    public async Task LoginUser_accepts_a_current_identity_password_hash()
    {
        const string password = "Modern-Password-2026-رمز!";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var user = CreateUser("pending");
        user.Password = _identityHasher.HashPassword(user, password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var authenticated = new Userservice(dbContext).LoginUser(new LoginViewModel
        {
            Email = user.Email,
            Password = password
        });

        Assert.NotNull(authenticated);
        Assert.Equal(user.UserId, authenticated.UserId);
    }

    [Fact]
    public async Task ChangePassword_stores_a_salted_identity_password_hash()
    {
        const string oldPassword = "Old-Password-2026!";
        const string newPassword = "New-Password-2026-رمز!";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var user = CreateUser(PasswordEncript.EncriptPassword(oldPassword));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        new Userservice(dbContext).ChangePassword(user.UserName, newPassword);

        var persisted = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(u => u.UserId == user.UserId, TestContext.Current.CancellationToken);

        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            _identityHasher.VerifyHashedPassword(persisted, persisted.Password, newPassword));
        Assert.NotEqual(PasswordEncript.EncriptPassword(newPassword), persisted.Password);
    }

    [Fact]
    public async Task AddUserForAdmin_stores_a_salted_identity_password_hash()
    {
        const string password = "Admin-Created-Password-2026!";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();

        var userId = new Userservice(dbContext).AddUserForAdmin(new CreateNewUserViewModel
        {
            UserName = $"password-admin-{Guid.NewGuid():N}",
            Email = $"password-admin-{Guid.NewGuid():N}@example.test",
            Password = password,
            UserImage = null!
        });

        var persisted = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(u => u.UserId == userId, TestContext.Current.CancellationToken);

        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            _identityHasher.VerifyHashedPassword(persisted, persisted.Password, password));
    }

    [Fact]
    public async Task Failed_legacy_login_does_not_modify_the_stored_password_hash()
    {
        const string password = "Legacy-Password-2026!";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var user = CreateUser(PasswordEncript.EncriptPassword(password));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalHash = user.Password;

        var authenticated = new Userservice(dbContext).LoginUser(new LoginViewModel
        {
            Email = user.Email,
            Password = "Incorrect-Password-2026!"
        });

        Assert.Null(authenticated);
        var persisted = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(u => u.UserId == user.UserId, TestContext.Current.CancellationToken);
        Assert.Equal(originalHash, persisted.Password);
    }

    private static Users CreateUser(string passwordHash)
    {
        var id = Guid.NewGuid().ToString("N");
        return new Users
        {
            UserName = $"password-test-{id}",
            Email = $"password-test-{id}@example.test",
            Password = passwordHash,
            UserImage = "Defult.jpg",
            ActiveCode = Guid.NewGuid().ToString("N"),
            RegisterDate = DateTime.UtcNow,
            IsActive = true,
            IsDelete = false
        };
    }
}
