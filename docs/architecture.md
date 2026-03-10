# Architecture

## System Overview

The Blazor Hybrid Case Worker App is an **offline-first** application designed for case workers who enter facilities with limited or no internet connectivity. It uses a store-and-forward pattern to ensure all data (form fields, documents, photos, videos) is captured reliably and synced when connectivity is restored.

## Architecture Diagram

```
┌─────────────────────────────────────────────┐
│           .NET MAUI Blazor Hybrid App       │
│                                             │
│  ┌─────────────┐    ┌──────────────────┐   │
│  │  Blazor UI   │───▶│  Service Layer   │   │
│  │  (Razor)     │    │  (C# Services)   │   │
│  └─────────────┘    └──────┬───────────┘   │
│                            │                │
│  ┌─────────────┐    ┌──────▼───────────┐   │
│  │   Media      │    │   SQLite DB      │   │
│  │   Storage    │    │   (Local)        │   │
│  │   (Files)    │    └──────┬───────────┘   │
│  └─────────────┘           │                │
│                     ┌──────▼───────────┐   │
│                     │  Background Sync  │   │
│                     │  Worker (30s)     │   │
│                     └──────┬───────────┘   │
└────────────────────────────┼────────────────┘
                             │ HTTPS (when online)
                             ▼
┌─────────────────────────────────────────────┐
│              Azure Cloud                     │
│                                             │
│  ┌──────────────────┐                       │
│  │  Azure Functions  │ (REST API)           │
│  │  (.NET 10)        │                      │
│  └──┬──────────┬─────┘                      │
│     │          │                            │
│  ┌──▼────┐  ┌──▼──────────┐                │
│  │Cosmos  │  │Azure Blob   │                │
│  │DB      │  │Storage      │                │
│  │(NoSQL) │  │(Media Files)│                │
│  └────────┘  └─────────────┘                │
│                                             │
│  ┌────────────┐  ┌──────────────┐          │
│  │ Key Vault   │  │ App Insights │          │
│  └────────────┘  └──────────────┘          │
└─────────────────────────────────────────────┘
```

## Data Flow

### Offline Write Path (Local-First)
1. User creates/updates data in the Blazor UI
2. Data is written immediately to **SQLite** (local database)
3. A **SyncOperation** record is enqueued in the sync queue
4. Media files (photos/videos/documents) are saved to the device **filesystem**
5. User gets instant feedback — no network required

### Sync Path (Store-and-Forward)
1. **BackgroundSyncWorker** runs every 30 seconds
2. Checks connectivity via MAUI's `Connectivity` API
3. If online, reads pending operations from the sync queue (FIFO)
4. Uploads media files to Azure Blob Storage via SAS tokens
5. Sends metadata batch to the Azure Functions `/api/sync` endpoint
6. Marks successful operations as completed; retries failures (up to 5 times)

### Read Path (Optional Online)
1. When online, the app can fetch the latest data from the API
2. Server-side data can be pulled down to update the local SQLite database
3. This enables multi-device sync scenarios

## Local Database Schema

```
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  Entities    │   │  Photos      │   │  Videos      │
├──────────────┤   ├──────────────┤   ├──────────────┤
│ Id (PK)      │   │ Id (PK)      │   │ Id (PK)      │
│ CaseWorkerId │   │ EntityId (FK)│   │ EntityId (FK)│
│ Name         │   │ FileName     │   │ FileName     │
│ Description  │   │ LocalFilePath│   │ LocalFilePath│
│ EntityType   │   │ FileSizeBytes│   │ FileSizeBytes│
│ FormFieldsJson│  │ Latitude     │   │ DurationTicks│
│ IsSynced     │   │ Longitude    │   │ Latitude     │
│ CreatedAt    │   │ Caption      │   │ Longitude    │
│ UpdatedAt    │   │ IsSynced     │   │ Caption      │
└──────────────┘   │ CapturedAt   │   │ IsSynced     │
                   └──────────────┘   │ CapturedAt   │
┌──────────────┐                      └──────────────┘
│  Documents   │   ┌──────────────────┐
├──────────────┤   │ SyncOperations   │
│ Id (PK)      │   ├──────────────────┤
│ EntityId (FK)│   │ Id (PK)          │
│ FileName     │   │ OperationType    │
│ ContentType  │   │ EntityType       │
│ LocalFilePath│   │ EntityId         │
│ FileSizeBytes│   │ PayloadJson      │
│ Notes        │   │ FilePath         │
│ IsSynced     │   │ Status (Indexed) │
│ CreatedAt    │   │ RetryCount       │
└──────────────┘   │ LastError        │
                   │ CreatedAt        │
                   └──────────────────┘
```

## Cosmos DB Container Design

| Container | Partition Key | Purpose |
|-----------|--------------|---------|
| `entities` | `/caseWorkerId` | Entity records (forms, case data) |
| `documents` | `/entityId` | Document metadata |
| `media` | `/entityId` | Photo and video metadata |
| `sync-log` | `/deviceId` | Audit trail of sync operations |

## Conflict Resolution Strategy

The system uses **last-write-wins** based on server timestamps:

1. Each entity has a `UpdatedAt` timestamp
2. When syncing, the server compares timestamps
3. The latest write wins (server-side `UpsertItemAsync`)
4. The sync-log container maintains a full audit trail for review
5. Future enhancement: implement custom merge logic for specific entity types

## Authentication & Security

```
┌────────────┐     ┌──────────────┐     ┌────────────┐
│ MAUI App   │────▶│ Microsoft    │────▶│ Azure      │
│ (MSAL.NET) │     │ Entra ID     │     │ Functions  │
│            │◀────│ (OAuth 2.0)  │     │ (JWT)      │
└────────────┘     └──────────────┘     └────────────┘
```

1. **Client**: MSAL.NET acquires tokens via interactive login (system browser)
2. **Token Cache**: Stored securely per platform (Keychain/Keystore/DPAPI)
3. **API Auth**: Azure Functions validate JWT bearer tokens
4. **Blob Access**: Short-lived SAS tokens issued by the API (30-minute expiry)
5. **Secrets**: Key Vault stores connection strings; Function App uses managed identity

## Technology Stack

| Component | Technology | Justification |
|-----------|-----------|---------------|
| Client UI | Blazor Hybrid (Razor) | Code sharing, web dev skills |
| Client Framework | .NET MAUI | Cross-platform native access |
| Local DB | SQLite (sqlite-net-pcl) | Lightweight, reliable, offline |
| Backend API | Azure Functions (isolated) | Serverless, cost-effective |
| Cloud DB | Azure Cosmos DB | Global distribution, flexible schema |
| Media Storage | Azure Blob Storage | Scalable, cost-effective for large files |
| Auth | Microsoft Entra ID | Enterprise-grade identity |
| IaC | Bicep | Native Azure, declarative |
| CI/CD | GitHub Actions | Integrated with repo |
