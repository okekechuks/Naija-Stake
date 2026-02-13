namespace NaijaStake.API.DTOs;

/// <summary>
/// DTO for wallet balance response.
/// </summary>
public class WalletDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal LockedBalance { get; set; }
    public decimal TotalBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for transaction response.
/// </summary>
public class TransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public Guid? BetId { get; set; }
    public Guid? StakeId { get; set; }
}

/// <summary>
/// DTO for transaction history response.
/// </summary>
public class TransactionHistoryDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// DTO for deposit request.
/// </summary>
public class DepositDto
{
    public decimal Amount { get; set; }
    public string PaymentMethodId { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
}
