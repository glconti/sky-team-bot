namespace SkyTeam.Domain;

sealed class ConcentrationModule : GameModule
{
    private const int SlotsPerRound = 2;

    private int _slotsUsed;
    private CoffeeTokenPool _tokenPool;

    internal CoffeeTokenPool TokenPool => _tokenPool;

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _slotsUsed < SlotsPerRound;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _slotsUsed < SlotsPerRound;

    public override string GetModuleName() => "Concentration";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice)
    {
        if (_slotsUsed >= SlotsPerRound) yield break;

        if (currentPlayer == Player.Pilot)
        {
            foreach (var value in unusedBlueDice.Select(die => (int)die).Distinct().Order())
                yield return new AssignConcentrationBlueDieCommand(value);

            yield break;
        }

        if (currentPlayer != Player.Copilot) yield break;

        foreach (var value in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
            yield return new AssignConcentrationOrangeDieCommand(value);
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_slotsUsed >= SlotsPerRound)
            throw new InvalidOperationException("No concentration spaces available this round.");

        _slotsUsed++;
        _tokenPool = _tokenPool.Earn();
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_slotsUsed >= SlotsPerRound)
            throw new InvalidOperationException("No concentration spaces available this round.");

        _slotsUsed++;
        _tokenPool = _tokenPool.Earn();
    }

    public override void ResetRound() => _slotsUsed = 0;

    private sealed record AssignConcentrationBlueDieCommand(int Value) : GameCommand
    {
        public override string CommandId => $"Concentration.AssignBlue:{Value}";
        public override string DisplayName => $"Concentration: place blue {Value}";
    }

    private sealed record AssignConcentrationOrangeDieCommand(int Value) : GameCommand
    {
        public override string CommandId => $"Concentration.AssignOrange:{Value}";
        public override string DisplayName => $"Concentration: place orange {Value}";
    }
}
