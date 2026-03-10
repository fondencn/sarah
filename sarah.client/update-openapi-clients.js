#!/usr/bin/env node

/**
 * Sarah OpenAPI Client Generator
 * 
 * This script downloads OpenAPI specifications from all Sarah microservices
 * and generates TypeScript Angular clients for each service.
 * 
 * Usage:
 *   node update-openapi-clients.js [options]
 * 
 * Options:
 *   --service <name>    Generate client for a specific service only
 *   --save-specs        Save downloaded OpenAPI specs to specs/ directory
 *   --skip-generate     Download specs only, skip client generation
 *   --help             Show this help message
 * 
 * Examples:
 *   node update-openapi-clients.js                    # Generate all clients
 *   node update-openapi-clients.js --save-specs       # Save specs and generate
 *   node update-openapi-clients.js --service device   # Generate device service only
 */

const { exec } = require('child_process');
const http = require('http');
const https = require('https');
const { urlToHttpOptions } = require('url');
const fs = require('fs');
const path = require('path');

const OPENAPI_TOOLS_CONFIG = path.join(__dirname, 'openapitools.json');
let useDockerGenerator = false;
if (fs.existsSync(OPENAPI_TOOLS_CONFIG)) {
    try {
        const config = JSON.parse(fs.readFileSync(OPENAPI_TOOLS_CONFIG, 'utf8'));
        useDockerGenerator = config?.['generator-cli']?.useDocker === true;
    } catch {
        useDockerGenerator = false;
    }
}

// Configuration constants
const MAX_BUFFER_SIZE = 10 * 1024 * 1024; // 10MB for large OpenAPI specs

// ⚠️ DEVELOPMENT TOOL ONLY: Create HTTPS agent with optional relaxed certificate validation
// 
// SECURITY NOTE:
// This script is a development tool that downloads OpenAPI specifications from
// local microservices running on localhost with self-signed SSL certificates.
// 
// To avoid disabling TLS verification by default, certificate validation is kept
// enabled unless the environment variable ALLOW_INSECURE_LOCALHOST_SSL is set
// to "true". This makes any insecure behavior explicit and opt-in.
// 
// This is acceptable because:
// 1. Script runs locally on developer machines, not in production
// 2. Downloads are from localhost (127.0.0.1) services only
// 3. Script is never deployed to production servers
// 4. Alternative would be forcing developers to install CA certificates
// 5. Script displays clear warnings about development-only usage
// 6. Agent is scoped to this script only, doesn't affect other code
//
// Production microservices should use proper SSL certificates from trusted CAs.
const allowInsecureLocalhost = process.env.ALLOW_INSECURE_LOCALHOST_SSL === 'true';
const httpsAgent = new https.Agent({
    // When ALLOW_INSECURE_LOCALHOST_SSL=true, disable certificate verification for localhost dev.
    // Otherwise, use the default secure behavior.
    rejectUnauthorized: !allowInsecureLocalhost
});

// Show warning in development mode
if (process.env.NODE_ENV !== 'production') {
    if (allowInsecureLocalhost) {
        console.warn('⚠️  Development mode: Accepting self-signed SSL certificates (ALLOW_INSECURE_LOCALHOST_SSL=true)');
        console.warn('   This is only for local OpenAPI spec downloads.');
        console.warn('   Do NOT use this setting in production environments.\n');
    } else {
        console.warn('ℹ️  Development mode: Using standard TLS certificate verification.');
        console.warn('   To temporarily allow self-signed localhost certificates,');
        console.warn('   set ALLOW_INSECURE_LOCALHOST_SSL=true when running this script.\n');
    }
}

// Parse command line arguments
const args = process.argv.slice(2);
const options = {
    service: null,
    saveSpecs: args.includes('--save-specs'),
    skipGenerate: args.includes('--skip-generate'),
    help: args.includes('--help') || args.includes('-h')
};

// Get specific service if provided
const serviceIndex = args.indexOf('--service');
if (serviceIndex !== -1 && args[serviceIndex + 1]) {
    options.service = args[serviceIndex + 1];
}

