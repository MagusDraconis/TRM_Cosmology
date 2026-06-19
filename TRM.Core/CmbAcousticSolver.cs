using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Core;

public class CmbAcousticSolver
{
    // Struktur für das resultierende Winkelleistungsspektrum
    public record CmbPeakResult(int MultipoleL, double AmplitudeTT);

    /// <summary>
    /// Integriert die Plasma-Oszillationen für eine Wellenzahl k mittels RK4
    /// </summary>
    private double SolveAcousticAmplitude(double k, double etaRec, double cs)
    {
        // Startbedingungen zur frühen kosmischen Zeit (Konforme Zeit eta)
        double etaStart = 0.01 * etaRec;
        double step = (etaRec - etaStart) / 500.0; // 500 Integrationsschritte

        // Zustandsvariablen: [0] = Amplitude Theta, [1] = Erste Ableitung (Dotiert)
        double[] state = new double[] { 1.0, 0.0 };

        double eta = etaStart;
        while (eta < etaRec)
        {
            // RK4 Integrationsschritt
            double[] k1 = Derivatives(eta, state, k, cs);

            double[] nextState = state.Zip(k1, (s, ex) => s + ex * step * 0.5).ToArray();
            double[] k2 = Derivatives(eta + step * 0.5, nextState, k, cs);

            nextState = state.Zip(k2, (s, ex) => s + ex * step * 0.5).ToArray();
            double[] k3 = Derivatives(eta + step * 0.5, nextState, k, cs);

            nextState = state.Zip(k3, (s, ex) => s + ex * step).ToArray();
            double[] k4 = Derivatives(eta + step, nextState, k, cs);

            for (int i = 0; i < 2; i++)
            {
                state[i] += (step / 6.0) * (k1[i] + 2.0 * k2[i] + 2.0 * k3[i] + k4[i]);
            }

            eta += step;
        }

        // Die beobachtbare Temperatur-Anisotropie ist proportional zum Quadrat der Amplitude
        return Math.Pow(state[0], 2);
    }

    /// <summary>
    /// Definiert die Differentialgleichungen des TRM-angetriebenen Oszillators
    /// </summary>
    private double[] Derivatives(double eta, double[] state, double k, double cs)
    {
        double theta = state[0];
        double thetaDot = state[1];

        // Kosmische Expansion dämpft die Schwingung (Hubble-Dämpfung im TRM-Raum)
        double hubbleDamping = 1.0 / eta;

        // TRM Hintergrund-Antriebskraft (Simuliert die intrinsischen Matrixfluktuationen)
        double trmDrive = 0.05 * Math.Cos(0.5 * k * eta);

        // Schwingungsgleichung: d2Theta/deta2 = - Hubble*dTheta/deta - k^2 * cs^2 * Theta + Drive
        double thetaDoubleDot = -hubbleDamping * thetaDot - Math.Pow(k * cs, 2) * theta + trmDrive;

        return new double[] { thetaDot, thetaDoubleDot };
    }

    /// <summary>
    /// Berechnet das vollständige CMB TT-Leistungsspektrum für das Paper
    /// </summary>
    public List<CmbPeakResult> ComputeCmbSpectrum(int maxL = 1500)
    {
        var spectrum = new List<CmbPeakResult>();

        // Physikalische Parameter der Rekombinationsepoche
        double etaRec = 280.0; // Konforme Zeit bei z=1100 (in Mpc)
        double cs = 1.0 / Math.Sqrt(3.0); // Schallgeschwindigkeit im relativistischen Plasma (c / sqrt(3))
        double angularDiameterDistance = 14000.0; // Distanz zur CMB-Oberfläche heute

        Console.WriteLine("Berechne akustische TRM-Peaks für den CMB...");

        // Wir scannen die Multipole l (entspricht den Winkelskalen am Himmel)
        for (int l = 50; l <= maxL; l += 10)
        {
            // Geometrische Projektion: l = k * D_A => Effektive Wellenzahl k bestimmen
            double k = l / angularDiameterDistance;

            double power = SolveAcousticAmplitude(k, etaRec, cs);

            // Sachs-Wolfe-Sackgasse verhindern und Spektrum normieren für die typische CMB-Kurve
            double normFactor = l * (l + 1);
            double finalAmplitude = power * normFactor * 2.5;

            spectrum.Add(new CmbPeakResult(l, finalAmplitude));
        }

        return spectrum;
    }
}