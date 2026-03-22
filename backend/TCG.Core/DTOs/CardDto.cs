namespace TCG.Core.DTOs;

public record CardDefinitionDto(
    Guid Id,
    string Name,
    string Description,
    int Cost,
    int? Attack,
    int? Defense,
    string CardType
);
