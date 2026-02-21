namespace SkyTeam.Application.Round;

public enum PlayerSeat
{
    Pilot,
    Copilot
}

public static class PlayerSeatExtensions
{
    public static PlayerSeat Other(this PlayerSeat seat) => seat == PlayerSeat.Pilot
        ? PlayerSeat.Copilot
        : PlayerSeat.Pilot;
}
