namespace SkyTeam.Domain;

sealed class GameState
{
    private readonly List<BlueDie> _unusedBlueDice = [];
    private readonly List<OrangeDie> _unusedOrangeDice = [];

    internal Player CurrentPlayer { get; private set; } = Player.Pilot;

    internal CoffeeTokenPool TokenPool { get; private set; } = new();

    internal IReadOnlyList<BlueDie> UnusedBlueDice => _unusedBlueDice.AsReadOnly();
    internal IReadOnlyList<OrangeDie> UnusedOrangeDice => _unusedOrangeDice.AsReadOnly();

    internal void SetCurrentPlayer(Player player) => CurrentPlayer = player;

    internal void ClearUnusedDice()
    {
        _unusedBlueDice.Clear();
        _unusedOrangeDice.Clear();
    }

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

    internal void EarnCoffeeTokens(int amount = 1) => TokenPool = TokenPool.Earn(amount);
    internal void SpendCoffeeTokens(int amount) => TokenPool = TokenPool.Spend(amount);

    internal void Reset()
    {
        ClearUnusedDice();
        CurrentPlayer = Player.Pilot;
        TokenPool = new();
    }
}
