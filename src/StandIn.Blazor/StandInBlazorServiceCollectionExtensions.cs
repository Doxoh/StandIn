using System;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using StandIn.Client;

namespace StandIn.Blazor;

public static class StandInBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Fuer Blazor WebAssembly, Auto (WASM-Teil) und Hybrid: registriert <see cref="StandInSession"/> und den
    /// <see cref="StandInAuthenticationStateProvider"/>. Nur in Entwicklungsumgebungen aufrufen. API-Clients bekommen
    /// das Token mit <c>AddHttpClient("api").AddStandInBearerToken()</c>.
    /// </summary>
    public static IServiceCollection AddStandInBlazor(this IServiceCollection services, Action<StandInClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddStandInClient(configure);
        services.AddAuthorizationCore();
        services.AddSingleton<AuthenticationStateProvider, StandInAuthenticationStateProvider>();

        return services;
    }
}
