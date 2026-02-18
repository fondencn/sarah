#!/usr/bin/env node

console.log('[DEBUG-ENV] === All Environment Variables ===');
console.log('[DEBUG-ENV] (showing only variables that might be from Aspire)');
console.log('');

const envKeys = Object.keys(process.env).sort();
const aspirePatterns = [
  'services__',
  'keycloak',
  'aspire',
  'aspnetcore',
  'port',
  'hostname',
  'host'
];

let foundAspire = false;
for (const key of envKeys) {
  if (aspirePatterns.some(p => key.toLowerCase().includes(p))) {
    console.log(`[DEBUG-ENV] ${key} = ${process.env[key]}`);
    foundAspire = true;
  }
}

if (!foundAspire) {
  console.log('[DEBUG-ENV] ⚠️ NO ASPIRE/SERVICES ENV VARS FOUND');
  console.log('[DEBUG-ENV] This means Aspire is not passing environment variables to the npm process');
  console.log('[DEBUG-ENV]');
  console.log('[DEBUG-ENV] First 10 env vars for reference:');
  envKeys.slice(0, 10).forEach(key => {
    console.log(`[DEBUG-ENV] ${key} = ${process.env[key]}`);
  });
}

console.log('');
console.log(`[DEBUG-ENV] Total env vars: ${envKeys.length}`);
