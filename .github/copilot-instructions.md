# Copilot Instructions

## Project Guidelines
- Use English consistently for code comments and user-facing output strings; avoid mixed-language text.
- UI component choice is flexible as long as the stack is free/open-source for everyone; MudBlazor is only an initial suggestion, not a strict requirement.
- Blazor.Extensions.Canvas is approved as an optional free component for custom visualizations if needed.

## Testing Guidelines
- For xUnit in this project, long calculation tests should be skipped by default and only run in an explicit long-running test mode.
- **Performance optimization:** When writing or modifying tests that contain many independent loop iterations (e.g., seed sweeps, parameter scans, variant comparisons), use `Parallel.For`, `Parallel.ForEach`, or `Parallel.Invoke` to distribute work across CPU cores. Collect results in thread-safe collections (`ConcurrentDictionary`, `ConcurrentBag`), then output sequentially after parallel work completes. This is especially important for tests tagged `LongRunning`.

## Documentation Maintenance

The following documentation files must be kept up to date at all times:

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