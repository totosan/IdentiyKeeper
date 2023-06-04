#!/bin/bash
docker run --rm -p 11111:11111 -p 30000:30000 -e ASPNETCORE_ENVIRONMENT=Development totosan/identitykeeper-server:latest &
docker run --rm -p 8080:5049 -e ASPNETCORE_ENVIRONMENT=Development totosan/identitykeeper-client:latest &