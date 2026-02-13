using NaijaStake.Domain.ValueObjects;

namespace NaijaStake.Domain.Entities;

/// <summary>
/// Outcome entity represents one possible resolution option for a bet.
/// Example: "Arsenal wins", "Draw", "Chelsea wins" for a sports bet.
/// </summary>
public class Outcome
{
    public Guid Id { get; private set; }
    public Guid BetId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    
    // Financial tracking per outcome
    public Money TotalStaked { get; private set; } = Money.Zero;
    public int StakeCount { get; private set; } = 0;
    
    public DateTime CreatedAt { get; private set; }

    private Outcome() { }

    /// <summary>
    /// Factory method to create an outcome for a bet.
    /// </summary>
    public static Outcome Create(Guid betId, string title)
    {
        if (betId == Guid.Empty)
            throw new ArgumentException("Bet ID is required.", nameof(betId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Outcome title is required.", nameof(title));

        return new Outcome
        {
            Id = Guid.NewGuid(),
            BetId = betId,
            Title = title.Trim(),
            TotalStaked = Money.Zero,
            StakeCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Records a stake placed on this outcome.
    /// </summary>
    public void RecordStake(Money stakeAmount)
    {
        if (stakeAmount == null)
            throw new ArgumentNullException(nameof(stakeAmount));

        TotalStaked = TotalStaked.Add(stakeAmount);
        StakeCount++;
    }
}
