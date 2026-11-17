using System;
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
using DataLayer.Context;
using DataLayer.Entities.User;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Asancode.Tests;

public sealed class AuthenticationHttpIntegrationTests(MediaSqlServerFixture fixture)
    : IClassFixture<MediaSqlServerFixture>
{
    [Fact]
    public async Task Login_get_renders_post_form_with_antiforgery_token()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            "/Login",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("name=\"Email\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"RememberMe\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(ExtractAntiforgeryToken(html)));
    }

    [Fact]
    public async Task Register_post_without_antiforgery_token_is_rejected_before_creating_user()
    {
        var id = Guid.NewGuid().ToString("N");
        var email = $"register-{id}@example.test";
        using var client = CreateClient();

        using var response = await client.PostAsync(
            "/Register",
            CreateForm(
                ("UserName", $"register-{id}"),
                ("Email", email),
                ("Password", "Register-Password-2026!"),
                ("RePassword", "Register-Password-2026!")),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        Assert.False(await db.Users.AnyAsync(
            user => user.Email == email,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_post_with_valid_antiforgery_token_creates_inactive_local_account_and_sends_activation_email()
    {
        var emailSender = fixture.Factory.Services.GetRequiredService<RecordingEmailSender>();
        emailSender.Clear();
        var id = Guid.NewGuid().ToString("N");
        var email = $"register-{id}@example.test";
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Register");

        using var response = await client.PostAsync(
            "/Register",
            CreateForm(
                ("UserName", $"register-{id}"),
                ("Email", email),
                ("Password", "Register-Password-2026!"),
                ("RePassword", "Register-Password-2026!"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var activationEmail = Assert.Single(emailSender.Messages);
        Assert.Equal(email, activationEmail.To);
        Assert.Equal("ایمیل فعال سازی", activationEmail.Subject);
        Assert.Contains($"/Account/ActiveCode/", activationEmail.Body);
        Assert.Contains("ثبت نام شما با موفقیت انجام گرفت", html);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var user = await db.Users.SingleAsync(
            item => item.Email == activationEmail.To,
            TestContext.Current.CancellationToken);
        Assert.False(user.IsActive);
        Assert.False(user.IsDelete);
        Assert.NotEqual("Register-Password-2026!", user.Password);
        Assert.False(
            response.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.StartsWith(
                ".AsanCode.Authentication=",
                StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Login_post_without_antiforgery_token_is_rejected()
    {
        using var client = CreateClient();
        var user = await SeedUserAsync();

        using var response = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Initial-Password-2026!")),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(HasApplicationCookie(response));
    }

    [Fact]
    public async Task Login_with_valid_credentials_issues_real_secure_cookie_and_allows_local_redirect()
    {
        var user = await SeedUserAsync();
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Login");

        using var response = await client.PostAsync(
            "/login?ReturnUrl=%2FUserPanel",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Initial-Password-2026!"),
                ("RememberMe", "false"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/UserPanel", response.Headers.Location?.OriginalString);
        AssertApplicationCookieIsSecure(response, isPersistent: false);

        using var authenticatedResponse = await client.GetAsync(
            "/Login",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
        var html = await authenticatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("شما با موفقیت وارد سایت شدید", html);
    }

    [Fact]
    public async Task Login_rejects_external_and_protocol_relative_return_urls()
    {
        var user = await SeedUserAsync();
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Login");

        using var externalResponse = await client.PostAsync(
            "/login?ReturnUrl=https%3A%2F%2Fevil.example%2Fphish",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Initial-Password-2026!"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, externalResponse.StatusCode);
        Assert.Equal("/", externalResponse.Headers.Location?.OriginalString);
        Assert.DoesNotContain("evil.example", externalResponse.Headers.Location?.OriginalString ?? string.Empty);

        using var protocolRelativeResponse = await client.PostAsync(
            "/login?ReturnUrl=%2F%2Fevil.example%2Fphish",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Initial-Password-2026!"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, protocolRelativeResponse.StatusCode);
        Assert.Equal("/", protocolRelativeResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_does_not_issue_cookie_for_wrong_password_or_inactive_account()
    {
        var activeUser = await SeedUserAsync();
        var inactiveUser = await SeedUserAsync(isActive: false);
        var deletedUser = await SeedUserAsync(isDeleted: true);
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Login");

        using var wrongPasswordResponse = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", activeUser.Email),
                ("Password", "Wrong-Password-2026!"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, wrongPasswordResponse.StatusCode);
        Assert.False(HasApplicationCookie(wrongPasswordResponse));
        var wrongPasswordHtml = await wrongPasswordResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("اطلاعات ورود نامعتبر است یا حساب کاربری فعال نیست", wrongPasswordHtml);

        using var inactiveResponse = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", inactiveUser.Email),
                ("Password", "Initial-Password-2026!"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, inactiveResponse.StatusCode);
        Assert.False(HasApplicationCookie(inactiveResponse));
        var inactiveHtml = await inactiveResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("اطلاعات ورود نامعتبر است یا حساب کاربری فعال نیست", inactiveHtml);

        using var deletedResponse = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", deletedUser.Email),
                ("Password", "Initial-Password-2026!"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, deletedResponse.StatusCode);
        Assert.False(HasApplicationCookie(deletedResponse));
        var deletedHtml = await deletedResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("اطلاعات ورود نامعتبر است یا حساب کاربری فعال نیست", deletedHtml);
    }

    [Fact]
    public async Task Remember_me_issues_a_persistent_application_cookie()
    {
        var user = await SeedUserAsync();
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/Login");

        using var response = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Initial-Password-2026!"),
                ("RememberMe", "true"),
                ("__RequestVerificationToken", token)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertApplicationCookieIsSecure(response, isPersistent: true);
    }

    [Fact]
    public async Task Logout_is_post_only_requires_antiforgery_and_clears_the_real_cookie()
    {
        var user = await SeedUserAsync();
        using var client = CreateClient();
        var loginToken = await GetAntiforgeryTokenAsync(client, "/Login");

        using var loginResponse = await client.PostAsync(
            "/login",
            CreateForm(
                ("Email", user.Email),
                ("Password", "Initial-Password-2026!"),
                ("__RequestVerificationToken", loginToken)),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        using var getLogoutResponse = await client.GetAsync(
            "/Logout",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, getLogoutResponse.StatusCode);

        using var invalidLogoutResponse = await client.PostAsync(
            "/Logout",
            new StringContent(string.Empty),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidLogoutResponse.StatusCode);

        using var stillAuthenticated = await client.GetAsync(
            "/Login",
            TestContext.Current.CancellationToken);
        var stillAuthenticatedHtml = await stillAuthenticated.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.Contains("شما با موفقیت وارد سایت شدید", stillAuthenticatedHtml);

        var logoutToken = await GetAntiforgeryTokenAsync(client, "/Login");
        using var logoutResponse = await client.PostAsync(
            "/Logout",
            CreateForm(("__RequestVerificationToken", logoutToken)),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.True(HasExpiredApplicationCookie(logoutResponse));

        using var anonymousResponse = await client.GetAsync(
            "/Login",
            TestContext.Current.CancellationToken);
        var anonymousHtml = await anonymousResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain("شما با موفقیت وارد سایت شدید", anonymousHtml);
    }

    [Fact]
    public async Task Unknown_external_provider_entry_and_callback_do_not_create_a_local_session()
    {
        using var client = CreateClient();

        using var providerResponse = await client.GetAsync(
            "/provider/Google",
            TestContext.Current.CancellationToken);
        using var callbackResponse = await client.GetAsync(
            "/external-login",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, providerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, callbackResponse.StatusCode);
        Assert.False(HasApplicationCookie(providerResponse));
        Assert.False(HasApplicationCookie(callbackResponse));
    }

    [Fact]
    public void Application_cookie_has_explicit_secure_settings_and_bounded_lifetime()
    {
        var options = fixture.Factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(".AsanCode.Authentication", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(Microsoft.AspNetCore.Http.SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.True(options.SlidingExpiration);
        Assert.Equal(TimeSpan.FromDays(14), options.ExpireTimeSpan);
    }

    private HttpClient CreateClient()
    {
        var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
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

    private static bool HasApplicationCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(cookie => cookie.StartsWith(
            ".AsanCode.Authentication=",
            StringComparison.OrdinalIgnoreCase));

    private static bool HasExpiredApplicationCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(cookie =>
            cookie.StartsWith(".AsanCode.Authentication=", StringComparison.OrdinalIgnoreCase)
            && (cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase)
                || cookie.Contains("max-age=0", StringComparison.OrdinalIgnoreCase)));

    private static void AssertApplicationCookieIsSecure(
        HttpResponseMessage response,
        bool isPersistent)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookie = Assert.Single(
            cookies!,
            value => value.StartsWith(
                ".AsanCode.Authentication=",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            isPersistent,
            cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Users> SeedUserAsync(bool isActive = true, bool isDeleted = false)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AsanwebContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<UserPasswordHasher>();
        var id = Guid.NewGuid().ToString("N");
        var user = new Users
        {
            UserName = $"auth-http-{id}",
            Email = $"auth-http-{id}@example.test",
            Password = "pending",
            UserImage = "Defult.jpg",
            ActiveCode = Guid.NewGuid().ToString("N"),
            RegisterDate = DateTime.UtcNow,
            IsActive = isActive,
            IsDelete = isDeleted
        };

        user.Password = hasher.HashPassword(user, "Initial-Password-2026!");
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }
}
