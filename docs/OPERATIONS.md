# Operations Guide

Monitoring, scaling, troubleshooting, and maintaining the BlazorWASM_PWA application in production.

---

## Monitoring with Application Insights

The Azure Functions API is instrumented with Application Insights via the `Microsoft.ApplicationInsights.WorkerService` package, providing automatic telemetry collection.

### Key Dashboards

Access monitoring through the Azure Portal:

1. **Application Insights** → Overview → Examine request rates, failure rates, and response times
2. **Application Insights** → Live Metrics → Real-time monitoring during deployments or incidents
3. **Application Insights** → Failures → Investigate exceptions and failed requests
4. **Application Insights** → Performance → Identify slow API endpoints

### Custom Telemetry

The API can emit custom events for sync-specific telemetry:

```csharp
// In Azure Functions code
telemetryClient.TrackEvent("SyncCompleted", new Dictionary<string, string>
{
    { "UserId", userId },
    { "OperationCount", operationCount.ToString() },
    { "Duration", duration.TotalMilliseconds.ToString() }
});

telemetryClient.TrackMetric("SyncOperationsProcessed", operationCount);
```

### Useful KQL Queries

Run these in Application Insights → Logs:

**Sync success/failure rate (last 24 hours):**

```kusto
requests
| where timestamp > ago(24h)
| where name contains "sync"
| summarize
    SuccessCount = countif(success == true),
    FailureCount = countif(success == false),
    SuccessRate = round(100.0 * countif(success == true) / count(), 2)
    by bin(timestamp, 1h)
| order by timestamp desc
```

**Top errors by frequency:**

```kusto
exceptions
| where timestamp > ago(7d)
| summarize Count = count() by type, outerMessage
| order by Count desc
| take 20
```

**API latency percentiles by endpoint:**

```kusto
requests
| where timestamp > ago(24h)
| summarize
    P50 = percentile(duration, 50),
    P90 = percentile(duration, 90),
    P99 = percentile(duration, 99)
    by name
| order by P90 desc
```

**Blob upload performance:**

```kusto
dependencies
| where timestamp > ago(24h)
| where type == "Azure blob"
| summarize
    AvgDuration = avg(duration),
    FailureRate = round(100.0 * countif(success == false) / count(), 2),
    Count = count()
    by name
```

**Active users (unique sync callers):**

```kusto
requests
| where timestamp > ago(7d)
| where name contains "sync"
| extend UserId = tostring(customDimensions.UserId)
| summarize DailyUsers = dcount(UserId) by bin(timestamp, 1d)
| order by timestamp desc
```

---

## Key Metrics to Watch

### Application Health

| Metric | Healthy Range | Alert Threshold | Source |
|--------|--------------|-----------------|--------|
| API response time (P95) | < 500 ms | > 2,000 ms | Application Insights |
| API error rate | < 1% | > 5% | Application Insights |
| Sync success rate | > 99% | < 95% | Custom telemetry |
| Failed sync operations | 0 | > 10 per hour | Custom telemetry |

### Infrastructure

| Metric | Healthy Range | Alert Threshold | Source |
|--------|--------------|-----------------|--------|
| Azure SQL DTU consumption | < 60% | > 80% | Azure SQL Metrics |
| Azure SQL storage used | < 70% of max | > 85% of max | Azure SQL Metrics |
| Storage account egress | Varies | > 80% of limit | Storage Metrics |
| Blob storage capacity | Monitor growth | Approaching quota | Storage Metrics |
| Functions execution count | Normal baseline | 3× baseline (spike) | Functions Metrics |

### Setting Up Alerts

```bash
# Create an alert rule for high API error rate
az monitor metrics alert create \
  --name "HighApiErrorRate" \
  --resource-group rg-blazorpwa-prod \
  --scopes "/subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Insights/components/<ai-name>" \
  --condition "avg requests/failed > 5" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group <action-group-id> \
  --description "API error rate exceeds 5%"
```

---

## Scaling Guidance

### Azure SQL Database

| Tier | DTUs | Max Size | Recommended For |
|------|------|----------|-----------------|
| Basic | 5 | 2 GB | Development/testing |
| Standard S1 | 20 | 250 GB | Small deployments (< 50 users) |
| Standard S2 | 50 | 250 GB | Medium deployments (50–200 users) |
| Standard S3 | 100 | 250 GB | Large deployments (200+ users) |

Scale the database tier:

```bash
az sql db update \
  --resource-group rg-blazorpwa-prod \
  --server <sql-server> \
  --name BlazorPWA \
  --service-objective S2
```

