# Running Sarah with .NET Aspire

.NET Aspire is the recommended way to run Sarah. It provides automatic service orchestration, service discovery, and zero manual configuration.

## Why Aspire?

✅ **Zero Manual Configuration** – No need to manually configure Keycloak realm, clients, or users  
✅ **Automatic Service Discovery** – Services automatically find each other  
✅ **Built-in Dashboard** – Visual monitoring of all services and their dependencies  
✅ **Reproducible Setup** – Same configuration every time, perfect for team development  
✅ **Fast Startup** – All services orchestrated efficiently

## Prerequisites

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**
- **[Docker Desktop](https://www.docker.com/)** – Must be running
- **[Node.js 20+](https://nodejs.org/)** – For Angular frontend

## Quick Start

### Step 1: Clone the Repository

```bash
git clone https://github.com/fondencn/sarah.git
cd sarah
```

### Step 2: (Optional) Customize Credentials

The default credentials work out of the box. To customise them, edit `Sarah.AppHost/appsettings.json`:

```json
{
  "Keycloak": {
    "AdminUser": "admin",
    "AdminPassword": "ChangeMe123!",
    "TestUser": {
      "Username": "sarah-admin",
      "Password": "TestPassword123!",
      "Email": "admin@sarah.local"
    }
  }
}
```

For development-specific overrides, edit `Sarah.AppHost/appsettings.Development.json`.

> **Note**: These are default development credentials. Never use them in production.

### Step 3: Start All Services

```bash
dotnet run --project Sarah.AppHost
```

Aspire will:
1. Start the Aspire Dashboard
2. Pull and start Docker containers (Keycloak, RabbitMQ, PostgreSQL)
3. Automatically import the Keycloak realm with pre-configured client and test user
4. Start all microservices with automatic service discovery
5. Start the Angular frontend

**First run**: Takes 60–90 seconds for all services to be ready.

### Step 4: Access the Application

| Service | URL | Credentials |
|---------|-----|-------------|
| **Aspire Dashboard** | http://localhost:15888 | None (dev mode) |
| **Angular Frontend** | http://localhost:4200 | `sarah-admin` / `TestPassword123!` |
| **Keycloak Admin Console** | http://localhost:8080 | `admin` / `ChangeMe123!` |
| **RabbitMQ Management** | http://localhost:15672 | `guest` / `guest` |

### Step 5: Login to Sarah

1. Navigate to http://localhost:4200
2. You will be redirected to Keycloak login
3. Login with `sarah-admin` / `TestPassword123!` (or your custom password)
4. You will be redirected back to Sarah's home page

No manual Keycloak configuration needed – everything is pre-configured.

### Stopping Aspire

Press `Ctrl+C` in the terminal where Aspire is running. All services will stop gracefully.

---

## What Aspire Configures Automatically

| Item | Value |
|------|-------|
| Keycloak Realm | `sarah-realm` |
| Keycloak Client | `sarah-client` (Angular SPA) |
| Redirect URIs | `http://localhost:4200/*` |
| Test user | from `appsettings.json` |
| Access token lifetime | 5 minutes |
| Refresh token lifetime | 30 minutes |
| Service discovery | All services resolve Keycloak, RabbitMQ, and PostgreSQL automatically |

---

## Aspire Service Discovery

`WithReference(resource)` in `Sarah.AppHost/Program.cs` injects `services__<name>__http__0` (and `https__0`) as environment variables into referencing projects. Do not hardcode hostnames – read from config and let Aspire override them.

---

## Troubleshooting

**Keycloak takes a long time to start**  
First startup may take 60–90 seconds to download the image and import the realm. Check Keycloak logs in the Aspire Dashboard for progress.

**Services can't authenticate**  
Ensure Keycloak is fully started before other services. The realm import happens automatically but takes time. Check for "Imported realm" in the Keycloak container logs.

**Port conflicts**  
Ensure ports 4200, 8080, 5001–5008, 5672, 15672, and 15888 are free. Stop any conflicting containers or processes before starting Aspire.

---

For more details on AppHost configuration, see [Sarah.AppHost/README.md](../Sarah.AppHost/README.md).
