using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TRM.Core;

public class PantheonTrmSolver
{
    // Struktur für einen Supernova-Datenpunkt aus dem Pantheon-Katalog
    public record SupernovaPoint(string Name, double Z, double MuObs, double MuErr);

    // Ergebnis des 2D-Sweeps
    public record PantheonFitResult(
        double BestHt,
        double BestBetaTrm,
        double RmsError,
        int AnalyzedPoints
    );

    private const double C_Kms = 299792.458; // Lichtgeschwindigkeit in km/s



    /// <summary>
    /// Berechnet das theoretische Distanzmodul nach den Gesetzen der Clockwork Cosmology
    /// </summary>
    public double CalculateTrmDistanceModulus(double z, double hT, double betaTrm)
    {
        // TRM Leuchtkraftdistanz in Mpc: d_L = (c / H_T) * z * (1+z) * exp(beta * z)
        double dL = (C_Kms / hT) * z * (1.0 + z) * Math.Exp(betaTrm * z);

        // Umrechnung in das Distanzmodul: mu = 5 * log10(d_L) + 25
        return 5.0 * Math.Log10(dL) + 25.0;
    }

    /// <summary>
    /// Hochauflösender 2D-Sweep zur Isolierung des TRM-Drifts (Ersatz für Dunkle Energie)
    /// </summary>
    public PantheonFitResult FindDarkEnergyReplacement(List<SupernovaPoint> data)
    {
        if (data == null || data.Count == 0) return new PantheonFitResult(0, 0, double.MaxValue, 0);

        double bestHt = 0;
        double bestBeta = 0;
        double minChi2 = double.MaxValue;

        Console.WriteLine($"Starte hochpräzisen TRM-Sweep über {data.Count} Pantheon-Supernovae...");

        // H_T (Basis-Taktung) suchen wir im typischen Fenster 65 bis 75 km/s/Mpc
        int htSteps = 1000;
        double htStart = 65.0;
        double htStep = 0.01;

        // beta_TRM (Der "Dark Energy" Drift) suchen wir zwischen -0.5 und 1.0
        int betaSteps = 1500;
        double betaStart = -0.5;
        double betaStep = 0.001;

        object sync = new();

        Parallel.For(0, htSteps, i =>
        {
            double ht = htStart + (i * htStep);
            double localMinChi2 = double.MaxValue;
            double localBestBeta = 0;

            for (int j = 0; j < betaSteps; j++)
            {
                double beta = betaStart + (j * betaStep);
                double chi2 = 0;

                foreach (var sn in data)
                {
                    double muTheo = CalculateTrmDistanceModulus(sn.Z, ht, beta);

                    // Gewichtete kleinste Quadrate (Chi^2)
                    double residual = sn.MuObs - muTheo;
                    chi2 += (residual * residual) / (sn.MuErr * sn.MuErr);
                }

                if (chi2 < localMinChi2)
                {
                    localMinChi2 = chi2;
                    localBestBeta = beta;
                }
            }

            lock (sync)
            {
                if (localMinChi2 < minChi2)
                {
                    minChi2 = localMinChi2;
                    bestHt = ht;
                    bestBeta = localBestBeta;
                }
            }
        });

        // Reduzierter RMS Fehler
        double rmsError = Math.Sqrt(minChi2 / data.Count);
        return new PantheonFitResult(bestHt, bestBeta, rmsError, data.Count);
    }
    /// <summary>
    /// Loads the standardized Pantheon+ SH0ES database dynamically
    /// </summary>
    public List<SupernovaPoint> LoadPantheonData(string filePath)
    {
        var points = new List<SupernovaPoint>();
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Pantheon+ file missing: {filePath}");

        string[] lines = File.ReadAllLines(filePath);
        if (lines.Length < 2) return points;

        // Parse the header dynamically to find the exact column indices
        var header = lines[0].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        int idxCid = header.IndexOf("CID");
        int idxZ = header.IndexOf("zHD");
        int idxMu = header.IndexOf("MU_SH0ES");
        int idxMuErr = header.IndexOf("MU_SH0ES_ERR_DIAG");

        if (idxCid == -1 || idxZ == -1 || idxMu == -1 || idxMuErr == -1)
        {
            throw new FormatException("The provided file does not contain the required Pantheon+ columns (CID, zHD, MU_SH0ES, MU_SH0ES_ERR_DIAG).");
        }

        Console.WriteLine("Pantheon+ header verified. Parsing supernova catalog...");

        // Loop through the data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure the row has enough columns to prevent index out of bounds
            if (parts.Length <= Math.Max(Math.Max(idxCid, idxZ), Math.Max(idxMu, idxMuErr))) continue;

            try
            {
                string name = parts[idxCid];
                double z = double.Parse(parts[idxZ], CultureInfo.InvariantCulture);
                double mu = double.Parse(parts[idxMu], CultureInfo.InvariantCulture);
                double err = double.Parse(parts[idxMuErr], CultureInfo.InvariantCulture);

                // Cosmological Filter: We strictly consider the Hubble flow regime (z > 0.01)
                // This ensures we are measuring the universe, not just local galaxy drift.
                if (z > 0.01)
                {
                    points.Add(new SupernovaPoint(name, z, mu, err));
                }
            }
            catch (FormatException) { continue; }
        }

        return points;
    }
}
