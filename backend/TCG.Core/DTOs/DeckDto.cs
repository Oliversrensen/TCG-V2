namespace TCG.Core.DTOs;

public record DeckDto(
    Guid Id,
    string Name,
    List<DeckSlotDto> Slots,
    DateTime CreatedAt
);

public record DeckSlotDto(Guid CardDefinitionId, int Quantity);

public record CreateDeckRequest(string Name, List<DeckSlotDto> Slots);

public record UpdateDeckRequest(string Name, List<DeckSlotDto> Slots);