> **Tip:** Monitor DTU usage for a week before scaling. Azure SQL provides recommendations under **Performance overview** in the portal.

### Azure Storage Account

- **Standard general-purpose v2** is sufficient for most workloads
- Default limits: 20,000 requests/sec per account, 500 TB capacity
- Consider **Cool access tier** for infrequently accessed blobs (documents older than 30 days)

```bash
# Move older blobs to Cool tier using a lifecycle management policy
az storage account management-policy create \
  --account-name <storage-account> \
  --resource-group rg-blazorpwa-prod \
  --policy @lifecycle-policy.json
```

### Azure Static Web Apps

- **Free tier:** 100 GB bandwidth/month, 2 custom domains, 250 MB app size
- **Standard tier:** 100 GB bandwidth/month (overage billed), 5 custom domains, 500 MB app size
- If you exceed bandwidth limits, upgrade to Standard or add Azure CDN

### Azure Functions (within Static Web Apps)

Azure Functions within Static Web Apps (managed functions) have specific limits:
- Max request size: 100 MB
- Execution timeout: 45 seconds (Standard tier) or 10 minutes (Dedicated plan)
- For high-throughput sync scenarios, consider a separate Azure Functions Premium plan

---

## Backup and Restore

### Azure SQL Automated Backups

Azure SQL provides automatic backups:

| Feature | Basic | Standard | Premium |
|---------|-------|----------|---------|
| Point-in-time restore | 7 days | 35 days | 35 days |
| Long-term retention | Manual config | Up to 10 years | Up to 10 years |
| Geo-redundant backups | Available | Available | Available |

**Restore to a point in time:**

```bash
az sql db restore \
  --resource-group rg-blazorpwa-prod \
  --server <sql-server> \
  --name BlazorPWA \
  --dest-name BlazorPWA-Restored \
  --time "2025-01-15T10:00:00Z"
```

**Configure long-term retention:**

```bash
az sql db ltr-policy set \
  --resource-group rg-blazorpwa-prod \
  --server <sql-server> \
  --name BlazorPWA \
  --weekly-retention P4W \
  --monthly-retention P12M \
  --yearly-retention P5Y \
  --week-of-year 1
```

### Blob Storage Protection

**Enable soft delete** (retain deleted blobs for recovery):

```bash
az storage blob service-properties delete-policy update \
  --account-name <storage-account> \
  --enable true \
  --days-retained 30
```

**Enable container soft delete:**

```bash
az storage account blob-service-properties update \
  --account-name <storage-account> \
  --resource-group rg-blazorpwa-prod \
  --enable-container-delete-retention true \
  --container-delete-retention-days 30
```

**Enable blob versioning** (maintain previous versions of blobs):

```bash
az storage account blob-service-properties update \
  --account-name <storage-account> \
  --resource-group rg-blazorpwa-prod \
  --enable-versioning true
```

> **Recommendation:** Enable both soft delete (30 days) and versioning in production. This protects against accidental deletion and overwrites of case worker documents and photos.

---

## Troubleshooting Common Issues

### Sync Failures

**Symptom:** Case workers see increasing "pending operations" that never resolve.

**Diagnosis:**

1. Check Application Insights for sync endpoint errors:
   ```kusto
   requests
   | where name contains "sync" and success == false
   | where timestamp > ago(1h)
   | project timestamp, resultCode, duration, customDimensions
   ```

