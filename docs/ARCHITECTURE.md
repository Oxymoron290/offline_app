# Architecture

Technical architecture, design decisions, and implementation details for the BlazorWASM_PWA offline-first case worker application.

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Case Worker Device                             │
│                                                                         │
│  ┌─────────────────────┐     ┌──────────────────────────────────────┐  │
│  │   Blazor WASM App   │     │           IndexedDB (Dexie.js)       │  │
│  │                     │     │                                      │  │
│  │  ┌───────────────┐  │     │  ┌────────────┐  ┌───────────────┐  │  │
│  │  │  UI / Pages   │  │◄───►│  │  Entities  │  │OperationsQueue│  │  │
│  │  └───────┬───────┘  │     │  └────────────┘  └───────────────┘  │  │
│  │          │          │     │  ┌────────────┐  ┌───────────────┐  │  │
│  │  ┌───────▼───────┐  │     │  │   Photos   │  │   Documents   │  │  │
│  │  │ Sync Service  │──┼────►│  └────────────┘  └───────────────┘  │  │
│  │  └───────┬───────┘  │     │  ┌────────────┐                     │  │
│  │          │          │     │  │   Videos    │                     │  │
│  └──────────┼──────────┘     │  └────────────┘                     │  │
│             │                └──────────────────────────────────────┘  │
│  ┌──────────▼──────────┐                                               │
│  │   Service Worker    │  ← Caches static assets for offline loading   │
│  └──────────┬──────────┘                                               │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              │  HTTPS (when online)
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     Azure Static Web Apps                               │
│                                                                         │
│  ┌──────────────────┐    ┌──────────────────────────────────────────┐  │
│  │  Static Hosting  │    │         Azure Functions API              │  │
│  │  (Blazor WASM    │    │                                          │  │
│  │   assets)        │    │  ┌────────────┐  ┌───────────────────┐  │  │
│  └──────────────────┘    │  │ Sync API   │  │ Entity CRUD API   │  │  │
│                          │  └─────┬──────┘  └────────┬──────────┘  │  │
│                          │        │                   │             │  │
│                          │  ┌─────▼───────────────────▼──────────┐  │  │
│                          │  │      EF Core / Data Layer          │  │  │
│                          │  └─────┬───────────────────┬──────────┘  │  │
│                          └────────┼───────────────────┼─────────────┘  │
└────────────────────────────────────┼───────────────────┼────────────────┘
                                     │                   │
                    ┌────────────────▼──┐          ┌─────▼──────────────┐
                    │  Azure SQL        │          │  Azure Blob        │
                    │  Database         │          │  Storage           │
                    │                   │          │                    │
                    │  - Entities       │          │  - Photos          │
                    │  - Users          │          │  - Videos          │
                    │  - Audit logs     │          │  - Documents       │
                    └───────────────────┘          └────────────────────┘

           ┌────────────────┐          ┌─────────────────────────┐
           │  Azure Key     │          │  Microsoft Entra ID     │
           │  Vault         │          │  (Authentication)       │
           │  - Secrets     │          │  - OAuth 2.0 / OIDC     │
           │  - Conn strings│          │  - MSAL                 │
           └────────────────┘          └─────────────────────────┘
