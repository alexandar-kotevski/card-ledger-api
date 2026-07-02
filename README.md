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
