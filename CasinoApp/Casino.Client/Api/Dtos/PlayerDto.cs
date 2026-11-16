namespace Client.Dtos;

public sealed record PlayerDto(Guid PlayerId, string Name, decimal Balance);