using System;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using StandIn.Client;

namespace StandIn.Blazor;

public static class StandInBlazorServiceCollectionExtensions
{
    /// <summary>
    /// For Blazor WebAssembly, Auto (WASM part) and Hybrid: registers <see cref="StandInSession"/> and the
    /// <see cref="StandInAuthenticationStateProvider"/>. Call only in development environments. API clients get
    /// the token with <c>AddHttpClient("api").AddStandInBearerToken()</c>.
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
