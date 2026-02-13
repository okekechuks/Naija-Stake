using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Repositories;

/// <summary>
/// Transaction repository interface for transaction queries.
/// Transactions are immutable, so no update operations.
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByWalletIdAsync(Guid walletId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
