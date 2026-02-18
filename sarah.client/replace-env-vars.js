const fs = require('fs');
const dotenv = require('dotenv');

dotenv.config();

const environmentFilePath = './src/environments/environment.ts';
const environmentProdFilePath = './src/environments/environment.prod.ts';

const getEnvValue = (keys) => {
  for (const key of keys) {
    if (process.env[key]) {
      console.log(`[DETECT] Found env var: ${key} = ${process.env[key]}`);
      return process.env[key];
    }
  }
  return undefined;
};

const resolveKeycloakIssuer = (defaultIssuer) => {
  // Log all services-related env vars for debugging
  const allEnvKeys = Object.keys(process.env);
  const serviceEnvVars = allEnvKeys.filter(k => k.toLowerCase().includes('services__keycloak'));
  if (serviceEnvVars.length > 0) {
    console.log(`[DEBUG] Found Keycloak-related env vars:`, serviceEnvVars.map(k => `${k}=${process.env[k]}`).join(', '));
  } else {
    console.log(`[DEBUG] No Keycloak-related env vars found. Available services envs:`, allEnvKeys.filter(k => k.toLowerCase().startsWith('services__')));
  }

  const keycloakEndpoint = getEnvValue([
    'services__keycloak__https__0',
    'SERVICES__KEYCLOAK__HTTPS__0',
    'services__keycloak__https__0__url',
    'SERVICES__KEYCLOAK__HTTPS__0__URL'
  ]);

  if (!keycloakEndpoint) {
    console.log(`[OIDC] No Aspire Keycloak endpoint found, using default: ${defaultIssuer}`);
    return defaultIssuer;
  }

  if (keycloakEndpoint.includes('/realms/')) {
    console.log(`[OIDC] Using Aspire endpoint with realm already included: ${keycloakEndpoint}`);
    return keycloakEndpoint;
  }

  const realm = process.env.KEYCLOAK_REALM || 'sarah-realm';
  const issuer = `${keycloakEndpoint.replace(/\/$/, '')}/realms/${realm}`;
  console.log(`[OIDC] Built Keycloak issuer from Aspire endpoint (${keycloakEndpoint}) + realm (${realm}): ${issuer}`);
  return issuer;
};

const replaceEnvVars = (filePath, defaultIssuer) => {
  let content = fs.readFileSync(filePath, 'utf8');
  const issuer = resolveKeycloakIssuer(defaultIssuer);
  content = content.replace(/KEYCLOAK_ISSUER_PLACEHOLDER/g, issuer);
  content = content.replace(/BING_MAPS_KEY_PLACEHOLDER/g, process.env.BING_MAPS_KEY);
  fs.writeFileSync(filePath, content, 'utf8');
  console.log(`[ENV] Updated ${filePath} with Keycloak issuer and Bing Maps key`);
};

console.log('[STARTUP] Running environment variable substitution for frontend...');
replaceEnvVars(environmentFilePath, 'https://localhost:8443/realms/sarah-realm');
replaceEnvVars(environmentProdFilePath, 'https://pi:8443/realms/sarah-realm');
console.log('[STARTUP] Environment variables substitution complete');