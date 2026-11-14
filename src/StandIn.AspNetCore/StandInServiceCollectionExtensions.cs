using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace StandIn.AspNetCore;

/// <summary>Marker: present in the container when StandIn is active in this environment.</summary>
internal sealed class StandInMarker
{
}

internal static class StandInServiceProviderExtensions
{
    /// <summary>The options, or <c>null</c> if StandIn is not active in this environment.</summary>
    internal static StandInOptions? GetStandInOptions(this IServiceProvider services)
    {
        return services.GetService<StandInMarker>() is null
            ? null
            : services.GetRequiredService<IOptionsMonitor<StandInOptions>>().Get(StandInDefaults.AuthenticationScheme);
    }
}

public static class StandInServiceCollectionExtensions
{
    /// <summary>
    /// Registers StandIn. Active only in the "Development" environment or in the explicitly
    /// allowed <paramref name="additionalEnvironments"/>. In all other environments (including
    /// Production) nothing happens: the same code can therefore ship without a case distinction.
    /// </summary>
    /// <exception cref="ArgumentException">"Production" is not allowed in <paramref name="additionalEnvironments"/>.</exception>
    public static AuthenticationBuilder AddStandIn(
        this AuthenticationBuilder builder,
        IHostEnvironment environment,
        Action<StandInOptions>? configure = null,
        IEnumerable<string>? additionalEnvironments = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(environment);

        var extra = additionalEnvironments?.ToArray() ?? Array.Empty<string>();

        if (extra.Any(name => string.Equals(name, Environments.Production, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "StandIn must never be enabled in the Production environment.",
                nameof(additionalEnvironments));
        }

        var enabled = environment.IsDevelopment()
            || extra.Contains(environment.EnvironmentName, StringComparer.OrdinalIgnoreCase);

        if (!enabled)
        {
            return builder;
        }

        builder.Services.AddSingleton<StandInMarker>();

        // Like Stuntman: StandIn becomes the default scheme as long as it is active.
        builder.Services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = StandInDefaults.AuthenticationScheme;
        });

        return builder.AddScheme<StandInOptions, StandInAuthenticationHandler>(
            StandInDefaults.AuthenticationScheme,
            StandInDefaults.AuthenticationScheme,
            configure ?? (_ => { }));
    }
}
