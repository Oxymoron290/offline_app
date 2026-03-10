# BlazorPWA — Offline-First Field Service Application

[![Deploy Infrastructure](https://img.shields.io/badge/Deploy-Infrastructure-blue)](#deployment)
[![Deploy Application](https://img.shields.io/badge/Deploy-Application-green)](#deployment)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Azure](https://img.shields.io/badge/Azure-Bicep-0078D4)](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)

A **store-and-forward** Blazor WASM PWA for case workers operating in facilities with limited or intermittent connectivity. Data (form fields, entity records, documents, photos, and videos) is captured offline and automatically synced when connectivity is restored.

## Architecture

```
Field Device (Browser)                    Azure Cloud
┌──────────────────────┐     HTTPS     ┌──────────────────────────┐
│ Blazor WASM PWA      │──────────────►│ ASP.NET Core Minimal API │
│ ├── IndexedDB        │               │ ├── Entity Endpoints     │
│ │   ├── entities     │               │ ├── Sync Endpoints       │
│ │   ├── syncQueue    │               │ └── Media Endpoints      │
│ │   └── mediaBlobs   │               │         │          │     │
│ └── Sync Worker      │               │         ▼          ▼     │
└──────────────────────┘               │   Azure SQL   Service Bus│
                                       │                    │     │
                                       │              ┌─────▼───┐ │
                                       │              │ Worker  │ │
                                       │              │    │    │ │
                                       │              │    ▼    │ │
                                       │              │  Blob   │ │
                                       │              │ Storage │ │
                                       │              └─────────┘ │
                                       └──────────────────────────┘
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WASM PWA, DexieJS (IndexedDB), Bootstrap 5 |
| Backend API | ASP.NET Core 8 Minimal API, EF Core |
| Database | Azure SQL Database |
| Media Pipeline | Azure Service Bus → Background Worker → Azure Blob Storage |
| Infrastructure | Azure Bicep (App Services, SQL, Service Bus, Storage, Key Vault, App Insights) |
| CI/CD | GitHub Actions |

## Key Features

- **Offline-first**: Full functionality without connectivity — create reports, capture photos/videos, add notes
- **Operation-based sync**: Queues discrete operations (`CREATE_REPORT`, `ADD_PHOTO`, `ADD_NOTE`) to avoid conflicts
- **Conflict resolution**: Server wins for immutable data, client wins for new records, manual resolution for edits
- **Decoupled media uploads**: Large files (photos, videos) processed asynchronously via Azure Service Bus
- **PWA installable**: Install on any device via Chrome/Edge — works like a native app

## Quick Start

```powershell
# Clone and setup
git clone <repo-url>
cd BlazorWASM_PWA_Scaled
dotnet restore BlazorPWA.sln

# Local setup (configures user secrets, installs tools)
.\scripts\setup-local.ps1

# Run all services
.\scripts\run-local.ps1
```

Access the PWA at `https://localhost:5001`

## Project Structure

```
├── src/
│   ├── BlazorPWA.Client/      # Blazor WASM PWA frontend
│   ├── BlazorPWA.API/         # ASP.NET Core Minimal API
│   ├── BlazorPWA.Worker/      # Background media processor
│   └── BlazorPWA.Shared/      # Shared models, enums, constants
├── infra/                     # Azure Bicep templates
│   ├── main.bicep             # Main orchestration
│   ├── modules/               # Individual resource modules
│   └── parameters/            # Environment configs (dev/staging/prod)
├── .github/workflows/         # CI/CD pipelines
├── scripts/                   # Local dev helper scripts
└── docs/                      # Comprehensive documentation
```

## Deployment

```powershell
# Deploy Azure infrastructure
.\infra\deploy.ps1 -Environment dev -ResourceGroupName rg-blazorpwa-dev -Location eastus

# Or use GitHub Actions (recommended)
# Configure secrets: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, SQL_ADMIN_PASSWORD
```

## Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](docs/GETTING-STARTED.md) | Prerequisites, local setup, first run |
| [Architecture](docs/ARCHITECTURE.md) | System design, data flows, sync strategy |
| [Deployment](docs/DEPLOYMENT.md) | Azure deployment, Bicep, CI/CD setup |
| [Operations](docs/OPERATIONS.md) | Monitoring, alerting, scaling, troubleshooting |
| [User Guide](docs/USER-GUIDE.md) | End-user guide for field workers |

## License

Private — All rights reserved.