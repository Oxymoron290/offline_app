# Case Worker PWA — Offline-First Data Collection

A **Blazor WebAssembly Progressive Web App** (PWA) enabling case workers to collect data (form fields, entity records, documents, photos, videos) in facilities with limited or no internet connectivity. Data is stored locally in IndexedDB and automatically synced to Azure when connectivity returns.

## Architecture

```
┌─────────────────────────────────────────────────┐
│  Blazor WASM PWA (browser / installable)        │
│  ┌──────────┐  ┌──────────┐  ┌───────────────┐ │
│  │ UI Pages │  │ Services │  │ Service Worker│ │
│  └────┬─────┘  └────┬─────┘  └───────┬───────┘ │
│       │              │                │          │
│       ▼              ▼                ▼          │
│  ┌──────────────────────────────────────────┐   │
│  │  IndexedDB (Dexie.js via JS interop)     │   │
│  │  • Entities  • Photos  • Videos          │   │
│  │  • Documents • OperationsQueue           │   │
│  └──────────────────┬───────────────────────┘   │
└─────────────────────┼───────────────────────────┘
                      │  (when online)
                      ▼
┌─────────────────────────────────────────────────┐
│  Azure Functions API (via Static Web Apps)       │
│  ┌─────────┐  ┌──────────┐  ┌───────────────┐  │
│  │ Sync API│  │ CRUD API │  │ Blob Upload   │  │
│  └────┬────┘  └────┬─────┘  └───────┬───────┘  │
│       │             │                │           │
│       ▼             ▼                ▼           │
│  ┌──────────┐  ┌──────────────────────────────┐ │
│  │ Azure SQL│  │ Azure Blob Storage           │ │
│  └──────────┘  └──────────────────────────────┘ │
└─────────────────────────────────────────────────┘
         Microsoft Entra ID (authentication)
```

## Tech Stack

| Component | Technology |
|---|---|
| Frontend | .NET 10 Blazor WebAssembly PWA |
| Offline Storage | IndexedDB via Dexie.js (JS interop) |
| Hosting | Azure Static Web Apps |
| API | Azure Functions (isolated worker, .NET 10) |
| Database | Azure SQL Database |
| File Storage | Azure Blob Storage |
| Authentication | Microsoft Entra ID (MSAL) |
| Infrastructure | Bicep |
| CI/CD | GitHub Actions |

## Key Features

- **Offline-first**: All data saved locally in IndexedDB; works without internet
- **Store-and-forward sync**: Operations queued locally, pushed to server when online
- **PWA installable**: Install on phones, tablets, and desktops — no app store needed
- **Photo & document capture**: Camera integration and file upload with local caching
- **Automatic retry**: Failed sync operations retry with exponential backoff (max 5 attempts)
- **Pull sync**: Fetches server updates when reconnecting (`GET /api/sync?since=timestamp`)
- **Enterprise auth**: Microsoft Entra ID with MSAL for secure case worker access

## Project Structure

```
BlazorWASM_PWA/
├── src/
│   ├── BlazorWASM_PWA.Client/      # Blazor WASM PWA frontend
│   ├── BlazorWASM_PWA.Api/         # Azure Functions API backend
│   └── BlazorWASM_PWA.Shared/      # Shared models (DTOs, enums)
├── infra/                           # Bicep infrastructure-as-code
│   ├── main.bicep                   # Main orchestrator
│   ├── modules/                     # Individual Azure resource modules
│   └── scripts/deploy.ps1           # Deployment helper script
├── docs/                            # Documentation
│   ├── GETTING-STARTED.md           # Local development setup
│   ├── DEPLOYMENT.md                # Azure deployment guide
│   ├── OPERATIONS.md                # Monitoring & operations
│   ├── USER-GUIDE.md                # End-user guide for case workers
│   └── ARCHITECTURE.md              # Technical architecture
└── .github/workflows/deploy.yml     # CI/CD pipeline
```

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, Azure Functions Core Tools, SWA CLI, Azurite

# Clone and restore
git clone <repo-url>
cd BlazorWASM_PWA
dotnet restore

# Start Azurite (local storage emulator) in a separate terminal
azurite --silent

# Run the API locally
cd src/BlazorWASM_PWA.Api
func start

# Run the client (in another terminal)
cd src/BlazorWASM_PWA.Client
dotnet run

# Or use SWA CLI to run both together
swa start https://localhost:5000 --api-location src/BlazorWASM_PWA.Api
```

See [docs/GETTING-STARTED.md](docs/GETTING-STARTED.md) for full setup instructions.

## Documentation

- **[Getting Started](docs/GETTING-STARTED.md)** — Prerequisites, local dev setup, first run
- **[Deployment](docs/DEPLOYMENT.md)** — Deploy to Azure with Bicep and GitHub Actions
- **[Operations](docs/OPERATIONS.md)** — Monitoring, scaling, troubleshooting
- **[User Guide](docs/USER-GUIDE.md)** — End-user guide for case workers
- **[Architecture](docs/ARCHITECTURE.md)** — System design, sync algorithm, security model

## Offline Data Model

Operations are queued instead of saving directly to the API:

```
OfflineOperation
├── id                 # Unique operation ID
├── operationType      # CreateEntity, UpdateEntity, UploadPhoto, etc.
├── entityType         # Property, Inspection, Violation, Contact
├── entityId           # Target entity ID
├── payloadJson        # Serialized operation data
├── blobReferenceIds   # Associated blob IDs
├── status             # Pending, InProgress, Completed, Failed
├── createdAt          # Timestamp
├── retryCount         # Number of retry attempts
└── lastError          # Last error message (if failed)
```

## Sync Algorithm

```
When connection returns:
    foreach operation in pendingQueue (ordered by createdAt):
        try:
            POST /api/sync with batch of operations
            for each success: mark Completed
            for each failure: increment retryCount
                if retryCount > 5: mark Failed
        catch: retry later with exponential backoff
    
    Pull server updates:
        GET /api/sync?since=lastSyncTimestamp
        Merge updated entities into local IndexedDB
```

## License

[Add your license here]
