# Getting Started

This guide walks you through setting up the BlazorPWA field service application for local development and running it for the first time.

---

## Prerequisites

Ensure the following tools are installed before you begin:

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0+ | Build and run the API, Client, and Worker projects |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | 2.50+ | Azure resource management and deployment |
| [Node.js](https://nodejs.org/) | 18+ | DexieJS tooling and front-end build utilities |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) | Latest | IDE — VS Code users should install the C# Dev Kit extension |
| [Azure Subscription](https://azure.microsoft.com/free/) | — | Required for cloud deployment (not needed for local dev) |
| [Git](https://git-scm.com/) | 2.40+ | Source control |
| [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) | Latest | Local Azure Blob Storage emulator (optional but recommended) |
| [SQL Server LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) | 2022 | Local database (ships with Visual Studio) |

### Verify installations

```powershell
dotnet --version          # Should print 8.x.x
az --version              # Should print 2.50+
node --version            # Should print v18+
git --version             # Should print 2.40+
```

---

## Clone and Setup

### 1. Clone the repository

```powershell
git clone https://github.com/your-org/BlazorWASM_PWA_Scaled.git
cd BlazorWASM_PWA_Scaled
```

### 2. Open the solution

**Visual Studio 2022:**
Open `BlazorPWA.sln` from the root of the repository.

**VS Code:**
```powershell
code .
```
Install the recommended extensions when prompted (C# Dev Kit, Azure Tools, Bicep).

### 3. Restore packages

```powershell
dotnet restore
```

This restores NuGet packages for all projects in the solution, including EF Core, Azure SDK libraries, and the Blazor WASM toolchain.

If the client project uses npm-based DexieJS tooling:

```powershell
cd src\BlazorPWA.Client
npm install
cd ..\..
```

---

## Local Development Configuration

The application uses the .NET user-secrets mechanism for local development so that connection strings and keys are never committed to source control.

### Option A: User Secrets (Recommended)

Initialize user secrets for each project that needs them:

```powershell
# API project
cd src\BlazorPWA.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=BlazorPWA;Trusted_Connection=True;MultipleActiveResultSets=true"
dotnet user-secrets set "ServiceBus:ConnectionString" "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=LOCAL_DEV_KEY"
dotnet user-secrets set "BlobStorage:ConnectionString" "UseDevelopmentStorage=true"
cd ..\..

# Worker project
cd src\BlazorPWA.Worker
dotnet user-secrets init
dotnet user-secrets set "ServiceBus:ConnectionString" "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=LOCAL_DEV_KEY"
dotnet user-secrets set "BlobStorage:ConnectionString" "UseDevelopmentStorage=true"
cd ..\..
```

### Option B: Azurite for Blob Storage Emulation

[Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) provides a local Azure Blob Storage emulator. Install and start it:

```powershell
# Install globally via npm
npm install -g azurite

# Start Azurite (blob service on port 10000)
azurite --silent --location .azurite --debug .azurite\debug.log
```

When Azurite is running, the connection string `UseDevelopmentStorage=true` routes blob operations to `http://127.0.0.1:10000`.

### Option C: appsettings.Development.json

For shared local-dev settings that are safe to commit, edit `src\BlazorPWA.API\appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:5001",
      "http://localhost:5000"
    ]
  },
  "Sync": {
    "BatchSize": 50,
    "MaxRetries": 3,
    "RetryDelaySeconds": 5
  }
}
```

> **Note:** Never put real connection strings or secrets in `appsettings.*.json` files. Use user-secrets or environment variables instead.

---

## Running Locally

You need to start three services: the **API**, the **Client** (Blazor WASM), and the **Worker** (background media processor).

### Start each service individually

Open three terminal windows and run:

**Terminal 1 — API:**
```powershell
dotnet run --project src\BlazorPWA.API
```
The API listens on `https://localhost:5002` by default.

**Terminal 2 — Client:**
```powershell
dotnet run --project src\BlazorPWA.Client
```
The Blazor WASM PWA is served on `https://localhost:5001`.

**Terminal 3 — Worker:**
```powershell
dotnet run --project src\BlazorPWA.Worker
```
The Worker runs as a background service and processes media from the Service Bus queue.

### Or use the helper script

A convenience script starts all three services together:

```powershell
.\scripts\run-local.ps1
```

This script:
- Starts Azurite (if not already running)
- Applies pending EF Core migrations
- Launches the API, Client, and Worker in parallel
- Opens the browser to `https://localhost:5001`

### Database migration

Before your first run, apply EF Core migrations to create the local database:

```powershell
dotnet ef database update --project src\BlazorPWA.API
```

If you don't have the EF Core CLI tools installed:

```powershell
dotnet tool install --global dotnet-ef
```

---

## First Run Walkthrough

### 1. Access the PWA

Open **https://localhost:5001** in Chrome or Edge. You may see a certificate warning for the development certificate — accept it to proceed.

> **Tip:** If the dev certificate is not trusted, run `dotnet dev-certs https --trust`.

### 2. Install the PWA (optional for local dev)

The browser may prompt you to install the app. Click **Install** to test the PWA experience, or continue using it in the browser tab.

### 3. Create your first inspection report

1. Click **New Report** on the dashboard.
2. Fill in the required fields: facility name, address, inspection date.
3. Add notes in the text area.
4. Click **Save** — the report is saved to IndexedDB locally.

### 4. Add media

1. Open the report you just created.
2. Click **Add Photo** and use your device camera or select a file.
3. Attach a document (PDF or Word) using the **Add Document** button.
4. Media is stored locally in IndexedDB until synced.

### 5. Test offline mode

1. Open the browser DevTools (F12) → **Network** tab.
2. Check **Offline** to simulate no connectivity.
3. Create another report and add notes — everything is saved locally.
4. Notice the **Offline** indicator in the app header and "Pending Sync" badges.

### 6. Reconnect and verify sync

1. Uncheck **Offline** in DevTools.
2. The sync engine detects connectivity and begins uploading.
3. Watch the sync status change from ⟳ Pending → ✓ Synced.
4. Verify the data appears in the API: `https://localhost:5002/api/reports`.

---

## Project Structure Overview

```
BlazorWASM_PWA_Scaled/
├── BlazorPWA.sln                    # Solution file
├── src/
│   ├── BlazorPWA.Client/            # Blazor WASM PWA (front-end)
│   │   ├── Pages/                   # Razor pages (Dashboard, Reports, etc.)
│   │   ├── Components/              # Shared Razor components
│   │   ├── Services/                # Client-side services (Sync, IndexedDB, Media)
│   │   ├── wwwroot/
│   │   │   ├── js/                  # DexieJS wrapper and interop scripts
│   │   │   ├── service-worker.js    # PWA service worker
│   │   │   └── manifest.json        # PWA manifest
│   │   └── Program.cs               # Client entry point and DI setup
│   │
│   ├── BlazorPWA.API/               # ASP.NET Core Minimal API (back-end)
│   │   ├── Endpoints/               # Minimal API endpoint definitions
│   │   ├── Data/                    # EF Core DbContext and migrations
│   │   ├── Models/                  # Domain models and DTOs
│   │   ├── Services/                # Server-side business logic
│   │   └── Program.cs               # API entry point, middleware, DI
│   │
│   ├── BlazorPWA.Worker/            # Background Worker Service
│   │   ├── Processors/              # Media processing logic
│   │   └── Program.cs               # Worker entry point
│   │
│   └── BlazorPWA.Shared/            # Shared models and contracts
│       ├── Models/                   # Shared DTOs between Client and API
│       ├── Enums/                    # Operation types, sync statuses
│       └── Constants/                # Shared constants
│
├── tests/
│   ├── BlazorPWA.API.Tests/         # API unit and integration tests
│   ├── BlazorPWA.Client.Tests/      # Client component tests (bUnit)
│   └── BlazorPWA.Worker.Tests/      # Worker unit tests
│
├── infra/                            # Azure Bicep IaC templates
│   ├── main.bicep                   # Root Bicep template
│   ├── modules/                     # Bicep modules (App Service, SQL, etc.)
│   └── parameters/                  # Environment-specific parameter files
│
├── .github/
│   └── workflows/                   # GitHub Actions CI/CD pipelines
│       ├── deploy-infra.yml
│       ├── deploy-app.yml
│       └── pr-validation.yml
│
├── scripts/                          # Developer helper scripts
│   ├── run-local.ps1
│   └── seed-data.ps1
│
└── docs/                             # Documentation (you are here)
    ├── GETTING-STARTED.md
    ├── ARCHITECTURE.md
    ├── DEPLOYMENT.md
    ├── OPERATIONS.md
    └── USER-GUIDE.md
```

### Project roles

| Project | Role |
|---------|------|
| **BlazorPWA.Client** | The front-end Blazor WASM Progressive Web App. Runs entirely in the browser, stores data in IndexedDB via DexieJS, handles offline capture, and syncs with the API. |
| **BlazorPWA.API** | The ASP.NET Core Minimal API back-end. Handles CRUD operations, sync endpoints, media upload, conflict resolution, and communicates with Azure SQL and Service Bus. |
| **BlazorPWA.Worker** | A background worker service that listens to the Azure Service Bus `media-processing` queue and uploads media files to Azure Blob Storage. |
| **BlazorPWA.Shared** | Shared library containing models, DTOs, enums, and constants used by both Client and API projects. |

---

## Next Steps

- Read the [Architecture Guide](ARCHITECTURE.md) to understand system design decisions
- Read the [Deployment Guide](DEPLOYMENT.md) when you're ready to deploy to Azure
- Read the [Operations Guide](OPERATIONS.md) for monitoring and troubleshooting
- Read the [User Guide](USER-GUIDE.md) for end-user documentation
