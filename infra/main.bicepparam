using './main.bicep'

// ── Dev Environment Parameters ──────────────────────────────────────────────

param location = 'eastus2'
param environmentName = 'dev'
param appName = 'blazorhybrid'
param cosmosDbEnableFreeTier = true
