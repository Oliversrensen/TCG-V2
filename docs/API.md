# TCG-V2 API Reference

## REST API

Base URL: `https://<host>/api`

All protected endpoints require: `Authorization: Bearer <jwt>`

### Auth

Handled by **Neon Auth**. This API only validates JWT. Sign-in/sign-up are done via Neon Auth REST API.

### Users

- `GET /users/me` – Current user profile (from JWT + optional DB data)

### Decks

- `GET /decks` – List user's decks
- `POST /decks` – Create deck
- `GET /decks/{id}` – Get deck by ID
- `PUT /decks/{id}` – Update deck
- `DELETE /decks/{id}` – Delete deck

### Cards

- `GET /cards/definitions` – List all card definitions (pool for deck building)

### Leaderboard

- `GET /leaderboard?top=10` – Top players by wins

## SignalR Hub: GameHub

Endpoint: `/hubs/game`

### Client → Server

- `JoinQueue(deckId)` – Enter matchmaking
- `LeaveQueue()` – Leave matchmaking
- `PlayCard(cardId, targetId?)` – Play a card
- `Attack(attackerId, targetId)` – Attack with creature
- `EndTurn()` – End current turn

### Server → Client

- `MatchFound(matchId, opponentDeckId)` – Match ready
- `StateUpdate(state)` – Full game state update
- `TurnChange(playerId)` – Turn changed
- `GameOver(winnerId)` – Match ended
- `Error(message)` – Error (e.g., invalid move)

### Reconnection

- `RejoinMatch(matchId)` – After reconnect, call with the match ID from MatchFound to resume
