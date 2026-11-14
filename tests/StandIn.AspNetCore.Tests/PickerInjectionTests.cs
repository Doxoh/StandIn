using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace StandIn.AspNetCore.Tests;

public class PickerInjectionTests
{
    private static Task<TestApp> StartAsync(string environment = "Development") =>
        TestApp.StartAsync(
            options => options.AddUser(TestApp.Admin()).AddUser(TestApp.Viewer()),
            environment);

    private static HttpRequestMessage Page(string url, string? cookie = null)
    {
        var request = TestApp.Get(url, cookie);
        request.Headers.Accept.ParseAdd("text/html");
        return request;
    }

    [Fact]
    public async Task Picker_is_inserted_before_the_closing_body_tag()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(Page("/page"));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("OverrideUserId=admin", html, StringComparison.Ordinal);
        Assert.True(
            html.IndexOf("class=\"standin ", StringComparison.Ordinal) < html.IndexOf("</body>", StringComparison.Ordinal));
        Assert.StartsWith("<html><body><h1>Hi</h1>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Picker_shows_the_signed_in_user()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(Page("/page", cookie: "viewer"));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("StandIn: Local Viewer", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Content_length_matches_the_changed_body()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(Page("/page"));
        var bytes = await response.Content.ReadAsByteArrayAsync();

        var length = response.Content.Headers.ContentLength;
        Assert.True(length is null || length == bytes.Length);
    }

    [Fact]
    public async Task Requests_that_do_not_expect_html_are_left_alone()
    {
        await using var app = await StartAsync();

        var html = await app.Client.GetStringAsync("/page");

        Assert.Equal("<html><body><h1>Hi</h1></body></html>", html);
    }

    [Fact]
    public async Task Html_without_a_body_tag_is_left_alone()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(Page("/fragment"));

        Assert.Equal("<p>no body tag</p>", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_page_does_not_get_a_second_picker()
    {
        await using var app = await StartAsync();

        var response = await app.Client.SendAsync(Page("/stuntman/sign-in"));
        var html = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("class=\"standin ", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Picker_is_not_inserted_outside_development()
    {
        await using var app = await StartAsync("Production");

        var response = await app.Client.SendAsync(Page("/page"));

        Assert.Equal("<html><body><h1>Hi</h1></body></html>", await response.Content.ReadAsStringAsync());
    }
}
