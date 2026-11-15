using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace StandIn.Client;

/// <summary>
/// Remembers which user the client acts as and fetches that user's bearer token from the StandIn server.
/// One instance per application (singleton).
/// </summary>
public sealed class StandInSession
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _rootPath;
    private readonly string? _initialUserId;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _initialized;

    /// <param name="http">Client with the server's <c>BaseAddress</c>.</param>
    /// <param name="options">Root path and start user.</param>
    public StandInSession(HttpClient http, StandInClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);

        _http = http;
        _rootPath = options.RootPath.Trim('/');
        _initialUserId = options.UserId;
    }

    /// <summary>The signed-in user, or <c>null</c> (anonymous).</summary>
    public StandInToken? Current { get; private set; }

    /// <summary>Raised when <see cref="Current"/> changes.</summary>
    public event Action? Changed;

    public async Task<IReadOnlyList<StandInUserInfo>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var document = await _http.GetFromJsonAsync<UsersDto>($"{_rootPath}/users", Json, cancellationToken);

        return (document?.Users ?? new List<UserDto>())
            .Select(user => new StandInUserInfo(user.Id ?? string.Empty, user.Name ?? string.Empty, user.Description))
            .ToList();
    }

    /// <summary>Signs the user in: fetches the token from the token endpoint (<c>EnableTokenEndpoint()</c> on the server).</summary>
    public async Task SignInAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        using var response = await _http.PostAsJsonAsync($"{_rootPath}/token", new { userId }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<TokenDto>(Json, cancellationToken)
            ?? throw new InvalidOperationException("The StandIn token response was empty.");

        _initialized = true;
        Current = new StandInToken(
            dto.AccessToken ?? throw new InvalidOperationException("The StandIn token response has no access_token."),
            dto.UserId ?? userId,
            dto.Name ?? userId,
            dto.NameClaimType ?? "name",
            dto.RoleClaimType ?? "role",
            (dto.Claims ?? new List<ClaimDto>())
                .Select(claim => new StandInClaim(claim.Type ?? string.Empty, claim.Value ?? string.Empty))
                .ToList());
        Changed?.Invoke();
    }

    public void SignOut()
    {
        _initialized = true;
        Current = null;
        Changed?.Invoke();
    }

    /// <summary>The bearer token of the signed-in user; signs in the start user on the first call.</summary>
    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (!_initialized)
                {
                    // Set only after a successful sign-in, so a failed first attempt (server still starting) is retried.
                    if (!string.IsNullOrWhiteSpace(_initialUserId))
                    {
                        await SignInAsync(_initialUserId, cancellationToken);
                    }

                    _initialized = true;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        return Current?.AccessToken;
    }

    private sealed class UsersDto
    {
        public List<UserDto>? Users { get; set; }
    }

    private sealed class UserDto
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }
    }

    private sealed class TokenDto
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        public string? Name { get; set; }

        [JsonPropertyName("name_claim_type")]
        public string? NameClaimType { get; set; }

        [JsonPropertyName("role_claim_type")]
        public string? RoleClaimType { get; set; }

        public List<ClaimDto>? Claims { get; set; }
    }

    private sealed class ClaimDto
    {
        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}
