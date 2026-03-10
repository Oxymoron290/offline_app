# Architecture

This document describes the system architecture of the BlazorPWA field service application — an offline-first Progressive Web App built for case workers who enter facilities with limited or no connectivity.

---

## Table of Contents

- [System Overview](#system-overview)
- [Architecture Diagram](#architecture-diagram)
- [Data Flow Diagrams](#data-flow-diagrams)
- [Sync Strategy](#sync-strategy)
- [Conflict Resolution](#conflict-resolution)
- [IndexedDB Schema](#indexeddb-schema)
- [Service Bus Queue Design](#service-bus-queue-design)
- [Security Architecture](#security-architecture)
- [Technology Choices](#technology-choices)

---

## System Overview

The BlazorPWA application is designed around the **offline-first** principle: the client application must be fully functional without a network connection. All data entry, photo capture, video recording, and document attachment happen locally in the browser's IndexedDB. When connectivity is restored, a sync engine reconciles local changes with the server.

**Key design goals:**
- **Offline resilience:** Field workers can complete entire site visits without connectivity.
- **Data integrity:** No data loss, even during interrupted syncs.
- **Idempotent sync:** Operations can be safely retried without side effects.
- **Scalable media pipeline:** Large media files are processed asynchronously via a message queue.
- **Secure by default:** Managed Identity, Key Vault, HTTPS everywhere.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                     FIELD DEVICE (Browser)                          │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                    Blazor WASM PWA                             │  │
│  │                                                               │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────┐  │  │
│  │  │   Pages &    │  │  Services    │  │  JS Interop Layer   │  │  │
│  │  │  Components  │  │  (C#/.NET)   │  │  (DexieJS Bridge)   │  │  │
│  │  └──────┬───────┘  └──────┬───────┘  └──────────┬──────────┘  │  │
│  │         │                 │                      │             │  │
│  │         └────────┬────────┘                      │             │  │
│  │                  │                               │             │  │
│  │  ┌───────────────▼───────────────────────────────▼──────────┐  │  │
│  │  │                   IndexedDB (DexieJS)                     │  │  │
│  │  │  ┌──────────┐ ┌───────────┐ ┌────────────┐ ┌──────────┐ │  │  │
│  │  │  │ entities │ │ syncQueue │ │ mediaBlobs │ │  config  │ │  │  │
│  │  │  └──────────┘ └───────────┘ └────────────┘ └──────────┘ │  │  │
│  │  └──────────────────────────────────────────────────────────┘  │  │
│  │                                                               │  │
│  │  ┌────────────────────┐  ┌──────────────────────────────────┐ │  │
│  │  │  Service Worker     │  │  Connectivity Monitor            │ │  │
│  │  │  (Cache & Offline)  │  │  (Online/Offline Detection)      │ │  │
│  │  └────────────────────┘  └──────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                        HTTPS (TLS 1.2+)
                             │
┌────────────────────────────▼────────────────────────────────────────┐
│                        AZURE CLOUD                                  │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  App Service — API  (ASP.NET Core Minimal API)               │   │
│  │  ┌────────────────┐ ┌──────────────┐ ┌────────────────────┐ │   │
│  │  │ Entity         │ │ Sync         │ │ Media Upload       │ │   │
│  │  │ Endpoints      │ │ Endpoints    │ │ Endpoint           │ │   │
│  │  │ (CRUD)         │ │ (Batch Ops)  │ │ (Multipart)       │ │   │
│  │  └────────┬───────┘ └──────┬───────┘ └─────────┬──────────┘ │   │
│  │           │                │                    │            │   │
│  │  ┌────────▼────────────────▼────────┐  ┌───────▼──────────┐ │   │
│  │  │  Conflict Resolver               │  │  Service Bus     │ │   │
│  │  │  (Version Comparison)            │  │  Publisher       │ │   │
│  │  └────────┬─────────────────────────┘  └───────┬──────────┘ │   │
│  └───────────┼────────────────────────────────────┼─────────────┘   │
│              │                                    │                  │
│  ┌───────────▼──────────┐          ┌──────────────▼──────────────┐  │
│  │  Azure SQL Database  │          │  Azure Service Bus          │  │
│  │  (EF Core)           │          │  ┌───────────────────────┐  │  │
│  │  ├─ Reports          │          │  │ media-processing      │  │  │
│  │  ├─ Attachments      │          │  │ queue                 │  │  │
│  │  ├─ SyncOperations   │          │  │ (+ dead-letter queue) │  │  │
│  │  └─ Users            │          │  └───────────┬───────────┘  │  │
│  └──────────────────────┘          └──────────────┼──────────────┘  │
│                                                   │                  │
│                                    ┌──────────────▼──────────────┐  │
│                                    │  App Service — Worker       │  │
│                                    │  (Background Service)       │  │
│                                    │                             │  │
│                                    │  ┌───────────────────────┐  │  │
│                                    │  │  Media Processor      │  │  │
│                                    │  │  (Resize, Transcode,  │  │  │
│                                    │  │   Upload to Blob)     │  │  │
│                                    │  └───────────┬───────────┘  │  │
│                                    └──────────────┼──────────────┘  │
│                                                   │                  │
│                                    ┌──────────────▼──────────────┐  │
│                                    │  Azure Blob Storage         │  │
│                                    │  ├─ photos/                 │  │
│                                    │  ├─ videos/                 │  │
│                                    │  └─ documents/              │  │
│                                    └─────────────────────────────┘  │
│                                                                     │
│  ┌─────────────────────┐  ┌──────────────────────────────────────┐  │
│  │  Azure Key Vault    │  │  Application Insights                │  │
│  │  (Secrets & Keys)   │  │  (Telemetry & Monitoring)            │  │
│  └─────────────────────┘  └──────────────────────────────────────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Diagrams

### Online Flow

When the device has connectivity, operations are sent directly to the API:

```
User Action → Save to IndexedDB → Send to API → API writes to Azure SQL
                                                → API returns serverVersion
                                  ← Update local entity with serverVersion
```

1. User fills out a form and clicks **Save**.
2. Data is written to the `entities` store in IndexedDB (syncStatus = `syncing`).
3. The sync service sends the operation to the API immediately.
4. The API validates the data, writes to Azure SQL, and returns the `serverVersion`.
5. The client updates the local entity with the new `serverVersion` and sets syncStatus = `synced`.

### Offline Capture Flow

When offline, all operations are queued locally:

```
User Action → Save to IndexedDB (entities store, syncStatus = "pending")
            → Enqueue operation in syncQueue store
            → If media: save binary to mediaBlobs store
            → UI shows "Pending Sync" indicator
```

1. User fills out a form and clicks **Save**.
2. Data is written to the `entities` store with syncStatus = `pending`.
3. An operation record is enqueued in the `syncQueue` store.
4. If media is attached, the binary data is stored in the `mediaBlobs` store.
5. The UI displays a "Pending Sync" badge on the record.

### Sync Flow (Reconnection)

When connectivity is restored, the sync engine processes the queue:

```
Connectivity Detected
  → Read syncQueue (chronological order)
  → Batch operations (up to 50 per batch)
  → POST /api/sync/batch
  → For each operation result:
      Success → Update entity syncStatus = "synced", remove from syncQueue
      Conflict → Mark entity syncStatus = "conflict", store both versions
      Failure → Increment retryCount, keep in syncQueue
  → Repeat until syncQueue is empty or max retries exceeded
```

1. The Connectivity Monitor detects a network change (online event or successful ping).
2. The sync engine reads all `pending` operations from the `syncQueue`, ordered by `createdAt`.
3. Operations are batched in groups of up to 50 and sent to `POST /api/sync/batch`.
4. The API processes each operation and returns per-operation results.
5. Successful operations: local entity updated with server version, removed from queue.
6. Conflicts: entity flagged for manual resolution, both versions stored.
7. Failures: retryCount incremented; after max retries (5), marked as `failed`.

### Media Upload Flow (via Service Bus)

Large media files follow an asynchronous pipeline:

```
Client → POST /api/media (multipart upload)
  → API receives file, validates, stores temporarily
  → API publishes message to Service Bus "media-processing" queue
  → API returns 202 Accepted with attachmentId
  
Worker (async):
  → Receives message from Service Bus queue
  → Downloads temporary file from API staging area
  → Processes media (resize photos, transcode video if needed)
  → Uploads processed file to Azure Blob Storage
  → Updates attachment record in Azure SQL (blobUrl, status = "processed")
  → Completes Service Bus message
```

This pipeline ensures that large media uploads do not block the API or the client sync process.

---

## Sync Strategy

### Operation-Based Sync

The sync engine uses an **operation-based** (event-sourced) approach rather than state-based sync. Instead of comparing full entity state, the client sends discrete operations that describe what changed.

### Operation Types

| Operation | Description |
|-----------|-------------|
| `CREATE_REPORT` | Create a new inspection report |
| `UPDATE_REPORT` | Update fields on an existing report |
| `ADD_PHOTO` | Attach a photo to a report |
| `ADD_VIDEO` | Attach a video to a report |
| `ADD_DOCUMENT` | Attach a document (PDF, Word, etc.) to a report |
| `ADD_NOTE` | Add a text note to a report |
| `COMPLETE_TASK` | Mark a checklist task as complete |

### Operation Schema

Each operation is an immutable event:

```json
{
  "id": "op-a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "operationType": "UPDATE_REPORT",
  "entityId": "rpt-11223344-5566-7788-99aa-bbccddeeff00",
  "payload": {
    "facilityName": "Sunrise Care Center",
    "inspectionDate": "2024-03-15",
    "notes": "Updated facility name after verification."
  },
  "clientVersion": 3,
  "serverVersion": 2,
  "timestamp": "2024-03-15T14:30:00Z",
  "deviceId": "device-abc123"
}
```

### Key design properties

- **Immutable:** Operations are never modified after creation.
- **Idempotent:** Each operation has a unique `id`. The server detects and ignores duplicate submissions.
- **Ordered:** Operations are applied in `timestamp` order within each entity.
- **Batched:** The client sends operations in chronological batches (up to 50 per request) to reduce round-trips.

### Sync Endpoint

```
POST /api/sync/batch
Content-Type: application/json

{
  "deviceId": "device-abc123",
  "operations": [ ... ]
}
```

Response:

```json
{
  "results": [
    { "operationId": "op-...", "status": "applied", "serverVersion": 4 },
    { "operationId": "op-...", "status": "duplicate", "serverVersion": 3 },
    { "operationId": "op-...", "status": "conflict", "serverVersion": 5, "serverData": { ... } }
  ]
}
```

---

## Conflict Resolution

Conflicts occur when two devices (or a device and a server-side edit) modify the same entity concurrently. The conflict resolution strategy depends on the type of data:

### Resolution Rules

| Scenario | Strategy | Rationale |
|----------|----------|-----------|
| **New records** (never synced) | Client wins | The server has no existing data to conflict with. |
| **Immutable data** (completed reports, uploaded media) | Server wins | Once data is finalized, the server version is authoritative. |
| **Concurrent edits** (same field modified on both sides) | Manual resolution | Present both versions to the user for selection. |
| **Non-overlapping edits** (different fields modified) | Auto-merge | Apply both changes without conflict. |

### Version Tracking

Every entity has two version numbers:

- **`clientVersion`**: Incremented each time the client modifies the entity locally.
- **`serverVersion`**: Set by the server each time it accepts a change.

When the client sends an operation, it includes the `serverVersion` it last saw. The server compares this to the current `serverVersion`:

```
if operation.serverVersion == entity.currentServerVersion:
    # No conflict — apply the operation
    entity.currentServerVersion += 1
elif operation.serverVersion < entity.currentServerVersion:
    # Potential conflict — check field overlap
    if fieldsOverlap(operation.payload, pendingChanges):
        return ConflictResult(serverData, clientData)
    else:
        # Auto-merge non-overlapping changes
        applyMerge(operation.payload)
```

### Conflict Resolution UI

When a conflict is detected:

1. The entity's `syncStatus` is set to `conflict` in IndexedDB.
2. The UI displays an orange ⚠ indicator on the affected record.
3. The user opens the record and sees a side-by-side comparison.
4. The user selects which version to keep (or merges manually).
5. A new operation is created with the resolved data and sent to the server.

---

## IndexedDB Schema

The client stores all data in IndexedDB using [DexieJS](https://dexie.org/) as the wrapper library. The database is named `blazorpwa-db` with the following object stores:

### `entities` store

Stores all domain entities (reports, tasks, etc.) as JSON documents.

| Field | Type | Indexed | Description |
|-------|------|---------|-------------|
| `id` | string (UUID) | Primary key | Globally unique entity identifier, generated client-side |
| `type` | string | Yes | Entity type: `report`, `task`, `note`, `attachment` |
| `data` | object (JSON) | No | The entity payload (all fields) |
| `version` | number | No | Local version counter |
| `serverVersion` | number | No | Last known server version (null if never synced) |
| `syncStatus` | string | Yes | `synced`, `pending`, `syncing`, `conflict`, `failed` |
| `createdAt` | DateTime | No | When the entity was first created |
| `updatedAt` | DateTime | Yes | When the entity was last modified |

**DexieJS schema definition:**
```javascript
entities: '&id, type, syncStatus, updatedAt'
```

### `syncQueue` store

Stores pending sync operations in chronological order.

| Field | Type | Indexed | Description |
|-------|------|---------|-------------|
| `id` | string (UUID) | Primary key | Unique operation identifier |
| `operationType` | string | Yes | Operation type (CREATE_REPORT, UPDATE_REPORT, etc.) |
| `entityId` | string (UUID) | Yes | The entity this operation targets |
| `payload` | object (JSON) | No | Operation data |
| `status` | string | Yes | `pending`, `sending`, `sent`, `failed` |
| `retryCount` | number | No | Number of send attempts (max 5) |
| `createdAt` | DateTime | Yes | Timestamp for ordering |

**DexieJS schema definition:**
```javascript
syncQueue: '&id, operationType, entityId, status, createdAt'
```

### `mediaBlobs` store

Stores binary media data (photos, videos, documents) locally until synced.

| Field | Type | Indexed | Description |
|-------|------|---------|-------------|
| `id` | string (UUID) | Primary key | Unique media identifier |
| `reportId` | string (UUID) | Yes | Parent report ID |
| `fileName` | string | No | Original file name |
| `contentType` | string | No | MIME type (image/jpeg, video/mp4, etc.) |
| `blob` | Blob (binary) | No | The actual file data |
| `thumbnailBlob` | Blob (binary) | No | Thumbnail for photos/videos (optional) |
| `fileSizeBytes` | number | No | File size for upload progress tracking |
| `syncStatus` | string | Yes | `pending`, `uploading`, `uploaded`, `failed` |
| `createdAt` | DateTime | No | Capture timestamp |

**DexieJS schema definition:**
```javascript
mediaBlobs: '&id, reportId, syncStatus'
```

### `config` store

Key-value store for application configuration and state.

| Field | Type | Indexed | Description |
|-------|------|---------|-------------|
| `key` | string | Primary key | Configuration key |
| `value` | any | No | Configuration value |

**Example entries:**

| Key | Value | Purpose |
|-----|-------|---------|
| `lastSyncTimestamp` | `"2024-03-15T14:30:00Z"` | Track last successful sync |
| `deviceId` | `"device-abc123"` | Unique device identifier |
| `apiBaseUrl` | `"https://app-api-blazorpwa-dev.azurewebsites.net"` | API endpoint |
| `syncBatchSize` | `50` | Operations per sync batch |

**DexieJS schema definition:**
```javascript
config: '&key'
```

### Full DexieJS Database Definition

```javascript
import Dexie from 'dexie';

const db = new Dexie('blazorpwa-db');

db.version(1).stores({
  entities:   '&id, type, syncStatus, updatedAt',
  syncQueue:  '&id, operationType, entityId, status, createdAt',
  mediaBlobs: '&id, reportId, syncStatus',
  config:     '&key'
});

export default db;
```

---

## Service Bus Queue Design

Azure Service Bus decouples media uploads from the API request/response cycle, ensuring that large file processing does not block the sync flow.

### Queue: `media-processing`

| Property | Value |
|----------|-------|
| Queue name | `media-processing` |
| Max delivery count | 5 |
| Lock duration | 5 minutes |
| Message TTL | 7 days |
| Dead-letter queue | Enabled (auto) |
| Duplicate detection | Enabled (10-minute window) |
| Sessions | Disabled |
| Partitioning | Disabled |

### Message Schema

```json
{
  "id": "msg-a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "reportId": "rpt-11223344-5566-7788-99aa-bbccddeeff00",
  "attachmentId": "att-aabbccdd-eeff-0011-2233-445566778899",
  "fileName": "facility-entrance.jpg",
  "contentType": "image/jpeg",
  "mediaType": "photo",
  "blobPath": "staging/att-aabbccdd-eeff-0011-2233-445566778899/facility-entrance.jpg",
  "fileSizeBytes": 4521938,
  "uploadedBy": "user@example.com",
  "timestamp": "2024-03-15T14:35:00Z"
}
```

### Processing Pipeline

```
API receives media upload
  → Validates file (type, size)
  → Stores raw file in staging blob container
  → Publishes message to "media-processing" queue
  → Returns 202 Accepted

Worker receives message
  → Downloads raw file from staging container
  → Processes based on mediaType:
      photo:    Resize, generate thumbnail, strip EXIF GPS (privacy)
      video:    Validate format, generate thumbnail frame
      document: Validate, virus scan
  → Uploads processed file to permanent blob container:
      photos/{reportId}/{attachmentId}.jpg
      videos/{reportId}/{attachmentId}.mp4
      documents/{reportId}/{attachmentId}.pdf
  → Updates attachment record in Azure SQL:
      blobUrl, thumbnailUrl, status = "processed", processedAt
  → Completes (removes) the Service Bus message
```

### Retry and Dead-Letter Policy

- If the worker fails to process a message, it is automatically returned to the queue.
- After **5 failed delivery attempts**, the message is moved to the **dead-letter queue**.
- Dead-lettered messages retain the original body plus a `DeadLetterReason` property.
- Operations monitors the dead-letter queue depth (see [Operations Guide](OPERATIONS.md)).
- Lock duration is set to **5 minutes** to accommodate large video file processing.

---

## Security Architecture

### Managed Identity

All Azure service-to-service communication uses **Azure Managed Identity** (system-assigned) — no connection strings or keys stored in app configuration for Azure services.

| Source | Target | Auth Method |
|--------|--------|-------------|
| API App Service | Azure SQL Database | Managed Identity (AAD auth) |
| API App Service | Azure Service Bus | Managed Identity (Azure.Messaging.ServiceBus) |
| API App Service | Azure Blob Storage | Managed Identity (Azure.Storage.Blobs) |
| API App Service | Azure Key Vault | Managed Identity (SecretClient) |
| Worker App Service | Azure Service Bus | Managed Identity |
| Worker App Service | Azure Blob Storage | Managed Identity |
| Worker App Service | Azure SQL Database | Managed Identity |
| Worker App Service | Azure Key Vault | Managed Identity |

### Key Vault

Azure Key Vault stores:
- SQL connection strings (for local dev/fallback)
- Third-party API keys (if any)
- Custom encryption keys
- Certificate references

Key Vault is accessed via Managed Identity. In code:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://kv-blazorpwa-{environment}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### HTTPS Enforcement

- All traffic is TLS 1.2+ encrypted.
- Azure App Service is configured with `httpsOnly: true`.
- HTTP requests are automatically redirected to HTTPS.
- The Blazor WASM client enforces HTTPS in the service worker registration.

### CORS Configuration

The API allows requests only from known origins:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(
                "https://app-client-blazorpwa-dev.azurewebsites.net",
                "https://app-client-blazorpwa-staging.azurewebsites.net",
                "https://app-client-blazorpwa-prod.azurewebsites.net")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

### Service Worker Security

- The service worker is served with the `Service-Worker-Allowed` header set to `/`.
- Cache storage is scoped to the application origin.
- The service worker does **not** cache API responses containing sensitive data — only static assets (WASM DLLs, CSS, images).
- IndexedDB data is encrypted at rest on devices that support it (OS-level encryption).

### Content Security Policy

The API sets the following CSP headers:

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'wasm-unsafe-eval';
  style-src 'self' 'unsafe-inline';
  img-src 'self' blob: data:;
  connect-src 'self' https://*.azurewebsites.net;
  media-src 'self' blob:;
```

---

## Technology Choices

| Technology | Choice | Rationale |
|------------|--------|-----------|
| **Frontend framework** | Blazor WebAssembly | Full .NET runtime in the browser; shared models with the API; strong typing reduces bugs in complex form logic. |
| **PWA capability** | Service Worker + Manifest | Native-like install experience; offline caching of static assets; background sync capability. |
| **Client-side storage** | IndexedDB via DexieJS | Large storage quota (browser-dependent, typically 50%+ of disk); binary blob support for media; indexed queries for performance. Dexie provides a clean Promise-based API over raw IndexedDB. |
| **Backend framework** | ASP.NET Core Minimal API | Lightweight, high-performance HTTP API; clean endpoint definitions; excellent Azure integration. |
| **Database** | Azure SQL Database | Relational model suits structured inspection data; EF Core provides migrations and LINQ; built-in backup and geo-replication. |
| **ORM** | Entity Framework Core 8 | Code-first migrations; LINQ query support; excellent Azure SQL integration. |
| **Message queue** | Azure Service Bus | Reliable message delivery with dead-letter support; managed service requiring zero infrastructure maintenance; built-in retry policies. |
| **Blob storage** | Azure Blob Storage | Cost-effective storage for large media files; tiering support (Hot/Cool/Archive); CDN integration for global access. |
| **IaC** | Azure Bicep | Native Azure ARM abstraction; type-safe; first-class Azure tooling support; simpler than raw ARM JSON. |
| **CI/CD** | GitHub Actions | Integrated with GitHub repository; marketplace actions for Azure deployment; matrix builds for testing. |
| **Identity** | Azure Managed Identity | Eliminates credential management for service-to-service auth; automatic token rotation; zero-secret deployments. |
| **Secrets** | Azure Key Vault | Centralized secret management; audit logging; RBAC-based access control; automatic secret rotation support. |
| **Monitoring** | Application Insights | Integrated .NET SDK; client-side and server-side telemetry; KQL-based querying; alerting rules. |

### Why not alternatives?

| Alternative considered | Reason not chosen |
|------------------------|-------------------|
| React / Angular SPA | Blazor WASM allows shared C# models between client and server, reducing serialization bugs and code duplication. |
| SQLite (client-side) | IndexedDB has broader PWA support and no WASM overhead. SQLite WASM is experimental and adds complexity. |
| Azure Queue Storage | Service Bus provides dead-letter queues, message locking, duplicate detection, and longer lock durations needed for large media processing. |
| Cosmos DB | Relational schema is a better fit for structured inspection forms. Azure SQL is more cost-effective at this scale. |
| Terraform | Bicep is Azure-native with better type safety and Azure-specific tooling. |
| SignalR (for sync) | HTTP-based batch sync is simpler, more reliable across poor connections, and doesn't require persistent WebSocket connections. |

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Set up your development environment
- [Deployment Guide](DEPLOYMENT.md) — Deploy to Azure
- [Operations Guide](OPERATIONS.md) — Monitor and troubleshoot
- [User Guide](USER-GUIDE.md) — End-user documentation for field workers
