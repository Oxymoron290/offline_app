# Operations Guide

This guide covers monitoring, alerting, scaling, backup, troubleshooting, and maintenance for the BlazorPWA field service application in production.

---

## Table of Contents

- [Monitoring with Application Insights](#monitoring-with-application-insights)
- [Alerting Rules](#alerting-rules)
- [Scaling Guidance](#scaling-guidance)
- [Backup and Recovery](#backup-and-recovery)
- [Troubleshooting Common Issues](#troubleshooting-common-issues)
- [Log Analysis](#log-analysis)
- [Maintenance Tasks](#maintenance-tasks)

---

## Monitoring with Application Insights

Application Insights collects telemetry from the API, Worker, and Client. Access it in the Azure Portal under the `appi-blazorpwa-{env}` resource.

### Key Metrics to Watch

| Metric | Source | Healthy Range | Warning Threshold |
|--------|--------|---------------|-------------------|
| API response time (P95) | API | < 500ms | > 1000ms |
| API error rate | API | < 1% | > 5% |
| Sync operation throughput | API | Steady | Sudden drop or spike |
| Service Bus queue depth | Service Bus | < 100 | > 500 |
| Service Bus dead-letter count | Service Bus | 0 | > 10 |
| Worker processing time (P95) | Worker | < 30s (photos), < 120s (videos) | 2× baseline |
| SQL DTU usage | Azure SQL | < 60% | > 80% |
| Client-side JS errors | Client | < 5/hour | > 20/hour |
| Blob Storage throughput | Storage | < account limits | Throttling (HTTP 503) |

### Useful KQL Queries

Access these in **Application Insights → Logs**.

#### Failed sync operations in the last 24 hours

```kql
requests
| where timestamp > ago(24h)
| where url contains "/api/sync"
| where success == false
| summarize count() by bin(timestamp, 1h), resultCode
| render timechart
```

#### Slow media uploads (> 5 seconds)

```kql
requests
| where timestamp > ago(24h)
| where url contains "/api/media"
| where duration > 5000
| project timestamp, duration, resultCode, customDimensions
| order by duration desc
```

#### Sync batch sizes and durations

```kql
requests
| where timestamp > ago(24h)
| where url contains "/api/sync/batch"
| where success == true
| extend batchSize = toint(customDimensions["batchSize"])
| summarize avg(duration), percentile(duration, 95), avg(batchSize) by bin(timestamp, 1h)
| render timechart
```

#### Top client-side errors

```kql
exceptions
| where timestamp > ago(24h)
| where client_Type == "Browser"
| summarize count() by type, outerMessage
| order by count_ desc
| take 20
```

#### Service Bus queue processing latency

```kql
dependencies
| where timestamp > ago(24h)
| where type == "Azure Service Bus"
| summarize avg(duration), percentile(duration, 95), count() by bin(timestamp, 1h)
| render timechart
```

#### Worker media processing performance

```kql
customEvents
| where timestamp > ago(24h)
| where name == "MediaProcessed"
| extend mediaType = tostring(customDimensions["mediaType"])
| extend processingTimeMs = todouble(customDimensions["processingTimeMs"])
| extend fileSizeMB = todouble(customDimensions["fileSizeBytes"]) / 1048576
| summarize avg(processingTimeMs), percentile(processingTimeMs, 95), avg(fileSizeMB) by mediaType
```

#### Active users in the last 7 days

```kql
customEvents
| where timestamp > ago(7d)
| where name == "SyncCompleted"
| extend deviceId = tostring(customDimensions["deviceId"])
| summarize lastSync = max(timestamp), syncCount = count() by deviceId
| order by lastSync desc
```

#### Failed operations by type

```kql
customEvents
| where timestamp > ago(24h)
| where name == "SyncOperationFailed"
| extend operationType = tostring(customDimensions["operationType"])
| extend errorMessage = tostring(customDimensions["errorMessage"])
| summarize count() by operationType, errorMessage
| order by count_ desc
```

### Dashboard Setup

Create an Application Insights dashboard with these panels:

1. **API Health** — Server response time and error rate (line chart)
2. **Sync Activity** — Sync batch count and throughput (bar chart)
3. **Queue Depth** — Service Bus active message count (line chart)
4. **Worker Performance** — Media processing time by type (bar chart)
5. **Active Devices** — Unique devices syncing per hour (bar chart)
6. **Error Summary** — Top 10 errors across all services (table)

---

## Alerting Rules

Configure these alerts in **Azure Monitor → Alerts**.

### Recommended Alert Configuration

| Alert Name | Condition | Severity | Action |
|------------|-----------|----------|--------|
| API Error Rate - Warning | Error rate > 5% over 15 min | Warning (Sev 2) | Email ops team |
| API Error Rate - Critical | Error rate > 15% over 5 min | Critical (Sev 1) | Email + SMS + PagerDuty |
| API Response Time | P95 response time > 2000ms over 15 min | Warning (Sev 2) | Email ops team |
| Service Bus Dead Letters | Dead-letter count > 10 | Warning (Sev 2) | Email ops team |
| Service Bus Dead Letters - Critical | Dead-letter count > 50 | Critical (Sev 1) | Email + SMS |
| Worker Failures | > 5 failures in 15 min | Critical (Sev 1) | Email + SMS + PagerDuty |
| SQL DTU Usage | DTU > 80% for 30 min | Warning (Sev 2) | Email ops team |
| SQL DTU Usage - Critical | DTU > 95% for 15 min | Critical (Sev 1) | Email + SMS |
| Storage Throttling | HTTP 503 count > 10 in 5 min | Warning (Sev 2) | Email ops team |
| App Service Down | Availability < 99% over 5 min | Critical (Sev 1) | Email + SMS + PagerDuty |

### Setting Up Alerts via Azure CLI

```powershell
# Example: API error rate > 5% alert
az monitor metrics alert create `
  --resource-group rg-blazorpwa-prod `
  --name "api-error-rate-warning" `
  --scopes "/subscriptions/{sub}/resourceGroups/rg-blazorpwa-prod/providers/Microsoft.Web/sites/app-api-blazorpwa-prod" `
  --condition "avg requests/failed > 5" `
  --window-size 15m `
  --evaluation-frequency 5m `
  --severity 2 `
  --action "/subscriptions/{sub}/resourceGroups/rg-blazorpwa-prod/providers/Microsoft.Insights/actionGroups/ops-team"
```

### Action Groups

Create action groups for notification routing:

| Action Group | Recipients | Channels |
|-------------|------------|----------|
| `ops-team` | Operations engineers | Email |
| `ops-critical` | Operations + on-call engineer | Email + SMS |
| `management` | Project managers | Email (weekly digest) |

---

## Scaling Guidance

### App Service Scaling

#### Horizontal Scaling (Scale Out)

| Environment | Min Instances | Max Instances | Scale Rule |
|-------------|---------------|---------------|------------|
| Dev | 1 | 1 | Fixed |
| Staging | 1 | 3 | CPU > 70% for 10 min |
| Production | 2 | 10 | CPU > 70% for 10 min, or HTTP queue > 100 |

```powershell
# Configure autoscale for production API
az monitor autoscale create `
  --resource-group rg-blazorpwa-prod `
  --resource app-api-blazorpwa-prod `
  --resource-type Microsoft.Web/serverFarms `
  --name "api-autoscale" `
  --min-count 2 `
  --max-count 10 `
  --count 2
```

#### Vertical Scaling (Scale Up)

| Workload Pattern | Recommended SKU | Notes |
|------------------|-----------------|-------|
| < 50 field workers | B1 | Sufficient for dev/testing |
| 50–200 field workers | S1 | Standard with autoscale |
| 200–500 field workers | P1v3 | Premium with better CPU/memory |
| 500+ field workers | P2v3 or P3v3 | High-performance tier |

### Azure SQL Scaling

#### DTU-Based Tiers

| Field Workers | Recommended Tier | DTUs | Max Size |
|---------------|-----------------|------|----------|
| < 50 | Basic | 5 | 2 GB |
| 50–200 | Standard S0 | 10 | 250 GB |
| 200–500 | Standard S2 | 50 | 250 GB |
| 500–1000 | Standard S3 | 100 | 250 GB |
| 1000+ | Consider vCore | — | — |

```powershell
# Scale up SQL database
az sql db update `
  --resource-group rg-blazorpwa-prod `
  --server sql-blazorpwa-prod `
  --name sqldb-blazorpwa-prod `
  --edition Standard `
  --capacity 50
```

#### When to Consider vCore Model

- Need more than 100 DTUs.
- Need fine-grained CPU/memory control.
- Using Azure Hybrid Benefit (existing SQL Server licenses).
- Need read replicas for reporting workloads.

### Service Bus Scaling

| Tier | Use Case | Max Message Size | Throughput |
|------|----------|-----------------|------------|
| Standard | Dev/Staging, < 500 workers | 256 KB | Shared throughput |
| Premium (1 MU) | Production, 500+ workers | 100 MB | Dedicated throughput |

> **Note:** With Standard tier, the media upload message contains only metadata (not the file itself). The actual file is in Blob Storage staging. Standard tier's 256 KB message limit is sufficient.

### Storage Account Limits

| Limit | Value | Impact |
|-------|-------|--------|
| Max request rate (per account) | 20,000 requests/sec | Rarely hit for this workload |
| Max bandwidth (per account, GRS) | 10 Gbps ingress, 20 Gbps egress | Monitor during peak sync periods |
| Max blob size | 190.7 TiB | Not a concern |
| Single upload limit | 5,000 MiB | Videos under 5 GB use single upload |

### Expected Load Patterns

```
        Field Worker Day
        ─────────────────
  ▲
  │     ┌───┐
  │     │   │              ┌───────┐
  │ ┌───┤   │              │       │
  │ │   │   ├──┐     ┌────┤       │
  │ │   │   │  │     │    │       │
──┴─┴───┴───┴──┴─────┴────┴───────┴──── Time
  6am  8am 10am 12pm  2pm  4pm   6pm

  Peak sync times:
  - 7-9 AM: Workers sync before heading out
  - 12-1 PM: Midday check-in sync
  - 4-6 PM: End-of-day sync with all collected data
```

Plan capacity for the **4-6 PM peak** when all field workers sync simultaneously with media files.

---

## Backup and Recovery

### Azure SQL Database

Azure SQL provides automatic backups with Point-in-Time Restore (PITR).

| Backup Type | Frequency | Retention |
|-------------|-----------|-----------|
| Full backup | Weekly | 7–35 days (depends on tier) |
| Differential backup | Every 12 hours | Same as full |
| Transaction log backup | Every 5–10 minutes | Same as full |

**Default retention by tier:**
- Basic: 7 days
- Standard: 35 days
- Premium: 35 days

```powershell
# Restore to a point in time
az sql db restore `
  --resource-group rg-blazorpwa-prod `
  --server sql-blazorpwa-prod `
  --name sqldb-blazorpwa-prod `
  --dest-name sqldb-blazorpwa-prod-restored `
  --time "2024-03-15T14:00:00Z"
```

**Long-term retention (LTR):** Configure weekly, monthly, or yearly backups retained for up to 10 years:

```powershell
az sql db ltr-policy set `
  --resource-group rg-blazorpwa-prod `
  --server sql-blazorpwa-prod `
  --name sqldb-blazorpwa-prod `
  --weekly-retention P4W `
  --monthly-retention P12M `
  --yearly-retention P5Y `
  --week-of-year 1
```

### Blob Storage Redundancy

| Environment | Redundancy | Protection |
|-------------|-----------|------------|
| Dev | LRS (Locally Redundant) | 3 copies in one datacenter |
| Staging | LRS | 3 copies in one datacenter |
| Production | GRS (Geo-Redundant) | 6 copies across 2 regions |

**Soft delete** is enabled on all blob containers (14-day retention) to protect against accidental deletion:

```powershell
az storage account blob-service-properties update `
  --account-name stblazorpwaprod `
  --enable-delete-retention true `
  --delete-retention-days 14
```

**Blob versioning** can be enabled for additional protection:

```powershell
az storage account blob-service-properties update `
  --account-name stblazorpwaprod `
  --enable-versioning true
```

### Service Bus Message Retention

- Active messages: retained in queue until processed or TTL expires (7 days default).
- Dead-letter messages: retained indefinitely until manually processed or purged.
- **Recommendation:** Process dead-letter messages within 48 hours. Set up an alert if the count exceeds 10.

### Disaster Recovery Procedures

#### Scenario 1: API App Service outage

1. Azure App Service has built-in redundancy within a region.
2. If a regional outage occurs, failover to the secondary region (if geo-redundant deployment is configured).
3. Client PWAs continue to function offline and queue operations.
4. When the API is restored, clients sync automatically.

#### Scenario 2: Database corruption or data loss

1. Identify the point in time before the corruption using Application Insights logs.
2. Restore the database to that point using PITR (see above).
3. Swap the connection string to the restored database.
4. Verify data integrity.
5. Re-process any Service Bus dead-letter messages.

#### Scenario 3: Service Bus queue issues

1. Check queue health: `az servicebus queue show ...`
2. If messages are stuck, check the Worker App Service logs.
3. Restart the Worker: `az webapp restart --name app-worker-blazorpwa-prod ...`
4. Process dead-letter messages manually if needed.
5. If the namespace is unhealthy, create a new namespace and update configuration.

#### Scenario 4: Blob Storage data loss

1. If soft delete is enabled, recover deleted blobs within the retention window.
2. For GRS-enabled accounts, initiate failover to the secondary region.
3. Re-process media from the database attachment records (which contain the original upload references).

---

## Troubleshooting Common Issues

### Sync Failures

**Symptom:** Records stuck in "Pending Sync" status on client devices.

| Possible Cause | Diagnosis | Resolution |
|----------------|-----------|------------|
| Network timeout | Check client connectivity; check API response times in App Insights | Sync engine auto-retries; verify API is responsive |
| Version conflict | Check API logs for 409 responses; query syncQueue for `failed` status | Resolve conflicts manually in the UI; reset retryCount |
| API returning 500 | Check API logs in App Insights for exceptions | Fix the server-side bug; redeploy |
| Request too large | Check API logs for 413 responses | Reduce sync batch size in client config |
| Authentication failure | Check for 401/403 responses | Verify CORS config; check token expiration |

**KQL to diagnose:**
```kql
requests
| where timestamp > ago(1h)
| where url contains "/api/sync"
| where success == false
| project timestamp, resultCode, duration, customDimensions
| order by timestamp desc
| take 50
```

### Service Bus Dead-Letter Messages

**Symptom:** Dead-letter queue count increasing; media not appearing in reports.

**Diagnosis:**
```powershell
# Check dead-letter count
az servicebus queue show `
  --resource-group rg-blazorpwa-prod `
  --namespace-name sb-blazorpwa-prod `
  --name media-processing `
  --query "countDetails.deadLetterMessageCount" `
  --output tsv

# Peek at dead-letter messages (use Service Bus Explorer in Azure Portal)
```

**Common causes:**
- Worker not running (check App Service status).
- Blob Storage access denied (check Managed Identity role assignment).
- File too large for processing timeout (increase lock duration).
- Malformed message (check message body in dead-letter queue).

**Resolution:**
1. Fix the root cause.
2. Resubmit dead-letter messages using Service Bus Explorer or a script.
3. Or manually process the media using the attachment records in the database.

### Media Upload Failures

**Symptom:** Photos/videos not appearing after sync.

| Possible Cause | Diagnosis | Resolution |
|----------------|-----------|------------|
| File exceeds size limit | Check API logs for 413 responses | Client should validate before upload; update limits if needed |
| Upload timeout | Check API request duration > 30s | Increase API timeout for media endpoint; use chunked upload for large videos |
| Staging blob write failure | Check Worker logs for StorageException | Verify Managed Identity has Storage Blob Data Contributor role |
| Worker crash during processing | Check Worker logs for unhandled exceptions | Fix bug in processing code; dead-lettered message will be retried |

### PWA Cache Issues (Stale Service Worker)

**Symptom:** Users see an old version of the app after deployment.

**Diagnosis:** The service worker is caching the old version and not updating.

**Resolution:**
1. **For individual users:** Instruct them to:
   - Open the app.
   - Look for the "Update Available" prompt and click **Update**.
   - If no prompt: open DevTools → Application → Service Workers → click **Update**.
   - Hard refresh: `Ctrl + Shift + R`.

2. **For all users (deployment-level):**
   - Ensure the service worker version is incremented on each deployment.
   - Use `self.skipWaiting()` in the service worker install event.
   - Cache-bust the `service-worker.js` file (query string or hash).

3. **Nuclear option:** Instruct users to clear site data:
   - DevTools → Application → Storage → **Clear site data**.
   - **Warning:** This clears IndexedDB — ensure all data is synced first!

### Database Connection Pool Exhaustion

**Symptom:** API returns 500 errors intermittently; logs show `SqlException: Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.`

**Diagnosis:**
```kql
exceptions
| where timestamp > ago(1h)
| where type contains "SqlException"
| where outerMessage contains "connection pool"
| summarize count() by bin(timestamp, 5m)
| render timechart
```

**Resolution:**
1. Increase `Max Pool Size` in the connection string (default is 100):
   ```
   Server=...;Max Pool Size=200;Connection Timeout=30;
   ```
2. Ensure all database connections are properly disposed (use `using` statements with EF Core).
3. Check for long-running queries blocking connections.
4. Scale up the SQL database tier if DTU is saturated.
5. Add connection resiliency:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseSqlServer(connectionString, sql =>
           sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));
   ```

---

## Log Analysis

### Where to Find Logs

| Log Source | Location | Access |
|------------|----------|--------|
| API application logs | Application Insights → Logs | KQL queries on `requests`, `exceptions`, `traces` |
| Worker application logs | Application Insights → Logs | Same workspace, filter by `cloud_RoleName` |
| Client-side telemetry | Application Insights → Logs | Filter by `client_Type == "Browser"` |
| App Service platform logs | App Service → Monitoring → Log stream | Real-time streaming |
| App Service detailed logs | App Service → Monitoring → Logs | Diagnostic settings → Log Analytics |
| Service Bus metrics | Service Bus → Monitoring → Metrics | Built-in charts |
| SQL audit logs | Azure SQL → Auditing | Log Analytics or Storage Account |

### Correlating Client-Side and Server-Side Logs

Application Insights uses **operation IDs** to correlate requests across the distributed system:

```kql
// Find all telemetry related to a specific sync operation
union requests, dependencies, exceptions, traces
| where timestamp > ago(1h)
| where operation_Id == "abc123-operation-id"
| order by timestamp asc
| project timestamp, itemType, name, success, duration, message
```

To trace from a client-side error to the server:

```kql
// 1. Find the client error
exceptions
| where timestamp > ago(1h)
| where client_Type == "Browser"
| where outerMessage contains "sync failed"
| project timestamp, operation_Id, outerMessage

// 2. Use the operation_Id to find the server request
requests
| where operation_Id == "the-operation-id-from-step-1"
| project timestamp, url, resultCode, duration, customDimensions
```

### Diagnosing Sync Issues

When a field worker reports sync problems:

```kql
// Find all sync operations for a specific device
customEvents
| where timestamp > ago(7d)
| where customDimensions["deviceId"] == "device-abc123"
| where name in ("SyncStarted", "SyncCompleted", "SyncFailed", "SyncConflict")
| project timestamp, name, customDimensions
| order by timestamp desc
| take 100
```

```kql
// Find operations that failed for a specific entity
customEvents
| where timestamp > ago(7d)
| where name == "SyncOperationFailed"
| where customDimensions["entityId"] == "rpt-specific-report-id"
| project timestamp, customDimensions["operationType"], customDimensions["errorMessage"]
```

---

## Maintenance Tasks

### Certificate Rotation

App Service managed certificates are automatically renewed. For custom domains:

```powershell
# Check certificate expiration
az webapp config ssl list `
  --resource-group rg-blazorpwa-prod `
  --query "[].{name:name, expirationDate:expirationDate, thumbprint:thumbprint}" `
  --output table
```

- **Managed certificates:** Automatically renewed 45 days before expiration.
- **Custom certificates:** Upload new certificate and bind to the domain.
- **Set an alert** for certificates expiring within 30 days.

### Key Vault Secret Rotation

Establish a rotation schedule for secrets stored in Key Vault:

| Secret | Rotation Frequency | Procedure |
|--------|-------------------|-----------|
| SQL admin password | 90 days | Update in Key Vault; update App Service config |
| Service principal client secret | 365 days | `az ad sp credential reset`; update GitHub secret |
| Third-party API keys | Per vendor policy | Update in Key Vault |

```powershell
# Check secret expiration dates
az keyvault secret list `
  --vault-name kv-blazorpwa-prod `
  --query "[].{name:name, expires:attributes.expires}" `
  --output table

# Rotate a secret
az keyvault secret set `
  --vault-name kv-blazorpwa-prod `
  --name "secret-name" `
  --value "new-secret-value" `
  --expires "2025-06-15T00:00:00Z"
```

### Database Index Maintenance

Azure SQL handles most index maintenance automatically, but monitor fragmentation for large tables:

```sql
-- Check index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
    AND ips.page_count > 1000
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

```sql
-- Rebuild fragmented indexes (run during off-peak hours)
ALTER INDEX [IX_Reports_CreatedAt] ON [dbo].[Reports] REBUILD;
ALTER INDEX [IX_SyncOperations_EntityId] ON [dbo].[SyncOperations] REBUILD;
```

**Schedule:** Review index fragmentation monthly; rebuild indexes with > 30% fragmentation.

### Blob Storage Lifecycle Management

Configure lifecycle policies to manage storage costs:

```json
{
  "rules": [
    {
      "name": "move-old-media-to-cool",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["photos/", "videos/", "documents/"]
        },
        "actions": {
          "baseBlob": {
            "tierToCool": { "daysAfterModificationGreaterThan": 90 },
            "tierToArchive": { "daysAfterModificationGreaterThan": 365 }
          }
        }
      }
    },
    {
      "name": "cleanup-staging",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["staging/"]
        },
        "actions": {
          "baseBlob": {
            "delete": { "daysAfterModificationGreaterThan": 7 }
          }
        }
      }
    }
  ]
}
```

```powershell
# Apply lifecycle policy
az storage account management-policy create `
  --account-name stblazorpwaprod `
  --policy @lifecycle-policy.json
```

### Updating the PWA Service Worker

When deploying a new version of the service worker:

1. **Increment the cache version** in `service-worker.js`:
   ```javascript
   const CACHE_VERSION = 'v2.1.0'; // Bump on every deployment
   ```

2. **Ensure clean activation:**
   ```javascript
   self.addEventListener('activate', event => {
     event.waitUntil(
       caches.keys().then(cacheNames =>
         Promise.all(
           cacheNames
             .filter(name => name !== CACHE_VERSION)
             .map(name => caches.delete(name))
         )
       )
     );
   });
   ```

3. **Monitor update adoption** in Application Insights:
   ```kql
   customEvents
   | where timestamp > ago(7d)
   | where name == "ServiceWorkerUpdated"
   | extend version = tostring(customDimensions["version"])
   | summarize count() by version, bin(timestamp, 1d)
   | render columnchart
   ```

4. **If users are stuck on an old version:**
   - Add a cache-busting query parameter to the service worker registration.
   - Communicate to users via in-app notification to refresh.

### Regular Maintenance Checklist

| Task | Frequency | Responsible |
|------|-----------|-------------|
| Review Application Insights dashboard | Daily | Operations |
| Check Service Bus dead-letter queue | Daily | Operations |
| Review alert notifications | Daily | Operations |
| Check certificate expiration dates | Weekly | Operations |
| Review SQL DTU usage trends | Weekly | Operations |
| Review Blob Storage usage and costs | Monthly | Operations |
| Check index fragmentation | Monthly | DBA / Operations |
| Rotate Key Vault secrets | Per schedule | Security |
| Review and update autoscale rules | Quarterly | Architecture |
| Test disaster recovery procedures | Semi-annually | Operations + Dev |
| Update dependencies and frameworks | Per release cycle | Development |
| Audit RBAC and access policies | Quarterly | Security |

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Local development setup
- [Architecture Guide](ARCHITECTURE.md) — System design and data flows
- [Deployment Guide](DEPLOYMENT.md) — Deploy to Azure
- [User Guide](USER-GUIDE.md) — End-user documentation