```

---

## Offline-First Architecture

### Why Operation-Based Sync (Not State-Based)

The application uses an **operation-based** (event-sourced) sync model rather than a **state-based** (snapshot) model. This is a deliberate design decision.

**State-based sync** transmits the full current state of each entity. This approach is simpler but has significant drawbacks for offline-first applications:

- Large payload sizes (entire entities sent even for small changes)
- Difficult to resolve conflicts (which field changed?)
- Loss of intent (what was the user trying to do?)
- Cannot partially apply changes

**Operation-based sync** transmits individual operations (create, update, delete) as discrete units:

- Each operation captures user intent
- Operations can be replayed, retried, or skipped individually
- Smaller payloads (only the change, not the full state)
- Better conflict detection (field-level changes are explicit)
- Natural audit trail (every operation is a log entry)

| Aspect | State-Based | Operation-Based (Our Choice) |
|--------|-------------|------------------------------|
| Payload size | Full entity per sync | Only changed fields |
| Conflict resolution | Difficult (whole entity) | Granular (per field/operation) |
| Offline queue | Replace on each save | Append each operation |
| Audit trail | Requires separate logging | Built into the model |
| Partial failure handling | All or nothing | Per-operation retry |
| Complexity | Lower | Higher (but worthwhile) |

---

## Data Flow

### Write Path (User Creates/Edits Data)

```
User Action
    │
    ▼
┌─────────────────────────────┐
│ 1. Validate form input      │
│ 2. Save entity to IndexedDB │ ← Immediate, works offline
│    (Entities store)          │
│ 3. Queue sync operation      │
│    (OperationsQueue store)   │
│ 4. Update UI (optimistic)    │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Sync Service detects:       │
│   - Connectivity available  │ ← Watches navigator.onLine + fetch probe
│   - Operations in queue     │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Batch operations from queue │
│ POST /api/sync              │ ← Sends batch of operations to API
│   {                         │
│     operations: [...],      │
│     lastSyncTimestamp: ...  │
│   }                         │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ API processes each          │
│ operation:                  │
│   - Validate                │
│   - Apply to Azure SQL      │
│   - Upload blobs to Storage │
│   - Return results          │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Client receives results:    │
│   - Mark operations as      │
│     complete in IndexedDB   │
│   - Update entities with    │
│     server-assigned data    │
│   - Handle any errors       │
└─────────────────────────────┘
```

### Read Path (Pulling Server Updates)

```
Sync Service triggers pull
    │
    ▼
GET /api/sync?since={lastSyncTimestamp}
    │
    ▼
API returns:
  - Entities modified since timestamp
  - Deleted entity IDs
  - New server timestamp
    │
    ▼
Client merges into IndexedDB:
  - Upsert modified entities
  - Remove deleted entities
  - Update lastSyncTimestamp
    │
    ▼
UI re-renders with updated data
```

---

## IndexedDB Schema

The client uses [Dexie.js](https://dexie.org/) for IndexedDB access via JavaScript interop. Dexie provides a clean API, robust transaction support, and excellent IndexedDB compatibility across browsers.

### Database Definition

```javascript
const db = new Dexie('BlazorPWA');

