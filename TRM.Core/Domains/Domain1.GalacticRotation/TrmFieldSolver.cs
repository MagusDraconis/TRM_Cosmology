using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Core
{
    public static class TrmFieldSolver
    {

        public static ThetaFieldProfile SolveField(List<RarPoint> galaxy)
        {
            return SolveField(
                galaxy,
                sourceStrength: 1.0,
                dampingStrength: 0.45,
                syncStrength: TrmDerivedParameters.GetPhiBeta() * 0.05,
                iterations: 600,
                relaxation: 0.01
            );
        }


        public static ThetaFieldProfile SolveField(
            List<RarPoint> galaxy,
            double sourceStrength,
            double dampingStrength,
            double syncStrength,
            int iterations,
            double relaxation)
        {
            if (galaxy == null || galaxy.Count < 3)
                throw new ArgumentException(
                    "Galaxy profile must contain at least 3 points.",
                    nameof(galaxy));

            var ordered = galaxy
                .Where(p => p.RadiusKpc > 0 && p.GbarMs2 > 0 && double.IsFinite(p.GbarMs2))
                .OrderBy(p => p.RadiusKpc)
                .ToList();

            if (ordered.Count < 3)
                throw new ArgumentException(
                    "Galaxy profile has too few valid baryonic points.",
                    nameof(galaxy));

            var profile = InitializeProfile(ordered);

            RunRelaxation(
                profile,
                sourceStrength,
                dampingStrength,
                syncStrength,
                iterations,
                relaxation);

            return profile;
        }



        public static double ComputeEffectiveAcceleration(
            ThetaFieldProfile field,
            double targetRadiusKpc,
            double gradientCoupling = 1.0,
            double levelCoupling = 1.0,
            int halfWindow = 3)
        {
            if (field == null || field.Points.Count < 5)
                return 0.0;

            int center = FindNearestIndex(field, targetRadiusKpc);
            if (center < 0)
                return 0.0;

            int start = Math.Max(1, center - halfWindow);
            int end = Math.Min(field.Points.Count - 2, center + halfWindow);

            if (end < start)
                return 0.0;

            double gradientSum = 0.0;
            double levelSum = 0.0;
            int count = 0;

            for (int i = start; i <= end; i++)
            {
                var left = field.Points[i - 1];
                var mid = field.Points[i];
                var right = field.Points[i + 1];

                double dr = right.RadiusKpc - left.RadiusKpc;
                if (dr <= 0)
                    continue;

                double rSafe = Math.Max(mid.RadiusKpc, 1e-6);

                // lokaler Gradient
                double dThetaDr = (right.Theta - left.Theta) / dr;
                double gradientTerm = Math.Abs(dThetaDr);

                // lokales Feldniveau radial eingebettet
                double levelTerm = Math.Max(mid.Theta, 0.0) / rSafe;

                gradientSum += gradientTerm;
                levelSum += levelTerm;
                count++;
            }

            if (count == 0)
                return 0.0;

            double meanGradient = gradientSum / count;
            double meanLevel = levelSum / count;

            double gEff =
                gradientCoupling * meanGradient
                + levelCoupling * meanLevel;

            return (double.IsFinite(gEff) && gEff >= 0.0) ? gEff : 0.0;
        }



        public static double ComputeLocalSource(RarPoint p)
        {
            if (p == null || p.GbarMs2 <= 0 || !double.IsFinite(p.GbarMs2))
                return 0.0;

            // Stabiler baryonischer Quellterm
            return Math.Log10(1.0 + p.GbarMs2 / 1e-12);
        }

        public static double ComputeSyncTerm(
            List<RarPoint> orderedGalaxy,
            int index)
        {
            // Platzhalter für spätere echte Synchronisationslogik:
            // - Orbit-Zustand
            // - globaler φ-Term
            // - radialer Regimezustand
            // - später ggf. shear / inertia

            if (orderedGalaxy == null || orderedGalaxy.Count < 3)
                return 0.0;

            if (index <= 0 || index >= orderedGalaxy.Count - 1)
                return 0.0;

            var left = orderedGalaxy[index - 1];
            var mid = orderedGalaxy[index];
            var right = orderedGalaxy[index + 1];

            if (left.GbarMs2 <= 0 || mid.GbarMs2 <= 0 || right.GbarMs2 <= 0)
                return 0.0;

            // Sanfter lokaler Kohärenz-Proxy:
            // wenn benachbarte baryonische Struktur ähnlich ist -> mehr Kohärenz
            double contrastLeft = Math.Abs(Math.Log10(mid.GbarMs2) - Math.Log10(left.GbarMs2));
            double contrastRight = Math.Abs(Math.Log10(right.GbarMs2) - Math.Log10(mid.GbarMs2));

            double contrast = 0.5 * (contrastLeft + contrastRight);

            // Hohe Kontraste -> geringere Synchronisation
            double sync = Math.Exp(-contrast);

            return double.IsFinite(sync) ? sync : 0.0;
        }

        private static ThetaFieldProfile InitializeProfile(List<RarPoint> ordered)
        {
            var profile = new ThetaFieldProfile();

            for (int i = 0; i < ordered.Count; i++)
            {
                var p = ordered[i];

                double source = ComputeLocalSource(p);
                double sync = ComputeSyncTerm(ordered, i);

                profile.Points.Add(new ThetaFieldPoint
                {
                    RadiusKpc = p.RadiusKpc,
                    Source = source,
                    Sync = sync,
                    Theta = source // stabiler Startwert
                });
            }

            return profile;
        }

        private static void RunRelaxation(
            ThetaFieldProfile profile,
            double sourceStrength,
            double dampingStrength,
            double syncStrength,
            int iterations,
            double relaxation)
        {
            if (profile == null || profile.Points.Count < 3)
                return;

            int n = profile.Points.Count;

            for (int iter = 0; iter < iterations; iter++)
            {
                var newTheta = new double[n];

                // Randbedingungen: vorerst festgehalten
                newTheta[0] = profile.Points[0].Theta;
                newTheta[n - 1] = profile.Points[n - 1].Theta;

                for (int i = 1; i < n - 1; i++)
                {
                    var left = profile.Points[i - 1];
                    var mid = profile.Points[i];
                    var right = profile.Points[i + 1];

                    double updated = ComputeRelaxedTheta(
                        left,
                        mid,
                        right,
                        sourceStrength,
                        dampingStrength,
                        syncStrength,
                        relaxation);

                    newTheta[i] = updated;
                }

                for (int i = 1; i < n - 1; i++)
                {
                    if (double.IsFinite(newTheta[i]))
                        profile.Points[i].Theta = newTheta[i];
                }
            }
        }

        private static double ComputeRelaxedTheta(
            ThetaFieldPoint left,
            ThetaFieldPoint mid,
            ThetaFieldPoint right,
            double sourceStrength,
            double dampingStrength,
            double syncStrength,
            double relaxation)
        {
            double drLeft = mid.RadiusKpc - left.RadiusKpc;
            double drRight = right.RadiusKpc - mid.RadiusKpc;

            if (drLeft <= 0 || drRight <= 0)
                return mid.Theta;

            double dr = 0.5 * (drLeft + drRight);
            double drSafe = Math.Max(dr, 1e-2);
            double r = Math.Max(mid.RadiusKpc, 1e-4);

            double laplace =
                (right.Theta - 2.0 * mid.Theta + left.Theta) / (drSafe * drSafe);

            double radialGradient =
                (right.Theta - left.Theta) / Math.Max(drLeft + drRight, 1e-6);

            // ✅ WICHTIG: radialen Einfluss weich skalieren
            double radialStrength = 0.05;

            double radialTerm = radialStrength * (
                laplace + (1.0 / r) * radialGradient
            );

            double rhs =
                sourceStrength * mid.Source
                - dampingStrength * mid.Theta
                + syncStrength * mid.Sync;

            double delta = rhs - radialTerm;

            // Clamp bleibt nur als Notbremse
            delta = Math.Clamp(delta, -0.25, 0.25);

            double thetaTarget = mid.Theta + delta;

            double thetaNew =
                (1.0 - relaxation) * mid.Theta
                + relaxation * thetaTarget;

            thetaNew = Math.Clamp(thetaNew, -20.0, 20.0);

            return double.IsFinite(thetaNew) ? thetaNew : mid.Theta;
        }



        private static int FindNearestIndex(ThetaFieldProfile field, double targetRadiusKpc)
        {
            int bestIndex = -1;
            double bestDistance = double.MaxValue;

            for (int i = 0; i < field.Points.Count; i++)
            {
                double d = Math.Abs(field.Points[i].RadiusKpc - targetRadiusKpc);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}