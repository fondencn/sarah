#!/bin/bash

# Sarah API Client Generator
# This script generates TypeScript API clients for all microservices

echo "======================================================================"
echo "Sarah API Client Generator"
echo "======================================================================"
echo ""
echo "This script will:"
echo "  1. Download OpenAPI specifications from all running microservices"
echo "  2. Generate TypeScript Angular clients using openapi-generator"
echo "  3. Create wrapper services for each client"
echo ""
echo "Prerequisites:"
echo "  - All microservices must be running"
echo "  - Node.js and npm must be installed"
echo "  - @openapitools/openapi-generator-cli must be available"
echo ""
echo "Press Ctrl+C to cancel, or Enter to continue..."
read

# Install openapi-generator-cli if not present
if ! command -v openapi-generator-cli &> /dev/null; then
    echo "Installing @openapitools/openapi-generator-cli..."
    npm install -g @openapitools/openapi-generator-cli
fi

# Run the generator script
node generate-api-clients.js

echo ""
echo "======================================================================"
echo "Next Steps:"
echo "======================================================================"
echo ""
echo "1. Review the generated clients in src/app/services/api/"
echo "2. Create Angular wrapper services for each client"
echo "3. Register the services in your Angular modules"
echo ""
echo "Example usage in an Angular service:"
echo ""
echo "  constructor(private deviceClient: DevicesControllerService) {}"
echo ""
echo "  getDevices() {"
echo "    return this.deviceClient.getLamps();"
echo "  }"
echo ""
