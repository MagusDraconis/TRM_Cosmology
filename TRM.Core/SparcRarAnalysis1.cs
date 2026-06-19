using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace TRM.Core
{

    public class SparcRarAnalysis1
    {
        // Umrechnungsfaktor von (km/s)^2 / kpc in m/s^2:
        // (10^3 m/s)^2 / (3.08567758 * 10^19 m) = 10^6 / 3.08567758 * 10^19
        private const double Kms2KpcToMs2 = 3.240779289e-14;
        static HashSet<string> blacklist = new(StringComparer.OrdinalIgnoreCase)
            {
                "CamB", "NGC6789", "UGC07399", "UGC11557", "KK98-251",
                "UGC07125", "UGC08837", "F568-V1", "NGC2915", "UGC06667"
            };


        public static List<RarPoint> ParseRarFromZip(string zipPath, double upsilonDisk = 0.5, double upsilonBulge = 0.7)
        {
            var rarPoints = new List<RarPoint>();

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"Die Datei {zipPath} wurde nicht gefunden.");

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".rot", StringComparison.OrdinalIgnoreCase) &&
                        !entry.FullName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string galaxyName = Path.GetFileNameWithoutExtension(entry.Name)
                                        .Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase);

                    using (Stream stream = entry.Open())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                                continue;

                            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5)
                            {
                                try
                                {
                                    double r = double.Parse(parts[0], CultureInfo.InvariantCulture);
                                    double vObs = double.Parse(parts[1], CultureInfo.InvariantCulture);

                                    double vGas, vDisk, vBulge;
                                    double errVobs = 0;

                                    if (parts.Length >= 6)
                                    {
                                        errVobs = double.Parse(parts[2], CultureInfo.InvariantCulture);
                                        vGas = double.Parse(parts[3], CultureInfo.InvariantCulture);
                                        vDisk = double.Parse(parts[4], CultureInfo.InvariantCulture);
                                        vBulge = double.Parse(parts[5], CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        vGas = double.Parse(parts[2], CultureInfo.InvariantCulture);
                                        vDisk = double.Parse(parts[3], CultureInfo.InvariantCulture);
                                        vBulge = double.Parse(parts[4], CultureInfo.InvariantCulture);
                                    }

                                    // --- PHYSIKALISCHES PRUNING ---
                                    // 1. Division-by-Zero und instabile Kernregionen (R < 0.5 kpc) ausschließen
                                    if (r < 0.5) continue;

                                    // 2. Extrem verrauschte Datenpunkte ausschließen (Fehler in Vobs > 15% oder > 20 km/s)
                                    if (parts.Length >= 6 && (errVobs > 20.0 || (vObs > 0 && errVobs / vObs > 0.15)))
                                        continue;

                                    // 3. Nur Punkte mit stabiler Rotation betrachten
                                    if (vObs < 10.0) continue;

                                    double gObsAstronomical = (vObs * vObs) / r;
                                    double gObsMs2 = gObsAstronomical * Kms2KpcToMs2;

                                    double vDiskSq = vDisk > 0 ? vDisk * vDisk : 0;
                                    double vBulgeSq = vBulge > 0 ? vBulge * vBulge : 0;
                                    double vGasSq = vGas > 0 ? vGas * vGas : 0;

                                    double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (upsilonBulge * vBulgeSq);

                                    if (vBarSq > 0)
                                    {
                                        double gBarAstronomical = vBarSq / r;
                                        double gBarMs2 = gBarAstronomical * Kms2KpcToMs2;

                                        // Asymmetrische Schocks ausschließen (wenn beobachtete Energie massiv unter der baryonischen liegt)
                                        if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
                                        {
                                            rarPoints.Add(new RarPoint(
                                                galaxyName, r, vObs, vGas, vDisk, vBulge, gObsMs2, gBarMs2
                                            ));
                                        }
                                    }

                                }
                                catch (FormatException)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            return rarPoints;
        }

        public static List<RarPoint> ParseRarWithFixedWidthInclinationFilter(string zipPath, string mrtPath, double upsilonDisk = 0.5, double upsilonBulge = 0.7)
        {
            var validGalaxies = new Dictionary<string, (double Inclination, int Quality)>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"Die Datei {zipPath} wurde nicht gefunden.");

            if (!File.Exists(mrtPath))
                throw new FileNotFoundException($"Die Datei {mrtPath} wurde nicht gefunden.");

            bool dataSectionStarted = false;
            int separatorCount = 0;

            foreach (var line in File.ReadLines(mrtPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Datenblock startet nach der dritten "-----" Trennlinie
                if (trimmed.StartsWith("----", StringComparison.Ordinal))
                {
                    separatorCount++;
                    if (separatorCount >= 3)
                        dataSectionStarted = true;

                    continue;
                }

                if (!dataSectionStarted || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 18)
                    continue;

                string galaxyKey = NormalizeGalaxyKey(parts[0]);
                if (double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double inc) &&
                    int.TryParse(parts[17], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quality) &&
                    !validGalaxies.ContainsKey(galaxyKey))
                {
                    validGalaxies.Add(galaxyKey, (inc, quality));
                }
            }

            var rarPoints = new List<RarPoint>();

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".rot", StringComparison.OrdinalIgnoreCase) &&
                        !entry.FullName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Bereinige den Galaxiennamen aus dem Dateinamen
                    string galaxyName = Path.GetFileNameWithoutExtension(entry.Name)
                                        .Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase)
                                        .Trim();
                    string galaxyKey = NormalizeGalaxyKey(galaxyName);

                    // Cross-Matching mit der fehlerfreien Master-Tabelle
                    if (validGalaxies.TryGetValue(galaxyKey, out var galaxyMeta))
                    {
                        // Gesetzmäßiger Filter: Face-On-Galaxien verzerren die Kreisbahngeschwindigkeit quadratisch
                        if (galaxyMeta.Inclination < 30.0 || galaxyMeta.Quality >= 3)
                            continue;
                    }
                    else
                    {
                        // Galaxie nicht in Tabelle 1 vorhanden -> Ausschließen zur Absicherung der Datenreinheit
                        continue;
                    }

                    using (Stream stream = entry.Open())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string dataLine;
                        while ((dataLine = reader.ReadLine()) != null)
                        {
                            string trimmed = dataLine.Trim();
                            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                                continue;

                            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 5)
                            {
                                if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double r) ||
                                    !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double vObs))
                                    continue;

                                double errVobs = 0;
                                int componentStart = 2;
                                if (parts.Length >= 6)
                                {
                                    if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out errVobs))
                                        continue;
                                    componentStart = 3;
                                }

                                if (parts.Length <= componentStart + 2)
                                    continue;

                                if (!double.TryParse(parts[componentStart], NumberStyles.Float, CultureInfo.InvariantCulture, out double vGas) ||
                                    !double.TryParse(parts[componentStart + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double vDisk) ||
                                    !double.TryParse(parts[componentStart + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double vBulge))
                                    continue;

                                // Lokales Pruning
                                if (r < 0.5) continue;
                                if (vObs < 10.0) continue;
                                if (parts.Length >= 6 && (errVobs > 20.0 || (vObs > 0 && errVobs / vObs > 0.15)))
                                    continue;

                                double gObsAstronomical = (vObs * vObs) / r;
                                double gObsMs2 = gObsAstronomical * Kms2KpcToMs2;

                                double vDiskSq = vDisk > 0 ? vDisk * vDisk : 0;
                                double vBulgeSq = vBulge > 0 ? vBulge * vBulge : 0;
                                double vGasSq = vGas > 0 ? vGas * vGas : 0;

                                double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (upsilonBulge * vBulgeSq);

                                if (vBarSq > 0)
                                {
                                    double gBarAstronomical = vBarSq / r;
                                    double gBarMs2 = gBarAstronomical * Kms2KpcToMs2;

                                    if (gObsMs2 > 0 && gBarMs2 > 0 && (gObsMs2 / gBarMs2 > 0.01))
                                    {
                                        rarPoints.Add(new RarPoint(
                                            galaxyName, r, vObs, vGas, vDisk, vBulge, gObsMs2, gBarMs2
                                        ));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return rarPoints;
        }




        public record RarBinResult(
                    double LogGbarCenter,
                    double MeanLogGobs,
                    double StandardDeviation,
                    int PointCount
                );

        public static List<RarBinResult> ComputeRarProfiles(List<RarPoint> points, double binSize = 0.2)
        {
            // Wir gruppieren die Daten im Log10-Raum der baryonischen Beschleunigung
            var groupedPoints = points
                .GroupBy(p => Math.Floor(Math.Log10(p.GbarMs2) / binSize) * binSize)
                .OrderBy(g => g.Key)
                .ToList();

            var binResults = new List<RarBinResult>();

            foreach (var group in groupedPoints)
            {
                double binCenter = group.Key + (binSize / 2.0);

                // Logarithmierte beobachtete Beschleunigungen in diesem Intervall
                var logGobsValues = group.Select(p => Math.Log10(p.GobsMs2)).ToList();

                int count = logGobsValues.Count;
                if (count < 5) continue; // Ignoriere unterbesetzte Randbereiche

                double meanLogGobs = logGobsValues.Average();

                // Varianz und Standardabweichung berechnen
                double sumOfSquares = logGobsValues.Sum(v => Math.Pow(v - meanLogGobs, 2));
                double stdDev = Math.Sqrt(sumOfSquares / count);

                binResults.Add(new RarBinResult(binCenter, meanLogGobs, stdDev, count));
            }

            return binResults;
        }
        /// <summary>
        /// Berechnet die theoretische beobachtete Beschleunigung basierend auf der 
        /// standardmäßigen RAR-Interpolationsfunktion für einen gegebenen a0-Wert.
        /// </summary>
        public static double PredictGobs(double gBar, double a0)
        {
            if (gBar <= 0 || a0 <= 0) return 0;

            // g_obs = g_bar / (1 - exp(-sqrt(g_bar / a_0)))
            double absoluteDeviation = Math.Sqrt(gBar / a0);
            return gBar / (1.0 - Math.Exp(-absoluteDeviation));
        }

        /// <summary>
        /// Führt eine hochpräzise zweistufige Rastersuche durch, um dasjenige log10(a0) zu finden,
        /// welches die Summe der Quadrate der Log-Residuen über alle Datenpunkte minimiert.
        /// </summary>
        public static (double BestLogA0, double BestA0, double RmsError) FitA0(
            List<RarPoint> points,
            Dictionary<string, double>? inclinations = null)
        {


            if (points == null || points.Count == 0)
                throw new ArgumentException("Keine Datenpunkte für den Fit übergeben.");

            var normalizedInclinations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (inclinations is { Count: > 0 })
            {
                foreach (var kvp in inclinations!)
                {
                    var key = NormalizeGalaxyKey(kvp.Key);
                    if (!normalizedInclinations.ContainsKey(key))
                        normalizedInclinations[key] = kvp.Value;
                }
            }

            // Heuristische Upsilon-Map vor dem Fit erzeugen
            var upsilonDiskMap = EstimateUpsilonMap(points);

            double bestLogA0 = 0;
            double minSsr = double.MaxValue; // Sum of Squared Residuals

            // Erste Stufe: Grobes Raster von -11.0 bis -9.0 mit Schrittweite 0.01
            for (double logA0 = -11.0; logA0 <= -9.0; logA0 += 0.01)
            {
                double a0 = Math.Pow(10, logA0);
                double ssr = CalculateWeightedSsr(points, a0, normalizedInclinations, upsilonDiskMap);

                if (ssr < minSsr)
                {
                    minSsr = ssr;
                    bestLogA0 = logA0;
                }
            }

            // Zweite Stufe: Feines Kämmen um das gefundene Minimum herum (±0.02) mit Schrittweite 0.0001
            double startFine = bestLogA0 - 0.02;
            double endFine = bestLogA0 + 0.02;

            for (double logA0 = startFine; logA0 <= endFine; logA0 += 0.0001)
            {
                double a0 = Math.Pow(10, logA0);
                double ssr = CalculateWeightedSsr(points, a0, normalizedInclinations, upsilonDiskMap);

                if (ssr < minSsr)
                {
                    minSsr = ssr;
                    bestLogA0 = logA0;
                }
            }

            // Root Mean Square Error (Standardfehler) berechnen
            double rmsError = Math.Sqrt(minSsr);
            double bestA0 = Math.Pow(10, bestLogA0);

            return (bestLogA0, bestA0, rmsError);
        }

        private static string NormalizeGalaxyKey(string name)
    => name.Replace("_rotmod", "", StringComparison.OrdinalIgnoreCase)
           .Replace(" ", "", StringComparison.Ordinal)
           .Trim();

        public static void AnalyzeOutliers(
    List<RarPoint> points,
    double bestA0,
    Dictionary<string, double> inclinations,
    Dictionary<string, double> upsilonDiskMap)
        {
            Console.WriteLine("\n--- Residuen-Analyse der Galaxien ---");

            // Wir gruppieren nach Galaxie, um den durchschnittlichen Fehler pro Galaxie zu sehen
            var residuals = points.GroupBy(p => p.GalaxyName)
                .Select(group =>
                {
                    double sumResidual = 0;
                    int count = 0;

                    // Re-berechne das Residuum für diese Galaxie mit dem BESTEN A0
                    foreach (var p in group)
                    {
                        string galaxyKey = NormalizeGalaxyKey(p.GalaxyName);
                        double inc = inclinations.GetValueOrDefault(galaxyKey, 60.0);
                        double sinI = Math.Sin(inc * Math.PI / 180.0);
                        double upsilonDisk = upsilonDiskMap.GetValueOrDefault(galaxyKey, 0.5);

                        double vBarSq = (p.Vgas * p.Vgas) + (upsilonDisk * p.Vdisk * p.Vdisk) + (0.7 * p.Vbulge * p.Vbulge);
                        double gBarMs2 = (vBarSq / p.RadiusKpc) * Kms2KpcToMs2;

                        double gObsPredicted = PredictGobs(gBarMs2, bestA0);
                        if (gObsPredicted > 0)
                        {
                            double res = Math.Abs(Math.Log10(p.GobsMs2) - Math.Log10(gObsPredicted));
                            sumResidual += res;
                            count++;
                        }
                    }
                    return new { Name = group.Key, AvgResidual = count > 0 ? sumResidual / count : 0 };
                })
                .OrderByDescending(x => x.AvgResidual) // Die schlimmsten Ausreißer zuerst!
                .ToList();

            foreach (var item in residuals.Take(20)) // Zeige die Top 20 "Problemkinder"
            {
                Console.WriteLine($"Galaxie: {item.Name,-15} | Avg Residuum: {item.AvgResidual:F4}");
            }
        }
        private static double CalculateWeightedSsr(
                List<RarPoint> points,
                double a0,
                Dictionary<string, double> inclinations,
                Dictionary<string, double> upsilonDiskMap) // NEU: Mapping pro Galaxie
        {
            double totalWeight = 0;
            double weightedSsr = 0;
            foreach (var p in points)
            {
                string galaxyKey = NormalizeGalaxyKey(p.GalaxyName);
                // Checken, ob Galaxie auf der Blacklist steht
                if (blacklist.Contains(galaxyKey))
                {
                    continue; // Diese Galaxie komplett ignorieren
                }
                // 1. Inklination holen
                double inc = inclinations.GetValueOrDefault(galaxyKey, 60.0); // Fallback falls keine Inklination vorhanden
                double sinI = Math.Sin(inc * Math.PI / 180.0);
                double weight = sinI * sinI;

                // 2. Individuelles Upsilon holen (Default 0.5, falls nicht in der Map)
                double upsilonDisk = upsilonDiskMap.GetValueOrDefault(galaxyKey, 0.5);

                // 3. Baryonische Beschleunigung mit dynamischem Upsilon berechnen
                // g_bar = (v_gas^2 + upsilon * v_disk^2 + upsilon_bulge * v_bulge^2) / r
                double vDiskSq = p.Vdisk * p.Vdisk;
                double vBulgeSq = p.Vbulge * p.Vbulge;
                double vGasSq = p.Vgas * p.Vgas;

                // Wir gehen davon aus, dass Bulge-Upsilon stabil bleibt (da meist alte Population)
                double vBarSq = vGasSq + (upsilonDisk * vDiskSq) + (0.7 * vBulgeSq);
                double gBarMs2 = (vBarSq / p.RadiusKpc) * Kms2KpcToMs2;

                // Restliche Berechnung (Log-Residuen)
                double logGobsActual = Math.Log10(p.GobsMs2);
                double gObsPredicted = PredictGobs(gBarMs2, a0);
                if (gObsPredicted <= 0) continue;

                double residual = logGobsActual - Math.Log10(gObsPredicted);

                weightedSsr += weight * (residual * residual);
                totalWeight += weight;
                // Debugging-Schritt:
                
            }
            
            
            return totalWeight <= 0 ? double.MaxValue : weightedSsr / totalWeight;
        }

        public static Dictionary<string, double> EstimateUpsilonMap(List<RarPoint> allPoints)
        {
            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (allPoints == null || allPoints.Count == 0)
                return map;

            // Heuristik:
            // - Gasanteil > 30% => Upsilon_disk = 0.3
            // - sonst         => Upsilon_disk = 0.5
            foreach (var galaxyGroup in allPoints.GroupBy(p => NormalizeGalaxyKey(p.GalaxyName)))
            {
                double gasContribution = 0;
                double stellarContribution = 0;

                foreach (var p in galaxyGroup)
                {
                    double vGasSq = p.Vgas > 0 ? p.Vgas * p.Vgas : 0;
                    double vDiskSq = p.Vdisk > 0 ? p.Vdisk * p.Vdisk : 0;
                    double vBulgeSq = p.Vbulge > 0 ? p.Vbulge * p.Vbulge : 0;

                    gasContribution += vGasSq;
                    // Referenz-Sternanteil mit Standardwerten
                    stellarContribution += (0.5 * vDiskSq) + (0.7 * vBulgeSq);
                }

                double baryonicTotal = gasContribution + stellarContribution;
                double gasFraction = baryonicTotal > 0 ? gasContribution / baryonicTotal : 0;

                // Statt hartem Sprung:
                // Skaliere Upsilon zwischen 0.5 (bei 0% Gas) und 0.3 (bei 50% Gas)
                double upsilonDisk = 0.5 - (gasFraction * 0.4);
                // Sicherstellen, dass wir im physikalisch sinnvollen Bereich bleiben
                upsilonDisk = Math.Max(0.3, Math.Min(0.5, upsilonDisk));

                map[galaxyGroup.Key] = upsilonDisk;
            }

            return map;
        }

    }
}
