# Dynamic Keycloak Endpoint Discovery for Angular Frontend

## Problem

Aspire assigns **dynamic TCP ports** to services each time it starts. With Keycloak, this means:
- Aspire might assign port 35929 on run 1, then 37305 on run 2
- The frontend can't use a hardcoded OIDC issuer URL like `http://localhost:8080/realms/sarah-realm` if the port changes
- Environment variables from Aspire weren't being passed to the npm process

## Solution: Aspire Resource Service Discovery

Instead of hardcoding ports, the frontend now **discovers** Keycloak's actual endpoint at runtime by querying Aspire's resource service API.

### How It Works

1. **Application Bootstrap** (`app.module.ts`):
   - Adds `APP_INITIALIZER` that waits for `AuthService.waitForInitialization()`
   - This ensures auth is set up before any components load

2. **Auth Service** (`auth.service.ts`):
   - Constructor creates an async initialization promise
   - Calls `AspireResourceService.discoverKeycloakIssuer()` to get the dynamic port

3. **Aspire Resource Discovery** (`aspire-resource.service.ts`):
   - Queries Aspire's resource service API at `https://localhost:15888/resources/keycloak/endpoints/https`
   - Aspire responds with the actual endpoint the container is listening on
   - Builds the OIDC issuer URL: `https://localhost:{dynamicPort}/realms/sarah-realm`

4. **Fallback**:
   - If Aspire API is unavailable (offline dev, prod, etc.), falls back to hardcoded `http://localhost:8080/realms/sarah-realm`

## Files Created/Modified

### New Files
- **`src/app/services/aspire-resource.service.ts`** - Queries Aspire resource discovery API

### Modified Files
- **`src/app/services/auth.service.ts`** - Now discovers endpoint before configuring OAuth
- **`src/app/app.module.ts`** - Added `APP_INITIALIZER` to wait for auth init
- **`src/environments/environment.ts`** - Fallback uses port 25443

## How to Use

### In Development with Aspire

Just run Aspire normally (F5 in VS Code):

1. Aspire starts and assigns a random port to Keycloak (e.g., `:36847`)
2. Frontend boots, calls `https://localhost:15888/resources/keycloak/endpoints/https`
3. Gets back `https://localhost:36847` 
4. Configures OIDC to use `https://localhost:36847/realms/sarah-realm`
5. Login works! ✅

### In Development Without Aspire

If Aspire API is unavailable:
- Keycloak must be running on the fallback port: `http://localhost:8080`
- Or you can manually set `environment.keycloakIssuer` to the correct URL

### In Production

Set the Aspire resource service URL appropriately:
- Either through environment: `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`
- Or modify `AspireResourceService.getAspireApiUrl()` 

## Console Logs

Watch for these messages in the browser console:

```
[ASPIRE] Resource discovery service initialized, API URL: https://localhost:15888
[ASPIRE] Discovering endpoint: https://localhost:15888/resources/keycloak/endpoints/https
[ASPIRE] ✓ Discovered keycloak: https://localhost:36847
[AUTH] Initializing authentication service...
[AUTH] ✓ Using Aspire-discovered endpoint: https://localhost:36847/realms/sarah-realm
[AUTH] Configuring OAuth with issuer: https://localhost:36847/realms/sarah-realm
[AUTH] Discovery document loaded
```

## Advantages

✅ Works with Aspire's dynamic port assignment  
✅ Automatic discovery - no manual port configuration needed  
✅ Fallback to hardcoded port if Aspire unavailable  
✅ Clear console logging for debugging  
✅ Resilient - graceful error handling  

## Testing

1. Start Aspire (which may assign a random port)
2. Frontend boots and queries the resource service
3. Check browser console for `[ASPIRE]` and `[AUTH]` messages
4. Verify the OIDC issuer URL matches Aspire's actual port
5. Login should work with dynamically discovered endpoint

## Limitations

- Requires Aspire's resource service to be available (or hardcoded fallback)
- Self-signed certificates from Aspire are handled with `rejectUnauthorized: false` 
- Aspire resource API is `https://localhost:15888` by default (might differ in custom setups)
