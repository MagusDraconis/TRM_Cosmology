using System;
using System.IO;
using System.Linq;
using TRM.Core;
using TRM.Core.Domains.Domain4.Supernovae;
using TRM.Core.Shared;

namespace TRM.CMD
{
    class Program
    {
        static void Main(string[] args)
        {
            DrawBanner();
            var isFirstIteration = true;
            while (true)
            {
                if (!isFirstIteration)
                {
                    ClearConsole();
                }
                isFirstIteration = false;
                
                Console.WriteLine("\n=======================================================");
                Console.WriteLine(" V3.0 EXPERIMENT SELECTION (CLAIM-SAFE)");
                Console.WriteLine("=======================================================");
                Console.WriteLine(" [1] Run Cluster Diagnostics (TRM vs Newtonian Gravity)");
                Console.WriteLine(" [2] Analyze SPARC Galactic Rotations (TRM and MOND comparison)");
                Console.WriteLine(" [3] Analyze CMB Acoustic Peaks (Planck Cosmology)");
                Console.WriteLine(" [4] Analyze Pantheon+ Supernovae (TRM scale-distance diagnostics)");
                Console.WriteLine(" [5] Show Sector Status Snapshot (Scalar / Vector / Theta)");
                Console.WriteLine(" [0] Exit Framework");
                Console.WriteLine("=======================================================");
                Console.Write(" Select an option: ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        RunCluster_Diagnostics();
                        break;
                    case "2":
                        RunSparcGalacticAnalysis();
                        break;
                    case "3":
                        RunCmbAnalysis();
                        break;
                    case "4":
                        RunPantheonAnalysis();
                        break;
                    case "5":
                        ShowSectorStatusSnapshot();
                        break;

                    case "0":
                        Console.WriteLine("Exiting TRM Cosmology Framework. Goodbye!");
                        return;
                    default:                                                
                        Console.WriteLine("Invalid selection. Please enter a valid number.");
                        break;
                }
            }
        }
        private static void RunCluster_Diagnostics()
        {
            ClearConsole();
            Console.WriteLine("--- DOMAIN 2: CLUSTER DIAGNOSTICS ---");
            Console.WriteLine("This module provides diagnostic tools for analyzing galaxy cluster data.");

            string dataPath;
            string redshiftPath;

            try
            {
                dataPath = WorkspaceFileLocator.GetFilePath("Coma_Cluster_Chandra_temperature_all_profiles.dat");
                redshiftPath = WorkspaceFileLocator.GetFilePath("Coma_Cluster_Chandra_temperature_accept_main.tab");
            }
            catch(FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Required datasets not found.");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                return;
            }

            var dataDir = Path.GetDirectoryName(dataPath) ?? Directory.GetCurrentDirectory();

            if(!File.Exists(dataPath) || !File.Exists(redshiftPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Required datasets not found.");
                Console.WriteLine($"Please ensure the following files exist in: {dataDir}");
                Console.WriteLine($" - Coma_Cluster_Chandra_temperature_all_profiles.dat");
                Console.WriteLine($" - Coma_Cluster_Chandra_temperature_accept_main.tab");
                Console.ResetColor();
                return;
            }


            var analysis = new BulletClusterAnalysis2();

            Console.WriteLine("Loading cluster shells and redshifts...");

            var allClusters = analysis.LoadAllClusterShells(dataPath);
            var redshifts = analysis.LoadClusterRedshifts(redshiftPath);

            Console.WriteLine($"Successfully loaded {allClusters.Count} clusters.\n");

            // ✅ TRM-Geometrie aktivieren
            var scaling = TrmCosmologyParameters.Current();
            var mapper = new TrmDistanceMapper(scaling);
            analysis.ApplyTrmGeometry(allClusters, redshifts, mapper);

            // 🔧 gleiche Werte wie im Cluster-Run verwenden
            double optimizedThreshold = 5.30e-33;   // aus deinem letzten Sweep
            double optimizedBeta = 1.20;            // dito

            var diagnostics = analysis.RunClusterDiagnostics(
                allClusters,
                redshifts,
                pressureThreshold: optimizedThreshold,
                C: 1.3195,
                alpha: -0.7589,
                beta: optimizedBeta
            );
            analysis.PrintWeightSystematics(diagnostics);

            Console.WriteLine("\n--- CLUSTER DIAGNOSTICS ---");

            foreach(var d in diagnostics.OrderBy(x => x.Improvement))
            {
                Console.WriteLine(
                    $"{d.Name,-18} | z={d.Z:F3} | f={d.Fz:F3} | " +
                    $"gradP={d.MaxPressureGradient:E2} | " +
                    $"{d.Regime,-15} | {d.Improvement,5:F2}x | {d.Diagnosis}"
                );

            }
            var worst = diagnostics.Where(d => d.Improvement < 0.8);
            if(worst.Any())
            {
                Console.WriteLine("\n--- WORST PERFORMING CLUSTERS ---");
                foreach(var d in worst)
                {
                    Console.WriteLine(
                        $"{d.Name,-18} | z={d.Z:F3} | f={d.Fz:F3} | " +
                        $"gradP={d.MaxPressureGradient:E2} | " +
                        $"{d.Regime,-15} | {d.Improvement,5:F2}x | {d.Diagnosis}"
                    );

                }
            }

            Console.WriteLine("\nPress any key to return to the menu...");
            WaitForMenuReturn();
        }

        private static void RunSparcGalacticAnalysis()
        {
            ClearConsole();
            Console.WriteLine("--- DOMAIN 1: GALACTIC ROTATION CURVES (SPARC DATABASE) ---");
            Console.WriteLine("This module executes the non-linear co-fit for the universal acceleration constant a_0.");
            Console.WriteLine("Resolving SPARC datasets...");

            try
            {
                string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
                string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

                Console.WriteLine($" - Rotmod source: {zipPath}");
                Console.WriteLine($" - SPARC catalog: {mrtPath}");
                Console.WriteLine("\nRunning weighted global co-fit...");

                var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
                var inclinations = SparcMrtParser
                    .ParseFile(mrtPath)
                    .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

                var (trmLogA0, trmA0, trmRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);
                var (mondLogA0, mondA0, mondRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.MOND);

                Console.WriteLine("\n--- GLOBAL SPARC CO-FIT RESULTS ---");
                Console.WriteLine($"Analyzed points:                   {rarData.Count}");
                Console.WriteLine($"TRM  -> log10(a_0): {trmLogA0:F4} | a_0: {trmA0:E4} | RMS: {trmRms:F4} dex");
                Console.WriteLine($"MOND -> log10(a_0): {mondLogA0:F4} | a_0: {mondA0:E4} | RMS: {mondRms:F4} dex");

                Console.WriteLine("\n--- TURNING-MEMORY (NO-REFIT, HOLDOUT-ONLY) ---");
                PrintTurningMemoryModeReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.None,
                    "None");
                PrintTurningMemoryModeReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.BinBased,
                    "BinBased");
                PrintTurningMemoryModeReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.Interpolated,
                    "Interpolated");

