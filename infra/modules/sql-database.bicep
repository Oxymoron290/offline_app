// Azure SQL Server + Database for backend data storage

@description('Azure region for the SQL Server.')
param location string

@description('Name of the SQL Server.')
param sqlServerName string

@description('Name of the SQL Database.')
param sqlDatabaseName string

@description('Environment name used for tagging (dev, staging, prod).')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Project name used for tagging.')
param projectName string = 'LACO'

@description('SQL Server administrator login.')
param sqlAdminLogin string

@description('SQL Server administrator password.')
@secure()
param sqlAdminPassword string

@description('SQL Database SKU name. Use Basic for dev, S0+ for prod.')
@allowed(['Basic', 'S0', 'S1', 'S2', 'P1', 'P2'])
param databaseSkuName string = 'Basic'

@description('SQL Database SKU tier.')
@allowed(['Basic', 'Standard', 'Premium'])
param databaseSkuTier string = 'Basic'

@description('SQL Database max size in bytes. Default is 2GB.')
param maxSizeBytes int = 2147483648

@description('Azure AD admin object ID for optional Azure AD admin configuration.')
param aadAdminObjectId string = ''

@description('Azure AD admin display name.')
param aadAdminDisplayName string = ''

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: {
    environment: environmentName
    project: projectName
  }
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to access the SQL Server
resource firewallRuleAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: {
    environment: environmentName
    project: projectName
  }
  sku: {
    name: databaseSkuName
    tier: databaseSkuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: maxSizeBytes
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: environmentName == 'prod' ? 'Geo' : 'Local'
  }
}

// Optional: Azure AD admin configuration
resource aadAdmin 'Microsoft.Sql/servers/administrators@2023-08-01-preview' = if (!empty(aadAdminObjectId)) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: aadAdminDisplayName
    sid: aadAdminObjectId
    tenantId: tenant().tenantId
  }
}

@description('The fully qualified domain name of the SQL Server.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The resource ID of the SQL Server.')
output sqlServerId string = sqlServer.id

@description('The resource ID of the SQL Database.')
output sqlDatabaseId string = sqlDatabase.id

@description('The name of the SQL Server.')
output sqlServerName string = sqlServer.name

@description('The name of the SQL Database.')
output sqlDatabaseName string = sqlDatabase.name

@description('The ADO.NET connection string for the SQL Database.')
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=placeholder;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
