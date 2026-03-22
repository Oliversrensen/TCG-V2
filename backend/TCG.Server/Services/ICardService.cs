using TCG.Core.DTOs;

namespace TCG.Server.Services;

public interface ICardService
{
    Task<IReadOnlyList<CardDefinitionDto>> GetCardDefinitionsAsync(CancellationToken ct = default);
}
