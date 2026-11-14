using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StandIn.Blazor;
using StandIn.Client;

namespace StandIn.AspNetCore.Tests;

public class BlazorTests
{
    private static Task<TestApp> StartAsync() =>
        TestApp.StartAsync(options =>
        {
            options.AddUser(TestApp.Admin().SetDescription("Voller Zugriff"));
            options.AddUser(new StandInUser("ops", "Ops User", "given_name", "groups").AddRole("Ops"));
            options.EnableTokenEndpoint();
        });

    private static StandInSession Session(TestApp app, string? userId = null) =>
        new(app.Client, new StandInClientOptions { UserId = userId });

    [Fact]
    public async Task Provider_is_anonymous_without_a_user()
    {
        await using var app = await StartAsync();
        using var provider = new StandInAuthenticationStateProvider(Session(app));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task Provider_builds_the_principal_of_the_signed_in_user()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        using var provider = new StandInAuthenticationStateProvider(session);
        await session.SignInAsync("admin");

        var user = (await provider.GetAuthenticationStateAsync()).User;

        Assert.True(user.Identity?.IsAuthenticated);
        Assert.Equal("Local Admin", user.Identity?.Name);
        Assert.True(user.IsInRole("Administrator"));
    }

    [Fact]
    public async Task Provider_respects_custom_claim_types()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        using var provider = new StandInAuthenticationStateProvider(session);
        await session.SignInAsync("ops");

        var user = (await provider.GetAuthenticationStateAsync()).User;

        Assert.Equal("Ops User", user.Identity?.Name);
        Assert.True(user.IsInRole("Ops"));
    }

    [Fact]
    public async Task Provider_signs_in_the_start_user_on_first_use()
    {
        await using var app = await StartAsync();
        using var provider = new StandInAuthenticationStateProvider(Session(app, userId: "admin"));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("Local Admin", state.User.Identity?.Name);
    }

    [Fact]
    public async Task Provider_notifies_when_the_user_changes()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        using var provider = new StandInAuthenticationStateProvider(session);
        Task<AuthenticationState>? notified = null;
        provider.AuthenticationStateChanged += task => notified = task;

        await session.SignInAsync("admin");

        Assert.NotNull(notified);
        Assert.Equal("Local Admin", (await notified!).User.Identity?.Name);
    }

    [Fact]
    public async Task Picker_lists_the_users_and_marks_the_signed_in_one()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        await session.SignInAsync("admin");
        var services = new ServiceCollection().AddLogging().AddSingleton(session).BuildServiceProvider();

        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<StandInPicker>();
            await root.QuiescenceTask;
            return root.ToHtmlString();
        });

        Assert.Contains("Local Admin", html, StringComparison.Ordinal);
        Assert.Contains("Ops User", html, StringComparison.Ordinal);
        Assert.Contains("Voller Zugriff", html, StringComparison.Ordinal);
        Assert.Matches(new Regex("<option[^>]*value=\"admin\"[^>]*selected"), html);
        Assert.DoesNotMatch(new Regex("<option[^>]*value=\"ops\"[^>]*selected"), html);
    }

    [Fact]
    public async Task Registration_provides_session_and_state_provider()
    {
        await using var app = await StartAsync();
        var services = new ServiceCollection();
        services.AddStandInBlazor(options => options.ServerAddress = new Uri("http://localhost/"));
        services.AddHttpClient("StandIn").ConfigurePrimaryHttpMessageHandler(() => app.App.GetTestServer().CreateHandler());
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<StandInSession>().SignInAsync("admin");
        var state = await provider.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync();

        Assert.Equal("Local Admin", state.User.Identity?.Name);
    }

    [Fact]
    public void Registration_requires_the_server_address()
    {
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddStandInBlazor(_ => { }));
    }
}
