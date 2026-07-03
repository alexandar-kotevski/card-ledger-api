# Research: Card Ledger API

**Feature**: [spec.md](./spec.md) | **Date**: 2026-07-02

Phase 0 research resolving technical unknowns for implementation planning.

## API Style

**Decision**: ASP.NET Core Minimal APIs with route groups under `/api/cards`.

**Rationale**: User requirement; thin endpoints delegate to application services only (constitution compliance).

**Alternatives considered**:
- MVC controllers — rejected; Minimal APIs are lighter and match user specification
- gRPC — rejected; REST aligns with spec integrator-facing operations

## Treasury Data Source

**Decision**: U.S. Treasury Fiscal Data — Reporting Rates of Exchange endpoint.

**Rationale**: Official public API, no authentication required, matches spec "Treasury API".

**URL**: `https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange`

**Fields used**: `country_currency_desc`, `exchange_rate`, `record_date`, `effective_date`

**Example request**:
```
GET .../rates_of_exchange?fields=country_currency_desc,exchange_rate,record_date
    &filter=record_date:gte:2025-01-01
    &sort=-record_date
    &page[size]=1000
```

**Alternatives considered**:
- ECB exchange rates — rejected; spec names Treasury explicitly
- Hardcoded test rates only — rejected; production requires live Treasury sync

## Currency Mapping

**Decision**: Static ISO 4217 → Treasury `country_currency_desc` lookup table in Infrastructure.

**Rationale**: Treasury uses descriptive labels (e.g. `Euro-Euro`, `Australia-Dollar`), not ISO codes.

**Alternatives considered**:
- Runtime fuzzy matching — rejected; ambiguous and error-prone
- USD-only cards — rejected; spec supports multiple currencies

## Rate Selection Rules

**Decision**: Use `effective_date` as the lookup date for all rate queries; retain `record_date` for Treasury sync filtering and audit.

**Rationale**: Treasury may publish multiple effective periods under one `record_date` (e.g. Israel-Shekel with effective dates 2026-03-31 and 2026-05-29 on record_date 2026-03-31). Lookback must key off when the rate applies, not when it was filed.

### Transaction FX (historical lookback)

- Window: inclusive 6 calendar months `[transactionDate - 6 months, transactionDate]` on **`effective_date`**
- Selection: most recent `effective_date` on or before the transaction date within the window
- API `rateDate` response field reflects **`effective_date`**
- Failure: `ExchangeRateNotFoundException` with card, transaction, currency, and date context

### Balance FX (latest rate)

- Selection: `MAX(effective_date)` for the currency in the cached `ExchangeRates` table
- Distinct code path from per-transaction historical lookback (spec FR-013)

### Purchase ledger debit

- Convert purchase amount to card currency using **latest** cached rate (by `effective_date`) before debiting ledger

### Treasury sync storage

- Fetch filter: `record_date:gte:{windowStart}` (captures backfills under older publication dates)
- Upsert key: `(CurrencyCode, EffectiveDate)` — multiple rows per `record_date` allowed

## Treasury Rate Sync Service

**Decision**: `TreasuryRateSyncService` implementing `IHostedService` with startup bootstrap and daily schedule.

### Startup bootstrap

- Fetches lookback window: `record_date:gte:max(cutoff, UtcToday - 6 months)`
- Runs **before** API accepts traffic (blocking bootstrap)
- Paginated fetch (Treasury `page[size]` + `page[number]`)

**Rationale**: Matches maximum transaction FX lookback window; no multi-year backfill on first start.

### Daily schedule

- Fires at **00:00 UTC** (configurable `TreasurySync:DailyRunTimeUtc`, default `"00:00:00"`)
- **Full-window reconciliation**: `record_date:gte:max(cutoff, UtcToday - 6 months)` — same window as bootstrap; catches late Treasury backfills
- No separate yesterday-only incremental pass

### Rate retention

- **Append-only historical cache**
- New `(CurrencyCode, EffectiveDate)` → INSERT new row
- Existing effective date re-synced → UPSERT (update rate and `record_date` in place)
- Rows **never deleted**
- Over time, table grows beyond 6 months via daily appends

**Alternatives considered**:
- Full multi-year backfill on startup — rejected per user requirement (6 months only)
- Hourly polling — rejected; Treasury data is quarterly; daily midnight is sufficient

## Persistence

**Decision**: EF Core 10 + Npgsql targeting PostgreSQL **18**.

**Decimal mapping**: `decimal(18,4)` for monetary fields via `HasPrecision(18,4)`; `decimal(18,8)` for exchange rates.

**Rationale**: Constitution decimal-only requirement; PostgreSQL `numeric` avoids floating-point.

**Alternatives considered**:
- SQLite for dev — rejected; user specified PostgreSQL with Docker
- Dapper — rejected; EF Core aligns with constitution Infrastructure layer pattern

## Security

**Decision**: Store CVV as SHA-256 hash; return plaintext CVV only in issue response.

**Rationale**: Validation on purchase compares hash; reduces exposure in database.

**Alternatives considered**:
- Reversible encryption — rejected; hashing sufficient for 3-digit CVV validation

## PAN Generation

**Decision**: `RandomNumberGenerator` producing 16-digit numeric string with DB unique index and retry on collision.

**Rationale**: Spec FR-002 requires exactly 16 digits; uniqueness enforced at persistence layer.

## Solution Format

**Decision**: `CardLedger.slnx` (XML solution), not `.sln`.

**Rationale**: User requirement.

## Testing

**Decision**: xUnit + NSubstitute for unit tests; coverlet 100% gate on Application lookback namespace; Testcontainers.PostgreSql (PostgreSQL 18) for integration tests.

**Rationale**: Constitution and user requirements; NSubstitute mocks `IExchangeRateRepository` for deterministic lookback tests.

**Alternatives considered**:
- Moq — rejected; user specified NSubstitute

## Docker

**Decision**: Multi-stage Dockerfile (`sdk:10.0` / `aspnet:10.0`); docker-compose with `postgres:18`.

**Rationale**: User deployment requirement; api waits for postgres healthy then runs bootstrap sync.