db.version(1).stores({
  // Core entity data
  entities: 'id, entityType, name, updatedAt, createdBy, isDeleted',

  // Binary data stores (photos, videos, documents)
  photos: 'id, entityId, fileName, syncStatus, createdAt',
  videos: 'id, entityId, fileName, syncStatus, createdAt',
  documents: 'id, entityId, fileName, syncStatus, createdAt',

  // Sync operation queue
  operationsQueue: '++localId, operationId, operationType, entityId, status, createdAt, retryCount',

  // Sync metadata
  syncMeta: 'key'
});
```

### Store Details

**Entities Store**

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Primary key, client-generated |
| `entityType` | string | Type discriminator (e.g., "case_report", "inspection") |
| `name` | string | Display name |
| `description` | string | Description |
| `jsonData` | string | Flexible JSON payload for form fields |
| `blobReferences` | array | References to associated photos/videos/documents |
| `createdAt` | datetime | Creation timestamp |
| `updatedAt` | datetime | Last modification timestamp |
| `createdBy` | string | User ID of creator |
| `isDeleted` | boolean | Soft delete flag |

**Photos / Videos / Documents Stores**

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Primary key, client-generated |
| `entityId` | string (GUID) | Foreign key to parent entity |
| `fileName` | string | Original file name |
| `contentType` | string | MIME type (e.g., "image/jpeg") |
| `sizeBytes` | number | File size in bytes |
| `data` | Blob | Binary file data |
| `syncStatus` | string | "pending" / "syncing" / "synced" / "error" |
| `blobUrl` | string | Server blob URL (populated after sync) |
| `createdAt` | datetime | Capture/upload timestamp |

**OperationsQueue Store**

| Field | Type | Description |
|-------|------|-------------|
| `localId` | number | Auto-increment local ID |
| `operationId` | string (GUID) | Unique operation identifier |
| `operationType` | string | CreateEntity, UpdateEntity, DeleteEntity, UploadPhoto, etc. |
| `entityType` | string | Type of entity being operated on |
| `entityId` | string (GUID) | Target entity ID |
| `payloadJson` | string | Operation payload (JSON) |
| `blobReferenceIds` | array | IDs of associated blobs to upload |
| `status` | string | "pending" / "in_progress" / "completed" / "failed" |
| `createdAt` | datetime | When the operation was queued |
| `retryCount` | number | Number of retry attempts |
| `lastError` | string | Last error message (if failed) |

**SyncMeta Store**

| Key | Value | Description |
|-----|-------|-------------|
| `lastSyncTimestamp` | datetime | Last successful server sync time |
| `lastPullTimestamp` | datetime | Last successful pull of server updates |

---

## Sync Algorithm

### Queue Processing (Push)

```
FUNCTION processSyncQueue():
    IF NOT isOnline():
        RETURN  // Skip if offline

    operations = db.operationsQueue
        .where('status').equals('pending')
        .sortBy('createdAt')
        .limit(BATCH_SIZE)          // Process in batches of 20

    IF operations.length == 0:
        RETURN  // Nothing to sync

    // Mark batch as in-progress
    FOR EACH op IN operations:
        op.status = 'in_progress'
        db.operationsQueue.put(op)

    // Prepare blob uploads for operations that reference files
    blobUploads = []
    FOR EACH op IN operations:
        IF op.blobReferenceIds.length > 0:
            FOR EACH blobId IN op.blobReferenceIds:
                blob = db.photos.get(blobId)
                    ?? db.videos.get(blobId)
                    ?? db.documents.get(blobId)
                IF blob:
                    blobUploads.push({ id: blobId, data: blob.data })

    // Upload blobs first (if any)
    FOR EACH upload IN blobUploads:
        TRY:
            sasUrl = POST /api/blobs/sas-token { blobId: upload.id }
            PUT sasUrl WITH upload.data
            markBlobSynced(upload.id)
        CATCH error:
            markBlobError(upload.id, error)

    // Send operation batch to server
    TRY:
        payload = {
            operations: operations.map(toSyncOperation),
            lastSyncTimestamp: db.syncMeta.get('lastSyncTimestamp')
        }
        result = POST /api/sync payload

        IF result.success:
            FOR EACH opResult IN result.operationResults:
                IF opResult.success:
                    markOperationComplete(opResult.operationId)
                ELSE:
                    handleOperationError(opResult.operationId, opResult.error)

            // Update local entities with server data
            FOR EACH entity IN result.updatedEntities:
                db.entities.put(entity)

            db.syncMeta.put('lastSyncTimestamp', result.serverTimestamp)

    CATCH networkError:
        // Revert batch to pending for retry
        FOR EACH op IN operations:
            op.status = 'pending'
            op.retryCount += 1
            IF op.retryCount >= MAX_RETRIES:     // MAX_RETRIES = 5
                op.status = 'failed'
                op.lastError = networkError.message
            db.operationsQueue.put(op)
```

### Retry with Exponential Backoff

```
FUNCTION getRetryDelay(retryCount):
    // Exponential backoff: 2s, 4s, 8s, 16s, 32s
    baseDelay = 2000  // 2 seconds
    maxDelay = 32000  // 32 seconds
    delay = min(baseDelay * 2^retryCount, maxDelay)

    // Add jitter (±25%) to prevent thundering herd
    jitter = delay * 0.25 * (random() * 2 - 1)
    RETURN delay + jitter

