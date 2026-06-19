param location string = 'centralus'
param appName string = 'station-triage'
param existingPlanName string = 'spacestation-api-plan'

@secure()
param databaseUrl string

@secure()
param djangoSecretKey string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' existing = {
  name: existingPlanName
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
      ]
    }
  }
}