# Feature Specification: Card Ledger API

**Feature Branch**: `001-card-ledger-api`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Define a Card Ledger API feature with four criteria: (1) Create/Issue Card with credit limit and currency, returning PAN, expiry, CVV; (2) Store/Purchase transactions with card validation; (3) Retrieve Transactions with Currency Conversion using Treasury API and strict 6-month historical lookback; (4) Retrieve Available Balance from ledger, converted to target currency using latest exchange rate."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Issue Card (Priority: P1)

An integrator issues a new card by submitting a credit limit and currency. The
system generates card credentials, returns them to the integrator, and
initialises a ledger record for the card.

**Why this priority**: Card issuance is the foundation — no card exists without
it, and the ledger must be created before any purchases or balance queries can
occur.

**Independent Test**: Submit an issue request with a credit limit and currency;
verify the response contains a 16-digit card number, expiry date, CVV, currency,
and credit limit; confirm a ledger record exists with available balance equal
to the credit limit.

**Acceptance Scenarios**:

1. **Given** a valid credit limit and currency, **When** an integrator issues a card, **Then** the response includes a 16-digit card number, expiry date (3 years from issue by default), a random 3-digit CVV, currency, and credit limit.
2. **Given** a successful card issue, **When** the ledger is queried, **Then** a ledger record exists for the card with available balance equal to the credit limit in the card currency.
3. **Given** a credit limit of zero or negative value, **When** an issue is attempted, **Then** the system rejects the request with a clear error.

---

### User Story 2 - Purchase (Priority: P2)

A cardholder (via an integrator) makes a purchase by submitting card
credentials, an amount, currency, and description. The system validates the
card, records the transaction, and updates the ledger.

**Why this priority**: Purchases are the primary business action once a card
exists; they drive transaction history and ledger balance changes.

**Independent Test**: Issue a card, submit a purchase with valid credentials,
verify the response returns a transaction identifier, amount, currency, and
description; confirm the transaction is persisted and the ledger balance is
reduced.

**Acceptance Scenarios**:

1. **Given** a valid issued card, **When** a purchase is submitted with card number, expiry date, CVV, amount, currency, and description, **Then** the response returns a transaction identifier, amount, currency, and description.
2. **Given** a successful purchase, **When** the transaction is stored, **Then** the transaction datetime defaults to the current time.
3. **Given** a successful purchase, **When** the ledger is queried, **Then** the available balance has been reduced by the purchase amount (converted to card currency if the purchase currency differs).
4. **Given** an invalid card number, expiry date, or CVV, **When** a purchase is attempted, **Then** the system rejects the request with a clear error.
5. **Given** a purchase amount exceeding the ledger available balance, **When** the purchase is attempted, **Then** the system rejects the request with a clear error.

---

### User Story 3 - Retrieve Transactions with Currency Conversion (Priority: P3)

An integrator retrieves all transactions for a card, with each transaction
amount optionally converted to a requested target currency using Treasury
exchange rates.

**Why this priority**: Transaction history with accurate currency conversion is
essential for reporting and reconciliation; it depends on cards and purchases
being in place first.

**Independent Test**: Issue a card, make one or more purchases, retrieve
transactions with a target currency; verify converted amounts match expected
values using Treasury rates within the 6-month lookback window.

**Acceptance Scenarios**:

1. **Given** a card with stored transactions, **When** transactions are retrieved with a target currency, **Then** each transaction amount is converted using the Treasury rate on or before the transaction date within a strict 6-month lookback window.
2. **Given** no Treasury rate exists within the 6-month lookback window for a transaction, **When** transactions are retrieved with currency conversion, **Then** the system fails with a precise, identifiable error describing the card, transaction, currency, and date context.
3. **Given** multiple Treasury rates exist on or before a transaction date within the lookback window, **When** conversion is performed, **Then** the most recent applicable rate is used.
4. **Given** a card with no transactions, **When** transactions are retrieved, **Then** an empty list is returned.

---

### User Story 4 - Retrieve Available Balance (Priority: P4)

An integrator checks the remaining spend capacity for a card in a requested
target currency. The balance is read from the ledger and converted using the
latest available Treasury exchange rate.

**Why this priority**: Available balance enables spend-authorisation decisions;
it depends on the ledger being maintained through issue and purchase operations.

**Independent Test**: Issue a card, make purchases, request available balance
in a target currency; verify the returned balance matches the ledger value
converted using the latest Treasury rate.

**Acceptance Scenarios**:

1. **Given** a card with a ledger record, **When** available balance is requested for a target currency, **Then** the system returns the ledger-stored available balance converted using the latest available Treasury exchange rate.
2. **Given** a newly issued card with no purchases, **When** available balance is requested, **Then** the returned balance equals the credit limit (converted to the target currency if different from the card currency).
3. **Given** no latest Treasury rate exists for the required currency pair, **When** available balance is requested, **Then** the system fails with a precise, identifiable error.
4. **Given** a card that has had multiple purchases, **When** available balance is requested, **Then** the returned balance reflects all prior purchase debits recorded in the ledger.

---

### Edge Cases

**Issue and purchase**

- What happens when a purchase is attempted with an expired card (expiry date in the past)?
- How does the system handle an invalid, non-existent, or duplicate card number?
- What happens when the submitted CVV or expiry date does not match the issued card?
- How does the system handle a purchase that exceeds the ledger available balance?
- What happens when a purchase is submitted in a currency different from the card currency?
- How does the system handle a zero-amount purchase (valid entry with no ledger impact)?
- What happens if the system generates a duplicate 16-digit card number?

