# Gimli: PR #38 sync with master

Date: 2026-02-21

Decision:
- To unblock PR #38 after PR #37, I merged `origin/master` into `squad/31-domain-tests` and resolved conflicts by taking the `origin/master` implementations for the affected Domain modules and related tests.

Rationale:
- PR #37 introduced a newer command execution pattern (commands own `Execute(Game)` and carry module references); keeping master’s versions minimizes risk and preserves the current architecture while retaining PR #38’s non-conflicting changes.
