# TCG-V2 Architecture

## Overview

Authoritative server TCG: the backend owns all game state. Clients send intents; the server validates, applies, and broadcasts state.

## Components

```
Unity Clients
      │
      ├── REST (decks, users) ──► ASP.NET Core API ──► Neon Auth (JWT)
      │                                    │
      │                                    └──► Neon PostgreSQL
      │
      └── SignalR (matchmaking, game) ──► GameHub ──► GameLogic
                                                   │
                                                   ├──► Neon PostgreSQL
                                                   └──► Redis (match state)
```

## Auth Flow

1. Unity calls Neon Auth REST API (sign-in/sign-up).
2. Neon Auth returns JWT.
3. Unity sends `Authorization: Bearer <token>` on all requests.
4. ASP.NET Core validates JWT via JWKS (`<NEON_AUTH_URL>/.well-known/jwks.json`).

## Game Flow

1. Player joins matchmaking queue (SignalR).
2. Server pairs two players, creates match.
3. Clients receive `MatchFound` with match ID.
4. Clients send `PlayCard`, `Attack`, `EndTurn` intents.
5. Server validates, applies, broadcasts `StateUpdate`.
6. Clients render; no client-side game logic.

## Domains

| Domain | Responsibility |
|--------|----------------|
| Auth | Neon Auth; backend only validates JWT |
| Decks | CRUD, validation, deck selection for match |
| Matchmaking | Queue, pairing, match creation |
| Game State | Turns, play card, attack, win condition |
