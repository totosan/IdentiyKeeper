#!/bin/bash
docker build -t totosan/identitykeeper-server:latest -f Dockerfile.Silo .
docker build -t totosan/identitykeeper-client:latest -f Dockerfile.Api .