namespace Client.Dtos;

public sealed record RoundDto(Guid RoundId, string Status, decimal Bet, decimal Win, decimal? Balance);