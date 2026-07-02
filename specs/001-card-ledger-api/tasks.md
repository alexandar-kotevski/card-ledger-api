# Tasks: Card Ledger API

**Input**: Design documents from `/specs/001-card-ledger-api/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Per the project constitution, tests are MANDATORY for exchange-rate maths
and lookback logic (100% xUnit coverage gate). Tests are RECOMMENDED for all
other code. Include test tasks for gated modules in every feature plan.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/CardLedger.{Domain,Application,Infrastructure,Api}/`
- **Tests**: `tests/CardLedger.{Domain,Application,Integration}.Tests/`
- **Solution**: `CardLedger.slnx` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and tooling bootstrap

- [ ] T001 Create `CardLedger.slnx` with all src and test projects at repository root
- [ ] T002 Create `global.json` pinning .NET 10 SDK at repository root
- [ ] T003 Create `Directory.Build.props` with `TreatWarningsAsErrors`, nullable enable, and coverlet collector at repository root
- [ ] T004 [P] Create `src/CardLedger.Domain/CardLedger.Domain.csproj` targeting `net10.0` with C# 14
- [ ] T005 [P] Create `src/CardLedger.Application/CardLedger.Application.csproj` targeting `net10.0` referencing Domain
- [ ] T006 [P] Create `src/CardLedger.Infrastructure/CardLedger.Infrastructure.csproj` targeting `net10.0` referencing Application and Domain
- [ ] T007 [P] Create `src/CardLedger.Api/CardLedger.Api.csproj` targeting `net10.0` referencing Infrastructure
- [ ] T008 [P] Create `tests/CardLedger.Domain.Tests/CardLedger.Domain.Tests.csproj` with xUnit and coverlet
- [ ] T009 [P] Create `tests/CardLedger.Application.Tests/CardLedger.Application.Tests.csproj` with xUnit, NSubstitute, and coverlet
- [ ] T010 [P] Create `tests/CardLedger.Integration.Tests/CardLedger.Integration.Tests.csproj` with xUnit, Testcontainers.PostgreSql, and WebApplicationFactory
- [ ] T011 Add NuGet packages to Infrastructure: EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.Extensions.Http
- [ ] T012 Create multi-stage `Dockerfile` and `docker-compose.yml` with `postgres:18` and api on port 8080 at repository root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain, persistence, Treasury sync, and API shell that MUST be complete before ANY user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T013 [P] Create `src/CardLedger.Domain/ValueObjects/Money.cs` with decimal amount and currency code
- [ ] T014 [P] Create `src/CardLedger.Domain/ValueObjects/CurrencyCode.cs` with ISO 4217 validation
- [ ] T015 [P] Create `src/CardLedger.Domain/ValueObjects/Pan.cs` with 16-digit validation
- [ ] T016 [P] Create `src/CardLedger.Domain/ValueObjects/Cvv.cs` with 3-digit validation
- [ ] T017 [P] Create `src/CardLedger.Domain/Entities/Card.cs` per data-model.md
- [ ] T018 [P] Create `src/CardLedger.Domain/Entities/Ledger.cs` per data-model.md
- [ ] T019 [P] Create `src/CardLedger.Domain/Entities/Transaction.cs` per data-model.md
- [ ] T020 [P] Create `src/CardLedger.Domain/Entities/ExchangeRate.cs` per data-model.md
- [ ] T021 [P] Create domain exceptions in `src/CardLedger.Domain/Exceptions/` (ExchangeRateNotFoundException, InsufficientBalanceException, InvalidCardCredentialsException, CardExpiredException, CardNotFoundException)
- [ ] T022 [P] Create `src/CardLedger.Domain/Services/CardNumberGenerator.cs` using RandomNumberGenerator for unique 16-digit PAN
- [ ] T023 [P] Create `src/CardLedger.Domain/Services/CvvHasher.cs` with SHA-256 hash and compare
- [ ] T024 [P] Create repository abstractions in `src/CardLedger.Application/Abstractions/` (ICardRepository, ILedgerRepository, ITransactionRepository, IExchangeRateRepository, ITreasuryRateClient, IUnitOfWork)
- [ ] T025 Create `src/CardLedger.Infrastructure/Persistence/CardLedgerDbContext.cs` with DbSets for Card, Ledger, Transaction, ExchangeRate
- [ ] T026 [P] Create EF entity configurations in `src/CardLedger.Infrastructure/Persistence/Configurations/` with decimal(18,4)/decimal(18,8) precision and unique indexes
- [ ] T027 Create initial EF migration in `src/CardLedger.Infrastructure/Persistence/Migrations/`
- [ ] T028 [P] Implement `src/CardLedger.Infrastructure/Persistence/Repositories/CardRepository.cs`
- [ ] T029 [P] Implement `src/CardLedger.Infrastructure/Persistence/Repositories/LedgerRepository.cs`
- [ ] T030 [P] Implement `src/CardLedger.Infrastructure/Persistence/Repositories/TransactionRepository.cs`
- [ ] T031 [P] Implement `src/CardLedger.Infrastructure/Persistence/Repositories/ExchangeRateRepository.cs`
- [ ] T032 Create `src/CardLedger.Infrastructure/Treasury/TreasuryRateClient.cs` calling Fiscal Data rates_of_exchange API
- [ ] T033 Create `src/CardLedger.Infrastructure/Treasury/TreasuryCurrencyMapper.cs` mapping ISO 4217 to country_currency_desc
- [ ] T034 Create `src/CardLedger.Infrastructure/Treasury/TreasuryRateSyncService.cs` with startup 6-month backfill and daily 00:00 UTC incremental sync
- [ ] T035 Create `src/CardLedger.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` registering DbContext, repositories, Treasury client, and sync service
- [ ] T036 Create `src/CardLedger.Api/Program.cs` with DI, problem details middleware, health checks, and deferred Kestrel listen until bootstrap sync completes
- [ ] T037 Create `src/CardLedger.Api/appsettings.json` with ConnectionStrings, TreasurySync:BaseUrl, and TreasurySync:DailyRunTimeUtc

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 — Issue Card (Priority: P1) 🎯 MVP

