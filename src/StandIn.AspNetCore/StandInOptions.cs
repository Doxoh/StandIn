using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace StandIn.AspNetCore;

/// <summary>Options for StandIn. API names follow RimDev.Stuntman (StuntmanOptions).</summary>
public sealed class StandInOptions : AuthenticationSchemeOptions
{
    private readonly List<StandInUser> _users = new();
    private readonly HashSet<string> _allowedReturnOrigins = new(StringComparer.OrdinalIgnoreCase);
    private string _rootPath = StandInDefaults.DefaultRootPath;
    private string? _frontendBaseUrl;

    /// <summary>Base path of the StandIn endpoints, default "/stuntman" (without a trailing slash).</summary>
    public string RootPath
    {
        get => _rootPath;
        set => _rootPath = NormalizeRootPath(value);
    }

    public string SignInPath => RootPath + "/sign-in";

    public string SignOutPath => RootPath + "/sign-out";

    public string ServerPath => RootPath + "/server";

    public string UsersPath => RootPath + "/users";

    /// <summary>Cookie sign-in and login page (default: on).</summary>
    public bool AllowCookieAuthentication { get; set; } = true;

    /// <summary>Bearer token sign-in (default: on).</summary>
    public bool AllowBearerTokenAuthentication { get; set; } = true;

    /// <summary>
    /// A bearer token without a matching StandIn user does not lead to 403 but is passed on to other
    /// authentication handlers (e.g. a real JWT next to StandIn).
    /// </summary>
    public bool AllowBearerTokenPassthrough { get; set; }

    /// <summary>
    /// Test header <c>X-Stuntman-User: &lt;Id&gt;</c> (default: off). Caution: without a sign-in any user counts,
    /// so turn it on in tests only.
    /// </summary>
    public bool AllowTestHeader { get; set; }

    /// <summary>
    /// Address of the frontend when frontend and API run separately (e.g. "https://localhost:5173/"). The
    /// return redirect after sign-in/sign-out goes there when neither ReturnUrl nor Referer is present.
    /// Its origin counts automatically as allowed (return redirect and CORS).
    /// </summary>
    public string? FrontendBaseUrl
    {
        get => _frontendBaseUrl;
        set
        {
            if (value is not null)
            {
                AddAllowedReturnOrigin(value);
            }

            _frontendBaseUrl = value;
        }
    }

    /// <summary>Is <c>POST {RootPath}/token</c> active? Caution: it hands out a bearer token for any user without a sign-in.</summary>
    public bool TokenEndpointEnabled { get; private set; }

    public StandInAlignment UserPickerAlignment { get; private set; }

    public StandInVerticalPosition UserPickerVerticalPosition { get; private set; }

    /// <summary>Is the server endpoint active? Caution: it hands out all access tokens without a sign-in.</summary>
    public bool ServerEnabled { get; private set; }

    public IReadOnlyList<StandInUser> Users => _users;

    /// <summary>Hook after a successful bearer sign-in (e.g. for tests).</summary>
    public Action<HttpContext, StandInUser>? AfterBearerValidateIdentity { get; set; }

    /// <summary>Adds a user. The id must be unique.</summary>
    public StandInOptions AddUser(StandInUser user) => AddUser(user, StandInDefaults.LocalSource);

    /// <summary>
    /// Adds users from a JSON file or http(s) URL.
    /// Format: { "Users": [ { "Id": "user-1", "Name": "User 1" } ] }
    /// </summary>
    public StandInOptions AddUsersFromJson(string pathOrUrl, Action<StandInUser>? configureUsers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathOrUrl);

        foreach (var user in StandInUserLoader.Parse(StandInUserLoader.ReadText(pathOrUrl)))
        {
            configureUsers?.Invoke(user);
            AddUser(user, pathOrUrl);
        }

