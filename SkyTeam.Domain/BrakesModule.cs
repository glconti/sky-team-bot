namespace SkyTeam.Domain;

sealed class BrakesModule : GameModule
{
    private static readonly int[] RequiredValues = [2, 4, 6];

    private int _nextRequiredIndex;

    internal int BrakesValue { get; private set; }

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _nextRequiredIndex < RequiredValues.Length;

    public override bool CanAcceptOrangeDie(Player player) => false;

    public override string GetModuleName() => "Brakes";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice)
    {
        if (currentPlayer != Player.Pilot) yield break;
        if (_nextRequiredIndex >= RequiredValues.Length) yield break;

        var requiredValue = RequiredValues[_nextRequiredIndex];
        if (!unusedBlueDice.Any(die => (int)die == requiredValue)) yield break;

        yield return new ActivateBrakesCommand(requiredValue, _nextRequiredIndex + 1);
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_nextRequiredIndex >= RequiredValues.Length)
            throw new InvalidOperationException("All brakes switches are already activated.");

        var requiredValue = RequiredValues[_nextRequiredIndex];
        if ((int)die != requiredValue)
            throw new InvalidOperationException($"Brakes requires die value {requiredValue} next.");

        _nextRequiredIndex++;
        BrakesValue = requiredValue;
    }

    private sealed record ActivateBrakesCommand(int Value, int SwitchNumber) : GameCommand
    {
        public override string CommandId => $"Brakes.AssignBlue:{Value}";
        public override string DisplayName => $"Brakes: activate switch {SwitchNumber} with {Value}";
    }
}
