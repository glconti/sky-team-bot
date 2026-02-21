---
date: 2026-02-21
decided_by: Sully
epic: "#26"
status: "CLOSEABLE"
---

# Epic #26 Triage: Telegram MVP Playable in Group Chat

## Decision
**CLOSE Epic #26 as COMPLETED.**

## Rationale

**All 10 child issues are CLOSED:**
- **P0 Path (Core MVP):** #27 (start command), #28 (app state), #29 (secret DM roll), #30 (public placements), #31 (domain modules), #32 (round resolution)
- **P1 Polish:** #33 (undo support), #34 (cockpit renderer), #35 (per-chat dedup), #36 (app tests)

**Merged Work:**
- PR #47: Undo (#33) + cockpit renderer (#34) + app tests (#36)
- PR #48: Per-chat serialization + idempotency (#35)

**MVP Goal Satisfied:**
✅ Playable in group chat (2 seated players + spectators)
✅ Secret dice rolls per player (DM'd privately)
✅ Public placements with strict alternation
✅ Undo support (current player, before opponent plays)
✅ Round resolution + win/loss broadcast
✅ Complete domain modules (all 7)
✅ In-memory persistence (OK for MVP)
✅ Hardening: per-chat serialization, idempotency, dedup

**Constraints Met:**
- Secret dice roll ✅
- Placements public ✅
- Strict alternation ✅
- Conditional undo (before opponent plays) ✅
- In-memory OK ✅

## Next Steps

1. **Gianluigi:** Close #26 via comment or PR comment linking to this decision.
2. **Team:** Create new Epic for post-MVP work:
   - Persistence (DB, Redis)
   - Spectator visibility (cards dealt, token spends)
   - UX polish (better messages, inline keyboards)
   - Stats/leaderboard
   - Reroll/mulligan mechanics

## Links

- Epic #26: https://github.com/glconti/sky-team-bot/issues/26
- PR #47 (undo + cockpit): https://github.com/glconti/sky-team-bot/pull/47
- PR #48 (per-chat dedup): https://github.com/glconti/sky-team-bot/pull/48
