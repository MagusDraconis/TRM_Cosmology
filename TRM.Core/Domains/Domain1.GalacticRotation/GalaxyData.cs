using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.Core
{
    // Extended data model
    public record GalaxyData(
        string Name,
        double L36,      // Leuchtkraft 10^9 L_sun
        double MHI,      // HI-Masse 10^9 M_sun
        double Vflat,    // km/s
        double EVflat,   // Error km/s
        double Inc,      // Inclination angle in degrees
        int Q            // Quality flag (1=good, 2=acceptable, 3=poor)
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

                    // Require at least 18 elements so index 17 (Q) is safe
                    if (parts.Length >= 18)
                    {
                        string name = parts[0];
                        bool okInc = double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double inc);      // Column 6 (deg)
                        bool okL36 = double.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out double l36);      // Column 8 (10^9 L_sun)
                        bool okMhi = double.TryParse(parts[13], NumberStyles.Float, CultureInfo.InvariantCulture, out double mHi);     // Column 14 (10^9 M_sun)
                        bool okVf = double.TryParse(parts[15], NumberStyles.Float, CultureInfo.InvariantCulture, out double vFlat);    // Column 16 (km/s)
                        bool okEvf = double.TryParse(parts[16], NumberStyles.Float, CultureInfo.InvariantCulture, out double eVflat);  // Column 17 (km/s)
                        bool okQ = int.TryParse(parts[17], NumberStyles.Integer, CultureInfo.InvariantCulture, out int q);             // Column 18 (flag)

                        if (okInc && okL36 && okMhi && okVf && okEvf && okQ && vFlat > 0)
                        {
                            galaxies.Add(new GalaxyData(name, l36, mHi, vFlat, eVflat, inc, q));
                        }
                    }
                }
            }
            return galaxies;
        }

        public static (double DirectSlope, double InverseSlopePhysical, double RmaSlope, double Intercept) FitBtrf(List<GalaxyData> data)
        {
            // Filter points by physical and quality constraints
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

            // Symmetric RMA estimator (Reduced Major Axis)
            double rmaSlope = Math.Sqrt(directSlope * inverseSlopePhysical);

            return (directSlope, inverseSlopePhysical, rmaSlope, intercept);
        }
    }
}