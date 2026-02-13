# Database Verification Guide

## Prerequisites

- PostgreSQL installed and running
- `psql` CLI tool available
- Backend migrations applied

## 1. Database Schema Verification

Connect to the database:
```bash
psql -h localhost -U postgres -d naija_stake_dev
```

### Check All Tables
```sql
-- List all tables
\dt

-- Expected output:
--  Schema |             Name              | Type  | Owner
-- --------+-------------------------------+-------+----------
--  public | __EFMigrationsHistory        | table | postgres
--  public | Users                        | table | postgres
--  public | Wallets                      | table | postgres
--  public | Transactions                 | table | postgres
--  public | Bets                         | table | postgres
--  public | Outcomes                     | table | postgres
--  public | Stakes                       | table | postgres
```

### Verify Column Definitions

#### Users Table
```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Users'
ORDER BY ordinal_position;
```

Expected columns:
- `id` (uuid, NOT NULL)
- `email` (varchar(255), NOT NULL, UNIQUE)
- `phone_number` (varchar(20), NOT NULL, UNIQUE)
- `password_hash` (text, NOT NULL)
- `first_name` (varchar(100), NOT NULL)
- `last_name` (varchar(100), NOT NULL)
- `status` (integer, NOT NULL)
- `created_at` (timestamp, NOT NULL)
- `updated_at` (timestamp, nullable)
- `last_login_at` (timestamp, nullable)

#### Wallets Table
```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Wallets'
ORDER BY ordinal_position;
```

Expected columns:
- `id` (uuid, NOT NULL)
- `user_id` (uuid, NOT NULL, UNIQUE)
- `available_balance` (numeric(18,2), NOT NULL)
- `locked_balance` (numeric(18,2), NOT NULL)
- `created_at` (timestamp, NOT NULL)
- `updated_at` (timestamp, nullable)

#### Transactions Table
```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Transactions'
ORDER BY ordinal_position;
```

Expected columns:
- `id` (uuid, NOT NULL)
- `wallet_id` (uuid, NOT NULL)
- `user_id` (uuid, NOT NULL)
- `type` (integer, NOT NULL)
- `amount` (numeric(18,2), NOT NULL)
- `description` (varchar(500), NOT NULL)
- `status` (integer, NOT NULL)
- `created_at` (timestamp, NOT NULL)
- `bet_id` (uuid, nullable)
- `stake_id` (uuid, nullable)
- `payment_id` (uuid, nullable)
- `idempotency_key` (varchar(255), nullable, UNIQUE)
- `metadata` (text, nullable)

#### Bets Table
```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Bets'
ORDER BY ordinal_position;
```

Expected columns:
- `id` (uuid, NOT NULL)
- `title` (varchar(500), NOT NULL)
- `description` (varchar(2000), NOT NULL)
- `category` (integer, NOT NULL)
- `status` (integer, NOT NULL)
- `total_staked` (numeric(18,2), NOT NULL)
- `participant_count` (integer, NOT NULL)
- `closing_time` (timestamp, NOT NULL)
- `resolution_time` (timestamp, NOT NULL)
- `resolved_outcome_id` (uuid, nullable)
- `resolved_at` (timestamp, nullable)
- `resolution_notes` (text, nullable)
- `resolution_idempotency_key` (varchar(255), nullable, UNIQUE)
- `created_at` (timestamp, NOT NULL)
- `updated_at` (timestamp, nullable)

#### Outcomes Table
```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Outcomes'
ORDER BY ordinal_position;
```

Expected columns:
- `id` (uuid, NOT NULL)
- `bet_id` (uuid, NOT NULL)
- `title` (varchar(500), NOT NULL)
- `total_staked` (numeric(18,2), NOT NULL)
- `stake_count` (integer, NOT NULL)
- `created_at` (timestamp, NOT NULL)

#### Stakes Table
```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Stakes'
ORDER BY ordinal_position;
```

Expected columns:
- `id` (uuid, NOT NULL)
- `user_id` (uuid, NOT NULL)
- `bet_id` (uuid, NOT NULL)
- `outcome_id` (uuid, NOT NULL)
- `status` (integer, NOT NULL)
- `stake_amount` (numeric(18,2), NOT NULL)
- `actual_payout` (numeric(18,2), nullable)
- `created_at` (timestamp, NOT NULL)
- `resolved_at` (timestamp, nullable)
- `idempotency_key` (varchar(255), NOT NULL, UNIQUE)

## 2. Index Verification

### Check Indices
```sql
-- List all indices
SELECT tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename;
```

