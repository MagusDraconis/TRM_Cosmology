using System;
using System.Threading.Tasks;

namespace TRM.Core;

public class CmbAcousticSolver
{
    public record CmbPeakResult(int MultipoleL, double AmplitudeTT);

    // Result structure for CMB optimization outputs
    public record CmbOptimizationResult(
        double TrmDriveFreq,
        double DopplerWeight,
        double K1,   // <<< NEU
        double K2,   // <<< NEU
        double PeakRatio,
        double Fitness
    );

    private double SolveAcousticAmplitude(double k, double etaRec, double cs, double driveFreq, double dopplerWeight)
    {
        double etaStart = 0.01 * etaRec;
        double step = (etaRec - etaStart) / 400.0; // High-resolution integration

        double theta = 1.0;
        double thetaDot = 0.0;

        double eta = etaStart;
        while (eta < etaRec)
        {
            var (k1Theta, k1ThetaDot) = Derivatives(eta, theta, thetaDot, k, cs, driveFreq);
            var (k2Theta, k2ThetaDot) = Derivatives(
                eta + step * 0.5,
                theta + k1Theta * step * 0.5,
                thetaDot + k1ThetaDot * step * 0.5,
                k,
                cs,
                driveFreq);
            var (k3Theta, k3ThetaDot) = Derivatives(
                eta + step * 0.5,
                theta + k2Theta * step * 0.5,
                thetaDot + k2ThetaDot * step * 0.5,
                k,
                cs,
                driveFreq);
            var (k4Theta, k4ThetaDot) = Derivatives(
                eta + step,
                theta + k3Theta * step,
                thetaDot + k3ThetaDot * step,
                k,
                cs,
                driveFreq);

            theta += (step / 6.0) * (k1Theta + 2.0 * k2Theta + 2.0 * k3Theta + k4Theta);
            thetaDot += (step / 6.0) * (k1ThetaDot + 2.0 * k2ThetaDot + 2.0 * k3ThetaDot + k4ThetaDot);
            eta += step;
        }

        // 1) Sachs-Wolfe contribution (density)
        double sachsWolfe = theta * theta;

        // 2) Doppler contribution (baryon velocity, tunable weight)
        double velocity = thetaDot / (k * cs);
        double doppler = velocity * velocity * dopplerWeight;

        return sachsWolfe + doppler;
    }

    private (double theta, double thetaDot) Derivatives(double eta, double theta, double thetaDot, double k, double cs, double driveFreq)
    {
        double hubbleDamping = 1.0 / eta;
        double kcs = k * cs;
        double kcsSquared = kcs * kcs;

        // Primordial TRM drive component
        double trmDrive = 0.05 * Math.Cos(driveFreq * k * eta);

        double thetaDoubleDot = -hubbleDamping * thetaDot - kcsSquared * theta + trmDrive;

        return (thetaDot, thetaDoubleDot);
    }

    /// <summary>
    /// Finds the first two physical maxima in continuous k-space (Sachs-Wolfe filtered)
    /// </summary>
    /// <summary>
    /// Finds the first two physical maxima in continuous normalized k-space.
    /// The scan variable q is dimensionless/raw, while kEff = q / etaRec is
    /// the effective acoustic k used in the oscillator.
    /// </summary>
    private (double k1, double k2) FindPeaksInKSpace(
        double etaRec,
        double cs,
        double driveFreq,
        double dopplerWeight)
    {
        const double qStart = 0.5;
        const double qEnd = 50.0;
        const double qStep = 0.01;

        int qSteps = (int)Math.Round((qEnd - qStart) / qStep) + 1;

        double prevPrevPower = double.NaN;
        double prevPower = double.NaN;

        double prevQ = double.NaN;
        double prevKEff = double.NaN;

        double k1 = 0.0;
        double k2 = 0.0;
        int peakCount = 0;

        for (int i = 0; i < qSteps; i++)
        {
            double q = qStart + (i * qStep);

            // Effective acoustic k used by the solver
            double kEff = q / etaRec;

            double power = SolveAcousticAmplitude(
                kEff,
                etaRec,
                cs,
                driveFreq,
                dopplerWeight);

            if (double.IsNaN(power) || double.IsInfinity(power))
            {
                prevPrevPower = prevPower;
                prevPower = double.NaN;
                prevQ = q;
                prevKEff = kEff;
                continue;
            }

            if (!double.IsNaN(prevPrevPower) && !double.IsNaN(prevPower))
            {
                bool isPeak = prevPower > prevPrevPower && prevPower > power;

                if (isPeak)
                {
                    // Sachs-Wolfe plateau filter must apply to effective k,
                    // not raw q.
                    if (prevKEff > 0.005)
                    {
                        if (peakCount == 0)
                        {
                            k1 = prevKEff;
                        }
                        else
                        {
                            k2 = prevKEff;
                        }

                        peakCount++;

                        if (peakCount == 2)
                            break;
                    }
                }
            }

            prevPrevPower = prevPower;
            prevPower = power;
            prevQ = q;
            prevKEff = kEff;
        }

        return (k1, k2);
    }

