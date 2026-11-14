using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using StandIn.Client;
using StandIn.Samples.MinimalApi;
using StandIn.Testing;

namespace StandIn.Samples.Tests;

public class MinimalApiSampleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MinimalApiSampleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static HttpRequestMessage Get(string url, string? bearer = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (bearer is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearer}");
        }

        return request;
    }

    [Fact]
    public async Task Home_is_public()
    {
        var response = await _factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_returns_401_without_a_token()
    {
        var response = await _factory.CreateClient().GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bearer_token_signs_in_the_user()
    {
        var response = await _factory.CreateClient().SendAsync(Get("/me", "dev-admin-token"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Local Admin", json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Roles_decide_access_to_the_admin_endpoint()
    {
        var client = _factory.CreateClient();

        var admin = await client.SendAsync(Get("/admin", "dev-admin-token"));
        var viewer = await client.SendAsync(Get("/admin", "dev-viewer-token"));

        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
    }

    [Fact]
    public async Task Unknown_token_returns_403()
    {
        var response = await _factory.CreateClient().SendAsync(Get("/me", "nobody"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Token_endpoint_hands_out_the_token_of_a_user()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/stuntman/token", new { userId = "viewer" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("dev-viewer-token", json.GetProperty("access_token").GetString());
    }

    [Fact]
    public async Task Testing_helper_sends_requests_as_a_user()
    {
        var client = _factory.CreateStandInClient("admin");

        var json = await client.GetFromJsonAsync<JsonElement>("/me");

        Assert.Equal("Local Admin", json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Client_package_signs_in_through_the_token_endpoint()
    {
        var session = new StandInSession(_factory.CreateClient(), new StandInClientOptions { UserId = "viewer" });
        using var api = new HttpClient(new StandInTokenHandler(session) { InnerHandler = _factory.Server.CreateHandler() })
        {
            BaseAddress = _factory.Server.BaseAddress,
        };

        var json = await api.GetFromJsonAsync<JsonElement>("/me");

        Assert.Equal("Local Viewer", json.GetProperty("name").GetString());
    }
}