FUNCTION scheduleRetry(operation):
    delay = getRetryDelay(operation.retryCount)
    setTimeout(() => processSyncQueue(), delay)
```

### Pull Server Updates

```
FUNCTION pullServerUpdates():
    IF NOT isOnline():
        RETURN

    lastPull = db.syncMeta.get('lastPullTimestamp') ?? '1970-01-01T00:00:00Z'

    TRY:
        response = GET /api/sync?since={lastPull}

        // Apply updates to local database
        FOR EACH entity IN response.updatedEntities:
            localEntity = db.entities.get(entity.id)

            IF localEntity == null:
                // New entity from server
                db.entities.put(entity)
            ELSE IF NOT hasPendingOperations(entity.id):
                // No local changes pending — safe to overwrite
                db.entities.put(entity)
            ELSE:
                // Conflict: local changes exist for this entity
                resolveConflict(localEntity, entity)

        // Apply deletions
        FOR EACH deletedId IN response.deletedEntityIds:
            db.entities.delete(deletedId)
            db.operationsQueue.where('entityId').equals(deletedId).delete()

        db.syncMeta.put('lastPullTimestamp', response.serverTimestamp)

    CATCH error:
        log('Pull failed, will retry on next cycle', error)
```

### Connectivity Detection

```
FUNCTION isOnline():
    IF NOT navigator.onLine:
        RETURN false

    // navigator.onLine can be unreliable — verify with a fetch probe
    TRY:
        response = fetch('/api/health', { method: 'HEAD', timeout: 5000 })
        RETURN response.ok
    CATCH:
        RETURN false

// Listen for connectivity changes
window.addEventListener('online', () => {
    processSyncQueue()
    pullServerUpdates()
})

// Periodic sync check (every 60 seconds when online)
setInterval(() => {
    IF isOnline():
        processSyncQueue()
        pullServerUpdates()
}, 60000)
```

---

## Conflict Resolution

### Current Strategy: Last-Writer-Wins (v1)

For the initial version, the application uses a **last-writer-wins** strategy based on the `updatedAt` timestamp:

```
FUNCTION resolveConflict(localEntity, serverEntity):
    IF serverEntity.updatedAt > localEntity.updatedAt:
        // Server version is newer — accept server version
        db.entities.put(serverEntity)
        // Discard conflicting local operations
        db.operationsQueue
            .where('entityId').equals(localEntity.id)
            .and(op => op.status == 'pending')
            .delete()
    ELSE:
        // Local version is newer — keep local, it will sync on next push
        // No action needed; local operations will overwrite server on next sync
```

**Trade-offs:**
- Simple to implement and reason about
- No data loss if sync intervals are short
- Can lose changes if two users edit the same entity offline simultaneously (the earlier sync is overwritten)

### Future Improvements (v2+)

**Field-level merge:**
Instead of overwriting entire entities, compare individual fields and merge non-conflicting changes:
```
IF localEntity.name != serverEntity.name AND localEntity.description != serverEntity.description:
    // Different fields changed — merge both
    merged = { ...serverEntity, name: localEntity.name }
