using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StandIn.AspNetCore;

public static class StandInEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Bildet die StandIn-Endpunkte unter dem Root-Pfad (Standard "/stuntman") ab:
    /// sign-in, sign-out, server (nur mit EnableServer) und users.
    /// Ist StandIn in dieser Umgebung nicht aktiv, passiert nichts.
    /// </summary>
    public static IEndpointRouteBuilder MapStandIn(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var services = endpoints.ServiceProvider;
        var options = services.GetStandInOptions();

        if (options is null)
        {
            return endpoints;
        }

        if (options.ServerEnabled)
        {
            services.GetRequiredService<ILoggerFactory>().CreateLogger("StandIn").LogWarning(
                "StandIn: the endpoint {Path} returns all users including their access tokens without authentication. Use it for development only.",
                options.ServerPath);
        }

        var group = endpoints.MapGroup(options.RootPath);

        if (options.AllowCookieAuthentication)
        {
            group.MapGet("/sign-in", async (HttpContext context) =>
            {
                var userId = context.Request.Query[StandInDefaults.OverrideUserIdQueryKey].ToString();
                var returnUrl = ResolveReturnUrl(context, options);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(
                        StandInPickerRenderer.RenderLoginPage(options, context.Request.PathBase, returnUrl));
                    return;
                }

                var user = options.Users.FirstOrDefault(candidate => candidate.Id == userId);

                if (user is null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync($"options provided does not include the requested '{userId}' user.");
                    return;
                }

                context.Response.Cookies.Append(StandInDefaults.CookieName, user.Id, CreateCookieOptions(context));
                context.Response.Redirect(returnUrl);
            });

            group.MapGet("/sign-out", (HttpContext context) =>
            {
                context.Response.Cookies.Delete(StandInDefaults.CookieName, CreateCookieOptions(context));
                context.Response.Redirect(ResolveReturnUrl(context, options));
            });
        }

        if (options.ServerEnabled)
        {
            group.MapGet("/server", (HttpContext context) => context.Response.WriteAsJsonAsync(
                StandInUserLoader.ToDocument(options.Users, includeAccessTokens: true),
                StandInUserLoader.WriteOptions));
        }

        group.MapGet("/users", (HttpContext context) => context.Response.WriteAsJsonAsync(
            StandInUserLoader.ToDocument(options.Users, includeAccessTokens: false),
            StandInUserLoader.WriteOptions));

        return endpoints;
    }

    /// <summary>
    /// Ziel nach dem Anmelden: ReturnUrl, sonst der Referer (wie bei Stuntman). Nur lokale oder
    /// freigegebene Ziele; alles andere fuehrt zur Startseite der Anwendung.
    /// </summary>
    internal static string ResolveReturnUrl(HttpContext context, StandInOptions options)
    {
        var candidate = context.Request.Query[StandInDefaults.ReturnUrlQueryKey].ToString();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            var referer = context.Request.Headers.Referer.ToString();

            candidate = Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)
                && string.Equals(refererUri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase)
                    ? refererUri.PathAndQuery
                    : referer;
        }

        if (options.IsAllowedReturnUrl(candidate))
        {
            return candidate;
        }

        return context.Request.PathBase.HasValue ? context.Request.PathBase.Value! : "/";
    }

    private static CookieOptions CreateCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Secure = context.Request.IsHttps,
        Path = "/",
    };
}
