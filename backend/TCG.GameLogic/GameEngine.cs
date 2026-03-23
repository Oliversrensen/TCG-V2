using TCG.Core.Models;

namespace TCG.GameLogic;

public class GameEngine : IGameEngine
{
    private const int StartingLife = 20;
    private const int StartingHandSize = 5;

    public GameStateSnapshot CreateNewGame(string player1Id, Guid deck1Id, string player2Id, Guid deck2Id)
    {
        // Simplified: we don't load decks here - caller must provide initial hand/board
        // For a full implementation, inject a service to load deck contents
        return new GameStateSnapshot
        {
            MatchId = Guid.NewGuid(),
            CurrentTurn = 1,
            CurrentPlayerId = player1Id,
            Phase = GamePhase.Main,
            Players = new Dictionary<string, PlayerBoardState>
            {
                [player1Id] = new() { UserId = player1Id, LifeTotal = StartingLife, Hand = new(), Board = new() },
                [player2Id] = new() { UserId = player2Id, LifeTotal = StartingLife, Hand = new(), Board = new() },
            },
        };
    }

    public bool CanPlayCard(GameStateSnapshot state, string playerId, string instanceId, string? targetId)
    {
        if (state.CurrentPlayerId != playerId) return false;
        if (!state.Players.TryGetValue(playerId, out var player)) return false;
        if (!player.Hand.Contains(instanceId)) return false;
        if (!state.CardInstances.TryGetValue(instanceId, out var info)) return false;
        return info.CardType == CardType.Creature;
    }

    public GameStateSnapshot PlayCard(GameStateSnapshot state, string playerId, string instanceId, string? targetId)
    {
        if (!CanPlayCard(state, playerId, instanceId, targetId))
            throw new InvalidOperationException("Cannot play card");
        if (!state.CardInstances.TryGetValue(instanceId, out var info) || info.CardType != CardType.Creature)
            throw new InvalidOperationException("Only creatures can be played to the board");
        var next = CloneState(state);
        var player = next.Players[playerId];
        player.Hand.Remove(instanceId);
        player.Board.Add(new BoardCreature
        {
            InstanceId = instanceId,
            CardDefinitionId = info.CardDefinitionId,
            CurrentAttack = info.Attack,
            CurrentDefense = info.Defense,
            PlayedOnTurn = next.CurrentTurn
        });
        return next;
    }

    public bool CanAttack(GameStateSnapshot state, string playerId, string attackerInstanceId, string targetInstanceId)
    {
        if (state.CurrentPlayerId != playerId) return false;
        if (!state.Players.TryGetValue(playerId, out var player)) return false;
        var attacker = player.Board.FirstOrDefault(c => c.InstanceId == attackerInstanceId);
        if (attacker == null) return false;
        if (attacker.PlayedOnTurn >= state.CurrentTurn) return false; // Summoning sickness
        return true;
    }

    public GameStateSnapshot Attack(GameStateSnapshot state, string playerId, string attackerInstanceId, string targetInstanceId)
    {
        if (!CanAttack(state, playerId, attackerInstanceId, targetInstanceId))
            throw new InvalidOperationException("Cannot attack");
        var next = CloneState(state);
        var opponent = next.Players.Values.First(p => p.UserId != playerId);
        var target = opponent.Board.FirstOrDefault(c => c.InstanceId == targetInstanceId);
        if (target != null)
        {
            target.CurrentDefense -= next.Players[playerId].Board.First(c => c.InstanceId == attackerInstanceId).CurrentAttack;
            if (target.CurrentDefense <= 0)
                opponent.Board.Remove(target);
        }
        else
            opponent.LifeTotal -= next.Players[playerId].Board.First(c => c.InstanceId == attackerInstanceId).CurrentAttack;
        return next;
    }

    public GameStateSnapshot EndTurn(GameStateSnapshot state, string playerId)
    {
        if (state.CurrentPlayerId != playerId)
            throw new InvalidOperationException("Not your turn");
        var next = CloneState(state);
        var ids = next.Players.Keys.ToList();
        var idx = ids.IndexOf(playerId);
        next.CurrentPlayerId = ids[(idx + 1) % ids.Count];
        next.CurrentTurn += idx == ids.Count - 1 ? 1 : 0;
        next.Phase = GamePhase.Main;
        return next;
    }

    public string? GetWinner(GameStateSnapshot state)
    {
        foreach (var (_, p) in state.Players)
            if (p.LifeTotal <= 0)
                return state.Players.Values.First(x => x.UserId != p.UserId).UserId;
        return null;
    }

    private static GameStateSnapshot CloneState(GameStateSnapshot s)
    {
        return new GameStateSnapshot
        {
            MatchId = s.MatchId,
            CurrentTurn = s.CurrentTurn,
            CurrentPlayerId = s.CurrentPlayerId,
            Phase = s.Phase,
            CardInstances = new Dictionary<string, CardInstanceInfo>(s.CardInstances),
            Players = s.Players.ToDictionary(kv => kv.Key, kv => new PlayerBoardState
            {
                UserId = kv.Value.UserId,
                LifeTotal = kv.Value.LifeTotal,
                Hand = new List<string>(kv.Value.Hand),
                Board = kv.Value.Board.Select(c => new BoardCreature
                {
                    InstanceId = c.InstanceId,
                    CardDefinitionId = c.CardDefinitionId,
                    CurrentAttack = c.CurrentAttack,
                    CurrentDefense = c.CurrentDefense,
                    PlayedOnTurn = c.PlayedOnTurn,
                }).ToList(),
                Library = new List<string>(kv.Value.Library),
            }),
        };
    }
}
