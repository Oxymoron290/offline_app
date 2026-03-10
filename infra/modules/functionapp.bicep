// ============================================================================
// Function App Module
// Deploys: Consumption plan Function App (.NET 10 isolated worker) with
// system-assigned managed identity and app settings for Cosmos/Storage/KV.
// ============================================================================

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for the Function App.')
param location string

@description('Resource naming token (appName-env).')
param resourceToken string

@description('Resource tags.')
param tags object

@description('Deployment environment.')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Application Insights connection string.')
param appInsightsConnectionString string

@description('Application Insights instrumentation key.')
param appInsightsInstrumentationKey string

@description('Cosmos DB account endpoint.')
param cosmosDbAccountEndpoint string

@description('Cosmos DB database name.')
param cosmosDbDatabaseName string

@description('Storage account name for blob access.')
param storageAccountName string

@description('Key Vault name for secret references.')
param keyVaultName string

@description('Key Vault URI for secret references.')
param keyVaultUri string

// ── Variables ───────────────────────────────────────────────────────────────

var functionAppName = 'func-${resourceToken}'
var hostingPlanName = 'plan-${resourceToken}'

// Function App requires its own storage account for runtime (triggers, logs)
var sanitizedToken = replace(replace(resourceToken, '-', ''), '_', '')
var funcStorageAccountName = take('stfunc${sanitizedToken}', 24)

// ── Resources ───────────────────────────────────────────────────────────────

// Dedicated storage account for Function App runtime (WebJobs, triggers)
resource funcStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: funcStorageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    encryption: {
      services: {
        blob: { enabled: true, keyType: 'Account' }
        queue: { enabled: true, keyType: 'Account' }
        table: { enabled: true, keyType: 'Account' }
        file: { enabled: true, keyType: 'Account' }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Consumption plan (serverless)
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false // Windows
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v10.0'
      use32BitWorkerProcess: false
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
        ]
        supportCredentials: false
      }
      appSettings: [
        // ── Runtime settings ──
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${funcStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${funcStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${funcStorageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${funcStorageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        // ── Monitoring ──
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        // ── Cosmos DB (uses managed identity via DefaultAzureCredential) ──
        {
          name: 'CosmosDb__AccountEndpoint'
          value: cosmosDbAccountEndpoint
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: cosmosDbDatabaseName
        }
        // ── Storage (blob data access via managed identity) ──
        {
          name: 'BlobStorage__AccountName'
          value: storageAccountName
        }
        {
          name: 'BlobStorage__ServiceUri'
          value: 'https://${storageAccountName}.blob.${environment().suffixes.storage}'
        }
        // ── Key Vault ──
        {
          name: 'KeyVault__VaultUri'
          value: keyVaultUri
        }
        {
          name: 'KeyVault__Name'
          value: keyVaultName
        }
        // ── Environment ──
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : environmentName == 'staging' ? 'Staging' : 'Development'
        }
      ]
    }
  }
}

// ── Role Assignments ────────────────────────────────────────────────────────

// Cosmos DB Built-in Data Contributor role for managed identity
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: 'cosmos-${resourceToken}'
}

var cosmosDbDataContributorRoleId = '00000000-0000-0000-0000-000000000002'

resource cosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosDbAccount
  name: guid(cosmosDbAccount.id, functionApp.id, cosmosDbDataContributorRoleId)
  properties: {
    principalId: functionApp.identity.principalId
    roleDefinitionId: '${cosmosDbAccount.id}/sqlRoleDefinitions/${cosmosDbDataContributorRoleId}'
    scope: cosmosDbAccount.id
  }
}

// Storage Blob Data Contributor for managed identity on the app storage account
resource appStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storageBlobRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appStorageAccount.id, functionApp.id, storageBlobDataContributorRoleId)
  scope: appStorageAccount
  properties: {
    principalId: functionApp.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('Function App name.')
output functionAppName string = functionApp.name

@description('Function App default hostname.')
output defaultHostname string = functionApp.properties.defaultHostName

@description('Function App managed identity principal ID.')
output principalId string = functionApp.identity.principalId

@description('Function App resource ID.')
output functionAppResourceId string = functionApp.id
