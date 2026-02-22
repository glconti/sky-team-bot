# Skiles — Issue #65 PR note

- PR: https://github.com/glconti/sky-team-bot/pull/73
- Branch: `skiles/issue-65-miniapp-hardening`
- Draft: `false`
- Mergeability: `UNKNOWN` (GitHub has not resolved mergeability yet)

## Acceptance summary
- Added idempotent placement replay support in `InMemoryGroupGameSessionStore` with optional `idempotencyKey` and bounded replay cache.
- Added regression tests for placement replay/idempotency and round/lobby guard paths.
- Updated README Mini App flow wording for secret actions.

Closes #65.
