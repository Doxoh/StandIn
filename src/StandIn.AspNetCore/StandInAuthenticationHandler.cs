using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace StandIn.AspNetCore;

/// <summary>
/// Signs users in through bearer token or cookie. Behaves like RimDev.Stuntman:
/// no header: continue; header in the wrong format: 400; unknown token: 403
/// (unless <see cref="StandInOptions.AllowBearerTokenPassthrough"/>).
/// 400 and 403 are written at challenge, i.e. where a sign-in is required.
/// </summary>
public sealed class StandInAuthenticationHandler : AuthenticationHandler<StandInOptions>
{
    private const string FailureItem = "StandIn.Failure";

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

        if (Options.AllowTestHeader
            && Request.Headers.TryGetValue(StandInDefaults.TestHeaderName, out var headerValue)
            && !StringValues.IsNullOrEmpty(headerValue))
        {
            var headerUser = Options.FindUser(headerValue.ToString());

            return Task.FromResult(headerUser is null
                ? Fail(StatusCodes.Status403Forbidden, "No StandIn user has the requested id.")
                : Success(headerUser, accessToken: null));
        }

        if (Options.AllowCookieAuthentication
            && Request.Cookies.TryGetValue(StandInDefaults.CookieName, out var userId)
            && !string.IsNullOrEmpty(userId))
        {
            var user = Options.FindUser(userId);

            if (user is not null)
            {
                return Task.FromResult(Success(user, accessToken: null));
            }
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Context.Items.TryGetValue(FailureItem, out var item) && item is Failure failure)
        {
            Response.StatusCode = failure.Status;
            Response.ContentType = "text/plain; charset=utf-8";
            await Response.WriteAsync(failure.Message);
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
        Context.Items[FailureItem] = new Failure(statusCode, message);
        return AuthenticateResult.Fail(message);
    }

    private sealed record Failure(int Status, string Message);

    private bool IsBrowserNavigation()
    {
        return HttpMethods.IsGet(Request.Method)
            && Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