Expected indices:
```sql
-- On Users
Users - email UNIQUE
Users - phone_number UNIQUE

-- On Wallets
Wallets - user_id UNIQUE

-- On Transactions
Transactions - wallet_id
Transactions - user_id
Transactions - created_at
Transactions - type
Transactions - idempotency_key UNIQUE

-- On Bets
Bets - status
Bets - category
Bets - closing_time
Bets - created_at
Bets - resolution_idempotency_key UNIQUE

-- On Outcomes
Outcomes - bet_id

-- On Stakes
Stakes - user_id
Stakes - bet_id
Stakes - outcome_id
Stakes - status
Stakes - created_at
Stakes - idempotency_key UNIQUE
```

Verify specific index:
```sql
-- Check email index on Users
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Users' AND indexname LIKE '%email%';
```

## 3. Foreign Key Verification

```sql
-- List all foreign keys
SELECT constraint_name, table_name, column_name, referenced_table_name, referenced_column_name
FROM information_schema.key_column_usage
WHERE table_schema = 'public' AND referenced_table_name IS NOT NULL;
```

Expected foreign keys:
- `Wallets.user_id` → `Users.id`
- `Transactions.wallet_id` → `Wallets.id`
- `Transactions.user_id` → `Users.id`
- `Bets.resolved_outcome_id` → `Outcomes.id`
- `Outcomes.bet_id` → `Bets.id`
- `Stakes.user_id` → `Users.id`
- `Stakes.bet_id` → `Bets.id`
- `Stakes.outcome_id` → `Outcomes.id`

## 4. Data Integrity Tests

### Test 1: Decimal Precision
```sql
-- Verify decimal precision for monetary columns
SELECT 
    table_name,
    column_name,
    data_type,
    numeric_precision,
    numeric_scale
FROM information_schema.columns
WHERE data_type = 'numeric'
AND table_schema = 'public';

-- Expected: precision=18, scale=2 for all monetary columns
```

### Test 2: Create Test Data
```sql
-- Insert a test user
INSERT INTO "Users" (
    "Id", "Email", "PhoneNumber", "PasswordHash", "FirstName", 
    "LastName", "Status", "CreatedAt"
) VALUES (
    'f47ac10b-58cc-4372-a567-0e02b2c3d479',
    'test@example.com',
    '1234567890',
    'hash123',
    'Test',
    'User',
    1,  -- Active status
    NOW()
);

-- Verify insertion
SELECT * FROM "Users" WHERE "Email" = 'test@example.com';
```

### Test 3: Create Wallet for User
```sql
-- Insert wallet
INSERT INTO "Wallets" (
    "Id", "UserId", "AvailableBalance", "LockedBalance", "CreatedAt"
) VALUES (
    'c3fb8b8e-8b3c-4d3c-8b3c-8b3c8b3c8b3c',
    'f47ac10b-58cc-4372-a567-0e02b2c3d479',
    1000.00,
    0.00,
    NOW()
);

-- Verify
SELECT * FROM "Wallets" WHERE "UserId" = 'f47ac10b-58cc-4372-a567-0e02b2c3d479';
```

### Test 4: Create Transaction (Immutable Ledger)
```sql
-- Insert deposit transaction
INSERT INTO "Transactions" (
    "Id", "WalletId", "UserId", "Type", "Amount", "Description", 
    "Status", "CreatedAt", "IdempotencyKey"
) VALUES (
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'c3fb8b8e-8b3c-4d3c-8b3c-8b3c8b3c8b3c',
    'f47ac10b-58cc-4372-a567-0e02b2c3d479',
    1,  -- Deposit type
    500.00,
    'Test deposit',
    1,  -- Completed status
    NOW(),
    'test-idempotency-key-1'
);

-- Verify transactions are append-only
SELECT * FROM "Transactions" ORDER BY "CreatedAt" DESC;

-- Check that duplicate idempotency key is rejected
INSERT INTO "Transactions" (
    "Id", "WalletId", "UserId", "Type", "Amount", "Description", 
    "Status", "CreatedAt", "IdempotencyKey"
) VALUES (
    'b1b2c3d4-e5f6-7890-abcd-ef1234567891',
    'c3fb8b8e-8b3c-4d3c-8b3c-8b3c8b3c8b3c',
    'f47ac10b-58cc-4372-a567-0e02b2c3d479',
    1,
    100.00,
    'Duplicate attempt',
    1,
    NOW(),
    'test-idempotency-key-1'  -- Same key
);
-- Should fail with UNIQUE constraint violation
```

