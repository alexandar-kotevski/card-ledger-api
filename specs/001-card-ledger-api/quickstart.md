# Quickstart: Card Ledger API

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-07-02

Validation guide for end-to-end feature verification. See [data-model.md](./data-model.md) and [contracts/openapi.yaml](./contracts/openapi.yaml) for details.

## Prerequisites

- Docker Desktop (or Docker Engine + Compose)
- .NET 10 SDK (for local test runs)
- `curl` or equivalent HTTP client

## Start the Stack

```powershell
docker compose up --build
```

Wait until:
1. PostgreSQL 18 is healthy
2. Treasury bootstrap sync completes (last 6 months of rates loaded)
3. API logs show listening on port 8080

Base URL: `http://localhost:8080`

## Scenario A — Issue a Card

```powershell
curl -X POST http://localhost:8080/api/cards `
  -H "Content-Type: application/json" `
  -d '{"creditLimit":"5000.00","currency":"USD"}'
```

**Expected (201)**:
- `cardNumber`: 16-digit PAN
- `expiryDate`: MM/YY (~3 years from today, e.g. `07/29`)
- `cvv`: 3-digit string
- `currency`: `USD`
- `creditLimit`: `5000.00`

Save `cardNumber`, `expiryDate`, and `cvv` for subsequent scenarios.

## Scenario B — Purchase

```powershell
curl -X POST http://localhost:8080/api/cards/transactions `
  -H "Content-Type: application/json" `
  -d '{
    "cardNumber": "<PAN from Scenario A>",
    "expiryDate": "<expiry from Scenario A>",
    "cvv": "<cvv from Scenario A>",
    "amount": "150.00",
    "currency": "USD",
    "description": "Test purchase"
  }'
```

**Expected (201)**:
- `id`: transaction Guid
- `amount`: `150.00`
- `currency`: `USD`
- `description`: `Test purchase`

## Scenario C — List Transactions with FX

```powershell
curl "http://localhost:8080/api/cards/<PAN>/transactions?targetCurrency=EUR"
```

**Expected (200)**:
- Array with at least one transaction from Scenario B
- `convertedAmount`, `convertedCurrency`, `rateUsed`, `rateDate` populated when FX applied

## Scenario D — Single Transaction with FX

```powershell
curl "http://localhost:8080/api/cards/<PAN>/transactions/<transaction-id>?targetCurrency=EUR"
```

**Expected (200)**:
- Single transaction matching Scenario B purchase
- FX conversion fields present

## Scenario E — Available Balance (ledger currency)

```powershell
curl "http://localhost:8080/api/cards/<PAN>/balance"
```

**Expected (200)**:
- `availableBalance`: `4850.00` (5000.00 - 150.00)
- `currency`: `USD` (ledger currency)
- `rateUsed` and `rateDate` omitted (no FX applied)

## Scenario E2 — Available Balance with FX

```powershell
curl "http://localhost:8080/api/cards/<PAN>/balance?targetCurrency=EUR"
```

**Expected (200)**:
- `availableBalance`: converted amount in EUR
- `currency`: `EUR`
- `rateUsed` and `rateDate` present when FX applied

## Scenario F — Lookback Failure

This scenario validates the 6-month lookback domain exception. Requires a
transaction where no Treasury rate exists within the window — typically
verified via integration tests with seeded data rather than manual curl.

**Expected (422)**:
- Problem details with `title` indicating exchange rate not found
- Context fields: `cardNumber`, `transactionId`, `sourceCurrency`, `targetCurrency`, `transactionDate`

## Scenario G — Invalid Purchase (Insufficient Balance)

Issue a card with low limit, attempt purchase exceeding balance.

**Expected (422)**:
- Problem details indicating insufficient balance

## Scenario H — Invalid Credentials

Submit purchase with wrong CVV.

**Expected (422)**:
- Problem details indicating invalid card credentials (no credential values echoed)

## Run Tests

```powershell
dotnet test CardLedger.slnx --collect:"XPlat Code Coverage"
```

Verify:
- All tests pass
- `CardLedger.Application` lookback namespace meets 100% line and branch coverage (coverlet gate)

## Teardown

```powershell
docker compose down
```

Remove volumes if a clean database is needed:

```powershell
docker compose down -v
```

## Troubleshooting

| Symptom | Likely cause | Action |
|---------|--------------|--------|
| API not ready on startup | Treasury bootstrap still running | Wait for sync logs; 6-month fetch may take 30–60s |
| 422 on FX conversion | Rate not in cached window | Confirm bootstrap sync completed; check `ExchangeRates` table |
| Connection refused | Postgres not healthy | `docker compose ps`; check postgres healthcheck |
| 404 on balance/transactions | Wrong PAN | Use exact 16-digit card number from issue response |

## Related Documents

- [research.md](./research.md) — Treasury sync and lookback decisions
- [data-model.md](./data-model.md) — entity definitions and state transitions
- [contracts/openapi.yaml](./contracts/openapi.yaml) — full API contract
