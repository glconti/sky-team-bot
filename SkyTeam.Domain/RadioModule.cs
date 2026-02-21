namespace SkyTeam.Domain;

sealed class RadioModule(Airport airport) : GameModule
{
    private const int MaxOrangeDicePerRound = 2;

    private readonly Airport _airport = airport ?? throw new ArgumentNullException(nameof(airport));

    private BlueDie? _blueDie;
    private readonly List<OrangeDie> _orangeDice = [];

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _blueDie is null;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _orangeDice.Count < MaxOrangeDicePerRound;

    public override string GetModuleName() => "Radio";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice)
    {
        if (currentPlayer == Player.Pilot)
        {
            if (_blueDie is not null) yield break;

            foreach (var value in unusedBlueDice.Select(die => (int)die).Distinct().Order())
                yield return new AssignRadioBlueDieCommand(value);

            yield break;
        }

        if (currentPlayer != Player.Copilot) yield break;
        if (_orangeDice.Count >= MaxOrangeDicePerRound) yield break;

        foreach (var value in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
            yield return new AssignRadioOrangeDieCommand(value);
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_blueDie is not null)
            throw new InvalidOperationException("Blue die already assigned.");

        _blueDie = die;
        _airport.TryRemovePlaneTokenAtOffset((int)die - 1);
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_orangeDice.Count >= MaxOrangeDicePerRound)
            throw new InvalidOperationException("All orange dice already assigned.");

        _orangeDice.Add(die);
        _airport.TryRemovePlaneTokenAtOffset((int)die - 1);
    }

    public override void ResetRound()
    {
        _blueDie = null;
        _orangeDice.Clear();
    }

    private sealed record AssignRadioBlueDieCommand(int Value) : GameCommand
    {
        public override string CommandId => $"Radio.AssignBlue:{Value}";
        public override string DisplayName => $"Radio: place blue {Value}";
    }

    private sealed record AssignRadioOrangeDieCommand(int Value) : GameCommand
    {
        public override string CommandId => $"Radio.AssignOrange:{Value}";
        public override string DisplayName => $"Radio: place orange {Value}";
    }
}