**Currency conversion and balance**

- What happens when a transaction date falls exactly on the 6-month lookback boundary (inclusive: a rate on the boundary date is valid)?
- How does the system handle a target currency that matches the card currency (no conversion needed)?
- What happens when the Treasury service is unavailable or returns no rates?
- How does the system handle multiple transactions on the same date, each requiring independent lookback?
- What happens when no latest Treasury rate is available for balance conversion (distinct from historical lookback failure)?

## Requirements *(mandatory)*

### Constitution Constraints (mandatory for this project)

- Monetary amounts, limits, and balances MUST be specified as decimal-precision values (no floating-point)
- Exchange-rate and lookback behaviour MUST be independently testable with xUnit
- Feature design MUST respect Domain/Application/Infrastructure/Api separation

### Functional Requirements

**Issue and ledger initialisation**

- **FR-001**: System MUST expose an issue operation accepting credit limit (decimal) and currency.
- **FR-002**: On successful issue, system MUST return a 16-digit card number, expiry date (default 3 years from issue), random 3-digit CVV, currency, and credit limit.
- **FR-003**: On successful issue, system MUST create a ledger record for the card with available balance equal to the credit limit.

**Purchase and ledger update**

- **FR-004**: System MUST expose a purchase operation accepting card number, expiry date, CVV, amount (decimal), currency, and description.
- **FR-005**: On successful purchase, system MUST return transaction identifier, amount, currency, and description; transaction datetime MUST default to current time.
- **FR-006**: On successful purchase, system MUST persist the transaction and update the card's ledger available balance.
- **FR-007**: System MUST validate card number, expiry date, and CVV before accepting a purchase; invalid credentials MUST be rejected.
- **FR-008**: System MUST reject purchases that exceed the ledger available balance.

**Transaction retrieval with currency conversion**

- **FR-009**: System MUST retrieve all transactions for a card, optionally expressing each amount in a requested target currency.
- **FR-010**: For transaction currency conversion, system MUST obtain exchange rates from the Treasury service using a strict 6-month historical lookback on or before each transaction's effective date.
- **FR-011**: When no Treasury rate exists within the 6-month lookback window for a transaction, system MUST fail with a precise domain exception (not a generic error).

**Available balance**

- **FR-012**: System MUST return available balance from the ledger record, expressed in a requested target currency.
- **FR-013**: For available balance conversion, system MUST use the latest available Treasury exchange rate (not the per-transaction historical lookback rule).

**Cross-cutting**

- **FR-014**: All monetary values (limits, amounts, balances, converted totals) MUST use decimal precision — no floating-point types.
- **FR-015**: Exchange-rate lookback and conversion logic MUST be independently verifiable.

### Key Entities *(include if feature involves data)*

- **Card**: Represents an issued payment card. Attributes: 16-digit card number (PAN), expiry date, CVV, credit limit (decimal + currency). Linked to one Ledger record and many Transactions.
- **Ledger**: Represents the available spend balance for a card. One record per card. Attributes: available balance (decimal + currency). Created on card issue; updated on each successful purchase.
- **Transaction**: Represents a purchase entry. Attributes: unique identifier, description, effective datetime, amount (decimal + currency). Belongs to one Card.
- **ExchangeRate**: Logical representation of a currency conversion rate sourced from the Treasury service. Attributes: source currency, target currency, rate (decimal), effective date.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Integrators can issue a card and receive card number, expiry, CVV, currency, and credit limit within 2 seconds under normal load.
- **SC-002**: Integrators can complete a purchase with valid card credentials and receive a transaction identifier within 2 seconds under normal load.
- **SC-003**: Ledger available balance is always consistent with issue and purchase history (initialised on issue, decremented on each purchase).
- **SC-004**: 100% of stored transactions are retrievable with original description, datetime, and amount intact.
- **SC-005**: Currency-converted transaction retrieval produces deterministic results for the same inputs and Treasury rate data.
- **SC-006**: When no rate exists within the 6-month lookback window, integrators receive an explicit, actionable error — never a silent fallback or incorrect conversion.
- **SC-007**: Available balance read from the ledger is correctly converted to the requested currency using the latest Treasury rate.
- **SC-008**: All four capabilities operate correctly in combination (issue → purchase → retrieve → balance).

## Assumptions

- Ledger available balance is maintained in the card's issue currency; purchases in a different currency are converted to the card currency (using the latest Treasury rate) before debiting the ledger.
- On card issue, ledger available balance is set equal to the credit limit in the card currency.
- Card number (PAN) is a system-generated unique 16-digit number; CVV is a random 3-digit value returned at issue and validated on subsequent purchases.
- Expiry date defaults to issue date plus 3 years.
- Purchase amounts are positive spends that reduce ledger available balance; refunds and reversals are out of scope.
- Treasury API is an external rate provider supplying historical and latest exchange rates by currency pair and date.
- Integrators and downstream services are the primary actors; no end-user interface is in scope.
- Within the lookback window, the most recent Treasury rate on or before the transaction date is used for conversion.
- Authentication and authorisation are out of scope for this feature.
- Card lifecycle management beyond issue (update, close, freeze, reissue) is out of scope.
- Transaction reversal, editing, and refunds are out of scope.
- Pagination and filtering of transaction lists are out of scope.
