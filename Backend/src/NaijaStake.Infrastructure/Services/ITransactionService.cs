using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Services;

/// <summary>
/// Service for managing transactions.
/// Transactions are immutable records of all wallet balance changes.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Creates and saves a new transaction.
    /// Validates that the transaction is properly formed before saving.
    /// </summary>
    Task<Transaction> CreateAsync(
        Guid walletId,
        Guid userId,
        TransactionType type,
        decimal amount,
        string description,
        Guid? betId = null,
        Guid? stakeId = null,
        Guid? paymentId = null,
        string? idempotencyKey = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by its ID.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for a specific wallet, ordered by most recent first.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByWalletIdAsync(Guid walletId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for a specific user, ordered by most recent first.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by its idempotency key.
    /// Used to prevent duplicate processing of the same request.
    /// </summary>
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
