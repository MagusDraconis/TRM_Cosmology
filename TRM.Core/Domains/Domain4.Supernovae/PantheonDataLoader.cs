using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TRM.Core.Domains.Domain4.Supernovae;

public class PantheonDataLoader
{
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