**Goal**: Integrators issue a card via `POST /api/cards`; system returns PAN, expiry, CVV, and initialises ledger

**Independent Test**: Submit issue request; verify 16-digit PAN, expiry (+3 years), CVV, currency, credit limit returned; ledger balance equals credit limit

- [ ] T038 [P] [US1] Create `src/CardLedger.Application/DTOs/IssueCardRequest.cs` with decimal creditLimit and currency
- [ ] T039 [P] [US1] Create `src/CardLedger.Application/DTOs/IssueCardResponse.cs` with cardNumber, expiryDate, cvv, currency, creditLimit
- [ ] T040 [US1] Implement `src/CardLedger.Application/Services/IssueCardService.cs` creating Card + Ledger atomically via IUnitOfWork
- [ ] T041 [US1] Create `src/CardLedger.Api/Endpoints/CardEndpoints.cs` mapping `POST /api/cards` per contracts/openapi.yaml
- [ ] T042 [US1] Register IssueCardService and CardEndpoints in `src/CardLedger.Api/Program.cs`
- [ ] T043 [US1] Add validation rejecting zero or negative creditLimit in `src/CardLedger.Application/Services/IssueCardService.cs`
- [ ] T044 [P] [US1] Add recommended unit tests in `tests/CardLedger.Application.Tests/IssueCardServiceTests.cs`

**Checkpoint**: User Story 1 fully functional — card issuance and ledger initialisation work independently

---

## Phase 4: User Story 2 — Purchase (Priority: P2)

**Goal**: Integrators record purchases via `POST /api/cards/transactions`; system validates credentials and debits ledger

**Independent Test**: Issue card, submit valid purchase; verify transaction Guid returned and ledger balance reduced; invalid credentials and insufficient balance rejected

