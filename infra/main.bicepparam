using './main.bicep'

// Dev environment defaults
param environmentName = 'dev'
param resourceNamePrefix = 'laco'
param location = 'eastus2'

// SQL admin credentials — override these at deploy time or via CI/CD pipeline variables
param sqlAdminLogin = 'lacoadmin'
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')

// Dev tier database
param databaseSkuName = 'Basic'
param databaseSkuTier = 'Basic'

// Azure AD — populate with your app registration values
param azureAdClientId = ''
param azureAdTenantId = ''

// Key Vault access — add object IDs of users/service principals that need secret access
param keyVaultAccessObjectIds = []
