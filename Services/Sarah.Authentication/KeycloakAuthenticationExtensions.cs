using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;

namespace Sarah.Authentication;

/// <summary>
/// Extension methods for configuring JWT authentication with Keycloak
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    // Lazy initialization ensures thread-safe singleton creation
    private static readonly Lazy<HttpClient> _httpClientDev = new Lazy<HttpClient>(() =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(30); // Prevent indefinite blocking
        return client;
    });

    private static readonly Lazy<HttpClient> _httpClientProd = new Lazy<HttpClient>(() =>
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30); // Prevent indefinite blocking
        return client;
    });

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the service collection
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Select appropriate HttpClient based on environment
        var httpClient = environment.IsDevelopment() ? _httpClientDev.Value : _httpClientProd.Value;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var authority = configuration["OIDCAuthority"] 
                    ?? "https://keycloak:8443/realms/sarah-realm";
                var audience = configuration["Jwt:Audience"] ?? "account";
                
                options.Authority = authority;
                options.Audience = audience;
                
                // Only disable HTTPS metadata check in development
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        // Use the shared HttpClient with timeout to avoid resource leaks
                        var certUri = authority + "/protocol/openid-connect/certs";
                        
                        // Note: Using .Result here is a limitation of the synchronous IssuerSigningKeyResolver
                        // The signing keys are cached by the JWT Bearer middleware, so this is called infrequently
                        // Timeout is configured on HttpClient to prevent indefinite blocking
                        try
                        {
                            var jwks = httpClient.GetStringAsync(certUri).Result;
                            return new JsonWebKeySet(jwks).GetSigningKeys();
                        }
                        catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                        {
                            throw new InvalidOperationException(
                                $"Timeout retrieving JWKS from {certUri}. Ensure Keycloak is accessible.", 
                                ex.InnerException);
                        }
                    },
                    NameClaimType = "preferred_username"
                };
            });

        services.AddAuthorization();

        return services;
    }
}
