// Main orchestrator for LACO Blazor WASM PWA infrastructure
// Deploys: Static Web App, SQL Database, Storage Account, Key Vault

targetScope = 'resourceGroup'

// ─── Parameters ────────────────────────────────────────────────────────────────

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod).')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'dev'

@description('Prefix for resource names (lowercase, no special characters).')
@minLength(2)
@maxLength(10)
param resourceNamePrefix string = 'laco'

@description('SQL Server administrator login.')
param sqlAdminLogin string

@description('SQL Server administrator password.')
@secure()
param sqlAdminPassword string

@description('SQL Database SKU name.')
@allowed(['Basic', 'S0', 'S1', 'S2', 'P1', 'P2'])
param databaseSkuName string = 'Basic'

@description('SQL Database SKU tier.')
@allowed(['Basic', 'Standard', 'Premium'])
param databaseSkuTier string = 'Basic'

@description('Azure AD client ID for authentication.')
param azureAdClientId string = ''

@description('Azure AD tenant ID for authentication.')
param azureAdTenantId string = ''

@description('Object IDs that should have access to Key Vault secrets.')
param keyVaultAccessObjectIds array = []

// ─── Variables ─────────────────────────────────────────────────────────────────

var envSuffix = environmentName == 'prod' ? '' : '-${environmentName}'
var baseName = '${resourceNamePrefix}${envSuffix}'

var staticWebAppName = '${baseName}-swa'
var sqlServerName = '${baseName}-sql'
var sqlDatabaseName = '${resourceNamePrefix}-db'
// Storage account names: lowercase alphanumeric only, max 24 chars
var storageAccountName = toLower(replace('${resourceNamePrefix}${environmentName}stor', '-', ''))
var keyVaultName = '${baseName}-kv'

var storageSkuName = environmentName == 'prod' ? 'Standard_GRS' : 'Standard_LRS'
var enablePurgeProtection = environmentName == 'prod'

var accessPolicies = [
  for objectId in keyVaultAccessObjectIds: {
    objectId: objectId
    secretPermissions: ['get', 'list', 'set', 'delete']
  }
]

// ─── Modules ───────────────────────────────────────────────────────────────────

module storageAccount 'modules/storage-account.bicep' = {
  name: 'deploy-storage-account'
  params: {
    location: location
    storageAccountName: storageAccountName
    environmentName: environmentName
    skuName: storageSkuName
    containerName: 'uploads'
  }
}

module sqlDatabase 'modules/sql-database.bicep' = {
  name: 'deploy-sql-database'
  params: {
    location: location
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    environmentName: environmentName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    databaseSkuName: databaseSkuName
    databaseSkuTier: databaseSkuTier
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    location: location
    keyVaultName: keyVaultName
    environmentName: environmentName
    enablePurgeProtection: enablePurgeProtection
    accessPolicies: accessPolicies
    sqlConnectionString: 'Server=tcp:${sqlDatabase.outputs.sqlServerFqdn},1433;Initial Catalog=${sqlDatabase.outputs.sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    storageConnectionString: storageAccount.outputs.connectionString
  }
}

module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'deploy-static-web-app'
  params: {
    location: location
    staticWebAppName: staticWebAppName
    environmentName: environmentName
    sqlConnectionString: 'Server=tcp:${sqlDatabase.outputs.sqlServerFqdn},1433;Initial Catalog=${sqlDatabase.outputs.sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
    storageConnectionString: storageAccount.outputs.connectionString
    keyVaultUri: keyVault.outputs.keyVaultUri
    azureAdClientId: azureAdClientId
    azureAdTenantId: azureAdTenantId
  }
}

// ─── Outputs ───────────────────────────────────────────────────────────────────

@description('Static Web App default hostname.')
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname

@description('Static Web App URL.')
output staticWebAppUrl string = 'https://${staticWebApp.outputs.defaultHostname}'

@description('Static Web App deployment token for CI/CD.')
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken

@description('SQL Server fully qualified domain name.')
output sqlServerFqdn string = sqlDatabase.outputs.sqlServerFqdn

@description('SQL Database name.')
output sqlDatabaseName string = sqlDatabase.outputs.sqlDatabaseName

@description('Storage Account name.')
output storageAccountName string = storageAccount.outputs.storageAccountName

@description('Storage Account blob endpoint.')
output storageBlobEndpoint string = storageAccount.outputs.primaryBlobEndpoint

@description('Key Vault name.')
output keyVaultName string = keyVault.outputs.keyVaultName

@description('Key Vault URI.')
output keyVaultUri string = keyVault.outputs.keyVaultUri
