# Frontend OIDC Endpoint Resolution - Current Status

## What We Discovered

**The Problem**: When you run `npm start`, the Node.js process is NOT receiving Aspire's injected environment variables (`services__keycloak__https__0`, etc.).

### Evidence
1. Running `node replace-env-vars.js` manually shows: `[DEBUG] No Keycloak-related env vars found`
2. The `[OIDC] No Aspire Keycloak endpoint found` message appears, falling back to hardcoded value
3. But Aspire **assigns dynamic ports** each time it starts (37305, 38099, 35929, etc.)

### Root Cause
Aspire's `.WithReference(keycloak)` for JavaScript apps may not properly inject `services__` environment variables into the npm process like it does for .NET applications.

## Current Situation

### What's Working
- ✅ Frontend HTTPS-only configuration
- ✅ Keycloak HTTPS-only setup in Aspire
- ✅ Dynamic endpoint injection mechanism (code is ready)
- ✅ Fallback to hardcoded default if env vars missing

### What's Not Working  
- ❌ Aspire env vars not being passed to npm/Node process
- ❌ Frontend can't automatically adapt to Keycloak's dynamic port assignments

## What We Need to Test

To determine if this is a configuration issue or a limitation of Aspire:

### Step 1: Run Aspire
Start your Aspire AppHost from VS Code (F5 or the Run button)

### Step 2: Check Generated Logs
Once `npm start` has run (look for the typical webpack output), check for diagnostics:

```bash
cd /home/cf/git/sarah/sarah.client

# Check if environment logs were created
ls -lrt .startup-logs/

# Check the latest startup trace
cat $(ls -t .startup-logs/startup-env-*.json | head -1) | jq '.servicesEnvVars'
```

### Step 3: Interpret Results

**If you see:**
```json
{
  "services__keycloak__https__0": "https://localhost:37305",
  ...
}
```
→ **Aspire IS working**. The env vars are being set. The npm process should be receiving them.

**If you see:**
```json
{}
```
or the directory doesn't exist → **Aspire is NOT passing env vars to npm process**. This is the current suspected issue.

## Recommended Solutions

### Option A: Verify Current Configuration Works (Recommended First Step)
Even if env vars aren't injecting dynamically, the hardcoded fallback (`https://localhost:8443/realms/sarah-realm`) might still work if:
- You use Docker compose or configure Keycloak to be on that port
- Or there's a proxy/reverse proxy routing requests correctly

**Test**: Are you able to log in to the frontend currently? If yes, the current config works and we don't need to change anything.

### Option B: Force Hardcoded Keycloak Port in Aspire
Modify `Sarah.AppHost/Program.cs` to use a fixed port instead of dynamic:

```csharp
var keycloak = builder.AddKeycloak("keycloak")
    // ... existing settings ...
    .WithHttpsEndpoint(port: 8443, targetPort: 8443, name: "https"); // Fixed port
```

Then the hardcoded default `https://localhost:8443/realms/sarah-realm` will always work.

### Option C: Use Hostname Resolution Instead of Port
If running in Docker/containers, use the service hostname:
```typescript
keycloakIssuer: 'KEYCLOAK_ISSUER_PLACEHOLDER'
// Injects: https://keycloak:8443/realms/sarah-realm  (uses hostname not port)
```

This works in containerized environments because `keycloak` is resolvable within the docker network.

### Option D: Implement Aspire Resource Service Discovery
Create a fallback mechanism that queries Aspire's resource service API at:
`https://localhost:15888/resources/keycloak/endpoints/https`

This would work even if env vars aren't injected (already added `resolve-aspire-keycloak.js` for this).

## Next Steps

1. **Start Aspire and run the diagnostics** to see if env vars are actually being passed
2. **Try logging in** to the frontend to see if current hardcoded config works anyway
3. **Share your findings** with the results of the diagnostic checks
4. Based on results, implement one of the solutions above

## Files Added for Diagnosis

- `startup-diagnostics.js` - Captures env state at Node startup
- `debug-env.js` - Shows filtered services env vars
- `resolve-aspire-keycloak.js` - Alternative resolver using Aspire API (not yet integrated)
- `check-aspire-env.sh` - Quick bash script to check env vars
- `ASPIRE_ENV_VAR_DIAGNOSIS.md` - Detailed diagnosis guide

## Key Configuration Files

- `replace-env-vars.js` - Main injection script (runs in npm prestart)
- `package.json` - Updated prestart with diagnostics
- `src/environments/environment.ts` - Uses `KEYCLOAK_ISSUER_PLACEHOLDER` for dynamic injection
- `Sarah.AppHost/Program.cs` - `.WithReference(keycloak)` configuration

---

**Status**: Awaiting diagnostic results to determine if this is an Aspire configuration issue or a feature limitation.
