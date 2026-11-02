# AI in SDLC Assignment — Submission Details

## Pull request and commit sequence

- Pull request: [#1 — Feature/approval limits](https://github.com/SurajPandey186/ai-sdlc-demo-app/pull/1)
- Fork branch: `SurajPandey186:feature/approval-limits`

1. [`620aead`](https://github.com/NITIN1670/ai-sdlc-demo-app/commit/620aead927ef84c84e2033baf9540656685bab9a) — OpenSpec and project guidance
2. [`f58c4a2`](https://github.com/NITIN1670/ai-sdlc-demo-app/commit/f58c4a2609d61e87889b968b01010c967399dd35) — Single-approver authority limits
3. [`583df7c`](https://github.com/NITIN1670/ai-sdlc-demo-app/commit/583df7c9c08e8730f81e721e6f4bfb957e3f2350) — Distinct Director second approval
4. [`b5692cc`](https://github.com/NITIN1670/ai-sdlc-demo-app/commit/b5692cc4b8f0c0392a73c4993e436778c73ecd48) — UI, Playwright, and mutation evidence
5. `f9bab63` — Pull-request evidence

## Specification and context

- [Agent guidance](AGENTS.md)
- [OpenSpec change](openspec/changes/approval-limits-by-role/)
- [Design](openspec/changes/approval-limits-by-role/design.md)
- [Specification](openspec/changes/approval-limits-by-role/specs/invoice-approval-limits/spec.md)
- [Task checklist](openspec/changes/approval-limits-by-role/tasks.md)

## Feature delivered

- Managers approve invoices through 50,000; Directors through 500,000.
- Invoices above 500,000 require two approvals from distinct users.
- A Manager or Director may record the first high-value approval; a different Director must record the second.
- Rejected attempts return a clear reason without changing invoice state.

## Verification

- `dotnet test backend/InvoiceApp.sln`: 24 passed, 0 failed.
- `npm test` from `e2e`: 2 passed.
- Stryker.NET 4.13.0 baseline: 30.77%.
- Stryker.NET 4.13.0 post-feature: 58.44%.
- `openspec.cmd validate approval-limits-by-role --strict`: valid.
- Detailed mutation evidence: [docs/mutation-baseline.txt](docs/mutation-baseline.txt).

## PR review verdicts

| Finding | Verdict | Reason |
| --- | --- | --- |
| Approval-domain and generated-unit-test review found no concrete correctness defect. | Dismissed | Review confirmed exact thresholds, ordered high-value approvals, distinct-user enforcement, and no-mutation rejection tests. |
| UI and Playwright review found no concrete defect. | Dismissed | The UI derives progress from API data only; page objects use role/label locators; tests cover the required rejection/reload flow. |
| The PR description initially lacked explicit review verdicts. | Fixed | PR #1 now contains the full verdict table. |
| Repository-root `ASSIGNMENT.md` was missing. | Fixed | This file supplies the required repository-root write-up. |
| The linked demo video is 5:54, below the required 8–10 minutes. | Deferred | A compliant replacement recording must be uploaded before final submission. |

## AI interaction evidence

- [AI interaction log](docs/ai-interaction-log.md)
- Hook configuration: [.codex/hooks.json](.codex/hooks.json)

## Demo video

- Current video: https://www.youtube.com/watch?v=D1OFUfrMEGY
- Status: replacement required — the current video is 5:54; the assignment requires an accessible 8–10 minute recording with a live test, mutation run, or Playwright run.
