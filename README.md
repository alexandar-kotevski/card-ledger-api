# Card Ledger API

Financial ledger API for card transaction processing, exchange-rate conversion,
and lookback logic.

## Engineering Standards

This project is governed by the [project constitution](.specify/memory/constitution.md).
Key non-negotiables:

- **.NET 10 / C# 14** — all projects target `net10.0` with nullable reference types enabled
- **Decimal-only currency** — `float`, `double`, and `Half` are forbidden for monetary values
- **Domain isolation** — ledger logic lives in Domain/Application layers, not in API controllers
- **xUnit coverage gate** — 100% line and branch coverage required for exchange-rate maths and lookback logic

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 18 (local or Docker)
- Docker (optional, for containerized stack and integration tests)

## Build

```bash
dotnet build CardLedger.slnx
```

## Test

```bash
dotnet test CardLedger.slnx
```

Integration tests use Testcontainers with PostgreSQL 18. They are skipped automatically
when Docker is unavailable.

## Run locally

1. Start PostgreSQL (or use Docker Compose for the database only):

   ```bash
   docker compose up postgres -d
   ```

2. Update `src/CardLedger.Api/appsettings.Development.json` if your connection string differs.

3. Run the API:

   ```bash
   dotnet run --project src/CardLedger.Api/CardLedger.Api.csproj
   ```

4. Health check: `GET http://localhost:5000/health` (port from launch settings)

On first startup, Treasury sync backfills exchange rates for the last 6 months before
the API accepts traffic. Set `TreasurySync:Enabled` to `false` to disable sync (used in tests).

## Docker

Run the full stack (PostgreSQL + API on port 8080):

```bash
docker compose up --build
```

API endpoints are under `/api/cards`.

## API overview

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/cards` | Issue card |
| POST | `/api/cards/transactions` | Record purchase |
| GET | `/api/cards/{cardNumber}/transactions` | List transactions |
| GET | `/api/cards/{cardNumber}/transactions/{guid}` | Get transaction |
| GET | `/api/cards/{cardNumber}/balance?targetCurrency=` | Available balance |

Monetary amounts are serialized as decimal strings in JSON. See
[specs/001-card-ledger-api/contracts/openapi.yaml](specs/001-card-ledger-api/contracts/openapi.yaml).
