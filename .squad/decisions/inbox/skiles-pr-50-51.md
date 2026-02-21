# Skiles — PR publish decision for issues #50 and #51

## Decision
Publish callback plumbing (#50) and cockpit lifecycle (#51) together in one draft PR because both changes converge in `SkyTeam.TelegramBot\Program.cs` through the shared group cockpit refresh flow.

## Why
- Callback `v1:grp:refresh` behavior depends on the same edit/recreate cockpit lifecycle introduced for #51.
- A single PR gives reviewers one coherent end-to-end path for group state rendering, callback answering, and cockpit message persistence.

## Test Evidence
- `dotnet test SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj`

## Follow-up
- Replace skipped Telegram contract tests in `SkyTeam.Application.Tests\Telegram\Issue50CallbackQueryFlowTests.cs` and `SkyTeam.Application.Tests\Telegram\Issue51CockpitLifecycleTests.cs` with executable integration tests after introducing Telegram client seams.
