# Phase 1 Testing Guide

## Testing Strategy

Phase 1 includes three layers of tests:

```
Unit Tests (Domain Layer)
    ↓ No database, tests pure business logic
    ↓ Fast execution (milliseconds)
    ↓ Tests: Money, User, Wallet, Bet, Stake, Outcome

Integration Tests (Repository + Database)
    ↓ Uses SQLite in-memory database
    ↓ Tests data access layer
    ↓ Verifies database schema and EF Core mappings

API Tests (Coming in Phase 2)
    ↓ HTTP endpoints
    ↓ Authentication
    ↓ Request/response validation
```

## Running Tests

### Prerequisites
```bash
cd Backend
```

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
# Unit tests only
dotnet test tests/NaijaStake.Domain.Tests

# Integration tests only
dotnet test tests/NaijaStake.Integration.Tests

# API tests (Phase 2)
dotnet test tests/NaijaStake.API.Tests
```

### Run Specific Test Class
```bash
dotnet test tests/NaijaStake.Domain.Tests --filter MoneyTests
```

### Run with Verbose Output
```bash
dotnet test --verbosity normal
```

### Run with Code Coverage
```bash
dotnet test /p:CollectCoverage=true
```

## Test Organization

### Unit Tests: Domain Layer

**Location**: `tests/NaijaStake.Domain.Tests/`

#### Value Objects
- **MoneyTests.cs** (12 tests)
  - Creation with valid/invalid amounts
  - Arithmetic operations (add, subtract)
  - Comparisons (>, <, >=, <=, ==)
  - Immutability verification
  - Decimal precision handling

#### Entities
- **UserTests.cs** (7 tests)
  - User creation with validation
  - Email normalization
  - Login recording
  - Account activation/deactivation

- **WalletTests.cs** (11 tests)
  - Wallet creation
  - Balance management (available vs locked)
  - Stake locking/unlocking
  - Payout recording
  - Platform fee deduction
  - Affordability checks

- **BetTests.cs** (14 tests)
  - Bet creation with validation
  - Lifecycle transitions (Draft → Open → Closed → Resolved)
  - Outcome creation
  - Stake recording
  - Resolution with idempotency
  - Status checks

- **StakeTests.cs** (9 tests)
  - Stake creation
  - Win/loss marking
  - Cancellation
  - Idempotency verification

**Total Unit Tests**: ~50+

### Integration Tests: Data Access

**Location**: `tests/NaijaStake.Integration.Tests/`

Uses **SQLite in-memory database** for fast integration testing.

#### DatabaseFixture
- Provides clean in-memory database for each test
- Auto-creates schema
- Auto-cleans up after test
- Implements IAsyncLifetime for proper async disposal

#### Repositories Tests

- **UserRepositoryTests.cs** (6 tests)
  - Create user
  - Find by email (case-insensitive)
  - Find by phone
  - Email existence check
  - Load with wallet relationship

- **WalletRepositoryTests.cs** (3 tests)
  - Create wallet
  - Find by user ID
  - Load with transaction history

- **BetRepositoryTests.cs** (5 tests)
  - Create bet
  - Filter by category
  - Find open bets
  - Load with outcomes
  - Load with stakes

**Total Integration Tests**: ~15+

## Test Execution Walkthrough

### Example Unit Test: Money Addition

```csharp
[Fact]
public void Money_Add_ReturnsNewMoneyWithSum()
{
    // Arrange
    var money1 = Money.From(100);
    var money2 = Money.From(50);

    // Act
    var result = money1.Add(money2);

    // Assert
    result.Amount.Should().Be(150);
    money1.Amount.Should().Be(100); // Original unchanged (immutability)
}
```

Running:
```bash
dotnet test tests/NaijaStake.Domain.Tests --filter "MoneyTests and Add"
```

### Example Integration Test: User Creation

```csharp
[Fact]
public async Task AddAsync_CreatesUser()
{
    // Arrange
    var user = User.Create("john@example.com", "1234567890", "hash", "John", "Doe");

    // Act
    await _userRepository.AddAsync(user);
    await _userRepository.SaveChangesAsync();

    // Assert
    var retrieved = await _userRepository.GetByIdAsync(user.Id);
    retrieved.Should().NotBeNull();
    retrieved!.Email.Should().Be("john@example.com");
}
```

Running:
```bash
dotnet test tests/NaijaStake.Integration.Tests --filter "UserRepositoryTests"
```

## Expected Test Results

### Success Output
```
Test Run Successful.
Total tests: 65
Passed: 65
Failed: 0
Skipped: 0
Duration: ~2 seconds
```

## Debugging Failed Tests

### View Detailed Failure Info
```bash
dotnet test --verbosity detailed
```

### Run Single Test with Debugging
```bash
dotnet test tests/NaijaStake.Domain.Tests --filter "WalletTests" --no-build -c Debug
```

### Common Issues & Solutions

#### Issue: "Null reference exception in test"
**Solution**: Check test fixture initialization order

#### Issue: "Database locked in integration tests"
**Solution**: Ensure `IAsyncLifetime` is properly implemented to await database cleanup

#### Issue: "Entity mapping errors"
**Solution**: Review DbContext configuration in `StakeItDbContext.cs`

## Test Coverage

Current coverage by layer:

| Layer | Coverage | Tests |
|-------|----------|-------|
| Domain - ValueObjects | ~95% | 12 |
| Domain - Entities | ~90% | 40+ |
| Infrastructure - Repositories | ~80% | 15+ |
| API Layer | TBD | Phase 2 |

Aim for:
- **Domain layer**: 95%+ (critical business logic)
- **Infrastructure**: 80%+ (data access)
- **API layer**: 70%+ (thin layer, mostly routing)

## Adding New Tests

When you add new entities or features:

1. **Create corresponding test file**
   ```
   src/NaijaStake.Domain/Entities/MyEntity.cs
   tests/NaijaStake.Domain.Tests/Entities/MyEntityTests.cs
   ```

2. **Follow AAA pattern**
   ```csharp
   [Fact]
   public void MyTest()
   {
       // Arrange - Set up test data
       var entity = MyEntity.Create(...);
       
       // Act - Perform the action
       var result = entity.DoSomething();
       
       // Assert - Verify results
       result.Should().Be(expected);
   }
   ```

3. **Use meaningful test names**
   Format: `[UnitOfWork]_[Scenario]_[ExpectedResult]`
   
   Example: `Wallet_AddStake_IncreasesLockedBalance`

4. **Test both success and failure paths**
   ```csharp
   [Fact]
   public void MyEntity_Create_WithValidData_Succeeds() { ... }
   
   [Fact]
   public void MyEntity_Create_WithInvalidData_ThrowsException() { ... }
   ```

## Continuous Integration

For CI/CD pipeline, use:

```bash
# Full test suite with coverage
dotnet test --configuration Release /p:CollectCoverage=true --logger "console;verbosity=detailed"
```

## Performance Benchmarking

For performance-critical operations:

```bash
# Run specific high-performance critical tests
dotnet test tests/NaijaStake.Domain.Tests --filter "Money" -c Release
```

Money operations should complete in < 1ms.

## Next Steps (Phase 2)

1. Add API endpoint tests
2. Add authentication tests (JWT)
3. Add concurrency/race condition tests (Redis locks)
4. Add payment webhook tests
5. Load testing with high stakes volume
