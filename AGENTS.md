# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only, approvals are Approver or Admin.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)

## Approval-limit feature rules

- Approval authority is distinct from endpoint access roles:
  - For a single-approver decision, Managers may approve invoices up to and including 50,000; Directors may approve invoices up to and including 500,000.
- An invoice above 500,000 requires two approvals from distinct users.
- For an invoice above 500,000, a Manager or Director may record the first, non-final approval; the invoice remains pending until a different Director records the second approval.
- The second approval for an invoice above 500,000 must be from a Director.
- A user must never count as both approvers for the same invoice.
- A rejected approval attempt must return a clear reason and must not change invoice status, recorded approvals, decision fields, or timestamps.
- Keep these rules in the Domain project and `ApprovalService`; controllers only pass authenticated-user data and map results.
- Tests must cover both limits, exact boundaries, distinct approvers, an invalid second approver, and unchanged state after every rejected attempt.
- Review all AI-generated tests and pull-request findings. Do not accept generated output without checking the rule it proves.

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