                Console.WriteLine("\n--- TURNING-MEMORY GATE-COMPARISON (NO-REFIT, HOLDOUT-ONLY) ---");
                PrintTurningMemoryGateComparisonReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.BinBased,
                    "BinBased");
                PrintTurningMemoryGateComparisonReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.Interpolated,
                    "Interpolated");

                Console.WriteLine("\n--- DISK EDGE / SURFACE-COUPLING DIAGNOSTIC (NO-REFIT, HOLDOUT-ONLY) ---");
                PrintDiskEdgeSurfaceCouplingReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.BinBased,
                    "BinBased");
                PrintDiskEdgeSurfaceCouplingReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.Interpolated,
                    "Interpolated");

                Console.WriteLine("\n--- PHYSICAL DISK-STRUCTURE COUPLING DIAGNOSTIC (NO-REFIT, HOLDOUT-ONLY) ---");
                PrintPhysicalDiskStructureCouplingReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.BinBased,
                    "BinBased");
                PrintPhysicalDiskStructureCouplingReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.Interpolated,
                    "Interpolated");

                Console.WriteLine("\n--- OUTER-INNER TAKT SYNCHRONIZATION DIAGNOSTIC (NO-REFIT, HOLDOUT-ONLY) ---");
                PrintOuterInnerTaktSynchronizationReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.BinBased,
                    "BinBased");
                PrintOuterInnerTaktSynchronizationReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.Interpolated,
                    "Interpolated");

                Console.WriteLine("\n--- GLOBAL DISK-COHERENCE DIAGNOSTIC (NO-REFIT, HOLDOUT-ONLY) ---");
                PrintGlobalDiskCoherenceReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.BinBased,
                    "BinBased");
                PrintGlobalDiskCoherenceReport(
                    rarData,
                    trmA0,
                    TurningMemoryCorrectionMode.Interpolated,
                    "Interpolated");

                Console.WriteLine("\n--- WORST-GALAXY GEOMETRY-VARIATION DIAGNOSTIC (NO-REFIT) ---");
                PrintWorstGalaxyGeometryVariationReport(
                    rarData,
                    trmA0,
                    topGalaxyCount: 20);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUCCESS] SPARC fit completed.");
                Console.ResetColor();
            }
            catch(FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Required dataset missing: {ex.Message}");
                Console.ResetColor();
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] SPARC analysis failed: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to return to the menu...");
            WaitForMenuReturn();
        }

        private static void PrintTurningMemoryModeReport(
            List<RarPoint> rarData,
            double fixedA0,
            TurningMemoryCorrectionMode mode,
            string modeLabel)
        {
            var options = new TurningMemoryCorrectionOptions(
                Mode: mode,
                HoldoutModulo: 5,
                HoldoutRemainder: 0,
                BinCount: 3);

            var metrics = SparcRarAnalysis.EvaluateTurningMemoryCorrectionNoRefit(rarData, fixedA0, options);
            var improvements = metrics.PerGalaxyImprovement;

            int improvedCount = improvements.Count(x => x.DeltaRms > 0);
            int worsenedCount = improvements.Count(x => x.DeltaRms < 0);

            Console.WriteLine($"\n[{modeLabel}]");
            Console.WriteLine($"Baseline RMS:  {metrics.BaselineRms:F6}");
            Console.WriteLine($"Corrected RMS: {metrics.CorrectedRms:F6}");
            Console.WriteLine($"Delta RMS:     {metrics.DeltaRms:F6}");
            Console.WriteLine($"Improved galaxies: {improvedCount} | Worsened galaxies: {worsenedCount}");

            var topImproved = improvements
                .Where(x => x.DeltaRms > 0)
                .OrderByDescending(x => x.DeltaRms)
                .Take(10)
                .ToList();

            var topWorsened = improvements
                .Where(x => x.DeltaRms < 0)
                .OrderBy(x => x.DeltaRms)
                .Take(10)
                .ToList();

            Console.WriteLine("Top 10 improved:");
            if (topImproved.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in topImproved)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }

            Console.WriteLine("Top 10 worsened:");
            if (topWorsened.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in topWorsened)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }
        }

        private static void PrintTurningMemoryGateComparisonReport(
            List<RarPoint> rarData,
            double fixedA0,
            TurningMemoryCorrectionMode mode,
            string modeLabel)
        {
            var report = SparcRarAnalysis.EvaluateTurningMemoryGateComparisonNoRefit(
                rarData,
                fixedA0,
                mode,
                new TurningMemoryCorrectionOptions(
                    Mode: mode,
                    HoldoutModulo: 5,
                    HoldoutRemainder: 0,
                    BinCount: 3));

            Console.WriteLine($"\n[{modeLabel}]");
            PrintGateSummary(report.Baseline);
            PrintGateSummary(report.Ungated);
            PrintGateSummary(report.HsbSoftGated);

            foreach (var gradient in report.GradientSoftGated)
            {
                PrintGateSummary(gradient);
            }

            Console.WriteLine(
                $"Best gate: {report.BestGradientGate.GateVariable} | " +
                $"threshold={report.BestGradientGate.GateThreshold:F6} | " +
                $"width={report.BestGradientGate.GateWidth:F6} | " +
                $"delta RMS all={report.BestGradientGate.DeltaRmsAll:F6}");
        }

        private static void PrintGateSummary(TurningMemoryDiagnosticSummary summary)
        {
            Console.WriteLine($"\n{summary.Label}:");
            Console.WriteLine(
                $"  RMS all  baseline/corrected/delta: {summary.BaselineRmsAll:F6} / {summary.CorrectedRmsAll:F6} / {summary.DeltaRmsAll:F6}");
            Console.WriteLine(
                $"  RMS HSB  baseline/corrected/delta: {summary.BaselineRmsHsb:F6} / {summary.CorrectedRmsHsb:F6} / {summary.DeltaRmsHsb:F6}");
            Console.WriteLine(
                $"  RMS LSB  baseline/corrected/delta: {summary.BaselineRmsLsb:F6} / {summary.CorrectedRmsLsb:F6} / {summary.DeltaRmsLsb:F6}");
            Console.WriteLine(
                $"  Improved/Worsened galaxies: {summary.ImprovedGalaxyCount}/{summary.WorsenedGalaxyCount}");

            if (summary.GateVariable.HasValue)
            {
                Console.WriteLine(
                    $"  Gate: {summary.GateVariable} | threshold={summary.GateThreshold:F6} | width={summary.GateWidth:F6}");
            }

            Console.WriteLine("  Top improved:");
            if (summary.TopImproved.Count == 0)
            {
                Console.WriteLine("   - none");
            }
            else
            {
                foreach (var g in summary.TopImproved)
                {
                    Console.WriteLine(
                        $"   - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }

            Console.WriteLine("  Top worsened:");
            if (summary.TopWorsened.Count == 0)
            {
                Console.WriteLine("   - none");
            }
            else
            {
                foreach (var g in summary.TopWorsened)
                {
                    Console.WriteLine(
                        $"   - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }
        }

        private static void PrintDiskEdgeSurfaceCouplingReport(
            List<RarPoint> rarData,
            double fixedA0,
            TurningMemoryCorrectionMode mode,
            string modeLabel)
        {
            var report = SparcRarAnalysis.EvaluateDiskEdgeSurfaceCouplingNoRefit(
                rarData,
                fixedA0,
                mode,
                new TurningMemoryCorrectionOptions(
                    Mode: mode,
                    HoldoutModulo: 5,
                    HoldoutRemainder: 0,
                    BinCount: 3));

            Console.WriteLine($"\n[{modeLabel}]");
            Console.WriteLine(
                $"Baseline RMS(all): {report.BaselineRmsAll:F6} | " +
                $"Ungated RMS(all): {report.UngatedRmsAll:F6} | " +
                $"Ungated delta: {report.UngatedDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best proxy: {report.BestProxyName} | threshold={report.BestProxyThreshold:F6} | width={report.BestProxyWidth:F6}");
            Console.WriteLine(
                $"Best proxy corrected RMS(all): {report.BestProxyCorrectedRmsAll:F6} | " +
                $"delta: {report.BestProxyDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best proxy improved/worsened galaxies: {report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

            Console.WriteLine("Proxy correlations (residual, turning-delta):");
            foreach (var c in report.ProxyCorrelations)
            {
                Console.WriteLine(
                    $" - {c.ProxyName}: residual r/rho={c.ResidualPearson:F4}/{c.ResidualSpearman:F4}, " +
                    $"turning-delta r/rho={c.TurningDeltaPearson:F4}/{c.TurningDeltaSpearman:F4}");
            }

            Console.WriteLine("Top improved:");
            if (report.TopImproved.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopImproved)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }

            Console.WriteLine("Top worsened:");
            if (report.TopWorsened.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopWorsened)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }
        }

        private static void PrintPhysicalDiskStructureCouplingReport(
            List<RarPoint> rarData,
            double fixedA0,
            TurningMemoryCorrectionMode mode,
            string modeLabel)
        {
            var report = SparcRarAnalysis.EvaluatePhysicalDiskStructureCouplingNoRefit(
                rarData,
                fixedA0,
                mode,
                new TurningMemoryCorrectionOptions(
                    Mode: mode,
                    HoldoutModulo: 5,
                    HoldoutRemainder: 0,
                    BinCount: 3));

            Console.WriteLine($"\n[{modeLabel}]");
            Console.WriteLine(
                $"Baseline RMS(all): {report.BaselineRmsAll:F6} | Ungated RMS(all): {report.UngatedRmsAll:F6} | Ungated delta: {report.UngatedDeltaRmsAll:F6}");
            Console.WriteLine(
                $"RMS HSB baseline/ungated: {report.BaselineRmsHsb:F6}/{report.UngatedRmsHsb:F6}");
            Console.WriteLine(
                $"RMS LSB baseline/ungated: {report.BaselineRmsLsb:F6}/{report.UngatedRmsLsb:F6}");
            Console.WriteLine(
                $"Best physical proxy: {report.BestProxyName} | threshold={report.BestProxyThreshold:F6} | width={report.BestProxyWidth:F6}");
            Console.WriteLine(
                $"Best proxy corrected RMS(all): {report.BestProxyCorrectedRmsAll:F6} | delta: {report.BestProxyDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best proxy corrected RMS(HSB/LSB): {report.BestProxyCorrectedRmsHsb:F6}/{report.BestProxyCorrectedRmsLsb:F6}");
            Console.WriteLine(
                $"Best proxy improved/worsened galaxies: {report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

            Console.WriteLine("Physical proxy correlations (residual, turning-delta, HSB-LSB):");
            foreach (var c in report.ProxyCorrelations)
            {
                Console.WriteLine(
                    $" - {c.ProxyName}: residual r/rho={c.ResidualPearson:F4}/{c.ResidualSpearman:F4}, " +
                    $"turning-delta r/rho={c.TurningDeltaPearson:F4}/{c.TurningDeltaSpearman:F4}, HSB-LSB={c.HsbMinusLsb:F4}");
            }

            Console.WriteLine("Top improved:");
            if (report.TopImproved.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopImproved)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }

            Console.WriteLine("Top worsened:");
            if (report.TopWorsened.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopWorsened)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }
        }

        private static void PrintOuterInnerTaktSynchronizationReport(
            List<RarPoint> rarData,
            double fixedA0,
            TurningMemoryCorrectionMode mode,
            string modeLabel)
        {
            var report = SparcRarAnalysis.EvaluateOuterInnerTaktSynchronizationNoRefit(
                rarData,
                fixedA0,
                mode,
                new TurningMemoryCorrectionOptions(
                    Mode: mode,
                    HoldoutModulo: 5,
                    HoldoutRemainder: 0,
                    BinCount: 3));

            Console.WriteLine($"\n[{modeLabel}]");
            Console.WriteLine(
                $"Baseline RMS(all): {report.BaselineRmsAll:F6} | Ungated RMS(all): {report.UngatedRmsAll:F6} | Ungated delta: {report.UngatedDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best sync proxy: {report.BestProxyName} | threshold={report.BestProxyThreshold:F6} | width={report.BestProxyWidth:F6}");
            Console.WriteLine(
                $"Best proxy corrected RMS(all): {report.BestProxyCorrectedRmsAll:F6} | delta: {report.BestProxyDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best proxy improved/worsened galaxies: {report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

            Console.WriteLine("Sync proxy correlations (residual, turning-delta):");
            foreach (var c in report.ProxyCorrelations)
            {
                Console.WriteLine(
                    $" - {c.ProxyName}: residual r/rho={c.ResidualPearson:F4}/{c.ResidualSpearman:F4}, " +
                    $"turning-delta r/rho={c.TurningDeltaPearson:F4}/{c.TurningDeltaSpearman:F4}");
            }

            Console.WriteLine("Top improved:");
            if (report.TopImproved.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopImproved)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }

            Console.WriteLine("Top worsened:");
            if (report.TopWorsened.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopWorsened)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }
        }

        private static void PrintGlobalDiskCoherenceReport(
            List<RarPoint> rarData,
            double fixedA0,
            TurningMemoryCorrectionMode mode,
            string modeLabel)
        {
            var report = SparcRarAnalysis.EvaluateGlobalDiskCoherenceNoRefit(
                rarData,
                fixedA0,
                mode,
                new TurningMemoryCorrectionOptions(
                    Mode: mode,
                    HoldoutModulo: 5,
                    HoldoutRemainder: 0,
                    BinCount: 3));

            Console.WriteLine($"\n[{modeLabel}]");
            Console.WriteLine(
                $"Baseline RMS(all): {report.BaselineRmsAll:F6} | Ungated RMS(all): {report.UngatedRmsAll:F6} | Ungated delta: {report.UngatedDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best coherence proxy: {report.BestProxyName} | threshold={report.BestProxyThreshold:F6} | width={report.BestProxyWidth:F6}");
            Console.WriteLine(
                $"Best proxy corrected RMS(all): {report.BestProxyCorrectedRmsAll:F6} | delta: {report.BestProxyDeltaRmsAll:F6}");
            Console.WriteLine(
                $"Best proxy improved/worsened galaxies: {report.BestProxyImprovedGalaxyCount}/{report.BestProxyWorsenedGalaxyCount}");

            Console.WriteLine("Coherence proxy correlations (residual, turning-delta):");
            foreach (var c in report.ProxyCorrelations)
            {
                Console.WriteLine(
                    $" - {c.ProxyName}: residual r/rho={c.ResidualPearson:F4}/{c.ResidualSpearman:F4}, " +
                    $"turning-delta r/rho={c.TurningDeltaPearson:F4}/{c.TurningDeltaSpearman:F4}");
            }

            Console.WriteLine("Top improved:");
            if (report.TopImproved.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopImproved)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }

            Console.WriteLine("Top worsened:");
            if (report.TopWorsened.Count == 0)
            {
                Console.WriteLine(" - none");
            }
            else
            {
                foreach (var g in report.TopWorsened)
                {
                    Console.WriteLine(
                        $" - {g.GalaxyKey}: delta={g.DeltaRms:F5}, baseline={g.BaselineRms:F5}, corrected={g.CorrectedRms:F5}, n={g.PointCount}");
                }
            }
        }

        private static void PrintWorstGalaxyGeometryVariationReport(
            List<RarPoint> rarData,
            double fixedA0,
            int topGalaxyCount)
        {
            var report = SparcRarAnalysis.EvaluateWorstGalaxyGeometryVariationNoRefit(
                rarData,
                fixedA0,
                topGalaxyCount);

            Console.WriteLine(
                $"Fixed a0: {report.FixedA0:E4} | worst galaxies analyzed: {report.TopGalaxyCount} | best variant: {report.BestVariantName}");
            Console.WriteLine(
                $"Smooth kernel (train-fitted): {report.BestSmoothKernelKind} width={report.BestSmoothKernelWidthKpc:F2} kpc | " +
                $"smooth>single: {report.SmoothBeatsSingleCount} | smooth>toy: {report.SmoothBeatsToyCount}");
            Console.WriteLine(
                $"Mean smooth delta vs single: {report.MeanSmoothDeltaVsSingle:F6} | " +
                $"Mean smooth delta vs toy: {report.MeanSmoothDeltaVsToy:F6}");

            Console.WriteLine("Variant vs disk-structure proxy correlations:");
            foreach (var c in report.VariantCorrelations)
            {
                Console.WriteLine(
                    $" - {c.VariantName}: outer/inner r/rho={c.OuterInnerRatioPearson:F4}/{c.OuterInnerRatioSpearman:F4}, " +
                    $"gas r/rho={c.GasDominancePearson:F4}/{c.GasDominanceSpearman:F4}, " +
                    $"span r/rho={c.RadialSpanPearson:F4}/{c.RadialSpanSpearman:F4}, " +
                    $"n r/rho={c.PointCountPearson:F4}/{c.PointCountSpearman:F4}, " +
                    $"structure-correlated={c.CorrelatesWithDiskStructure}");
            }

            Console.WriteLine("Per-galaxy worst-baseline geometry variation:");
            foreach (var g in report.Galaxies)
            {
                Console.WriteLine(
                    $" - {g.GalaxyKey}: baseline={g.BaselineRms:F5} | single={g.SingleCenterRms:F5} (d={g.SingleCenterDelta:F5}) | " +
                    $"distributed={g.DiskDistributedRms:F5} (d={g.DiskDistributedDelta:F5}) | multi={g.MultiCenterToyRms:F5} (d={g.MultiCenterToyDelta:F5}) | " +
                    $"smooth={g.SmoothDistributedFieldRms:F5} (d={g.SmoothDistributedFieldDelta:F5}) | " +
                    $"outer/inner={g.OuterInnerRatio:F4} | gas={g.GasDominance:F4} | span={g.RadialSpanKpc:F2} | n={g.PointCount} | " +
                    $"proxy-aligned={g.ImprovementCorrelatesWithDiskStructure}");
            }
        }

        private static void RunCmbAnalysis()
        {
            ClearConsole();
            Console.WriteLine("--- DOMAIN 3: COSMIC MICROWAVE BACKGROUND (TRM k-SPACE + SCALE TEST) ---");
            Console.WriteLine("Initializing high-performance k-space sweep...");

            var solver = new CmbAcousticSolver();

            // 1. Acoustic k-space structure
            var acoustic = solver.FindPerfectPhysicalParameters();

            Console.WriteLine("\n--- TRM CMB k-SPACE RESULT ---");
            Console.WriteLine($"Isolated TRM Drive Frequency:        {acoustic.TrmDriveFreq:F3}");
            Console.WriteLine($"Kinetic Doppler Weight:              {acoustic.DopplerWeight:F3}");
            Console.WriteLine($"K1:                                  {acoustic.K1:F6}");
            Console.WriteLine($"K2:                                  {acoustic.K2:F6}");
            Console.WriteLine($"Peak Ratio K2/K1:                    {acoustic.PeakRatio:F6}");
            Console.WriteLine($"Ratio Fitness:                       {acoustic.Fitness:F6}");

            // 2. TRM scale consistency
            var scaling = TrmCosmologyParameters.Current();
            double cs = 1.0 / Math.Sqrt(3.0);

            var prediction = solver.CalculateCmbScalePrediction(
                acoustic.K1,
                cs,
                acoustic.TrmDriveFreq,
                scaling);

            Console.WriteLine("\n--- TRM CMB SCALE PREDICTION ---");
            Console.WriteLine($"HT:                                  {scaling.HT:F3} km/s/Mpc");
            Console.WriteLine($"BetaEta:                             {scaling.BetaEta:F6}");
            Console.WriteLine($"Alpha:                               {scaling.Alpha:F3}");
            Console.WriteLine($"etaRec:                              {prediction.EtaRec:F6}");
            Console.WriteLine($"zRec:                                {prediction.ZRec:F6}");
            Console.WriteLine($"Angular Diameter Distance:           {prediction.AngularDiameterDistance:F2} Mpc");
            Console.WriteLine($"Predicted First Multipole lPred:     {prediction.LPred:F2}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCCESS] CMB k-space structure and TRM scale prediction evaluated.");
            Console.ResetColor();

            Console.WriteLine("\nPress any key to return to the menu...");
            WaitForMenuReturn();
        }
        private static void RunPantheonAnalysis()
        {
            ClearConsole();
            Console.WriteLine("--- DOMAIN 4: SUPERNOVAE & DISTANCE SCALE (PANTHEON+ DATABASE) ---");
            Console.WriteLine("Evaluating Pantheon+ with the current TRM distance mapper...");

            string dataPath;
            try
            {
                dataPath = WorkspaceFileLocator.GetFilePath("Pantheon+SH0ES.dat");
            }
            catch (FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[ERROR] Pantheon+ dataset not found.");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine("\nPress any key to return to the menu...");
                WaitForMenuReturn();
                return;
            }

            var loader = new PantheonDataLoader();
            var snData = loader.LoadPantheonData(dataPath);

            var scaling = TrmCosmologyParameters.Current();
            var mapper = new TrmDistanceMapper(scaling);
            var solver = new PantheonTrmScaleSolver(mapper);

            var result = solver.Evaluate(snData);

            Console.WriteLine("\n--- TRM PANTHEON SCALE-DISTANCE RESULTS ---");
            Console.WriteLine($"Analyzed Supernovae:                {result.AnalyzedPoints}");
            Console.WriteLine($"HT:                                  {scaling.HT:F3} km/s/Mpc");
            Console.WriteLine($"BetaEta:                             {scaling.BetaEta:F6}");
            Console.WriteLine($"Alpha:                               {scaling.Alpha:F3}");
            Console.WriteLine($"RMS Error:                           {result.RmsError:F6} mag");
            Console.WriteLine($"Mean Residual:                       {result.MeanResidual:F6} mag");
            Console.WriteLine($"Mean Absolute Residual:              {result.MeanAbsResidual:F6} mag");
            Console.WriteLine($"Centered RMS Error:                  {result.CenteredRmsError:F6} mag");
            Console.WriteLine($"Max Absolute Residual:               {result.MaxAbsResidual:F6} mag");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCCESS] Pantheon+ evaluated with the current TRM distance scale.");
            Console.ResetColor();

            Console.WriteLine("\nPress any key to return to the menu...");
            WaitForMenuReturn();
        }

        private static void ShowSectorStatusSnapshot()
        {
            ClearConsole();
            Console.WriteLine("--- TRM FIELD SECTOR STATUS SNAPSHOT ---");
            Console.WriteLine("Positioning: weak-field effective transport/synchronization framework.");
            Console.WriteLine();
            Console.WriteLine("[Scalar sector] T, phi, memory, EL/Fermat bridge");
            Console.WriteLine(" - tested-effective in current weak-field pipeline");
            Console.WriteLine(" - not yet full first-principles closure");
            Console.WriteLine();
            Console.WriteLine("[Vector sector] A_T, B_T (frame-dragging candidate)");
            Console.WriteLine(" - FD01-FD15: structural weak-field LT-shape compatibility, stable effective k_T");
            Console.WriteLine(" - scalar-limit preserved at spin zero, prograde/retrograde asymmetry present");
            Console.WriteLine(" - not yet quantitative GR-equivalent, not first-principles-derived");
            Console.WriteLine();
            Console.WriteLine("[Theta sector] Theta, O5, lambda_Theta");
            Console.WriteLine(" - TO01-TO28, TQK01-TQK04, LC01-LC08 tested-effective candidate path");
            Console.WriteLine(" - not yet theorem-level first-principles closure");
            Console.WriteLine();
            Console.WriteLine("See docs/Theory/TRM_Field_Sector_Map.md and docs/Theory/TRM_First_Principles_Gap_List.md");
            Console.WriteLine("\nPress any key to return to the menu...");
            WaitForMenuReturn();
        }

        private static void WaitForMenuReturn()
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return;
            }

            try
            {
                Console.ReadKey(intercept: true);
            }
            catch (InvalidOperationException)
            {
                // Non-interactive host: skip blocking wait.
            }
        }
        
        private static void ClearConsole()
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }

            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                // Ignore and try ANSI clear sequence fallback.
            }

            Console.Write("\u001b[3J\u001b[2J\u001b[H");
        }

        private static void DrawBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  _______ _____  __  __   _____                          _                   
 |__   __|  __ \|  \/  | / ____|                        | |                  
    | |  | |__) | \  / || |     ___  ___ _ __ ___   ___ | | ___   __ _ _   _ 
    | |  |  _  /| |\/| || |    / _ \/ __| '_ ` _ \ / _ \| |/ _ \ / _` | | | |
    | |  | | \ \| |  | || |___| (_) \__ \ | | | | | (_) | | (_) | (_| | |_| |
    |_|  |_|  \_\_|  |_| \_____\___/|___/_| |_| |_|\___/|_|\___/ \__, |\__, |
                                                                  __/ | __/ |
                                                                 |___/ |___/ 
            ");
            Console.WriteLine("   Clockwork Cosmology V3.0 - Theoretical Physics Evaluation Engine");
            Console.WriteLine("   Scope: tested-effective weak-field candidate sectors (no theorem-level overclaims)");
            Console.ResetColor();
        }
    }
}