# Proposal

## Why

The invoice service currently allows any authenticated approver to approve any pending invoice alone. The assignment requires authority-based limits and a two-person control for high-value invoices, with rejected attempts leaving the invoice untouched.

## What Changes

- Add Manager and Director approval authorities while retaining Approver/Admin as endpoint access roles.
- Managers may complete single-approver approvals through 50,000; Directors may complete them through 500,000.
- Invoices above 500,000 require two approvals from different users. A Manager or Director may make the first approval; a distinct Director must make the second approval for completion.
- Return a clear reason for an ineligible approval attempt without changing invoice status, recorded approvals, decision fields, or timestamps.
- Add focused domain tests and one Playwright test for a rejected over-limit approval through the UI.

## Capabilities

### New Capabilities

- `invoice-approval-limits`: Defines authority-based approval thresholds, high-value two-person approval, and no-mutation rejection behavior.

### Modified Capabilities

None.

## Impact

- Domain: `ApprovalService` and `Invoice` need to represent authority and multiple approval records.
- API/authentication: authenticated user data must convey approval authority separately from the existing access role; approval endpoints must pass that data to the domain service.
- Demo configuration and static UI: demo identities need authorities, and the UI must display a clear rejection reason.
- Tests: expand `ApprovalServiceTests` and add an E2E page object/test under `e2e`.
- Assignment artifacts: retain the staged commits, interaction log, mutation results, PR review verdicts, and demo evidence required by `Requirement.Doc.txt`.
