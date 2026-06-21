using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TRM.QuantumCore.Planck;

namespace TRM.QuantumCore.Statistics;

public static class UncertaintyAnalyzer
{

    public class AnalysisResult
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double RelativeError { get; set; }

        public override string ToString()
        {
            return
                $"Mean      : {Mean:E6}\n" +
                $"StdDev    : {StdDev:E6}\n" +
                $"Min       : {Min:E6}\n" +
                $"Max       : {Max:E6}\n" +
                $"Rel Error : {RelativeError:P6}";
        }
    }

    public static AnalysisResult Analyze(
        IReadOnlyList<double> deltaE,
        IReadOnlyList<double> deltaT)
    {
        if (deltaE.Count != deltaT.Count)
            throw new ArgumentException("deltaE and deltaT size mismatch");

        int n = deltaE.Count;

        var products = new double[n];

        for (int i = 0; i < n; i++)
            products[i] = deltaE[i] * deltaT[i];

        double mean = products.Average();

        double variance = 0.0;
        for (int i = 0; i < n; i++)
        {
            double d = products[i] - mean;
            variance += d * d;
        }
        variance /= n;

        double stdDev = Math.Sqrt(variance);

        return new AnalysisResult
        {
            Mean = mean,
            StdDev = stdDev,
            Min = products.Min(),
            Max = products.Max(),
            RelativeError = (mean - PhysicalConstantsSI.hbar) / PhysicalConstantsSI.hbar
        };
    }

    // ✅ wichtig für deine Log-Sampling-Physik
    public static double WeightedMean(
        IReadOnlyList<double> deltaE,
        IReadOnlyList<double> deltaT)
    {
        double sum = 0;
        double weightSum = 0;

        for (int i = 0; i < deltaE.Count; i++)
        {
            double product = deltaE[i] * deltaT[i];

            // physikalisch sinnvolle Gewichtung (Zeitfenster)
            double w = deltaT[i];

            sum += product * w;
            weightSum += w;
        }

        return sum / weightSum;
    }

    // CSV Export
    public static void ExportProducts(
        IReadOnlyList<double> deltaT,
        IReadOnlyList<double> deltaE,
        string filePath)
    {
        using var writer = new StreamWriter(filePath);

        writer.WriteLine("DeltaT,DeltaE,Product");

        for (int i = 0; i < deltaT.Count; i++)
        {
            double product = deltaE[i] * deltaT[i];

            writer.WriteLine(
                $"{deltaT[i].ToString("E", CultureInfo.InvariantCulture)}," +
                $"{deltaE[i].ToString("E", CultureInfo.InvariantCulture)}," +
                $"{product.ToString("E", CultureInfo.InvariantCulture)}");
        }
    }

    // Histogramm (für deine Python-Plots)
    public static void ExportHistogram(
        IReadOnlyList<double> deltaE,
        IReadOnlyList<double> deltaT,
        string filePath,
        int bins = 50)
    {
        var products = deltaE.Zip(deltaT, (e, t) => e * t).ToArray();

        double min = products.Min();
        double max = products.Max();
        double width = (max - min) / bins;

        var counts = new int[bins];

        foreach (var p in products)
        {
            int index = (int)((p - min) / width);
            if (index >= bins) index = bins - 1;
            counts[index]++;
        }

        using var writer = new StreamWriter(filePath);
        writer.WriteLine("BinStart,BinEnd,Count");

        for (int i = 0; i < bins; i++)
        {
            double start = min + i * width;
            double end = start + width;

            writer.WriteLine(
                $"{start.ToString("E", CultureInfo.InvariantCulture)}," +
                $"{end.ToString("E", CultureInfo.InvariantCulture)}," +
                $"{counts[i]}");
        }
    }
}

