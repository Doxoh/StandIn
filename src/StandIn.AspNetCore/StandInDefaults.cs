namespace StandIn.AspNetCore;

/// <summary>Feste Namen und Standardwerte. Query-Parameter entsprechen RimDev.Stuntman.</summary>
public static class StandInDefaults
{
    public const string AuthenticationScheme = "StandIn";
    public const string DefaultRootPath = "/stuntman";
    internal const string CookieName = ".StandIn.User";
    internal const string LocalSource = "Local";

    /// <summary>Test-Header: nennt die Id des Benutzers, als der die Anfrage gilt (nur mit <see cref="StandInOptions.AllowTestHeader"/>).</summary>
    public const string TestHeaderName = "X-Stuntman-User";

    internal const string OverrideUserIdQueryKey = "OverrideUserId";
    internal const string ReturnUrlQueryKey = "ReturnUrl";

    /// <summary>Claim mit der StandIn-Benutzer-Id (zum Erkennen des aktiven Benutzers im Picker).</summary>
    internal const string UserIdClaimType = "standin_user_id";

    /// <summary>Claim mit dem verwendeten Bearer-Token (wie bei Stuntman).</summary>
    public const string AccessTokenClaimType = "access_token";
}
