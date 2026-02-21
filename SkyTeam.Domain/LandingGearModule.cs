namespace SkyTeam.Domain;

sealed class LandingGearModule(Airport airport) : GameModule
{
    private static readonly int[][] AllowedValuesBySwitch =
    [
        [1, 2],
        [3, 4],
        [5, 6]
    ];

    private readonly Airport _airport = airport ?? throw new ArgumentNullException(nameof(airport));
    private readonly bool[] _isSwitchActivated = new bool[AllowedValuesBySwitch.Length];

    internal int LandingGearValue { get; private set; }

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _isSwitchActivated.Any(activated => !activated);

    public override bool CanAcceptOrangeDie(Player player) => false;

    public override string GetModuleName() => "Landing Gear";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice)
    {
        if (currentPlayer != Player.Pilot) yield break;
        if (_isSwitchActivated.All(activated => activated)) yield break;

        foreach (var value in unusedBlueDice.Select(die => (int)die).Distinct().Order())
        {
            var switchIndex = GetSwitchIndexForValue(value);
            if (_isSwitchActivated[switchIndex]) continue;

            yield return new ActivateLandingGearCommand(value, switchIndex + 1);
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_isSwitchActivated.All(activated => activated))
            throw new InvalidOperationException("All landing gear switches are already activated.");

        var value = (int)die;
        var switchIndex = GetSwitchIndexForValue(value);

        if (_isSwitchActivated[switchIndex]) return;

        _isSwitchActivated[switchIndex] = true;
        LandingGearValue++;
        _airport.MoveBlueAerodynamicsRight();
    }

    private static int GetSwitchIndexForValue(int value) => value switch
    {
        1 or 2 => 0,
        3 or 4 => 1,
        5 or 6 => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Die values must be between 1 and 6.")
    };

    private sealed record ActivateLandingGearCommand(int Value, int SwitchNumber) : GameCommand
    {
        public override string CommandId => $"LandingGear.AssignBlue:{Value}";
        public override string DisplayName => $"Landing gear: deploy switch {SwitchNumber} with {Value}";
    }
}
