// Azure Key Vault for storing connection strings and secrets

@description('Azure region for the Key Vault.')
param location string

@description('Name of the Key Vault (must be globally unique, 3-24 alphanumeric + hyphens).')
@minLength(3)
@maxLength(24)
param keyVaultName string

@description('Environment name used for tagging (dev, staging, prod).')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Project name used for tagging.')
param projectName string = 'LACO'

@description('The tenant ID for the Key Vault.')
param tenantId string = tenant().tenantId

@description('SKU for the Key Vault.')
@allowed(['standard', 'premium'])
param skuName string = 'standard'

@description('Enable soft delete for the Key Vault.')
param enableSoftDelete bool = true

@description('Number of days to retain soft-deleted items.')
param softDeleteRetentionInDays int = 90

@description('Enable purge protection (recommended for prod).')
param enablePurgeProtection bool = false

@description('Access policies for the Key Vault.')
param accessPolicies array = []

@description('SQL Database connection string to store as a secret.')
@secure()
param sqlConnectionString string = ''

@description('Storage Account connection string to store as a secret.')
@secure()
param storageConnectionString string = ''

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: {
    environment: environmentName
    project: projectName
  }
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: skuName
    }
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : null
    enableRbacAuthorization: false
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    accessPolicies: [
      for policy in accessPolicies: {
        tenantId: tenantId
        objectId: policy.objectId
        permissions: {
          secrets: policy.?secretPermissions ?? ['get', 'list']
          keys: policy.?keyPermissions ?? []
          certificates: policy.?certificatePermissions ?? []
        }
      }
    ]
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(sqlConnectionString)) {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: sqlConnectionString
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(storageConnectionString)) {
  parent: keyVault
  name: 'StorageConnectionString'
  properties: {
    value: storageConnectionString
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

@description('The resource ID of the Key Vault.')
output keyVaultId string = keyVault.id

@description('The name of the Key Vault.')
output keyVaultName string = keyVault.name

@description('The URI of the Key Vault.')
output keyVaultUri string = keyVault.properties.vaultUri
