#!/bin/bash

# Get the absolute path to the script's directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Change to the solution directory
cd "$SCRIPT_DIR"

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build the Solution
echo "Building solution..."
dotnet build

# "Starting SMS Rate Limiting API..."
cd SMSRateLimiter.API
dotnet run
