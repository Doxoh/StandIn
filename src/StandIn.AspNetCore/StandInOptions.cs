using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace StandIn.AspNetCore;

/// <summary>Optionen fuer StandIn. API-Namen folgen RimDev.Stuntman (StuntmanOptions).</summary>
public sealed class StandInOptions : AuthenticationSchemeOptions
{
    private readonly List<StandInUser> _users = new();
    private readonly HashSet<string> _allowedReturnOrigins = new(StringComparer.OrdinalIgnoreCase);
    private string _rootPath = StandInDefaults.DefaultRootPath;

    /// <summary>Basispfad der StandIn-Endpunkte, Standard "/stuntman" (ohne abschliessenden Schraegstrich).</summary>
    public string RootPath
    {
        get => _rootPath;
        set => _rootPath = NormalizeRootPath(value);
    }

    public string SignInPath => RootPath + "/sign-in";

    public string SignOutPath => RootPath + "/sign-out";

    public string ServerPath => RootPath + "/server";

    public string UsersPath => RootPath + "/users";

    /// <summary>Cookie-Anmeldung und Login-Seite (Standard: an).</summary>
    public bool AllowCookieAuthentication { get; set; } = true;

    /// <summary>Bearer-Token-Anmeldung (Standard: an).</summary>
    public bool AllowBearerTokenAuthentication { get; set; } = true;

    /// <summary>
    /// Ein Bearer-Token ohne passenden StandIn-Benutzer fuehrt nicht zu 403, sondern wird an andere
    /// Authentifizierungsverfahren weitergereicht (z. B. echtes JWT neben StandIn).
    /// </summary>
    public bool AllowBearerTokenPassthrough { get; set; }

    public StandInAlignment UserPickerAlignment { get; private set; }

    /// <summary>Ist der Server-Endpunkt aktiv? Achtung: er liefert alle Access Tokens ohne Anmeldung aus.</summary>
    public bool ServerEnabled { get; private set; }

    public IReadOnlyList<StandInUser> Users => _users;

    /// <summary>Hook nach erfolgreicher Bearer-Anmeldung (z. B. fuer Tests).</summary>
    public Action<HttpContext, StandInUser>? AfterBearerValidateIdentity { get; set; }

    /// <summary>Fuegt einen Benutzer hinzu. Die Id muss eindeutig sein.</summary>
    public StandInOptions AddUser(StandInUser user) => AddUser(user, StandInDefaults.LocalSource);

    public StandInOptions AddUsers(IEnumerable<StandInUser> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        foreach (var user in users)
        {
            AddUser(user);
        }

        return this;
    }

    /// <summary>
    /// Fuegt Benutzer aus einer JSON-Datei oder http(s)-URL hinzu.
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
    /// Laedt Benutzer von einer anderen StandIn-Anwendung mit aktivem Server-Endpunkt.
    /// Fehler werden geworfen.
    /// </summary>
    /// <param name="serverBaseUrl">Basis-URL der Server-Anwendung, z. B. https://auth.example.com/</param>
    /// <param name="serverRootPath">Root-Pfad der Server-Anwendung, Standard "/stuntman".</param>
    public StandInOptions AddConfigurationFromServer(string serverBaseUrl, string serverRootPath = StandInDefaults.DefaultRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverBaseUrl);

        var endpoint = new Uri(new Uri(serverBaseUrl), NormalizeRootPath(serverRootPath) + "/server");

        foreach (var user in StandInUserLoader.Parse(StandInUserLoader.ReadText(endpoint.AbsoluteUri)))
        {
            AddUser(user, serverBaseUrl);
        }

        return this;
    }

    /// <summary>Wie <see cref="AddConfigurationFromServer"/>, aber Fehler werden nur an <paramref name="onException"/> gemeldet.</summary>
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

    /// <summary>Aktiviert den Server-Endpunkt dieser Anwendung (liefert Benutzer inkl. Tokens ohne Anmeldung).</summary>
    public StandInOptions EnableServer()
    {
        ServerEnabled = true;
        return this;
    }

    public StandInOptions SetUserPickerAlignment(StandInAlignment alignment)
    {
        UserPickerAlignment = alignment;
        return this;
    }

    /// <summary>Erlaubt Weiterleitungen nach dem Anmelden zu einer fremden Adresse, z. B. "https://localhost:5173".</summary>
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
    /// Nur lokale Pfade ("/home") oder freigegebene Origins sind als Ziel nach dem Anmelden erlaubt.
    /// "//host" und "/\host" gelten nicht als lokal (Open-Redirect-Schutz).
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
