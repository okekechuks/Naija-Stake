using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Repositories;

namespace NaijaStake.Infrastructure.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUserRepository _userRepository;

    public WalletService(IWalletRepository walletRepository, IUserRepository userRepository)
    {
        _walletRepository = walletRepository;
        _userRepository = userRepository;
    }

    public async Task<Wallet> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new ArgumentException("User not found", nameof(userId));

        var wallet = Wallet.Create(userId);
        await _walletRepository.AddAsync(wallet, cancellationToken);
        await _walletRepository.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _walletRepository.GetByUserIdAsync(userId, cancellationToken);
}
