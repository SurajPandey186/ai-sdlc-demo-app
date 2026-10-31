# Spec Delta

## Purpose

Defines which approval authorities can decide invoices at each amount and how high-value invoices require distinct approvers. It also specifies the observable rejection behavior when an approval attempt is not permitted.

## ADDED Requirements

### Requirement: Single-approver amount limits
The system SHALL allow a Manager to complete a single-approver decision only for invoices up to and including 50,000, and a Director to complete a single-approver decision only for invoices up to and including 500,000. Invoices above 500,000 SHALL use the two-approver process.

#### Scenario: Manager completes approval at the Manager limit
- **WHEN** a Manager approves a pending invoice for 50,000
- **THEN** the invoice becomes Approved and records that Manager as the decision maker

#### Scenario: Manager exceeds the single-approver limit
- **WHEN** a Manager attempts a single-approver decision for an invoice above 50,000 and not above 500,000
- **THEN** the attempt is rejected with a clear reason and the invoice remains unchanged

#### Scenario: Director completes approval at the Director limit
- **WHEN** a Director approves a pending invoice for 500,000
- **THEN** the invoice becomes Approved and records that Director as the decision maker

#### Scenario: Invoice above the Director limit starts the two-approver process
- **WHEN** a Manager or Director records the first approval for a pending invoice above 500,000
- **THEN** the invoice remains PendingApproval and records the first approver without marking the invoice Approved

### Requirement: Distinct approvers for high-value invoices
The system SHALL require two approvals by different users to approve an invoice above 500,000, and the second approver SHALL have Director authority.

#### Scenario: Distinct Director completes high-value approval
- **GIVEN** a pending invoice above 500,000 has a recorded first approval
- **WHEN** a different user with Director authority records the second approval
- **THEN** the invoice becomes Approved and retains both approvers in its approval record

#### Scenario: First approver cannot approve the same invoice twice
- **GIVEN** a pending invoice above 500,000 has a recorded first approval by a user
- **WHEN** that same user attempts the second approval
- **THEN** the attempt is rejected with a clear reason and all invoice data remains unchanged

#### Scenario: Non-Director cannot complete high-value approval
- **GIVEN** a pending invoice above 500,000 has a recorded first approval
- **WHEN** a different Manager attempts the second approval
- **THEN** the attempt is rejected with a clear reason and all invoice data remains unchanged

### Requirement: Rejected approval attempts preserve invoice state
When the system rejects an approval attempt, it SHALL return a clear reason and SHALL NOT change the invoice status, recorded approvals, decision fields, or timestamps.

#### Scenario: Rejection preserves an invoice before any approval is recorded
- **GIVEN** a pending invoice has no recorded approval
- **WHEN** an authority attempts an approval that exceeds its permitted single-approver limit
- **THEN** the response explains why the attempt was rejected and the invoice remains identical to its prior state

#### Scenario: Rejection preserves a high-value invoice's first approval
- **GIVEN** a pending invoice above 500,000 has one recorded approval
- **WHEN** an ineligible user attempts the second approval
- **THEN** the response explains why the attempt was rejected and the invoice remains identical to its prior state, including the first approval
