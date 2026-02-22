const { exec } = require('child_process');
const https = require('https');
const fs = require('fs');
const path = require('path');

// Disable SSL certificate validation for local development
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// Configuration for all microservices
const microservices = [
    {
        name: 'device-service',
        port: 5001,
        title: 'Device Service',
        outputDir: './src/app/services/api/device-service'
    },
    {
        name: 'persons-service',
        port: 5002,
        title: 'Persons Service',
        outputDir: './src/app/services/api/persons-service'
    },
    {
        name: 'geofences-service',
        port: 5003,
        title: 'Geofences Service',
        outputDir: './src/app/services/api/geofences-service'
    },
    {
        name: 'eventprocessing-service',
        port: 5004,
        title: 'Event Processing Service',
        outputDir: './src/app/services/api/eventprocessing-service'
    },
    {
        name: 'monitoring-service',
        port: 5005,
        title: 'Monitoring Service',
        outputDir: './src/app/services/api/monitoring-service'
    },
    {
        name: 'rules-service',
        port: 5006,
        title: 'Rules Service',
        outputDir: './src/app/services/api/rules-service'
    }
];

// Function to download OpenAPI spec
function downloadSpec(url, dest) {
    return new Promise((resolve, reject) => {
        const file = fs.createWriteStream(dest);
        https.get(url, (response) => {
            if (response.statusCode !== 200) {
                reject(new Error(`Failed to get '${url}' (${response.statusCode})`));
                return;
            }
            response.pipe(file);
            file.on('finish', () => {
                file.close();
                resolve();
            });
        }).on('error', (err) => {
            fs.unlink(dest, () => reject(err));
        });
    });
}

// Function to generate client
function generateClient(specPath, outputDir, serviceName) {
    return new Promise((resolve, reject) => {
        const command = `npx @openapitools/openapi-generator-cli generate -i ${specPath} -g typescript-angular -o ${outputDir} --additional-properties=ngVersion=18,npmName=${serviceName},serviceSuffix=Client,modelSuffix=Model,withInterfaces=true`;
        
        console.log(`Generating client for ${serviceName}...`);
        exec(command, (error, stdout, stderr) => {
            if (error) {
                console.error(`Error generating ${serviceName}: ${error.message}`);
                reject(error);
                return;
            }
            if (stderr && !stderr.includes('Downloaded')) {
                console.warn(`Warnings for ${serviceName}: ${stderr}`);
            }
            console.log(`✓ Generated ${serviceName} client`);
            resolve();
        });
    });
}

// Main execution
async function generateAllClients() {
    console.log('='.repeat(60));
    console.log('Sarah API Client Generator');
    console.log('='.repeat(60));
    
    // Create temp directory for swagger files
    const tempDir = path.join(__dirname, '.temp-swagger');
    if (!fs.existsSync(tempDir)) {
        fs.mkdirSync(tempDir);
    }
    
    for (const service of microservices) {
        try {
            const specUrl = `http://localhost:${service.port}/swagger/v1/swagger.json`;
            const specPath = path.join(tempDir, `${service.name}.json`);
            
            console.log(`\nProcessing ${service.title}...`);
            console.log(`  URL: ${specUrl}`);
            
            // Download OpenAPI spec
            await downloadSpec(specUrl, specPath);
            console.log(`  ✓ Downloaded OpenAPI spec`);
            
            // Generate TypeScript client
            await generateClient(specPath, service.outputDir, service.name);
            
        } catch (error) {
            console.error(`✗ Failed to process ${service.title}:`, error.message);
            console.error(`  Make sure ${service.title} is running on port ${service.port}`);
        }
    }
    
    // Cleanup temp directory
    if (fs.existsSync(tempDir)) {
        fs.rmSync(tempDir, { recursive: true, force: true });
    }
    
    console.log('\n' + '='.repeat(60));
    console.log('Generation complete!');
    console.log('='.repeat(60));
}

// Run
generateAllClients().catch(err => {
    console.error('Fatal error:', err);
    process.exit(1);
});
