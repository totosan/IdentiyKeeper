#!/bin/bash

if [ "$1" == "dev" ]; then
  ASPNETCORE_ENVIRONMENT=Development
    docker run --rm -p 11111:11111 -p 30000:30000 -e ASPNETCORE_ENVIRONMENT=Development totosan/identitykeeper-server:latest &
    docker run --rm -p 8080:5049 -e ASPNETCORE_ENVIRONMENT=Development totosan/identitykeeper-client:latest &
else
    docker run --rm -p 11111:11111 -p 30000:30000 -e AZURE_STORAGE_CONNECTION_STRING=$AZURE_STORAGE_CONNECTION_STRING totosan/identitykeeper-server:latest &
    docker run --rm -p 8080:5049 -e AZURE_STORAGE_CONNECTION_STRING=$AZURE_STORAGE_CONNECTION_STRING totosan/identitykeeper-client:latest &
fi
