// ============================================================================
// Monitoring Module
// Deploys: Log Analytics Workspace, Application Insights, and diagnostic
// settings for the BlazorHybrid backend.
// ============================================================================

// ── Parameters ──────────────────────────────────────────────────────────────

@description('Azure region for monitoring resources.')
param location string

@description('Resource naming token (appName-env).')
param resourceToken string

@description('Resource tags.')
param tags object

@description('Log Analytics workspace retention in days.')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

@description('Log Analytics workspace SKU.')
@allowed(['PerGB2018', 'Free', 'Standalone', 'PerNode'])
param logAnalyticsSkuName string = 'PerGB2018'

@description('Daily cap in GB for Log Analytics ingestion. -1 = unlimited.')
param dailyQuotaGb int = -1

// ── Variables ───────────────────────────────────────────────────────────────

var logAnalyticsName = 'log-${resourceToken}'
var appInsightsName = 'appi-${resourceToken}'

// ── Resources ───────────────────────────────────────────────────────────────

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: logAnalyticsSkuName
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: dailyQuotaGb > 0
      ? {
          dailyQuotaGb: dailyQuotaGb
        }
      : null
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: union(tags, { 'hidden-link:${resourceGroup().id}/providers/Microsoft.Web/sites/func-${resourceToken}': 'Resource' })
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: 90
  }
}

// ── Outputs ─────────────────────────────────────────────────────────────────

@description('Application Insights connection string.')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Application Insights instrumentation key.')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('Application Insights name.')
output appInsightsName string = appInsights.name

@description('Application Insights resource ID.')
output appInsightsResourceId string = appInsights.id

@description('Log Analytics workspace ID.')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id

@description('Log Analytics workspace name.')
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
