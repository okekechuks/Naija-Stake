# Phase 1 Quick Start Testing Checklist

## ✅ Setup Phase

### 1. Install Prerequisites
```bash
# .NET 8.0 SDK (for building and running tests)
dotnet --version  # Should be 8.0 or higher

# PostgreSQL (for integration tests with real DB)
psql --version    # Should be installed

# Redis (for Phase 2)
redis-cli --version
```

### 2. Navigate to Backend
```bash
cd Backend
```

### 3. Restore Dependencies
```bash
dotnet restore
```

## ✅ Running Unit Tests (Domain Layer)

### Quick Test
```bash
# Run all domain unit tests (fastest, ~2 seconds)
dotnet test tests/NaijaStake.Domain.Tests -c Release

# Expected output:
# Test Run Successful.
# Total tests: 50
# Passed: 50
# Failed: 0
# Skipped: 0
# Duration: ~1-2 seconds
```

### Test Individual Components
```bash
# Money value object tests
dotnet test tests/NaijaStake.Domain.Tests --filter MoneyTests

# User entity tests
dotnet test tests/NaijaStake.Domain.Tests --filter UserTests

# Wallet entity tests  
dotnet test tests/NaijaStake.Domain.Tests --filter WalletTests

# Bet entity tests
dotnet test tests/NaijaStake.Domain.Tests --filter BetTests

# Stake entity tests
dotnet test tests/NaijaStake.Domain.Tests --filter StakeTests
```

### With Verbose Output
```bash
dotnet test tests/NaijaStake.Domain.Tests --verbosity detailed
```

## ✅ Running Integration Tests (Repositories)

### Prerequisites
Integration tests use **SQLite in-memory** (no PostgreSQL required)

### Run Integration Tests
```bash
# Run all integration tests (~2 seconds, uses in-memory DB)
dotnet test tests/NaijaStake.Integration.Tests -c Release

# Expected output:
# Test Run Successful.
# Total tests: 15
# Passed: 15
# Failed: 0
# Skipped: 0
# Duration: ~2-3 seconds
```

### Test Specific Repositories
```bash
# User repository tests
dotnet test tests/NaijaStake.Integration.Tests --filter UserRepositoryTests

# Wallet repository tests
dotnet test tests/NaijaStake.Integration.Tests --filter WalletRepositoryTests

# Bet repository tests
dotnet test tests/NaijaStake.Integration.Tests --filter BetRepositoryTests
```

## ✅ Running All Tests

```bash
# Run all tests (unit + integration)
dotnet test

# Expected output:
# Total tests: 65
# Passed: 65
# Failed: 0
# Duration: ~5 seconds
```

## ✅ Database Setup (PostgreSQL)

### 1. Create Database
```bash
# Connect to PostgreSQL as superuser
psql -U postgres

# Create development database
CREATE DATABASE naija_stake_dev;
CREATE DATABASE naija_stake_test;

# Exit psql
\q
```

### 2. Update Connection String
Edit [Backend\src\NaijaStake.API\appsettings.Development.json](Backend\src\NaijaStake.API\appsettings.Development.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=naija_stake_dev;Username=postgres;Password=your_password;"
}
```

### 3. Run Migrations
```bash
cd src/NaijaStake.API

# Create initial migration
dotnet ef migrations add InitialCreate --project ../NaijaStake.Infrastructure

# Apply migrations
dotnet ef database update

# Verify (should show 7 tables)
psql -U postgres -d naija_stake_dev -c "\dt"
```

## ✅ Database Verification

See [DATABASE_VERIFICATION.md](DATABASE_VERIFICATION.md) for detailed checks.

### Quick Verification
```bash
# Connect to database
psql -U postgres -d naija_stake_dev

# List tables
\dt

# Check Users table structure
\d "Users"

# Exit
\q
```

## ✅ Manual API Testing (Preparation for Phase 2)

### Start the API
```bash
cd src/NaijaStake.API
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to stop.
```

### View Swagger API Documentation
```
https://localhost:5001/swagger/index.html
```

## 📊 Test Summary

### Unit Tests (Domain Layer) - 50+ tests
- ✅ Money value object (12 tests)
- ✅ User entity (7 tests)
- ✅ Wallet entity (11 tests)
- ✅ Bet entity (14 tests)
- ✅ Stake entity (9 tests)

**Run**: `dotnet test tests/NaijaStake.Domain.Tests`
**Speed**: ~1-2 seconds
**Database**: None (pure business logic)

### Integration Tests (Data Access) - 15+ tests
- ✅ User repository (6 tests)
- ✅ Wallet repository (3 tests)
- ✅ Bet repository (5 tests)

**Run**: `dotnet test tests/NaijaStake.Integration.Tests`
**Speed**: ~2-3 seconds
**Database**: SQLite in-memory (auto-created)

## 🎯 Testing Workflow

### Daily Development
```bash
# After making changes
dotnet test tests/NaijaStake.Domain.Tests    # Quick feedback (1s)
dotnet test tests/NaijaStake.Integration.Tests # Data layer (2s)

# Total time: ~3 seconds
```

### Before Committing
```bash
# Full test suite
dotnet test

# With coverage report
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### After Adding New Feature
```bash
# Run all tests including new ones
dotnet test

# Run specific new test (if failing)
dotnet test --filter "NewFeatureName" --verbosity detailed
```

## 🚨 Troubleshooting

### Tests Won't Run
```bash
# Clean build
dotnet clean
dotnet build

# Run tests again
dotnet test
```

### Database Connection Error
```bash
# Verify PostgreSQL is running
sudo service postgresql status

# Test connection
psql -h localhost -U postgres -d postgres -c "SELECT 1;"

# Check connection string in appsettings.Development.json
```

### Async/Await Issues in Integration Tests
```bash
# Ensure async test fixture is properly initialized
# Check that test inherits from IAsyncLifetime

// Correct pattern:
public class MyTests : IAsyncLifetime
{
    public async Task InitializeAsync() { ... }
    public async Task DisposeAsync() { ... }
}
```

## 📝 Next Steps

After verifying all tests pass:

1. **Phase 2**: Create Service layer
   - AuthService
   - StakingService
   - BetService
   - WalletService

2. **Phase 2**: Implement Controllers
   - AuthController (register, login, refresh)
   - WalletController (balance, transactions, deposit)
   - BetsController (list, details, create)
   - StakesController (place stake, history)

3. **Phase 2**: Add API Tests
   - Authentication flow tests
   - Endpoint tests with various scenarios
   - Error handling tests

## 📚 Resources

- [TESTING_GUIDE.md](TESTING_GUIDE.md) - Comprehensive testing documentation
- [DATABASE_VERIFICATION.md](DATABASE_VERIFICATION.md) - Database schema and integrity checks
- [ARCHITECTURE.md](ARCHITECTURE.md) - Design patterns and architectural decisions
- [README.md](README.md) - Backend project overview

## ✨ Success Criteria

Your Phase 1 setup is complete when:

✅ All unit tests pass (50+ tests)  
✅ All integration tests pass (15+ tests)  
✅ PostgreSQL database is created with all 7 tables  
✅ API starts without errors and serves Swagger docs  
✅ Team can run `dotnet test` and see green results  

Total test execution time: **< 10 seconds**
