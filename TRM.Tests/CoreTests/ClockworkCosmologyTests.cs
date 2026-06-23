using System;
using System.Data;
using TRM.Core;
using TRM.Core.Domains.Domain4.Supernovae;
using TRM.Core.Shared;
using Xunit;
using Xunit.Abstractions;
using static System.Net.Mime.MediaTypeNames;

namespace TRM.Tests.CoreTests;

public class ClockworkCosmologyTests
{
    private readonly ITestOutputHelper _output;
    public ClockworkCosmologyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_CMB_KSpacePeakRatio_IsStable()
    {
        var solver = new CmbAcousticSolver();

        // Act: k-space sweep only
        var result = solver.FindPerfectPhysicalParameters();

        _output.WriteLine("--- TRM CMB K-SPACE PEAK RATIO TEST ---");
        _output.WriteLine($"Drive frequency: {result.TrmDriveFreq}");
        _output.WriteLine($"Doppler weight:  {result.DopplerWeight}");
        _output.WriteLine($"K1:              {result.K1}");
        _output.WriteLine($"K2:              {result.K2}");
        _output.WriteLine($"PeakRatio:       {result.PeakRatio}");
        _output.WriteLine($"Fitness:         {result.Fitness}");

        // Guard: valid peak detection
        Assert.True(result.K1 > 0.0, "K1 must be positive.");
        Assert.True(result.K2 > 0.0, "K2 must be positive.");
        Assert.True(result.Fitness < double.MaxValue, "CMB k-space sweep did not find valid peaks.");

        // Target acoustic peak ratio from Planck-like reference:
        // l2 / l1 ~= 540 / 220 = 2.454545...
        double targetRatio = 540.0 / 220.0;

        _output.WriteLine($"TargetRatio:     {targetRatio}");

        // Ratio must be in the expected physical band
        Assert.InRange(result.PeakRatio, 2.40, 2.50);

        // Fitness should be the absolute ratio error:
        // Fitness = Abs((K2 / K1) - targetRatio)
        Assert.True(
            result.Fitness < 0.05,
            $"Ratio fitness too high: {result.Fitness}. Target ratio: {targetRatio}, actual ratio: {result.PeakRatio}");
    }
    [Fact]
    public void Test_CMB_ScaleConsistency_WithCurrentTrmParameters()
    {
        var solver = new CmbAcousticSolver();

        var acoustic = solver.FindPerfectPhysicalParameters();

        Assert.True(acoustic.K1 > 0.0, "K1 must be positive.");
        Assert.True(acoustic.K2 > 0.0, "K2 must be positive.");
        Assert.True(acoustic.Fitness < double.MaxValue, "CMB k-space sweep did not find valid peaks.");

        double cs = 1.0 / Math.Sqrt(3.0);
        double driveFreq = acoustic.TrmDriveFreq;

        var scaling = TrmCosmologyParameters.Current();

        var prediction = solver.CalculateCmbScalePrediction(
            acoustic.K1,
            cs,
            acoustic.TrmDriveFreq,
            scaling);

        _output.WriteLine("--- TRM CMB SCALE CONSISTENCY TEST ---");
        _output.WriteLine($"Drive frequency: {acoustic.TrmDriveFreq}");
        _output.WriteLine($"Doppler weight:  {acoustic.DopplerWeight}");
        _output.WriteLine($"K1:              {acoustic.K1}");
        _output.WriteLine($"K2:              {acoustic.K2}");
        _output.WriteLine($"PeakRatio:       {acoustic.PeakRatio}");
        _output.WriteLine($"BetaEta:         {scaling.BetaEta}");
        _output.WriteLine($"Alpha:           {scaling.Alpha}");
        _output.WriteLine($"HT:              {scaling.HT}");
        _output.WriteLine($"etaRec:          {prediction.EtaRec}");
        _output.WriteLine($"zRec:            {prediction.ZRec}");
        _output.WriteLine($"dA:              {prediction.AngularDiameterDistance}");
        _output.WriteLine($"lPred:           {prediction.LPred}");

        Assert.True(prediction.EtaRec > 0.0, "etaRec must be positive.");
        Assert.True(prediction.ZRec > 0.0, "zRec must be positive.");
        Assert.True(prediction.AngularDiameterDistance > 0.0, "dA must be positive.");

        Assert.InRange(prediction.ZRec, 500.0, 2000.0);
        Assert.InRange(prediction.AngularDiameterDistance, 20000.0, 40000.0);
        Assert.InRange(prediction.LPred, 180.0, 280.0);
    }

