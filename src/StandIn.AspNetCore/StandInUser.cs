using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace StandIn.AspNetCore;

/// <summary>Ein Benutzer, als der man sich waehrend der Entwicklung anmelden kann.</summary>
public sealed class StandInUser
{
    public const string DefaultNameClaimType = "name";
    public const string DefaultRoleClaimType = "role";

    private readonly List<Claim> _claims = new();

    public StandInUser(string id, string name, string nameClaimType, string roleClaimType)
    {
        Id = Require(id, nameof(id));
        Name = Require(name, nameof(name));
        NameClaimType = Require(nameClaimType, nameof(nameClaimType));
        RoleClaimType = Require(roleClaimType, nameof(roleClaimType));
    }

    public StandInUser(string id, string name)
        : this(id, name, DefaultNameClaimType, DefaultRoleClaimType)
    {
    }

    /// <summary>Legt einen Benutzer mit automatisch erzeugter Id an.</summary>
    public StandInUser(string name)
        : this(Guid.NewGuid().ToString("D"), name)
    {
    }

    public string Id { get; }

    public string Name { get; }

    public string NameClaimType { get; }

    public string RoleClaimType { get; }

    public string? AccessToken { get; private set; }

    public string? Description { get; private set; }

    public string? Source { get; private set; }

    public IReadOnlyList<Claim> Claims => _claims;

    public StandInUser AddClaim(string type, string value)
    {
        _claims.Add(new Claim(Require(type, nameof(type)), Require(value, nameof(value))));
        return this;
    }

    public StandInUser AddName(string name) => AddClaim(NameClaimType, name);

    public StandInUser AddRole(string role) => AddClaim(RoleClaimType, role);

    public StandInUser SetAccessToken(string accessToken)
    {
        AccessToken = Require(accessToken, nameof(accessToken));
        return this;
    }

    public StandInUser SetDescription(string description)
    {
        Description = Require(description, nameof(description));
        return this;
    }

    public StandInUser SetSource(string source)
    {
        Source = Require(source, nameof(source));
        return this;
    }

    /// <summary>
    /// Baut den ClaimsPrincipal. Neben den eigenen Claims kommen Name (unter dem Namens-Claim-Typ
    /// des Benutzers und unter ClaimTypes.Name), NameIdentifier und die StandIn-Id dazu.
    /// Doppelte Claims (gleicher Typ und Wert) werden nur einmal aufgenommen.
    /// </summary>
    internal ClaimsPrincipal ToPrincipal(string authenticationType, string? accessToken = null)
    {
        var claims = new List<Claim>();
        var seen = new HashSet<(string Type, string Value)>();

        void Add(string type, string value)
        {
            if (seen.Add((type, value)))
            {
                claims.Add(new Claim(type, value));
            }
        }

        Add(StandInDefaults.UserIdClaimType, Id);
        Add(NameClaimType, Name);
        Add(ClaimTypes.Name, Name);

        if (!_claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
            Add(ClaimTypes.NameIdentifier, Id);
        }

        foreach (var claim in _claims)
        {
            Add(claim.Type, claim.Value);
        }

        if (accessToken is not null)
        {
            Add(StandInDefaults.AccessTokenClaimType, accessToken);
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType, NameClaimType, RoleClaimType));
    }

    private static string Require(string? value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} must not be empty or whitespace.", parameterName);
        }

        return value;
    }
}
