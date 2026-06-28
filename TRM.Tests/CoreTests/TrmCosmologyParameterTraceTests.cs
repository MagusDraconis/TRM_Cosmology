using System;
using TRM.Core;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

/// <summary>
/// Regression tests for the cosmology parameter derivation trace.
/// These tests ensure that BetaEta, Alpha, and HT remain explicitly marked
/// as calibrated parameters and that their algebraic inversion helpers are
/// internally consistent.
/// Status: tested traceability; calibrated, not first-principles derived.
/// </summary>
public class TrmCosmologyParameterTraceTests
{
    private readonly ITestOutputHelper _output;

    public TrmCosmologyParameterTraceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("Category", "CoreRegression")]
    [Fact]
    public void DerivationTrace_ShouldExpose_ThreeCentralCalibratedParameters()
    {
        var traces = TrmCosmologyParameters.GetDerivationTrace();

        _output.WriteLine("--- TRM COSMOLOGY DERIVATION TRACE INVENTORY ---");
        foreach (var trace in traces)
        {
            _output.WriteLine($"{trace.Name} = {trace.Value:G9} [{trace.Unit}] | {trace.Status}");
            _output.WriteLine($"Equation: {trace.DerivationEquation}");
        }

        Assert.Equal(3, traces.Count);
        Assert.Contains(traces, t => t.Name == "BetaEta" && t.Status == TrmParameterStatus.Calibrated);
        Assert.Contains(traces, t => t.Name == "Alpha" && t.Status == TrmParameterStatus.Calibrated);
        Assert.Contains(traces, t => t.Name == "HT" && t.Status == TrmParameterStatus.Calibrated);
    }

    [Trait("Category", "CoreRegression")]
    [Fact]
    public void CmbReferenceInversion_ShouldReconstructCurrent_BetaEta_And_Alpha()
    {
        double betaEta = TrmCosmologyParameters.GetBetaEta();
        double alpha = TrmCosmologyParameters.GetAlpha();
        double etaRec = 207.6;
        double zRec = Math.Exp(betaEta * alpha * etaRec) - 1.0;

        double inferredBetaEta = TrmCosmologyParameters.DeriveBetaEtaFromCmbReference(zRec, alpha, etaRec);
        double inferredAlpha = TrmCosmologyParameters.DeriveAlphaFromCmbReference(zRec, betaEta, etaRec);

        _output.WriteLine("--- TRM CMB PARAMETER INVERSION ---");
        _output.WriteLine($"zRec            : {zRec:E6}");
        _output.WriteLine($"betaEta current : {betaEta:E9}");
        _output.WriteLine($"betaEta infer   : {inferredBetaEta:E9}");
        _output.WriteLine($"alpha current   : {alpha:E9}");
        _output.WriteLine($"alpha infer     : {inferredAlpha:E9}");

        Assert.Equal(betaEta, inferredBetaEta, 12);
        Assert.Equal(alpha, inferredAlpha, 12);
    }

    [Trait("Category", "CoreRegression")]
    [Fact]
    public void DistanceAnchorInversion_ShouldReconstructCurrentHT()
    {
        double z = 1.5;
        double ht = TrmCosmologyParameters.GetHT();
        double dBase = (PhysicalConstants.C_Kms / ht) * Math.Log(1.0 + z);

        double inferredHt = TrmCosmologyParameters.DeriveHTFromDistanceAnchor(z, dBase);

        _output.WriteLine("--- TRM HT DISTANCE-ANCHOR INVERSION ---");
        _output.WriteLine($"HT current      : {ht:E9}");
        _output.WriteLine($"HT inferred     : {inferredHt:E9}");
        _output.WriteLine($"D_base(z=1.5)   : {dBase:E9}");

        Assert.Equal(ht, inferredHt, 12);
    }
}
