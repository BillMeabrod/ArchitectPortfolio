param location string = 'centralus'
param appName string = 'station-dashboard'

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: appName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      appLocation: '/src/StationDashboard'
      outputLocation: 'dist'
    }
  }
}

output defaultHostname string = staticWebApp.properties.defaultHostname