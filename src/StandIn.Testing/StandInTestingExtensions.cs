using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StandIn.AspNetCore;

namespace StandIn.Testing;

/// <summary>Helpers for integration tests: send requests as a StandIn user.</summary>
public static class StandInTestingExtensions
{
    /// <summary>
    /// Turns the test header on, e.g. in <c>factory.WithWebHostBuilder(b =&gt; b.ConfigureTestServices(s =&gt; s.AllowStandInTestHeader()))</c>.
    /// </summary>
    public static IServiceCollection AllowStandInTestHeader(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.PostConfigure<StandInOptions>(
            StandInDefaults.AuthenticationScheme,
            options => options.AllowTestHeader = true);
    }

    /// <summary>A client of the <see cref="WebApplicationFactory{TEntryPoint}"/> whose requests count as user <paramref name="userId"/>.</summary>
    public static HttpClient CreateStandInClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory, string userId)
        where TEntryPoint : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AllowStandInTestHeader()))
            .CreateClient()
            .AsStandInUser(userId);
    }

    /// <summary>All requests of this client count as user <paramref name="userId"/>.</summary>
    public static HttpClient AsStandInUser(this HttpClient client, string userId)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        client.DefaultRequestHeaders.Remove(StandInDefaults.TestHeaderName);
        client.DefaultRequestHeaders.Add(StandInDefaults.TestHeaderName, userId);
        return client;
    }
}
