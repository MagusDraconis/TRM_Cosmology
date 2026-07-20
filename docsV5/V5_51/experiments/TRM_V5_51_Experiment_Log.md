# V5.51 Experiment Log | 2026-07-20
| Suite | Status | Tests | Outcome |
|-------|--------|-------|---------|
| SOO | COMPLETE | 1 (SOO_01) | Model A — Spread ordering K1>K3>K2 INHERITED from w0 |
| PWO | COMPLETE | 1 (PWO_01) | Model B — Ordering GENERATED at init→w0. Init K state has no ordering. |
| FEG | COMPLETE | 1 (FEG_01) | Model A — Ordering first appears at omega domain (Sim). DL disrupts, Cupd restores. |
| ERO | COMPLETE | 1 (ERO_01) | Model C — Raw freq ensemble means already K1>K3>K2. Sim amplifies. |

## SOO_01 — Spread Order Origin Audit (2026-07-20)
- **Passed.** 37s execution. **Decision: Model A — Ordering INHERITED from w0.**
- K1>K3>K2 present at w0 (km: 0.057>0.049>0.024, lam: 0.056>0.049>0.023)
- Ordering STRENGTHENED during w0→w1 (K1 grows +0.096, K3 +0.076, K2 +0.062)
- Ordering DISAPPEARS at w2 (K1 collapses -0.067 while K2 grows +0.043)
- The kernel transition (w1→w2) DESTROYS the ordering that pre-transition spread uses
- Ordering is km/lam-specific (d IQR does NOT show ordering)
- Jackknife: ordering is profile-sensitive (can flip with single removal)
- Stop-Low: 35 stop, 0 rescues → SAFE
