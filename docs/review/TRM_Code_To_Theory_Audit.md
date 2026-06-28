# TRM/TQM Code-to-Theory Audit

Stand: automatische Repository-Analyse (C#-Scope) für `TRM_Cosmology.slnx`

## Executive Summary (Peer-Review)

- **Teststatus:** In der aktuellen Ausführung wurden die priorisierten Suiten erfolgreich ausgeführt: **162/162 bestanden**, 0 failed, 0 skipped. Zusätzlich zeigt die Testübersicht 176 Tests mit Passed-Status.
- **Starke Abdeckung (bestätigt/tested):**
  - Newton-/Redshift-Basistests aus Phasenansatz (TRM Reality)
  - Mercury-Perihel-Serien (mehrere numerische Varianten)
  - Photon-Deflection inkl. Schwarzschild-Referenzvergleich (TRM49ff)
  - Shapiro-Delay-Serien (TRM67–TRM77; teils diagnostisch)
  - Planck-/Tick-/Action-Bridge (Uncertainty/Planck tests)
  - SPARC/RAR/Pantheon/CMB-Pipelines in CoreTests
- **Wesentliche Limitationen:**
  - **Lense-Thirring / Frame dragging:** keine aktive Testabdeckung gefunden.
  - **Euler-Lagrange / Geodäten-Herleitung im ausführbaren Code:** expliziter Euler-Lagrange/Fermat-Solverpfad vorhanden (EL01–EL03), aber noch keine vollständige formal geschlossene E-L-Produktionspipeline.
  - Mehrere Tests sind explizit **diagnostisch/exploratory** (oft `Assert.True(true)` oder weiche Assertions).
- **Theoriestatus:** Kombination aus abgeleiteten Formeln, numerischer Plausibilisierung und kalibrierten Parametern. Für Publikationsreife sind klarere Trennung von Herleitung vs. Fit und harte, reproduzierbare Akzeptanzkriterien nötig.

---

## Legende

- **bestätigt/tested** = explizite, harte Test-Assertions gegen physikalische Zielgrößen
- **numerisch diagnostisch** = numerische Exploration/Logging, schwache Assertions
- **exploratory** = Parameter-Scan/Hypothesentest ohne harte Falsifikation
- **not derived yet** = im Code als Placeholder/TODO/kalibrierter Wert markiert
- **limitation** = bekannte Abdeckungslücke oder methodische Einschränkung

---

## Dateiweise Code-to-Theory-Matrix

## 1) `TRM.QuantumCore/Planck/PlanckConstants.cs`
- **Namespace/Klasse:** `TRM.QuantumCore.Planck`, `PlanckConstants`, `PhysicalConstantsSI`
- **Enthaltene Modellfunktionen:** `FromPhysicalConstants()`
- **Implizite Gleichungen:**
  - \(l_P = \sqrt{\hbar G / c^3}\)
  - \(t_P = l_P / c\)
  - \(m_P = \sqrt{\hbar c / G}\)
- **Numerische Parameter/Konstanten:** `hbar`, `G`, `c`, `M_Solar`, `M_Earth`, `R_Earth`, `Earth_LowOrbit`, `b`
- **Was wird wirklich getestet?:** über `PlanckConsistencyTests` (c-Rekonstruktion, Sensitivität)
- **Diagnose vs Herleitung:** Formeln sind standardphysikalisch hergeleitet, numerische Nutzung teils diagnostisch
- **Fit/Kalibrierung:** keine explizite Fitlogik hier
- **Statusmarker:** **bestätigt/tested** (für Basis-Rekonstruktion), **limitation** (kein Unsicherheitsbudget für Konstanten)

## 2) `TRM.QuantumCore/Planck/DerivedConstants.cs`
- **Namespace/Klasse:** `TRM.QuantumCore.Planck`, `DerivedConstants`
- **Modellfunktionen:** Ableitung von `SpeedOfLight`, `ReducedPlanck`, `G` aus Planck-Skalen
- **Gleichungen:**
  - \(c = l_P/t_P\)
  - \(\hbar = m_P l_P^2 / t_P\)
  - \(G = l_P^3/(m_P t_P^2)\)
- **Tests:** `PlanckDerivedConstantsMatchReality`, `PlanckDerivedConstantsSensitivity_Test`
- **Statusmarker:** **bestätigt/tested**

## 3) `TRM.Core/Shared/PhysicalConstants.cs`
- **Namespace/Klasse:** `TRM.Core`, `PhysicalConstants`
- **Inhalt:** CGS/SI-Umrechnungen, `A0_Cosmic = 1.2e-8 cm/s²`
- **Rolle:** globale numerische Basis für SPARC/Cluster-Modelle
- **Herleitung/Fit:** `A0_Cosmic` als vorgegebener Arbeitswert (nicht lokal abgeleitet)
- **Statusmarker:** **not derived yet** (im Repository-Kontext), **limitation**

## 4) `TRM.Core/Shared/TrmModelParameters.cs` + `TrmDerivedParameters.cs`
- **Inhalt:** `DefaultA0_Ms2`, `DefaultPhiBeta`, `DefaultRegimeGamma`, Regimegrenzen, Clamp-Grenzen
- **Theoriebezug:** Parameterisierte Regime-/Synchronisationslogik
- **Herleitung/Fit:** mehrere Parameter sind Defaults/heuristische Bounds
- **Statusmarker:** **exploratory**, **not derived yet**

## 5) `TRM.Core/Shared/TrmCosmologyParameter.cs`
- **Inhalt:** `BetaEta`, `Alpha`, `HT` zentrale aktuelle Werte
- **Expliziter Codehinweis:** Werte als „calibrated working values“, TODO für Herleitung
- **Wichtige Zahl:** `HT = 70.30` (Pantheon-kalibriert laut Kommentar)
- **Statusmarker:** **fit/kalibriert**, **not derived yet**

## 6) `TRM.Core/Shared/PhotonTransportModel.cs`
- **Namespace/Klasse:** `TRM.Tests.RealityTests`, `PhotonTransportModel`
- **Modellfunktionen:** RK4-Photonpropagation, Deflection, Diagnostics, Shapiro-Integral
- **Implizite Gleichungen:**
  - \(\phi = GM/(c^2 r)\)
  - Basisterm: \(k_{base}=2+2a\phi+3b\phi^2\)
  - lokaler Term: \(\phi^2 |d\mu/dt|\)
  - vereinigt: \(n_{eff}=2+\lambda_t\phi+\lambda_s\phi^2|d\mu/dt|\)
  - Shapiro-geometrisch: \(\Delta T \sim \int \phi\,ds\)
- **Was wird getestet?:** über `PhotonTransportModel_FixationTests` + TRM67–TRM77
- **Was ist Diagnose?:** viele Langläufer in RealityTests mit Logging-Interpretation
- **Hergeleitet/Fit:** `A`, `B`, `Lambda` kommentiert aus früheren TRM-Serien (kalibriert)
- **Statusmarker:** **bestätigt/tested** (Invarianten), **numerisch diagnostisch** (Shapiro-Scans), **fit/kalibriert**

## 7) `TRM.Tests/RealityTests/PhotonTransportModel_FixationTests.cs`
- **Tests:** TRM78–TRM83
- **Physikinhalt:** Positivität `n_eff`, c-Erhaltung, Deflection~1/b, Shapiro-Skalierungsinvariante (aktuelle Implementationsinvariante), Kanaltrennung Zeit/Raum
- **Aussagekraft:** robust für Modellinvarianten, nicht vollständiger GR-Beweis
- **Statusmarker:** **bestätigt/tested**, teils **limitation** (implementation-bound invariants)

## 8) `TRM.Tests/RealityTests/TRM_Realtiy_Tests.cs`
- **Großsuite:** TRM-Basis, Mercury, Photon, Schwarzschild-Vergleiche, Shapiro-Serien
- **Wichtige Blöcke:**
  - Basistests: Newton/Redshift/Light-deflection (frühe Tests)
  - **Mercury perihelion tests:** TRM19–TRM31
  - Photon-Deflection: TRM32–TRM60 (inkl. TRM49 Null-Geodät-Referenz)
  - Shapiro: TRM67–TRM77
- **Was wird wirklich getestet?:** gemischtes Niveau; einige harte numerische Fenster, viele diagnostische/heuristische Checks
- **Lense-Thirring / frame-dragging:** keine aktive Testmethode gefunden
- **Euler-Lagrange/Geodät:** Schwarzschild-Referenzgleichung plus expliziter E-L/Fermat-Solververgleich (EL01–EL03); Vollherleitung im Produktionskern weiterhin unvollständig
- **Statusmarker:** **bestätigt/tested** + **numerisch diagnostisch** + **exploratory** (je nach Test)

## 9) `TRM.Tests/QuantumTests/PlanckConsistencyTests.cs`
- **Tests:** Planck-Konstanten-Konsistenz, Sensitivität, MultiScan-Export
- **Aussagekraft:** prüft numerische Selbstkonsistenz der Planck-Ableitungen
- **Statusmarker:** **bestätigt/tested** (Konsistenz), **exploratory** (Scan)

## 10) `TRM.Tests/QuantumTests/UncertaintyTests.cs` + `UncertaintyTests1.cs`
- **Inhalt:** Stabilitätslandschaften, Tick-Variationen \(\gamma t_P\), Action-Skalen, Resonanz/Phasen-Synchronisation
- **Relevante Punkte:**
  - Tick/Action-Bridge explizit behandelt
  - Gamma-Scans inkl. Bereich `0.85..1.15`
  - mehrere Synchronisations-/Phase-Lock-Tests
- **Was ist hart getestet?:** einige Nähebedingungen (z. B. best gamma nahe 1.0), aber viele weiche diagnostische Kriterien
- **Statusmarker:** **exploratory**, **numerisch diagnostisch**, teilweise **bestätigt/tested**

## 11) `TRM.Tests/QuantumTests/TRM_Micro_Makro.cs`
- **Inhalt:** frühe Mikro/Makro-Phase-Synchronisationsmodelle TRM01–TRM18
- **Physik:** emergente Gravitation aus Phasengradienten, Kuramoto-artige Kopplung
- **Diagnosecharakter:** viele Tests enden bewusst weich (`Assert.True(true)`), dienen Hypothesenmapping
- **Statusmarker:** **exploratory**, **numerisch diagnostisch**

## 12) `TRM.Core/Domains/Domain1.GalacticRotation/*` (SPARC/theta-field)
- **Dateien:** `SparcRarAnalysis`, `TrmFullModel`, `TrmRadialRegimeModel`, `TrmAdaptiveRegimeModel`, `TrmDualRegimeModel`, `TrmFieldSolver`, `TrmPhaseModel`, `TrmOrbitalPhaseService`
- **Theorie/Modelle:**
  - RAR-Mapping und \(g_{obs}\)-Prädiktion
  - lokaler MOND-artiger Basisterm \(g_{bar}+\sqrt{g_{bar}a_0}\)
  - orbit-/phase-integrierte Korrekturen
  - theta-field Relaxationssolver
- **Fit/Kalibrierung:** mehrere Arbeitsparameter und weiche Clamp/Regime-Bounds
- **Tests:** vor allem `RarRelationTests` + `OrbitalIntegratedTests`
- **Statusmarker:** **bestätigt/tested** (Pipeline-RMS/Konsistenz), **fit/kalibriert**, **not derived yet** (vollständige analytische Herleitung)

## 13) `TRM.Core/Domains/Domain2.GalaxyClusters/BulletClusterAnalysis2.cs`
- **Theorieinhalt:** hydrostatische Massenabschätzung + dynamisch gemischtes `G_effective`, ellipticity/geometric damping, bimodale TRM/Newton-Klassifikation
- **Fit-Parameter:** z. B. `C`, `alpha`, `baselineK`, `beta`, Pressure-Threshold, Referenzgradient
- **Testabdeckung:** keine dedizierte aktive Cluster-Testklasse in `TRM.Tests` gefunden
- **Statusmarker:** **exploratory**, **fit/kalibriert**, **limitation** (fehlende direkte Testhärtung)

## 14) `TRM.Core/Domains/Domain4.Supernovae/*` (Pantheon)
- **Dateien:** `PantheonDataLoader`, `PantheonTrmScaleSolver`
- **Modell:** Residualstatistik für Distanzmodul via `TrmDistanceMapper`
- **Tests:** `ClockworkCosmologyTests` (Pantheon RMS/Residuals, HT-Sensitivität/FineScan)
- **Statusmarker:** **bestätigt/tested** (numerische Pipeline), **fit/kalibriert** (HT)

## 15) `TRM.Tests/SimulationTests/WaveOpticsTests.cs`
- **Inhalt:** Deflection baseline vs SpatialCurvatureLikeGR, Skalierung mit M und b, Symmetrie, Konvergenz
- **Aussagekraft:** gute numerische Konsistenztests für Wellenoptik-Implementierung
- **Statusmarker:** **bestätigt/tested**, **limitation** (modellabhängige Approximation)

---

## A) Real-Physics-Testmatrix

| Testname | Physikalischer Effekt | Gleichung/Modell | Status | Aussagekraft | offene Punkte |
|---|---|---|---|---|---|
| `TRM_Should_Reproduce_Newton_Gravity_PhaseModel` | Newton-Beschleunigung aus Phasengradient | \(a=-c^2 d\phi/dr\), \(\phi=GM/(c^2r)\) | bestätigt/tested | Hoch (Baseline) | Keine starke Post-Newton-Abdeckung |
| `TRM_Should_Reproduce_Mercury_Perihelion_Precession` + TRM19–31 | Merkur-Periheldrehung | GR-Näherungsformel + numerische Orbitintegrationen | bestätigt/tested + numerisch diagnostisch | Mittel-Hoch | Methodik heterogen; Teiltests explorativ |
| `TRM49_RK4_Photon_TRM45AB_vs_SchwarzschildNullGeodesic_Test` | Photon-Ablenkung vs Schwarzschild | Nullgeodät-Referenz \(w''+w=3\epsilon w^2\) | bestätigt/tested | Hoch | Fitabhängigkeit von a,b in Nachbartests |
| TRM32–TRM38 (Photon) | Deflection, Konvergenz, M/b-Skalierung | RK4 + effektiver Index | bestätigt/tested | Mittel-Hoch | Approximationen in starkem Feld |
| TRM67–TRM77 (Shapiro) | Shapiro-Verzögerung | geometrisches Integral \(\int \phi ds\), log-fit | numerisch diagnostisch / exploratory | Mittel | mehrere weiche Assertions; teils long-running gate |
| `PhotonTransportModel_FixationTests` TRM78–83 | Modellinvarianten (n_eff, c, 1/b) | vereinheitlichtes n_eff-Modell | bestätigt/tested | Hoch (Regression) | Implementation-invariant, nicht vollständiger Theoriebeweis |
| `PlanckDerivedConstantsMatchReality` | Planck-Konsistenz | Rekonstruktion von c, \(\hbar\), G | bestätigt/tested | Hoch | keine Unsicherheitspropagation |
| `UncertaintyTests`: Tick/Action Gamma-Scans | Tick-/Action-Brücke | \(\Delta E\Delta t\)-Statistiken, \(\gamma t_P\)-Variationen | exploratory + numerisch diagnostisch | Mittel | robuste physikalische Falsifikationskriterien fehlen |
| `Should_Show_Strongest_Phase_Synchronization_Near_Planck_Tick` | Phasen-Synchronisation | Kuramoto-artiger Ordnungsparameter | exploratory | Mittel | Peak-Lage teils seed/kappa/topology-abhängig |
| `RarRelationTests` + `OrbitalIntegratedTests` | SPARC/RAR, g_obs-Prädiktion, Regime/Theta-Modelle | TRM Full/Regime/Adaptive Modelle | bestätigt/tested | Hoch (Datenpipeline) | analytische Herleitung vs Fit klarer trennen |
| `ClockworkCosmologyTests` (CMB/Pantheon) | CMB Peakratio, Distanzmaßstab, Pantheon-Residuale | TRM Mapper + Pantheon Solver | bestätigt/tested | Mittel-Hoch | HT aktuell kalibriert, nicht fundamental hergeleitet |
| Lense-Thirring / Frame-dragging | Rotationsbedingte GR-Effekte | — | limitation | Niedrig (keine direkte Tests) | dedizierte Modell-/Testimplementierung fehlt |

---

## B) Herleitungsstatus

| Thema | Code-Beleg | Mathematischer Status | Noch nötig für Publikation |
|---|---|---|---|
| Planck-Basisrelationen | `PlanckConstants`, `DerivedConstants` | formal klar / implementiert | Unsicherheitsanalyse + Fehlerfortpflanzung |
| Newton aus Phasengradient | `TRM_Realtiy_Tests` Basistests | numerisch bestätigt | formale Ableitung im Modellkapitel konsolidieren |
| Mercury-Perihel | TRM19–31 | numerisch bestätigt, methodisch gemischt | einheitlicher Integrator + klarer Fehlerrahmen |
| Photon-Deflection (weak field) | TRM32–TRM49+, `PhotonTransportModel` | numerisch gut abgedeckt | klare Trennung: abgeleitete Terme vs gefittete Terme |
| Schwarzschild-Referenz | `ComputeSchwarzschildNullDeflection_TRM49` | direkte Referenzimplementierung | unabhängige Verifikation + Dokumentation Randbedingungen |
| Shapiro-Delay | TRM67–77 + Fixation TRM82 | überwiegend diagnostisch/exploratory | harte Pass/Fail-Kriterien, long-test gating sauber dokumentieren |
| Euler-Lagrange/Geodät-Vollherleitung | Docs + Testhelper + expliziter E-L/Fermat-Solververgleich (EL01–EL03) | teilweise ausführbar, aber noch nicht formal vollständig geschlossen | vollständige E-L-Kette inkl. Ableitung, Randbedingungen und harter Referenzmetriken schließen |
| Phase lock / collective gamma | `UncertaintyTests` (gamma grids inkl. 0.85..1.15) | explorative numerische Evidenz | theoretische Begründung für Peaklage und Robustheit |
| SPARC theta-field / regime logic | Domain1 Modelle + CoreTests | numerisch bestätigt, teils parametriert | analytische Ableitung von beta/gamma/Regimegrenzen |
| Cluster-Regime-Modell | `BulletClusterAnalysis2` | explorativ + kalibriert | dedizierte automatisierte Tests + Fit-Unabhängigkeit |
| Pantheon/HT | `TrmCosmologyParameters`, `ClockworkCosmologyTests` | kalibriert, pipeline-getestet | fundamentale Herleitung von HT/BetaEta/Alpha |
| Lense-Thirring/frame-dragging | kein direkter Testcode gefunden | limitation | implementieren + gegen GR-Referenz testen |

---

## C) Parameter-Audit

| Parameter | Wert | Ort im Code | Herleitung/Fit/Kalibrierung | Kommentar |
|---|---:|---|---|---|
| `DefaultA0_Ms2` | 1.2e-10 | `TrmModelParameters` | kalibrierter Arbeitswert | breit in RAR/TRM-Modellen genutzt |
| `DefaultA0_Cgs` | 1.2e-8 | `TrmModelParameters` / `PhysicalConstants.A0_Cosmic` | kalibriert | MOND-nahe Skala |
| `DefaultPhiBeta` | 0.4 | `TrmModelParameters` | heuristic/calibrated | Synchronisationsgewicht |
| `DefaultRegimeGamma` | 0.25 | `TrmModelParameters` | heuristic/calibrated | Regimekorrekturstärke |
| Inner Regime Start/End | 1.0 / 4.0 (Rd) | `TrmModelParameters` | heuristic | Übergangsbereich |
| `HT` | 70.30 | `TrmCosmologyParameters.GetHT()` | Pantheon-kalibriert | im Code explizit als nicht fundamental hergeleitet markiert |
| `BetaEta` | 0.005 | `TrmCosmologyParameters.GetBetaEta()` | placeholder | TODO-Herleitung vorhanden |
| `Alpha` (cosmology scaling) | 6.8 | `TrmCosmologyParameters.GetAlpha()` | placeholder | TODO-Herleitung vorhanden |
| Photon A | -0.1701452243330672 | `PhotonTransportModel.Parameters`, TRM tests | fit (TRM43) | aus Deflection-Kalibrierserie |
| Photon B | -8.484408441898648 | `PhotonTransportModel.Parameters`, TRM tests | fit (TRM45) | aus Residualkalibrierung |
| Photon Lambda | 30.79445857638716 | `PhotonTransportModel.Parameters` | fit (TRM64-Historie) | aktuell Modellhistorie referenziert |
| `LambdaTime` / `LambdaSpace` | 1.0 / 30.0 | Photon-Model/Tests | kalibrierte Modellwahl | Unified time+space channel |
| Cluster `C`, `alpha`, `baselineK`, `beta` | 1.3195, -0.7589, 0.1, variabel | `BulletClusterAnalysis2` | fit/kalibriert | bimodale Klassifikation |
| Cluster reference gradient | 1e-33 | `BulletClusterAnalysis2` | heuristic | turbulenzgewichtete Mischung |
| Shapiro test epsilon | meist 0.01 | TRM67ff / Fixation | test setup | modellinterne Normierung, keine SI-Ableitung |
| Gamma-Scan (Tick/Action/Phase) | 0.85–1.15 u. a. | `UncertaintyTests` | exploratory | untersucht Peak-/Minimumstruktur |

---

## Spezifische Prüfungen (angefragt)

- **Mercury perihelion tests:** vorhanden und grün (TRM19–TRM31, plus Basis-Periheltest).
- **Euler-Lagrange / Geodesic derivation status:** teilweise umgesetzt (Schwarzschild-Nullgeodät-Referenz + expliziter E-L/Fermat-Solverpfad EL01–EL03), jedoch noch kein vollständig geschlossener produktiver E-L-Kernpfad (**partial / limitation**).
- **Lense-Thirring / frame-dragging coverage:** keine direkte aktive Testabdeckung gefunden (**limitation**).
- **photon deflection:** stark abgedeckt (TRM32+; TRM49 Referenzvergleich; WaveOptics-Checks).
- **Shapiro delay:** breit diagnostisch untersucht (TRM67–TRM77, Fixation TRM82), aber viele Scans explorativ.
- **Planck constants / tick / action bridge:** klar in `PlanckConsistencyTests`, `UncertaintyTests`, `UncertaintyTests1`.
- **phase lock and collective gamma ≈ 0.85:** Gamma-Grid enthält 0.85; Peak/Synchronisationsanalysen vorhanden, aber mehrheitlich explorativ/diagnostisch.
- **SPARC orbit/theta-field logic:** klar im Domain1-Code + `RarRelationTests`/`OrbitalIntegratedTests` numerisch abgesichert.
- **cluster regime model:** Implementiert in `BulletClusterAnalysis2`, aber ohne dedizierte harte Tests im Testprojekt (**limitation**).

---

## Ampel-Fazit

- **Grün (bestätigt/tested):** Kernnumerik für SPARC/RAR, Pantheon-Pipeline, CMB-Peak-Konsistenz, zentrale Photon-/Mercury-Tests, Planck-Konsistenz.
- **Gelb (numerisch diagnostisch / exploratory):** große Teile der TRM67–TRM77-Serie, weiche Scan-Tests in Micro/Makro und Uncertainty.
- **Rot/Offen (not derived yet / limitation):** vollständige formale E-L-Geodätenkette im Produktionsmodell, Lense-Thirring/frame-dragging, harte Cluster-Testhärtung.

---

## Kurzempfehlung für nächsten Review-Zyklus

1. **Hartes Testprofil definieren:** diagnostische Scans in klar getrennte „Research“-Suiten verschieben; Kernsuite mit harten Akzeptanzgrenzen.
2. **Derivation Track schließen:** explizite mathematische Herleitungen für `HT/BetaEta/Alpha`, Regime-Parameter und Photon-Zusatzterme dokumentieren und im Code verlinken.
3. **Frame-dragging/Lense-Thirring hinzufügen:** dedizierter Modellpfad + Referenztests.
4. **Cluster-Modul härten:** direkte Tests mit reproduzierbaren Datensätzen und Fit-robusten Kriterien.
5. **Publish-ready Traceability:** pro Parameter klar markieren: „abgeleitet“, „fit“, „kalibriert“, inkl. Unsicherheitsintervallen.
