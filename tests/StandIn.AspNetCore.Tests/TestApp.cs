using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StandIn.AspNetCore.Tests;

/// <summary>Startet eine kleine Anwendung im Speicher (TestServer) mit StandIn.</summary>
internal sealed class TestApp : IAsyncDisposable
{
    private TestApp(WebApplication app)
    {
        App = app;
        Client = app.GetTestClient();
    }

    public WebApplication App { get; }

    public HttpClient Client { get; }

    public static async Task<TestApp> StartAsync(
        Action<StandInOptions> configure,
        string environment = "Development")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();

        builder.Services.AddAuthentication().AddStandIn(builder.Environment, configure);
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStandIn();

        // Gibt den Benutzernamen zurueck, oder "anonymous".
        app.MapGet("/whoami", (ClaimsPrincipal user) => user.Identity?.Name ?? "anonymous");
        app.MapGet("/secure", (ClaimsPrincipal user) => user.Identity?.Name ?? "?").RequireAuthorization();
        app.MapGet("/claims", (ClaimsPrincipal user) =>
            string.Join(";", user.Claims.Select(c => $"{c.Type}={c.Value}")));

        await app.StartAsync();
        return new TestApp(app);
    }

    public static StandInUser Admin() =>
        new StandInUser("admin", "Local Admin").AddRole("Administrator").SetAccessToken("admin-token");

    public static StandInUser Viewer() =>
        new StandInUser("viewer", "Local Viewer").AddRole("Viewer").SetAccessToken("viewer-token");

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
    }

    public static HttpRequestMessage Get(string url, string? cookie = null, string? bearer = null, string? authorizationHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"{StandInDefaults.CookieName}={cookie}");
        }

        if (bearer is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearer}");
        }

        if (authorizationHeader is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        return request;
    }
}
