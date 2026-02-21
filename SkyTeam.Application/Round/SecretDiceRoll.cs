namespace SkyTeam.Application.Round;

public sealed record SecretDiceRoll(IReadOnlyList<int> PilotDice, IReadOnlyList<int> CopilotDice);
