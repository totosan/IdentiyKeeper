#!/bin/bash
docker build -t totosan/identitykeeper-server:latest -f Dockerfile.Silo .
docker build -t totosan/identitykeeper-client:latest -f Dockerfile.Api .
docker build -t totosan/identitykeeper-dashboard:latest -f Dockerfile.Dashb .

docker push totosan/identitykeeper-server:latest
docker push totosan/identitykeeper-client:latest
docker push totosan/identitykeeper-dashboard:latest
