#!/bin/bash
# Script to start the application in debug mode with hot reload support

echo "Starting Prototype in Debug Mode with Hot Reload..."
echo "================================================"
echo ""
echo "Note: This mode supports both debugging and hot reload."
echo "To attach debugger in Rider:"
echo "1. Go to Run > Attach to Process"
echo "2. Select 'Docker' connection type"
echo "3. Choose the backend container"
echo "4. Attach to the dotnet process"
echo ""

# Set environment variable to enable modifiable assemblies
export DOTNET_MODIFIABLE_ASSEMBLIES=Debug

# Start with debug compose file
docker compose -f docker-compose.yml -f docker-compose.debug.yml up --build
