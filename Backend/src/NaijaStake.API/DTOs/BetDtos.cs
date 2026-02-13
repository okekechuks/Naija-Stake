using NaijaStake.Domain.Entities;

namespace NaijaStake.API.DTOs;

/// <summary>
/// DTO for creating a new bet (admin/creator only).
/// </summary>
public class CreateBetDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public BetCategory Category { get; set; }
    public DateTime ClosingTime { get; set; }
    public DateTime ResolutionTime { get; set; }
    public List<string> OutcomeOptions { get; set; } = new();
}

/// <summary>
/// DTO for bet response (read-only).
/// </summary>
public class BetDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime ClosingTime { get; set; }
    public DateTime ResolutionTime { get; set; }
    public List<OutcomeDto> Outcomes { get; set; } = new();
    public decimal TotalStaked { get; set; }
    public int ParticipantCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for bet detail response (includes all info).
/// </summary>
public class BetDetailDto : BetDto
{
    public Guid? ResolvedOutcomeId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// DTO for outcome response.
/// </summary>
public class OutcomeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal TotalStaked { get; set; }
    public int StakeCount { get; set; }
}

/// <summary>
/// DTO for paginated bets response.
/// </summary>
public class BetsPageDto
{
    public List<BetDto> Bets { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// DTO to resolve a bet (admin only).
/// </summary>
public class ResolveBetDto
{
    public Guid WinningOutcomeId { get; set; }
    public string? Notes { get; set; }
    public string IdempotencyKey { get; set; } = null!;
}
