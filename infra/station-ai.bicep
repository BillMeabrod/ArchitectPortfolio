param location string = 'centralus'
param appName string = 'station-ai'
param existingPlanName string = 'spacestation-api-plan'

@secure()
param blobStorageConnection string

@secure()
param googleApiKey string

// Reference the existing shared Linux plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' existing = {
  name: existingPlanName
}

// Azure Web App (StationAI Hexagonal API, .NET 10)
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-api'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET|10.0'
      appSettings: [
        {
          name: 'ConnectionStrings__BlobStorageConnection'
          value: blobStorageConnection
        }
        {
          name: 'GOOGLE_API_KEY'
          value: googleApiKey
        }
      ]
    }
  }
}
