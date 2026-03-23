using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;
using TCG.Server.Data;

namespace TCG.Server.Services;

public class GameSetupService : IGameSetupService
{
    private const int StartingLife = 20;
    private const int StartingHandSize = 5;
    private readonly TcgDbContext _db;
    private static readonly Random Rng = new();

    public GameSetupService(TcgDbContext db) => _db = db;

    public async Task<GameStateSnapshot> CreateInitialStateAsync(string player1Id, Guid deck1Id, string player2Id, Guid deck2Id, Guid matchId, CancellationToken ct = default)
    {
        var (p1Hand, p1Library, p2Hand, p2Library, cardInstances) = await BuildAndDealDecksAsync(deck1Id, deck2Id, ct);

        var state = new GameStateSnapshot
        {
            MatchId = matchId,
            CurrentTurn = 1,
            CurrentPlayerId = player1Id,
            Phase = GamePhase.Main,
            CardInstances = cardInstances,
            Players = new Dictionary<string, PlayerBoardState>
            {
                [player1Id] = new()
                {
                    UserId = player1Id,
                    LifeTotal = StartingLife,
                    Hand = p1Hand,
                    Board = new(),
                    Library = p1Library
                },
                [player2Id] = new()
                {
                    UserId = player2Id,
                    LifeTotal = StartingLife,
                    Hand = p2Hand,
                    Board = new(),
                    Library = p2Library
                }
            }
        };
        return state;
    }

    private async Task<(List<string> p1Hand, List<string> p1Library, List<string> p2Hand, List<string> p2Library, Dictionary<string, CardInstanceInfo> cardInstances)> BuildAndDealDecksAsync(Guid deck1Id, Guid deck2Id, CancellationToken ct)
    {
        var cardInstances = new Dictionary<string, CardInstanceInfo>();

        var deck1 = await _db.Decks
            .AsNoTracking()
            .Include(d => d.Slots)
            .ThenInclude(s => s.CardDefinition)
            .FirstOrDefaultAsync(d => d.Id == deck1Id, ct)
            ?? throw new ArgumentException($"Deck {deck1Id} not found");

        var deck2 = await _db.Decks
            .AsNoTracking()
            .Include(d => d.Slots)
            .ThenInclude(s => s.CardDefinition)
            .FirstOrDefaultAsync(d => d.Id == deck2Id, ct)
            ?? throw new ArgumentException($"Deck {deck2Id} not found");

        var lib1 = BuildLibrary(deck1.Slots, cardInstances);
        var lib2 = BuildLibrary(deck2.Slots, cardInstances);

        Shuffle(lib1);
        Shuffle(lib2);

        var p1Hand = lib1.Take(StartingHandSize).ToList();
        var p1Library = lib1.Skip(StartingHandSize).ToList();
        var p2Hand = lib2.Take(StartingHandSize).ToList();
        var p2Library = lib2.Skip(StartingHandSize).ToList();

        return (p1Hand, p1Library, p2Hand, p2Library, cardInstances);
    }

    private static List<string> BuildLibrary(IEnumerable<DeckSlot> slots, Dictionary<string, CardInstanceInfo> cardInstances)
    {
        var library = new List<string>();
        foreach (var slot in slots)
        {
            var def = slot.CardDefinition;
            for (var i = 0; i < slot.Quantity; i++)
            {
                var instanceId = Guid.NewGuid().ToString();
                library.Add(instanceId);
                cardInstances[instanceId] = new CardInstanceInfo
                {
                    CardDefinitionId = def.Id,
                    Attack = def.Attack ?? 0,
                    Defense = def.Defense ?? 0,
                    CardType = def.CardType
                };
            }
        }
        return library;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        for (var i = n - 1; i > 0; i--)
        {
            var j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