2. Check for specific error patterns:
   - **HTTP 413 (Payload Too Large):** Sync batch contains too many operations or large blobs. Reduce batch size.
   - **HTTP 409 (Conflict):** Entity was modified on the server. Conflict resolution should handle this, but check for bugs.
   - **HTTP 401/403:** Token expired or permissions changed. See [Authentication Token Refresh Failures](#authentication-token-refresh-failures).
   - **HTTP 500:** Server-side bug. Check exception telemetry.

**Resolution:**
- Reduce sync batch size in client configuration
- Implement retry logic with exponential backoff (already designed into the architecture)
- Verify the API is healthy and the database is accessible

### IndexedDB Quota Exceeded

**Symptom:** Browser throws `QuotaExceededError` when saving data.

**Background:**
- Chrome/Edge: ~60% of disk space available to origin (minimum 10 GB typically)
- Firefox: ~50% of disk space
- Safari/iOS: ~1 GB (much more restrictive!)
- Quotas are per-origin and shared across IndexedDB, Cache API, and service worker storage

**Resolution:**

1. **Monitor storage usage** — Add storage estimation to the app:
   ```javascript
   const estimate = await navigator.storage.estimate();
   console.log(`Used: ${estimate.usage} / ${estimate.quota} bytes`);
   ```

2. **Clean up synced data** — After successful sync, remove operation payloads and blob data from IndexedDB:
   ```javascript
   // Delete synced blobs from local storage
   await db.photos.where('syncStatus').equals('synced').delete();
   ```

3. **Compress images client-side** before storing in IndexedDB (reduce from multi-MB camera photos to reasonable sizes)

4. **Request persistent storage** (prevents eviction by the browser):
   ```javascript
   if (navigator.storage && navigator.storage.persist) {
       const granted = await navigator.storage.persist();
       console.log(`Persistent storage granted: ${granted}`);
   }
   ```

5. **iOS-specific workaround:** Warn users that iOS has limited offline storage; ensure frequent syncs to prevent data loss.

### CORS Issues

**Symptom:** Browser console shows `Access-Control-Allow-Origin` errors.

**Cause:** The API is being called directly instead of through the SWA proxy, or CORS headers are misconfigured.

**Resolution:**

1. **Local development:** Always access the app through the SWA CLI proxy (`localhost:4280`), not the direct Blazor URL (`localhost:5000`).

2. **Production:** API calls should go through the SWA reverse proxy (same domain). If CORS headers are still needed:
   ```json
   // local.settings.json
   {
     "Host": {
       "CORS": "http://localhost:4280",
       "CORSCredentials": true
     }
   }
   ```

3. **Custom domain:** Ensure the SWA custom domain is correctly configured and the app is not making cross-origin requests.

### Authentication Token Refresh Failures

**Symptom:** Users are unexpectedly logged out or API calls return 401 after being signed in.

**Diagnosis:**

1. Check browser DevTools → Console for MSAL errors
2. Check Application → Local Storage for MSAL token cache state
3. Verify the token has not expired:
   ```javascript
   // In browser console
   const accounts = msalInstance.getAllAccounts();
   console.log(accounts);
   ```

**Common causes and resolutions:**

| Cause | Resolution |
|-------|------------|
| Token expired while offline | MSAL cannot refresh tokens without network access. Queue API calls and retry when online. |
| Redirect URI mismatch | Ensure the Entra ID app registration includes the exact redirect URI used by the app. |
| Third-party cookie blocking | Safari and some browsers block third-party cookies needed for silent token refresh. Use `iframe` fallback or prompt for interactive login. |
| Pop-up blocked | If using pop-up login flow, browser may block it. Switch to redirect flow. |

**Recommended MSAL configuration for offline-resilient auth:**

```csharp
// Program.cs - Configure MSAL with fallback
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.Cache.CacheLocation = "localStorage";
    options.ProviderOptions.LoginMode = "redirect";
});
```

### Service Worker Cache Stale

**Symptom:** Users see an old version of the app after deployment.

**Diagnosis:** The service worker is serving cached assets from a previous build.

**Resolution:**

1. **Automatic:** The `service-worker.published.js` uses cache-busting based on content hashes. When new assets are deployed, the service worker detects changes and updates.

2. **Force update:** Users can:
   - Hard refresh: `Ctrl + Shift + R` (Windows) or `Cmd + Shift + R` (Mac)
   - Clear site data: DevTools → Application → Storage → Clear site data

3. **Programmatic notification:** Implement an update notification in the app:
   ```javascript
   // In service-worker.published.js or app code
   navigator.serviceWorker.addEventListener('controllerchange', () => {
       window.location.reload();
   });
   ```

4. **Verify service worker version:** Check DevTools → Application → Service Workers. Compare the script URL hash with the latest deployed version.

---

## Security Operations

### Rotating Secrets in Key Vault

Regularly rotate secrets stored in Key Vault:

```bash
# List current secrets
az keyvault secret list --vault-name <vault-name> --output table

# Create a new version of a secret (automatically rotates)
az keyvault secret set \
  --vault-name <vault-name> \
  --name "SqlConnectionString" \
  --value "<new-connection-string>"
```

After rotation:
1. Restart the Azure Functions app to pick up the new secret (if using Key Vault references):
   ```bash
   # Static Web Apps managed Functions restart on config change
   az staticwebapp appsettings set --name <swa-name> --resource-group <rg> \
     --setting-names "TRIGGER_RESTART=$(date +%s)"
   ```
2. Verify API connectivity after rotation

### Storage Account Key Rotation

```bash
# Regenerate key1
az storage account keys renew \
  --account-name <storage-account> \
  --resource-group rg-blazorpwa-prod \
  --key key1

# Update the connection string in Key Vault
az keyvault secret set \
  --vault-name <vault-name> \
  --name "BlobStorageConnectionString" \
  --value "<new-connection-string-with-key1>"
```

> **Best practice:** Use managed identities instead of storage account keys where possible. Managed identities eliminate the need for key rotation.

### Reviewing Entra ID Sign-In Logs

Monitor for suspicious activity:

1. Azure Portal → **Microsoft Entra ID** → **Sign-in logs**
2. Filter by application: `BlazorWASM-PWA`
3. Look for:
   - Sign-ins from unexpected locations
   - Multiple failed sign-in attempts
   - Sign-ins outside normal business hours

**KQL query for sign-in anomalies (in Log Analytics):**

```kusto
SigninLogs
| where AppDisplayName == "BlazorWASM-PWA"
| where ResultType != 0  // Non-successful sign-ins
| summarize FailedAttempts = count() by UserPrincipalName, IPAddress, Location
| where FailedAttempts > 5
| order by FailedAttempts desc
```

### Certificate Management

Azure Static Web Apps automatically manage TLS certificates for custom domains. For custom certificates:

```bash
# Upload a custom certificate
az staticwebapp hostname set \
  --name <swa-name> \
  --resource-group rg-blazorpwa-prod \
  --hostname app.yourdomain.com
```

Monitor certificate expiration:

```bash
az staticwebapp hostname list \
  --name <swa-name> \
  --resource-group rg-blazorpwa-prod \
  --output table
```

---

## Cost Optimization Tips

### Right-Size Resources

| Resource | Cost Optimization |
|----------|-------------------|
| Azure SQL | Start with Basic/S0 tier and scale up based on DTU usage. Use serverless tier for dev/staging (auto-pause after inactivity). |
| Storage Account | Use Cool tier for infrequently accessed blobs. Implement lifecycle policies to move old blobs to Cool/Archive. |
| Static Web Apps | Free tier is sufficient for development. Standard tier for production with custom domains. |
| Application Insights | Set daily data cap to control ingestion costs. Use sampling for high-traffic scenarios. |

### Implement Azure SQL Serverless (Dev/Staging)

```bash
az sql db update \
  --resource-group rg-blazorpwa-dev \
  --server <sql-server> \
  --name BlazorPWA \
  --edition GeneralPurpose \
  --compute-model Serverless \
  --auto-pause-delay 60 \
  --min-capacity 0.5 \
  --max-capacity 2
```

This pauses the database after 60 minutes of inactivity, reducing costs to near-zero for idle environments.

### Storage Lifecycle Policies

Automatically tier blobs based on age:

```json
{
  "rules": [
    {
      "name": "MoveToCool",
      "type": "Lifecycle",
      "definition": {
        "filters": { "blobTypes": ["blockBlob"] },
        "actions": {
          "baseBlob": {
            "tierToCool": { "daysAfterModificationGreaterThan": 30 },
            "tierToArchive": { "daysAfterModificationGreaterThan": 365 },
            "delete": { "daysAfterModificationGreaterThan": 2555 }
          }
        }
      }
    }
  ]
}
```

### Monitor Costs

```bash
# View current month costs by resource
az cost management query \
  --type ActualCost \
  --timeframe MonthToDate \
  --dataset-grouping name=ResourceGroup type=Dimension
```

Set up budget alerts to avoid surprises:

```bash
az consumption budget create \
  --budget-name "BlazorPWA-Monthly" \
  --amount 100 \
  --resource-group rg-blazorpwa-prod \
  --time-grain Monthly \
  --start-date 2025-01-01 \
  --end-date 2026-01-01
```

---

## Maintenance Windows

### Recommended Maintenance Schedule

| Task | Frequency | Description |
|------|-----------|-------------|
| Review Application Insights | Daily | Check for new errors, performance regressions |
| Database backup verification | Weekly | Verify automated backups are running |
| Key rotation | Every 90 days | Rotate storage keys and other secrets |
| Dependency updates | Monthly | Update NuGet packages, check for security advisories |
| Database index maintenance | Monthly | Review and optimize query performance |
| Cost review | Monthly | Review spending and right-size resources |
| Entra ID sign-in audit | Monthly | Review sign-in logs for anomalies |
| DR test | Quarterly | Test database restore and failover procedures |

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Local development setup
- [Deployment Guide](DEPLOYMENT.md) — Deploy to Azure
- [Architecture](ARCHITECTURE.md) — System design and offline-first patterns
- [User Guide](USER-GUIDE.md) — End-user documentation
