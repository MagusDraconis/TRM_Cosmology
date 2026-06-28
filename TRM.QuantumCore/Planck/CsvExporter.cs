using System.Globalization;


namespace TRM.Simulations.Experiments;

public static class CsvExporter
{
    public static void Export(string path, List<ScanResult> data)
    {
        var lines = new List<string>
        {
            "epsL,epsT,epsM,c,hbar,G"
        };

        lines.AddRange(data.Select(r =>
            string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5}",
                r.epsL, r.epsT, r.epsM, r.c, r.hbar, r.G)
        ));

        File.WriteAllLines(path, lines);
    }
}