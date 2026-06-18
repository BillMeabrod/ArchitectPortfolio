param location string = resourceGroup().location
param appName string = 'station-triage'

@secure()
param databaseUrl string

@secure()
param djangoSecretKey string

// 1. Azure App Service Plan (Linux, Python workload)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: 'F1' // Free Tier
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// 2. Azure Web App (Django app, served via Gunicorn by App Service's Python runtime)
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-web'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'PYTHON|3.14'
      appSettings: [
        {
          name: 'DATABASE_URL'
          value: databaseUrl
        }
        {
          name: 'DJANGO_SECRET_KEY'
          value: djangoSecretKey
        }
        {
          name: 'DEBUG'
          value: 'False'
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'true'
        }
      ]
    }
  }
}
