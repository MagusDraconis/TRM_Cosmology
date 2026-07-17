# TRM V5.13 Protocol: High-Basin Pathway Validation

**Status:** INITIALIZED

## Suite: HVP — High-Basin Validation Protocol

### Purpose
Pre-register the experimental design for V5.13 pathway validation and scaling.

### Experimental N
- Primary: N=71, N=72
- Scaling: N=60-80 (grid)
- Negative control: N=67

### Seeds
- In-sample: 0-99
- Out-of-sample: 100-199

### Pathway Classes (from V5.12)
- P1b: d0 > 0.65 → 50% compression
- P1: d0 > 0.50 → 50% compression
- P2: d0 ≤ 0.40, km0 > 0.98, K-dist < 0.15 → 10% compression
- P3: ambiguous
- P4: low-probability

### Metrics
Same as V5.12: Omega, d_mean, K_mean, K_std, distHi, distLo, entry score, Δd_rel.

### Success Criteria
- Strict persistence: immHi + persisting after +1 epoch
- High-basin proximity: distHi < distLo
- No invalid state

### Claim Discipline
Same as V5.12.
