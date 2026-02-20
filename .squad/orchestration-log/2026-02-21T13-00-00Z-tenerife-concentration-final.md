# Orchestration Log: Tenerife (2026-02-21T13:00:00Z)

## Agent
**Name:** Tenerife  
**Role:** Rules Expert  
**Status:** Completed

## Task
Finalize Concentration rule specification reconciling official Sky Team rules with user clarifications on coffee tokens.

## Actions Taken
1. Reviewed official Sky Team Concentration rules from reference source.
2. Integrated user directive clarifications:
   - Shared pool with max capacity 3 tokens
   - Multiple tokens may be spent on same die placement
   - Token-cost options clearly marked in Telegram UI
3. Reconciled with Skiles domain modeling proposal and Sully architecture assessment.
4. Produced comprehensive spec addressing:
   - Token gain: +1 per die on Concentration, capped at 3
   - Token spend: Before placement, costs 1 token to adjust die by ±1
   - Boundary cases: Die values 1 and 6 restricted to {1,2} and {5,6}
   - Multiple spend interpretation: OPEN — needs user clarification on whether multi-token means ±N shift or just multi-die in same round
   - Telegram UX: Spend announcement public, placement secret until resolution

5. Documented 5 open questions for team vote:
   - Question 1: Multiple token spends per die (needs Gianluigi clarification)
   - Question 2: Token spend visibility (LOCKED: public)
   - Question 3: Token earn timing (LOCKED: immediate)

6. Produced edge case resolutions (e.g., pool full, boundary shifts, reroll interaction).

## Artifacts
- `tenerife-concentration-official-spec.md` — Official rules baseline + clarifications
- `concentration-coffee-tokens-final.md` — User-directive-aligned summary
- Acceptance criteria for Skiles (implementation) and Aloha (testing)

## Next Steps
- **Gianluigi (User):** Clarify Question 1 (multi-token interpretation) before implementation
- **Team:** Vote on open questions once clarified
- **Skiles:** Implement CoffeeTokenPool value object + GameState integration
- **Aloha:** Write tests per acceptance criteria
- **Scribe:** Move spec to `.squad/decisions.md` once locked