        return this;
    }

    /// <summary>
    /// Adds users from a configuration section, e.g. <c>GetSection("StandIn")</c> with
    /// <c>Users:0:Id</c>, <c>Users:0:Name</c>, ... (same structure as the JSON file).
    /// </summary>
    public StandInOptions AddUsersFromConfiguration(IConfiguration configuration, Action<StandInUser>? configureUsers = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        foreach (var user in StandInUserLoader.FromConfiguration(configuration))
        {
            configureUsers?.Invoke(user);
            AddUser(user, "Configuration");
        }

        return this;
    }

    /// <summary>
    /// Loads users from another StandIn application with an active server endpoint.
    /// Errors are thrown. https is allowed; http only for localhost, because the response contains access tokens.
    /// </summary>
    /// <param name="serverBaseUrl">Base URL of the server application, e.g. https://auth.example.com/</param>
    /// <param name="serverRootPath">Root path of the server application, default "/stuntman".</param>
    public StandInOptions AddConfigurationFromServer(string serverBaseUrl, string serverRootPath = StandInDefaults.DefaultRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverBaseUrl);

        var endpoint = new Uri(new Uri(serverBaseUrl), NormalizeRootPath(serverRootPath) + "/server");

        var isHttps = endpoint.Scheme == Uri.UriSchemeHttps;
        var isLocalHttp = endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback;

        if (!isHttps && !isLocalHttp)
        {
            throw new ArgumentException("The StandIn server must be reached via https (http is only allowed for localhost).", nameof(serverBaseUrl));
        }

        foreach (var user in StandInUserLoader.Parse(StandInUserLoader.ReadText(endpoint.AbsoluteUri)))
        {
            AddUser(user, serverBaseUrl);
        }

        return this;
    }

    /// <summary>Like <see cref="AddConfigurationFromServer"/>, but errors are only reported to <paramref name="onException"/>.</summary>
    public StandInOptions TryAddConfigurationFromServer(
        string serverBaseUrl,
        Action<Exception>? onException = null,
        string serverRootPath = StandInDefaults.DefaultRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverBaseUrl);

        try
        {
            AddConfigurationFromServer(serverBaseUrl, serverRootPath);
        }
        catch (Exception exception)
        {
            onException?.Invoke(exception);
        }

        return this;
    }

    /// <summary>Activates the server endpoint of this application (hands out users including tokens without a sign-in).</summary>
    public StandInOptions EnableServer()
    {
        ServerEnabled = true;
        return this;
    }

    /// <summary>
    /// Activates <c>POST {RootPath}/token</c> with <c>{ "userId": "..." }</c>: returns the user's bearer token
    /// (for Blazor WebAssembly, MAUI, WPF, SPAs). Users without a token get a random development token.
    /// </summary>
    public StandInOptions EnableTokenEndpoint()
    {
        TokenEndpointEnabled = true;
        return this;
    }

    public StandInOptions SetUserPickerAlignment(StandInAlignment alignment)
    {
        UserPickerAlignment = alignment;
        return this;
    }

    public StandInOptions SetUserPickerVerticalPosition(StandInVerticalPosition position)
    {
        UserPickerVerticalPosition = position;
        return this;
    }

    /// <summary>Allows redirects after sign-in to a foreign address, e.g. "https://localhost:5173".</summary>
    public StandInOptions AddAllowedReturnOrigin(string origin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("The origin must be an absolute http or https URL.", nameof(origin));
        }

        _allowedReturnOrigins.Add(uri.GetLeftPart(UriPartial.Authority));
        return this;
    }

    /// <summary>May this origin (from the <c>Origin</c> header) read the endpoints through CORS?</summary>
    internal bool IsAllowedOrigin(string? origin)
    {
        return !string.IsNullOrEmpty(origin) && _allowedReturnOrigins.Contains(origin);
    }

    internal StandInOptions AddUser(StandInUser user, string? source)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (_users.Any(existing => existing.Id == user.Id))
        {
            throw new InvalidOperationException($"A user with the Id '{user.Id}' already exists. User ids must be unique.");
        }

        if (source is not null)
        {
            user.SetSource(source);
        }

        _users.Add(user);
        return this;
    }

    /// <summary>
    /// Only local paths ("/home") or allowed origins are valid targets after sign-in.
    /// "//host" and "/\host" do not count as local (open-redirect protection).
    /// </summary>
    internal bool IsAllowedReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url[0] == '/')
        {
            return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return _allowedReturnOrigins.Contains(uri.GetLeftPart(UriPartial.Authority));
        }

        return false;
    }

    private static string NormalizeRootPath(string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim().TrimEnd('/');

        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return trimmed[0] == '/' ? trimmed : "/" + trimmed;
    }
}
