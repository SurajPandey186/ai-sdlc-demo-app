# Design

## Context

See `proposal.md` for motivation and `specs/invoice-approval-limits/spec.md` for required behavior. Today `ApprovalService.Approve` moves a pending invoice directly to Approved and `Invoice` stores one decision maker. The API session contains only a username and access role; `ApprovalsController` passes the username to the service. `ApiControllerBase` maps `InvalidOperationException` to HTTP 409, and the static UI displays the response's `error` field in its decision alert.

## Goals / Non-Goals

**Goals:**

- Enforce approval authority in the Domain service and preserve current authentication and endpoint access behavior.
- Record the first high-value approval while keeping the invoice pending, then complete it only after a distinct Director approves.
- Make every rejected attempt validation-only: no invoice fields or approval records change and the repository is not updated.
- Keep the change compatible with the in-memory demo; no persistent database migration is required.

**Non-Goals:**

- Replace Approver/Admin endpoint access roles with Manager/Director roles.
- Add a persistent audit store, approval delegation, or configurable thresholds.
- Change invoice submission, rejection, or deletion rules.

## Decisions

1. **Represent approval authority separately from access role.** Add a Domain `ApprovalAuthority` value (Manager or Director), configure it per `DemoUser`, and copy it into `UserSession`. Keep `[RequireRole("Approver", "Admin")]` for endpoint access and pass both username and authority to the Domain service. This avoids coupling business limits to web authorization roles. Alternative considered: reuse `UserSession.Role`; rejected because it would conflate access control and business authority.

   For the current demo accounts, assign `demo.approver` Manager authority and `demo.admin` Director authority while preserving their existing access roles. Treat these as demo mappings, not a rule that Admin users must always be Directors.

2. **Keep approval history on the invoice.** Add an ordered approval record containing username, authority, and timestamp. Preserve `DecidedBy` and `DecidedAt` as final-decision fields: they are set when an invoice becomes Approved, not when a high-value invoice receives only its first approval. A separate event store was considered but is unnecessary for this in-memory demonstration and would add infrastructure outside the assignment.

3. **Keep the policy and mutation ordering in `ApprovalService`.** The service checks pending status, amount, authority, and duplicate identity before changing the invoice. For amounts through 50,000, Manager or Director can complete approval; above 50,000 through 500,000, only Director can complete it. Above 500,000, either authority can record the first approval and the invoice remains pending; only a different Director can complete the second approval. This preserves the user's clarification that a Manager may be the first approver in the two-person path without allowing a Manager to complete a single-approver decision above 50,000.

4. **Use the existing error path for rejected attempts.** Throw a clear `InvalidOperationException` before mutation; the existing API base maps it to 409 with an `error` message, and the UI already presents that message. A new response contract was considered but is not needed for this feature.

5. **Test state preservation at both layers.** Domain tests compare all relevant invoice fields and approval records before and after rejected calls. The Playwright test signs in as the Manager demo account, attempts an over-limit approval, verifies the visible reason, reloads the UI, and confirms the invoice remains pending. Use page objects and accessible role/label locators as required by `AGENTS.md`.

## Risks / Trade-offs

- [Demo authority mapping could be mistaken for production policy] - Keep it explicit in demo configuration and separate from the access role; test the mapping at the API boundary.
- [An accepted first approval intentionally leaves a high-value invoice pending] - Expose the approval history in the invoice response and ensure the UI clearly communicates that another approval is required.
- [Concurrent approval requests could race because the current repository API has separate read and update operations] - The demo workflow and assignment tests are sequential; keep this limitation documented and do not claim concurrency guarantees. A production repository would need an atomic conditional update or transaction.
- [Existing clients may assume `DecidedBy` is set after any approval attempt] - Preserve its final-decision meaning and add a separate approvals collection; there are no persistent clients or stored records in this demo.

## Migration Plan

The repository is in-memory and seeded from configuration. Add authority values to the demo users and initialize approval history on invoices; no database migration is required. Revert the feature commits to roll back the change. Existing access roles and demo login credentials remain in place.

## Open Questions

None that affect this proposal. The first-approver eligibility for invoices above 500,000 was clarified by the user: Manager or Director may record the first approval; the second must be a distinct Director.
