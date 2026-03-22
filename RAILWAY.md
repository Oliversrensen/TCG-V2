# Railway Deployment

## Important: Root Directory

**Leave Root Directory empty** in Railway service settings. The build must run from the repo root so `TCG.Core`, `TCG.GameLogic`, and `TCG.Economy` are available.

## Build / Start

Configured via `nixpacks.toml`:
- **Build**: `dotnet publish backend/TCG.Server/TCG.Server.csproj -c Release -o out`
- **Start**: `dotnet out/TCG.Server.dll`

## Environment Variables

- `NEON_DATABASE_URL` – Neon PostgreSQL connection string
- `NEON_AUTH_URL` – Neon Auth URL
- `ASPNETCORE_ENVIRONMENT` – `Production`
- `REDIS_CONNECTION` – (optional) Redis URL
