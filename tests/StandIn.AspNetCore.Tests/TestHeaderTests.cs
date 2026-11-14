using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using StandIn.Testing;

namespace StandIn.AspNetCore.Tests;

public class TestHeaderTests
{
    private static Task<TestApp> StartAsync(Action<StandInOptions>? extra = null, bool allowHeader = true) =>
        TestApp.StartAsync(
            options =>
            {
                options.AddUser(TestApp.Admin()).AddUser(TestApp.Viewer());
                options.AllowTestHeader = allowHeader;
                extra?.Invoke(options);
            });

    private static HttpRequestMessage WithHeader(string url, string userId)
    {
        var request = TestApp.Get(url);
        request.Headers.Add(StandInDefaults.TestHeaderName, userId);
        return request;
    }

    [Fact]
    public void Test_header_is_off_by_default()
    {
        Assert.False(new StandInOptions().AllowTestHeader);
    }

    [Fact]
    public async Task Header_is_ignored_while_it_is_not_allowed()
    {
        await using var app = await StartAsync(allowHeader: false);

        var response = await app.Client.SendAsync(WithHeader("/whoami", "admin"));

        Assert.Equal("anonymous", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Header_signs_in_the_named_user()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(WithHeader("/whoami", "viewer"));

        Assert.Equal("Local Viewer", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Header_with_unknown_user_returns_403()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(WithHeader("/secure", "nobody"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Bearer_token_wins_over_the_header()
    {
        await using var app = await StartAsync();
        var request = TestApp.Get("/whoami", bearer: "viewer-token");
        request.Headers.Add(StandInDefaults.TestHeaderName, "admin");

        var response = await app.Client.SendAsync(request);

        Assert.Equal("Local Viewer", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Testing_helpers_enable_the_header_and_send_it()
    {
        await using var app = await TestApp.StartAsync(
            options => options.AddUser(TestApp.Admin()).AddUser(TestApp.Viewer()),
            configureServices: services => services.AllowStandInTestHeader());

        var name = await app.Client.AsStandInUser("admin").GetStringAsync("/whoami");

        Assert.Equal("Local Admin", name);
    }

    [Fact]
    public async Task AsStandInUser_replaces_the_previous_user()
    {
        await using var app = await TestApp.StartAsync(
            options => options.AddUser(TestApp.Admin()).AddUser(TestApp.Viewer()),
            configureServices: services => services.AllowStandInTestHeader());

        var name = await app.Client.AsStandInUser("admin").AsStandInUser("viewer").GetStringAsync("/whoami");

        Assert.Equal("Local Viewer", name);
    }
}
