using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TRM.Simulations.Experiments;

public static class UncertaintyCsvExporter
{
    public static void Export(string path, List<UncertaintyResult> data)
    {
        var lines = new List<string>
        {
            "DeltaT,MeanTemporalFluctuation,StdTemporalFluctuation,DeltaE,Product"
        };

        lines.AddRange(data.Select(r =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4}",
                r.DeltaT,
                r.MeanTemporalFluctuation,
                r.StdTemporalFluctuation,
                r.DeltaE,
                r.Product)
        ));

        File.WriteAllLines(path, lines);
    }
}
