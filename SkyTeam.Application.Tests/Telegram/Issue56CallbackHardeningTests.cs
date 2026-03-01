namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;
using SkyTeam.TelegramBot;

public sealed class Issue56CallbackHardeningTests
{
    [Fact]
    public void CallbackDataCodec_ShouldRoundTripSupportedAction_WhenFormatIsVersioned()
    {
        // Arrange
        var encoded = CallbackDataCodec.EncodeGroupAction("refresh");

        // Act
        var parsed = CallbackDataCodec.TryDecodeGroupAction(encoded, out var canonical);

        // Assert
        parsed.Should().BeTrue();
        canonical.Should().Be(encoded);
    }

    [Fact]
    public void CallbackDataCodec_ShouldRejectUnknownOrMalformedPayload()
    {
        // Arrange
        const string malformed = "v1:grp:unknown-action";

        // Act
        var parsed = CallbackDataCodec.TryDecodeGroupAction(malformed, out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void CallbackDataCodec_ShouldReject_WhenPayloadExceedsTelegramMaxLength()
    {
        // Arrange
        var payload = new string('x', 65);

        // Act
        var parsed = CallbackDataCodec.TryDecodeGroupAction(payload, out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void MenuStateStore_ShouldReturnUnknownOrExpired_WhenChatBindingDoesNotMatch()
    {
        // Arrange
        var store = new CallbackMenuStateStore();
        var callbackData = CallbackDataCodec.EncodeGroupAction("refresh");
        store.RegisterGroupMenu(groupChatId: 100, messageId: 10, [callbackData]);

        // Act
        var status = store.ValidateAndMarkProcessed(userId: 1, groupChatId: 999, messageId: 10, callbackData);

        // Assert
        status.Should().Be(CallbackMenuValidationStatus.UnknownOrExpired);
    }

    [Fact]
    public void MenuStateStore_ShouldAllowMultipleUsers_WhenMenuIsNotUserBound()
    {
        // Arrange
        var store = new CallbackMenuStateStore();
        var callbackData = CallbackDataCodec.EncodeGroupAction("refresh");
        store.RegisterGroupMenu(groupChatId: 100, messageId: 10, [callbackData]);

        // Act
        var statuses = new[]
        {
            store.ValidateAndMarkProcessed(userId: 1, groupChatId: 100, messageId: 10, callbackData),
            store.ValidateAndMarkProcessed(userId: 2, groupChatId: 100, messageId: 10, callbackData)
        };

        // Assert
        statuses.Should().Equal(CallbackMenuValidationStatus.Valid, CallbackMenuValidationStatus.Valid);
    }

    [Fact]
    public void MenuStateStore_ShouldRejectDifferentUser_WhenMenuIsUserBound()
    {
        // Arrange
        var store = new CallbackMenuStateStore();
        var callbackData = CallbackDataCodec.EncodeGroupAction("refresh");
        store.RegisterGroupMenu(groupChatId: 100, messageId: 10, [callbackData], userId: 1);

        // Act
        var status = store.ValidateAndMarkProcessed(userId: 2, groupChatId: 100, messageId: 10, callbackData);

        // Assert
        status.Should().Be(CallbackMenuValidationStatus.UnknownOrExpired);
    }

    [Fact]
    public void MenuStateStore_ShouldInvalidateOldMessageId_WhenGroupMenuIsReRegistered()
    {
        // Arrange
        var store = new CallbackMenuStateStore();
        var callbackData = CallbackDataCodec.EncodeGroupAction("refresh");
        store.RegisterGroupMenu(groupChatId: 100, messageId: 10, [callbackData]);
        store.RegisterGroupMenu(groupChatId: 100, messageId: 11, [callbackData]);

        // Act
        var status = store.ValidateAndMarkProcessed(userId: 1, groupChatId: 100, messageId: 10, callbackData);

        // Assert
        status.Should().Be(CallbackMenuValidationStatus.UnknownOrExpired);
    }

    [Fact]
    public void MenuStateStore_ShouldReturnDuplicate_WhenSameCallbackIsReplayed()
    {
        // Arrange
        var store = new CallbackMenuStateStore();
        var callbackData = CallbackDataCodec.EncodeGroupAction("refresh");
        store.RegisterGroupMenu(groupChatId: 100, messageId: 10, [callbackData]);

        // Act
        var first = store.ValidateAndMarkProcessed(userId: 1, groupChatId: 100, messageId: 10, callbackData);
        var replay = store.ValidateAndMarkProcessed(userId: 1, groupChatId: 100, messageId: 10, callbackData);

        // Assert
        first.Should().Be(CallbackMenuValidationStatus.Valid);
        replay.Should().Be(CallbackMenuValidationStatus.Duplicate);
    }

    [Fact]
    public void MenuStateStore_ShouldExpireState_WhenTtlElapsed()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var store = new CallbackMenuStateStore(
            timeToLive: TimeSpan.FromSeconds(1),
            utcNow: () => now);
        var callbackData = CallbackDataCodec.EncodeGroupAction("refresh");
        store.RegisterGroupMenu(groupChatId: 100, messageId: 10, [callbackData]);
        now = now.AddSeconds(2);

        // Act
        var status = store.ValidateAndMarkProcessed(userId: 1, groupChatId: 100, messageId: 10, callbackData);

        // Assert
        status.Should().Be(CallbackMenuValidationStatus.UnknownOrExpired);
    }
}