- [ ] T045 [P] [US2] Create `src/CardLedger.Application/DTOs/PurchaseRequest.cs` with cardNumber, expiryDate, cvv, amount, currency, description
- [ ] T046 [P] [US2] Create `src/CardLedger.Application/DTOs/PurchaseResponse.cs` with id, amount, currency, description
- [ ] T047 [US2] Implement `src/CardLedger.Application/Services/CurrencyConversionService.cs` using latest cached rate for purchase debit
- [ ] T048 [US2] Implement `src/CardLedger.Application/Services/PurchaseService.cs` with credential validation, FX debit, and transaction persist
- [ ] T049 [US2] Create `src/CardLedger.Api/Endpoints/TransactionEndpoints.cs` mapping `POST /api/cards/transactions` per contracts/openapi.yaml
- [ ] T050 [US2] Register PurchaseService and purchase endpoint in `src/CardLedger.Api/Program.cs`
- [ ] T051 [US2] Handle edge cases in PurchaseService: expired card, invalid CVV, insufficient balance, zero-amount purchase in `src/CardLedger.Application/Services/PurchaseService.cs`
- [ ] T052 [P] [US2] Add recommended unit tests in `tests/CardLedger.Application.Tests/PurchaseServiceTests.cs`

**Checkpoint**: User Stories 1 AND 2 work — cards can be issued and purchases recorded

---

## Phase 5: User Story 3 — Retrieve Transactions with FX (Priority: P3)

**Goal**: Integrators retrieve transactions with optional currency conversion using 6-month Treasury lookback

**Independent Test**: Issue card, make purchases, retrieve with targetCurrency; verify converted amounts; missing rate returns 422 with problem details

### Tests for User Story 3 (MANDATORY — constitution 100% coverage gate)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T053 [P] [US3] Create `tests/CardLedger.Application.Tests/ExchangeRate/ExchangeRateLookbackServiceTests.cs` with NSubstitute mocking IExchangeRateRepository (boundary, missing rate, multiple rates, same currency)
- [ ] T054 [P] [US3] Create `tests/CardLedger.Application.Tests/ExchangeRate/CurrencyConversionTests.cs` for decimal arithmetic and rounding
- [ ] T055 [US3] Verify 100% coverlet line and branch coverage gate passes for lookback namespace in `tests/CardLedger.Application.Tests/`

### Implementation for User Story 3

- [ ] T056 [US3] Implement `src/CardLedger.Application/Services/ExchangeRateLookbackService.cs` with 6-month inclusive lookback and latest-rate path
- [ ] T057 [P] [US3] Create `src/CardLedger.Application/DTOs/TransactionDetailDto.cs` per contracts/openapi.yaml
- [ ] T058 [US3] Implement `src/CardLedger.Application/Services/TransactionQueryService.cs` for list and single transaction with optional FX
- [ ] T059 [US3] Add `GET /api/cards/{cardNumber}/transactions` and `GET /api/cards/{cardNumber}/transactions/{guid}` to `src/CardLedger.Api/Endpoints/TransactionEndpoints.cs`
- [ ] T060 [US3] Map ExchangeRateNotFoundException to 422 problem details with card/transaction/currency/date context in `src/CardLedger.Api/Program.cs`

**Checkpoint**: Transaction retrieval with FX conversion works; lookback tests pass at 100% coverage

---

## Phase 6: User Story 4 — Retrieve Available Balance (Priority: P4)

**Goal**: Integrators query ledger-backed available balance converted to target currency via latest Treasury rate

**Independent Test**: Issue card, make purchases, request balance in target currency; verify ledger amount converted using latest rate

- [ ] T061 [P] [US4] Create `src/CardLedger.Application/DTOs/BalanceResponse.cs` per contracts/openapi.yaml
- [ ] T062 [US4] Implement `src/CardLedger.Application/Services/BalanceService.cs` reading ledger and converting via latest rate
- [ ] T063 [US4] Create `src/CardLedger.Api/Endpoints/BalanceEndpoints.cs` mapping `GET /api/cards/{cardNumber}/balance` per contracts/openapi.yaml
- [ ] T064 [US4] Register BalanceService and BalanceEndpoints in `src/CardLedger.Api/Program.cs`
- [ ] T065 [US4] Map missing latest rate to 422 problem details in BalanceService error path

