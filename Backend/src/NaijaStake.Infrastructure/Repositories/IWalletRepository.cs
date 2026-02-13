using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Repositories;

/// <summary>
/// Wallet repository interface for wallet-specific operations.
/// </summary>
public interface IWalletRepository : IRepository<Wallet, Guid>
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Wallet?> GetWithTransactionsAsync(Guid walletId, CancellationToken cancellationToken = default);
}
