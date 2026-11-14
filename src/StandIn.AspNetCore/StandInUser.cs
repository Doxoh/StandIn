using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace StandIn.AspNetCore;

/// <summary>A user you can sign in as during development.</summary>
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

    /// <summary>Creates a user with an automatically generated id.</summary>
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

    internal StandInUser SetSource(string source)
    {
        Source = Require(source, nameof(source));
        return this;
    }

    /// <summary>
    /// Builds the ClaimsPrincipal. Besides the own claims it adds the name (under the name claim type
    /// of the user and under ClaimTypes.Name), NameIdentifier and the StandIn id.
    /// Duplicate claims (same type and value) are added only once.
    /// </summary>
    internal ClaimsPrincipal ToPrincipal(string authenticationType, string? accessToken = null)
    {
        var claims = new List<Claim>(_claims);

        void Ensure(string type, string value)
        {
            if (!claims.Any(c => c.Type == type && c.Value == value))
            {
                claims.Add(new Claim(type, value));
            }
        }

        Ensure(StandInDefaults.UserIdClaimType, Id);
        Ensure(NameClaimType, Name);
        Ensure(ClaimTypes.Name, Name);

        if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, Id));
        }

        if (accessToken is not null)
        {
            claims.Add(new Claim(StandInDefaults.AccessTokenClaimType, accessToken));
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
