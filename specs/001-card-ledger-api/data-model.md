# Data Model: Card Ledger API

**Feature**: [spec.md](./spec.md) | **Date**: 2026-07-02

## Entity Relationship

```mermaid
erDiagram
    Card ||--|| Ledger : has
    Card ||--o{ Transaction : has
    Card {
        uuid Id PK
        string Pan UK
        date ExpiryDate
        string CvvHash
        decimal CreditLimit
        string Currency
        timestamptz IssuedAt
    }
    Ledger {
        uuid Id PK
        uuid CardId FK_UK
        decimal AvailableBalance
        string Currency
        timestamptz UpdatedAt
    }
    Transaction {
        uuid Id PK
        uuid CardId FK
        string Description
        timestamptz TransactionDate
        decimal Amount
        string Currency
    }
    ExchangeRate {
        bigint Id PK
        string CountryCurrencyDesc
        string CurrencyCode
        decimal Rate
        date RecordDate
    }
```

## Card

Represents an issued payment card.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `uuid` | PK | Internal identifier |
| `Pan` | `varchar(16)` | NOT NULL, UNIQUE, INDEX | 16-digit card number returned to integrator |
| `ExpiryDate` | `date` | NOT NULL | Default: issue date + 3 years |
| `CvvHash` | `varchar(64)` | NOT NULL | SHA-256 hash of 3-digit CVV |
| `CreditLimit` | `numeric(18,4)` | NOT NULL, >= 0 | Decimal precision |
| `Currency` | `varchar(3)` | NOT NULL | ISO 4217 (card issue currency) |
| `IssuedAt` | `timestamptz` | NOT NULL | UTC issue timestamp |

**Validation rules**:
- `CreditLimit` MUST be positive on issue
- `Pan` MUST be exactly 16 numeric digits
- `Currency` MUST be a supported ISO 4217 code

## Ledger

One record per card tracking available spend balance. Maintained in card currency.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `uuid` | PK | |
| `CardId` | `uuid` | FK → Card.Id, UNIQUE | 1:1 with Card |
| `AvailableBalance` | `numeric(18,4)` | NOT NULL, >= 0 | Decimal precision |
| `Currency` | `varchar(3)` | NOT NULL | Mirrors card currency |
| `UpdatedAt` | `timestamptz` | NOT NULL | Last mutation timestamp |

**Validation rules**:
- Created on card issue with `AvailableBalance = CreditLimit`
- Decremented on each successful purchase (after FX conversion to card currency)
- MUST NOT go negative (enforced before debit)

## Transaction

Represents a purchase entry against a card.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `uuid` | PK | Returned to client as transaction identifier |
| `CardId` | `uuid` | FK → Card.Id, INDEX | |
| `Description` | `varchar(200)` | NOT NULL | Purchase description |
| `TransactionDate` | `timestamptz` | NOT NULL | Defaults to UTC now on purchase |
| `Amount` | `numeric(18,4)` | NOT NULL, >= 0 | Decimal precision; zero allowed |
| `Currency` | `varchar(3)` | NOT NULL | ISO 4217 purchase currency |

**Validation rules**:
- `Amount` of zero is valid (no ledger impact)
- Positive amounts reduce ledger `AvailableBalance`

## ExchangeRate

Cached Treasury reporting rates. Append-only historical cache.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `bigint` | PK, IDENTITY | |
| `CountryCurrencyDesc` | `varchar(100)` | NOT NULL | Treasury label (e.g. `Euro-Euro`) |
| `CurrencyCode` | `varchar(3)` | NOT NULL | Mapped ISO 4217 code |
| `Rate` | `numeric(18,8)` | NOT NULL | Foreign currency units per 1 USD (Treasury convention) |
| `RecordDate` | `date` | NOT NULL | Treasury publication date |

**Indexes**:
- UNIQUE (`CurrencyCode`, `RecordDate`)
- INDEX (`CurrencyCode`, `RecordDate` DESC) for lookback queries

**Sync behaviour**:
- Startup: insert rates where `record_date >= UtcToday - 6 months`
- Daily: insert/upsert rates where `record_date >= yesterday`
- Rows never deleted

## Domain Value Objects

| Value Object | Fields | Purpose |
|--------------|--------|---------|
| `Money` | `Amount` (decimal), `Currency` (string) | Enforce decimal-only monetary operations |
| `CurrencyCode` | ISO 4217 string | Validation wrapper |
| `Pan` | 16-digit string | Card number validation |
| `Cvv` | 3-digit string | CVV validation at issue |

## State Transitions

### Issue Card

```
POST /api/cards
  → Validate creditLimit > 0, currency supported
  → Generate Pan (unique), Cvv (random 3-digit), ExpiryDate (now + 3 years)
  → INSERT Card
  → INSERT Ledger (AvailableBalance = CreditLimit, Currency = card currency)
  → Return Pan, ExpiryDate, Cvv (plaintext), Currency, CreditLimit
```

### Purchase

```
POST /api/cards/transactions
  → Validate card credentials (Pan, ExpiryDate, CvvHash match, not expired)
  → If purchase currency ≠ card currency: convert amount using latest cached rate
  → Validate AvailableBalance >= debit amount
  → INSERT Transaction (TransactionDate = UtcNow)
  → UPDATE Ledger (AvailableBalance -= debit amount)
  → Return Id, Amount, Currency, Description
```

### Retrieve Transactions (with FX)

```
GET /api/cards/{cardNumber}/transactions[?targetCurrency=]
  → Load transactions for card
  → If targetCurrency provided: for each transaction, look up rate within
    6-month window on or before TransactionDate
  → On missing rate: throw ExchangeRateNotFoundException
```

### Retrieve Single Transaction (with FX)

```
GET /api/cards/{cardNumber}/transactions/{guid}[?targetCurrency=]
  → Load transaction by card + guid
  → If targetCurrency provided: apply same lookback rule as list
```

### Retrieve Balance

```
GET /api/cards/{cardNumber}/balance?targetCurrency=
  → Load Ledger for card
  → Convert AvailableBalance to targetCurrency using MAX(record_date) rate
  → On missing latest rate: throw ExchangeRateNotFoundException
```

## Domain Exceptions

| Exception | When | Context carried |
|-----------|------|-----------------|
| `ExchangeRateNotFoundException` | No rate in 6-month window (transaction FX) or no latest rate (balance) | Card number, transaction id, source/target currency, transaction date |
| `InsufficientBalanceException` | Purchase exceeds ledger balance | Card number, requested amount, available balance |
| `InvalidCardCredentialsException` | Wrong Pan, expiry, or CVV | Generic message (no credential leakage) |
| `CardExpiredException` | Expiry date in the past | Card number, expiry date |
| `CardNotFoundException` | Pan not found | Card number |

## EF Core Configuration Notes

- All monetary properties: `.HasPrecision(18, 4)` with `decimal` CLR type
- Exchange rate `Rate`: `.HasPrecision(18, 8)`
- No `float`/`double` column types anywhere
- Migrations applied on startup in Development/Docker environments
