#!/bin/bash
# Run this while Aspire is running to check if services env vars are being passed

echo "==== Aspire Environment Variable Check ===="
echo ""
echo "Checking for Aspire services environment variables..."
echo ""

# Check if any services__ env vars exist
SERVICES_VARS=$(env | grep -i "services__keycloak" || echo "")

if [ -z "$SERVICES_VARS" ]; then
    echo "❌ NO services__keycloak env vars found in current shell"
    echo ""
    echo "This means Aspire's environment variables are NOT inherited by this shell."
    echo "Next, we'll check if they exist in the Node process when npm start runs..."
    echo ""
    echo "To proceed:"
    echo "1. Start the app with: npm start (from this directory)"
    echo "2. Once npm starts successfully, look for [STARTUP] messages in the terminal"
    echo "3. Check the .startup-logs/ directory for the captured environment state"
    echo ""
else
    echo "✓ Found services env vars:"
    echo "$SERVICES_VARS"
    echo ""
    echo "Aspire IS passing env vars. Try:"
    echo "  npm start"
fi

echo ""
echo "==== Checking .startup-logs/ directory ===="
if [ -d .startup-logs ]; then
    ls -lrt .startup-logs/
    echo ""
    echo "Most recent log:"
    LATEST=$(ls -t .startup-logs/startup-env-*.json 2>/dev/null | head -1)
    if [ ! -z "$LATEST" ]; then
        echo "File: $LATEST"
        echo ""
        echo "services__ vars found in that run:"
        cat "$LATEST" | jq '.servicesEnvVars' 2>/dev/null || cat "$LATEST" | grep "services__keycloak"
    fi
else
    echo "No .startup-logs/ directory yet. Start npm to create it."
fi
