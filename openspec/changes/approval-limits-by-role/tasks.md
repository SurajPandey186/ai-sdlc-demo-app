# Tasks

## 1. Context and specification baseline

- [x] 1.1 Fork the reference repository and create a feature branch; verify `git remote -v` points to the user's fork and `git status` shows the working tree before feature implementation.
- [x] 1.2 Commit the updated `AGENTS.md` and OpenSpec artifacts as a documentation-only commit; verify with `git show --stat --oneline HEAD` that it contains the guidance and proposal files but no application code.
- [x] 1.3 Configure a supported agent hook to capture structured AI interaction records in a consistent format in `docs/ai-interaction-log.md`; verify at least one generated entry matches the agreed format.
- [x] 1.4 Install/use `dotnet-stryker` version 4.13.0 and capture the pre-feature mutation score; verify the baseline output is saved as text for `ASSIGNMENT.md`.

## 2. Stage one: single-approver authority limits

- [x] 2.1 Add Manager/Director approval authority separately from the existing Approver/Admin access role, configure the demo mappings, and pass the authority through the authenticated session; verify the API still enforces existing access roles and tests confirm each demo authority mapping.
- [x] 2.2 Enforce single-approver limits in the Domain service and add boundary tests for Manager at 50,000, Manager above 50,000 through 500,000, and Director at 500,000; verify `dotnet test backend/InvoiceApp.sln` passes.
- [x] 2.3 Add rejection tests that snapshot status, approval records, decision fields, timestamps, and repository update behavior; verify rejected over-limit calls return a clear reason and leave every snapshotted value unchanged.
- [x] 2.4 Update the AI interaction log for this implementation stage and commit the single-approver implementation and its tests separately; verify the commit contains no two-approver workflow and the full unit test command passes.

## 3. Stage two: distinct Director second approval

- [x] 3.1 Add ordered approval records and implement the high-value first-approval state, preserving `PendingApproval`; verify Manager and Director first approvals are recorded without setting final decision fields.
- [x] 3.2 Add tests for a distinct Director completing approval, the same user being rejected as second approver, and a different Manager being rejected as second approver; verify all rejected attempts preserve the invoice and its first-approval record.
- [x] 3.3 Update the AI interaction log for this stage and commit the two-approver workflow and its tests separately; verify `dotnet test backend/InvoiceApp.sln` passes and the commit is distinct from stage one.

## 4. UI test and assignment evidence

- [ ] 4.1 Add a page object and one Playwright test that signs in as the Manager, attempts an over-limit approval through the UI, checks the visible reason, reloads, and confirms the invoice is still pending; verify `npm test` passes from `e2e`.
- [ ] 4.2 Run the required mutation test after implementation with Stryker 4.13.0 and record the post-feature score alongside the baseline; verify both score outputs are present as text in the write-up.
- [ ] 4.3 Push the completed commit sequence to the fork and open a pull request; verify the PR contains at least three independent commits and the expected feature/spec files.
- [ ] 4.4 Use focused sub-agents and appropriate review skills to inspect generated unit tests and the PR diff; record prompts/findings in the interaction log, assign every finding Fixed, Dismissed with a reason, or Deferred with a reason, and put the complete verdict list in the PR description.
- [ ] 4.5 Create `ASSIGNMENT.md` with the PR link, commit sequence, spec links, test results, mutation scores, review verdicts, AI interaction log, and demo link; verify every required hand-in item from `Requirement.Doc.txt` is accounted for.
- [ ] 4.6 Record an 8-to-10-minute demo with a live test, mutation run, or Playwright run and link the uploaded video from `ASSIGNMENT.md`; verify the video duration and that its link is accessible to the intended reviewers.
- [ ] 4.7 Run the final unit, Playwright, and OpenSpec validation commands and record their outcomes; verify all checks pass before finalizing the PR.
