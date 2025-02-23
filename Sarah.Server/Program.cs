using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sarah.Data;
using Sarah.Server.Extensions;

namespace Sarah.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add appsettings.secrets.json to the configuration
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            if (string.IsNullOrEmpty(builder.Configuration["OIDCAuthority"]))
            {
                throw new NotSupportedException("OIDCAuthority is not set in appsettings.json or appsettings.secrets.json.");
            }

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            // Add services to the container.
            builder.Services.AddSarahServices(builder.Configuration);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure JWT Bearer Token Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["OIDCAuthority"];
                options.Audience = builder.Configuration["Jwt:Audience"];
                options.RequireHttpsMetadata = false; // Set to true in production
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        // Retrieve the JWKS from the JWKS endpoint
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                        };
                        var client = new HttpClient(handler);
                        var certUri = builder.Configuration["OIDCAuthority"] + "/protocol/openid-connect/certs";
                        var jwks = client.GetStringAsync(certUri).Result;
                        return new JsonWebKeySet(jwks).GetSigningKeys();
                    },
                    NameClaimType = "preferred_username" // Map preferred_username to User.Identity.Name
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log the error without modifying the response
                        Console.WriteLine("Authentication failed: " + context.Exception.ToString());
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // if (builder.Environment.IsDevelopment())
                        // {
                        //     Console.WriteLine("Token validated successfully.");
                        // }
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        // Log the Authorization header
                        if (builder.Environment.IsDevelopment())
                        {
                            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                            {
                                Console.WriteLine("Authorization Header is missing.");
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            // Add CORS services
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:4200", "https://localhost:4200", "https://pi:4200")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });

            if (builder.Environment.IsProduction())
            {
                var httpsPort = builder.Configuration["HTTPS_BACKEND_PORT"] ?? "7165";
                var certPath = builder.Configuration["CERT_PATH"];
                var certPassword = builder.Configuration["CERT_PASSWORD"];

                if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(certPassword))
                {
                    throw new InvalidOperationException("Certificate path or password is not set in environment variables.");
                }

                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ListenAnyIP(int.Parse(httpsPort), listenOptions =>
                    {
                        listenOptions.UseHttps(certPath, certPassword);
                    });
                });
            }

            var app = builder.Build();

            // Apply pending migrations
            using (var scope = app.Services.CreateScope())
            {
                if (!File.Exists(ApplicationDbContext.DBPath))
                {
                    Console.WriteLine("Database file not found. Creating a new one...");
                    app.Services.GetRequiredService<Sarah.Logging.Logger>().LogInfo("Creating a new database...");
                }
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
                Console.WriteLine("Apply DB migrations to " + ApplicationDbContext.DBPath + "...");
                app.Services.GetRequiredService<Sarah.Logging.Logger>().LogInfo("Applying pending migrations...");
                dbContext.Database.Migrate();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseCors("AllowSpecificOrigins");

            app.UseAuthentication(); // Add authentication middleware
            app.UseAuthorization();  // Add authorization middleware

            app.MapControllers();

            app.Services.GetRequiredService<Sarah.Logging.Logger>().LogInfo("Server started. Enable logging system.");

            app.Run();
        }
    }
}
