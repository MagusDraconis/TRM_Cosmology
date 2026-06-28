# TRM Real Physics Test Coverage

Stand: konsolidierte Abdeckung aus Codebasis + Theorieabgleich

Quellen:
- `docs/review/TRM_Code_To_Theory_Audit.md`
- `docs/review/TRM_Service_Test_Consolidation.md`
- `docs/Theory/TRM_Geodesic_Derivation.md`
- `docs/Theory/TRM_Finsler_Optical_Action.md`
- bestehende C# Services und Tests (`TRM.Tests`, `TRM.Core`, `TRM.QuantumCore`)

---

## Legende (Statusmarker)

- **tested** = harte Assertions gegen physikalische Zielgrößen
- **diagnostic** = numerische Diagnostik/Logging, oft weiche Schwellen
- **exploratory** = Hypothesen-/Parameterscans ohne harte Falsifikationskriterien
- **calibrated** = Parameter primär aus Fit/Kalibrierung
- **not derived yet** = im Code/Doku als nicht vollständig aus 1. Prinzipien hergeleitet erkennbar
- **limitation** = bekannte Abdeckungs- oder Modelllücke

---

## Executive Coverage Snapshot

- Breite numerische Abdeckung vorhanden für: Newton/Redshift-Basis, Mercury, Photon-Deflection, Schwarzschild-Referenzvergleich, Shapiro-Diagnostik, Planck-Konsistenz, Tick/Action/Phase-Scans, SPARC/RAR, CMB, Pantheon.
- Theta/O5-Observable-Pfad ist jetzt **tested-effective**: TO01–TO28, TQK01–TQK04 und LC01–LC08 stützen O5-W6-InvDistance plus regularisiertes \(\lambda_\Theta\) als derzeit stärksten nichtlokalen Theta-Observable-Kandidaten; Claim-Grenze bleibt **hypothesis-supported**, nicht theorem-level derived.
- Vektor-/Frame-dragging-Pfad ist als **tested-effective candidate sector** gestartet: FD01–FD15 stützen weak-field Lense-Thirring-Shape-Kompatibilität, stabile effektive Kopplungsnormalisierung, SpinZero-Skalar-Limit-Erhalt und prograde/retrograde Asymmetrie.
- Wesentliche offene Punkte:
  - vollständige Euler-Lagrange-Produktionspipeline fehlt weiterhin (expliziter E-L/Fermat-Solverpfad vorhanden, aber noch nicht formal geschlossen),
  - mehrere zentrale Parameter sind kalibriert und als noch nicht vollständig hergeleitet markiert.

---

## Vollständige Real-Physics-Testübersicht

## 1) Newton gravity

- **Relevante Tests:**
  - `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs`
	- `TRM_Should_Reproduce_Newton_Gravity_PhaseModel`
- **Modellbezug:**
  - Phase-/Potentialansatz mit schwachem Feldbezug (`phi = GM/(c^2 r)`), Abgleich gegen Newton-Beschleunigung.
- **Status:** **tested**
- **Einordnung:** solide Baseline, aber nicht gleichbedeutend mit vollständiger relativistischer Ableitung.

## 2) redshift / time dilation

- **Relevante Tests:**
  - `TRM_Should_Reproduce_Gravitational_Redshift_Phase`
  - `TRM_Should_Match_Redshift_Between_Two_Heights_Phase`
  - `TRM_Should_Reproduce_Gravitational_Redshift_Phase_DIFFERENTIAL`
- **Modellbezug:**
  - gravitativer Redshift aus TRM-Phasen-/Zeitratenstruktur.
- **Status:** **tested** (mit diagnostischen Anteilen)
- **Einordnung:** numerisch konsistent im Testfenster; globale Theorieableitung bleibt teilweise offen.

## 3) Mercury perihelion

- **Relevante Tests:**
  - `TRM_Should_Reproduce_Mercury_Perihelion_Precession`
  - Serien TRM19–TRM31 in `TRM_Realtiy_Tests.cs`
- **Modellbezug:**
  - verschiedene Integrations-/Korrekturvarianten für Perihel-Drift.
- **Status:** **tested** + **diagnostic** + teilweise **exploratory**
- **Einordnung:** starke numerische Präsenz, aber heterogene Methodik über viele Varianten.

## 4) photon deflection

- **Relevante Tests:**
  - `TRM_Realtiy_Tests.cs`: TRM32–TRM60 (u. a. Deflection/Skalierungsblöcke)
  - `TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs`: TRM78–TRM87 + MC01–MC08 + HOA01 + CLAIM01
  - `TRM.Tests/RealityTests/PhotonTransportModel_GeodesicSolverTests.cs`: MEM01–MEM02 (Memory-Ablation + Weak-Field-Boundary)
  - `TRM.Tests/SimulationTests/WaveOpticsTests.cs` (M- und 1/b-Skalierungen, Konvergenz)
