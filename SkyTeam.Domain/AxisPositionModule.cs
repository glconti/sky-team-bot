namespace SkyTeam.Domain;

sealed class AxisPositionModule : GameModule
{
    private const int LossLimit = 3;

    private int _axisPosition;
    private BlueDie? _blueDie;
    private OrangeDie? _orangeDie;

    internal int AxisPosition => _axisPosition;

    public override bool CanAcceptBlueDie(Player player) =>
        player == Player.Pilot && _blueDie is null;

    public override bool CanAcceptOrangeDie(Player player) =>
        player == Player.Copilot && _orangeDie is null;

    public override string GetModuleName() => "Axis Position";

    public override IEnumerable<GameCommand> GetAvailableCommands(
        Player currentPlayer,
        IReadOnlyList<BlueDie> unusedBlueDice,
        IReadOnlyList<OrangeDie> unusedOrangeDice)
    {
        if (currentPlayer == Player.Pilot && _blueDie is null)
        {
            foreach (var value in unusedBlueDice.Select(die => (int)die).Distinct().Order())
                yield return CreateBlueCommand(value);
        }

        if (currentPlayer == Player.Copilot && _orangeDie is null)
        {
            foreach (var value in unusedOrangeDice.Select(die => (int)die).Distinct().Order())
                yield return CreateOrangeCommand(value);
        }
    }

    public void AssignBlueDie(BlueDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_blueDie is not null)
            throw new InvalidOperationException("Blue die already assigned.");

        _blueDie = die;
        ResolveAxisIfReady();
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        ArgumentNullException.ThrowIfNull(die);

        if (_orangeDie is not null)
            throw new InvalidOperationException("Orange die already assigned.");

        _orangeDie = die;
        ResolveAxisIfReady();
    }

    public override void ResetRound()
    {
        _blueDie = null;
        _orangeDie = null;
    }

    private void ResolveAxisIfReady()
    {
        if (_blueDie is null || _orangeDie is null) return;

        var blueValue = (int)_blueDie;
        var orangeValue = (int)_orangeDie;
        var newAxisPosition = CalculateAxisPositionAfterResolution(blueValue, orangeValue);

        EnsureInBounds(newAxisPosition);
        _axisPosition = newAxisPosition;
    }

    private int CalculateAxisPositionAfterResolution(int blueValue, int orangeValue)
    {
        if (blueValue == orangeValue) return _axisPosition;

        var difference = Math.Abs(blueValue - orangeValue);
        return blueValue > orangeValue ? _axisPosition + difference : _axisPosition - difference;
    }

    private static void EnsureInBounds(int axisPosition)
    {
        if (axisPosition is > -LossLimit and < LossLimit) return;

        throw new InvalidOperationException("Axis position out of bounds.");
    }

    private GameCommand CreateBlueCommand(int value)
    {
        int? resulting = _orangeDie is null
            ? null
            : CalculateAxisPositionAfterResolution(value, (int)_orangeDie);

        return new AssignAxisBlueDieCommand(value, resulting);
    }

    private GameCommand CreateOrangeCommand(int value)
    {
        int? resulting = _blueDie is null
            ? null
            : CalculateAxisPositionAfterResolution((int)_blueDie, value);

        return new AssignAxisOrangeDieCommand(value, resulting);
    }

    private sealed record AssignAxisBlueDieCommand(int Value, int? ResultingAxisPosition) : GameCommand
    {
        public override string CommandId => $"Axis.AssignBlue:{Value}";

        public override string DisplayName => ResultingAxisPosition is null
            ? $"Axis: place blue {Value}"
            : $"Axis: place blue {Value} (axis -> {ResultingAxisPosition})";
    }

    private sealed record AssignAxisOrangeDieCommand(int Value, int? ResultingAxisPosition) : GameCommand
    {
        public override string CommandId => $"Axis.AssignOrange:{Value}";

        public override string DisplayName => ResultingAxisPosition is null
            ? $"Axis: place orange {Value}"
            : $"Axis: place orange {Value} (axis -> {ResultingAxisPosition})";
    }
}
