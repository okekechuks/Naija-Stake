using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;
using NaijaStake.Infrastructure.Repositories;

namespace NaijaStake.Infrastructure.Services;

public class StakeService : IStakeService
{
    private readonly IStakeRepository _stakeRepository;
    private readonly IBetRepository _betRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public StakeService(
        IStakeRepository stakeRepository,
        IBetRepository betRepository,
        IUserRepository userRepository,
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _stakeRepository = stakeRepository ?? throw new ArgumentNullException(nameof(stakeRepository));
        _betRepository = betRepository ?? throw new ArgumentNullException(nameof(betRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public async Task<Stake> PlaceStakeAsync(Guid userId, Guid betId, Guid outcomeId, Money stakeAmount, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (stakeAmount == null)
            throw new ArgumentNullException(nameof(stakeAmount));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        var existingStake = await _stakeRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingStake != null)
        {
            var isSameRequest =
                existingStake.UserId == userId &&
                existingStake.BetId == betId &&
                existingStake.OutcomeId == outcomeId &&
                existingStake.StakeAmount == stakeAmount;

            if (!isSameRequest)
                throw new InvalidOperationException($"Stake with idempotency key '{idempotencyKey}' already exists for a different request.");

            return existingStake;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new ArgumentException("User not found", nameof(userId));

        var bet = await _betRepository.GetWithOutcomesAsync(betId, cancellationToken);
        if (bet == null)
            throw new ArgumentException("Bet not found", nameof(betId));

        if (!bet.IsOpen)
            throw new InvalidOperationException("Bet is not open for staking.");

        var outcome = bet.Outcomes.FirstOrDefault(o => o.Id == outcomeId);
        if (outcome == null)
            throw new ArgumentException("Outcome does not belong to the specified bet.", nameof(outcomeId));

        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet == null)
            throw new InvalidOperationException("Wallet not found for user.");

        if (!wallet.CanAfford(stakeAmount))
            throw new InvalidOperationException("Insufficient available balance to place stake.");

        var stake = Stake.Create(userId, betId, outcomeId, stakeAmount, idempotencyKey);

        wallet.RecordStakeLocked(stakeAmount, idempotencyKey);
        bet.AddStake(stakeAmount);
        outcome.RecordStake(stakeAmount);

        var transaction = Transaction.Create(
            wallet.Id,
            userId,
            TransactionType.StakeLocked,
            stakeAmount,
            $"Locked funds for stake on bet '{bet.Title}'",
            betId: betId,
            stakeId: stake.Id,
            idempotencyKey: idempotencyKey);

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _stakeRepository.AddAsync(stake, cancellationToken);
        await _stakeRepository.SaveChangesAsync(cancellationToken);
        return stake;
    }
}
