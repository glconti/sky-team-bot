namespace SkyTeam.Application.Round;

public readonly record struct DieValue
{
    public int Value { get; }

    public DieValue(int value)
    {
        if (value is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(value), "Die values must be between 1 and 6.");

        Value = value;
    }

    public static implicit operator int(DieValue die) => die.Value;
}
