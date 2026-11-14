using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StandIn.Client;

namespace StandIn.AspNetCore.Tests;

public class ClientTests
{
    private static Task<TestApp> StartAsync() =>
        TestApp.StartAsync(options =>
        {
            options.AddUser(TestApp.Admin().SetDescription("Voller Zugriff"));
            options.AddUser(new StandInUser("guest", "Guest User"));
            options.EnableTokenEndpoint();
        });

    private static StandInSession Session(TestApp app, string? userId = null) =>
        new(app.Client, new StandInClientOptions { UserId = userId });

    /// <summary>A client that sends to the test application through the token handler.</summary>
    private static HttpClient Api(TestApp app, StandInSession session) =>
        new(new StandInTokenHandler(session) { InnerHandler = app.App.GetTestServer().CreateHandler() })
        {
            BaseAddress = new Uri("http://localhost/"),
        };

    [Fact]
    public async Task Users_are_listed_without_tokens()
    {
        await using var app = await StartAsync();

        var users = await Session(app).GetUsersAsync();

        Assert.Equal(new[] { "admin", "guest" }, users.Select(u => u.Id).ToArray());
        Assert.Equal("Voller Zugriff", users[0].Description);
        Assert.Equal("Guest User", users[1].Name);
    }

    [Fact]
    public async Task Sign_in_stores_token_and_claims_and_raises_changed()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        var raised = 0;
        session.Changed += () => raised++;

        await session.SignInAsync("admin");

        Assert.Equal("admin-token", session.Current?.AccessToken);
        Assert.Equal("Local Admin", session.Current?.Name);
        Assert.Equal("name", session.Current?.NameClaimType);
        Assert.Equal("role", session.Current?.RoleClaimType);
        Assert.Contains(session.Current!.Claims, c => c.Type == "role" && c.Value == "Administrator");
        Assert.Contains(session.Current.Claims, c => c.Type == "name" && c.Value == "Local Admin");
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task Sign_out_clears_the_user_and_raises_changed()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        await session.SignInAsync("admin");
        var raised = 0;
        session.Changed += () => raised++;

        session.SignOut();

        Assert.Null(session.Current);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task Sign_in_with_unknown_user_throws()
    {
        await using var app = await StartAsync();

        await Assert.ThrowsAsync<HttpRequestException>(() => Session(app).SignInAsync("nobody"));
    }

    [Fact]
    public async Task Handler_sends_the_bearer_token_of_the_signed_in_user()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        await session.SignInAsync("admin");
        using var api = Api(app, session);

        Assert.Equal("Local Admin", await api.GetStringAsync("/whoami"));
    }

    [Fact]
    public async Task Handler_follows_the_user_switch()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        using var api = Api(app, session);

        await session.SignInAsync("admin");
        var first = await api.GetStringAsync("/whoami");
        await session.SignInAsync("guest");
        var second = await api.GetStringAsync("/whoami");
        session.SignOut();
        var third = await api.GetStringAsync("/whoami");

        Assert.Equal(new[] { "Local Admin", "Guest User", "anonymous" }, new[] { first, second, third });
    }

    [Fact]
    public async Task Handler_signs_in_the_start_user_on_first_use()
    {
        await using var app = await StartAsync();
        var session = Session(app, userId: "guest");
        using var api = Api(app, session);

        Assert.Equal("Guest User", await api.GetStringAsync("/whoami"));
        Assert.Equal("guest", session.Current?.UserId);
    }

    [Fact]
    public async Task Handler_without_a_user_sends_no_token()
    {
        await using var app = await StartAsync();
        using var api = Api(app, Session(app));

        Assert.Equal("anonymous", await api.GetStringAsync("/whoami"));
    }

    [Fact]
    public async Task Handler_keeps_an_existing_authorization_header()
    {
        await using var app = await StartAsync();
        var session = Session(app);
        await session.SignInAsync("admin");
        using var api = Api(app, session);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "guest-explicit");

        using var response = await api.SendAsync(request);

        // The unknown token belongs to the caller: StandIn answers 403 and does not replace it.
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dependency_injection_wires_session_and_handler()
    {
        await using var app = await StartAsync();
        var services = new ServiceCollection();
        services.AddStandInClient(options => options.ServerAddress = new Uri("http://localhost/"));
        services.AddHttpClient("StandIn").ConfigurePrimaryHttpMessageHandler(() => app.App.GetTestServer().CreateHandler());
        services.AddHttpClient("api", client => client.BaseAddress = new Uri("http://localhost/"))
            .ConfigurePrimaryHttpMessageHandler(() => app.App.GetTestServer().CreateHandler())
            .AddStandInBearerToken();
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<StandInSession>().SignInAsync("admin");
        var name = await provider.GetRequiredService<IHttpClientFactory>().CreateClient("api").GetStringAsync("/whoami");

        Assert.Equal("Local Admin", name);
    }

    [Fact]
    public void Client_registration_requires_the_server_address()
    {
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddStandInClient(_ => { }));
    }
}
