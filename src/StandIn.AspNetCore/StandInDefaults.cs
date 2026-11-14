namespace StandIn.AspNetCore;

/// <summary>Fixed names and default values. Query parameters match RimDev.Stuntman.</summary>
public static class StandInDefaults
{
    public const string AuthenticationScheme = "StandIn";
    public const string DefaultRootPath = "/stuntman";
    internal const string CookieName = ".StandIn.User";
    internal const string LocalSource = "Local";

    /// <summary>Test header: names the id of the user the request counts as (only with <see cref="StandInOptions.AllowTestHeader"/>).</summary>
    public const string TestHeaderName = "X-Stuntman-User";

    internal const string OverrideUserIdQueryKey = "OverrideUserId";
    internal const string ReturnUrlQueryKey = "ReturnUrl";

    /// <summary>Claim with the StandIn user id (to recognize the active user in the picker).</summary>
    internal const string UserIdClaimType = "standin_user_id";

    /// <summary>Claim with the bearer token used (like Stuntman).</summary>
    public const string AccessTokenClaimType = "access_token";
}