// Configuration for all Sarah microservices
const microservices = [
    {
        name: 'device-service',
        port: 5001,
        title: 'Device Service',
        outputDir: './src/app/services/api/device-service',
        description: 'Manages smart home devices (lamps, sensors, switches)'
    },
    {
        name: 'persons-service',
        port: 5002,
        title: 'Persons Service',
        outputDir: './src/app/services/api/persons-service',
        description: 'Manages persons and user profiles'
    },
    {
        name: 'geofences-service',
        port: 5003,
        title: 'Geofences Service',
        outputDir: './src/app/services/api/geofences-service',
        description: 'Manages geofences and location-based automation'
    },
    {
        name: 'room-service',
        port: 5004,
        title: 'Room Service',
        outputDir: './src/app/services/api/room-service',
        description: 'Manages rooms and device organization'
    },
    {
        name: 'monitoring-service',
        port: 5005,
        title: 'Monitoring Service',
        outputDir: './src/app/services/api/monitoring-service',
        description: 'System monitoring and health checks'
    },
    {
        name: 'rules-service',
        port: 5006,
        title: 'Rules Service',
        outputDir: './src/app/services/api/rules-service',
        description: 'Automation rules engine'
    } //speech-server commented out because no direct access from frontend needed
    // {
    //     name: 'speech-server',
    //     port: 5008,
    //     title: 'Speech Server',
    //     outputDir: './src/app/services/api/speech-server',
    //     description: 'Voice recognition and text-to-speech'
    // }
];

/**
 * Show help message
 */
function showHelp() {
    console.log(`
${'='.repeat(70)}
Sarah OpenAPI Client Generator
${'='.repeat(70)}

This script downloads OpenAPI specifications from all Sarah microservices
and generates TypeScript Angular clients for each service.

Usage:
  node update-openapi-clients.js [options]

Options:
  --service <name>    Generate client for a specific service only
  --save-specs        Save downloaded OpenAPI specs to specs/ directory
  --skip-generate     Download specs only, skip client generation
  --help, -h         Show this help message

Available Services:
${microservices.map(s => `  - ${s.name.padEnd(20)} (Port ${s.port}) - ${s.description}`).join('\n')}

Examples:
  node update-openapi-clients.js
    Generate all clients

  node update-openapi-clients.js --save-specs
    Save OpenAPI specs to specs/ directory and generate clients

  node update-openapi-clients.js --service device-service
    Generate client for device service only

  node update-openapi-clients.js --skip-generate --save-specs
    Download and save OpenAPI specs without generating clients

Prerequisites:
  - All microservices must be running (or at least the ones you want to update)
  - Node.js and npm must be installed
  - @openapitools/openapi-generator-cli will be used if available

${'='.repeat(70)}
`);
}

/**
 * Download OpenAPI spec from a URL
 */
function downloadSpec(url, dest) {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(dest);
        const requestUrl = new URL(url);
        const client = requestUrl.protocol === 'https:' ? https : http;
        
        // Only pass custom https agent for https requests
        const requestOptions = requestUrl.protocol === 'https:'
            ? { ...urlToHttpOptions(requestUrl), agent: httpsAgent }
            : urlToHttpOptions(requestUrl);
        
        client.get(requestOptions, (response) => {
            if (response.statusCode === 302 || response.statusCode === 301) {
                // Follow redirect with same agent; resolve relative Location URLs against the original request URL
                const redirectUrl = new URL(response.headers.location, requestUrl);
                const redirectClient = redirectUrl.protocol === 'https:' ? https : http;
                const redirectOptions = redirectUrl.protocol === 'https:'
                    ? { ...urlToHttpOptions(redirectUrl), agent: httpsAgent }
                    : urlToHttpOptions(redirectUrl);
                redirectClient.get(redirectOptions, (redirectResponse) => {
                    if (redirectResponse.statusCode !== 200) {
                        reject(new Error(`Failed to get '${url}' (${redirectResponse.statusCode})`));
                        return;
                    }
                    redirectResponse.pipe(file);
                    file.on('finish', () => {
                        file.close();
                        resolve();
                    });
                }).on('error', (err) => {
                    fs.unlink(dest, () => reject(err));
                });
            } else if (response.statusCode !== 200) {
                reject(new Error(`Failed to get '${url}' (${response.statusCode})`));
                return;
            } else {
                response.pipe(file);
                file.on('finish', () => {
                    file.close();
                    resolve();
                });
            }
        }).on('error', (err) => {
            fs.unlink(dest, () => reject(err));
        });
        
        file.on('error', (err) => {
            fs.unlink(dest, () => reject(err));
        });
    });
}

