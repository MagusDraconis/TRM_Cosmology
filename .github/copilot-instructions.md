# Copilot Instructions

## Project Guidelines
- Use English consistently for code comments and user-facing output strings; avoid mixed-language text.
- UI component choice is flexible as long as the stack is free/open-source for everyone; MudBlazor is only an initial suggestion, not a strict requirement.
- Blazor.Extensions.Canvas is approved as an optional free component for custom visualizations if needed.

## Testing Guidelines
- For xUnit in this project, long-running tests must be excluded from default test runs and executed only when explicitly required, as individual long-running tests may take minutes to up to one hour.
- Long calculation tests should be skipped by default and only run in an explicit long-running test mode.
- **Performance optimization:** When writing or modifying tests that contain many independent loop iterations (e.g., seed sweeps, parameter scans, variant comparisons), use `Parallel.For`, `Parallel.ForEach`, or `Parallel.Invoke` to distribute work across CPU cores. Collect results in thread-safe collections (`ConcurrentDictionary`, `ConcurrentBag`), then output sequentially after parallel work completes. This is especially important for tests tagged `LongRunning`.

  **Parallel output pattern (preferred):** Never use `lock(_o) _o.WriteLine(...)` inside parallel loops — this serializes work and causes contention. Instead, collect output strings into a `ConcurrentDictionary<int, string>` keyed by `Interlocked.Increment(ref counter)`, then emit in order after parallel work completes:
  ```csharp
  var outputRows = new ConcurrentDictionary<int, string>();
  int rowIdx = 0;
  Parallel.ForEach(items, item => {
      // ... compute ...
      int r = Interlocked.Increment(ref rowIdx);
      outputRows[r] = $"  result: {value}";
  });
  foreach (var kv in outputRows.OrderBy(k => k.Key))
      _o.WriteLine(kv.Value);
  ```

  **Output volume (critical):** Minimize I/O in long-running tests. Use `StringBuilder` to accumulate all output, then emit once: `_o.WriteLine(sb.ToString())`. A single `WriteLine` call is orders of magnitude faster than hundreds of individual calls. This applies to both sequential and parallel sections.

  **Parallel architecture pattern:** When processing multiple architectures at multiple resolutions, use sequential outer loops (one per architecture) with `Parallel.ForEach` inside for resolution-level parallelism. Do NOT use nested `Parallel.Invoke` wrapping calls that themselves contain `Parallel.For` — this causes thread explosion and process crashes. Inner `Build3DGraph` already uses `Parallel.For` internally.
  ```csharp
  foreach (var (arch, fam, sizes) in architectures)
  {
      Parallel.ForEach(sizes, nGrid => { /* build, measure, collect */ });
      // emit ordered output for this architecture
  }
  ```

  **Thread-safe collections for results:** Use `ConcurrentBag<T>` for accumulating result objects from parallel work. Avoid `lock`-guarded `List<T>.Add()` inside parallel regions.
- TRM.Tests is intentionally used as a fast calculation/audit harness rather than a classic unit-test suite; many green assertions (including `Assert.True(true)`) are intentional and not meant as strict behavioral verification.

## Shared Code Organization
- **Always prefer shared helper classes over inline helpers in test files.** When writing new tests or adding utility methods (records, enums, static helpers, evaluation functions, sweep runners, statistical methods), place them in a shared helper class rather than defining them directly in the test file. This applies globally to all test namespaces.
- For each test namespace/folder (e.g., `V5`, `V7`, `V4_1`), create or use an existing `*TestHelpers.cs` static class. Example: `V7_TestHelpers.cs` → static class `V7TestHelpers` imported via `using static TRM.Tests.V7_3_and_4.V7TestHelpers;`.
- When a helper is used by only a single test, it may be defined as a local static function within that test method. If used by two or more tests, move it to the shared helper class.
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