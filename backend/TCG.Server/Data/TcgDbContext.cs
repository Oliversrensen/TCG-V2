using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;

namespace TCG.Server.Data;

public class TcgDbContext : DbContext
{
    public TcgDbContext(DbContextOptions<TcgDbContext> options) : base(options) { }

    public DbSet<CardDefinition> CardDefinitions => Set<CardDefinition>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckSlot> DeckSlots => Set<DeckSlot>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Deck>()
            .HasIndex(d => d.UserId);

        modelBuilder.Entity<UserProfile>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<DeckSlot>()
            .HasOne(ds => ds.Deck)
            .WithMany(d => d.Slots)
            .HasForeignKey(ds => ds.DeckId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeckSlot>()
            .HasOne(ds => ds.CardDefinition)
            .WithMany()
            .HasForeignKey(ds => ds.CardDefinitionId);

        modelBuilder.Entity<MatchParticipant>()
            .HasOne(mp => mp.Match)
            .WithMany(m => m.Participants)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
