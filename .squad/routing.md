# Routing Rules

## Domain Signal → Agent

| Signal | Route to |
|--------|----------|
| Architecture, design, code review, refactoring direction | Sully |
| C# code, module implementation, aggregate logic, value objects, commands | Skiles |
| Game rules, mechanic questions, rule validation, "how does X work in Sky Team" | Tenerife |
| Tests, edge cases, test coverage, FluentAssertions, test failures | Aloha |
| Multi-domain or "team" requests | Sully (lead) + relevant agents |
| Telegram bot integration, infrastructure | Skiles (primary) + Sully (review) |

## Reviewer Gates

| Reviewer | Reviews |
|----------|---------|
| Sully | Architecture decisions, API surface changes |
| Tenerife | Game mechanic correctness — any module implementing Sky Team rules |
| Aloha | Test quality, coverage gaps |
