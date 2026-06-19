using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core;

public static class ClusterRadialDataService
{
    // Hier berechnen wir, was die Schwerkraft halten müsste
    public static void CalculateHydrostaticMass(List<ClusterRadialData> profile)
    {
        // Wir iterieren durch das Profil
        for (int i = 1; i < profile.Count - 1; i++)
        {
            var p = profile[i];

            // 1. Berechne die Steigung (Ableitung) der Dichte und Temperatur
            // Das entspricht dem (d ln rho / d ln r) Teil der Formel
            double gradRho = (Math.Log(profile[i + 1].ElectronDensity) - Math.Log(profile[i - 1].ElectronDensity)) /
                             (Math.Log(profile[i + 1].RadiusKpc) - Math.Log(profile[i - 1].RadiusKpc));

            double gradT = (Math.Log(profile[i + 1].TemperatureKev) - Math.Log(profile[i - 1].TemperatureKev)) /
                           (Math.Log(profile[i + 1].RadiusKpc) - Math.Log(profile[i - 1].RadiusKpc));

            // 2. Jetzt die Masse berechnen (M ~ r * T * (Gradienten))
            // Das ist die Masse, die das Gas physikalisch "spürt"
            p.RequiredMass = Math.Abs(p.RadiusKpc * p.TemperatureKev * (gradRho + gradT));
        }
    }

    // Wir speichern die Daten in einem Dictionary: Cluster-Name -> Liste der Schalen
    public static Dictionary<string, List<AcceptShell>> LoadAllClusters(string filePath)
    {
        var clusterDb = new Dictionary<string, List<AcceptShell>>();
        var lines = System.IO.File.ReadAllLines(filePath);

        foreach (var line in lines.Skip(2)) // Überspringe Header
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10) continue;

            string clusterName = parts[0]; // Das ist der Name (z.B. "A1656")

            if (!clusterDb.ContainsKey(clusterName))
            {
                clusterDb[clusterName] = new List<AcceptShell>();
            }

            clusterDb[clusterName].Add(new AcceptShell
            {
                RadiusKpc = double.Parse(parts[1]) * 1000, // Rin
                ElectronDensity = double.Parse(parts[3]),
                Pressure = double.Parse(parts[8]),         // Pitpl
                ReportedMass = double.Parse(parts[10])    // Mgrav               

            });
        }
        return clusterDb;
    }

    public static List<AcceptShell> LoadComaData(string filePath)
    {
        var list = new List<AcceptShell>();
        var lines = System.IO.File.ReadAllLines(filePath);

        // Wir überspringen die Zeilen, die mit "#" beginnen (Header)
        foreach (var line in lines)
        {
            if (line.StartsWith("#")) continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 12) continue; // Sicherheitscheck

            // Mapping basierend auf deiner Spaltenübersicht:
            // Spalte 1: Rin (Mpc), Spalte 3: nelec, Spalte 8: Pitpl, Spalte 10: Mgrav
            list.Add(new AcceptShell
            {
                RadiusKpc = double.Parse(parts[1]) * 1000,
                ElectronDensity = double.Parse(parts[3]),
                Pressure = double.Parse(parts[8]),
                ReportedMass = double.Parse(parts[10])
            });
        }
        return list;
    }

    public static List<AcceptShell> ParseAcceptData(string filePath)
    {
        var list = new List<AcceptShell>();
        var lines = System.IO.File.ReadAllLines(filePath);

        // Wir überspringen die Kopfzeilen (bei dir ca. 2)
        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10) continue;

            list.Add(new AcceptShell
            {
                RadiusKpc = double.Parse(parts[0]) * 1000, // Mpc zu kpc
                ElectronDensity = double.Parse(parts[2]),
                Pressure = double.Parse(parts[7]),         // Spalte Pitpl
                ReportedMass = double.Parse(parts[10])    // Spalte Mgrav
            });
        }
        return list;
    }


    public static void RunAnalysis(List<AcceptShell> clusterData)
    {
        double G = 4.30091e-6; // Gravitationskonstante (kpc * km^2 / s^2 / M_sun)

        for (int i = 1; i < clusterData.Count - 1; i++)
        {
            var prev = clusterData[i - 1];
            var next = clusterData[i + 1];
            var curr = clusterData[i];

            // 1. Berechnung des Druckgradienten (dP / dr)
            // Wir nutzen den Differenzenquotienten zwischen den Nachbar-Schalen
            double dP = next.Pressure - prev.Pressure;
            double dr = next.RadiusKpc - prev.RadiusKpc;
            double dPdr = dP / dr;

            // 2. Umrechnung von Elektronendichte (nelec) in Massendichte (rho)
            // Faktor 1.9 berücksichtigt Elektronen + Ionen im Plasma
            double rho = curr.ElectronDensity * 1.9 * 1.67e-24;

            // 3. Anwendung der hydrostatischen Formel: M(r) = - (r^2 / G * rho) * (dP/dr)
            // Wir nehmen den Betrag, da die Masse positiv sein muss
            curr.CalculatedMass = Math.Abs(Math.Pow(curr.RadiusKpc, 2) / (G * rho) * dPdr);
        }
    }
}
