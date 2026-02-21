namespace SkyTeam.Application.Presentation;

using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;

public static class GroupChatCockpitRenderer
{
    public static string RenderLobby(LobbySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var pilot = snapshot.Pilot?.DisplayName ?? "(empty)";
        var copilot = snapshot.Copilot?.DisplayName ?? "(empty)";
        var ready = snapshot.IsReady ? "Yes" : "No";

        return $"Lobby:\nPilot: {pilot}\nCopilot: {copilot}\nReady: {ready}";
    }

    public static string RenderInGame(GameSessionPublicState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var session = state.Session;

        var placementsText = state.PlacementsMade is null || state.PlacementsRemaining is null
            ? "-"
            : $"{state.PlacementsMade}/{RoundTurnState.MaxPlacementsPerRound} ({state.PlacementsRemaining} remaining)";

        var currentPlayerText = state.CurrentPlayer?.ToString() ?? "-";

        return $"Round {session.Round.RoundNumber} ({session.Round.Status})\n" +
               $"Pilot: {session.Pilot.DisplayName}\n" +
               $"Copilot: {session.Copilot.DisplayName}\n" +
               $"Turn: {currentPlayerText}\n" +
               $"Placements: {placementsText}\n\n" +
               RenderCockpit(state.Cockpit) +
               $"\n\nGame status: {state.GameStatus}";
    }

    public static string RenderRoundResolution(GameRoundResolutionPublicInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var text = $"Round {info.ResolvedRoundNumber} resolved.\n" +
                   RenderCockpit(info.ResolvedState) +
                   $"\n\nGame status: {info.GameStatus}";

        if (info.NextRoundNumber is not null)
            text += $"\n\nNext: round {info.NextRoundNumber} starts with {info.NextStartingPlayer}. In the group chat, run: /sky roll";

        return text;
    }

    private static string RenderCockpit(GameStatePublicSnapshot cockpit)
    {
        ArgumentNullException.ThrowIfNull(cockpit);

        var enginesSpeed = cockpit.EnginesSpeed is null ? "-" : cockpit.EnginesSpeed.ToString();

        return $"Axis: {cockpit.AxisPosition}\n" +
               $"Engines speed: {enginesSpeed}\n" +
               $"Approach: {cockpit.ApproachPositionIndex + 1}/{cockpit.ApproachSegmentCount} (planes remaining: {cockpit.TotalPlanesRemaining})\n" +
               $"Brakes: {cockpit.BrakesActivatedSwitchCount}/3 (capability: {cockpit.BrakesCapability})\n" +
               $"Flaps: {cockpit.FlapsValue}/4\n" +
               $"Landing gear: {cockpit.LandingGearValue}/3\n" +
               $"Coffee tokens: {cockpit.CoffeeTokens}\n" +
               $"Aerodynamics thresholds: blue {cockpit.BlueAerodynamicsThreshold}, orange {cockpit.OrangeAerodynamicsThreshold}";
    }
}
