# 2026-02-22: Epic #49 re-sliced into vertical slices (Sully)

**By:** Sully (Architect)

## Decision
Reframe Epic **#49 (Button-first Telegram UX)** and child issues **#50–#57** as **incremental vertical slices** where *each* issue delivers a small but shippable UX improvement end-to-end.

## Rationale
Purely horizontal infrastructure issues tend to stall value delivery and make progress hard to validate; vertical slices allow shipping and validating UX improvements continuously while keeping `/sky ...` as a safe fallback.

## Slice order (issue mapping)
1. **Slice 1 — #50:** CallbackQuery support + one safe **Refresh** button end-to-end.
2. **Slice 2 — #51:** Cockpit lifecycle (single edited group message) + **best-effort auto-pin**.
3. **Slice 3 — #52:** Lobby buttons (**New / Join / Start**) end-to-end.
4. **Slice 4 — #53:** In-game cockpit buttons (**Roll / Place (DM) / Refresh**) + DM onboarding hint/link.
5. **Slice 5 — #54:** DM Hand menu v1 (**Refresh / Undo**) via buttons.
6. **Slice 6 — #55:** DM placement flow via buttons (**die → command → place**).
7. **Slice 7 — #56:** Hardening: callback_data encoding + menu state store (**expiry / dedup / validation**) so stale buttons show "menu expired".

**Stretch:** **#57** (Telegram Menu Button / WebApp cockpit).

## Guardrails / confirmed constraints (applied to all slices)
- Group chat UX uses a **single edited cockpit message**; auto-pin if possible.
- **Anyone can press** group buttons; server-side rules enforce seat/turn validity.
- Group **"Place (DM)"** only refreshes the pressing user's DM UI; **no secret leakage** in group.
- `callback_data` must stay **<= 64 bytes**; use short versioned tokens and server-side mappings.
- `/sky ...` text commands remain fully supported as fallback.
