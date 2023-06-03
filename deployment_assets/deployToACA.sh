resourceGroupName="ORLEANS-ACA"
location="westeurope"
appName="identitykeeper-app"
image="totosan/identitykeeper:latest"
registryUsername="totosan"
registryPassword="QxT638_"
environmentName="identitykeeperEnv"

az containerapp env create -g $resourceGroupName -n $environmentName --logs-destination none
az containerapp create \
    --resource-group $resourceGroupName \
    --name $appName \
    --image $image \
    --registry-server docker.io \
    --registry-username $registryUsername \
    --registry-password $registryPassword \
    --exposed-port 5049 \
    --target-port 5049 \
    --ingress external \
    --environment $environmentName \
    --env-vars "AZURE_STORAGE_CONNECTION_STRING="$AZURE_STORAGE_CONNECTION_STRING