# Aspire Connection String Configuration

## Overview
This document describes the changes made to support .NET Aspire connection string injection for RabbitMQ and PostgreSQL services.

## Changes Made

### 1. RabbitMQ Connection String Support

**File:** `Libs/Sarah.Messaging.RabbitMQ/RabbitMQClient.cs`

**Change:** Updated `ConnectAsync` method to support both Aspire connection strings and traditional configuration.

```csharp
// Priority 1: Check for Aspire connection string (ConnectionStrings:rabbitmq)
var connectionString = _configuration.GetConnectionString("rabbitmq");

if (!string.IsNullOrEmpty(connectionString))
{
    // Use Aspire-provided connection string (format: amqp://username:password@hostname:port)
    factory.Uri = new Uri(connectionString);
}
else
{
    // Fall back to individual configuration values from RabbitMQ section
    factory.HostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
    factory.Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");
    factory.UserName = _configuration["RabbitMQ:UserName"] ?? "guest";
    factory.Password = _configuration["RabbitMQ:Password"] ?? "guest";
}
```

**Benefit:** Services can now use Aspire-generated RabbitMQ credentials automatically while maintaining backward compatibility with the existing `RabbitMQ` configuration section in appsettings.json.

### 2. PostgreSQL Connection String Naming

**File:** `Sarah.AppHost/Program.cs`

**Change:** Added explicit connection string naming when referencing PostgreSQL databases.

**Before:**
```csharp
.WithReference(postgresDevices)
```

**After:**
```csharp
.WithReference(postgresDevices, "PostgresConnection")
```

**Benefit:** Aspire now injects the PostgreSQL connection string with the name `PostgresConnection`, which matches what the microservices expect in their `builder.Configuration.GetConnectionString("PostgresConnection")` calls.

## How Aspire Connection Strings Work

### RabbitMQ
When you use `.WithReference(rabbitmq)` in the AppHost:
- Aspire generates a RabbitMQ connection string in the format: `amqp://username:password@hostname:port`
- This is injected as `ConnectionStrings:rabbitmq` into the microservice's configuration
- The RabbitMQClient now checks for this connection string first before falling back to the `RabbitMQ` section

### PostgreSQL
When you use `.WithReference(postgresDevices, "PostgresConnection")` in the AppHost:
- Aspire generates a PostgreSQL connection string in the format: `Host=hostname;Port=port;Database=dbname;Username=user;Password=pass`
- This is injected as `ConnectionStrings:PostgresConnection` into the microservice's configuration
- Entity Framework Core automatically uses this when you call `builder.Configuration.GetConnectionString("PostgresConnection")`

## Testing

### Verify RabbitMQ Connection
Check the logs when starting a service. You should see:
```
Using Aspire RabbitMQ connection string
Connected to RabbitMQ at {hostname}:{port}
```

### Verify PostgreSQL Connection
If EF Core migrations run successfully on startup, the PostgreSQL connection is working.

## Backward Compatibility

### Development without Aspire
If you run a microservice directly (without Aspire), it will:
- For RabbitMQ: Fall back to the `RabbitMQ` section in appsettings.json
- For PostgreSQL: Use the `ConnectionStrings:PostgresConnection` in appsettings.json

### Existing Configuration
The hardcoded configuration in `appsettings.json` files is preserved as fallback:

**RabbitMQ section (fallback):**
```json
"RabbitMQ": {
  "HostName": "rabbitmq",
  "Port": 5672,
  "UserName": "guest",
  "Password": "guest"
}
```

**PostgreSQL connection (overridden by Aspire):**
```json
"ConnectionStrings": {
  "PostgresConnection": "Host=localhost;Database=devicesdb;Username=postgres;Password=postgres"
}
```

## Security

When using Aspire:
- RabbitMQ credentials are generated automatically and securely passed via environment variables
- PostgreSQL credentials are generated automatically and securely passed via environment variables
- No hardcoded credentials are used in production-like environments

The fallback credentials in appsettings.json should only be used for local development outside of Aspire.

## Affected Services

All microservices that use RabbitMQ and/or PostgreSQL:
- ✅ Sarah.DeviceService.WebApi
- ✅ Sarah.Persons.WebApi
- ✅ Sarah.Geofences.WebApi
- ✅ Sarah.EventProcessing.WebApi
- ✅ Sarah.Monitoring.WebApi
- ✅ Sarah.Rules.WebApi
- ✅ Sarah.LocationServer.WebApi
- ✅ Sarah.SpeechServer.WebApi

## References

- [.NET Aspire PostgreSQL Component](https://learn.microsoft.com/en-us/dotnet/aspire/database/postgresql-component)
- [.NET Aspire RabbitMQ Component](https://learn.microsoft.com/en-us/dotnet/aspire/messaging/rabbitmq-component)
- [Connection String Management in Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-discovery)
