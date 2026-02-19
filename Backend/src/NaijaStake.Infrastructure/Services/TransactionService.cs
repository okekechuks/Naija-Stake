using NaijaStake.Domain.Entities;
using NaijaStake.Domain.ValueObjects;
using NaijaStake.Infrastructure.Repositories;

namespace NaijaStake.Infrastructure.Services;

/// <summary>
/// Service for managing transactions.
/// Transactions are immutable records of all wallet balance changes.
/// This service provides validation and business logic around transaction creation.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWalletRepository _walletRepository;

    public TransactionService(ITransactionRepository transactionRepository, IWalletRepository walletRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
    }

    public async Task<Transaction> CreateAsync(
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
        CancellationToken cancellationToken = default)
    {
        // Validate wallet exists
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet == null)
            throw new ArgumentException("Wallet not found", nameof(walletId));

        // Validate wallet belongs to user
        if (wallet.UserId != userId)
            throw new ArgumentException("Wallet does not belong to the specified user", nameof(userId));

        // Check for duplicate idempotency key
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _transactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException($"Transaction with idempotency key '{idempotencyKey}' already exists");
        }

        // Validate amount is positive
        if (amount <= 0)
            throw new ArgumentException("Transaction amount must be greater than zero", nameof(amount));

        // Create transaction using domain factory
        var moneyAmount = Money.From(amount);
        var transaction = Transaction.Create(
            walletId,
            userId,
            type,
            moneyAmount,
            description,
            betId,
            stakeId,
            paymentId,
            idempotencyKey,
            metadata);

        // Save transaction
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        return transaction;
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _transactionRepository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<Transaction>> GetByWalletIdAsync(Guid walletId, int limit = 100, CancellationToken cancellationToken = default)
        => _transactionRepository.GetByWalletIdAsync(walletId, limit, cancellationToken);

    public Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default)
        => _transactionRepository.GetByUserIdAsync(userId, limit, cancellationToken);

    public Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => _transactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
}
