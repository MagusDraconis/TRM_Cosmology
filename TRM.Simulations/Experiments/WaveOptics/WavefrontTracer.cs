using System;
using System.Collections.Generic;
using System.Text;
using TRM.QuantumCore.Planck;

namespace TRM.Simulations.Experiments.WaveOptics;

public class WavefrontTracer
{
    private readonly double G = PhysicalConstantsSI.G;
    private readonly double c = PhysicalConstantsSI.c;

    public double Simulate(double M, double impactParameter)
    {
        // Startposition: weit links
        double x = -1e10;
        double y = impactParameter;

        // Anfangsrichtung: nach rechts
        Vec2 dir = new Vec2(1.0, 0.0);

        // räumliche Schrittweite (nicht Zeit!)
        double ds = 1e7; // 10,000 km pro Schritt

        for (int i = 0; i < 500000; i++)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < 1e-6)
                break;

            // T-Feld
            double T = 1.0 - (G * M) / (c * c * r);

            // Schutz gegen unphysikalische Werte
            if (T <= 0.0)
                break;

            // Brechungsindex
            double n = 1.0 / T;

            // dT/dr
            double dTdr = (G * M) / (c * c * r * r);

            // dn/dr = -(1/T^2) * dT/dr
            double dndr = -(1.0 / (T * T)) * dTdr;

            // Gradientenkomponenten von n
            double gradNx = dndr * (x / r);
            double gradNy = dndr * (y / r);

            // Projektion des Gradienten auf die aktuelle Richtung
            double proj = gradNx * dir.X + gradNy * dir.Y;

            // Nur die transversale Komponente ändert die Richtung
            double perpX = gradNx - proj * dir.X;
            double perpY = gradNy - proj * dir.Y;

            // Ray equation: du/ds = (∇n - (u·∇n)u)/n
            Vec2 newDir = new Vec2(
                dir.X + (perpX / n) * ds,
                dir.Y + (perpY / n) * ds
            ).Normalize();

            dir = newDir;

            // räumlicher Schritt entlang des Strahls
            x += dir.X * ds;
            y += dir.Y * ds;

