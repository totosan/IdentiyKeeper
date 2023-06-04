resourceGroupName="ORLEANS-ACA"
location="westeurope"
appName="identitykeeper-app"
siloName="identitykeeper-server"
imageApp="totosan/identitykeeper-client:latest"
imageSilo="totosan/identitykeeper-server:latest"
registryUsername="totosan"
registryPassword=$REGISTRY_PWD
environmentName="identitykeeperEnv"

az containerapp env create -g $resourceGroupName -n $environmentName --logs-destination none
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

az containerapp create \
    --resource-group $resourceGroupName \
    --name $appName \
    --image $imageApp \
    --registry-server docker.io \
    --registry-username $registryUsername \
    --registry-password $registryPassword \
    --target-port 5049 \
    --ingress external \
    --environment $environmentName \
    --env-vars "AZURE_STORAGE_CONNECTION_STRING="$AZURE_STORAGE_CONNECTION_STRING