/**
 * Generate TypeScript Angular client from OpenAPI spec
 */
function generateClient(specPath, outputDir, serviceName) {
    return new Promise((resolve, reject) => {
        const relativeSpecPath = path.relative(process.cwd(), specPath);
        const relativeOutputDir = path.relative(process.cwd(), outputDir);

        const generatorInputPath = useDockerGenerator
            ? `/local/${relativeSpecPath.replace(/\\/g, '/')}`
            : relativeSpecPath;
        const generatorOutputPath = useDockerGenerator
            ? `/local/${relativeOutputDir.replace(/\\/g, '/')}`
            : relativeOutputDir;

        // Additional properties for Angular 18+ compatibility
        const additionalProps = [
            'ngVersion=18',
            `npmName=${serviceName}`,
            'serviceSuffix=Client',
            'modelSuffix=Model',
            'withInterfaces=true',
            'stringEnums=true',
            'useSingleRequestParameter=false',
            'supportsES6=true',
            'providedInRoot=true'
        ].join(',');
        
        const command = `npx @openapitools/openapi-generator-cli generate -i ${generatorInputPath} -g typescript-angular -o ${generatorOutputPath} --additional-properties=${additionalProps}`;
        
        console.log(`  Generating TypeScript client...`);
        
        exec(command, { maxBuffer: MAX_BUFFER_SIZE }, (error, stdout, stderr) => {
            if (error) {
                console.error(`  ✗ Error: ${error.message}`);
                reject(error);
                return;
            }
            
            // openapi-generator writes most output to stderr, not stdout
            if (stderr && !stderr.includes('Downloaded') && !stderr.includes('writing file')) {
                // Only show stderr if it looks like an actual error
                const lowerStderr = stderr.toLowerCase();
                if (lowerStderr.includes('error') || lowerStderr.includes('failed')) {
                    console.warn(`  ⚠ Warnings: ${stderr.substring(0, 200)}...`);
                }
            }
            
            console.log(`  ✓ Client generated successfully`);
            resolve();
        });
    });
}

/**
 * Process a single microservice
 */
async function processService(service, tempDir, specsDir) {
    const specUrl = `http://localhost:${service.port}/swagger/v1/swagger.json`;
    const tempSpecPath = path.join(tempDir, `${service.name}.json`);
    const savedSpecPath = specsDir ? path.join(specsDir, `${service.name}.json`) : null;
    
    console.log(`\n${'─'.repeat(70)}`);
    console.log(`Processing: ${service.title}`);
    console.log(`${'─'.repeat(70)}`);
    console.log(`  Description: ${service.description}`);
    console.log(`  URL: ${specUrl}`);
    console.log(`  Output: ${service.outputDir}`);
    
    try {
        // Download OpenAPI spec
        console.log(`  Downloading OpenAPI specification...`);
        await downloadSpec(specUrl, tempSpecPath);
        console.log(`  ✓ OpenAPI spec downloaded`);
        
        // Save spec if requested
        if (savedSpecPath) {
            fs.copyFileSync(tempSpecPath, savedSpecPath);
            console.log(`  ✓ Spec saved to: ${savedSpecPath}`);
        }
        
        // Generate client if not skipped
        if (!options.skipGenerate) {
            // Ensure output directory exists
            const outputDirPath = path.dirname(service.outputDir);
            if (!fs.existsSync(outputDirPath)) {
                fs.mkdirSync(outputDirPath, { recursive: true });
            }
            
            await generateClient(tempSpecPath, service.outputDir, service.name);
        } else {
            console.log(`  ⊘ Client generation skipped`);
        }
        
        console.log(`  ✓ ${service.title} completed successfully`);
        return { success: true, service: service.name };
        
    } catch (error) {
        console.error(`  ✗ Failed: ${error.message}`);
        console.error(`  → Make sure ${service.title} is running on port ${service.port}`);
        console.error(`  → Check: http://localhost:${service.port}/swagger`);
        return { success: false, service: service.name, error: error.message };
    }
}

