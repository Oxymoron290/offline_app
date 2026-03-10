# Blazor Hybrid Case Worker App

An **offline-first** .NET MAUI Blazor Hybrid application for case workers who operate in environments with limited or intermittent connectivity. Data (form fields, entity records, documents, photos, and videos) is stored locally and synced to Azure when connectivity is restored.

## Architecture

```
┌──────────────────────┐         ┌──────────────────────────┐
│   .NET MAUI App      │  HTTPS  │     Azure Cloud           │
│   (Blazor Hybrid)    │────────▶│                            │
│                      │         │  Azure Functions (API)     │
│  SQLite + Filesystem │         │  Cosmos DB (NoSQL)         │
│  Background Sync     │         │  Blob Storage (Media)      │
│  MSAL.NET Auth       │         │  Key Vault + App Insights  │
└──────────────────────┘         └──────────────────────────┘
```

## Key Features

- **True offline-first** — All data saved locally with SQLite; works without internet
- **Store-and-forward sync** — Background worker syncs every 30 seconds when online
- **Media capture** — Camera photos, videos, and document attachments
- **Cross-platform** — Windows, Android, iOS, macOS
- **Enterprise auth** — Microsoft Entra ID (MSAL.NET)
- **Serverless backend** — Azure Functions + Cosmos DB + Blob Storage
- **Infrastructure-as-Code** — Full Bicep templates for Azure deployment

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Client | .NET MAUI Blazor Hybrid (.NET 10) |
| Local DB | SQLite (sqlite-net-pcl) |
| Auth | MSAL.NET / Microsoft Entra ID |
| API | Azure Functions (isolated worker) |
| Cloud DB | Azure Cosmos DB (Serverless) |
| Media | Azure Blob Storage |
| IaC | Bicep |
| CI/CD | GitHub Actions |

## Quick Start

```bash
# Clone
git clone https://github.com/YOUR_ORG/BlazorHybrid.git
cd BlazorHybrid

# Restore
dotnet restore BlazorHybrid.sln

# Run API locally
cd src/BlazorHybrid.Api
func start

# Run MAUI app (Windows)
dotnet build src/BlazorHybrid.App -t:Run -f net10.0-windows10.0.19041.0
```

See [Getting Started](docs/getting-started.md) for full setup instructions.

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](docs/getting-started.md) | Dev environment setup, running locally |
| [Architecture](docs/architecture.md) | System design, data flow, sync strategy |
| [Deployment](docs/deployment.md) | Azure deployment with Bicep, Entra ID setup |
| [Operations](docs/operations.md) | Monitoring, troubleshooting, scaling |
| [User Guide](docs/user-guide.md) | End-user documentation for case workers |

## Project Structure

```
BlazorHybrid/
├── src/
│   ├── BlazorHybrid.App/      # .NET MAUI Blazor Hybrid client app
│   ├── BlazorHybrid.Shared/   # Shared DTOs and enums
│   └── BlazorHybrid.Api/      # Azure Functions REST API
├── tests/                      # Unit and integration tests
├── infra/                      # Bicep infrastructure templates
├── docs/                       # Documentation
└── .github/workflows/          # CI/CD pipelines
```

## Deploy to Azure

```powershell
# Deploy all infrastructure
cd infra
.\scripts\deploy.ps1 -ResourceGroupName "rg-blazorhybrid-dev" -Location "eastus2" -EnvironmentName "dev"

# Deploy the API
cd ../src/BlazorHybrid.Api
func azure functionapp publish func-blazorhybrid-dev
```

See [Deployment Guide](docs/deployment.md) for full instructions including Entra ID configuration.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.
