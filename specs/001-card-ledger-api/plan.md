# Implementation Plan: Card Ledger API

**Branch**: `001-card-ledger-api` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-card-ledger-api/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Card Ledger API exposing card issuance, purchase, transaction retrieval with FX
conversion, and ledger-backed balance queries. ASP.NET Core Minimal APIs on
.NET 10, clean architecture (Domain/Application/Infrastructure/Api), PostgreSQL
18 persistence via EF Core, Treasury rates cached locally and synced by a
`BackgroundService` (6-month startup backfill, daily midnight UTC incremental
sync).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`net10.0`)

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 + Npgsql, HttpClient (Treasury), NSubstitute

**Storage**: PostgreSQL 18 — Cards, Ledgers, Transactions, ExchangeRates (cached Treasury data)

**Testing**: xUnit + NSubstitute + coverlet (100% coverage gate on `ExchangeRateLookbackService` and related conversion logic in Application layer)

**Target Platform**: Linux containers (Docker)

**Project Type**: Web API (backend service)

**Performance Goals**: SC-001/SC-002: issue and purchase under 2 seconds p95 at normal load

**Constraints**: Decimal-only money; 6-month inclusive lookback; domain exceptions on missing rates; startup FX backfill limited to last 6 months

**Scale/Scope**: Single-tenant integrator API; no authentication in v1

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] All projects target `net10.0` with C# 14 and nullable enabled
- [x] No `float`/`double`/`Half` in monetary/limit/balance fields (decimal only)
- [x] Ledger transaction logic isolated in Domain/Application layers per constitution layout
- [x] Exchange-rate and lookback modules identified with 100% xUnit coverage plan
- [x] Complexity Tracking filled if any gate is intentionally violated (N/A — no violations)

## Project Structure

### Documentation (this feature)

```text
specs/001-card-ledger-api/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── CardLedger.Domain/
│   ├── Entities/          # Card, Ledger, Transaction
│   ├── ValueObjects/      # Money, CurrencyCode, Pan, Cvv
│   ├── Exceptions/        # ExchangeRateNotFoundException, InsufficientBalanceException, etc.
│   └── Services/          # CardNumberGenerator, domain invariants
├── CardLedger.Application/
│   ├── Abstractions/      # ICardRepository, ILedgerRepository, IExchangeRateRepository, ITreasuryRateClient
│   ├── Services/          # IssueCard, Purchase, TransactionQuery, Balance, ExchangeRateLookback
│   └── DTOs/              # Request/response records (decimal fields)
├── CardLedger.Infrastructure/
│   ├── Persistence/       # CardLedgerDbContext, EF configurations, migrations
│   ├── Treasury/          # TreasuryRateClient, TreasuryRateSyncService (BackgroundService)
│   └── DependencyInjection/
└── CardLedger.Api/
    ├── Endpoints/         # CardEndpoints, TransactionEndpoints, QueryEndpoints
    ├── Program.cs         # composition root
    └── appsettings.json

tests/
├── CardLedger.Domain.Tests/
├── CardLedger.Application.Tests/   # 100% coverage gate (lookback + conversion)
└── CardLedger.Integration.Tests/   # WebApplicationFactory + Testcontainers PostgreSQL 18

global.json                        # SDK pin
CardLedger.slnx                    # XML-based solution file (.slnx, not .sln)
Dockerfile                         # multi-stage
docker-compose.yml                 # api + postgres:18
```

**Structure Decision**: Constitution-mandated four-project layout. Api hosts
Minimal APIs and registers `TreasuryRateSyncService` as `IHostedService`.
Solution uses `CardLedger.slnx` (not `.sln`).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Design Artifacts

| Artifact | Path | Status |
|----------|------|--------|
| Research | [research.md](./research.md) | Complete |
| Data model | [data-model.md](./data-model.md) | Complete |
| API contracts | [contracts/openapi.yaml](./contracts/openapi.yaml) | Complete |
| Quickstart | [quickstart.md](./quickstart.md) | Complete |

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/cards` | Issue card |
| POST | `/api/cards/transactions` | Purchase |
| GET | `/api/cards/{cardNumber}/transactions` | List transactions (optional FX) |
| GET | `/api/cards/{cardNumber}/transactions/{guid}` | Single transaction (optional FX) |
| GET | `/api/cards/{cardNumber}/balance` | Available balance in target currency |

## Treasury Rate Sync

- **Startup**: Blocking bootstrap fetches last 6 calendar months (`record_date:gte:{UtcToday - 6 months}`) before API accepts traffic
- **Daily**: 00:00 UTC incremental sync (`record_date:gte:{yesterday}`)
- **Retention**: Append-only — new `record_date` inserts row; same date upserts; rows never deleted

## Docker Deployment

- **Dockerfile**: Multi-stage (`sdk:10.0` build, `aspnet:10.0` runtime), expose 8080
- **docker-compose.yml**: `postgres:18` + api service; bootstrap sync before listening

## Testing Strategy

- **Application.Tests**: `ExchangeRateLookbackServiceTests` with NSubstitute; 100% coverlet gate on lookback namespace
- **Integration.Tests**: WebApplicationFactory + Testcontainers PostgreSQL 18; issue → purchase → balance flow

## Post-Design Constitution Check

All gates re-confirmed after Phase 1 design. `ExchangeRateLookbackService` lives
in Application layer; EF mappings use `decimal` only; no complexity violations.
