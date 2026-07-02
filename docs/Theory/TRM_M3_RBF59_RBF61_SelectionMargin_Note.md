# TRM M3 RBF59–RBF61 Selection-Margin Note

## Scope

This note summarizes RBF59–RBF61 as bounded selection-margin diagnostics for the shared-functional `m=3` path.

---

## 1) RBF59: explicit m=3 margin vs admissible competitors

`RBF59_SharedFunctional_Should_Report_M3SelectionMarginAgainstAdmissibleCompetitors` evaluates the baseline shared-functional result where admissible modes can include `[3,4,2]`, then reports:

1. total shared-functional energy for `m=1..5`,
2. `m3 vs m2` and `m3 vs m4` total-energy margins,
3. `m3 vs next-best admissible` margin,
4. selection class (`strict unique`, `minimal-by-energy`, `tie/boundary`, `non-unique`).

Diagnostic outcome:

> `m=3` is selected as minimal by shared functional energy in baseline diagnostics; selection rationale is margin-based, not only admissibility membership.

---

## 2) RBF60: ordering/tie-break sensitivity test

`RBF60_M3Selection_Should_Not_Depend_OnArbitraryTieBreaking` compares selection under alternative ordering rules:

1. lowest `m` first,
2. lowest energy first,
3. strongest bridge-prior first,
4. strongest phase-closure first,
5. strongest action-stationarity first.

Diagnostic outcome:

> Naive ordering (lowest `m`) can prefer `m=2`, while physically motivated ordering rules keep `m=3` selected; ordering sensitivity is explicitly classified as bounded boundary behavior when present.

---

## 3) RBF61: stronger structural component margins

`RBF61_AdmissibleCompetitors_Should_Fail_AtLeastOne_StrongerStructuralMargin` inspects admissible competitors (e.g., `m=2`, `m=4`) against `m=3` by:

1. phase-defect margin,
2. bridge-prior/qCore margin,
3. action-stationarity margin,
4. total-functional margin.

Diagnostic outcome:

> In baseline diagnostics, `m=3` has the best combined structural margin and each admissible competitor is blocked by at least one stronger structural component.

---

## 4) Updated status

Current reviewer-safe status:

> `m=3` is a bounded shared-functional selection candidate, selected by minimal shared energy and structural margins rather than arbitrary ordering.

---

## 5) Remaining gap

Primary remaining gap:

> Formal theorem-level derivation of a necessary selection rule from these margins remains open.

Natural next step:

> RBF62+ can target a formalized selection rule derived from shared-functional energy margins under explicit admissible-domain assumptions.

---

## 6) Claim boundaries

- diagnostic/candidate only
- not theorem-level proof
- not full first-principles closure
- not universal `m=3` selection
- not GR replacement
- no numerology claim

