using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using StandIn.Client;

namespace StandIn.Blazor;

/// <summary>
/// Provides the user signed in through <see cref="StandInSession"/> as <see cref="AuthenticationState"/>
/// (Blazor WebAssembly, Auto, Hybrid). When the user changes, components are notified.
/// </summary>
public sealed class StandInAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly StandInSession _session;

    public StandInAuthenticationStateProvider(StandInSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _session.Changed += OnSessionChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Signs in the start user on the first call (StandInClientOptions.UserId).
        await _session.GetAccessTokenAsync();

        return new AuthenticationState(ToPrincipal(_session.Current));
    }

    public void Dispose()
    {
        _session.Changed -= OnSessionChanged;
    }

    private void OnSessionChanged()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(ToPrincipal(_session.Current))));
    }

    private static ClaimsPrincipal ToPrincipal(StandInToken? token)
    {
        if (token is null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = token.Claims.Select(claim => new Claim(claim.Type, claim.Value));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "StandIn", token.NameClaimType, token.RoleClaimType));
    }
}
