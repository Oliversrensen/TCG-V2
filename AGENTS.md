# TCG-V2: Agent Context Guide

This document gives AI agents a fast mental model of the project. Use it to orient before making changes.

## What This Project Is

**TCG-V2** is an authoritative server TCG (Trading Card Game) backend with a Unity client. The server owns all game state; clients send intents and render what the server tells them.

- **Auth**: Neon Auth (managed JWT). Users live in `neon_auth` schema.
- **DB**: Neon PostgreSQL (same project as auth).
- **Backend**: ASP.NET Core (REST + SignalR), C#, .NET 8.
- **Client**: Unity (C#).

## Directory Map

```
TCG-V2/
├── AGENTS.md                 # This file
├── README.md
├── docs/                     # Architecture and API documentation
│   ├── ARCHITECTURE.md
│   ├── API.md
│   └── DATA_MODELS.md
├── .cursor/rules/            # Cursor rules for AI guidance
├── backend/
│   ├── TCG.Server/           # ASP.NET Core API, SignalR hub, JWT validation
│   ├── TCG.Core/             # Shared models, DTOs, interfaces
│   ├── TCG.GameLogic/        # Authoritative game engine
│   └── TCG.Economy/          # (Later) Packs, currency, drops
├── shared/                   # Optional shared C# models
└── unity-client/             # Unity project
```

## Key Files and Their Purpose

| Path | Purpose |
|------|---------|
| `backend/TCG.Server/Program.cs` | App entry, DI, middleware |
| `backend/TCG.Server/Hubs/GameHub.cs` | SignalR hub for matchmaking and gameplay |
| `backend/TCG.Server/Controllers/DecksController.cs` | Deck CRUD REST API |
| `backend/TCG.Core/` | Models, DTOs, interfaces |
| `backend/TCG.GameLogic/` | Turn logic, play card, attack, win condition |
| `unity-client/Assets/Scripts/TcgClient/` | Unity REST + SignalR client scripts |

## Data Flow

1. **Auth**: Unity → Neon Auth REST API → JWT. Unity sends JWT in `Authorization: Bearer <token>`.
2. **REST**: Unity → ASP.NET Core → validates JWT via JWKS → business logic → Neon DB.
3. **Real-time**: Unity → SignalR (with JWT) → GameHub → matchmaking / game logic → broadcast to players.

## Configuration

- `NEON_DATABASE_URL` – PostgreSQL connection string (Neon).
- `NEON_AUTH_URL` – Neon Auth base URL for JWKS.
- `REDIS_URL` or `REDIS_CONNECTION` – Redis for matchmaking queue, match state, connection store (optional; uses in-memory when unset).

## Conventions

- **Server is authoritative**: Never trust client for game state.
- **JWT**: Validate on every protected endpoint and SignalR connection.
- **C#**: Use async/await; follow standard .NET naming.

For detailed architecture and API contracts, see `docs/ARCHITECTURE.md` and `docs/API.md`.
