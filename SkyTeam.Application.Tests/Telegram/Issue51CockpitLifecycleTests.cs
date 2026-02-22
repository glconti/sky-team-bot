namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;
using SkyTeam.Application.GameSessions;

public sealed class Issue51CockpitLifecycleTests
{
    [Fact]
    public void CockpitMessageId_ShouldBePersistedPerGroupSession()
    {
        // Arrange
        const long groupChatId = 123;
        const int cockpitMessageId = 456;
        var store = new InMemoryGroupGameSessionStore();

        // Act
        store.SetCockpitMessageId(groupChatId, cockpitMessageId);
        var found = store.TryGetCockpitMessageId(groupChatId, out var storedMessageId);

        // Assert
        found.Should().BeTrue();
        storedMessageId.Should().Be(cockpitMessageId);
    }

    [Fact]
    public void CockpitMessageId_ShouldKeepSingleLatestValue_WhenRecreated()
    {
        // Arrange
        const long groupChatId = 123;
        var store = new InMemoryGroupGameSessionStore();

        // Act
        store.SetCockpitMessageId(groupChatId, cockpitMessageId: 111);
        store.SetCockpitMessageId(groupChatId, cockpitMessageId: 222);
        var found = store.TryGetCockpitMessageId(groupChatId, out var storedMessageId);

        // Assert
        found.Should().BeTrue();
        storedMessageId.Should().Be(222);
    }

    [Fact(Skip = "Issue #51 recreate-on-missing-message flow is not implemented yet in SkyTeam.TelegramBot.Program.")]
    public void CockpitLifecycle_ShouldRecreateCockpit_WhenStoredMessageIsMissingOrNotEditable()
    {
        // Arrange
        var implemented = false;

        // Act
        // TODO(issue-51): simulate missing/uneditable cockpit message and verify recreate + id replacement flow.

        // Assert
        implemented.Should().BeTrue("missing or uneditable cockpit message should be recreated and tracking updated");
    }

    [Fact(Skip = "Issue #51 best-effort auto-pin behavior is not implemented yet in SkyTeam.TelegramBot.Program.")]
    public void CockpitLifecycle_ShouldContinue_WhenBestEffortAutoPinFails()
    {
        // Arrange
        var implemented = false;

        // Act
        // TODO(issue-51): simulate pin failure and verify cockpit creation/edit flow still succeeds.

        // Assert
        implemented.Should().BeTrue("pin failures should not break cockpit lifecycle");
    }

    [Fact(Skip = "Issue #51 cockpit lifecycle is not implemented yet in SkyTeam.TelegramBot.Program.")]
    public void SkyCommandFallback_ShouldRefreshCockpit_WhenButtonLifecycleCannotProceed()
    {
        // Arrange
        var implemented = false;

        // Act
        // TODO(issue-51): validate /sky state still renders and refreshes cockpit when callback path cannot proceed.

        // Assert
        implemented.Should().BeTrue("text command fallback should remain a valid cockpit refresh path");
    }
}
