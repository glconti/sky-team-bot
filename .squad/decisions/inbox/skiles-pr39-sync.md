## 2026-02-21T18:56:06Z: PR #39 sync with master (Skiles)

**By:** Skiles (Domain Developer)

**Decision:** Rebased PR #39 branch (`squad/28-round-turn-state-secret-hand`) onto `origin/master` to unblock merge conflicts; resolved Brakes-related conflicts by keeping the current master landing/braking-capability logic, and skipped the older coffee-token adjustment commit that was already upstream.

**Rationale:** Minimizes divergence from master, avoids re-introducing stale token-adjustment implementations, and keeps PR focused on Issue #28 changes.