/**
 * Main execution
 */
async function main() {
    // Show help if requested
    if (options.help) {
        showHelp();
        process.exit(0);
    }
    
    console.log('='.repeat(70));
    console.log('Sarah OpenAPI Client Generator');
    console.log('='.repeat(70));
    
    // Filter services if specific service requested
    let servicesToProcess = microservices;
    if (options.service) {
        servicesToProcess = microservices.filter(s => 
            s.name === options.service || s.name === `${options.service}-service`
        );
        
        if (servicesToProcess.length === 0) {
            console.error(`\nError: Service '${options.service}' not found.`);
            console.log('\nAvailable services:');
            microservices.forEach(s => console.log(`  - ${s.name}`));
            process.exit(1);
        }
        
        console.log(`\nMode: Single service (${servicesToProcess[0].name})`);
    } else {
        console.log(`\nMode: All services (${microservices.length} services)`);
    }
    
    if (options.saveSpecs) {
        console.log('Save specs: Enabled');
    }
    if (options.skipGenerate) {
        console.log('Generate clients: Disabled');
    }
    
    // Create temporary directory for swagger files
    const tempDir = path.join(__dirname, '.temp-swagger');
    if (!fs.existsSync(tempDir)) {
        fs.mkdirSync(tempDir);
    }
    
    // Create specs directory if saving specs
    let specsDir = null;
    if (options.saveSpecs) {
        specsDir = path.join(__dirname, 'openapi-specs');
        if (!fs.existsSync(specsDir)) {
            fs.mkdirSync(specsDir);
        }
    }
    
    // Process all services
    const results = [];
    for (const service of servicesToProcess) {
        const result = await processService(service, tempDir, specsDir);
        results.push(result);
    }
    
    // Cleanup temp directory
    if (fs.existsSync(tempDir)) {
        fs.rmSync(tempDir, { recursive: true, force: true });
    }
    
    // Summary
    console.log('\n' + '='.repeat(70));
    console.log('Summary');
    console.log('='.repeat(70));
    
    const successful = results.filter(r => r.success);
    const failed = results.filter(r => !r.success);
    
    console.log(`\n✓ Successful: ${successful.length}/${results.length}`);
    if (successful.length > 0) {
        successful.forEach(r => console.log(`  - ${r.service}`));
    }
    
    if (failed.length > 0) {
        console.log(`\n✗ Failed: ${failed.length}/${results.length}`);
        failed.forEach(r => console.log(`  - ${r.service}: ${r.error}`));
    }
    
    console.log('\n' + '='.repeat(70));
    
    if (failed.length === 0) {
        console.log('✓ All services processed successfully!');
        console.log('='.repeat(70));
        
        if (!options.skipGenerate) {
            console.log('\nNext Steps:');
            console.log('  1. Review generated clients in src/app/services/api/');
            console.log('  2. Import and use the clients in your Angular services');
            console.log('  3. Configure base URLs in environment.ts');
        }
    } else {
        console.log('⚠ Some services failed. See errors above.');
        console.log('='.repeat(70));
        process.exit(1);
    }
}

// Run
main().catch(err => {
    console.error('\n✗ Fatal error:', err);
    process.exit(1);
});
