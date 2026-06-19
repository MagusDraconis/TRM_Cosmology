using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.Core;

public class GalacticRotationAnalysis
{
    // Physical constants in CGS units
    private const double KpcToCm = 3.08567758e21;
    private const double KmsToCmS = 100000.0;
    
    // Milgrom acceleration constant (linked to TRM background drift field H_T)
    private const double A0_Cosmic = 1.2e-8; // cm/s^2

    public readonly record struct RotationPoint(
        double RadiusKpc,
        double VObs,
        double VError,
        double VGas,
        double VDisk,
        double VBulge
    );

    public record GalaxyFitResult(
        string GalaxyName,
        double BestLambda,
        double MinChi2,
        int ValidPoints
    );

    /// <summary>
    /// Loads a standardized SPARC rotation-curve file.
    /// </summary>
    public List<RotationPoint> LoadSparcGalaxy(string filePath)
    {
        var points = new List<RotationPoint>();
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"SPARC file not found: {filePath}");

        foreach (var rawLine in File.ReadLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
                continue;

            // SPARC Format: Rad(1) Vobs(2) errV(3) Vgas(4) Vdisk(5) Vbulge(6)
            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var rad) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var vobs) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var verr) ||
                !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var vgas) ||
                !double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var vdisk) ||
                !double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var vbulge))
            {
                continue;
            }

            // Guard against zero measurement error to avoid division by zero in Chi²
            if (verr <= 0) verr = 1.0;

            points.Add(new RotationPoint(rad, vobs, verr, vgas, vdisk, vbulge));
        }

        return points;
    }

    /// <summary>
    /// Computes the theoretical TRM velocity for a given point.
    /// </summary>
    public double CalculateTheoreticalVelocity(
        RotationPoint point,
        double lambda,
        double upsilonDisk = 0.5,
        double upsilonBulge = 0.7)
    {
        // 1) Convert radial distance to cm
        double rCm = point.RadiusKpc * KpcToCm;

        // 2) Compute total baryonic Newtonian velocity squared using mass-to-light ratios (Upsilon)
        // V_bar^2 = V_gas^2 + Y_disk * V_disk^2 + Y_bulge * V_bulge^2
        double vBarSquaredKm2S2 = Math.Pow(point.VGas, 2)
                                  + upsilonDisk * Math.Pow(point.VDisk, 2)
                                  + upsilonBulge * Math.Pow(point.VBulge, 2);

        if (vBarSquaredKm2S2 <= 0) return 0.0;

        // Konvertierung in CGS (cm^2/s^2)
        double vBarSquaredCm2S2 = vBarSquaredKm2S2 * Math.Pow(KmsToCmS, 2);

        // 3) Compute local classical Newtonian acceleration: g_Newt = V_bar^2 / r
        double gNewt = vBarSquaredCm2S2 / rCm;

        // 4) Apply TRM metric coupling (smooth regime transition)
        // g_eff = g_Newt + sqrt(g_Newt * a0) / lambda
        double gTRM = Math.Sqrt(gNewt * A0_Cosmic) / lambda;
        double gEff = gNewt + gTRM;

        // 5) Convert back to observable orbital velocity (km/s)
        // V_theo = sqrt(r * g_eff)
        double vTheoCmS = Math.Sqrt(rCm * gEff);
        return vTheoCmS / KmsToCmS;
    }

    /// <summary>
    /// Runs a high-precision fit for one galaxy to calibrate lambda.
    /// </summary>
    public GalaxyFitResult FitGalaxy(
        string galaxyName,
        List<RotationPoint> points,
        double upsilonDisk = 0.5,
        double upsilonBulge = 0.7)
    {
        double bestLambda = 1.0;
        double minChi2 = double.MaxValue;

        if (points == null || points.Count == 0)
            return new GalaxyFitResult(galaxyName, 0, double.MaxValue, 0);

        // Dense sweep for lambda (coherence scaling factor) from 0.5 to 3.0
        for (double l = 0.5; l <= 3.0; l += 0.01)
        {
            double currentChi2 = 0.0;

            foreach (var point in points)
            {
                double vTheo = CalculateTheoreticalVelocity(point, l, upsilonDisk, upsilonBulge);

                // Compute weighted residual square (Chi²)
                double residual = Math.Pow(point.VObs - vTheo, 2) / Math.Pow(point.VError, 2);
                currentChi2 += residual;
            }

            // Minimize reduced Chi²
            if (currentChi2 < minChi2)
            {
                minChi2 = currentChi2;
                bestLambda = l;
            }
        }

        // Compute reduced Chi² for reporting (Chi² / degrees of freedom)
        double reducedChi2 = minChi2 / points.Count;

        return new GalaxyFitResult(galaxyName, bestLambda, reducedChi2, points.Count);
    }
}
