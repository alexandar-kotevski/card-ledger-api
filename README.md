# Card Ledger API

Financial ledger API for card transaction processing, exchange-rate conversion,
and lookback logic.

## Engineering Standards

This project is governed by the [project constitution](.specify/memory/constitution.md).
Key non-negotiables:

- **.NET 10 / C# 14** — all projects target `net10.0` with nullable reference types enabled
- **Decimal-only currency** — `float`, `double`, and `Half` are forbidden for monetary values
- **Domain isolation** — ledger logic lives in Domain/Application layers, not in API controllers
- **xUnit coverage gate** — 100% line coverage on exchange-rate and lookback types (`ExchangeRateLookbackService`, `CurrencyConversionService`, `ConversionRateMetadata`) when the full unit test suite runs (see `Directory.Build.props`)

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

4. Health check: `GET http://localhost:5195/health` (port from launch settings)

5. **API documentation:**
   - Interactive UI (Scalar): `http://localhost:5195/scalar/v1`
   - OpenAPI JSON: `http://localhost:5195/openapi/v1.json`

   Enabled automatically in Development. `dotnet run` opens Scalar in the browser.

On first startup, Treasury sync backfills exchange rates for the last 6 months (by
`effective_date`) before the API accepts traffic. Set `TreasurySync:Enabled` to `false`
to disable sync (used in tests).

### Database migrations

Schema changes use EF Core migrations under `src/CardLedger.Infrastructure/Persistence/Migrations/`.
Each migration needs both the `.cs` and `.Designer.cs` files (or generate with
`dotnet ef migrations add`). Migrations run automatically on API startup via `MigrateAsync()`.

## Docker

Run the full stack (PostgreSQL + API on port 8080):

```bash
docker compose up --build
```

API endpoints are under `/api/cards`.

**API documentation** (enabled in Docker Compose via `OpenApi__Enabled=true`):

- Scalar UI: `http://localhost:8080/scalar/v1`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`

To disable docs in Docker, set `OpenApi__Enabled=false` on the `api` service. In bare
Production deployments, docs are off unless you opt in with the same setting.

## API overview

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/cards` | Issue card |
| POST | `/api/cards/transactions` | Record purchase |
| GET | `/api/cards/{cardNumber}/transactions` | List transactions |
| GET | `/api/cards/{cardNumber}/transactions/{guid}` | Get transaction |
| GET | `/api/cards/{cardNumber}/balance` | Ledger balance; optional `?targetCurrency=` adds convertedBalance/convertedCurrency |

Monetary amounts are serialized as decimal strings in JSON. Card expiry dates use
MM/YY format (e.g. `"07/29"`). Supported currencies are ISO 4217 codes with cached
Treasury exchange rates on or after 2025-12-31. See
[specs/001-card-ledger-api/contracts/openapi.yaml](specs/001-card-ledger-api/contracts/openapi.yaml).

### Example — issue card

```json
POST /api/cards
{
  "creditLimit": "5000.00",
  "currency": "USD"
}

201 Response:
{
  "cardNumber": "4111111111111111",
  "expiryDate": "07/29",
  "cvv": "123",
  "currency": "USD",
  "creditLimit": "5000.00"
}
```

### Example — balance with FX

```json
GET /api/cards/4111111111111111/balance?targetCurrency=EUR

200 Response:
{
  "availableBalance": "4850.00",
  "currency": "USD",
  "convertedBalance": "4365.00",
  "convertedCurrency": "EUR",
  "rateUsed": "0.9000",
  "rateDate": "2026-06-30"
}
```