- **Modellbezug:**
  - `TRM.Core/Shared/PhotonTransportModel.cs` mit RK4, `n_eff`-Kanaltrennung, Deflection-Diagnostik.
- **Status:** **tested** (Invarianten/Skalierungen) + **diagnostic**
- **Einordnung:** numerisch gut abgesichert; Interpretation stark modellabhängig.
  - MC01–MC08 schließen den lokalen Memory-Invariant-Selektionsblock ab (ohne bereits eine mikroskopische Theorem-Herleitung zu beanspruchen).

## 5) Schwarzschild null-geodesic comparison

- **Relevante Tests:**
  - `TRM49_RK4_Photon_TRM45AB_vs_SchwarzschildNullGeodesic_Test`
  - Folgeblöcke TRM50+ mit Residual-/Parameterdiagnostik gegen Schwarzschild-Referenz
- **Modellbezug:**
  - Referenzvergleich zur Nullgeodäten-Form als externer Konsistenzanker.
- **Status:** **tested** + **diagnostic**
- **Einordnung:** starker Referenzvergleich im Testkontext; Nachbartests teils fit-/residual-getrieben.

## 6) Shapiro delay

- **Relevante Tests:**
  - `TRM_Realtiy_Tests.cs`: TRM67–TRM77
  - `PhotonTransportModel_FixationTests.cs`: TRM82 (Skalierungsinvariante des implementierten Diagnostics)
  - `PhotonTransportModel_GeodesicSolverTests.cs`: MEM01 (zeigt nahezu neutralen Shapiro-Effekt bei Memory-Ablation im aktuellen Modell)
- **Modellbezug:**
  - geometrische Verzögerungsdiagnostik über `∫ phi ds` im Photon-Transport.
- **Status:** **diagnostic** + **exploratory** (mit teilweiser **tested**-Fixierung auf Implementationsinvariante)
- **Einordnung:** breite numerische Untersuchung, aber viele weiche Kriterien.

## 7) Planck constants

- **Relevante Services:**
  - `TRM.QuantumCore/Planck/PlanckConstants.cs`
  - `TRM.QuantumCore/Planck/DerivedConstants.cs`
- **Relevante Tests:**
  - `TRM.Tests/QuantumTests/PlanckConsistencyTests.cs`
- **Modellbezug:**
  - Standard-Planck-Relationen und Rückrekonstruktion von `c`, `ħ`, `G`.
- **Status:** **tested**
- **Einordnung:** konsistent und robust, jedoch ohne vollständiges Unsicherheitsbudget.

## 8) tick/action bridge

- **Relevante Tests:**
  - `TRM.Tests/QuantumTests/UncertaintyTests.cs`
  - `TRM.Tests/QuantumTests/UncertaintyTests1.cs`
- **Modellbezug:**
  - Tick-Variation (`gamma * tP`), Action-/Uncertainty-Produkte, Stabilitätsmetriken.
- **Status:** **exploratory** + **diagnostic** (teilweise **tested** auf lokale Kriterien)
- **Einordnung:** starker Scan-Charakter, begrenzte harte Falsifikationsschärfe.

## 9) phase lock / collective gamma ≈ 0.85

- **Relevante Tests/Signale:**
  - `UncertaintyTests.cs`
	- `Should_Show_Strongest_Phase_Synchronization_Near_Planck_Tick`
	- `Should_Show_PhaseLock_Near_Integer_Tick_Ratios`
  - `CollectiveModeLockingTests.cs`
    - `CML01_Should_Reproduce_20_17_ModeLock_Without_PhotonTransport`
    - `CML02_ModeLock_Should_Depend_On_CellCoupling_Not_PhotonFit`
    - `CML03_ModeLock_Should_Degrade_When_PhaseClosure_IsBroken`
    - `CML04_Gamma_Should_Approach_17_20_From_CollectiveCadence`
    - `RBF13_ConstraintRemoval_Should_Reveal_ActionTick_As_Key_For_M3Selection`
    - `RBF14_CompetingBands_Should_Reveal_LockingVsELTradeoff`
    - `RBF15_DeriveOrFalsify_Should_Show_M3Uniqueness_WithAllConstraints_And_NonUniqueness_Without_ActionTick`
  - wiederkehrende Gamma-Grid-Werte inkl. `0.85`
