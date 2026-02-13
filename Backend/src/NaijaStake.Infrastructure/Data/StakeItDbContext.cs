using Microsoft.EntityFrameworkCore;
using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the StakeIt platform.
/// All database operations flow through this context.
/// </summary>
public class StakeItDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Wallet> Wallets { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Bet> Bets { get; set; } = null!;
    public DbSet<Outcome> Outcomes { get; set; } = null!;
    public DbSet<Stake> Stakes { get; set; } = null!;

    public StakeItDbContext(DbContextOptions<StakeItDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Unique index on email
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            
            // Relationships
            // One-to-one: User has one Wallet, Wallet has one User
            entity.HasOne(e => e.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallet>(w => w.UserId)
                .IsRequired();

            entity.HasMany(e => e.Stakes)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Transactions)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Wallet entity configuration
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            
            // Decimal precision: 18,2 allows amounts up to 9,999,999,999,999,999.99
            entity.Property(e => e.AvailableBalance)
                .HasConversion(
                    v => v.Amount,
                    v => Money.From(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.LockedBalance)
                .HasConversion(
                    v => v.Amount,
                    v => Money.From(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt).IsRequired();

            // Relationships
            entity.HasMany(e => e.Transactions)
                .WithOne()
                .HasForeignKey(t => t.WalletId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: one wallet per user
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Transaction entity configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WalletId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Type).IsRequired().HasConversion<int>();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            
            entity.Property(e => e.Amount)
                .HasConversion(
                    v => v.Amount,
                    v => Money.From(v))
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasMaxLength(255);

            // Indices for common queries
            entity.HasIndex(e => e.WalletId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
            
            // Unique idempotency key to prevent duplicate transactions
            entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

            // Foreign keys
            entity.HasIndex(e => e.BetId).HasFilter("[BetId] IS NOT NULL");
            entity.HasIndex(e => e.StakeId).HasFilter("[StakeId] IS NOT NULL");
        });

        // Bet entity configuration
        modelBuilder.Entity<Bet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Category).IsRequired().HasConversion<int>();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            
            entity.Property(e => e.TotalStaked)
                .HasConversion(
                    v => v.Amount,
                    v => Money.From(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.ClosingTime).IsRequired();
            entity.Property(e => e.ResolutionTime).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ResolvedAt);
            entity.Property(e => e.ResolutionIdempotencyKey).HasMaxLength(255);

            // Indices
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.ClosingTime);
            entity.HasIndex(e => e.CreatedAt);
            
            // Unique idempotency key for resolution
            entity.HasIndex(e => e.ResolutionIdempotencyKey).IsUnique().HasFilter("[ResolutionIdempotencyKey] IS NOT NULL");

            // Relationships
            entity.HasMany(e => e.Outcomes)
                .WithOne()
                .HasForeignKey(o => o.BetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Stakes)
                .WithOne(s => s.Bet)
                .HasForeignKey(s => s.BetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Outcome entity configuration
        modelBuilder.Entity<Outcome>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BetId).IsRequired();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            
            entity.Property(e => e.TotalStaked)
                .HasConversion(
                    v => v.Amount,
                    v => Money.From(v))
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt).IsRequired();

            // Index for queries by bet
            entity.HasIndex(e => e.BetId);
        });

        // Stake entity configuration
        modelBuilder.Entity<Stake>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.BetId).IsRequired();
            entity.Property(e => e.OutcomeId).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            
            entity.Property(e => e.StakeAmount)
                .HasConversion(
                    v => v.Amount,
                    v => Money.From(v))
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.ActualPayout)
                .HasConversion(
                    v => v != null ? v.Amount : (decimal?)null,
                    v => v.HasValue ? Money.From(v.Value) : null)
                .HasPrecision(18, 2);

            entity.Property(e => e.PotentialPayout)
                .HasConversion(
                    v => v != null ? v.Amount : (decimal?)null,
                    v => v.HasValue ? Money.From(v.Value) : null)
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(255);

            // Indices for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BetId);
            entity.HasIndex(e => e.OutcomeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            // Unique idempotency key to prevent duplicate stakes
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Stakes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Bet)
                .WithMany(b => b.Stakes)
                .HasForeignKey(e => e.BetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Outcome)
                .WithMany()
                .HasForeignKey(e => e.OutcomeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
