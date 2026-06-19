using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.Core
{
    // 1. ERWEITERTES DATENMODELL
    public record GalaxyData(
        string Name,
        double L36,      // Leuchtkraft 10^9 L_sun
        double MHI,      // HI-Masse 10^9 M_sun
        double Vflat,    // km/s
        double EVflat,   // Fehler km/s
        double Inc,      // Inklination (Neigungswinkel) in Grad -> NEU
        int Q            // Qualitätsflag (1=gut, 2=akzeptabel, 3=schlecht) -> NEU
    )
    {
        public double Mbar => (0.5 * L36) + (1.33 * MHI);
        public double MbarAbsolute => Mbar * 1e9;
    }

    public class SparcMrtParser
    {
        public static List<GalaxyData> ParseFile(string filePath)
        {
            var galaxies = new List<GalaxyData>();
            var lines = File.ReadLines(filePath);
            bool headerEnded = false;
            int hyphenCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (trimmed.StartsWith("------"))
                {
                    hyphenCount++;
                    if (hyphenCount == 3) headerEnded = true;
                    continue;
                }

                if (headerEnded)
                {
                    if (trimmed.StartsWith("#")) continue;

                    var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    // Mindestens 18 Elemente, damit Index 17 (Q) sicher existiert
                    if (parts.Length >= 18)
                    {
                        try
                        {
                            string name = parts[0];
                            double inc = double.Parse(parts[5], CultureInfo.InvariantCulture);     // Spalte 6 (deg)
                            double l36 = double.Parse(parts[7], CultureInfo.InvariantCulture);     // Spalte 8 (10^9 L_sun)
                            double mHi = double.Parse(parts[13], CultureInfo.InvariantCulture);    // Spalte 14 (10^9 M_sun)
                            double vFlat = double.Parse(parts[15], CultureInfo.InvariantCulture);  // Spalte 16 (km/s)
                            double eVflat = double.Parse(parts[16], CultureInfo.InvariantCulture); // Spalte 17 (km/s)
                            int q = int.Parse(parts[17], CultureInfo.InvariantCulture);            // Spalte 18 (Flag)

                            if (vFlat > 0)
                            {
                                // Konstruktor mit den neuen Parametern aufrufen
                                galaxies.Add(new GalaxyData(name, l36, mHi, vFlat, eVflat, inc, q));
                            }
                        }
                        catch (FormatException)
                        {
                            // Überspringt unvollständige Zeilen am Tabellenende
                            continue;
                        }
                    }
                }
            }
            return galaxies;
        }

        public static (double DirectSlope, double InverseSlopePhysical, double RmaSlope, double Intercept) FitBtrf(List<GalaxyData> data)
        {
            // LINQ greift jetzt fehlerfrei auf .Inc und .Q zu
            var validPoints = data.Where(g => g.MbarAbsolute > 0
                                           && g.Vflat > 0
                                           && g.Inc >= 30.0
                                           && g.Q < 3)
                                  .Select(g => new
                                  {
                                      X = Math.Log10(g.Vflat),
                                      Y = Math.Log10(g.MbarAbsolute)
                                  }).ToList();

            int n = validPoints.Count;
            if (n == 0) return (0, 0, 0, 0);

            double sumX = validPoints.Sum(p => p.X);
            double sumY = validPoints.Sum(p => p.Y);
            double sumX2 = validPoints.Sum(p => p.X * p.X);
            double sumY2 = validPoints.Sum(p => p.Y * p.Y);
            double sumXY = validPoints.Sum(p => p.X * p.Y);

            double directSlope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - directSlope * sumX) / n;

            double inverseSlopeStandard = (n * sumXY - sumX * sumY) / (n * sumY2 - sumY * sumY);
            double inverseSlopePhysical = 1.0 / inverseSlopeStandard;

            // Symmetrischer RMA-Schätzer (Reduced Major Axis)
            double rmaSlope = Math.Sqrt(directSlope * inverseSlopePhysical);

            return (directSlope, inverseSlopePhysical, rmaSlope, intercept);
        }
    }
}