using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace StandIn.Client;

public static class StandInClientServiceCollectionExtensions
{
    private const string SessionClientName = "StandIn";

    /// <summary>
    /// Registriert die <see cref="StandInSession"/>. Danach den Token-Handler an die API-Clients haengen:
    /// <c>services.AddHttpClient("api").AddStandInBearerToken()</c>.
    /// </summary>
    public static IServiceCollection AddStandInClient(this IServiceCollection services, Action<StandInClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new StandInClientOptions();
        configure(options);

        var address = options.ServerAddress
            ?? throw new ArgumentException("StandInClientOptions.ServerAddress is required.", nameof(configure));

        services.AddHttpClient(SessionClientName, client => client.BaseAddress = address);
        services.AddSingleton(provider => new StandInSession(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(SessionClientName),
            options));

        return services;
    }

    /// <summary>Setzt das Bearer-Token des StandIn-Benutzers auf Anfragen dieses Clients (nicht auf den StandIn-Client selbst anwenden).</summary>
    public static IHttpClientBuilder AddStandInBearerToken(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddHttpMessageHandler(provider => new StandInTokenHandler(provider.GetRequiredService<StandInSession>()));
    }
}
