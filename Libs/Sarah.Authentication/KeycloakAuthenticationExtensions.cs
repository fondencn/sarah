using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Sarah.Authentication;

/// <summary>
/// Extension methods for configuring JWT authentication with Keycloak
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the service collection
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Aspire injects services__keycloak__http__0 as an env var with the actual URL.
                var keycloakBase = Environment.GetEnvironmentVariable("services__keycloak__http__0")
                    ?? Environment.GetEnvironmentVariable("services__keycloak__https__0");
                var realm = configuration["Keycloak:Realm"] ?? "sarah-realm";
                var authority = keycloakBase != null
                    ? $"{keycloakBase.TrimEnd('/')}/realms/{realm}"
                    : configuration["OIDCAuthority"]
                      ?? $"http://keycloak:8080/realms/{realm}";
                options.Authority = authority;
                
                // HTTP-only setup, so HTTPS metadata check must be disabled
                options.RequireHttpsMetadata = false;
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "preferred_username"
                };
            });

        services.AddAuthorization();

        return services;
    }
}
