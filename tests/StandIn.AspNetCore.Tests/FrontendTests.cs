using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace StandIn.AspNetCore.Tests;

public class FrontendTests
{
    private const string Frontend = "https://localhost:5173";

    private static Task<TestApp> StartAsync(
        Action<StandInOptions>? extra = null,
        string? pathBase = null) =>
        TestApp.StartAsync(
            options =>
            {
                options.AddUser(TestApp.Admin());
                extra?.Invoke(options);
            },
            pathBase: pathBase);

    private static HttpRequestMessage Request(HttpMethod method, string url, string? origin = null, string? referer = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        if (referer is not null)
        {
            request.Headers.Add("Referer", referer);
        }

        return request;
    }

    // ---- FrontendBaseUrl ----

    [Fact]
    public async Task Sign_out_without_a_return_url_goes_to_the_frontend()
    {
        await using var app = await StartAsync(o => o.FrontendBaseUrl = Frontend + "/");

        var response = await app.Client.GetAsync("/stuntman/sign-out");

        Assert.Equal(Frontend + "/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Sign_out_without_a_frontend_goes_to_the_start_page()
    {
        await using var app = await StartAsync();

        var response = await app.Client.GetAsync("/stuntman/sign-out");

        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Return_url_to_the_frontend_origin_is_allowed_and_other_origins_are_not()
    {
        await using var app = await StartAsync(o => o.FrontendBaseUrl = Frontend);

        var allowed = await app.Client.GetAsync($"/stuntman/sign-in?OverrideUserId=admin&ReturnUrl={Uri.EscapeDataString(Frontend + "/dashboard")}");
        var denied = await app.Client.GetAsync("/stuntman/sign-in?OverrideUserId=admin&ReturnUrl=https%3A%2F%2Fevil.example%2Fx");

        Assert.Equal(Frontend + "/dashboard", allowed.Headers.Location?.OriginalString);
        Assert.Equal(Frontend, denied.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Referer_from_the_frontend_is_used_as_return_target()
    {
        await using var app = await StartAsync(o => o.FrontendBaseUrl = Frontend);

        var response = await app.Client.SendAsync(Request(HttpMethod.Get, "/stuntman/sign-out", referer: Frontend + "/page"));

        Assert.Equal(Frontend + "/page", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public void Frontend_base_url_must_be_an_http_url()
    {
        Assert.Throws<ArgumentException>(() => new StandInOptions { FrontendBaseUrl = "not a url" });
    }

    // ---- CORS ----

    [Fact]
    public async Task Users_endpoint_answers_allowed_origins_with_cors_headers()
    {
        await using var app = await StartAsync(o => o.FrontendBaseUrl = Frontend);

        var response = await app.Client.SendAsync(Request(HttpMethod.Get, "/stuntman/users", origin: Frontend));

        Assert.Equal(Frontend, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Other_origins_get_no_cors_headers()
    {
        await using var app = await StartAsync(o => o.FrontendBaseUrl = Frontend);

        var response = await app.Client.SendAsync(Request(HttpMethod.Get, "/stuntman/users", origin: "https://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_for_the_token_endpoint_is_answered_for_allowed_origins_only()
    {
        await using var app = await StartAsync(o =>
        {
            o.FrontendBaseUrl = Frontend;
            o.EnableTokenEndpoint();
        });

        var allowed = await app.Client.SendAsync(Request(HttpMethod.Options, "/stuntman/token", origin: Frontend));
        var denied = await app.Client.SendAsync(Request(HttpMethod.Options, "/stuntman/token", origin: "https://evil.example"));

        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        Assert.Equal(Frontend, allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("POST", allowed.Headers.GetValues("Access-Control-Allow-Methods").Single(), StringComparison.Ordinal);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Token_response_carries_cors_headers_for_the_frontend()
    {
        await using var app = await StartAsync(o =>
        {
            o.FrontendBaseUrl = Frontend;
            o.EnableTokenEndpoint();
        });
        var request = Request(HttpMethod.Post, "/stuntman/token", origin: Frontend);
        request.Content = JsonContent.Create(new { userId = "admin" });

        var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Frontend, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Server_endpoint_gets_no_cors_headers()
    {
        await using var app = await StartAsync(o =>
        {
            o.FrontendBaseUrl = Frontend;
            o.EnableServer();
        });

        var response = await app.Client.SendAsync(Request(HttpMethod.Get, "/stuntman/server", origin: Frontend));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // ---- Geaenderter Root-Pfad ----

    [Fact]
    public async Task Old_paths_redirect_with_308_and_keep_sub_path_and_query()
    {
        await using var app = await StartAsync(o => o.RootPath = "/dev-login");

        var response = await app.Client.GetAsync("/stuntman/sign-out?ReturnUrl=%2Fhome");

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.Equal("/dev-login/sign-out?ReturnUrl=%2Fhome", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Redirect_keeps_the_request_method()
    {
        await using var app = await StartAsync(o => o.RootPath = "/dev-login");

        var response = await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "admin" });

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
    }

    [Fact]
    public async Task Root_path_below_the_default_path_does_not_redirect_in_a_loop()
    {
        await using var app = await StartAsync(o => o.RootPath = "/stuntman/v2");

        var moved = await app.Client.GetAsync("/stuntman/v2/users");
        var old = await app.Client.GetAsync("/stuntman/users");

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, old.StatusCode);
    }

    [Fact]
    public async Task Redirect_never_leaves_the_application()
    {
        await using var app = await StartAsync(o => o.RootPath = string.Empty);

        var response = await app.Client.GetAsync("/stuntman//evil.example");

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.Equal("/evil.example", response.Headers.Location?.OriginalString);
    }

    // ---- PathBase ----

    [Fact]
    public async Task Redirects_stay_below_the_path_base()
    {
        await using var app = await StartAsync(pathBase: "/app");

        var response = await app.Client.GetAsync("/app/stuntman/sign-out");

        Assert.Equal("/app", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_page_links_include_the_path_base()
    {
        await using var app = await StartAsync(pathBase: "/app");

        var html = await app.Client.GetStringAsync("/app/stuntman/sign-in");

        Assert.Contains("href=\"/app/stuntman/sign-in?OverrideUserId=admin", html, StringComparison.Ordinal);
    }
}
