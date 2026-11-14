namespace StandIn.AspNetCore;

/// <summary>Feste Namen und Standardwerte. Query-Parameter entsprechen RimDev.Stuntman.</summary>
public static class StandInDefaults
{
    public const string AuthenticationScheme = "StandIn";
    public const string DefaultRootPath = "/stuntman";
    public const string CookieName = ".StandIn.User";
    public const string LocalSource = "Local";

    public const string OverrideUserIdQueryKey = "OverrideUserId";
    public const string ReturnUrlQueryKey = "ReturnUrl";

    /// <summary>Claim mit der StandIn-Benutzer-Id (zum Erkennen des aktiven Benutzers im Picker).</summary>
    public const string UserIdClaimType = "standin_user_id";

    /// <summary>Claim mit dem verwendeten Bearer-Token (wie bei Stuntman).</summary>
    public const string AccessTokenClaimType = "access_token";
}