    [Fact]
    public void Test_TrmDistanceMapper_BasicRedshiftMapping_IsPositive()
    {
        var scaling = TrmCosmologyParameters.Current();
        var mapper = new TrmDistanceMapper(scaling);

        double z = 1.0;

        var result = mapper.MapFromRedshift(z);

        _output.WriteLine("--- TRM DISTANCE MAPPER TEST ---");
        _output.WriteLine($"z:        {result.Z}");
        _output.WriteLine($"dBase:    {result.TrmBaseDistance}");
        _output.WriteLine($"dAngular: {result.TrmAngularDiameterDistance}");
        _output.WriteLine($"dLum:     {result.TrmLuminosityDistance}");

        Assert.True(result.TrmBaseDistance > 0.0);
        Assert.True(result.TrmAngularDiameterDistance > 0.0);
        Assert.True(result.TrmLuminosityDistance > 0.0);
    }

    [Fact]
    public void Test_TrmDistanceMapper_ConversionFactor_IsComputed()
    {
        var scaling = TrmCosmologyParameters.Current();
        var mapper = new TrmDistanceMapper(scaling);

        double z = 1.0;
        double grLuminosityDistance = 6600.0; // example external GR-like distance in Mpc

        var result = mapper.MapFromRedshift(
            z,
            grLuminosityDistance,
            DistanceMeasureKind.Luminosity);

        _output.WriteLine("--- TRM / GR DISTANCE CONVERSION TEST ---");
        _output.WriteLine($"z:                 {result.Z}");
        _output.WriteLine($"Input GR distance: {result.InputGrDistance}");
        _output.WriteLine($"TRM luminosity:    {result.TrmLuminosityDistance}");
        _output.WriteLine($"Conversion factor: {result.ConversionFactor}");

        Assert.True(result.ConversionFactor.HasValue);
        Assert.True(result.ConversionFactor.Value > 0.0);
    }
    [Fact]
    public void Test_TrmDistanceMapper_CmbScaleRedshift_IsConsistent()
    {
        var scaling = TrmCosmologyParameters.Current();
        var mapper = new TrmDistanceMapper(scaling);

        double zRec = 1177.6950315159318;

        var result = mapper.MapFromRedshift(zRec);

        _output.WriteLine("--- TRM DISTANCE MAPPER CMB-SCALE TEST ---");
        _output.WriteLine($"zRec:     {result.Z}");
        _output.WriteLine($"dBase:    {result.TrmBaseDistance}");
        _output.WriteLine($"dAngular: {result.TrmAngularDiameterDistance}");
        _output.WriteLine($"dLum:     {result.TrmLuminosityDistance}");

        double expectedDA = result.TrmBaseDistance / (1.0 + zRec);


        Assert.Equal(
            result.TrmBaseDistance / (1.0 + zRec),
            result.TrmAngularDiameterDistance,
            6);

        Assert.Equal(
            result.TrmBaseDistance * (1.0 + zRec),
            result.TrmLuminosityDistance,
            6);

        Assert.True(result.TrmLuminosityDistance > 0.0);
    }
    [Fact]
    public void Test_TrmDistanceMapper_SanityCurve_Behaviour_IsConsistent()
    {
        var scaling = TrmCosmologyParameters.Current();
        var mapper = new TrmDistanceMapper(scaling);

        var redshifts = new double[]
        {
        0.01, 0.1, 0.5, 1.0, 3.0, 10.0, 100.0, 1100.0
        };

        double prevBase = 0.0;
        double prevLum = 0.0;

        _output.WriteLine("--- TRM DISTANCE MAPPER SANITY TEST ---");

        foreach(var z in redshifts)
        {
            var r = mapper.MapFromRedshift(z);

            _output.WriteLine(
                $"z={z,8:F2} | dBase={r.TrmBaseDistance,10:F2} | dA={r.TrmAngularDiameterDistance,10:F2} | dL={r.TrmLuminosityDistance,12:F2}");

            // ✅ dBase muss monoton wachsen
            Assert.True(r.TrmBaseDistance > prevBase, $"dBase not increasing at z={z}");

            // ✅ dLum muss monoton wachsen
            Assert.True(r.TrmLuminosityDistance > prevLum, $"dLum not increasing at z={z}");

            // ✅ Grundrelationen
            Assert.Equal(
                r.TrmBaseDistance / (1.0 + z),
                r.TrmAngularDiameterDistance,
                6);

            Assert.Equal(
                r.TrmBaseDistance * (1.0 + z),
                r.TrmLuminosityDistance,
                6);

            // ✅ zentrale physikalische Relation
            Assert.Equal(
                r.TrmLuminosityDistance,
                r.TrmAngularDiameterDistance * Math.Pow(1.0 + z, 2),
                6);

            prevBase = r.TrmBaseDistance;
            prevLum = r.TrmLuminosityDistance;
        }

        // ✅ zusätzlicher globaler Check: CMB Verhalten

        var lowZ = mapper.MapFromRedshift(0.5);
        var midZ = mapper.MapFromRedshift(3.0);
        var highZ = mapper.MapFromRedshift(1100.0);

        // Angular distance hat Maximum irgendwo im mittleren z
        Assert.True(midZ.TrmAngularDiameterDistance > lowZ.TrmAngularDiameterDistance);
        Assert.True(highZ.TrmAngularDiameterDistance < midZ.TrmAngularDiameterDistance);
    }

