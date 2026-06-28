using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core;

public static class ClusterRadialDataService
{
    // Compute the hydrostatic mass required by gravity
    public static void CalculateHydrostaticMass(List<ClusterRadialData> profile)
    {
        // Iterate through the radial profile
        for (int i = 1; i < profile.Count - 1; i++)
        {
            var p = profile[i];

            // 1) Compute density and temperature logarithmic gradients
            // This corresponds to the (d ln rho / d ln r) term
            double gradRho = (Math.Log(profile[i + 1].ElectronDensity) - Math.Log(profile[i - 1].ElectronDensity)) /
                             (Math.Log(profile[i + 1].RadiusKpc) - Math.Log(profile[i - 1].RadiusKpc));

            double gradT = (Math.Log(profile[i + 1].TemperatureKev) - Math.Log(profile[i - 1].TemperatureKev)) /
                           (Math.Log(profile[i + 1].RadiusKpc) - Math.Log(profile[i - 1].RadiusKpc));

            // 2) Compute mass proxy (M ~ r * T * gradients)
            // This is the mass effectively traced by the gas
            p.RequiredMass = Math.Abs(p.RadiusKpc * p.TemperatureKev * (gradRho + gradT));
        }
    }

    // Store data as: cluster name -> list of shells
    public static Dictionary<string, List<AcceptShell>> LoadAllClusters(string filePath)
    {
        var clusterDb = new Dictionary<string, List<AcceptShell>>();
        var lines = System.IO.File.ReadAllLines(filePath);

        foreach (var line in lines.Skip(2)) // Skip header lines
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10) continue;

            string clusterName = parts[0]; // Cluster name (e.g., "A1656")

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

        // Skip header lines starting with "#"
        foreach (var line in lines)
        {
            if (line.StartsWith("#")) continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 12) continue; // Safety check

            // Column mapping:
            // Col 1: Rin (Mpc), Col 3: nelec, Col 8: Pitpl, Col 10: Mgrav
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

        // Skip header lines (typically 2)
        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10) continue;

            list.Add(new AcceptShell
            {
                RadiusKpc = double.Parse(parts[0]) * 1000, // Mpc to kpc
                ElectronDensity = double.Parse(parts[2]),
                Pressure = double.Parse(parts[7]),         // Pitpl column
                ReportedMass = double.Parse(parts[10])    // Mgrav column
            });
        }
        return list;
    }


    public static void RunAnalysis(List<AcceptShell> clusterData)
    {
        double G = 4.30091e-6; // Gravitational constant (kpc * km^2 / s^2 / M_sun)

        for (int i = 1; i < clusterData.Count - 1; i++)
        {
            var prev = clusterData[i - 1];
            var next = clusterData[i + 1];
            var curr = clusterData[i];

            // 1) Compute pressure gradient (dP / dr)
            // Use finite difference between neighboring shells
            double dP = next.Pressure - prev.Pressure;
            double dr = next.RadiusKpc - prev.RadiusKpc;
            double dPdr = dP / dr;

            // 2) Convert electron density (nelec) to mass density (rho)
            // Factor 1.9 accounts for electrons + ions in plasma
            double rho = curr.ElectronDensity * 1.9 * 1.67e-24;

            // 3) Apply hydrostatic formula: M(r) = - (r^2 / G * rho) * (dP/dr)
            // Use absolute value since mass must be positive
            curr.CalculatedMass = Math.Abs(Math.Pow(curr.RadiusKpc, 2) / (G * rho) * dPdr);
        }
    }
}
