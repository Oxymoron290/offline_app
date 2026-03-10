// ============================================================================
// Key Vault Module
// Deploys: Azure Key Vault with RBAC authorization and optional access
// policies for the Function App's managed identity.
// ============================================================================

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for the Key Vault.')
param location string

@description('Resource naming token (appName-env).')
param resourceToken string

@description('Resource tags.')
param tags object

@description('Function App managed identity principal ID. Leave empty on initial deployment.')
param functionAppPrincipalId string = ''

@description('Key Vault SKU.')
@allowed(['standard', 'premium'])
param skuName string = 'standard'

@description('Enable soft delete for the Key Vault.')
param enableSoftDelete bool = true

@description('Soft delete retention in days.')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 30

@description('Enable purge protection (recommended for prod, prevents permanent deletion).')
param enablePurgeProtection bool = false

// ── Variables ───────────────────────────────────────────────────────────────

var keyVaultName = take('kv-${resourceToken}', 24)

// ── Resources ───────────────────────────────────────────────────────────────

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : null
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Key Vault Secrets User role for the Function App managed identity
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource functionAppKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(functionAppPrincipalId)) {
  name: guid(keyVault.id, functionAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    principalId: functionAppPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('Key Vault name.')
output keyVaultName string = keyVault.name

@description('Key Vault URI.')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Key Vault resource ID.')
output keyVaultResourceId string = keyVault.id
