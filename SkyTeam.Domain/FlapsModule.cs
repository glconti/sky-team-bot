namespace SkyTeam.Domain;

sealed class FlapsModule(Airport airport) : GameModule
{
    private static readonly int[][] AllowedValuesBySwitch =
    [
        [1, 2],
        [2, 3],
        [4, 5],
        [5, 6]
    ];

    private readonly Airport _airport = airport ?? throw new ArgumentNullException(nameof(airport));

    private int _nextRequiredIndex;

    internal int FlapsValue { get; private set; }

    public override bool CanAcceptBlueDie(Player player) => false;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _nextRequiredIndex < AllowedValuesBySwitch.Length;

    public override string GetModuleName() => "Flaps";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice,
        CoffeeTokenPool tokenPool)
    {
        if (currentPlayer != Player.Copilot) yield break;
        if (_nextRequiredIndex >= AllowedValuesBySwitch.Length) yield break;

        var availableTokens = tokenPool.Count;
        var allowedValues = AllowedValuesBySwitch[_nextRequiredIndex];
        var switchNumber = _nextRequiredIndex + 1;

        foreach (var rolledValue in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
        {
            foreach (var requiredValue in allowedValues)
            {
                var tokenCost = Math.Abs(requiredValue - rolledValue);
                if (tokenCost > availableTokens) continue;

                yield return new ActivateFlapsCommand(rolledValue, requiredValue, tokenCost, switchNumber);
            }
        }
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_nextRequiredIndex >= AllowedValuesBySwitch.Length)
            throw new InvalidOperationException("All flaps switches are already activated.");

        var allowedValues = AllowedValuesBySwitch[_nextRequiredIndex];
        var value = (int)die;

        if (!allowedValues.Contains(value))
            throw new InvalidOperationException($"Flaps requires die value {allowedValues[0]} or {allowedValues[1]} next.");

        _nextRequiredIndex++;
        FlapsValue++;
        _airport.MoveOrangeAerodynamicsRight();
    }

    private sealed record ActivateFlapsCommand(
        int RolledValue,
        int EffectiveValue,
        int TokenCost,
        int SwitchNumber) : GameCommand
    {
        public override string CommandId => TokenCost == 0
            ? $"Flaps.AssignOrange:{RolledValue}"
            : $"Flaps.AssignOrange:{RolledValue}>{EffectiveValue}";

        public override string DisplayName => TokenCost == 0
            ? $"Flaps: activate switch {SwitchNumber} with orange {RolledValue}"
            : $"Flaps: activate switch {SwitchNumber} with orange {RolledValue} as {EffectiveValue} (cost {TokenCost})";
    }
}
