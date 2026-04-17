# Skill: Chat-context-bound game mutations

## Context
Use this when the same Telegram user can be active in multiple game sessions across chats.

## Pattern
1. Accept both `groupChatId` and `userId` in mutation APIs (`Place`, `Undo`, etc.).
2. Resolve signed chat context at transport boundary (WebApp `gameId` + initData chat/start_param).
3. In application store, load session by `groupChatId` first, then validate user is seated in that session.
4. Keep temporary user-only wrappers to avoid breaking older callers while migrating.
5. Add a regression test where one user has two active sessions and verify only requested chat mutates.

## Why it works
- Prevents user-id-only routing bugs that can target the wrong chat session.
- Keeps authorization explicit at boundaries without polluting domain entities with transport concerns.
- Makes cross-chat rejection deterministic (`NotSeated`/conflict for unauthorized context).