    [Fact]
    public void Test_TrmDistanceMapper_LuminosityRelation_IsConsistent()
    {
        var scaling = TrmCosmologyParameters.Current();
        var mapper = new TrmDistanceMapper(scaling);

        double z = 1.5;

        var result = mapper.MapFromRedshift(z);

        double expectedLuminosity = result.TrmBaseDistance * (1.0 + z);

        _output.WriteLine("--- TRM DISTANCE MAPPER LUMINOSITY RELATION TEST ---");
        _output.WriteLine($"z:            {z}");
        _output.WriteLine($"dBase:        {result.TrmBaseDistance}");
        _output.WriteLine($"dLum:         {result.TrmLuminosityDistance}");
        _output.WriteLine($"expected dLum:{expectedLuminosity}");

        Assert.Equal(expectedLuminosity, result.TrmLuminosityDistance, 10);
    }

    [Fact]
    public void Test_Pantheon_TrmScaleDistance_WithCurrentParameters()
    {
        // Arrange
        var loader = new PantheonDataLoader();
        var dataPath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Pantheon+SH0ES.dat");

        if (!File.Exists(dataPath))
        {
            _output.WriteLine($"[SKIPPED] Data file not found at: {dataPath}");
            return;
        }

       

        var scaling = TrmCosmologyParameters.Current();
        var mapper = new TrmDistanceMapper(scaling);
        var solver = new PantheonTrmScaleSolver(mapper);
        var snData = loader.LoadPantheonData(dataPath);
        // Act
        var result = solver.Evaluate(snData);

        _output.WriteLine("--- TRM PANTHEON SCALE-DISTANCE TEST ---");
        _output.WriteLine($"Analyzed Supernovae:   {result.AnalyzedPoints}");
        _output.WriteLine($"HT:                    {scaling.HT}");
        _output.WriteLine($"BetaEta:               {scaling.BetaEta}");
        _output.WriteLine($"Alpha:                 {scaling.Alpha}");
        _output.WriteLine($"RMS Error:             {result.RmsError}");
        _output.WriteLine($"Mean Residual:         {result.MeanResidual}");
        _output.WriteLine($"Mean Abs Residual:     {result.MeanAbsResidual}");
        _output.WriteLine($"Max Abs Residual:      {result.MaxAbsResidual}");
        _output.WriteLine($"Centered RMS Error:    {result.CenteredRmsError}");

        // Assert
        Assert.True(result.AnalyzedPoints > 0, "No Pantheon supernovae were analyzed.");

        Assert.True(double.IsFinite(result.RmsError), "RMS error must be finite.");
        Assert.True(double.IsFinite(result.MeanResidual), "Mean residual must be finite.");
        Assert.True(double.IsFinite(result.MeanAbsResidual), "Mean absolute residual must be finite.");
        Assert.True(double.IsFinite(result.MaxAbsResidual), "Max absolute residual must be finite.");
        Assert.True(double.IsFinite(result.CenteredRmsError), "Centered RMS error must be finite.");

        Assert.True(result.CenteredRmsError > 0.0, "Centered RMS should not be zero unless all residuals are identical.");
        Assert.True(result.CenteredRmsError <= result.RmsError, "Centered RMS should be <= raw RMS.");

        Assert.True(result.RmsError < 0.25, $"TRM scale-distance Pantheon RMS is too high: {result.RmsError}");
        Assert.True(Math.Abs(result.MeanResidual) < 0.15, $"Mean residual too large: {result.MeanResidual}");
        Assert.True(result.MeanAbsResidual < 0.25, $"Mean absolute residual too high: {result.MeanAbsResidual}");
        Assert.True(result.CenteredRmsError < 0.25, $"Centered RMS error too high: {result.CenteredRmsError}");
        Assert.True(result.MaxAbsResidual < 1.5, $"Max absolute residual too high: {result.MaxAbsResidual}");
    }

