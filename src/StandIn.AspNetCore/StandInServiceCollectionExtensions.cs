using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StandIn.AspNetCore;

/// <summary>Marker: ist im Container vorhanden, wenn StandIn in dieser Umgebung aktiv ist.</summary>
internal sealed class StandInMarker
{
}

public static class StandInServiceCollectionExtensions
{
    /// <summary>
    /// Registriert StandIn. Aktiv nur in der Umgebung "Development" oder in den ausdruecklich
    /// freigegebenen <paramref name="additionalEnvironments"/>. In allen anderen Umgebungen (auch
    /// Production) passiert nichts: derselbe Code kann also ohne Fallunterscheidung ausgeliefert werden.
    /// </summary>
    /// <exception cref="ArgumentException">"Production" ist in <paramref name="additionalEnvironments"/> nicht erlaubt.</exception>
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

        // Wie Stuntman: StandIn uebernimmt als Standard-Schema, solange es aktiv ist.
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
