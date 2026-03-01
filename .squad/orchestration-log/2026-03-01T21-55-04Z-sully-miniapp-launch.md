# Orchestration: Sully — Mini App Launch Surface Decision

**Agent:** Sully (Lead/Architect)  
**Timestamp:** 2026-03-01T21:55:04Z  
**Task:** Mini App launch surface design review  
**Status:** Completed

## Summary
Reviewed Telegram Mini App launch flow and made decision on best surface for "Open app" button in group cockpit. Chose `startapp` deep links with Main Mini App BotFather configuration to avoid forcing users through private bot chat and enable a seamless app-first UX.

## Decision Logged
- **Title:** Mini App launch surface (avoid private bot chat)
- **Key:** Use `startapp` deep links + BotFather Main Mini App config
- **Fallback:** Direct-link Mini App form if needed
- **Inbox file:** `sully-miniapp-launch-surface.md`

## Next Steps
- BotFather configuration (Main Mini App URL + domain)
- Web backend validation of signed `start_param`
- QA validation of "Open app" flow from group cockpit
