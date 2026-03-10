// ============================================================================
// Storage Module
// Deploys: Storage Account with blob containers (photos, videos, documents)
// and CORS rules for media upload from the MAUI client.
// ============================================================================

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for the storage account.')
param location string

@description('Resource naming token (appName-env).')
param resourceToken string

@description('Resource tags.')
param tags object

@description('Deployment environment.')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Storage account SKU.')
@allowed(['Standard_LRS', 'Standard_GRS', 'Standard_RAGRS', 'Standard_ZRS'])
param skuName string = environmentName == 'prod' ? 'Standard_GRS' : 'Standard_LRS'

@description('Allowed origins for CORS (comma-separated). Empty string allows all origins in dev.')
param corsAllowedOrigins array = environmentName == 'dev' ? ['*'] : ['https://*.azurewebsites.net']

@description('Blob soft-delete retention in days.')
@minValue(1)
@maxValue(365)
param softDeleteRetentionDays int = 7

// ── Variables ───────────────────────────────────────────────────────────────

// Storage account names: 3-24 chars, lowercase alphanumeric only
var sanitizedToken = toLower(replace(replace(resourceToken, '-', ''), '_', ''))
var storageAccountName = take('st${sanitizedToken}0001', 24)

var blobContainers = [
  'photos'
  'videos'
  'documents'
]

// ── Resources ───────────────────────────────────────────────────────────────

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: skuName
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    encryption: {
      services: {
        blob: {
          enabled: true
          keyType: 'Account'
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: corsAllowedOrigins
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'HEAD', 'OPTIONS']
          allowedHeaders: ['*']
          exposedHeaders: ['Content-Length', 'Content-Type', 'x-ms-request-id', 'x-ms-blob-content-md5']
          maxAgeInSeconds: 3600
        }
      ]
    }
    deleteRetentionPolicy: {
      enabled: true
      days: softDeleteRetentionDays
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: softDeleteRetentionDays
    }
  }
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for containerName in blobContainers: {
    parent: blobServices
    name: containerName
    properties: {
      publicAccess: 'None'
    }
  }
]

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('Storage account name.')
output storageAccountName string = storageAccount.name

@description('Storage account resource ID.')
output storageAccountId string = storageAccount.id

@description('Primary blob endpoint.')
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('Storage account resource ID for role assignments.')
output storageResourceId string = storageAccount.id
