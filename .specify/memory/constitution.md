<!--
Sync Impact Report
Version change: (none) → 1.0.0
Principles added:
  - I. .NET 10 / C# 14 Conventions (NON-NEGOTIABLE)
  - II. Decimal-Only Currency (NON-NEGOTIABLE)
  - III. Structural Domain Isolation for Ledger Transactions
  - IV. Mandatory xUnit Coverage for Exchange Rate and Lookback Logic
Sections added:
  - Technology Stack Constraints
  - Development Workflow and Quality Gates
Templates:
  - .specify/templates/plan-template.md ✅ updated
  - .specify/templates/spec-template.md ✅ updated
  - .specify/templates/tasks-template.md ✅ updated
  - README.md ✅ updated
Deferred: none
-->

# Card Ledger API Constitution

## Core Principles

### I. .NET 10 / C# 14 Conventions (NON-NEGOTIABLE)

All projects MUST target `net10.0`. The .NET SDK version MUST be pinned in
`global.json` when the solution is created. C# 14 language version MUST be
enabled (`<LangVersion>14</LangVersion>` or SDK default).

Code MUST follow [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
and .NET API design guidelines. Nullable reference types MUST be enabled
(`<Nullable>enable</Nullable>`). I/O-bound work MUST use async patterns;
library code MUST use `ConfigureAwait(false)` where applicable. Analyser
warnings MUST be treated as errors for new code (`TreatWarningsAsErrors` or
equivalent rule set).

**Rationale**: A single, modern runtime baseline and consistent style prevent
drift before any ledger code lands and ensure analyser-driven quality from day
one.

### II. Decimal-Only Currency (NON-NEGOTIABLE)

All limits, balances, amounts, fees, exchange rates used in calculations, and
aggregated totals MUST use `decimal`. `float`, `double`, and `Half` are
FORBIDDEN for monetary or limit fields — including DTOs, EF mappings, JSON
serialisation models, and API contracts.

External inputs MUST be parsed directly to `decimal`; no intermediate
floating-point conversion is permitted. Rounding rules MUST be explicit (e.g.
`MidpointRounding.ToEven`) and documented per currency and operation. Code
review and CI MUST reject any monetary use of floating-point types.

**Rationale**: Floating-point arithmetic introduces precision loss that is
unacceptable in financial calculations and is a common source of ledger
defects.

### III. Structural Domain Isolation for Ledger Transactions

Ledger transaction logic MUST live in a dedicated domain boundary, separate
from API, hosting, and infrastructure concerns. The recommended solution layout
is:

```text
src/
├── CardLedger.Domain/           # Entities, value objects (Money, ExchangeRate), domain services
├── CardLedger.Application/      # Use cases, orchestration, lookback/exchange-rate application logic
├── CardLedger.Infrastructure/   # Persistence, external integrations
└── CardLedger.Api/              # HTTP endpoints, DI composition root only

tests/
├── CardLedger.Domain.Tests/
├── CardLedger.Application.Tests/   # Exchange-rate maths + lookback logic (100% coverage gate)
└── CardLedger.Integration.Tests/
```

The Domain layer MUST NOT reference Infrastructure or Api projects. The
Application layer MAY reference Domain only; Infrastructure implements
interfaces defined in Application or Domain. Cross-domain calls (e.g. posting a
transaction) MUST go through explicit application services or domain events —
no direct repository access from API controllers. Each ledger transaction type
(authorisation, settlement, reversal, FX conversion) MUST be modelled as an
isolated domain concept with explicit invariants.

**Rationale**: Financial ledgers require auditability. Blurred layer boundaries
make correctness and exhaustive testing impossible to guarantee.

### IV. Mandatory xUnit Coverage for Exchange Rate and Lookback Logic

Exchange-rate maths and lookback logic MUST have 100% line and branch coverage
measured by `coverlet` (or equivalent) in CI. The test framework MUST be
xUnit (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`).

The coverage gate applies specifically to:

- Exchange-rate conversion, rounding, and cross-currency aggregation code
- Lookback window selection, historical rate retrieval, and fallback logic

Coverage reports MUST fail the build if below 100% for designated
assemblies/namespaces (configured in CI, not the entire solution). Tests MUST
use Arrange-Act-Assert. Edge cases — zero amounts, missing rates, boundary
dates, currency precision — MUST be covered. Other code SHOULD have meaningful
tests but is not subject to the 100% gate unless promoted by future amendment.

**Rationale**: Exchange-rate and lookback logic are the highest-risk
calculation paths; exhaustive unit testing is the primary safety net.

## Technology Stack Constraints

- **Runtime**: .NET 10 (`net10.0`)
- **Language**: C# 14
- **Testing**: xUnit with coverlet for gated modules
- **Serialisation**: Monetary values MUST be serialised as `decimal` or string;
  JSON number-as-double for money is FORBIDDEN

## Development Workflow and Quality Gates

Every pull request MUST pass the Constitution Check defined in
`.specify/templates/plan-template.md`. The plan phase MUST document any
justified violations in the Complexity Tracking table. The implement phase MUST
verify decimal usage via analyser rule or code review checklist. CI MUST enforce
`net10.0` build, xUnit test execution, and the coverage threshold for
exchange-rate and lookback assemblies.

## Governance

This constitution supersedes ad-hoc conventions for the Card Ledger API project.
Amendments require a pull request to `.specify/memory/constitution.md` with a
version bump per semantic versioning:

- **MAJOR**: Backward-incompatible principle removal or redefinition
- **MINOR**: New principle or section added, or materially expanded guidance
- **PATCH**: Clarifications, wording, or non-semantic refinements

Compliance MUST be reviewed at the plan gate, during pull request review, and
in a quarterly audit. Runtime development guidance is provided by this
constitution and `README.md`.

**Version**: 1.0.0 | **Ratified**: 2026-07-02 | **Last Amended**: 2026-07-02
