using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace StandIn.AspNetCore.Tests;

public class AuthenticationTests
{
    private static Task<TestApp> StartAsync(Action<StandInOptions>? extra = null, string environment = "Development") =>
        TestApp.StartAsync(
            options =>
            {
                options.AddUser(TestApp.Admin()).AddUser(TestApp.Viewer());
                extra?.Invoke(options);
            },
            environment);

    // ---- Cookie ----

    [Fact]
    public async Task Anonymous_request_is_anonymous()
    {
        await using var app = await StartAsync();

        Assert.Equal("anonymous", await app.Client.GetStringAsync("/whoami"));
    }

    [Fact]
    public async Task Sign_in_sets_a_cookie_and_redirects_to_the_local_return_url()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/stuntman/sign-in?OverrideUserId=admin&ReturnUrl=%2Fwhoami");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/whoami", response.Headers.Location?.OriginalString);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith($"{StandInDefaults.CookieName}=admin", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cookie_authenticates_the_user_with_claims()
    {
        await using var app = await StartAsync();

        var name = await (await app.Client.SendAsync(TestApp.Get("/whoami", cookie: "admin"))).Content.ReadAsStringAsync();
        var claims = await (await app.Client.SendAsync(TestApp.Get("/claims", cookie: "admin"))).Content.ReadAsStringAsync();

        Assert.Equal("Local Admin", name);
        Assert.Contains("role=Administrator", claims, StringComparison.Ordinal);
        Assert.Contains($"{StandInDefaults.UserIdClaimType}=admin", claims, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cookie_with_unknown_user_is_ignored()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(TestApp.Get("/whoami", cookie: "nobody"));

        Assert.Equal("anonymous", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sign_in_with_unknown_user_returns_404()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/stuntman/sign-in?OverrideUserId=nobody&ReturnUrl=%2F");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example/phish")]
    [InlineData("//evil.example")]
    public async Task Sign_in_does_not_redirect_to_foreign_addresses(string returnUrl)
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync($"/stuntman/sign-in?OverrideUserId=admin&ReturnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Sign_in_without_return_url_uses_the_referer()
    {
        await using var app = await StartAsync();

        var request = TestApp.Get("/stuntman/sign-in?OverrideUserId=admin");
        request.Headers.Add("Referer", "http://localhost/some/page?x=1");

        var response = await app.Client.SendAsync(request);

        Assert.Equal("/some/page?x=1", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Sign_in_without_user_shows_the_login_page()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/stuntman/sign-in");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Local Admin", html, StringComparison.Ordinal);
        Assert.Contains("OverrideUserId=viewer", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sign_out_removes_the_cookie()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/stuntman/sign-out?ReturnUrl=%2F");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Browser_navigation_to_a_protected_page_is_sent_to_sign_in()
    {
        await using var app = await StartAsync();

        var request = TestApp.Get("/secure");
        request.Headers.Add("Accept", "text/html");

        var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/stuntman/sign-in?ReturnUrl=", response.Headers.Location?.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Api_request_to_a_protected_endpoint_gets_401()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cookie_authentication_can_be_switched_off()
    {
        await using var app = await StartAsync(o => o.AllowCookieAuthentication = false);

        var signIn = await app.Client.GetAsync("/stuntman/sign-in?OverrideUserId=admin&ReturnUrl=%2F");
        var whoami = await app.Client.SendAsync(TestApp.Get("/whoami", cookie: "admin"));

        Assert.Equal(HttpStatusCode.NotFound, signIn.StatusCode);
        Assert.Equal("anonymous", await whoami.Content.ReadAsStringAsync());
    }

    // ---- Bearer ----

    [Fact]
    public async Task Valid_bearer_token_authenticates_and_adds_the_access_token_claim()
    {
        await using var app = await StartAsync();

        var name = await app.Client.SendAsync(TestApp.Get("/secure", bearer: "viewer-token"));
        var claims = await (await app.Client.SendAsync(TestApp.Get("/claims", bearer: "viewer-token"))).Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, name.StatusCode);
        Assert.Equal("Local Viewer", await name.Content.ReadAsStringAsync());
        Assert.Contains("access_token=viewer-token", claims, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_bearer_token_returns_403()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(TestApp.Get("/secure", bearer: "wrong"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("Bearer a b")]
    public async Task Malformed_bearer_header_returns_400(string header)
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(TestApp.Get("/secure", authorizationHeader: header));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Other_authorization_schemes_are_left_alone()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(TestApp.Get("/secure", authorizationHeader: "Basic dXNlcjpwYXNz"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_token_passes_through_when_enabled()
    {
        await using var app = await StartAsync(o => o.AllowBearerTokenPassthrough = true);

        var response = await app.Client.SendAsync(TestApp.Get("/secure", bearer: "some-real-jwt"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bearer_authentication_can_be_switched_off()
    {
        await using var app = await StartAsync(o => o.AllowBearerTokenAuthentication = false);

        var response = await app.Client.SendAsync(TestApp.Get("/secure", bearer: "viewer-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task After_bearer_hook_is_called_with_the_user()
    {
        string? seen = null;
        await using var app = await StartAsync(o => o.AfterBearerValidateIdentity = (_, user) => seen = user.Id);

        await app.Client.SendAsync(TestApp.Get("/whoami", bearer: "admin-token"));

        Assert.Equal("admin", seen);
    }

    // ---- Umgebung ----

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Outside_development_nothing_is_registered(string environment)
    {
        await using var app = await StartAsync(environment: environment);

        var signIn = await app.Client.GetAsync("/stuntman/sign-in?OverrideUserId=admin&ReturnUrl=%2F");
        var whoami = await app.Client.SendAsync(TestApp.Get("/whoami", cookie: "admin", bearer: "admin-token"));

        Assert.Equal(HttpStatusCode.NotFound, signIn.StatusCode);
        Assert.Equal("anonymous", await whoami.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Extra_environments_can_be_allowed_explicitly()
    {
        var options = new Action<StandInOptions>(o => o.AddUser(TestApp.Admin()));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Staging" });
        builder.Services.AddAuthentication().AddStandIn(builder.Environment, options, new[] { "Staging" });

        await using var app = builder.Build();

        Assert.NotNull(app.Services.GetService(typeof(StandInMarker)));
    }

    [Fact]
    public void Production_can_never_be_allowed()
    {
        var builder = WebApplication.CreateBuilder();

        Assert.Throws<ArgumentException>(() =>
            builder.Services.AddAuthentication().AddStandIn(builder.Environment, null, new[] { "production" }));
    }

    // ---- Server und Benutzerliste ----

    [Fact]
    public async Task Server_endpoint_is_off_by_default()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/stuntman/server");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Server_endpoint_returns_users_with_tokens_in_stuntman_format()
    {
        await using var app = await StartAsync(o => o.EnableServer());

        var json = await app.Client.GetStringAsync("/stuntman/server");
        using var document = JsonDocument.Parse(json);

        var admin = document.RootElement.GetProperty("Users").EnumerateArray()
            .Single(u => u.GetProperty("Id").GetString() == "admin");

        Assert.Equal("admin-token", admin.GetProperty("AccessToken").GetString());
        Assert.Equal("Administrator", admin.GetProperty("Claims")[0].GetProperty("Value").GetString());
    }

    [Fact]
    public async Task Users_endpoint_never_contains_tokens()
    {
        await using var app = await StartAsync();

        var json = await app.Client.GetStringAsync("/stuntman/users");

        Assert.Contains("Local Admin", json, StringComparison.Ordinal);
        Assert.DoesNotContain("admin-token", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_path_can_be_changed()
    {
        await using var app = await StartAsync(o => o.RootPath = "/dev-login");

        var moved = await app.Client.GetAsync("/dev-login/sign-in?OverrideUserId=admin&ReturnUrl=%2F");
        var old = await app.Client.GetAsync("/stuntman/sign-in?OverrideUserId=admin&ReturnUrl=%2F");

        Assert.Equal(HttpStatusCode.Redirect, moved.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, old.StatusCode);
    }
}
