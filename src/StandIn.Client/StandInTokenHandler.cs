using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace StandIn.Client;

/// <summary>Haengt das Bearer-Token des angemeldeten StandIn-Benutzers an ausgehende Anfragen. Ein vorhandener Authorization-Header bleibt unveraendert.</summary>
public sealed class StandInTokenHandler : DelegatingHandler
{
    private readonly StandInSession _session;

    public StandInTokenHandler(StandInSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var token = await _session.GetAccessTokenAsync(cancellationToken);

            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
