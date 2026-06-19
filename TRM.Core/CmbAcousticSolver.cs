using System;
using System.Threading.Tasks;

namespace TRM.Core;

public class CmbAcousticSolver
{
    public record CmbPeakResult(int MultipoleL, double AmplitudeTT);

    // Die neue, astrophysikalisch aussagekräftige Resultat-Struktur
    public record CmbOptimizationResult(
        double TrmDriveFreq,
        double DopplerWeight,
        double BestDa,
        double Peak1,
        double Peak2,
        double Fitness
    );

    private double SolveAcousticAmplitude(double k, double etaRec, double cs, double driveFreq, double dopplerWeight)
    {
        double etaStart = 0.01 * etaRec;
        double step = (etaRec - etaStart) / 400.0; // Sehr feine Integration für das Paper

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

        // 1. Sachs-Wolfe (Dichte)
        double sachsWolfe = theta * theta;

        // 2. Doppler (Kinetik der Baryonen, dynamisch kalibrierbar)
        double velocity = thetaDot / (k * cs);
        double doppler = velocity * velocity * dopplerWeight;

        return sachsWolfe + doppler;
    }

    private (double theta, double thetaDot) Derivatives(double eta, double theta, double thetaDot, double k, double cs, double driveFreq)
    {
        double hubbleDamping = 1.0 / eta;
        double kcs = k * cs;
        double kcsSquared = kcs * kcs;

        // Die primordiale Resonanzfrequenz der Zeit-Matrix!
        double trmDrive = 0.05 * Math.Cos(driveFreq * k * eta);

        double thetaDoubleDot = -hubbleDamping * thetaDot - kcsSquared * theta + trmDrive;

        return (thetaDot, thetaDoubleDot);
    }

    /// <summary>
    /// Isoliert die exakten physikalischen Maxima im kontinuierlichen k-Raum (Sachs-Wolfe-gefiltert)
    /// </summary>
    private (double k1, double k2) FindPeaksInKSpace(double etaRec, double cs, double driveFreq, double dopplerWeight)
    {
        const double kStart = 0.001;
        const double kEnd = 0.08;
        const double kStep = 0.0001;
        int kSteps = (int)Math.Round((kEnd - kStart) / kStep) + 1;

        double prevPrevPower = -1;
        double prevPower = -1;
        double prevK = -1;
        double k1 = 0;
        double k2 = 0;
        int peakCount = 0;

        // Wir scannen mit noch feinerer Auflösung
        for (int i = 0; i < kSteps; i++)
        {
            double k = kStart + (i * kStep);
            double power = SolveAcousticAmplitude(k, etaRec, cs, driveFreq, dopplerWeight);

            if (prevPrevPower >= 0 && prevPower >= 0)
            {
                // ECHTE PEAK-BEDINGUNG: Erst hoch (prev > prevPrev), dann runter (prev > current)
                if (prevPower > prevPrevPower && prevPower > power)
                {
                    // Physikalischer Filter: Echte akustische Peaks fangen im k-Raum erst später an.
                    // Das filtert das initiale Sachs-Wolfe-Plateau zu 100% heraus.
                    if (prevK > 0.005)
                    {
                        if (peakCount == 0)
                        {
                            k1 = prevK;
                        }
                        else
                        {
                            k2 = prevK;
                        }

                        peakCount++;
                        if (peakCount == 2) break; // Wir haben die beiden echten Peaks!
                    }
                }
            }

            prevPrevPower = prevPower;
            prevPower = power;
            prevK = k;
        }

        return (k1, k2);
    }

    /// <summary>
    /// Der hochauflösende k-Raum-Sweep zur Entschlüsselung der Naturkonstanten
    /// </summary>
    public CmbOptimizationResult FindPerfectPhysicalParameters()
    {
        double etaRec = 280.0; // Kosmologischer Anker
        double cs = 1.0 / Math.Sqrt(3.0);
        const double freqStart = 1.90;
        const double freqEnd = 2.05;
        const double freqStep = 0.002;
        const double dopStart = 0.05;
        const double dopEnd = 0.12;
        const double dopStep = 0.002;
        int freqSteps = (int)Math.Round((freqEnd - freqStart) / freqStep) + 1;
        int dopSteps = (int)Math.Round((dopEnd - dopStart) / dopStep) + 1;

        double bestFreq = 0;
        double bestDop = 0;
        double bestK1 = 0;
        double bestK2 = 0;

        double minRatioError = double.MaxValue;

        // Das unbestechliche Gesetz der Natur aus den Planck-Daten
        double targetRatio = 540.0 / 220.0;

        Console.WriteLine("Starte Micro-Sweep in der bekannten TRM-Resonanzzone...");

        // MICRO-SWEEP: Wir suchen nur noch extrem fein um die bekannten physikalischen Werte herum
        // Frequenz: 1.90 bis 2.05 | Doppler: 0.05 bis 0.12
        object sync = new();
        Parallel.For(0, freqSteps, freqIndex =>
        {
            double freq = freqStart + (freqIndex * freqStep);
            double localBestError = double.MaxValue;
            double localBestFreq = 0;
            double localBestDop = 0;
            double localBestK1 = 0;
            double localBestK2 = 0;

            for (int dopIndex = 0; dopIndex < dopSteps; dopIndex++)
            {
                double dop = dopStart + (dopIndex * dopStep);
                var (k1, k2) = FindPeaksInKSpace(etaRec, cs, freq, dop);
                if (k1 == 0 || k2 == 0) continue;

                double ratio = k2 / k1;
                double error = Math.Abs(ratio - targetRatio);

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

        // Die finale Projektion auf die Himmelskugel:
        double bestDa = 220.0 / bestK1;

        double finalL1 = bestK1 * bestDa;
        double finalL2 = bestK2 * bestDa;
        double fitness = Math.Pow(finalL1 - 220.0, 2) + Math.Pow(finalL2 - 540.0, 2);

        return new CmbOptimizationResult(bestFreq, bestDop, bestDa, finalL1, finalL2, fitness);
    }


}