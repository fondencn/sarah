using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Bearer Token Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["OIDCAuthority"] 
            ?? "https://keycloak:8443/realms/sarah-realm";
        var audience = builder.Configuration["Jwt:Audience"] ?? "account";
        
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = false; // Set to true in production with proper certificates
        
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
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                var client = new HttpClient(handler);
                var certUri = authority + "/protocol/openid-connect/certs";
                var jwks = client.GetStringAsync(certUri).Result;
                return new JsonWebKeySet(jwks).GetSigningKeys();
            },
            NameClaimType = "preferred_username"
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
