# 2026-02-21: PR #39 (Issue #28) — ready to undraft

**By:** Skiles (Domain Dev)  
**Decision:** PR #39 (Issue #28: application-layer round/turn state + secret dice hand) is coherent and can be moved out of Draft.

**Evidence:**
- `dotnet test -c Release` passes (145 tests, 0 failures).
- Changes are isolated to application-layer round/turn orchestration primitives (no Telegram SDK leakage), matching prior Issue #28 design decision.

**Rationale:** The PR provides the minimal state machine + invariants needed for strict alternation and private placement flows, and it is now green on the full test suite in Release configuration.
