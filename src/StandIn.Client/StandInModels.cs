using System.Collections.Generic;

namespace StandIn.Client;

/// <summary>Ein Benutzer aus der Liste des Servers (ohne Token).</summary>
public sealed record StandInUserInfo(string Id, string Name, string? Description);

public sealed record StandInClaim(string Type, string Value);

/// <summary>Antwort des Token-Endpunkts: das Bearer-Token und die Claims, die der Server fuer den Benutzer bildet.</summary>
public sealed record StandInToken(
    string AccessToken,
    string UserId,
    string Name,
    string NameClaimType,
    string RoleClaimType,
    IReadOnlyList<StandInClaim> Claims);
