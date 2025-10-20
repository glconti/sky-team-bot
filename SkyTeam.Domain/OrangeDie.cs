namespace SkyTeam.Domain;

class OrangeDie : Die
{
    private OrangeDie(int value) : base(value)
    {
    }

    public new static OrangeDie Roll() => new(Die.Roll());
}