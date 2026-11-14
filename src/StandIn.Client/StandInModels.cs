using System.Collections.Generic;

namespace StandIn.Client;

/// <summary>A user from the server's list (without a token).</summary>
public sealed record StandInUserInfo(string Id, string Name, string? Description);

public sealed record StandInClaim(string Type, string Value);

/// <summary>Response of the token endpoint: the bearer token and the claims the server builds for the user.</summary>
public sealed record StandInToken(
    string AccessToken,
    string UserId,
    string Name,
    string NameClaimType,
    string RoleClaimType,
    IReadOnlyList<StandInClaim> Claims);
