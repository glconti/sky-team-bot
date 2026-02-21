## 2026-02-21: PR39 resync (2)

**Context:** PR #39 (`squad/28-round-turn-state-secret-hand`) was conflicting after `master` advanced.

**Decision:** Re-synced by rebasing the PR branch onto latest `origin/master` using `git rebase --rebase-merges origin/master` and force-pushing with `--force-with-lease`.

**Conflict resolution stance (if future conflicts recur):**
- Keep **current `master`** domain logic for landing/brakes (Domain layer is source of truth).
- Preserve PR branch changes for **application-layer** Round/Turn state and secret-hand workflow.

**Result:** Rebase applied cleanly (no manual conflict resolution needed) and branch updated on origin.
