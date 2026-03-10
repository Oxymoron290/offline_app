@description('Name of the Key Vault')
param name string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Tenant ID for Azure AD')
param tenantId string = subscription().tenantId

@description('Principal IDs to grant access to')
param accessPolicies array = []

@description('Tags for the resource')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
  }
}

@description('Grant Key Vault Secrets User role to specified principals')
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for policy in accessPolicies: {
    name: guid(keyVault.id, policy.principalId, '4633458b-17de-408a-b874-0445c86b69e6')
    scope: keyVault
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
      principalId: policy.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
