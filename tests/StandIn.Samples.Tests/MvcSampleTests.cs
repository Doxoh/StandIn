using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using StandIn.Samples.Mvc;

namespace StandIn.Samples.Tests;

public class MvcSampleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MvcSampleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>Like a browser: expects HTML, does not follow redirects on its own, keeps cookies.</summary>
    private static HttpClient Browser(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        return client;
    }

    [Fact]
    public async Task Home_page_contains_the_user_picker()
    {
        var html = await Browser(_factory).GetStringAsync("/");

        Assert.Contains("OverrideUserId=admin", html, StringComparison.Ordinal);
        Assert.Contains("Signed in as: nobody", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Protected_page_sends_an_anonymous_browser_to_the_sign_in_page()
    {
        var response = await Browser(_factory).GetAsync("/Home/Secret");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/stuntman/sign-in?ReturnUrl=%2FHome%2FSecret", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Picking_a_user_opens_the_protected_page()
    {
        var browser = Browser(_factory);

        var signIn = await browser.GetAsync("/stuntman/sign-in?OverrideUserId=admin&ReturnUrl=%2FHome%2FSecret");
        var page = await browser.GetAsync("/Home/Secret");
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal("/Home/Secret", signIn.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Signed in as: Local Admin", html, StringComparison.Ordinal);
        Assert.Contains("Roles: Administrator", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nothing_is_active_outside_development()
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var browser = Browser(factory);

        var signIn = await browser.GetAsync("/stuntman/sign-in");
        var html = await browser.GetStringAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, signIn.StatusCode);
        Assert.DoesNotContain("OverrideUserId", html, StringComparison.Ordinal);
    }
}
