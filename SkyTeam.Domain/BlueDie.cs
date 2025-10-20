namespace SkyTeam.Domain;

class BlueDie : Die
{
    private BlueDie(int value) : base(value)
    {
    }

    public new static BlueDie Roll() => new(Die.Roll());
}