**Checkpoint**: All four user stories independently functional

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Integration tests, coverage enforcement, documentation, and end-to-end validation

- [ ] T066 [P] Create E2E test in `tests/CardLedger.Integration.Tests/CardLedgerFlowTests.cs` (issue → purchase → balance) using WebApplicationFactory and Testcontainers PostgreSQL 18
- [ ] T067 [P] Create lookback failure integration test in `tests/CardLedger.Integration.Tests/ExchangeRateFailureTests.cs` expecting 422
- [ ] T068 Configure coverlet 100% threshold for Application lookback assembly in `Directory.Build.props`
- [ ] T069 Run full test suite with coverage gate via `dotnet test CardLedger.slnx --collect:"XPlat Code Coverage"`
- [ ] T070 Validate quickstart scenarios A–H from `specs/001-card-ledger-api/quickstart.md` using Docker Compose
- [ ] T071 Update `README.md` with build, test, and Docker run instructions
- [ ] T072 Perform final constitution compliance review against `.specify/memory/constitution.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational — MVP increment
- **User Story 2 (Phase 4)**: Depends on Foundational + US1 (cards must exist)
- **User Story 3 (Phase 5)**: Depends on Foundational + US2 (transactions for FX retrieval); lookback unit tests use mocks independently
- **User Story 4 (Phase 6)**: Depends on Foundational + US1 + US2 (ledger with debits)
- **Polish (Phase 7)**: Depends on all user stories

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational — no dependencies on other stories
- **US2 (P2)**: Requires US1 for realistic flow; independently testable with seeded card data
- **US3 (P3)**: Requires US2 for E2E; lookback service unit-testable with NSubstitute before US2 completes
- **US4 (P4)**: Requires US1 + US2 for meaningful balance queries

### Within Each User Story

- Mandatory lookback tests (US3) MUST be written and FAIL before ExchangeRateLookbackService implementation
- Application services before API endpoints
- Endpoints registered in Program.cs after service implementation

### Parallel Opportunities

- Phase 1: T004–T010 (all project creation tasks) in parallel
- Phase 2: T013–T024 (domain value objects, entities, exceptions) in parallel; T028–T031 (repositories) in parallel after T025–T027
- US1: T038–T039 (DTOs) in parallel
- US2: T045–T046 (DTOs) in parallel
- US3: T053–T054 (mandatory tests) in parallel before T056
- Phase 7: T066–T067 (integration tests) in parallel

---

## Parallel Example: User Story 3

```bash
# Launch mandatory lookback tests first (must fail before implementation):
Task: "Create tests/CardLedger.Application.Tests/ExchangeRate/ExchangeRateLookbackServiceTests.cs"
Task: "Create tests/CardLedger.Application.Tests/ExchangeRate/CurrencyConversionTests.cs"

# Then implement lookback service:
Task: "Implement src/CardLedger.Application/Services/ExchangeRateLookbackService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Issue a card via `POST /api/cards`; confirm ledger initialised
5. Demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 → Test independently → Demo (MVP)
3. Add US2 → Test independently → Demo
4. Add US3 → Verify 100% lookback coverage → Demo
5. Add US4 → Test independently → Demo
6. Polish → Full quickstart validation

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1 → US2
   - Developer B: US3 lookback tests + service (mock-based, parallel with US2)
   - Developer C: US4 after US2 completes
3. Team converges on Phase 7 integration tests

---

## Notes

- All monetary fields MUST use `decimal` — never `float`/`double`/`Half`
- Treasury bootstrap sync (6-month backfill) runs before API accepts traffic
- OpenAPI contract: `specs/001-card-ledger-api/contracts/openapi.yaml` (3.2.0)
- Solution file is `CardLedger.slnx` (not `.sln`)
- Commit after each task or logical group; stop at any checkpoint to validate story independently
