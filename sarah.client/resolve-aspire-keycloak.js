#!/usr/bin/env node
/**
 * Alternative Keycloak endpoint resolver
 * Attempts to fetch endpoint info from Aspire's resource service if direct env vars aren't available
 */

const https = require('https');
const fs = require('fs');

const ASPIRE_RESOURCE_URL = process.env.ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL;

/**
 * Try to fetch endpoint from Aspire's resource service
 * Format: https://localhost:15888/resources/keycloak/endpoints/https
 */
async function fetchFromAspireService() {
  return new Promise((resolve) => {
    if (!ASPIRE_RESOURCE_URL) {
      console.log('[ASPIRE-API] ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL not set');
      resolve(null);
      return;
    }

    console.log('[ASPIRE-API] Attempting to fetch Keycloak endpoint from Aspire service:', ASPIRE_RESOURCE_URL);

    // For self-signed certs from Aspire dev environment
    const agent = new https.Agent({ rejectUnauthorized: false });

    https.get(
      `${ASPIRE_RESOURCE_URL}/resources/keycloak/endpoints/https`,
      { agent },
      (res) => {
        let data = '';
        res.on('data', (chunk) => { data += chunk; });
        res.on('end', () => {
          try {
            const endpoint = JSON.parse(data);
            if (endpoint.address) {
              console.log('[ASPIRE-API] ✓ Got Keycloak endpoint from Aspire API:', endpoint.address);
              resolve(endpoint.address);
            } else {
              console.log('[ASPIRE-API] No address in endpoint response', endpoint);
              resolve(null);
            }
          } catch (e) {
            console.log('[ASPIRE-API] Failed to parse response:', e.message);
            resolve(null);
          }
        });
      }
    ).on('error', (err) => {
      console.log('[ASPIRE-API] Failed to connect to Aspire service:', err.message);
      resolve(null);
    });
  });
}

async function resolveKeycloakEndpoint() {
  // Try direct env var first (most reliable)
  const envEndpoint = process.env.services__keycloak__https__0 || 
                      process.env.SERVICES__KEYCLOAK__HTTPS__0;
  
  if (envEndpoint) {
    console.log('[KEYCLOAK-RESOLVER] Using env var: services__keycloak__https__0');
    return envEndpoint;
  }

  console.log('[KEYCLOAK-RESOLVER] No direct env var found, trying Aspire API...');
  
  // Try Aspire resource service API
  const apiEndpoint = await fetchFromAspireService();
  if (apiEndpoint) {
    return apiEndpoint;
  }

  // Fallback to default
  console.log('[KEYCLOAK-RESOLVER] No dynamic endpoint found, using default');
  return null;
}

// Run if called directly
if (require.main === module) {
  resolveKeycloakEndpoint().then((endpoint) => {
    if (!endpoint) {
      console.log('[KEYCLOAK-RESOLVER] Keycloak endpoint: <using default hardcoded>');
    } else {
      console.log('[KEYCLOAK-RESOLVER] Keycloak endpoint:', endpoint);
      // Log the issuer URL that would be used
      const issuer = endpoint.includes('/realms/') 
        ? endpoint 
        : `${endpoint}/realms/sarah-realm`;
      console.log('[KEYCLOAK-RESOLVER] OIDC Issuer URL:', issuer);
    }
  });
}

module.exports = { resolveKeycloakEndpoint };
