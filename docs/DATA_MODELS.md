# TCG-V2 Data Models

## Core Entities

### User

Links to `neon_auth.user`. Game-specific profile stored in our schema.

- `id` (PK, FK to neon_auth.user)
- `display_name`, `created_at`, etc.

### CardDefinition

Static card data. Seeded; all users share the same pool initially.

- `id`, `name`, `description`
- `cost`, `attack`, `defense` (for creatures)
- `card_type` (creature, spell, etc.)
- `effects` (JSONB)

### Deck

User-owned deck.

- `id`, `user_id`, `name`
- `created_at`, `updated_at`

### DeckSlot

Cards in a deck (many-to-many with quantity).

- `deck_id`, `card_definition_id`, `quantity`

### Match

Game session.

- `id`, `status` (waiting, in_progress, finished)
- `created_at`, `ended_at`

### MatchParticipant

Player in a match.

- `match_id`, `user_id`, `deck_id`
- `player_index` (0 or 1)
- `life_total`, `status`

### GameState

Authoritative board state (can be stored as JSONB or normalized).

- Hands, board, turn, phase, life totals, etc.
