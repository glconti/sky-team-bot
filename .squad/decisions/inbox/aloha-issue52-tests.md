# 2026-02-22: Issue #52 test contract status

**By:** Aloha (Tester)  
**Context:** Implementing tests for lobby button callback flow (`New`, `Join`, `Start`, `Refresh`) and fallback behavior.

## Decision
- Add issue-52 test coverage as mixed verification + contract scaffold:
  - Active checks for currently verifiable behavior (`Refresh` callback button presence and `/sky state` fallback contract).
  - Skipped contract tests (with explicit rationale) for callback paths not yet fully testable/implemented (`New/Join/Start` callbacks, invalid press no-op side effects, successful callback integration with existing handlers and cockpit edit lifecycle).

## Rationale
- Keeps CI green while making the missing behavior explicit and traceable.
- Allows fast unskip once callback handlers and injectable seams for side-effect assertions are available.

## Implications
- Issue #52 has concrete executable acceptance placeholders in `Issue52LobbyButtonFlowTests`.
- Team can treat skip reasons as implementation checklist for callback completion.
