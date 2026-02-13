# NaijaStake Backend - ASP.NET Core API

A secure, scalable real-money prediction/staking platform built with clean architecture principles.

## Architecture Overview

```
Backend/
├── src/
│   ├── NaijaStake.Domain/          # Domain entities, value objects, business logic
│   │   ├── Entities/               # User, Wallet, Bet, Stake, Transaction, Outcome
│   │   ├── ValueObjects/           # Money (immutable VO)
│   │   ├── Constants/              # App constants
│   │   └── ExceptionHandling/      # Domain exceptions
│   ├── NaijaStake.Infrastructure/  # Data access, repositories, external services
│   │   ├── Data/                   # DbContext, migrations
│   │   └── Repositories/           # Repository implementations
│   └── NaijaStake.API/             # ASP.NET Core Web API
│       ├── Controllers/            # API endpoints
│       ├── DTOs/                   # Data transfer objects
│       └── Middleware/             # Custom middleware (error handling, etc)
```

## Key Principles

- **Domain-Driven Design (DDD)**: Business logic lives in domain entities, not controllers
- **Repository Pattern**: Abstracted data access layer
- **Immutable Transactions**: All wallet changes are append-only
- **Decimal Money**: No floating-point arithmetic for currency
- **Redis Locks**: Prevent race conditions on critical operations
- **Idempotency**: Safe to replay failed requests
- **Clean Separation**: Frontend has zero business logic

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 13+
- Redis 6+

## Setup

### 1. Clone and Navigate

```bash
cd Backend/src/NaijaStake.API
```

### 2. Configure Database Connection

Edit `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=naija_stake_dev;Username=postgres;Password=your_password;"
}
```

### 3. Configure Redis

```json
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

### 4. Create Initial Migration

```bash
dotnet ef migrations add InitialCreate --project ../NaijaStake.Infrastructure --startup-project .
```

### 5. Run Migrations

```bash
dotnet ef database update --startup-project .
```

Alternatively, migrations run automatically in development mode on startup.

### 6. Run the API

```bash
dotnet run
```

The API will start on `https://localhost:5001`

## Database Schema

### Core Tables

- **Users**: User accounts with authentication
- **Wallets**: User wallet balances (calculated from transactions)
- **Transactions**: Immutable ledger of all money movements
- **Bets**: Prediction bets with lifecycle management
- **Outcomes**: Possible bet outcomes
- **Stakes**: User stakes placed on bet outcomes

### Key Design Patterns

1. **Money Handling**
   - All amounts stored as `NUMERIC(18,2)` in PostgreSQL
   - Mapped to `Money` value object in C#
   - Prevents floating-point precision issues

2. **Wallet Balance**
   - Calculated from transaction ledger (source of truth)
   - Cached in wallet for query performance
   - Never directly modified - only via transactions

3. **Idempotency Keys**
   - Unique constraints on `IdempotencyKey` columns
   - Allows safe request replay
   - Critical for distributed systems

4. **Redis Locks**
   - Lock before modifying wallet (5 seconds)
   - Lock before modifying bet (5 seconds)
   - Lock before resolving bet (30 seconds)
   - Prevents double-spend and double-resolution

## Entity Lifecycle

### Bet Lifecycle

```
Draft → Open → Closed → Resolved → Paid
       ↓      ↓        ↓          
    (cancel) (cancel) (cancel)
       ↓      ↓        ↓
    Cancelled (terminal state)
```

### Stake Lifecycle

```
Active → Won/Lost/Cancelled → (payout issued)
```

### Transaction Types

- `Deposit`: Money enters wallet
- `StakeLocked`: Funds locked from available to locked
- `StakeRefund`: Locked funds returned (lost stake or refund)
- `WinPayout`: Winnings paid (stake + profit moved to available)
- `PlatformFee`: Fee deducted from available
- `Withdrawal`: Money leaves wallet

## API Endpoints (Phase 1)

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/refresh` - Refresh JWT token

### Wallet
- `GET /api/wallet/balance` - Get wallet balance
- `GET /api/wallet/transactions` - Transaction history
- `POST /api/wallet/deposit` - Deposit money

### Bets
- `GET /api/bets` - List popular/open bets
- `GET /api/bets/{id}` - Get bet details
- `GET /api/bets/category/{category}` - Bets by category

### Stakes
- `POST /api/stakes` - Place a stake
- `GET /api/stakes` - User's stakes
- `GET /api/stakes/{id}` - Stake details

## Security

- JWT authentication with access/refresh tokens
- Password hashed with BCrypt
- Idempotency keys prevent duplicate processing
- Redis locks prevent race conditions
- Input validation on all DTOs
- CORS configured for frontend domains
- Database constraints enforce business rules

## Development Commands

```bash
# Run in development
dotnet run --configuration Development

# Run tests (coming soon)
dotnet test

# Create migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Revert migration
dotnet ef database update PreviousMigrationName
```

## Error Handling

All errors return a standard format:

```json
{
  "code": "BUSINESS_RULE_VIOLATION",
  "message": "Insufficient funds",
  "details": null,
  "timestamp": "2026-02-12T10:45:30.123Z"
}
```

Common error codes:
- `INSUFFICIENT_FUNDS`
- `BUSINESS_RULE_VIOLATION`
- `RESOURCE_NOT_FOUND`
- `INVALID_STATE_TRANSITION`
- `CONCURRENCY_ERROR`

## Next Steps

1. Implement services layer (AuthService, StakingService, etc.)
2. Add logging (Serilog)
3. Implement caching layer
4. Add unit and integration tests
5. Set up CI/CD pipeline
6. Performance optimization and load testing

## Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [PostgreSQL](https://www.postgresql.org/docs/)
- [Redis](https://redis.io/documentation)
