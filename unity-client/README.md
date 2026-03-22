# TCG Unity Client

Scaffold for the TCG-V2 Unity client. Integrates with the backend via REST and SignalR.

## Setup

1. Create a new Unity project (2022 LTS or newer).
2. Copy the contents of `Assets/Scripts/TcgClient/` into your project's `Assets/Scripts/TcgClient/`.
3. Create a TcgApiSettings asset: **Assets > Create > TCG > Api Settings**.
4. Set **Api Base Url** (e.g. `http://localhost:5000`).
5. For dev without Neon Auth, set **Dev User Id** (e.g. `test-user-1`).
6. Add **TcgGameManager** to a GameObject in your first scene.

## Scripts

- **TcgApiSettings** – ScriptableObject for API URL and auth config.
- **TcgRestClient** – REST client for decks, cards, leaderboard.
- **TcgSignalRClient** – Placeholder for SignalR (matchmaking, game actions).
- **TcgGameManager** – Example orchestrator.

## SignalR

For full SignalR support, add the Microsoft.AspNetCore.SignalR.Client package via NuGet for Unity, or use a WebSocket-based implementation. The current `TcgSignalRClient` is a stub showing the expected flow.

## Auth

- **Production**: Call Neon Auth REST API for sign-in, store JWT, pass to `TcgRestClient.SetJwt()` and `TcgSignalRClient.Configure()`.
- **Dev**: Use `X-User-Id` header (set `DevUserId` in TcgApiSettings). The backend accepts this when `NEON_AUTH_URL` is empty.
