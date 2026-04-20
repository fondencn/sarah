using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sarah.Authentication;

public sealed class ClientCredentialsHandler : DelegatingHandler
{
    private static readonly HttpClient _tokenClient = new HttpClient();

    private readonly IConfiguration _config;
    private readonly ILogger<ClientCredentialsHandler> _logger;

    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ClientCredentialsHandler(IConfiguration config, ILogger<ClientCredentialsHandler> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _cachedToken;

            var tokenEndpoint = BuildTokenEndpoint();
            var clientId = _config["Keycloak:ServiceClientId"] ?? "sarah-services";
            var clientSecret = _config["Keycloak:ServiceClientSecret"];

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogError("Keycloak:ServiceClientSecret is not configured; cannot obtain client credentials token");
                return null;
            }

            var response = await _tokenClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            ]), cancellationToken);

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            var accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 60;

            _cachedToken = accessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30);
            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain client credentials token from Keycloak");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private string BuildTokenEndpoint()
    {
        var realm = _config["Keycloak:Realm"] ?? "sarah-realm";

        var keycloakBase = Environment.GetEnvironmentVariable("services__keycloak__http__0")
            ?? Environment.GetEnvironmentVariable("services__keycloak__https__0");

        if (keycloakBase == null)
        {
            var authority = _config["OIDCAuthority"];
            if (authority != null)
            {
                var idx = authority.IndexOf("/realms/", StringComparison.Ordinal);
                keycloakBase = idx >= 0 ? authority[..idx] : authority;
            }
            else
            {
                keycloakBase = "http://keycloak:8080";
            }
        }

        return $"{keycloakBase.TrimEnd('/')}/realms/{realm}/protocol/openid-connect/token";
    }
}
