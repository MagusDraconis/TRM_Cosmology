using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TRM.Core;

namespace TRM.Tests;

public class RarRelationTests
{
    private readonly ITestOutputHelper _output;

    public RarRelationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_Parse_RotmodZip_And_Verify_AccelerationScale()
    {
        string zipPath = "Rotmod_LTG.zip";
        Assert.True(File.Exists(zipPath), "Die Datei Rotmod_LTG.zip muss im Ausgabeverzeichnis liegen.");

        // Extrahiere alle radialen Datenpunkte aller Galaxien
        var rarData = SparcRarAnalysis.ParseRarFromZip(zipPath);

        // Ausgaben für den Alltag im Visual Studio Test-Explorer
        _output.WriteLine($"Gesamte radiale Datenpunkte geladen: {rarData.Count}");

        // Validierung, dass Daten da sind
        Assert.NotEmpty(rarData);

        // Stichprobe: Berechnete Werte im Log10-Raum ausgeben
        var sample = rarData.First();
        _output.WriteLine($"Galaxie-Stichprobe: {sample.GalaxyName} bei R={sample.RadiusKpc} kpc");
        _output.WriteLine($"  log10(g_bar): {Math.Log10(sample.GbarMs2):F4}");
        _output.WriteLine($"  log10(g_obs): {Math.Log10(sample.GobsMs2):F4}");

        // Teste, ob sich die Werte auf der korrekten physikalischen Skala bewegen
        // Typische galaktische Beschleunigungen liegen zwischen 10^-12 und 10^-8 m/s^2
        foreach (var point in rarData.Take(100))
        {
            double logGobs = Math.Log10(point.GobsMs2);
            Assert.InRange(logGobs, -13.0, -7.0);
        }
    }

    [Fact]
    public void Test_Verify_Rar_Asymptotic_Limits()
    {
        string zipPath = "Rotmod_LTG.zip";
        var rarData = SparcRarAnalysis.ParseRarFromZip(zipPath);

        // Berechne die gemittelten Profile über alle Galaxien hinweg
        var bins = SparcRarAnalysis1.ComputeRarProfiles(rarData);

        _output.WriteLine("--- RADIAL ACCELERATION RELATION (RAR) PROFILE ---");
        _output.WriteLine("log10(g_bar) | log10(g_obs) | StdDev | Punkte");

        foreach (var bin in bins)
        {
            _output.WriteLine($"{bin.LogGbarCenter:F2}       | {bin.MeanLogGobs:F2}        | {bin.StandardDeviation:F3}  | {bin.PointCount}");
        }

        // 1. Validierung des Newton-Grenzfalls (hohe Beschleunigungen nahe -8.5)
        var highAccBin = bins.FirstOrDefault(b => Math.Abs(b.LogGbarCenter - (-8.6)) < 0.1);
        if (highAccBin != null)
        {
            // Im inneren Bereich der Galaxien darf die Abweichung von der 1:1 Linie nur minimal sein
            double deviation = Math.Abs(highAccBin.MeanLogGobs - highAccBin.LogGbarCenter);
            Assert.True(deviation < 0.15, $"Newton-Abweichung zu hoch bei hoher Beschleunigung: {deviation}");
        }

        // 2. Validierung des asymptotischen Verhaltens (tiefe Beschleunigungen nahe -11.5)
        var lowAccBin = bins.FirstOrDefault(b => Math.Abs(b.LogGbarCenter - (-11.4)) < 0.1);
        if (lowAccBin != null)
        {
            // Bei tiefen Beschleunigungen muss g_obs signifikant größer sein als g_bar (Scheinbare Dunkle Materie)
            Assert.True(lowAccBin.MeanLogGobs > lowAccBin.LogGbarCenter,
                "Im Außenbereich der Galaxie fehlt der makroskopische Synchronisations-Support!");

            // Bestimmung des impliziten a_0 Werts aus dem tiefsten verlässlichen Bin:
            // log10(g_obs) = 0.5 * log10(g_bar) + 0.5 * log10(a_0)
            // => log10(a_0) = 2 * log10(g_obs) - log10(g_bar)
            double calculatedLogA0 = 2 * lowAccBin.MeanLogGobs - lowAccBin.LogGbarCenter;
            _output.WriteLine($"\nAbgeleiteter kosmischer Beschleunigungsanker log10(a_0): {calculatedLogA0:F4} m/s^2");
        }
    }
    [Fact]
    public void Test_Global_NonLinear_Fit_For_A0()
    {
        string zipPath = "Rotmod_LTG.zip";
        string mrtPath = "SPARC_Lelli2016c.mrt";

        // Nutze die neue Methode mit integriertem Inklinations-Matching
        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser
            .ParseFile(mrtPath)
            .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        var (bestLogA0, bestA0, rmsError) = SparcRarAnalysis.FitA0(rarData, inclinations);



        _output.WriteLine("--- GLOBALER RAR-FIT ERGEBNISSE ---");
        _output.WriteLine($"Optimiertes log10(a_0): {bestLogA0:F4} m/s^2");
        _output.WriteLine($"Realer Wert a_0:        {bestA0:E4} m/s^2");
        _output.WriteLine($"Mittlerer Fehler (RMS): {rmsError:F4} dex");
        _output.WriteLine($"Analysierte Punkte:     {rarData.Count}");

        // Wissenschaftliche Validierung: 
        // Der Wert muss sich extrem stabil im astrophysikalischen Fenster einpendeln.
        // Lelli et al. finden empirisch ca. -9.85.
        Assert.InRange(bestLogA0, -10.1, -9.6);

        // Ein RMS-Fehler im Log-Raum unter 0.15 dex zeigt einen exzellenten Fit der Kurve
        Assert.True(rmsError < 0.15, $"Der RMS-Fehler des Modells ist ungewöhnlich hoch: {rmsError}");
        //Assert.True(rmsError == 0.15, $"Der RMS-Wert ist: {rmsError} Der beste Wert für a_0 ist: {bestA0}");
    }

    [Fact]
    public void Test_Clockwork_vs_MOND_Global_Fit()
    {
        string zipPath = "Rotmod_LTG.zip";
        string mrtPath = "SPARC_Lelli2016c.mrt";

        var rarData = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(zipPath, mrtPath);
        var inclinations = SparcMrtParser.ParseFile(mrtPath).ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

        // 1. RECHNE KLASSISCHES MOND
        var (mondLogA0, mondA0, mondRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.MOND);

        // 2. RECHNE CLOCKWORK COSMOLOGY TRM
        var (trmLogA0, trmA0, trmRms) = SparcRarAnalysis.FitA0(rarData, inclinations, ModelType.ClockworkTRM);

        _output.WriteLine($"--- REALE SPARC VERGLEICHS-ERGEBNISSE ---");
        _output.WriteLine($"MOND      -> log10(a0): {mondLogA0:F4} | RMS: {mondRms:F4} dex");
        _output.WriteLine($"CLOCKWORK -> log10(a0): {trmLogA0:F4} | RMS: {trmRms:F4} dex");

        // Beide Modelle müssen sich im physikalischen Fenster einpendeln
        Assert.InRange(trmLogA0, -10.1, -9.6);
        Assert.True(trmRms < 0.15);
    }
}
