using Microsoft.EntityFrameworkCore;
using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Data;

namespace NaijaStake.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation providing base CRUD operations.
/// </summary>
public abstract class Repository<T, TId> : IRepository<T, TId> where T : class
{
    protected readonly StakeItDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected Repository(StakeItDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object?[] { id }, cancellationToken: cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Update(entity);
        return entity;
    }

    public virtual async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// User repository implementation.
/// </summary>
public class UserRepository : Repository<User, Guid>, IUserRepository
{
    public UserRepository(StakeItDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        return await _dbSet
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return await _dbSet
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetWithWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
}

/// <summary>
/// Wallet repository implementation.
/// </summary>
public class WalletRepository : Repository<Wallet, Guid>, IWalletRepository
{
    public WalletRepository(StakeItDbContext context) : base(context) { }

    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<Wallet?> GetWithTransactionsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);
    }
}

/// <summary>
/// Transaction repository implementation.
/// Transactions are immutable, so no update operations.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly StakeItDbContext _context;
    private readonly DbSet<Transaction> _dbSet;

    public TransactionRepository(StakeItDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<Transaction>();
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        await _dbSet.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByWalletIdAsync(Guid walletId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Bet repository implementation.
/// </summary>
public class BetRepository : Repository<Bet, Guid>, IBetRepository
{
    public BetRepository(StakeItDbContext context) : base(context) { }

    public async Task<IEnumerable<Bet>> GetByCategoryAsync(BetCategory category, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Category == category)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Bet>> GetOpenBetsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Status == BetStatus.Open && b.ClosingTime > DateTime.UtcNow)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Bet>> GetClosedBetsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Status == BetStatus.Closed || b.Status == BetStatus.Resolved)
            .OrderByDescending(b => b.ResolvedAt ?? b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Bet?> GetWithOutcomesAsync(Guid betId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Outcomes)
            .FirstOrDefaultAsync(b => b.Id == betId, cancellationToken);
    }

    public async Task<Bet?> GetWithStakesAsync(Guid betId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Stakes)
            .FirstOrDefaultAsync(b => b.Id == betId, cancellationToken);
    }
}

/// <summary>
/// Stake repository implementation.
/// </summary>
public class StakeRepository : Repository<Stake, Guid>, IStakeRepository
{
    public StakeRepository(StakeItDbContext context) : base(context) { }

    public async Task<IEnumerable<Stake>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Stake>> GetByBetIdAsync(Guid betId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.BetId == betId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Stake>> GetByOutcomeIdAsync(Guid outcomeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.OutcomeId == outcomeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Stake?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IEnumerable<Stake>> GetActiveStakesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.Status == StakeStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Stake>> GetByStatusAsync(StakeStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Status == status)
            .ToListAsync(cancellationToken);
    }
}
