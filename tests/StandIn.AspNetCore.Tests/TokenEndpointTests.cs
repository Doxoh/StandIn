using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StandIn.AspNetCore.Tests;

public class TokenEndpointTests
{
    private static Task<TestApp> StartAsync(bool enabled = true) =>
        TestApp.StartAsync(options =>
        {
            options.AddUser(TestApp.Admin()).AddUser(new StandInUser("guest", "Guest User"));

            if (enabled)
            {
                options.EnableTokenEndpoint();
            }
        });

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Token_endpoint_is_off_by_default()
    {
        await using var app = await StartAsync(enabled: false);

        var response = await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "admin" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Token_endpoint_returns_the_configured_token_and_user_data()
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "admin" });
        var json = await ReadAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("admin-token", json.GetProperty("access_token").GetString());
        Assert.Equal("Bearer", json.GetProperty("token_type").GetString());
        Assert.Equal("admin", json.GetProperty("user_id").GetString());
        Assert.Equal("Administrator", json.GetProperty("claims")[0].GetProperty("value").GetString());
        Assert.Equal("name", json.GetProperty("name_claim_type").GetString());
        Assert.Equal("role", json.GetProperty("role_claim_type").GetString());
    }

    [Fact]
    public async Task Token_response_carries_the_claims_the_server_builds()
    {
        await using var app = await StartAsync();

        var json = await ReadAsync(await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "admin" }));
        var claims = json.GetProperty("claims").EnumerateArray()
            .Select(c => (Type: c.GetProperty("type").GetString(), Value: c.GetProperty("value").GetString()))
            .ToList();

        Assert.Contains(("name", "Local Admin"), claims);
        Assert.Contains(("standin_user_id", "admin"), claims);
        Assert.DoesNotContain(claims, c => c.Type == "access_token");
    }

    [Fact]
    public async Task Users_without_a_token_get_a_stable_random_token_that_works_as_bearer()
    {
        await using var app = await StartAsync();

        var first = await ReadAsync(await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "guest" }));
        var second = await ReadAsync(await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "guest" }));
        var token = first.GetProperty("access_token").GetString();

        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(token, second.GetProperty("access_token").GetString());

        var response = await app.Client.SendAsync(TestApp.Get("/whoami", bearer: token));
        Assert.Equal("Guest User", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_user_returns_404()
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "nobody" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{ \"userId\": \"\" }")]
    [InlineData("not json")]
    public async Task Missing_or_broken_body_returns_400(string body)
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsync(
            "/stuntman/token",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Only_json_is_accepted()
    {
        await using var app = await StartAsync();

        var response = await app.Client.PostAsync(
            "/stuntman/token",
            new StringContent("userId=admin", Encoding.UTF8, "application/x-www-form-urlencoded"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Token_endpoint_does_not_exist_outside_development()
    {
        await using var app = await TestApp.StartAsync(
            options => options.AddUser(TestApp.Admin()).EnableTokenEndpoint(),
            "Production");

        var response = await app.Client.PostAsJsonAsync("/stuntman/token", new { userId = "admin" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
