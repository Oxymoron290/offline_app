@description('Name of the App Service')
param name string

@description('Location for the resource')
param location string = resourceGroup().location

@description('App Service Plan ID')
param appServicePlanId string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Key Vault name for secret references')
param keyVaultName string

@description('Tags for the resource')
param tags object = {}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
      ]
    }
  }
}

output id string = appService.id
output name string = appService.name
output principalId string = appService.identity.principalId
