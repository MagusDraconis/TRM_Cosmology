# TRM.App Copilot CLI Instructions

## Language

- User communication may be German.
- Code, prompts, comments, component names, and technical implementation must remain English unless existing code uses another convention.

---

## Application Scope

This project is the public TRM website built with C# Blazor (Interactive Server) and MudBlazor (v8.10.0).

The website presents **Temporal Rate Matrix / Temporal Resonance Mechanics** research across multiple scales: quantum/UV, weak-field/PPN, strong-field, and cosmological.

---

## Critical Website Rule

Whenever any task changes one of the following:

- V4.1 status
- test counts (total / passed / failed / skipped)
- release notes or Zenodo metadata
- documentation links (URLs or filenames)
- roadmap entries
- claim discipline (supported / conditional / hypothesis / not-claimed items)
- scientific claims displayed on the website
- linked PDFs or Markdown documents in `wwwroot/documents/`
- current evidence or numerical benchmark results
- project milestones or priority rankings

**then the agent MUST also check and update:**

- `TRM.App/Components/Pages/ProjectStatus.razor`
- `TRM.App/wwwroot/data/trm-v4-1-status.json` (if present)

**Do not leave Project Status stale.**

---

## Navigation Rule

The Project Status page must remain **directly below Home** in the main navigation.

Route: `/project-status`

---

## Claim Discipline

The website must always use these labels:

| Label | Color | Meaning |
|:---|:---|:---|
| **SUPPORTED** | Green / Success | Numerical evidence from passing tests |
| **CONDITIONAL** | Yellow / Warning | Holds under tested parameters, not proven universal |
| **HYPOTHESIS** | Blue / Info | Plausible, no simulation data or formal proof |
| **NOT CLAIMED** | Red / Error | Explicitly denied — must never appear as true |

**Never promote a HYPOTHESIS to SUPPORTED without explicit test or document evidence.**

---

## Hard Safety Claims

The website must **NOT** claim:

- TRM replaces General Relativity
- D = 3 is derived
- quantum mechanics is derived
- ħ (hbar) is derived
- c is derived
- G is derived
- Planck length is derived
- Planck time is derived
- quantum gravity is solved

These restrictions apply to all pages, not just Project Status.

---

## Current Status Baseline

**Verified 2026-07-12:**

| Metric | Value |
|:---|:---|
| V4.1 tests total | 386 |
| Passed | 386 |
| Failed | 0 |
| Skipped | 0 |
| Verification | TEST-RUN-VERIFIED |
| Command | `dotnet test --filter "FullyQualifiedName~V4_1" -v normal` |

**If newer test output exists, replace this baseline only with verified output.** Do not extrapolate or estimate test counts.

---

## Documentation Sources

When updating website status, inspect these files if present:

| File | Purpose |
|:---|:---|
| `docsV4_1/TRM_V4_1_Current_Status_And_Test_Evidence.md` | Test suite table, release integrity, claim summary |
| `docsV4_1/TRM_V4_1_Zenodo_Release_Notes.md` | Build instructions, scope boundary, references |
| `docsV4_1/theory/TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md` | Pipeline definition, R candidates, open problems |
| `TRM.App/wwwroot/data/trm-v4-1-status.json` | Website status data (auto-loaded by ProjectStatus page) |
| `TRM.App/wwwroot/documents/` | Served document directory |

---

## Project Status Page Requirements

The Project Status page (`/project-status`) must include:

1. **Disclaimer / claim discipline box** — colored badges + denial statement
2. **Status overview** — V3.4/V4/V4.1 frozen/active, test count, verification
3. **Research status cards** — one per test suite with SUPPORTED/CONDITIONAL badges
4. **Claim discipline sections** — four colored panels (green/yellow/blue/red)
5. **Open problems** — roadmap items with P1/P2/P3 priority chips
6. **Roadmap** — recommended next test file + candidate update laws
7. **Document links** — with availability status
8. **Test summary footer** — verified pass count, command, date

---

## Auto-Sync Rule

If `TRM.App/wwwroot/data/trm-v4-1-status.json` exists:

- **Prefer loading website status from that JSON** instead of hard-coding status content in Razor components.
- The page uses `TrmStatusService` to load JSON via `HttpClient.GetFromJsonAsync<TrmStatusModel>()`.
- If JSON loading fails, the service returns safe fallback content from `BuildDefault()`.
- The page shows a `<MudProgressLinear Indeterminate>` spinner while loading.

---

## Document Rule

Documents should be served from:

```
TRM.App/wwwroot/documents/
```

- **Do not create broken links.**
- If a document file is missing, the page shows: *"Document not yet published on website."*
- To make a document available: copy it to `wwwroot/documents/` and set its `available: true` and `url` in the JSON.

---

## UI Rule

- Keep existing **MudBlazor** style (v8.10.0).
- **Do not introduce a new UI framework.**
- Use existing layout (`MainLayout`, `NavMenu`) and components (`MudCard`, `MudPaper`, `MudGrid`, `MudChip T="string"`, `MudText`, `MudButton`, `MudIcon`).
- Theme colors: Primary = `#1565C0`, Secondary = `#0D47A1`.
- Claim discipline panels use colored `border-top` (green/yellow/blue/red).

---

## Validation Rule

After any changes, run:

```bash
dotnet build TRM.App/TRM.App.csproj
```

If test changes were made, also run:

```bash
dotnet test TRM.Tests/TRM.Tests.csproj --filter "FullyQualifiedName~V4_1" -v normal
```

**Fix all compile errors before final response.**
Warnings from pre-existing code (not your changes) may be noted but do not block completion.

---

## Final Response Rule

Every agent response after website work must include:

- Files changed
- Routes changed
- Navigation changed
- Whether Project Status was checked/updated
- Build result
- Unresolved assumptions