```

**Vector clocks:**
Track causality using vector clocks to detect true conflicts (concurrent edits) vs. sequential changes:
```
localClock:  { deviceA: 3, deviceB: 1 }
serverClock: { deviceA: 2, deviceB: 2 }
// Concurrent! Neither dominates the other → true conflict
```

**CRDTs (Conflict-free Replicated Data Types):**
For specific data types (counters, sets, text), use CRDTs that mathematically guarantee convergence without conflict resolution.

**Conflict queue for manual resolution:**
When an unresolvable conflict is detected, queue it for the user to review and choose the correct version.

---

## Security Model

### Authentication Flow

```
┌──────────┐         ┌─────────────┐         ┌──────────────┐
│  Blazor  │         │ Entra ID    │         │  Azure       │
│  Client  │         │ (Azure AD)  │         │  Functions   │
└────┬─────┘         └──────┬──────┘         └──────┬───────┘
     │                      │                       │
     │  1. Login redirect   │                       │
     │─────────────────────►│                       │
     │                      │                       │
     │  2. User signs in    │                       │
     │  (MFA if required)   │                       │
     │◄─────────────────────│                       │
     │                      │                       │
     │  3. ID token +       │                       │
     │     access token     │                       │
     │◄─────────────────────│                       │
     │                      │                       │
     │  4. API call with    │                       │
     │     Bearer token     │                       │
     │─────────────────────────────────────────────►│
     │                      │                       │
     │                      │  5. Validate JWT      │
     │                      │◄──────────────────────│
     │                      │  (verify signature,   │
     │                      │   issuer, audience)   │
     │                      │──────────────────────►│
     │                      │                       │
     │  6. Response         │                       │
     │◄─────────────────────────────────────────────│
```

### Client-Side Authentication (MSAL)

The Blazor WASM client uses `Microsoft.Authentication.WebAssembly.Msal` for authentication:

- **Protocol:** OAuth 2.0 Authorization Code Flow with PKCE
- **Token storage:** Browser localStorage (survives page refreshes)
- **Token refresh:** MSAL handles silent token refresh automatically via hidden iframes
- **Offline handling:** Cached tokens are used for API calls; if expired and offline, API calls are queued

### API Authentication (JWT Bearer)

The Azure Functions API validates JWT tokens using `Microsoft.Identity.Web`:

- **Token validation:** Issuer, audience, signature, expiration
- **User identification:** Claims from the JWT (oid, name, email)
- **Authorization:** Role-based access via Entra ID app roles

### Blob Storage Security

Blobs are not directly accessible. The API generates **SAS (Shared Access Signature) tokens** with limited scope:

```
Client requests upload URL
    → API generates SAS token (write-only, 15-minute expiry, specific container/blob)
    → Client uploads directly to Blob Storage using SAS URL
    → Client confirms upload to API
```

For downloads:
```
Client requests blob
    → API generates SAS token (read-only, 1-hour expiry)
    → Client downloads directly from Blob Storage
```

### Key Vault Integration

All secrets are stored in Azure Key Vault:

| Secret | Purpose |
|--------|---------|
| `SqlConnectionString` | Azure SQL Database connection string |
| `BlobStorageConnectionString` | Storage account connection string |
| `ApplicationInsightsKey` | Telemetry instrumentation key |

Azure Functions access Key Vault via **managed identity** (no credentials in code or configuration).

---

## Service Worker Strategy

The PWA service worker manages caching for offline access:

### Static Assets: Cache-First

```
Request for static asset (JS, CSS, WASM, images)
    │
    ▼
Check service worker cache
    │
    ├── Cache HIT → Return cached response (fast, works offline)
    │
    └── Cache MISS → Fetch from network
                        │
                        ├── Network success → Cache response, return to client
                        │
                        └── Network failure → Return offline fallback page
