param location string = resourceGroup().location
param appName string = 'ship-manifest-logger'
param sqlAdminUsername string = 'dbadmin'

@secure()
param sqlAdminPassword string

// 1. Azure App Service Plan (The underlying server hardware)
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
          name: 'ConnectionStrings__AzureSqlConnection'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};User ID=${sqlAdminUsername};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
      ]
    }
  }
}

// 3. Azure SQL Logical Server
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${appName}-dbserver'
  location: location
  properties: {
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

// 4. Azure SQL Database (Configured for Serverless Compute)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'StationManifests'
  location: location
  sku: {
    name: 'GP_S_Gen5_1' // General Purpose, Serverless, 1 vCore base
    tier: 'GeneralPurpose'
    family: 'Gen5'
  }
  properties: {
    autoPauseDelay: 60 // Pauses compute after 60 minutes of zero activity to save money
    minCapacity: any('0.5')
  }
}

// 5. Firewall Rule to let your Web App talk to the SQL Database
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}