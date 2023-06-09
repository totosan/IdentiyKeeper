resourceGroupName="ORLEANS-ACA"
location="westeurope"
appName="identitykeeper-app"
siloName="identitykeeper-server"
dashboardName="identitykeeper-dashboard"
imageApp="totosan/identitykeeper-client:latest"
imageSilo="totosan/identitykeeper-server:latest"
imageDashboard="totosan/identitykeeper-dashboard:latest"
registryUsername="totosan"
registryPassword=$DOCKER_PASSWORD
environmentName="identitykeeperEnv"

az containerapp env create -g $resourceGroupName -n $environmentName --logs-destination none
# silo
az containerapp create \
    --resource-group $resourceGroupName \
    --name $siloName \
    --image $imageSilo \
    --registry-server docker.io \
    --registry-username $registryUsername \
    --registry-password $registryPassword \
    --environment $environmentName \
    --min-replicas 1 \
    --env-vars "AZURE_STORAGE_CONNECTION_STRING="$AZURE_STORAGE_CONNECTION_STRING

# app
az containerapp create \
    --resource-group $resourceGroupName \
    --name $appName \
    --image $imageApp \
    --registry-server docker.io \
    --registry-username $registryUsername \
    --registry-password $registryPassword \
    --target-port 80 \
    --ingress external \
    --environment $environmentName \
    --env-vars "AZURE_STORAGE_CONNECTION_STRING="$AZURE_STORAGE_CONNECTION_STRING

    # Dashboard
az containerapp create \
    --resource-group $resourceGroupName \
    --name $dashboardName \
    --image $imageDashboard \
    --registry-server docker.io \
    --registry-username $registryUsername \
    --registry-password $registryPassword \
    --target-port 80 \
    --ingress external \
    --environment $environmentName \
    --env-vars "AZURE_STORAGE_CONNECTION_STRING="$AZURE_STORAGE_CONNECTION_STRING