```

**Rationale:** Static assets change only on deployment. Cache-first provides instant loading and full offline support. The `service-worker.published.js` uses content hashes in cache keys, so new deployments automatically invalidate stale caches.

### API Calls: Network-First

```
Request for API endpoint (/api/*)
    │
    ▼
Attempt network fetch
    │
    ├── Network success → Return response to client
    │
    └── Network failure → Return error to client
                           (sync service handles offline queueing)
```

**Rationale:** API calls must hit the server for data freshness. The sync service (not the service worker) handles offline operation queueing. This keeps concerns separated — the service worker handles asset caching, while the sync service handles data synchronization.

### Cache Invalidation

On each deployment, the published service worker computes content hashes for all assets:

```javascript
// service-worker.published.js (auto-generated)
const cacheName = 'blazor-cache-v1';
const assetsToCache = [
  { url: '_framework/blazor.webassembly.js', hash: 'sha256-abc123...' },
  { url: '_framework/dotnet.js', hash: 'sha256-def456...' },
  // ... all static assets with content hashes
];
```

When the hash changes (new deployment), the service worker downloads the updated asset and replaces the cached version.

---

## Technology Choices Rationale

### Dexie.js over .NET IndexedDB Libraries

| Factor | Dexie.js (via JS interop) | .NET IndexedDB Libraries |
|--------|--------------------------|--------------------------|
| **Maturity** | Battle-tested, 10+ years, massive community | Relatively new, smaller community |
| **API quality** | Excellent — LINQ-like queries, transactions, versioning | Varies — some libraries are wrappers with limited features |
| **Blob support** | Excellent — native support for storing large binary data | Often limited or requires workarounds |
| **Performance** | Direct JS access to IndexedDB, no marshaling overhead | Additional overhead from .NET → JS interop on every operation |
| **Documentation** | Extensive docs, tutorials, Stack Overflow coverage | Limited documentation |

**Our choice:** Dexie.js via JavaScript interop. The JS interop boundary is crossed once per operation (not per query step), and Dexie's superior API, community support, and blob handling outweigh the indirection cost.

### Azure Static Web Apps over Azure App Service

| Factor | Static Web Apps | App Service |
|--------|----------------|-------------|
| **Hosting model** | Static files + serverless API (Azure Functions) | Full web server |
| **Cost** | Free tier available; pay per API execution | Always-on compute; minimum ~$13/month |
| **Scaling** | Automatic, serverless | Manual or auto-scale rules |
| **Deployment** | Git-integrated, preview environments per PR | Separate CI/CD setup |
| **SSL/TLS** | Automatic, free managed certificates | Manual or paid certificates |
| **Global distribution** | Built-in CDN for static assets | Requires separate CDN (Azure CDN or Front Door) |
| **Complexity** | Low — purpose-built for SPAs | Higher — general-purpose web server |

**Our choice:** Azure Static Web Apps. A Blazor WASM app is a set of static files served to the browser — it does not need a server-side runtime. SWA provides integrated Functions API, automatic deployments, preview environments, and free SSL with zero server management.

### Azure SQL Database over Cosmos DB / PostgreSQL

| Factor | Azure SQL | Cosmos DB | PostgreSQL (Flexible Server) |
|--------|-----------|-----------|------------------------------|
| **Data model** | Relational (tables, joins, constraints) | Document/multi-model | Relational |
| **EF Core support** | First-class | Limited | Good |
| **Tooling** | SSMS, Azure Data Studio, mature ecosystem | Portal explorer, SDK | pgAdmin, Azure Data Studio |
| **Cost (dev)** | $5/month (Basic) | $25/month minimum | $13/month minimum |
| **Offline sync** | N/A (server-side) | Change feed (not needed for our pattern) | N/A |
| **Familiarity** | High (.NET ecosystem standard) | Moderate | High |

**Our choice:** Azure SQL Database. Relational data is a natural fit for structured case worker records (entities, users, audit logs with foreign keys and constraints). EF Core provides excellent SQL Server support, and the team's existing expertise reduces risk.

### Operation-Based Sync over Microsoft Datasync / Realm / Firebase

| Factor | Custom Operation Sync | Microsoft Datasync | Realm Sync | Firebase |
|--------|----------------------|-----------------------|------------|----------|
| **Control** | Full control over sync logic | Framework-managed | SDK-managed | SDK-managed |
| **Flexibility** | Any data model or conflict strategy | Opinionated models | Realm objects only | Firestore documents |
| **Blob handling** | Custom (Blob Storage + SAS) | Separate implementation | Separate implementation | Firebase Storage |
| **Lock-in** | Low (standard HTTP + SQL) | Moderate (Azure Mobile Apps) | High (Realm-specific) | High (Google-specific) |
| **Blazor support** | Native | Limited | No official Blazor SDK | No official Blazor SDK |

**Our choice:** Custom operation-based sync. The application has specific offline requirements (large blob uploads, operation-level retry, field workers on multi-day offline periods) that benefit from full control over the sync algorithm. Third-party sync solutions either lack Blazor WASM support or impose constraints incompatible with our use case.

---

## Project Structure

```
BlazorWASM_PWA/
├── BlazorWASM_PWA.sln
├── .github/workflows/deploy.yml     # CI/CD pipeline
├── docs/                             # Documentation
├── infra/
│   ├── modules/                      # Bicep module definitions
│   │   └── main.bicep                # Main template (SQL, Storage, SWA, KV, AI)
│   └── scripts/
│       └── deploy.ps1                # Deployment script
└── src/
    ├── BlazorWASM_PWA.Client/        # Blazor WebAssembly PWA
    │   ├── Pages/                    # Razor page components
    │   ├── Layout/                   # App shell (MainLayout, NavMenu)
    │   ├── Services/                 # Client services
    │   │   ├── SyncService.cs        # Orchestrates push/pull sync
    │   │   ├── IndexedDbService.cs   # Dexie.js interop wrapper
    │   │   └── ConnectivityService.cs# Network status monitoring
    │   ├── Models/                   # Client-side view models
    │   └── wwwroot/
    │       ├── index.html            # Entry point
    │       ├── manifest.webmanifest  # PWA manifest
    │       ├── service-worker.js     # Dev service worker
    │       ├── service-worker.published.js  # Production service worker
    │       ├── js/
    │       │   └── dexie-interop.js  # Dexie.js database setup and interop
    │       └── css/app.css           # Application styles
    │
    ├── BlazorWASM_PWA.Api/           # Azure Functions API
    │   ├── Functions/
    │   │   ├── SyncFunction.cs       # POST /api/sync — process operations
    │   │   ├── EntityFunctions.cs    # CRUD endpoints for entities
    │   │   ├── BlobFunctions.cs      # SAS token generation, blob management
    │   │   └── HealthFunction.cs     # GET /api/health — health check
    │   ├── Data/
    │   │   ├── AppDbContext.cs        # EF Core DbContext
    │   │   └── Migrations/           # EF Core migrations
    │   ├── Models/                   # API-specific models
    │   ├── host.json                 # Functions host config
    │   └── local.settings.json       # Local dev settings
    │
    └── BlazorWASM_PWA.Shared/        # Shared library
        └── Models/
            ├── EntityDto.cs           # Entity data transfer object
            ├── OperationType.cs       # Sync operation type enum
            ├── SyncPayload.cs         # Sync request model
            └── SyncResult.cs          # Sync response model
```

---

## Key Design Constraints

1. **Browser storage limits** — IndexedDB quotas vary by browser and device. iOS Safari is the most restrictive (~1 GB). The app must monitor storage usage and warn users when space is low.

2. **Service worker lifecycle** — Service workers update asynchronously. The app must handle the transition between old and new service workers gracefully to prevent serving stale assets.

3. **Background sync limitations** — The Background Sync API has limited browser support and reliability. The app uses foreground sync (while the app is open) with periodic checks rather than relying on the Background Sync API.

4. **JWT token expiration** — Tokens cannot be refreshed offline. The app caches tokens in localStorage and queues API operations when tokens are expired and the device is offline.

5. **Blob upload size** — Azure Static Web Apps has a 100 MB request limit. Large videos must be uploaded directly to Blob Storage via SAS tokens, not through the Functions API.

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Local development setup
- [Deployment Guide](DEPLOYMENT.md) — Deploy to Azure
- [Operations Guide](OPERATIONS.md) — Monitoring and troubleshooting
- [User Guide](USER-GUIDE.md) — End-user documentation