            if (x > 1e10)
                break;
        }

        return Math.Atan2(dir.Y, dir.X);
    }
    public double SimulateSpatialCurvatureLikeGR(double M, double impactParameter, double ds)
    {
        // Start weit links
        double x = -1e10;
        double y = impactParameter;

        // Anfangsrichtung: nach rechts
        Vec2 dir = new Vec2(1.0, 0.0);

        for (int i = 0; i < 50_000_000; i++)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < 1e-6)
                break;

            // Newtonsches Potential:
            // Phi = -GM/r
            // d|Phi|/dr = GM/r^2
            double dPhidr = (G * M) / (r * r);

            // Radialrichtung
            double rx = x / r;
            double ry = y / r;

            // Gradient des Potentialbetrags
            double gradPhix = dPhidr * rx;
            double gradPhiy = dPhidr * ry;

            // Nur transversale Komponente zur aktuellen Richtung
            double proj = gradPhix * dir.X + gradPhiy * dir.Y;

            double perpX = gradPhix - proj * dir.X;
            double perpY = gradPhiy - proj * dir.Y;

            // Schwache-Feld-GR-artiges Richtungsupdate
            double factor = (2.0 / (c * c)) * ds;

            Vec2 newDir = new Vec2(
                dir.X - perpX * factor,
                dir.Y - perpY * factor
            ).Normalize();

            dir = newDir;

            // Räumlicher Schritt entlang des Strahls
            x += dir.X * ds;
            y += dir.Y * ds;

            if (x > 1e10)
                break;
        }

        return Math.Atan2(dir.Y, dir.X);
    }

    public double SimulateWaveOnly(double M, double impactParameter)
    {
        // Start weit links
        double x = -1e10;
        double y = impactParameter;

        // Start-Richtung: nach rechts
        Vec2 dir = new Vec2(1.0, 0.0);

        // Räumliche Schrittweite
        double ds = 1e6; // 1000 km

        // Referenzfrequenz (nur relative Dynamik wichtig)
        double omega0 = 1.0;

        for (int i = 0; i < 20_000_000; i++)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < 1e-6)
                break;

            // Zwei Punkte senkrecht zur aktuellen Bewegungsrichtung
            // repräsentieren obere / untere Wellenkante
            Vec2 normal = new Vec2(-dir.Y, dir.X);

            double halfWidth = 1.0; // 1 m Abstand von Mittelpunkt zu Wellenkante

            Vec2 top = new Vec2(
                x + normal.X * halfWidth,
                y + normal.Y * halfWidth
            );

            Vec2 bottom = new Vec2(
                x - normal.X * halfWidth,
                y - normal.Y * halfWidth
            );

            double rTop = Math.Sqrt(top.X * top.X + top.Y * top.Y);
            double rBottom = Math.Sqrt(bottom.X * bottom.X + bottom.Y * bottom.Y);

            // Lokale Zeitfaktoren
            double Ttop = 1.0 - (G * M) / (c * c * rTop);
            double Tbottom = 1.0 - (G * M) / (c * c * rBottom);

            // Lokale Frequenzen
            double omegaTop = omega0 * Ttop;
            double omegaBottom = omega0 * Tbottom;

            // Nur die lokale Frequenzdifferenz
            double dOmega = omegaTop - omegaBottom;

            // Wegstück -> Zeitstück
            double dt = ds / c;

            // Kleine rotationsartige Richtungsänderung
            // bewusst sehr klein, damit wir nur den isolierten Welleneffekt messen
            double rotation = dOmega * dt;

            double cos = Math.Cos(rotation);
            double sin = Math.Sin(rotation);

            Vec2 newDir = new Vec2(
                dir.X * cos - dir.Y * sin,
                dir.X * sin + dir.Y * cos
            ).Normalize();

            dir = newDir;

            // Schritt entlang der aktuellen Richtung
            x += dir.X * ds;
            y += dir.Y * ds;

            if (x > 1e10)
                break;
        }

        return Math.Atan2(dir.Y, dir.X);
    }

    public double SimulateSpatialCurvatureLikeGR(
    double M,
    double impactParameter,
    double ds,
    double xStart,
    double xStop)
    {
        double x = xStart;
        double y = impactParameter;

        Vec2 dir = new Vec2(1.0, 0.0);

        for (int i = 0; i < 100_000_000; i++)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < 1e-6)
                break;

            // Newtonsches Potential: Phi = -GM/r
            // Betrag des radialen Gradienten: GM/r^2
            double dPhidr = (G * M) / (r * r);

            double rx = x / r;
            double ry = y / r;

            double gradPhix = dPhidr * rx;
            double gradPhiy = dPhidr * ry;

            // nur transversale Komponente zur aktuellen Richtung
            double proj = gradPhix * dir.X + gradPhiy * dir.Y;

            double perpX = gradPhix - proj * dir.X;
            double perpY = gradPhiy - proj * dir.Y;

            // schwache-Feld-GR-artiges Richtungsupdate
            double factor = (2.0 / (c * c)) * ds;

            Vec2 newDir = new Vec2(
                dir.X - perpX * factor,
                dir.Y - perpY * factor
            ).Normalize();

            dir = newDir;

            x += dir.X * ds;
            y += dir.Y * ds;

            if (x > xStop)
                break;
        }

        return Math.Atan2(dir.Y, dir.X);
    }
    public double SimulateTRMBaseline(
    double M,
    double impactParameter,
    double ds,
    double xStart,
    double xStop)
    {
        double x = xStart;
        double y = impactParameter;

        Vec2 dir = new Vec2(1.0, 0.0);

        for (int i = 0; i < 100_000_000; i++)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < 1e-6)
                break;

            // T-Feld:
            // T = 1 - GM/(c^2 r)
            // dT/dr = + GM/(c^2 r^2)
            double dTdr = (G * M) / (c * c * r * r);

            double rx = x / r;
            double ry = y / r;

            double gradTx = dTdr * rx;
            double gradTy = dTdr * ry;

            // nur transversale Komponente zur aktuellen Richtung
            double proj = gradTx * dir.X + gradTy * dir.Y;

            double perpX = gradTx - proj * dir.X;
            double perpY = gradTy - proj * dir.Y;

            // TRM-Baseline:
            // Richtungsänderung aus c^2 ∇T
            double factor = (c * c) * ds / c;
            // = c * ds, formal aus bisheriger Baseline-Idee entlang des Wegs

            Vec2 newDir = new Vec2(
                dir.X - perpX * factor,
                dir.Y - perpY * factor
            ).Normalize();

            dir = newDir;

            x += dir.X * ds;
            y += dir.Y * ds;

            if (x > xStop)
                break;
        }

        return Math.Atan2(dir.Y, dir.X);
    }
    public double SimulateTRMBaselineWithRange(
    double M,
    double impactParameter,
    double ds,
    double xStart,
    double xStop)
    {
        double x = xStart;
        double y = impactParameter;

        Vec2 dir = new Vec2(1.0, 0.0);

        for (int i = 0; i < 100_000_000; i++)
        {
            double r = Math.Sqrt(x * x + y * y);
            if (r < 1e-6)
                break;

            // Potentialgradient
            double dPhidr = (G * M) / (r * r);

            double rx = x / r;
            double ry = y / r;

            double gradPhix = dPhidr * rx;
            double gradPhiy = dPhidr * ry;

            // transversale Komponente
            double proj = gradPhix * dir.X + gradPhiy * dir.Y;

            double perpX = gradPhix - proj * dir.X;
            double perpY = gradPhiy - proj * dir.Y;

            // TRM-/Newton-Baseline: Faktor 1 statt 2
            double factor = (1.0 / (c * c)) * ds;

            Vec2 newDir = new Vec2(
                dir.X - perpX * factor,
                dir.Y - perpY * factor
            ).Normalize();

            dir = newDir;

            x += dir.X * ds;
            y += dir.Y * ds;

            if (x > xStop)
                break;
        }

        return Math.Atan2(dir.Y, dir.X);
    }
}
