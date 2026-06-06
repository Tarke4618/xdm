#!/bin/bash
set -e

echo "=== XDM REBIRTH LOCAL BUILDER ==="

# Clean old publish dirs
echo "Cleaning output folders..."
rm -rf publish/ dist/

# Restore dependencies
echo "Restoring NuGet dependencies..."
dotnet restore app/XDM/XDM.sln

# Build solution
echo "Compiling projects in Release mode..."
dotnet build app/XDM/XDM.sln -c Release --no-restore

# Publish application components
echo "Publishing desktop app..."
dotnet publish app/XDM/XDM.Desktop/XDM.Desktop.csproj -c Release --no-restore -o publish/app

echo "Publishing native messaging host..."
dotnet publish app/XDM/XDM.MessagingHost/XDM.MessagingHost.csproj -c Release --no-restore -o publish/host

echo "Packaging browser extension..."
mkdir -p dist
zip -q -r dist/xdm-browser-monitor.zip app/XDM/chrome-extension/

echo "=== BUILD COMPLETE ==="
echo "Published binaries available in: publish/"
echo "Browser extension archive available in: dist/"
