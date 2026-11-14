using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace StandIn.AspNetCore;

/// <summary>Liest und schreibt das Stuntman-JSON-Format: { "Users": [ { "Id", "Name", ... } ] }.</summary>
internal static class StandInUserLoader
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>PascalCase wie bei Stuntman, damit Server und Clients unterschiedlicher Versionen zusammenarbeiten.</summary>
    internal static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Liest Text aus einer Datei (absolut oder relativ zum Arbeitsverzeichnis) oder einer http(s)-URL.</summary>
    public static string ReadText(string pathOrUrl)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                // Beim Start einmalig; synchron, damit die Optionen-API wie bei Stuntman fluent bleibt.
                return Http.GetStringAsync(uri).GetAwaiter().GetResult();
            }

            if (uri.IsFile)
            {
                return File.ReadAllText(uri.LocalPath);
            }
        }

        return File.ReadAllText(pathOrUrl);
    }

    public static IReadOnlyList<StandInUser> Parse(string json)
    {
        var document = JsonSerializer.Deserialize<UsersDocument>(json, ReadOptions)
            ?? throw new InvalidDataException("The StandIn user JSON is empty.");

        return ToUsers(document);
    }

    /// <summary>Liest dieselbe Struktur wie das JSON ({ "Users": [ ... ] }) aus einem Konfigurationsabschnitt.</summary>
    public static IReadOnlyList<StandInUser> FromConfiguration(IConfiguration configuration)
    {
        return ToUsers(configuration.Get<UsersDocument>() ?? new UsersDocument());
    }

    private static List<StandInUser> ToUsers(UsersDocument document)
    {
        var users = new List<StandInUser>();

        foreach (var dto in document.Users ?? new List<UserDto>())
        {
            var user = new StandInUser(
                Required(dto.Id, "Id"),
                Required(dto.Name, "Name"),
                dto.NameClaimType ?? StandInUser.DefaultNameClaimType,
                dto.RoleClaimType ?? StandInUser.DefaultRoleClaimType);

            foreach (var claim in dto.Claims ?? new List<ClaimDto>())
            {
                user.AddClaim(Required(claim.Type, "Claims[].Type"), Required(claim.Value, "Claims[].Value"));
            }

            if (!string.IsNullOrWhiteSpace(dto.AccessToken))
            {
                user.SetAccessToken(dto.AccessToken);
            }

            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                user.SetDescription(dto.Description);
            }

            users.Add(user);
        }

        return users;
    }

    public static UsersDocument ToDocument(IEnumerable<StandInUser> users, bool includeAccessTokens)
    {
        return new UsersDocument
        {
            Users = users.Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                AccessToken = includeAccessTokens ? user.AccessToken : null,
                Description = user.Description,
                NameClaimType = user.NameClaimType,
                RoleClaimType = user.RoleClaimType,
                Claims = user.Claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value }).ToList(),
            }).ToList(),
        };
    }

    private static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"The StandIn user JSON requires a value for '{field}'.");
        }

        return value;
    }

    internal sealed class UsersDocument
    {
        public List<UserDto>? Users { get; set; }
    }

    internal sealed class UserDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? AccessToken { get; set; }

        public string? Description { get; set; }

        public string? NameClaimType { get; set; }

        public string? RoleClaimType { get; set; }

        public List<ClaimDto>? Claims { get; set; }
    }

    internal sealed class ClaimDto
    {
        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}
