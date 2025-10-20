namespace SkyTeam.Domain;

class AxisPositionModule : GameModule
{
    private short _axisPosition;
    private BlueDie? _blueDie;
    private OrangeDie? _orangeDie;

    public void AssignBlueDie(BlueDie die)
    {
        if (_blueDie is not null)
            throw new InvalidOperationException("Blue die already assigned.");

        _blueDie = die;
        UpdateAxisPosition();
    }

    public void AssignOrangeDie(OrangeDie die)
    {
        if (_orangeDie is not null)
            throw new InvalidOperationException("Orange die already assigned.");

        _orangeDie = die;
        UpdateAxisPosition();
    }

    public void Reset()
    {
        _blueDie = null;
        _orangeDie = null;
    }

    private void UpdateAxisPosition()
    {
        if (_blueDie is null || _orangeDie is null) return;

        // Example logic: each die contributes a fixed value to the axis position
        _axisPosition += (short)Math.Abs(_blueDie - _orangeDie);

        if (_axisPosition is > -3 and < 3) return;

        throw new InvalidOperationException("Axis position out of bounds.");
    }
}