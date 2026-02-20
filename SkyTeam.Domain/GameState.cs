namespace SkyTeam.Domain;

sealed class GameState
{
    private readonly List<BlueDie> _unusedBlueDice = [];
    private readonly List<OrangeDie> _unusedOrangeDice = [];

    internal Player CurrentPlayer { get; private set; } = Player.Pilot;

    internal IReadOnlyList<BlueDie> UnusedBlueDice => _unusedBlueDice.AsReadOnly();
    internal IReadOnlyList<OrangeDie> UnusedOrangeDice => _unusedOrangeDice.AsReadOnly();

    internal void AddBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);
        _unusedBlueDice.Add(die);
    }

    internal void AddOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);
        _unusedOrangeDice.Add(die);
    }

    internal void RemoveBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_unusedBlueDice.Remove(die)) return;

        throw new InvalidOperationException("Blue die not found in unused dice.");
    }

    internal void RemoveOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_unusedOrangeDice.Remove(die)) return;

        throw new InvalidOperationException("Orange die not found in unused dice.");
    }

    internal void SwitchPlayer() =>
        CurrentPlayer = CurrentPlayer == Player.Pilot ? Player.Copilot : Player.Pilot;

    internal void Reset()
    {
        _unusedBlueDice.Clear();
        _unusedOrangeDice.Clear();
        CurrentPlayer = Player.Pilot;
    }
}
