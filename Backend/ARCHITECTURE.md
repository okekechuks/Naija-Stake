# Architecture Guide

## Project Structure

### Domain Layer (NaijaStake.Domain)
**Responsibility**: Business logic and rules
**Contents**:
- **Entities**: User, Wallet, Bet, Stake, Outcome, Transaction
- **Value Objects**: Money
- **Constants**: Application-wide constants
- **Exceptions**: Domain-specific exceptions

**Key Principle**: No external dependencies. Pure C# classes representing business concepts.

### Infrastructure Layer (NaijaStake.Infrastructure)
**Responsibility**: Data access and external services
**Contents**:
- **Data**: DbContext, configurations
- **Repositories**: Database access implementations

**Key Principle**: Implements abstractions defined by domain. Handles all database operations.

### API Layer (NaijaStake.API)
**Responsibility**: HTTP endpoints and request/response handling
**Contents**:
- **Controllers**: API endpoints
- **DTOs**: Request/response models
- **Middleware**: Logging, error handling, etc.

**Key Principle**: Controllers are thin. All business logic delegated to domain entities and services.

## Data Flow for Stake Placement

```
Frontend Request (PlaceStakeDto)
    ↓
Controller: ValidateRequest → Acquire Redis Lock
    ↓
Service: ValidateBetOpen → ValidateBalance → ValidateOutcome
    ↓
Repository: Get Wallet, Get Bet, Get Outcome
    ↓
Domain: Wallet.RecordStakeLocked() → Bet.AddStake() → Outcome.RecordStake()
    ↓
Repository: Create Transaction + Create Stake + Update Wallet/Bet/Outcome
    ↓
Commit Transaction (atomic)
    ↓
Release Redis Lock
    ↓
Return Response with New Balance
```

## Money Handling Rules

1. **Never use `decimal` directly** - Use `Money` value object
2. **Always create Transaction records** - Every balance change must be recorded
3. **Use decimal precision 18,2** - Allows up to 9,999,999,999,999,999.99
4. **Never allow negative balances** - Enforced in Money constructor
5. **Calculate payouts in backend** - Never in frontend
6. **Use immutable operations** - Money operations return new Money instances

### Example

```csharp
// WRONG - Don't do this
var newBalance = wallet.AvailableBalance + stakeAmount;

// RIGHT - Use Money value object
var newBalance = wallet.AvailableBalance.Add(Money.From(stakeAmount));
```

## Concurrency Control

### Problem
Multiple requests could cause:
- Double spending (user stakes more than balance)
- Double resolution (bet resolved twice)

### Solution
Use Redis distributed locks:

```csharp
// Acquire lock
var lockKey = $"stake_lock:{userId}";
if (!await redisService.AcquireLock(lockKey, duration: 5))
    throw new ConcurrencyException("Could not acquire lock");

try
{
    // Critical section
    await PlaceStakeAsync(stake);
}
finally
{
    // Always release
    await redisService.ReleaseLock(lockKey);
}
```

## Idempotency

### Why?
Network failures can cause client to retry the same request multiple times.

### How?
Create unique idempotency key on client:
```csharp
var idempotencyKey = Guid.NewGuid().ToString(); // Client generates
await api.PlaceStake(stake, idempotencyKey);
```

If request fails and is retried with same key:
1. Check if transaction with this key already exists
2. If yes, return existing result (safe replay)
3. If no, process normally

Enforced by unique constraint in database:
```csharp
entity.HasIndex(e => e.IdempotencyKey).IsUnique();
```

## Testing Strategy

### Unit Tests (Domain)
Test entities in isolation, no database:
```csharp
[Fact]
public void Wallet_CanLockStake_WhenBalanceSufficient()
{
    var wallet = Wallet.Create(userId);
    wallet.RecordDeposit(Money.From(1000), "tx-1");
    
    wallet.RecordStakeLocked(Money.From(500), "tx-2");
    
    Assert.Equal(500, wallet.AvailableBalance.Amount);
    Assert.Equal(500, wallet.LockedBalance.Amount);
}
```

### Integration Tests (Repositories)
Test with actual database:
```csharp
[Fact]
public async Task UserRepository_CreatesUser()
{
    var user = User.Create("test@example.com", "1234567890", "hash", "John", "Doe");
    await _userRepository.AddAsync(user);
    await _userRepository.SaveChangesAsync();
    
    var retrieved = await _userRepository.GetByIdAsync(user.Id);
    Assert.NotNull(retrieved);
}
```

### API Tests (Controllers)
Test HTTP layer:
```csharp
[Fact]
public async Task PlaceStake_Returns_200_WhenValid()
{
    var response = await _client.PostAsJsonAsync("/api/stakes", stakeDto);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

## Performance Considerations

1. **Database Indices**
   - On UserId, WalletId, BetId for fast lookups
   - On Status for filtering queries
   - On CreatedAt for sorting

2. **Caching Strategy**
   - Cache popular bets (5 min)
   - Cache wallet balances (1 min)
   - Invalidate on writes

3. **Query Optimization**
   - Use `.Include()` to load related data in one query
   - Pagination for large result sets
   - Avoid N+1 queries

4. **Database Optimization**
   - Transactions are append-only (fast writes)
   - Wallet balance cached for performance
   - Archive old data periodically

## Security Best Practices

1. **Input Validation**
   - Validate all DTOs
   - Range checks on amounts
   - Format validation on emails/phones

2. **Authentication**
   - JWT tokens with short expiration
   - Refresh tokens in secure HTTP-only cookies
   - Validate token signature

3. **Authorization**
   - Verify user ownership of resources
   - Admin-only operations protected
   - Role-based access control

4. **Money Safety**
   - All calculations in backend
   - Decimal precision checked
   - Ledger is immutable

5. **Race Condition Prevention**
   - Redis locks on critical sections
   - Database constraints (unique indexes)
   - Idempotency checks

## Monitoring & Debugging

### Logging Points
- User login/logout
- Stake placement (amount, outcome)
- Bet resolution
- Transaction creation
- Error events

### Metrics to Track
- Total stakes placed
- Total amount locked
- Resolution success rate
- Failed transactions
- API response times

### Debug Commands
```bash
# Check database
SELECT COUNT(*) FROM transactions WHERE created_at > NOW() - interval '1 day';

# Check Redis locks
redis-cli KEYS "stake_lock:*"
redis-cli TTL "stake_lock:user-id"

# Check migrations
dotnet ef migrations list
```