    /// <summary>
    /// High-resolution k-space sweep used to fit physical parameters
    /// </summary>
    public CmbOptimizationResult FindPerfectPhysicalParameters()
    {
        double etaRec = 280.0;
        double cs = 1.0 / Math.Sqrt(3.0);

        const double freqStart = 1.90;
        const double freqEnd = 2.05;
        const double freqStep = 0.002;

        const double dopStart = 0.05;
        const double dopEnd = 0.12;
        const double dopStep = 0.002;

        int freqSteps = (int)Math.Round((freqEnd - freqStart) / freqStep) + 1;
        int dopSteps = (int)Math.Round((dopEnd - dopStart) / dopStep) + 1;

        double bestFreq = 0.0;
        double bestDop = 0.0;
        double bestK1 = 0.0;
        double bestK2 = 0.0;

        double minRatioError = double.MaxValue;

        double targetRatio = 540.0 / 220.0;

        object sync = new();

        Parallel.For(0, freqSteps, freqIndex =>
        {
            double freq = freqStart + (freqIndex * freqStep);

            double localBestError = double.MaxValue;
            double localBestFreq = 0.0;
            double localBestDop = 0.0;
            double localBestK1 = 0.0;
            double localBestK2 = 0.0;

            for (int dopIndex = 0; dopIndex < dopSteps; dopIndex++)
            {
                double dop = dopStart + (dopIndex * dopStep);

                var (k1, k2) = FindPeaksInKSpace(etaRec, cs, freq, dop);

                if (k1 <= 0 || k2 <= 0 ||
                    double.IsNaN(k1) || double.IsNaN(k2) ||
                    double.IsInfinity(k1) || double.IsInfinity(k2))
                {
                    continue;
                }

                double ratio = k2 / k1;

                if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                {
                    continue;
                }

                double error = Math.Abs(ratio - targetRatio);

                if (double.IsNaN(error) || double.IsInfinity(error))
                {
                    continue;
                }

                if (error < localBestError)
                {
                    localBestError = error;
                    localBestFreq = freq;
                    localBestDop = dop;
                    localBestK1 = k1;
                    localBestK2 = k2;
                }
            }

            if (localBestError < double.MaxValue)
            {
                lock (sync)
                {
                    if (localBestError < minRatioError)
                    {
                        minRatioError = localBestError;
                        bestFreq = localBestFreq;
                        bestDop = localBestDop;
                        bestK1 = localBestK1;
                        bestK2 = localBestK2;
                    }
                }
            }
        });

        // ✅ HIER ist der richtige Ort für den Guard.
        // Nach Parallel.For, nicht darin.
        if (bestK1 <= 0 || bestK2 <= 0 ||
            double.IsNaN(bestK1) || double.IsNaN(bestK2) ||
            double.IsInfinity(bestK1) || double.IsInfinity(bestK2))
        {
            return new CmbOptimizationResult(
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                double.MaxValue
            );
        }

        double finalRatio = bestK2 / bestK1;

        if (double.IsNaN(finalRatio) || double.IsInfinity(finalRatio))
        {
            return new CmbOptimizationResult(
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                double.MaxValue
            );
        }

        double fitness = Math.Abs(finalRatio - targetRatio);

        return new CmbOptimizationResult(
            bestFreq,
            bestDop,
            bestK1,
            bestK2,
            finalRatio,
            fitness
        );
    }

    public record CmbScalePrediction(
        double EtaRec,
        double ZRec,
        double AngularDiameterDistance,
        double LPred
);
    public CmbScalePrediction CalculateCmbScalePrediction(
    double k1,
    double cs,
    double driveFreq,
    TrmCosmologyParameters scaling)
    {
        double etaRec = FindRecombinationEta(k1, cs, driveFreq);

        double zRec = CalculateTrmRecombinationRedshift(
            etaRec,
            scaling.BetaEta,
            scaling.Alpha);

        double dA = (PhysicalConstants.C_Kms / scaling.HT)
                    * Math.Log(1.0 + zRec);

        double lPred = k1 * dA;

        return new CmbScalePrediction(
            etaRec,
            zRec,
            dA,
            lPred);
    }



    public double CalculateTrmRecombinationRedshift(
        double etaRec,
        double betaTrm,
        double alpha)
    {
        return Math.Exp(betaTrm * alpha * etaRec) - 1.0;
    }

    public double FindRecombinationEta(double k, double cs, double driveFreq)
    {
        double etaStart = 0.01;
        double etaEnd = 400.0;
        double step = (etaEnd - etaStart) / 400.0;

        double theta = 1.0;
        double thetaDot = 0.0;
        double eta = etaStart;

        double bestEta = eta;
        double maxRatio = 0.0;

        while (eta < etaEnd)
        {
            var (k1Theta, k1ThetaDot) = Derivatives(eta, theta, thetaDot, k, cs, driveFreq);

            theta += k1Theta * step;
            thetaDot += k1ThetaDot * step;
            eta += step;

            double ratio = Math.Abs(thetaDot) / (Math.Abs(theta) + 1e-6);

            if (ratio > maxRatio)
            {
                maxRatio = ratio;
                bestEta = eta;
            }
        }

        return bestEta;
    }
}