    [Fact]
    public void Test_TrmDistanceMapper_HTSensitivity_CmbScale()
    {
        double zRec = 1177.6950315159318;

        var htValues = new[]
        {
        68.0,
        70.0,
        72.93,
        75.0,
        78.0
    };

        _output.WriteLine("--- TRM HT SENSITIVITY / CMB SCALE ---");

        foreach(var ht in htValues)
        {
            var scaling = TrmCosmologyParameters.WithHT(ht);
            var mapper = new TrmDistanceMapper(scaling);

            var r = mapper.MapFromRedshift(zRec);

            _output.WriteLine(
                $"HT={ht:F2} | " +
                $"dBase={r.TrmBaseDistance:F3} | " +
                $"dA={r.TrmAngularDiameterDistance:F6} | " +
                $"dL={r.TrmLuminosityDistance:E3}");

            Assert.True(r.TrmBaseDistance > 0);
            Assert.True(r.TrmAngularDiameterDistance > 0);
            Assert.True(r.TrmLuminosityDistance > 0);
        }
    }
    [Fact]
    public void Test_Pantheon_TrmScaleDistance_HTSensitivity()
    {
        var htValues = new[]
        {
        68.0,
        69.0,
        70.0,
        71.0,
        72.0,
        72.93,
        73.5,
        74.0,
        75.0,
        76.0,
        78.0
    };

        _output.WriteLine("--- TRM PANTHEON HT SENSITIVITY ---");

        foreach(var ht in htValues)
        {
            var scaling = TrmCosmologyParameters.WithHT(ht);

            var result = EvaluatePantheonScaleDistance(scaling);

            _output.WriteLine(
                $"HT={ht,6:F2} | " +
                $"RMS={result.RmsError:F6} | " +
                $"CenteredRMS={result.CenteredRmsError:F6} | " +
                $"MeanResidual={result.MeanResidual:F6} | " +
                $"MeanAbs={result.MeanAbsResidual:F6} | " +
                $"MaxAbs={result.MaxAbsResidual:F6}"
            );
        }
    }
    [Fact]
    public void Test_Pantheon_TrmScaleDistance_HTFineScan()
    {
        _output.WriteLine("--- TRM PANTHEON HT FINE SCAN ---");

        double bestHt = double.NaN;
        double bestRms = double.MaxValue;

        for(double ht = 69.5; ht <= 71.0; ht += 0.05)
        {
            var scaling = TrmCosmologyParameters.WithHT(ht);
            var result = EvaluatePantheonScaleDistance(scaling);

            _output.WriteLine(
                $"HT={ht,6:F2} | " +
                $"RMS={result.RmsError:F6} | " +
                $"MeanResidual={result.MeanResidual:F6} | " +
                $"MeanAbs={result.MeanAbsResidual:F6}");

            if(result.RmsError < bestRms)
            {
                bestRms = result.RmsError;
                bestHt = ht;
            }
        }

        _output.WriteLine($"BEST_HT={bestHt:F3} | BEST_RMS={bestRms:F6}");
    }
    private PantheonScaleDistanceResult EvaluatePantheonScaleDistance(
        TrmCosmologyParameters scaling)
    {
        var loader = new PantheonDataLoader();

        var dataPath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Pantheon+SH0ES.dat");

        if(!File.Exists(dataPath))
        {
            _output.WriteLine($"[SKIPPED] Data file not found at: {dataPath}");

            return new PantheonScaleDistanceResult(
                Count: 0,
                HT: scaling.HT,
                BetaEta: scaling.BetaEta,
                Alpha: scaling.Alpha,
                RmsError: double.NaN,
                MeanResidual: double.NaN,
                MeanAbsResidual: double.NaN,
                MaxAbsResidual: double.NaN,
                CenteredRmsError: double.NaN
            );
        }

        var mapper = new TrmDistanceMapper(scaling);
        var solver = new PantheonTrmScaleSolver(mapper);

        var snData = loader.LoadPantheonData(dataPath);

        var result = solver.Evaluate(snData);

        return new PantheonScaleDistanceResult(
            Count: result.AnalyzedPoints,
            HT: scaling.HT,
            BetaEta: scaling.BetaEta,
            Alpha: scaling.Alpha,
            RmsError: result.RmsError,
            MeanResidual: result.MeanResidual,
            MeanAbsResidual: result.MeanAbsResidual,
            MaxAbsResidual: result.MaxAbsResidual,
            CenteredRmsError: result.CenteredRmsError
        );
    }
}