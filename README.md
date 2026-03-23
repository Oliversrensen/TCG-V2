# TCG-V2

Authoritative server Trading Card Game backend with Unity client. Auth via Neon Auth, data in Neon PostgreSQL, real-time gameplay via SignalR.

## Architecture

- **Backend**: ASP.NET Core 8 (REST + SignalR), C#
- **Auth**: Neon Auth (JWT, users in `neon_auth` schema)
- **Database**: Neon PostgreSQL
- **Cache**: Redis (matchmaking, match state)
- **Client**: Unity (C#)

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for details.

## Quick Start

### Prerequisites

- .NET 8 SDK
- Neon project with Auth and PostgreSQL enabled
- Redis (optional for local dev)

### Backend

```bash
cd backend/TCG.Server
cp appsettings.Development.example.json appsettings.Development.json
# Edit appsettings.Development.json with NEON_DATABASE_URL and NEON_AUTH_URL (required)
dotnet run
```

### Unity Client

Open `unity-client/` in Unity 2022 LTS or newer. Configure API base URL and Neon Auth URL in project settings.

### Tests

```bash
dotnet test backend/TCG.Tests/TCG.Tests.csproj
```

## Project Structure

```
backend/
  TCG.Server/     # API, SignalR, JWT
  TCG.Core/       # Models, interfaces
  TCG.GameLogic/  # Game engine
  TCG.Economy/    # (Later) Packs, economy
  TCG.Tests/      # xUnit tests (DeckService, GameEngine)
unity-client/     # Unity project
docs/             # Architecture, API, data models
```

## Documentation

- [AGENTS.md](AGENTS.md) – AI agent orientation
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) – System design
- [docs/API.md](docs/API.md) – REST and SignalR contracts
- [docs/DATA_MODELS.md](docs/DATA_MODELS.md) – Entities and schema
