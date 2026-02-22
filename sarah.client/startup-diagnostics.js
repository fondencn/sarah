#!/usr/bin/env node
// This script runs BEFORE any other npm scripts and captures the environment state
// Run with: node startup-diagnostics.js

const fs = require('fs');
const path = require('path');

const logDir = path.join(__dirname, '.startup-logs');
if (!fs.existsSync(logDir)) {
  fs.mkdirSync(logDir, { recursive: true });
}

const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
const logFile = path.join(logDir, `startup-env-${timestamp}.json`);

const envDump = {
  timestamp: new Date().toISOString(),
  allEnvVars: process.env,
  servicesEnvVars: Object.entries(process.env)
    .filter(([key]) => key.toLowerCase().startsWith('services__'))
    .reduce((acc, [key, val]) => { acc[key] = val; return acc; }, {}),
  node: {
    version: process.version,
    execPath: process.execPath,
    cwd: process.cwd(),
    argv: process.argv
  }
};

fs.writeFileSync(logFile, JSON.stringify(envDump, null, 2));

console.log(`[STARTUP] Environment state saved to ${logFile}`);

if (Object.keys(envDump.servicesEnvVars).length === 0) {
  console.log('[STARTUP] ⚠️ WARNING: No services__ environment variables found!');
  console.log('[STARTUP] Aspire may not be passing environment variables to the npm process.');
  console.log('[STARTUP] You can manually check the log file: cat .startup-logs/startup-env-*.json');
} else {
  console.log('[STARTUP] ✓ Found services environment variables:');
  Object.entries(envDump.servicesEnvVars).forEach(([key, val]) => {
    console.log(`[STARTUP]   ${key} = ${val}`);
  });
}