### Test 5: Create Bet and Stakes
```sql
-- Create a bet
INSERT INTO "Bets" (
    "Id", "Title", "Description", "Category", "Status", 
    "TotalStaked", "ParticipantCount", "ClosingTime", "ResolutionTime", "CreatedAt"
) VALUES (
    'd1b2c3d4-e5f6-7890-abcd-ef1b34567890',
    'Bitcoin Price Bet',
    'Will BTC reach $100k?',
    4,  -- Market category
    2,  -- Open status
    0.00,
    0,
    NOW() + interval '24 hours',
    NOW() + interval '48 hours',
    NOW()
);

-- Create outcomes for the bet
INSERT INTO "Outcomes" (
    "Id", "BetId", "Title", "TotalStaked", "StakeCount", "CreatedAt"
) VALUES
    ('e1b2c3d4-e5f6-7890-abcd-ef1c34567890', 'd1b2c3d4-e5f6-7890-abcd-ef1b34567890', 'Yes', 0, 0, NOW()),
    ('f1b2c3d4-e5f6-7890-abcd-ef1d34567890', 'd1b2c3d4-e5f6-7890-abcd-ef1b34567890', 'No', 0, 0, NOW());

-- Create a stake
INSERT INTO "Stakes" (
    "Id", "UserId", "BetId", "OutcomeId", "Status", "StakeAmount",
    "CreatedAt", "IdempotencyKey"
) VALUES (
    'a2b2c3d4-e5f6-7890-abcd-ef1e34567890',
    'f47ac10b-58cc-4372-a567-0e02b2c3d479',
    'd1b2c3d4-e5f6-7890-abcd-ef1b34567890',
    'e1b2c3d4-e5f6-7890-abcd-ef1c34567890',
    1,  -- Active status
    100.00,
    NOW(),
    'test-stake-idempo-key-1'
);

-- Verify relationships
SELECT 
    s."Id", s."StakeAmount", b."Title", o."Title" as "OutcomeTitle"
FROM "Stakes" s
JOIN "Bets" b ON s."BetId" = b."Id"
JOIN "Outcomes" o ON s."OutcomeId" = o."Id"
WHERE s."UserId" = 'f47ac10b-58cc-4372-a567-0e02b2c3d479';
```

## 5. Performance Checks

### Count Records per Table
```sql
-- Verify no test data persists
SELECT 
    'Users' as table_name, COUNT(*) as record_count FROM "Users"
UNION ALL
SELECT 'Wallets', COUNT(*) FROM "Wallets"
UNION ALL
SELECT 'Transactions', COUNT(*) FROM "Transactions"
UNION ALL
SELECT 'Bets', COUNT(*) FROM "Bets"
UNION ALL
SELECT 'Outcomes', COUNT(*) FROM "Outcomes"
UNION ALL
SELECT 'Stakes', COUNT(*) FROM "Stakes";
```

### Verify Cascading Deletes

```sql
-- Deleting a bet should cascade to outcomes
DELETE FROM "Bets" WHERE "Id" = 'd1b2c3d4-e5f6-7890-abcd-ef1b34567890';

-- Verify outcomes are deleted
SELECT COUNT(*) FROM "Outcomes" WHERE "BetId" = 'd1b2c3d4-e5f6-7890-abcd-ef1b34567890';
-- Should return 0
```

### Check Query Performance

```sql
-- These should be fast (< 10ms) with indices
EXPLAIN ANALYZE
SELECT * FROM "Users" WHERE "Email" = 'test@example.com';

EXPLAIN ANALYZE
SELECT * FROM "Transactions" WHERE "WalletId" = 'c3fb8b8e-8b3c-4d3c-8b3c-8b3c8b3c8b3c';

EXPLAIN ANALYZE
SELECT * FROM "Stakes" WHERE "UserId" = 'f47ac10b-58cc-4372-a567-0e02b2c3d479' 
AND "Status" = 1;
```

## 6. Cleanup Test Data

```sql
-- WARNING: This deletes all data!
TRUNCATE TABLE "Stakes" CASCADE;
TRUNCATE TABLE "Outcomes" CASCADE;
TRUNCATE TABLE "Bets" CASCADE;
TRUNCATE TABLE "Transactions" CASCADE;
TRUNCATE TABLE "Wallets" CASCADE;
TRUNCATE TABLE "Users" CASCADE;

-- Verify all tables are empty
SELECT * FROM "Users" LIMIT 1;
```

## 7. Database Statistics

```sql
-- Generate statistics for query planner
ANALYZE;

-- Check database size
SELECT pg_size_pretty(pg_database_size('naija_stake_dev'));

-- Check table sizes
SELECT 
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

## Troubleshooting

### Connection Issues
```bash
# Test connection
psql -h localhost -U postgres -d naija_stake_dev -c "SELECT 1;"

# If fails, check PostgreSQL is running
sudo service postgresql status
```

### Migration Issues
```bash
# Check migration history
SELECT * FROM "__EFMigrationsHistory" ORDER BY "Id";

# If corrupted, reset migrations (CAUTION - loses data)
dotnet ef database drop --force
dotnet ef database update
```

### Foreign Key Constraint Violations
```sql
-- Check which foreign keys are violated
SELECT constraint_name, table_name
FROM information_schema.table_constraints
WHERE constraint_type = 'FOREIGN KEY'
AND table_schema = 'public';
```
