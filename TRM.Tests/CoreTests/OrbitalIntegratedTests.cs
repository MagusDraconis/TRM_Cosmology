using System;
using System.Collections.Generic;
using System.Text;
using TRM.Core;
using TRM.Core.Domains.Domain1.GalacticRotation;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

public class OrbitalIntegratedTests
{
    private readonly ITestOutputHelper _output;
    public OrbitalIntegratedTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_TRM_Rar_OrbitIntegratedModel()
    {
        string zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
        string mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");

        var rawPoints = SparcRarAnalysis.ParseRarFromZip(zipPath);
        var galaxyMeta = SparcRarAnalysis.LoadGalaxyMetaFromMrt(mrtPath);
        var scaling = TrmCosmologyParameters.Current();

        var trmDisk = SparcRarAnalysis.ApplyTrmDistanceMapping(
            rawPoints,
            galaxyMeta,
            scaling,
            BaryonMode.ExponentialDisk
        );

        double a0 = 1.2e-10;

        double sum = 0;
        int count = 0;

        var galaxies = trmDisk
            .GroupBy(p => p.GalaxyName);

        foreach(var g in galaxies)
        {
            var ordered = g.OrderBy(p => p.RadiusKpc).ToList();

            foreach(var p in ordered.Skip(2))
            {
                double gPred = OrbitalIntegrationService.ComputeIntegratedG(
                    ordered,
                    p.RadiusKpc,
                    a0
                );

                if(gPred <= 0 || p.GobsMs2 <= 0)
                    continue;

                double res = Math.Log10(p.GobsMs2) - Math.Log10(gPred);

                sum += res * res;
                count++;
            }
        }

        double rms = Math.Sqrt(sum / count);

        _output.WriteLine($"Orbit-integrated RMS = {rms:F4}");

        Assert.InRange(rms, 0.4, 1.5);
    }



}
