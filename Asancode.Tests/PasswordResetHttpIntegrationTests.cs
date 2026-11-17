using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Asancode.Tests.Infrastructure;
using Core.Security;
using Core.Services.Interfaces;
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Asancode.Tests;

public sealed class PasswordResetHttpIntegrationTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    [Fact]
    public async Task ForgotPassword_get_renders_form_with_antiforgery_token()
    {
        var emailSender = GetEmailSender();
        emailSender.Clear();
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/ForgotPassword",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("name=\"Email\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(ExtractAntiforgeryToken(html)));
        Assert.Empty(emailSender.Messages);
    }

    [Fact]
    public async Task ForgotPassword_invalid_email_is_rejected_without_sending_email()
    {
        var emailSender = GetEmailSender();
        emailSender.Clear();
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client, "/ForgotPassword");

        using var response = await client.PostAsync(
            "/ForgotPassword",
            CreateForm(
                ("Email", "not-an-email"),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("ایمیل وارد شده معتبر نمی باشد", html);
        Assert.DoesNotContain("اگر حسابی با این ایمیل وجود داشته باشد", html);
        Assert.Empty(emailSender.Messages);
    }

    [Fact]
    public async Task ForgotPassword_unknown_address_returns_generic_response_and_sends_no_email()
    {
        var emailSender = GetEmailSender();
        emailSender.Clear();
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client, "/ForgotPassword");

        using var response = await client.PostAsync(
            "/ForgotPassword",
            CreateForm(
                ("Email", "unknown-account@example.test"),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("اگر حسابی با این ایمیل وجود داشته باشد", html);
        Assert.Empty(emailSender.Messages);
    }

    [Fact]
    public async Task ForgotPassword_known_address_sends_one_email_with_reset_link_and_stores_only_hash()
    {
        var emailSender = GetEmailSender();
        emailSender.Clear();
        var user = await SeedUserAsync();
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client, "/ForgotPassword");

        using var response = await client.PostAsync(
            "/ForgotPassword",
            CreateForm(
                ("Email", user.Email),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("اگر حسابی با این ایمیل وجود داشته باشد", html);

        var email = Assert.Single(emailSender.Messages);
        Assert.Equal(user.Email, email.To);
        Assert.Equal("بازیابی کلمه عبور", email.Subject);
        Assert.Contains(user.UserName, email.Body);
        Assert.Contains("6 ساعت", email.Body);

        var linkMatch = Regex.Match(
            email.Body,
            "href=\"https://www\\.asancode\\.com/Account/ResetPassword/(?<token>[A-Za-z0-9_-]{43})\"",
            RegexOptions.IgnoreCase);
        Assert.True(linkMatch.Success, "The password reset email must contain a URL-safe reset link.");
        var token = linkMatch.Groups["token"].Value;

        Assert.Contains("ResetPassword/" + token, email.Body);
        Assert.True(
            fixture.Factory.Services.GetRequiredService<IUserService>()
                .IsPasswordResetTokenValid(token));

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);

        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            persisted.PasswordResetTokenHash);
        Assert.NotEqual(token, persisted.PasswordResetTokenHash);
        Assert.NotNull(persisted.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public async Task ForgotPassword_post_without_antiforgery_token_is_rejected_before_email_or_token_creation()
    {
        var emailSender = GetEmailSender();
        emailSender.Clear();
        var user = await SeedUserAsync();
        using var client = CreateClient();
        _ = await GetAntiforgeryTokenAsync(client, "/ForgotPassword");

        using var response = await client.PostAsync(
            "/ForgotPassword",
            CreateForm(("Email", user.Email)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(emailSender.Messages);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);
        Assert.Null(persisted.PasswordResetTokenHash);
        Assert.Null(persisted.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public async Task ResetPassword_get_renders_valid_token_and_rejects_invalid_token()
    {
        var user = await SeedUserAsync();
        var userService = fixture.Factory.Services.GetRequiredService<IUserService>();
        var token = userService.CreatePasswordResetToken(user.UserId);
        using var client = CreateClient();

        using var invalidResponse = await client.GetAsync(
            "/ResetPassword/xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, invalidResponse.StatusCode);

        using var response = await client.GetAsync(
            "/ResetPassword/" + token,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("name=\"ResetToken\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value=\"" + token + "\"", html, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(ExtractAntiforgeryToken(html)));
    }

    [Fact]
    public async Task ResetPassword_post_without_antiforgery_token_is_rejected_and_token_remains_valid()
    {
        var user = await SeedUserAsync();
        var userService = fixture.Factory.Services.GetRequiredService<IUserService>();
        var token = userService.CreatePasswordResetToken(user.UserId);
        using var client = CreateClient();
        _ = await GetAntiforgeryTokenAsync(client, "/ResetPassword/" + token);

        using var response = await client.PostAsync(
            "/ResetPassword",
            CreateForm(
                ("ResetToken", token),
                ("Password", "Replacement-Password-2026!"),
                ("RePassword", "Replacement-Password-2026!")),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(userService.IsPasswordResetTokenValid(token));
    }

    [Fact]
    public async Task ResetPassword_post_validates_confirmation_then_changes_password_and_consumes_token()
    {
        var user = await SeedUserAsync();
        var userService = fixture.Factory.Services.GetRequiredService<IUserService>();
        var token = userService.CreatePasswordResetToken(user.UserId);
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client, "/ResetPassword/" + token);

        using var invalidResponse = await client.PostAsync(
            "/ResetPassword",
            CreateForm(
                ("ResetToken", token),
                ("Password", "Replacement-Password-2026!"),
                ("RePassword", "Does-Not-Match-2026!"),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, invalidResponse.StatusCode);
        var invalidHtml = await invalidResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("تکرار کلمه عبور درست نمی باشد", invalidHtml);
        Assert.True(userService.IsPasswordResetTokenValid(token));

        using var validResponse = await client.PostAsync(
            "/ResetPassword",
            CreateForm(
                ("ResetToken", token),
                ("Password", "Replacement-Password-2026!"),
                ("RePassword", "Replacement-Password-2026!"),
                ("__RequestVerificationToken", antiforgeryToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        var validHtml = await validResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("رمز عبور شما با موفقیت تغییر کرد", validHtml);
        Assert.False(userService.IsPasswordResetTokenValid(token));

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var persisted = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.UserId == user.UserId, TestContext.Current.CancellationToken);
        var hasher = scope.ServiceProvider.GetRequiredService<UserPasswordHasher>();
        Assert.NotEqual(
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed,
            hasher.VerifyPassword(persisted, persisted.Password, "Replacement-Password-2026!"));
    }

    private RecordingEmailSender GetEmailSender() =>
        fixture.Factory.Services.GetRequiredService<RecordingEmailSender>();

    private HttpClient CreateClient()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static FormUrlEncodedContent CreateForm(params (string Key, string Value)[] values) =>
        new(values.ToDictionary(item => item.Key, item => item.Value));

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return ExtractAntiforgeryToken(html);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.IgnoreCase);

        Assert.True(match.Success, "The rendered POST form must contain an antiforgery request token.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private async Task<Users> SeedUserAsync()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<UserPasswordHasher>();
        var id = Guid.NewGuid().ToString("N");
        var user = new Users
        {
            UserName = $"password-http-{id}",
            Email = $"password-http-{id}@example.test",
            Password = "pending",
            UserImage = "Defult.jpg",
            ActiveCode = Guid.NewGuid().ToString("N"),
            RegisterDate = DateTime.UtcNow,
            IsActive = true,
            IsDelete = false
        };

        user.Password = hasher.HashPassword(user, "Initial-Password-2026!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }
}
