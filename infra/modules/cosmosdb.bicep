// ============================================================================
// Cosmos DB Module
// Deploys: Account, Database (BlazorHybridDb), and containers for
// entities, documents, media, and sync-log.
// ============================================================================

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for the Cosmos DB account.')
param location string

@description('Resource naming token (appName-env).')
param resourceToken string

@description('Resource tags.')
param tags object

@description('Enable the Cosmos DB free tier (one per subscription).')
param enableFreeTier bool = false

@description('Default consistency level for the Cosmos DB account.')
@allowed(['Eventual', 'ConsistentPrefix', 'Session', 'BoundedStaleness', 'Strong'])
param defaultConsistencyLevel string = 'Session'

@description('Default TTL in seconds for the sync-log container. -1 = off, 0 = on with no default, >0 = seconds.')
param syncLogDefaultTtl int = 2592000 // 30 days

// ── Variables ───────────────────────────────────────────────────────────────

var accountName = 'cosmos-${resourceToken}'
var databaseName = 'BlazorHybridDb'

var containers = [
  {
    name: 'entities'
    partitionKeyPath: '/caseWorkerId'
    defaultTtl: -1
    uniqueKeys: []
    includedPaths: [
      { path: '/caseWorkerId/?' }
      { path: '/entityType/?' }
      { path: '/status/?' }
      { path: '/createdAt/?' }
    ]
    excludedPaths: [
      { path: '/"_etag"/?' }
    ]
    compositeIndexes: [
      [
        { path: '/caseWorkerId', order: 'ascending' }
        { path: '/createdAt', order: 'descending' }
      ]
    ]
  }
  {
    name: 'documents'
    partitionKeyPath: '/entityId'
    defaultTtl: -1
    uniqueKeys: []
    includedPaths: [
      { path: '/entityId/?' }
      { path: '/documentType/?' }
      { path: '/uploadedAt/?' }
    ]
    excludedPaths: [
      { path: '/"_etag"/?' }
    ]
    compositeIndexes: [
      [
        { path: '/entityId', order: 'ascending' }
        { path: '/uploadedAt', order: 'descending' }
      ]
    ]
  }
  {
    name: 'media'
    partitionKeyPath: '/entityId'
    defaultTtl: -1
    uniqueKeys: []
    includedPaths: [
      { path: '/entityId/?' }
      { path: '/mediaType/?' }
      { path: '/createdAt/?' }
    ]
    excludedPaths: [
      { path: '/"_etag"/?' }
    ]
    compositeIndexes: [
      [
        { path: '/entityId', order: 'ascending' }
        { path: '/createdAt', order: 'descending' }
      ]
    ]
  }
  {
    name: 'sync-log'
    partitionKeyPath: '/deviceId'
    defaultTtl: syncLogDefaultTtl
    uniqueKeys: []
    includedPaths: [
      { path: '/deviceId/?' }
      { path: '/syncStatus/?' }
      { path: '/timestamp/?' }
    ]
    excludedPaths: [
      { path: '/"_etag"/?' }
    ]
    compositeIndexes: [
      [
        { path: '/deviceId', order: 'ascending' }
        { path: '/timestamp', order: 'descending' }
      ]
    ]
  }
]

// ── Resources ───────────────────────────────────────────────────────────────

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: accountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: enableFreeTier
    consistencyPolicy: {
      defaultConsistencyLevel: defaultConsistencyLevel
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 720 // 30 days
        backupStorageRedundancy: 'Local'
      }
    }
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    minimalTlsVersion: 'Tls12'
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource cosmosContainers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = [
  for container in containers: {
    parent: database
    name: container.name
    properties: {
      resource: {
        id: container.name
        partitionKey: {
          paths: [container.partitionKeyPath]
          kind: 'Hash'
          version: 2
        }
        indexingPolicy: {
          indexingMode: 'consistent'
          automatic: true
          includedPaths: concat(container.includedPaths, [{ path: '/*' }])
          excludedPaths: concat(container.excludedPaths, [{ path: '/_attachments/*' }])
          compositeIndexes: container.compositeIndexes
        }
        defaultTtl: container.defaultTtl
        conflictResolutionPolicy: {
          mode: 'LastWriterWins'
          conflictResolutionPath: '/_ts'
        }
      }
    }
  }
]

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('Cosmos DB account endpoint URI.')
output accountEndpoint string = cosmosAccount.properties.documentEndpoint

@description('Cosmos DB account name.')
output accountName string = cosmosAccount.name

@description('Cosmos DB database name.')
output databaseName string = databaseName

@description('Cosmos DB account resource ID.')
output accountResourceId string = cosmosAccount.id
