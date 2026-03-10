// Azure Storage Account for photos, videos, and documents

@description('Azure region for the Storage Account.')
param location string

@description('Name of the Storage Account (must be globally unique, 3-24 lowercase alphanumeric).')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Environment name used for tagging (dev, staging, prod).')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Project name used for tagging.')
param projectName string = 'LACO'

@description('Storage Account SKU.')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_ZRS'
  'Standard_RAGRS'
])
param skuName string = 'Standard_LRS'

@description('Name of the blob container for uploads.')
param containerName string = 'uploads'

@description('Allowed origins for CORS (e.g., the Static Web App hostname).')
param corsAllowedOrigins array = []

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: {
    environment: environmentName
    project: projectName
  }
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
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: !empty(corsAllowedOrigins)
        ? [
            {
              allowedOrigins: corsAllowedOrigins
              allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'HEAD', 'OPTIONS']
              allowedHeaders: ['*']
              exposedHeaders: ['*']
              maxAgeInSeconds: 3600
            }
          ]
        : []
    }
    deleteRetentionPolicy: {
      enabled: true
      days: environmentName == 'prod' ? 30 : 7
    }
  }
}

resource uploadsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}

@description('The resource ID of the Storage Account.')
output storageAccountId string = storageAccount.id

@description('The name of the Storage Account.')
output storageAccountName string = storageAccount.name

@description('The primary blob endpoint.')
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('The connection string for the Storage Account.')
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
