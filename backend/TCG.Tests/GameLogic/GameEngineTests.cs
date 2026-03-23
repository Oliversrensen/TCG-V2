using FluentAssertions;
using TCG.Core.Models;
using TCG.GameLogic;
using Xunit;

namespace TCG.Tests.GameLogic;

public class GameEngineTests
{
    private readonly GameEngine _engine = new();
    private const string Player1 = "player-1";
    private const string Player2 = "player-2";
    private static readonly Guid Deck1 = Guid.NewGuid();
    private static readonly Guid Deck2 = Guid.NewGuid();

    [Fact]
    public void CreateNewGame_creates_state_with_two_players_at_20_life()
    {
        var state = _engine.CreateNewGame(Player1, Deck1, Player2, Deck2);

        state.Players.Should().HaveCount(2);
        state.Players[Player1].LifeTotal.Should().Be(20);
        state.Players[Player2].LifeTotal.Should().Be(20);
        state.CurrentPlayerId.Should().Be(Player1);
        state.Phase.Should().Be(GamePhase.Main);
        state.CurrentTurn.Should().Be(1);
    }

    [Fact]
    public void CanPlayCard_returns_false_when_not_your_turn()
    {
        var state = CreateBasicStateWithHandCreature("inst-1", out _);

        _engine.CanPlayCard(state, Player2, "inst-1", null).Should().BeFalse();
    }

    [Fact]
    public void CanPlayCard_returns_false_when_card_not_in_hand()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player1;

        _engine.CanPlayCard(state, Player1, "nonexistent", null).Should().BeFalse();
    }

    [Fact]
    public void CanPlayCard_returns_true_when_valid()
    {
        var state = CreateBasicStateWithHandCreature("inst-1", out _);
        state.CurrentPlayerId = Player1;

        _engine.CanPlayCard(state, Player1, "inst-1", null).Should().BeTrue();
    }

    [Fact]
    public void PlayCard_throws_when_invalid()
    {
        var state = CreateBasicStateWithHandCreature("inst-1", out _);
        state.CurrentPlayerId = Player1;

        var act = () => _engine.PlayCard(state, Player2, "inst-1", null);

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot play card");
    }

    [Fact]
    public void PlayCard_moves_creature_from_hand_to_board()
    {
        var state = CreateBasicStateWithHandCreature("inst-1", out var cardDefId);
        state.CurrentPlayerId = Player1;

        var next = _engine.PlayCard(state, Player1, "inst-1", null);

        next.Players[Player1].Hand.Should().NotContain("inst-1");
        next.Players[Player1].Board.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { InstanceId = "inst-1", CardDefinitionId = cardDefId, CurrentAttack = 2, CurrentDefense = 3, PlayedOnTurn = 1 });
    }

    [Fact]
    public void Attack_reduces_opponent_life_when_no_blocker()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player1;
        state.Players[Player1].Board.Add(new BoardCreature
        {
            InstanceId = "attacker-1",
            CardDefinitionId = Guid.NewGuid(),
            CurrentAttack = 3,
            CurrentDefense = 2,
            PlayedOnTurn = 0
        });

        var next = _engine.Attack(state, Player1, "attacker-1", "nonexistent-target");

        next.Players[Player2].LifeTotal.Should().Be(17); // 20 - 3
    }

    [Fact]
    public void Attack_removes_creature_when_defense_reaches_zero()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player1;
        state.Players[Player1].Board.Add(new BoardCreature
        {
            InstanceId = "attacker-1",
            CardDefinitionId = Guid.NewGuid(),
            CurrentAttack = 3,
            CurrentDefense = 2,
            PlayedOnTurn = 0
        });
        state.Players[Player2].Board.Add(new BoardCreature
        {
            InstanceId = "blocker-1",
            CardDefinitionId = Guid.NewGuid(),
            CurrentAttack = 1,
            CurrentDefense = 2,
            PlayedOnTurn = 1
        });

        var next = _engine.Attack(state, Player1, "attacker-1", "blocker-1");

        next.Players[Player2].Board.Should().BeEmpty();
        next.Players[Player2].LifeTotal.Should().Be(20); // no damage to life
    }

    [Fact]
    public void CanAttack_returns_false_when_summoning_sickness()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player1;
        state.Players[Player1].Board.Add(new BoardCreature
        {
            InstanceId = "attacker-1",
            CardDefinitionId = Guid.NewGuid(),
            CurrentAttack = 3,
            CurrentDefense = 2,
            PlayedOnTurn = 1
        });

        _engine.CanAttack(state, Player1, "attacker-1", "face").Should().BeFalse();
    }

    [Fact]
    public void EndTurn_advances_current_player()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player1;

        var next = _engine.EndTurn(state, Player1);

        next.CurrentPlayerId.Should().Be(Player2);
    }

    [Fact]
    public void EndTurn_increments_turn_when_second_player_ends()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player2;

        var next = _engine.EndTurn(state, Player2);

        next.CurrentTurn.Should().Be(2);
        next.CurrentPlayerId.Should().Be(Player1);
    }

    [Fact]
    public void EndTurn_throws_when_not_your_turn()
    {
        var state = CreateBasicState();
        state.CurrentPlayerId = Player1;

        var act = () => _engine.EndTurn(state, Player2);

        act.Should().Throw<InvalidOperationException>().WithMessage("Not your turn");
    }

    [Fact]
    public void GetWinner_returns_opponent_when_player_life_zero()
    {
        var state = CreateBasicState();
        state.Players[Player1].LifeTotal = 0;

        _engine.GetWinner(state).Should().Be(Player2);
    }

    [Fact]
    public void GetWinner_returns_null_when_no_one_dead()
    {
        var state = CreateBasicState();

        _engine.GetWinner(state).Should().BeNull();
    }

    private static GameStateSnapshot CreateBasicState()
    {
        return new GameStateSnapshot
        {
            MatchId = Guid.NewGuid(),
            CurrentTurn = 1,
            CurrentPlayerId = Player1,
            Phase = GamePhase.Main,
            CardInstances = new Dictionary<string, CardInstanceInfo>(),
            Players = new Dictionary<string, PlayerBoardState>
            {
                [Player1] = new() { UserId = Player1, LifeTotal = 20, Hand = new(), Board = new(), Library = new() },
                [Player2] = new() { UserId = Player2, LifeTotal = 20, Hand = new(), Board = new(), Library = new() }
            }
        };
    }

    private static GameStateSnapshot CreateBasicStateWithHandCreature(string instanceId, out Guid cardDefId)
    {
        cardDefId = Guid.NewGuid();
        var state = CreateBasicState();
        state.Players[Player1].Hand.Add(instanceId);
        state.CardInstances[instanceId] = new CardInstanceInfo
        {
            CardDefinitionId = cardDefId,
            Attack = 2,
            Defense = 3,
            CardType = CardType.Creature
        };
        return state;
    }
}
