targetScope = 'resourceGroup'

@description('Environment name')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Base name for all resources')
param baseName string = 'blazorpwa'

@description('SQL admin username')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL admin password')
param sqlAdminPassword string

@description('App Service Plan SKU')
param appServicePlanSku string = 'B1'

@description('SQL Database SKU')
param sqlDatabaseSku string = 'Basic'

@description('Service Bus SKU')
param serviceBusSku string = 'Standard'

@description('Storage account SKU')
param storageSku string = 'Standard_LRS'

// Naming convention
var suffix = '${baseName}-${environment}'
var uniqueSuffix = uniqueString(resourceGroup().id, baseName, environment)

var tags = {
  Environment: environment
  Project: 'BlazorPWA'
  ManagedBy: 'Bicep'
}

// App Service Plan (shared by all app services)
module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'appServicePlan'
  params: {
    name: 'asp-${suffix}'
    location: location
    skuName: appServicePlanSku
    tags: tags
  }
}

// Application Insights
module appInsights 'modules/app-insights.bicep' = {
  name: 'appInsights'
  params: {
    name: 'ai-${suffix}'
    logAnalyticsName: 'law-${suffix}'
    location: location
    tags: tags
  }
}

// Azure SQL
module sql 'modules/sql-server.bicep' = {
  name: 'sqlServer'
  params: {
    serverName: 'sql-${suffix}-${uniqueSuffix}'
    databaseName: 'blazorpwa-db'
    location: location
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    skuName: sqlDatabaseSku
    tags: tags
  }
}

// Azure Service Bus
module serviceBus 'modules/service-bus.bicep' = {
  name: 'serviceBus'
  params: {
    namespaceName: 'sb-${suffix}-${uniqueSuffix}'
    location: location
    skuName: serviceBusSku
    tags: tags
  }
}

// Azure Blob Storage
module storage 'modules/storage-account.bicep' = {
  name: 'storageAccount'
  params: {
    name: 'st${replace(suffix, '-', '')}${take(uniqueSuffix, 6)}'
    location: location
    skuName: storageSku
    tags: tags
  }
}

// Key Vault
module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  params: {
    name: 'kv-${suffix}-${take(uniqueSuffix, 6)}'
    location: location
    tags: tags
    accessPolicies: [
      { principalId: apiAppService.outputs.principalId }
      { principalId: workerAppService.outputs.principalId }
    ]
  }
}

// API App Service
module apiAppService 'modules/app-service-api.bicep' = {
  name: 'apiAppService'
  params: {
    name: 'app-api-${suffix}'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: appInsights.outputs.connectionString
    keyVaultName: 'kv-${suffix}-${take(uniqueSuffix, 6)}'
    tags: tags
  }
}

// Client PWA App Service
module clientAppService 'modules/app-service-client.bicep' = {
  name: 'clientAppService'
  params: {
    name: 'app-client-${suffix}'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    apiBaseUrl: 'https://${apiAppService.outputs.defaultHostName}'
    appInsightsConnectionString: appInsights.outputs.connectionString
    tags: tags
  }
}

// Worker App Service
module workerAppService 'modules/app-service-worker.bicep' = {
  name: 'workerAppService'
  params: {
    name: 'app-worker-${suffix}'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    appInsightsConnectionString: appInsights.outputs.connectionString
    keyVaultName: 'kv-${suffix}-${take(uniqueSuffix, 6)}'
    tags: tags
  }
}

// Outputs
output apiUrl string = 'https://${apiAppService.outputs.defaultHostName}'
output clientUrl string = 'https://${clientAppService.outputs.defaultHostName}'
output sqlServerFqdn string = sql.outputs.serverFqdn
output serviceBusNamespace string = serviceBus.outputs.namespaceName
output storageAccountName string = storage.outputs.name
output keyVaultName string = keyVault.outputs.name
output appInsightsName string = appInsights.outputs.name
