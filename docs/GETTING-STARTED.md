# Getting Started

Local development setup guide for the BlazorWASM_PWA offline-first case worker application.

---

## Prerequisites

Install the following tools before proceeding:

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | Build and run Blazor WASM client, Azure Functions API, and shared library |
| [Node.js](https://nodejs.org/) | 20 LTS+ | Required by SWA CLI and Dexie.js tooling |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | Latest | Manage Azure resources and authenticate locally |
| [SWA CLI](https://azure.github.io/static-web-apps-cli/) | Latest | Local development proxy for Static Web Apps |
| [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) | v4+ | Run Azure Functions locally |
| [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) | Latest | Local Azure Blob Storage emulator |
| [SQL Server](https://www.microsoft.com/sql-server) | LocalDB or Docker | Local database for EF Core |
| [Git](https://git-scm.com/) | Latest | Source control |

### Install SWA CLI and Azurite

```bash
npm install -g @azure/static-web-apps-cli
npm install -g azurite
```

### Install Azure Functions Core Tools

```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

### SQL Server Options

**Option A — SQL Server LocalDB (Windows):**

LocalDB is included with Visual Studio or can be installed standalone via the [SQL Server Express installer](https://www.microsoft.com/sql-server/sql-server-downloads). Verify installation:

```powershell
sqllocaldb info
sqllocaldb start MSSQLLocalDB
```

**Option B — SQL Server in Docker (cross-platform):**

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStr0ngP@ssword" \
  -p 1433:1433 --name blazorpwa-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

---

## Clone and Restore

```bash
# Clone the repository
git clone https://github.com/<your-org>/BlazorWASM_PWA.git
cd BlazorWASM_PWA

# Restore .NET dependencies for all projects
dotnet restore
```

Verify the solution builds:

```bash
dotnet build BlazorWASM_PWA.sln
```

---

## Database Setup

### 1. Configure the Connection String

Create or update the local settings file for the API project:

**`src/BlazorWASM_PWA.Api/local.settings.json`**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=BlazorPWA;Trusted_Connection=true;TrustServerCertificate=true;",
    "BlobStorageConnectionString": "UseDevelopmentStorage=true",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": ""
  },
  "Host": {
    "CORS": "http://localhost:5000,http://localhost:5173",
    "CORSCredentials": true
  }
}
```

> **Docker SQL Server connection string:**
> `Server=localhost,1433;Database=BlazorPWA;User Id=sa;Password=YourStr0ngP@ssword;TrustServerCertificate=true;`

### 2. Run EF Core Migrations

```bash
# Install the EF Core tools if not already installed
dotnet tool install --global dotnet-ef

# From the API project directory, create and apply migrations
cd src/BlazorWASM_PWA.Api
dotnet ef database update
```

If no migrations exist yet, create the initial migration first:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Azurite Setup (Local Blob Storage)

Start Azurite to emulate Azure Blob Storage locally:

```bash
# Start all Azurite services (blob, queue, table)
azurite --silent --location .azurite --debug .azurite/debug.log
```

Or start only blob storage:

```bash
azurite-blob --silent --location .azurite
```

Azurite listens on `http://127.0.0.1:10000` (blob), `http://127.0.0.1:10001` (queue), and `http://127.0.0.1:10002` (table) by default.

The connection string `UseDevelopmentStorage=true` in `local.settings.json` automatically targets Azurite.

> **Tip:** If you use Visual Studio Code, the [Azurite extension](https://marketplace.visualstudio.com/items?itemName=Azurite.azurite) lets you start/stop Azurite from the command palette.

---

## Running the Application Locally

The SWA CLI orchestrates both the Blazor WASM frontend and the Azure Functions API behind a single local proxy, mimicking the Azure Static Web Apps production environment.

### 1. Start Azurite (in a separate terminal)

```bash
azurite --silent --location .azurite
```

### 2. Start the Application with SWA CLI

From the repository root:

```bash
swa start http://localhost:5000 \
  --run "dotnet run --project src/BlazorWASM_PWA.Client" \
  --api-location src/BlazorWASM_PWA.Api
```

This command:
- Starts the Blazor WASM client on `http://localhost:5000`
- Starts the Azure Functions API (auto-detected by SWA CLI)
- Creates a proxy at `http://localhost:4280` that routes `/api/*` requests to the Functions backend and everything else to the Blazor client

### Alternative: Start Components Individually

If you prefer to run each component separately for debugging:

**Terminal 1 — Blazor Client:**

```bash
cd src/BlazorWASM_PWA.Client
dotnet run
```

**Terminal 2 — Azure Functions API:**

```bash
cd src/BlazorWASM_PWA.Api
func start
```

**Terminal 3 — SWA CLI Proxy:**

```bash
swa start http://localhost:5000 --api-location http://localhost:7071
```

> **Note:** When running components individually, ensure the ports match your `launchSettings.json` configuration.

---

## First Run Walkthrough

1. **Open the application** — Navigate to `http://localhost:4280` in your browser (Edge or Chrome recommended for PWA support).

2. **Check the console** — Open browser DevTools (F12) and verify:
   - No errors in the Console tab
   - The service worker registers successfully (look for `Service worker registered` message)
   - IndexedDB databases appear in Application → Storage → IndexedDB

3. **Test offline capability:**
   - Navigate through the app while online to cache assets
   - Open DevTools → Application → Service Workers
   - Check "Offline" to simulate no connectivity
   - Refresh the page — the app should still load from the service worker cache

4. **Test the API connection:**
   - Verify that API calls to `/api/*` are proxied correctly through SWA CLI
   - Check the Functions terminal for incoming request logs

5. **Install as PWA (optional):**
   - In Chrome/Edge, click the install icon in the address bar
   - The app installs as a standalone window

---

## Project Structure

```
BlazorWASM_PWA/
├── BlazorWASM_PWA.sln              # Solution file
├── .github/workflows/deploy.yml    # CI/CD pipeline
├── docs/                           # Documentation
├── infra/                          # Bicep IaC modules and deploy scripts
│   ├── modules/                    # Bicep module definitions
│   └── scripts/                    # Deployment scripts (deploy.ps1)
└── src/
    ├── BlazorWASM_PWA.Client/      # Blazor WebAssembly PWA frontend
    │   ├── Pages/                  # Razor page components
    │   ├── Layout/                 # App layout (MainLayout, NavMenu)
    │   ├── Services/               # Client-side services (sync, IndexedDB)
    │   ├── Models/                 # Client-side models
    │   └── wwwroot/                # Static assets, service worker, manifest
    ├── BlazorWASM_PWA.Api/         # Azure Functions API backend
    │   ├── Functions/              # HTTP-triggered function endpoints
    │   ├── Data/                   # EF Core DbContext and configurations
    │   └── Models/                 # API-specific models
    └── BlazorWASM_PWA.Shared/      # Shared DTOs and enums
        └── Models/                 # EntityDto, SyncPayload, SyncResult, etc.
```

---

## Common Issues

| Issue | Solution |
|-------|----------|
| `swa` command not found | Run `npm install -g @azure/static-web-apps-cli` |
| Port 4280 already in use | Kill the existing process or use `swa start --port 4281` |
| EF Core migrations fail | Ensure SQL Server is running and the connection string is correct |
| Azurite connection refused | Start Azurite before launching the API |
| Service worker not updating | Hard refresh (`Ctrl+Shift+R`) or clear browser cache in DevTools → Application |
| CORS errors in browser | Ensure SWA CLI proxy is being used (`localhost:4280`), not direct client URL |

---

## Next Steps

- [Deployment Guide](DEPLOYMENT.md) — Deploy to Azure
- [Architecture](ARCHITECTURE.md) — Understand the offline-first design
- [Operations Guide](OPERATIONS.md) — Monitoring and troubleshooting
- [User Guide](USER-GUIDE.md) — End-user documentation
