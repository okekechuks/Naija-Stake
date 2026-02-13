using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Services;

public interface IWalletService
{
    Task<Wallet> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
