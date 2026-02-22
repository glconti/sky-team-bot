# 2026-02-22: PR #58 publication update for issue #52

**By:** Skiles (Domain Dev)  
**Context:** Publish completed issue #52 lobby cockpit button slice on existing draft PR #58.

## Decision
- Keep PR #58 as the single draft vehicle for issues #50, #51, and #52.
- Extend PR title/body/checklist to explicitly include issue #52 scope:
  - Group cockpit buttons: `New`, `Join`, `Start`, `Refresh`
  - Callback routing + legality toasts + no-op on invalid callbacks
  - Cockpit refresh via existing edit-first lifecycle
  - `/sky new|join|start` fallback parity
- Include current test evidence from `SkyTeam.Application.Tests` and note skipped-contract tests for remaining callback seam coverage.
- Add `Closes #52` in PR body because #52 implementation scope shipped in this branch.

## Rationale
- Preserves reviewer context on one branch and avoids splitting tightly coupled cockpit/callback work.
- Makes completion status explicit for issue tracking and release notes.
