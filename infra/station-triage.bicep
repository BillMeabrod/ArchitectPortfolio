param location string = 'centralus'
param appName string = 'station-triage'
param existingPlanName string = 'spacestation-api-plan'

@secure()
param databaseUrl string

@secure()
param djangoSecretKey string

@secure()
param azureStorageConnection string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' existing = {
  name: existingPlanName
}

resource triageFunctionsPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'ASP-StationTriage-Functions'
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-web'
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'PYTHON|3.14'
      appCommandLine: 'python manage.py migrate --noinput && python manage.py collectstatic --noinput && gunicorn station_triage.wsgi'
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
        {
          name: 'ORYX_DISABLE_COMPRESS_OUTPUT'
          value: 'true'
        }
        {
          name: 'CSRF_TRUSTED_ORIGINS'
          value: 'https://${appName}-web.azurewebsites.net'
        }
      ]
    }
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-functions'
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: triageFunctionsPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: azureStorageConnection
        }
        {
          name: 'DATABASE_URL'
          value: databaseUrl
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          value: azureStorageConnection
        }
      ]
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: 'https://spacestationstorage.blob.${environment().suffixes.storage}/app-package-station-triage-functions'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 100
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'python'
        version: '3.13'
      }
    }
  }
}