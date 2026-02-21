using System.Collections.Immutable;

namespace SkyTeam.Application.Round;

public enum RoundPhase
{
    InProgress,
    ReadyToResolve
}

public sealed record RoundPlacement(int Index, PlayerSeat Player, int DieIndex, DieValue Value);

public sealed class RoundTurnState
{
    public const int MaxPlacementsPerRound = SecretDiceHand.DicePerHand * 2;

    private readonly ImmutableArray<RoundPlacement> _placements;

    private RoundTurnState(
        int roundNumber,
        PlayerSeat startingPlayer,
        PlayerSeat currentPlayer,
        SecretDiceHand pilotHand,
        SecretDiceHand copilotHand,
        RoundPhase phase,
        ImmutableArray<RoundPlacement> placements)
    {
        if (roundNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(roundNumber), "Round numbers must be positive.");

        ArgumentNullException.ThrowIfNull(pilotHand);
        ArgumentNullException.ThrowIfNull(copilotHand);

        if (placements.Length > MaxPlacementsPerRound)
            throw new ArgumentOutOfRangeException(nameof(placements));

        RoundNumber = roundNumber;
        StartingPlayer = startingPlayer;
        CurrentPlayer = currentPlayer;
        PilotHand = pilotHand;
        CopilotHand = copilotHand;
        Phase = phase;
        _placements = placements;
    }

    public int RoundNumber { get; }
    public PlayerSeat StartingPlayer { get; }
    public PlayerSeat CurrentPlayer { get; }
    public SecretDiceHand PilotHand { get; }
    public SecretDiceHand CopilotHand { get; }
    public RoundPhase Phase { get; }

    public IReadOnlyList<RoundPlacement> Placements => _placements;

    public int PlacementsMade => _placements.Length;
    public int PlacementsRemaining => MaxPlacementsPerRound - PlacementsMade;

    public static RoundTurnState StartNew(
        int roundNumber,
        PlayerSeat startingPlayer,
        SecretDiceHand pilotHand,
        SecretDiceHand copilotHand)
        => new(
            roundNumber,
            startingPlayer,
            startingPlayer,
            pilotHand,
            copilotHand,
            RoundPhase.InProgress,
            ImmutableArray<RoundPlacement>.Empty);

    public bool CanPlace(PlayerSeat player) => Phase == RoundPhase.InProgress
                                              && player == CurrentPlayer
                                              && PlacementsMade < MaxPlacementsPerRound;

    public RoundTurnState RegisterPlacement(PlayerSeat player, int dieIndex)
    {
        if (Phase != RoundPhase.InProgress)
            throw new InvalidOperationException("This round is not accepting placements.");

        if (PlacementsMade >= MaxPlacementsPerRound)
            throw new InvalidOperationException("This round has no placements remaining.");

        if (player != CurrentPlayer)
            throw new InvalidOperationException($"It is not {player}'s turn.");

        var placements = _placements;
        var placementIndex = placements.Length;

        SecretDiceHand newPilotHand = PilotHand;
        SecretDiceHand newCopilotHand = CopilotHand;

        DieValue placedValue;

        if (player == PlayerSeat.Pilot)
            newPilotHand = PilotHand.UseDie(dieIndex, out placedValue);
        else
            newCopilotHand = CopilotHand.UseDie(dieIndex, out placedValue);

        placements = placements.Add(new RoundPlacement(placementIndex, player, dieIndex, placedValue));

        var newPhase = placements.Length == MaxPlacementsPerRound
            ? RoundPhase.ReadyToResolve
            : RoundPhase.InProgress;

        return new RoundTurnState(
            RoundNumber,
            StartingPlayer,
            currentPlayer: CurrentPlayer.Other(),
            newPilotHand,
            newCopilotHand,
            newPhase,
            placements);
    }

    public bool CanUndoLastPlacement(PlayerSeat requestingPlayer)
    {
        if (Phase != RoundPhase.InProgress) return false;
        if (_placements.IsEmpty) return false;

        var last = _placements[^1];
        return last.Player == requestingPlayer && CurrentPlayer != requestingPlayer;
    }

    public RoundTurnState UndoLastPlacement(PlayerSeat requestingPlayer)
    {
        if (!CanUndoLastPlacement(requestingPlayer))
            throw new InvalidOperationException("Cannot undo the last placement at this time.");

        var last = _placements[^1];

        SecretDiceHand newPilotHand = PilotHand;
        SecretDiceHand newCopilotHand = CopilotHand;

        if (last.Player == PlayerSeat.Pilot)
            newPilotHand = PilotHand.UnuseDie(last.DieIndex);
        else
            newCopilotHand = CopilotHand.UnuseDie(last.DieIndex);

        var placements = _placements.RemoveAt(_placements.Length - 1);

        return new RoundTurnState(
            RoundNumber,
            StartingPlayer,
            currentPlayer: last.Player,
            newPilotHand,
            newCopilotHand,
            RoundPhase.InProgress,
            placements);
    }
}
