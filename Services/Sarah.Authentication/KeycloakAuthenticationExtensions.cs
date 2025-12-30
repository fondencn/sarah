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
    private static HttpClient? _sharedHttpClient;
    private static readonly object _lock = new object();

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the service collection
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Ensure we have a shared HttpClient (singleton pattern to avoid socket exhaustion)
        if (_sharedHttpClient == null)
        {
            lock (_lock)
            {
                if (_sharedHttpClient == null)
                {
                    if (environment.IsDevelopment())
                    {
                        // In Development, allow self-signed certificates for local Keycloak
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = 
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        };
                        _sharedHttpClient = new HttpClient(handler);
                    }
                    else
                    {
                        // In production, use default certificate validation
                        _sharedHttpClient = new HttpClient();
                    }
                }
            }
        }

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
                        // Use the shared HttpClient to avoid resource leaks
                        var certUri = authority + "/protocol/openid-connect/certs";
                        
                        // Note: Using .Result here is a limitation of the synchronous IssuerSigningKeyResolver
                        // The signing keys are cached by the JWT Bearer middleware, so this is called infrequently
                        var jwks = _sharedHttpClient.GetStringAsync(certUri).Result;
                        return new JsonWebKeySet(jwks).GetSigningKeys();
                    },
                    NameClaimType = "preferred_username"
                };
            });

        services.AddAuthorization();

        return services;
    }
}
