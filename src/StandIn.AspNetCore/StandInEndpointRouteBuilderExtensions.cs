using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    /// Maps the StandIn endpoints under the root path (default "/stuntman"):
    /// sign-in, sign-out, server (only with EnableServer) and users.
    /// If StandIn is not active in this environment, nothing happens.
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
                    await WriteTextAsync(context, StatusCodes.Status404NotFound, $"options provided does not include the requested '{userId}' user.");
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

        group.MapGet("/users", (HttpContext context) =>
        {
            AddCorsHeaders(context, options);

            return context.Response.WriteAsJsonAsync(
                StandInUserLoader.ToDocument(options.Users, includeAccessTokens: false),
                StandInUserLoader.WriteOptions);
        });

        if (options.TokenEndpointEnabled)
        {
            MapToken(group, options);
            MapTokenPreflight(group, options);

            services.GetRequiredService<ILoggerFactory>().CreateLogger("StandIn").LogWarning(
                "StandIn: the endpoint {Path} hands out a bearer token for any user without authentication. Use it for development only.",
                options.RootPath + "/token");
        }

        // Old links and bookmarks keep working after the root path was changed.
        if (!IsAtOrUnder(options.RootPath, StandInDefaults.DefaultRootPath))
        {
            endpoints.Map(StandInDefaults.DefaultRootPath + "/{**rest}", (HttpContext context, string? rest) =>
                Results.Redirect(
                    $"{context.Request.PathBase}{options.RootPath}/{rest?.TrimStart('/', '\\')}{context.Request.QueryString}",
                    permanent: true,
                    preserveMethod: true));
        }

        return endpoints;
    }

    /// <summary>
    /// Target after sign-in: ReturnUrl, otherwise the referer (like Stuntman). Only local or
    /// allowed targets; anything else leads to the start page of the application.
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

        return options.FrontendBaseUrl ?? (context.Request.PathBase.HasValue ? context.Request.PathBase.Value! : "/");
    }

    private static void MapToken(RouteGroupBuilder group, StandInOptions options)
    {
        // Users without a token get a random development token (in memory only).
        foreach (var user in options.Users.Where(user => user.AccessToken is null))
        {
            user.SetAccessToken(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
        }

        group.MapPost("/token", async (HttpContext context) =>
        {
            AddCorsHeaders(context, options);

            // JSON only: a foreign form cannot trigger the endpoint, and CORS requires a preflight.
            if (!context.Request.HasJsonContentType())
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            TokenRequest? request;

            try
            {
                request = await context.Request.ReadFromJsonAsync<TokenRequest>();
            }
            catch (JsonException)
            {
                request = null;
            }

            var userId = request?.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                await WriteTextAsync(context, StatusCodes.Status400BadRequest, "The request needs a JSON body like { \"userId\": \"admin\" }.");
                return;
            }

            var user = options.Users.FirstOrDefault(candidate => candidate.Id == userId);

            if (user is null)
            {
                await WriteTextAsync(context, StatusCodes.Status404NotFound, $"options provided does not include the requested '{userId}' user.");
                return;
            }

            // The claims the server builds for the user: clients build the same principal from them.
            var principal = user.ToPrincipal(StandInDefaults.AuthenticationScheme);

            await context.Response.WriteAsJsonAsync(new
            {
                access_token = user.AccessToken,
                token_type = "Bearer",
                user_id = user.Id,
                name = user.Name,
                name_claim_type = user.NameClaimType,
                role_claim_type = user.RoleClaimType,
                claims = principal.Claims.Select(claim => new { type = claim.Type, value = claim.Value }),
            });
        });
    }

    private static Task WriteTextAsync(HttpContext context, int statusCode, string text)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(text);
    }

    /// <summary>CORS only for allowed origins (FrontendBaseUrl, AddAllowedReturnOrigin).</summary>
    private static bool AddCorsHeaders(HttpContext context, StandInOptions options)
    {
        var origin = context.Request.Headers.Origin.ToString();

        if (!options.IsAllowedOrigin(origin))
        {
            return false;
        }

        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers.Append("Vary", "Origin");
        return true;
    }

    /// <summary>Preflight for browser frontends: the JSON POST to the token endpoint needs it.</summary>
    private static void MapTokenPreflight(RouteGroupBuilder group, StandInOptions options)
    {
        group.MapMethods("/token", new[] { HttpMethods.Options }, (HttpContext context) =>
        {
            if (AddCorsHeaders(context, options))
            {
                context.Response.Headers["Access-Control-Allow-Methods"] = "POST";
                context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
                context.Response.Headers["Access-Control-Max-Age"] = "600";
            }

            context.Response.StatusCode = StatusCodes.Status204NoContent;
        });
    }

    private static bool IsAtOrUnder(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);

    private sealed class TokenRequest
    {
        public string? UserId { get; set; }
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