- **Modellbezug:**
  - Phase-Lock/Synchronisationsscans (Kuramoto-artige Auswertungslinien) plus isolierter 20:17-Kadenz-Block ohne Photon-Transport-Zirkularität.
- **Status:** **tested (isolated cadence block)** + **exploratory** + **diagnostic**
- **Einordnung:** deutlich stärkere, nicht-zirkuläre Evidenz für den kollektiv assistierten Kadenzpfad; first-principles-Herleitung des Peaks bleibt offen.

## 10) SPARC / RAR / orbit / theta field

- **Relevante Services:**
  - Domain1: `SparcRarAnalysis`, `TrmFullModel`, `TrmRadialRegimeModel`, `TrmAdaptiveRegimeModel`, `TrmDualRegimeModel`, `OrbitalIntegrationService`, `TrmFieldSolver`
- **Relevante Tests:**
  - `TRM.Tests/CoreTests/RarRelationTests.cs`
  - `TRM.Tests/CoreTests/OrbitalIntegratedTests.cs`
  - `TRM.Tests/CoreTests/ThetaObservableDerivationTests.cs` (TO01–TO28, TQK01–TQK04, LC01–LC08)
- **Modellbezug:**
  - `gbar + sqrt(gbar*a0)` Basisterm, Orbit-/Regime-/Theta-Feld-Korrekturen, RMS-/Bin-Auswertungen.
- **Status:** **tested** + **tested-effective (theta observable + lambda-response discipline gate)** + **calibrated** + teilweise **not derived yet**
- **Einordnung:** sehr starke Pipeline- und Derivation-Gate-Abdeckung. TO12–TO20 stützen O5-W6-InvDistance unter Klassen-/Holdout-/Strata-/Leakage-/Solver-Ablations-Validierung; TO21–TO24 ergänzen Operatorstruktur- und finite-coherence-Fenster-Guards; TO25–TO28 ergänzen diskrete Energiegradienten-Absicherung (\(O_5 \approx -\partial E_\Theta/\partial\Theta\)) mit Energy-Descent/Zero-Mode/Boundedness-Guards; TQK01–TQK04 ergänzen Small-Phase-Lattice-Energy-Reduktion und Gradientenkonsistenz; LC01–LC08 schließen die Lambda-Response-Disziplin mit Dimensions-/Globalisierungs-/Regularisierungs-/Strata-Safety-/Anti-Proxy-Guards ab. Claim-Status bleibt hypothesis-supported und nicht theorem-level fundamental.

## 11) cluster regime model

- **Relevanter Service:**
  - `TRM.Core/Domains/Domain2.GalaxyClusters/BulletClusterAnalysis2.cs`
- **Relevante Tests:**
  - `TRM.Tests/CoreTests/BulletClusterAnalysis2Tests.cs`
    - `HydrostaticMass_Should_Be_Finite_And_Positive_With_DeterministicFixtures`
    - `UnifiedMass_Should_Respond_To_PressureGradient_And_Damping`
    - `ClusterDiagnostics_Should_Be_Deterministic_Finite_And_Bounded`
    - `RegimeClassification_Should_Respect_Bounds`
- **Modellbezug:**
  - hydrostatische Masse + gewichtete TRM/Newton-Mischung, ellipticity/damping-Faktoren.
- **Status:** **tested (baseline deterministic)** + **calibrated** + **exploratory**
- **Einordnung:** baseline-härtung vorhanden; für physikalisch starke Aussagen sind weitergehende datennahe Akzeptanztests nötig.

## 12) CMB

- **Relevanter Service:**
  - `TRM.Core/Domains/Domain3.Cmb/CmbAcousticSolver.cs`
- **Relevante Tests:**
  - `TRM.Tests/CoreTests/ClockworkCosmologyTests.cs`
	- Peak-Ratio-Stabilität
	- Scale-Consistency mit aktuellem Parametersatz
- **Modellbezug:**
  - k-Space-Peaks, Drive/Doppler-Optimierung, Rekombinations-/Skalenableitung.
- **Status:** **tested** + **calibrated**
- **Einordnung:** numerisch robust; Parameterlage fit-getrieben.

## 13) Pantheon / HT

- **Relevante Services:**
  - `TRM.Core/Domains/Domain4.Supernovae/PantheonTrmScaleSolver.cs`
  - `TRM.Core/Shared/TrmCosmologyParameter.cs`
- **Relevante Tests:**
  - `ClockworkCosmologyTests.cs` (Pantheon-RMS/Residuals, HT-Sensitivity)
  - `TrmCosmologyParameterTraceTests.cs` (Derivation-Trace-Inversionen für `HT/BetaEta/Alpha`)
