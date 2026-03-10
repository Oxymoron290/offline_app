@description('Name of the App Service')
param name string

@description('Location for the resource')
param location string = resourceGroup().location

@description('App Service Plan ID')
param appServicePlanId string

@description('API base URL')
param apiBaseUrl string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Tags for the resource')
param tags object = {}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApiBaseUrl'
          value: apiBaseUrl
        }
      ]
    }
  }
}

output id string = appService.id
output name string = appService.name
output defaultHostName string = appService.properties.defaultHostName
