// Azure Static Web Apps resource for hosting Blazor WASM PWA with integrated Azure Functions API

@description('Azure region for the Static Web App resource.')
param location string

@description('Name of the Static Web App resource.')
param staticWebAppName string

@description('Environment name used for tagging (dev, staging, prod).')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Project name used for tagging.')
param projectName string = 'LACO'

@description('SKU for the Static Web App.')
@allowed(['Free', 'Standard'])
param skuName string = 'Standard'

@description('SQL Database connection string to configure in app settings.')
@secure()
param sqlConnectionString string = ''

@description('Storage Account connection string to configure in app settings.')
@secure()
param storageConnectionString string = ''

@description('Key Vault URI for secrets reference.')
param keyVaultUri string = ''

@description('Azure AD client ID for authentication.')
param azureAdClientId string = ''

@description('Azure AD tenant ID for authentication.')
param azureAdTenantId string = ''

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  tags: {
    environment: environmentName
    project: projectName
  }
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource staticWebAppSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    SQL_CONNECTION_STRING: sqlConnectionString
    STORAGE_CONNECTION_STRING: storageConnectionString
    KEY_VAULT_URI: keyVaultUri
    AZURE_AD_CLIENT_ID: azureAdClientId
    AZURE_AD_TENANT_ID: azureAdTenantId
    ENVIRONMENT: environmentName
  }
}

@description('The default hostname of the Static Web App.')
output defaultHostname string = staticWebApp.properties.defaultHostname

@description('The resource ID of the Static Web App.')
output staticWebAppId string = staticWebApp.id

@description('The name of the Static Web App.')
output staticWebAppName string = staticWebApp.name

@description('The API key for CI/CD deployment.')
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
