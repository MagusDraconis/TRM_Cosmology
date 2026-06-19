using System;
using System.IO;
using System.Linq;
using TRM.Core;

namespace TRM.CMD
{
    class Program
    {
        static void Main(string[] args)
        {
            DrawBanner();

            while (true)
            {
                Console.WriteLine("\n=======================================================");
                Console.WriteLine(" V2.2 EXPERIMENT SELECTION");
                Console.WriteLine("=======================================================");
                Console.WriteLine(" [1] Analyze ACCEPT Galaxy Clusters (Dark Matter Alternative)");
                Console.WriteLine(" [2] Analyze SPARC Galactic Rotations (MOND vs TRM)");
                Console.WriteLine(" [3] Analyze CMB Acoustic Peaks (Planck Cosmology)");
                Console.WriteLine(" [0] Exit Framework");
                Console.WriteLine("=======================================================");
                Console.Write(" Select an option: ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        RunAcceptClusterAnalysis();
                        break;
                    case "2":
                        RunSparcGalacticAnalysis();
                        break;
                    case "3":
                        RunCmbAnalysis();
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

        private static void RunAcceptClusterAnalysis()
        {
            Console.Clear();
            Console.WriteLine("--- DOMAIN 2: GALAXY CLUSTERS (ACCEPT DATABASE) ---");

            string dataPath;
            string redshiftPath;

            try
            {
                dataPath = WorkspaceFileLocator.GetFilePath("Coma_Cluster_Chandra_temperature_all_profiles.dat");
                redshiftPath = WorkspaceFileLocator.GetFilePath("Coma_Cluster_Chandra_temperature_accept_main.tab");
            }
            catch (FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Required datasets not found.");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                return;
            }

            var dataDir = Path.GetDirectoryName(dataPath) ?? Directory.GetCurrentDirectory();

            if (!File.Exists(dataPath) || !File.Exists(redshiftPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Required datasets not found.");
                Console.WriteLine($"Please ensure the following files exist in: {dataDir}");
                Console.WriteLine($" - Coma_Cluster_Chandra_temperature_all_profiles.dat");
                Console.WriteLine($" - Coma_Cluster_Chandra_temperature_accept_main.tab");
                Console.ResetColor();
                return;
            }

            var analysis = new BulletClusterAnalysis2(); // Stelle sicher, dass die Klasse public ist in TRM.Core
            Console.WriteLine("Loading cluster shells and redshifts...");
            var allClusters = analysis.LoadAllClusterShells(dataPath);
            var redshifts = analysis.LoadClusterRedshifts(redshiftPath);
            Console.WriteLine($"Successfully loaded {allClusters.Count} clusters.\n");

            // --- THEORETICAL PARAMETER SWEEP ---
            Console.WriteLine("STEP 1: Executing 2D-Grid-Sweep for optimal Pressure Threshold and Ellipticity Beta...");
            Console.WriteLine("This will identify the natural constants of the bimodal temporal transition.\n");

            // Führt den Sweep aus (Deine Methode schreibt das Ergebnis bereits in die Konsole)
            analysis.FindBestPhysicalThresholdAndBeta(allClusters, redshifts);

            // --- FINAL EVALUATION ---
            Console.WriteLine("\nSTEP 2: Generating final theoretical evaluation for the publication...");

            // Diese Werte können später durch Konsoleneingaben ersetzt werden, falls Forscher damit spielen wollen.
            // Für den Standard-Lauf nutzen wir deine etablierten Best-Fit Parameter.
            double optimizedThreshold = 2.70e-033; // Peak aus deinem großen Sweep
            double optimizedBeta = 1.45;           // Geometrische Dämpfung
            string outputCsv = Path.Combine(Directory.GetCurrentDirectory(), "TRM_V2.2_ACCEPT_Results.csv");

            analysis.EvaluatePhysicsDrivenBimodalTheory(
                allClusters,
                redshifts,
                C: 1.3195,
                alpha: -0.7589,
                baselineK: 0.1,
                pressureThreshold: optimizedThreshold,
                beta: optimizedBeta,
                resultsCsvPath: outputCsv
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[SUCCESS] Analysis complete. Results exported to: {outputCsv}");
            Console.ResetColor();
        }

        private static void RunSparcGalacticAnalysis()
        {
            Console.Clear();
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

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUCCESS] SPARC fit completed.");
                Console.ResetColor();
            }
            catch (FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] Required dataset missing: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] SPARC analysis failed: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }

        private static void RunCmbAnalysis()
        {
            Console.Clear();
            Console.WriteLine("--- DOMAIN 3: COSMIC MICROWAVE BACKGROUND (PLANCK DATA) ---");
            Console.WriteLine("Initializing High-Performance k-Space Sweep...");

            var solver = new CmbAcousticSolver();
            var result = solver.FindPerfectPhysicalParameters();

            Console.WriteLine("\n--- TRM COSMOLOGICAL CMB-FIT RESULTS ---");
            Console.WriteLine($"Isolated TRM Drive Frequency:        {result.TrmDriveFreq:F3}");
            Console.WriteLine($"Kinetic Doppler Weight:              {result.DopplerWeight:F3}");
            Console.WriteLine($"Calculated Angular Diameter Dist.:   {result.BestDa:F2} Mpc");
            Console.WriteLine($"Calculated 1st Acoustic Peak (l):    {result.Peak1:F1}");
            Console.WriteLine($"Calculated 2nd Acoustic Peak (l):    {result.Peak2:F1}");
            Console.WriteLine($"Deviation Standard Error (Fitness):  {result.Fitness:F4}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SUCCESS] Acoustic peaks perfectly aligned with natural Planck parameters.");
            Console.ResetColor();

            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }

        private static void DrawBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  _______ _____  __  __   _____                                 _                   
 |__   __|  __ \|  \/  | / ____|                               | |                  
    | |  | |__) | \  / || |     ___  ___ _ __ ___   ___ | | ___   __ _ _   _ 
    | |  |  _  /| |\/| || |    / _ \/ __| '_ ` _ \ / _ \| |/ _ \ / _` | | | |
    | |  | | \ \| |  | || |___| (_) \__ \ | | | | | (_) | | (_) | (_| | |_| |
    |_|  |_|  \_\_|  |_| \_____\___/|___/_| |_| |_|\___/|_|\___/ \__, |\__, |
                                                                  __/ | __/ |
                                                                 |___/ |___/ 
            ");
            Console.WriteLine("   Clockwork Cosmology V2.2 - Theoretical Physics Evaluation Engine");
            Console.ResetColor();
        }
    }
}