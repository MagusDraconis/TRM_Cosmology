# TRM V3.3 — Release Audit

## Repository Document Audit

### KEEP — Release-Critical Documents

| Document | Location | Role |
|:---|:---|:---|
| `TRM_V3_3_Reviewer_Status.md` | `docs/review/` | **Primary reviewer-facing status** — single best summary |
| `TRM_M3_V3_3_Closure_Status_Note.md` | `docs/Theory/` | Closure status details (RBF24–FP31) |
| `TRM_M3_Formal_Proof_Obligations.md` | `docs/Theory/` | Complete obligation tracking |
| `TRM_M3_First_Principles_Gap_Audit.md` | `docs/Theory/` | Gap audit with assumption dependency graph |
| `TRM_M3_FP31_CeilInequality_Closure_Note.md` | `docs/Theory/` | Final closure proof |
| `TRM_DS01_DS41_QuantumDiagnostics_Status_Note.md` | `docs/Theory/` | DS diagnostics consolidated status |
| `TRM_LPC_Final_Rebase.md` | `docs/Theory/` | LPC final rebase — assumption decomposition |
| `TRM_M3_FP01_FP31_FormalProofs_PR.md` | `docs/review/` | FP PR package |
| `TRM_DS01_DS41_QuantumDiagnostics_PR.md` | `docs/review/` | DS PR package |
| `TRM_M3_FP01_FP31_Reviewer_Checklist.md` | `docs/review/` | Reviewer checklist |
| `TRM_V3_3_Release_Manifest.md` | `docs/release/` | This release manifest |
| `TRM_V3_3_Release_Notes.md` | `docs/release/` | Release notes |

### KEEP — Supporting Theory Documents (Historical + Reference)

| Document | Role |
|:---|:---|
| `TRM_Theory_Lineage_V1_to_V3_3.md` | Historical lineage |
| `TRM_TQM_Theorie_Statement.md` | TQM theory statement |
| `TRM_Geodesic_Derivation.md` | Photon transport / geodesic derivation |
| `TRM_Memory_Channel_Microscopic_Derivation.md` | Lattice phase infrastructure |
| `TRM_Rational_Band_First_Principles.md` | Bridge-band origin question |
| `TRM_Collective_Mode_Locking_BridgeScale.md` | CML bridge-scale band |
| `TRM_M3_Closure_Derivation_Attempt.md` | Closure derivation context |
| `TRM_M3_Closure_First_Principles.md` | Closure formalization track |
| `TRM_M3_Closure_Theorem_Path.md` | Theorem path structure |
| `TRM_V3_1_Action_Based_Memory_Closure.md` | V3.1 memory closure |
| `TRM_V3_2_Minimal_Action_From_TQM_Lattice.md` | V3.2 minimal action |
| `TRM_V3_3_Research_Status.md` | V3.3 research status |
| All FP notes (FP01–FP31) | `TRM_M3_FP*_*.md` — proof obligation documentation |
| All DS notes (DS01–DS41) | `TRM_DS*_*.md` — diagnostic documentation |
| All RBF notes (RBF27–RBF82) | `TRM_M3_RBF*_*.md` — constraint-stack documentation |
| All LPC notes (LPC01–LPC03) | `TRM_LPC*_*.md` — assumption decomposition |
| `THEORY_STATUS.md` | Theory status overview |

### ARCHIVE — Historical V3.0–V3.2 Status Documents (Superseded by V3.3)

| Document | Reason |
|:---|:---|
| `docs/review/TRM_V3_1_Consolidated_Claim_Status.md` | Superseded by V3.3 Reviewer Status |
| `docs/review/TRM_V3_2_Minimal_Action_From_Lattice_Review_Note.md` | Superseded |
| `docs/review/TRM_V3_3_M3_Closure_Scaffold_PR.md` | Superseded by FP01–FP31 PR |
| `docs/review/TRM_V3_3_PhaseProxyResidualDiagnostics.md` | Superseded by DS01–DS41 |
| `docs/review/TRM_V3_3_RadialOrbitalSynchronizationHypothesis.md` | Separate track, not V3.3 core |
| `docs/review/TRM_V3_3_Research_Status.md` | Superseded by V3.3 Reviewer Status |
| `docs/review/TRM_V3_0_to_V3_2_Progress_Summary.md` | Historical; V3.3 is the current release |
| `docs/review/TRM_V3_1_Memory_Action_Closure_Review_Note.md` | Superseded |
| `docs/review/TRM_Current_Status_For_PeerReview.md` | Superseded by V3.3 Reviewer Status |
| `docs/review/TRM_Zenodo_Release_Notes.md` | Superseded; V3.3 Release Notes replace it |

### ARCHIVE — Draft / Work-in-Progress Documents

| Document | Reason |
|:---|:---|
| `TRM_Memory_Channel_Derivation_Attempt.md` | Draft attempt; superseded by Microscopic Derivation |
| `TRM_Memory_Channel_First_Principles.md` | Draft; superseded |
| `TRM_First_Principles_Gap_List.md` | Draft; superseded by Gap Audit |
| `TRM_First_Principles_Roadmap.md` | Draft planning; superseded by actual track completion |
| `TRM_Field_Sector_Map.md` | Draft; not V3.3 core |
| `TRM_Finsler_Optical_Action.md` | Draft; not V3.3 core |
| `TRM_Unified_Field_Action_Roadmap.md` | Draft planning; superseded |
| `TRM_Theta_O5_Lambda_Theorem_Path.md` | Separate track (Θ/O5); not V3.3 core |
| `TRM_Theta_Observable_First_Principles.md` | Separate track; not V3.3 core |
| `TRM_Theta_To_Observable_Derivation_Plan.md` | Separate track; not V3.3 core |
| `TRM_Vector_Tensor_Extension_FrameDragging.md` | Separate track; not V3.3 core |
| `TRM_Project_Update.md` | Outdated project status |
| `TRM_Collective_Mode_Locking_20_17.md` | Early CML draft; superseded by BridgeScale |

### CANDIDATE FOR REMOVAL — Duplicate or Obsolete

| Document | Reason |
|:---|:---|
| `docs/review/TRM_Code_To_Theory_Audit.md` | Duplicate of gap audit content |
| `docs/review/TRM_Service_Test_Consolidation.md` | Duplicate of test coverage content |
| `docs/review/TRM_TestSuite_Classification.md` | Superseded; DS01–DS41 covers all tests |
| `docs/review/TRM_Real_Physics_Test_Coverage.md` | Superseded by DS PR |
| `docs/review/TRM_Peer_Review_Request.md` | Superseded by V3.3 Reviewer Status |
| `docs/review/TRM_Cover_Letter_And_Abstract.md` | Superseded by Release Notes |
| `docs/review/REVIEW_PACKAGE.md` | Superseded by V3.3 release package |

### Summary

| Category | Count |
|:---|:---:|
| KEEP (release-critical) | 12 |
| KEEP (supporting theory + historical) | ~40 |
| ARCHIVE (superseded status docs) | 10 |
| ARCHIVE (draft/WIP) | 13 |
| CANDIDATE FOR REMOVAL | 7 |
