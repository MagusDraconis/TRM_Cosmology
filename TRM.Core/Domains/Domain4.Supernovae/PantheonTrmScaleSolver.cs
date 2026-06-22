using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TRM.Core.Shared;

namespace TRM.Core.Domains.Domain4.Supernovae;


public class PantheonTrmScaleSolver
{
    private readonly TrmDistanceMapper _mapper;

    public PantheonTrmScaleSolver(TrmDistanceMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public PantheonScaleFitResult Evaluate(
        List<SupernovaPoint> data)
    {
        if (data == null || data.Count == 0)
        {
            return new PantheonScaleFitResult(
                0,
                double.MaxValue,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN);
        }

        var residuals = new List<double>();

        double sumSquared = 0.0;
        double sumResidual = 0.0;
        double sumAbsResidual = 0.0;
        double maxAbsResidual = 0.0;

        int count = 0;

        foreach (var sn in data)
        {
            if (sn.Z <= 0.0)
                continue;

            double muTheo = _mapper.CalculateTrmDistanceModulus(sn.Z);

            double residual = sn.MuObs - muTheo;
            double absResidual = Math.Abs(residual);

            residuals.Add(residual);

            sumSquared += residual * residual;
            sumResidual += residual;
            sumAbsResidual += absResidual;

            if (absResidual > maxAbsResidual)
                maxAbsResidual = absResidual;

            count++;
        }

        if (count == 0)
        {
            return new PantheonScaleFitResult(
                0,
                double.MaxValue,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN);
        }

        double rms = Math.Sqrt(sumSquared / count);
        double meanResidual = sumResidual / count;
        double meanAbsResidual = sumAbsResidual / count;

        double centeredSumSquared = residuals
            .Select(r => r - meanResidual)
            .Sum(dr => dr * dr);

        double centeredRms = Math.Sqrt(centeredSumSquared / count);

        return new PantheonScaleFitResult(
            count,
            rms,
            meanResidual,
            meanAbsResidual,
            maxAbsResidual,
            centeredRms);
    }

    ///// <summary>
    ///// Hochauflösender 2D-Sweep zur Isolierung des TRM-Drifts (Ersatz für Dunkle Energie)
    ///// </summary>
    //public PantheonScaleFitResult FindDarkEnergyReplacement(List<SupernovaPoint> data)
    //{
    //    if (data == null || data.Count == 0) 
    //        return new PantheonScaleFitResult(
    //            0,
    //            double.MaxValue,
    //            double.NaN,
    //            double.NaN,
    //            double.NaN,
    //            double.NaN);

    //    double bestHt = 0;
    //    double bestBeta = 0;
    //    double minChi2 = double.MaxValue;

    //    Console.WriteLine($"Starte hochpräzisen TRM-Sweep über {data.Count} Pantheon-Supernovae...");

    //    // H_T (Basis-Taktung) suchen wir im typischen Fenster 65 bis 75 km/s/Mpc
    //    int htSteps = 1000;
    //    double htStart = 65.0;
    //    double htStep = 0.01;

    //    // beta_TRM (Der "Dark Energy" Drift) suchen wir zwischen -0.5 und 1.0
    //    int betaSteps = 1500;
    //    double betaStart = -0.5;
    //    double betaStep = 0.001;

    //    object sync = new();

    //    Parallel.For(0, htSteps, i =>
    //    {
    //        double ht = htStart + (i * htStep);
    //        double localMinChi2 = double.MaxValue;
    //        double localBestBeta = 0;

    //        for (int j = 0; j < betaSteps; j++)
    //        {
    //            double beta = betaStart + (j * betaStep);
    //            double chi2 = 0;

    //            foreach (var sn in data)
    //            {
    //                double muTheo = _mapper.CalculateTrmDistanceModulus(sn.Z);

    //                // Gewichtete kleinste Quadrate (Chi^2)
    //                double residual = sn.MuObs - muTheo;
    //                chi2 += (residual * residual) / (sn.MuErr * sn.MuErr);
    //            }

    //            if (chi2 < localMinChi2)
    //            {
    //                localMinChi2 = chi2;
    //                localBestBeta = beta;
    //            }
    //        }

    //        lock (sync)
    //        {
    //            if (localMinChi2 < minChi2)
    //            {
    //                minChi2 = localMinChi2;
    //                bestHt = ht;
    //                bestBeta = localBestBeta;
    //            }
    //        }
    //    });

    //    // Reduzierter RMS Fehler
    //    double rmsError = Math.Sqrt(minChi2 / data.Count);


    //    return new PantheonScaleFitResult(bestHt, bestBeta, rmsError, data.Count);
    //}
}