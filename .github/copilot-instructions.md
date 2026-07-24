# Copilot Instructions

## Project Guidelines
- Use English consistently for code comments and user-facing output strings; avoid mixed-language text.
- UI component choice is flexible as long as the stack is free/open-source for everyone; MudBlazor is only an initial suggestion, not a strict requirement.
- Blazor.Extensions.Canvas is approved as an optional free component for custom visualizations if needed.

## Testing Guidelines
- For xUnit in this project, long-running tests must be excluded from default test runs and executed only when explicitly required, as individual long-running tests may take minutes to up to one hour.
- Long calculation tests should be skipped by default and only run in an explicit long-running test mode.
- **Performance optimization:** When writing or modifying tests that contain many independent loop iterations (e.g., seed sweeps, parameter scans, variant comparisons), use `Parallel.For`, `Parallel.ForEach`, or `Parallel.Invoke` to distribute work across CPU cores. Collect results in thread-safe collections (`ConcurrentDictionary`, `ConcurrentBag`), then output sequentially after parallel work completes. This is especially important for tests tagged `LongRunning`.
- TRM.Tests is intentionally used as a fast calculation/audit harness rather than a classic unit-test suite; many green assertions (including `Assert.True(true)`) are intentional and not meant as strict behavioral verification.

## Shared Code Organization
- **Always prefer shared helper classes over inline helpers in test files.** When writing new tests or adding utility methods (records, enums, static helpers, evaluation functions, sweep runners, statistical methods), place them in the appropriate shared helper class rather than defining them directly in the test file.
- For V7 tests, use `V7_TestHelpers.cs` (static class `V7TestHelpers` in namespace `TRM.Tests.V7_3_and_4`) via `using static TRM.Tests.V7_3_and_4.V7TestHelpers;`.
- When a helper is only used by a single test, it may be defined as a local static function within that test method. If it is used by two or more tests, move it to the shared helper class.
- Keep test files focused on test logic only. Split large test files (>2000 lines) into separate test classes organized by topic for parallel execution and maintainability.

## Documentation Maintenance

The following documentation files must be kept up to date at all times:
- `docs/TRM_Current_Frontier.md`
- `docs/TRM_Project_Lineage_Overview.md` — Authoritative historical overview of the entire project.
- `docs/TRM_Project_QuickStart_For_New_Chats.md` — Short operational briefing for new chats and reviewers.



**Update rule:** After any session that changes project status (new findings, completed phases, test count changes, claim status updates, branch completions, new hypotheses, or changed open problems), review both documents and update any stale information. At minimum, review these documents at the start of every session if more than 24 hours have passed since last update.

**What to check:**
- Version status and branch name
- Test counts (total, passed, failed)
- Supported / Conditional / Hypothesis / Not Claimed items
- Current research frontier description
- Open problems list
- Development statistics table
- One-page snapshot

**Do not** modify theory content, introduce new claims, or alter historical descriptions without explicit evidence from branch completion reports.