- **Modellbezug:**
  - Distanzmodul-Residualstatistik gegen Pantheon-Daten; HT-Sweeps.
- **Status:** **tested** + **calibrated** + **not derived yet**
- **Einordnung:** Pipeline stark; `HT/BetaEta/Alpha` sind weiterhin kalibrierte Arbeitswerte, aber die Derivation-Trace-Gleichungen sind jetzt explizit im Code hinterlegt und über CoreRegression-Tests abgesichert.

## 14) Euler-Lagrange status

- **Theoriequelle:**
  - `docs/Theory/TRM_Geodesic_Derivation.md`
  - `docs/Theory/TRM_Finsler_Optical_Action.md`
  - Variationsprinzip: `δ ∫ n_eff dℓ = 0`
  - strukturelle Geodätengleichung mit Zeit-/Transport-/Memory-Term
- **Code-Status:**
  - Photon-Transport implementiert die strukturähnlichen Terme numerisch,
  - expliziter, eigenständiger Euler-Lagrange/Fermat-Solverpfad ist vorhanden (`PhotonTransportModel_GeodesicSolverTests`: EL01–EL17 + MEM01–MEM02),
  - aber die vollständige formale Produktionskette bleibt unvollständig.
- **Status:** **tested (partial)** + **not derived yet** + **limitation**
- **Einordnung:** ausführbarer Herleitungs-Track begonnen und testbar, aber noch nicht als vollständig geschlossene E-L-Kette publikationsreif.

## 15) Lense-Thirring / frame-dragging vector candidate sector

- **Code-/Testlage:**
  - `TRM.Tests/CoreTests/FrameDraggingVectorExtensionTests.cs` (`FD01–FD15`)
  - `docs/Theory/TRM_Vector_Tensor_Extension_FrameDragging.md`
- **Status:** **tested-effective (weak-field candidate sector)** + **calibrated (effective \(k_T\))** + **not derived yet**
- **Einordnung:** Der neue Vektor-Sektor zeigt strukturelle weak-field Lense-Thirring-Shape-Kompatibilität (J-linear, \(r^{-3}\), Spin-Sign-Flip, Null bei Nichtrotation), stabile globale effektive \(k_T\)-Normalisierung inkl. Holdout/Discretization-Robustheit sowie prograde/retrograde Lichtpfad-Asymmetrie. Gleichzeitig bleibt die Claim-Grenze strikt: nicht first-principles-derived, nicht quantitativ GR-äquivalent.

---

## Parameter-/Herleitungsstatus (quer über alle Themen)

- **calibrated (zentral):**
  - `TrmCosmologyParameters`: `HT`, `BetaEta`, `Alpha`
  - Teile der Photon-Transport-Koeffizienten (`A`, `B`, `Lambda`, `LambdaTime/LambdaSpace` im praktischen Einsatz)
  - Cluster-Parameter (`C`, `alpha`, `baselineK`, `beta`, Schwellwerte)
- **not derived yet (zentral):**
  - vollständige 1.-Prinzipien-Herleitung mehrerer Cosmology-/Regimeparameter
  - vollständige produktive Euler-Lagrange-Kette

---

## Kompakte Ampel nach angefragten Themen

| Thema | Primärstatus |
|---|---|
| Newton gravity | **tested** |
| redshift / time dilation | **tested** (+ diagnostic) |
| Mercury perihelion | **tested** + **diagnostic** + exploratory-Anteile |
| photon deflection | **tested** + **diagnostic** |
| Schwarzschild null-geodesic comparison | **tested** + **diagnostic** |
| Shapiro delay | **diagnostic** + **exploratory** (teils tested-Fixation) |
| Planck constants | **tested** |
| tick/action bridge | **exploratory** + **diagnostic** |
| phase lock / collective gamma ≈ 0.85 | **tested (isolated cadence block)** + **exploratory** + **diagnostic** |
| SPARC / RAR / orbit / theta field | **tested** + **tested-effective theta observable** + **calibrated** + **not derived yet** |
| cluster regime model | **tested (baseline deterministic)** + **calibrated** + **exploratory** |
| CMB | **tested** + **calibrated** |
| Pantheon / HT | **tested** + **calibrated** + **not derived yet** |
| Euler-Lagrange status | **tested (partial)** + **not derived yet** + **limitation** |
| Lense-Thirring / frame-dragging | **tested-effective candidate sector** + **calibrated** + **not derived yet** |

---

## Abschluss

Diese Übersicht bildet den aktuellen Stand der real-physikalischen Testabdeckung im Repository ab, inklusive klarer Trennung zwischen robust getesteten Kernpfaden und offenen Herleitungs-/Abdeckungslücken.