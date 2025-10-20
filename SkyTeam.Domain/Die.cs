namespace SkyTeam.Domain;

class Die
{
    private static readonly Random Random = new();

    private Die(int value) => Value = value;

    private int Value { get; }

    public static Die Roll() => new(Random.Next(1, 7)); // Returns 1-6
    public static implicit operator int(Die die) => die.Value;
}

class BlueDie
{
    private readonly Die _die;

    private BlueDie() => _die = Die.Roll();

    public static BlueDie Roll() => new();

    public static implicit operator int(BlueDie die) => die._die;
}

class OrangeDie
{
    private readonly Die _die;

    private OrangeDie() => _die = Die.Roll();

    public static OrangeDie Roll() => new();

    public static implicit operator int(OrangeDie die) => die._die;
}