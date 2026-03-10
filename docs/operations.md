# Operations Guide

This guide covers monitoring, troubleshooting, scaling, and maintaining the Blazor Hybrid Case Worker App in production.

## Monitoring

### Application Insights

The Azure Functions API is instrumented with Application Insights for telemetry, logging, and performance monitoring.

**Key dashboards to set up:**
- **Live Metrics** — Real-time request rates, failures, and performance
- **Failures** — Exception tracking and failed request analysis
- **Performance** — Response times and dependency call durations

### Key Metrics to Monitor

| Metric | Threshold | Action |
|--------|-----------|--------|
| Sync endpoint response time | > 5s avg | Investigate Cosmos DB query performance |
| Failed requests rate | > 5% | Check logs for errors |
| Sync batch size | > 100 ops/batch | Consider batch limits |
| Blob upload failures | Any | Check storage account quotas |
| Function execution count | Monitor trend | Watch for consumption plan scaling |

### Log Analytics Queries

**Recent sync failures:**
```kusto
traces
| where message contains "sync" and severityLevel >= 3
| order by timestamp desc
| take 50
```

**Sync batch processing times:**
```kusto
requests
| where name == "SyncBatch"
| summarize avg(duration), percentile(duration, 95), count() by bin(timestamp, 1h)
| render timechart
```

**Media upload volumes:**
```kusto
requests
| where name in ("GetUploadSasToken", "UploadMedia")
| summarize count() by bin(timestamp, 1h), name
| render timechart
```

**Cosmos DB request unit consumption:**
```kusto
dependencies
| where type == "Azure DocumentDB"
| summarize sum(todouble(customDimensions["RequestCharge"])) by bin(timestamp, 1h)
| render timechart
```

## Alerting

Set up alerts in Azure Monitor for:

1. **High failure rate**: > 10 failed requests in 5 minutes
2. **Slow sync**: Sync endpoint P95 > 10 seconds
3. **Storage quota**: Blob storage approaching capacity
4. **Function errors**: Any unhandled exceptions

```bash
# Example: Create an alert for high failure rate
az monitor metrics alert create \
  --resource-group rg-blazorhybrid-prod \
  --name "high-failure-rate" \
  --scopes "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{funcapp}" \
  --condition "count requests/failed > 10" \
  --window-size 5m \
  --action-group "{action-group-id}"
```

## Troubleshooting

### Common Issues

#### 1. Sync operations stuck in "Pending"

**Symptoms**: Client shows growing pending sync count; operations never complete.

**Causes & Solutions**:
- **No connectivity**: Normal behavior — operations will sync when online
- **Auth token expired**: Force re-login in the app
- **API unreachable**: Check Function App status in Azure Portal
- **Max retries exceeded**: Operations with `Status = "Failed"` need manual review

#### 2. Media upload failures

**Symptoms**: Photos/videos/documents fail to sync.

**Causes & Solutions**:
- **File too large**: Default Consumption plan has 100MB request limit. Consider chunked upload for large videos.
- **SAS token expired**: Tokens are valid for 30 minutes. Retry will generate a new token.
- **Storage account throttled**: Check storage metrics for throttling (HTTP 429/503)

#### 3. Cosmos DB throughput issues

**Symptoms**: Slow sync, 429 (Too Many Requests) errors.

**Causes & Solutions**:
- Serverless mode auto-scales, but has a max of 5000 RU/s burst
- Review partition key design for hot partitions
- Consider provisioned throughput for predictable high-volume workloads

#### 4. MAUI app crashes on startup

**Symptoms**: App crashes before showing UI.

**Causes & Solutions**:
- Check that SQLite database file is accessible (permissions)
- Verify MSAL configuration is correct for the platform
- Review crash logs in Application Insights (if telemetry is configured)

### Diagnostic Commands

```bash
# Check Function App status
az functionapp show --name func-blazorhybrid-prod --resource-group rg-blazorhybrid-prod --query state

# View recent Function App logs
az monitor app-insights query --app appi-blazorhybrid-prod \
  --analytics-query "exceptions | order by timestamp desc | take 20"

# Check Cosmos DB metrics
az cosmosdb sql database throughput show \
  --account-name cosmos-blazorhybrid-prod \
  --resource-group rg-blazorhybrid-prod \
  --name BlazorHybridDb

# Check Storage Account usage
az storage account show --name stblazorhybridprod --resource-group rg-blazorhybrid-prod \
  --query "primaryEndpoints"
```

## Scaling

### Azure Functions (Consumption Plan)
- Auto-scales automatically (0 to 200 instances)
- Cold start: ~2-5 seconds for .NET isolated worker
- For consistent performance, consider Premium plan (EP1+)

### Cosmos DB (Serverless)
- Auto-scales up to 5,000 RU/s
- For sustained high throughput, switch to provisioned mode
- Monitor RU consumption and partition key distribution

### Blob Storage
- Virtually unlimited storage capacity
- Consider lifecycle management policies for old media:
  ```bash
  az storage account management-policy create \
    --account-name stblazorhybridprod \
    --policy @lifecycle-policy.json
  ```

## Backup & Recovery

### Cosmos DB
- **Continuous backup** is enabled by default (30 days retention)
- Point-in-time restore available via Azure Portal or CLI:
  ```bash
  az cosmosdb sql database restore \
    --account-name cosmos-blazorhybrid-prod \
    --resource-group rg-blazorhybrid-prod \
    --name BlazorHybridDb \
    --restore-timestamp "2026-03-01T00:00:00Z"
  ```

### Blob Storage
- **Soft delete** is enabled (7 days retention for blobs)
- Consider enabling blob versioning for critical documents
- Set up geo-redundant storage (GRS) for disaster recovery

### Application Configuration
- All infrastructure is defined in Bicep — redeploy from source
- Secrets are managed in Key Vault — ensure regular rotation
- App registration settings should be documented outside the codebase

## Maintenance Tasks

| Task | Frequency | Description |
|------|-----------|-------------|
| Review sync-log | Weekly | Check for persistent sync failures |
| Monitor storage costs | Monthly | Review blob storage and Cosmos DB costs |
| Rotate secrets | Quarterly | Rotate Key Vault secrets and connection strings |
| Update dependencies | Monthly | Update NuGet packages for security patches |
| Review alert thresholds | Quarterly | Adjust based on usage patterns |
