using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StandIn.AspNetCore;

/// <summary>
/// Meldet Benutzer ueber Bearer-Token oder Cookie an. Verhalten wie RimDev.Stuntman:
/// kein Header: weiter; Header im falschen Format: 400; unbekanntes Token: 403
/// (ausser <see cref="StandInOptions.AllowBearerTokenPassthrough"/>).
/// 400 und 403 werden beim Challenge geschrieben, also dort, wo eine Anmeldung verlangt wird.
/// </summary>
public sealed class StandInAuthenticationHandler : AuthenticationHandler<StandInOptions>
{
    private const string FailureStatusItem = "StandIn.FailureStatus";
    private const string FailureMessageItem = "StandIn.FailureMessage";

    public StandInAuthenticationHandler(
        IOptionsMonitor<StandInOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (Options.AllowBearerTokenAuthentication && IsBearer(authorization))
        {
            return Task.FromResult(AuthenticateBearer(authorization));
        }

        if (Options.AllowCookieAuthentication
            && Request.Cookies.TryGetValue(StandInDefaults.CookieName, out var userId)
            && !string.IsNullOrEmpty(userId))
        {
            var user = Options.Users.FirstOrDefault(candidate => candidate.Id == userId);

            if (user is not null)
            {
                return Task.FromResult(Success(user, accessToken: null));
            }
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Context.Items.TryGetValue(FailureStatusItem, out var status) && status is int statusCode)
        {
            Response.StatusCode = statusCode;

            if (Context.Items.TryGetValue(FailureMessageItem, out var message) && message is string text)
            {
                Response.ContentType = "text/plain; charset=utf-8";
                await Response.WriteAsync(text);
            }

            return;
        }

        if (Options.AllowCookieAuthentication && IsBrowserNavigation())
        {
            var returnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
            Response.Redirect(
                $"{Request.PathBase}{Options.SignInPath}?{StandInDefaults.ReturnUrlQueryKey}={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    private static bool IsBearer(string authorization)
    {
        const string scheme = "Bearer";

        return authorization.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)
            && (authorization.Length == scheme.Length || authorization[scheme.Length] == ' ');
    }

    private AuthenticateResult AuthenticateBearer(string authorization)
    {
        var parts = authorization.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            return Fail(StatusCodes.Status400BadRequest, "Authorization header is not in correct format.");
        }

        var accessToken = parts[1];
        var user = Options.Users.FirstOrDefault(candidate => candidate.AccessToken == accessToken);

        if (user is null)
        {
            return Options.AllowBearerTokenPassthrough
                ? AuthenticateResult.NoResult()
                : Fail(StatusCodes.Status403Forbidden, "No StandIn user has the requested access token.");
        }

        var result = Success(user, accessToken);
        Options.AfterBearerValidateIdentity?.Invoke(Context, user);
        return result;
    }

    private AuthenticateResult Success(StandInUser user, string? accessToken)
    {
        var principal = user.ToPrincipal(Scheme.Name, accessToken);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private AuthenticateResult Fail(int statusCode, string message)
    {
        Context.Items[FailureStatusItem] = statusCode;
        Context.Items[FailureMessageItem] = message;
        return AuthenticateResult.Fail(message);
    }

    private bool IsBrowserNavigation()
    {
        return HttpMethods.IsGet(Request.Method)
            && Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
