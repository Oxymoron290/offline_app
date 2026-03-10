// ============================================================================
// BlazorHybrid Infrastructure - Main Orchestrator
// Deploys all Azure resources for the Blazor Hybrid MAUI offline-first app.
// ============================================================================

targetScope = 'resourceGroup'

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Deployment environment.')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Base application name used in resource naming.')
@minLength(3)
@maxLength(16)
param appName string

@description('Optional override for the Cosmos DB free-tier flag. Only one free-tier account is allowed per subscription.')
param cosmosDbEnableFreeTier bool = environmentName == 'dev'

@description('Timestamp for deployment tagging (auto-generated).')
param deploymentTimestamp string = utcNow('yyyy-MM-ddTHH:mm:ssZ')

// ── Variables ───────────────────────────────────────────────────────────────

var abbr = {
  dev: 'dev'
  staging: 'stg'
  prod: 'prd'
}

var envAbbr = abbr[environmentName]
var resourceToken = '${appName}-${envAbbr}'

var tags = {
  application: appName
  environment: environmentName
  managedBy: 'bicep'
  deployedAt: deploymentTimestamp
}

// ── Modules ─────────────────────────────────────────────────────────────────

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${envAbbr}'
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-${envAbbr}'
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
  }
}

module cosmosDb 'modules/cosmosdb.bicep' = {
  name: 'cosmosdb-${envAbbr}'
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
    enableFreeTier: cosmosDbEnableFreeTier
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage-${envAbbr}'
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
    environmentName: environmentName
  }
}

module functionApp 'modules/functionapp.bicep' = {
  name: 'functionapp-${envAbbr}'
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
    environmentName: environmentName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    cosmosDbAccountEndpoint: cosmosDb.outputs.accountEndpoint
    cosmosDbDatabaseName: cosmosDb.outputs.databaseName
    storageAccountName: storage.outputs.storageAccountName
    keyVaultName: keyVault.outputs.keyVaultName
    keyVaultUri: keyVault.outputs.keyVaultUri
  }
}

// Grant Function App's managed identity access to Key Vault
module keyVaultAccess 'modules/keyvault.bicep' = {
  name: 'keyvault-access-${envAbbr}'
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
    functionAppPrincipalId: functionApp.outputs.principalId
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('Name of the deployed Function App.')
output functionAppName string = functionApp.outputs.functionAppName

@description('Default hostname of the Function App.')
output functionAppHostname string = functionApp.outputs.defaultHostname

@description('Cosmos DB account endpoint.')
output cosmosDbEndpoint string = cosmosDb.outputs.accountEndpoint

@description('Storage account name.')
output storageAccountName string = storage.outputs.storageAccountName

@description('Key Vault URI.')
output keyVaultUri string = keyVault.outputs.keyVaultUri

@description('Application Insights connection string.')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('Log Analytics workspace ID.')
output logAnalyticsWorkspaceId string = monitoring.outputs.logAnalyticsWorkspaceId
