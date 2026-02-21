namespace SkyTeam.Domain;

/// <summary>
/// Exception type used to represent a game loss that occurs due to rules (not API misuse).
/// </summary>
sealed class GameRuleLossException(string message) : InvalidOperationException(message);
