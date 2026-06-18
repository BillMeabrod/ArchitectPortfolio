param location string = resourceGroup().location
param appName string = 'ship-manifest-logger'

@secure()
param azureStorageConnection string

// 1. Azure App Service Plan (The underlying server hardware)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'spacestation-api-plan'
  location: location
  sku: {
    name: 'F1' // Free Tier
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// 2. Azure Web App (The container running your .NET Web API)
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-api'
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET|10.0' // Matches your .NET 10 project
      appSettings: [
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Data Source=/home/data/StationManifests.db'
        }
        {
          name: 'ConnectionStrings__AzureStorageConnection'
          value: azureStorageConnection
        }
      ]
    }
  }
}
