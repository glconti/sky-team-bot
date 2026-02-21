## 2026-02-21: Issue #36 tests PR base branch

**Decision:** Deliver issue #36 (application turn/undo invariant tests) as a stacked PR targeting `squad/28-round-turn-state-secret-hand` (PR #39), because the tests exercise `RoundTurnState` / `SecretDiceHand` introduced by #28.

**Rationale:** Keeps CI green and aligns with the declared dependency graph (#36 depends on #28). Once #39 merges to `master`, this PR can be retargeted or merged normally.
