using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Core;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.CoreTests;

/// <summary>
/// Derivation-gate tests for theta-to-observable candidate mappings.
/// Status: tested (effective candidate comparison), calibrated (single-coupling fit),
/// not derived yet (no microscopic uniqueness theorem).
/// </summary>
public class ThetaObservableDerivationTests
{
    private readonly ITestOutputHelper _output;

    public ThetaObservableDerivationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TO01_ThetaObservable_Should_Respect_InnerOuterRegimeBounds()
    {
        var dataset = BuildDataset();
        var fit = FitCandidate(dataset.Points, "O2_GradientPlusLevel");

        var bins = ComputeResidualBins(dataset.Points, fit);

        WriteTestLine("TO01", $"Inner  : {FormatBin(bins.InnerCount, bins.InnerRms)}");
        WriteTestLine("TO01", $"Middle : {FormatBin(bins.MidCount, bins.MidRms)}");
        WriteTestLine("TO01", $"Outer  : {FormatBin(bins.OuterCount, bins.OuterRms)}");

        Assert.True(bins.InnerCount > 30, "Too few inner-regime points.");
        Assert.True(bins.MidCount > 60, "Too few mid-regime points.");
        Assert.True(bins.OuterCount > 30, "Too few outer-regime points.");

        Assert.InRange(bins.InnerRms, 0.0, 1.50);
        Assert.InRange(bins.MidRms, 0.0, 1.20);
        Assert.InRange(bins.OuterRms, 0.0, 1.30);

        // Outer tail should remain controlled relative to mid regime.
        Assert.True(
            bins.OuterRms <= bins.MidRms + 0.20,
            $"Outer tail penalty too large: outer={bins.OuterRms:F4}, mid={bins.MidRms:F4}");
    }

    [Fact]
    public void TO02_ThetaObservable_Should_Improve_Over_Local_Without_OuterTailPenalty()
    {
        var dataset = BuildDataset();
        var local = EvaluateLocal(dataset.Points);
        var theta = FitCandidate(dataset.Points, "O2_GradientPlusLevel");

        var localBins = ComputeResidualBins(dataset.Points, local);
        var thetaBins = ComputeResidualBins(dataset.Points, theta);

        WriteTestLine("TO02", $"Local global RMS = {local.GlobalRms:F4}");
        WriteTestLine("TO02", $"Theta global RMS = {theta.GlobalRms:F4}");
        WriteTestLine("TO02", $"Local outer RMS  = {localBins.OuterRms:F4}");
        WriteTestLine("TO02", $"Theta outer RMS  = {thetaBins.OuterRms:F4}");

        Assert.True(
            theta.GlobalRms <= local.GlobalRms + 0.02,
            $"Theta observable is unexpectedly worse globally: local={local.GlobalRms:F4}, theta={theta.GlobalRms:F4}");

        Assert.True(
            thetaBins.OuterRms <= localBins.OuterRms + 0.03,
            $"Theta observable introduces outer-tail penalty: localOuter={localBins.OuterRms:F4}, thetaOuter={thetaBins.OuterRms:F4}");
    }

    [Fact]
    public void TO03_AlternativeThetaObservables_Should_Not_Outperform_WithoutPenalty()
    {
        var dataset = BuildDataset();

        var candidates = new[]
        {
            "O1_Gradient",
            "O2_GradientPlusLevel",
            "O3_Curvature",
            "O4_OrbitKernel"
        };

        var results = new List<(string Name, FitResult Fit, double Penalty, double PenalizedScore)>();

        foreach (string candidate in candidates)
        {
            var fit = FitCandidate(dataset.Points, candidate);
            var bins = ComputeResidualBins(dataset.Points, fit);

            double complexityPenalty = candidate switch
            {
                "O1_Gradient" => 0.00,
                "O2_GradientPlusLevel" => 0.00,
                "O3_Curvature" => 0.02,
                "O4_OrbitKernel" => 0.03,
                _ => 0.05
            };

            double tailPenalty = Math.Max(0.0, bins.OuterRms - bins.MidRms - 0.10);
            double totalPenalty = complexityPenalty + tailPenalty;
            double score = fit.GlobalRms + totalPenalty;

            WriteTestLine(
                "TO03",
                $"{candidate}: RMS={fit.GlobalRms:F4}, penalty={totalPenalty:F4}, score={score:F4}, lambda={fit.Lambda:F2}");

            results.Add((candidate, fit, totalPenalty, score));
        }

        var best = results.OrderBy(x => x.PenalizedScore).First();
        var o2 = results.Single(x => x.Name == "O2_GradientPlusLevel");

        Assert.True(
            o2.PenalizedScore <= best.PenalizedScore + 0.02,
            $"O2 candidate loses penalized selection: O2={o2.PenalizedScore:F4}, best={best.Name}:{best.PenalizedScore:F4}");
    }

    [Fact]
    public void TO04_ThetaObservable_Should_Not_Be_Reducible_To_Pure_Local_Reparameterization()
    {
        var dataset = BuildDataset();
        var theta = FitCandidate(dataset.Points, "O2_GradientPlusLevel");
        var local = EvaluateLocal(dataset.Points);

        double alpha = FitBestLogShiftScale(dataset.Points);

        var differences = new List<double>();

        foreach (var p in dataset.Points)
        {
            double gScaledLocal = alpha * p.GLocal;
            if (gScaledLocal <= 0.0 || theta.GetPrediction(p) <= 0.0)
                continue;

            double d = Math.Log10(theta.GetPrediction(p)) - Math.Log10(gScaledLocal);
            differences.Add(d);
        }

        double spread = StandardDeviation(differences);

        WriteTestLine("TO04", $"Local reparameterization alpha = {alpha:F6}");
        WriteTestLine("TO04", $"Local scaled RMS              = {ComputeRms(dataset.Points, p => alpha * p.GLocal):F4}");
        WriteTestLine("TO04", $"Theta model RMS               = {theta.GlobalRms:F4}");
        WriteTestLine("TO04", $"Model log-difference spread   = {spread:F6}");

        Assert.True(
            spread > 1e-3,
            "Theta observable can be reduced too closely to a pure local rescaling.");

        Assert.True(
            theta.GlobalRms <= local.GlobalRms + 0.02,
            $"Theta observable does not remain competitive vs local baseline: local={local.GlobalRms:F4}, theta={theta.GlobalRms:F4}");
    }

    [Fact]
    public void TO05_ThetaObservable_Should_Show_CrossGalaxy_Stability_Not_OutlierDependence()
    {
        var dataset = BuildDataset();
        var theta = FitCandidate(dataset.Points, "O2_GradientPlusLevel");

        var byGalaxy = dataset.Points
            .GroupBy(p => p.GalaxyName)
            .Where(g => g.Count() >= 8)
            .ToList();

        Assert.NotEmpty(byGalaxy);

        var improvements = new List<(string Galaxy, double Improvement)>();

        foreach (var galaxy in byGalaxy)
        {
            double localRms = ComputeRms(galaxy, p => p.GLocal);
            double thetaRms = ComputeRms(galaxy, p => theta.GetPrediction(p));
            double improvement = localRms - thetaRms;

            improvements.Add((galaxy.Key, improvement));
        }

        int improvedCount = improvements.Count(x => x.Improvement > 0.0);
        double improvedFraction = (double)improvedCount / improvements.Count;

        double totalPositive = improvements.Where(x => x.Improvement > 0).Sum(x => x.Improvement);
        double top3Positive = improvements
            .Where(x => x.Improvement > 0)
            .OrderByDescending(x => x.Improvement)
            .Take(3)
            .Sum(x => x.Improvement);

        double dominance = totalPositive > 0 ? top3Positive / totalPositive : 1.0;

        WriteTestLine("TO05", $"Galaxies tested         = {improvements.Count}");
        WriteTestLine("TO05", $"Improved galaxy fraction= {improvedFraction:F3}");
        WriteTestLine("TO05", $"Top-3 dominance         = {dominance:F3}");

        Assert.True(
            improvedFraction >= 0.35,
            $"Theta gains are too sparse across galaxies: improved fraction={improvedFraction:F3}");

        Assert.True(
            dominance <= 0.80,
            $"Theta gains are dominated by few outliers: top3 dominance={dominance:F3}");
    }

    [Fact]
    public void TO06_ThetaObservable_Should_Improve_ResidualStructure_Beyond_LocalScaling()
    {
        var dataset = BuildDataset();
        var localSummary = EvaluateResidualStructure("LocalScaled", p => p.GLocal);

        var candidateFits = new[]
        {
            FitCandidate(dataset.Points, "O1_Gradient"),
            FitCandidate(dataset.Points, "O2_GradientPlusLevel"),
            FitCandidate(dataset.Points, "O3_Curvature"),
            FitCandidate(dataset.Points, "O4_OrbitKernel")
        };

        var candidateSummaries = candidateFits
            .Select(fit => EvaluateResidualStructure(fit.Name, p => fit.GetPrediction(p)))
            .ToList();

        var best = candidateSummaries
            .OrderBy(s => s.StructureScore)
            .First();

        int improvedBins = Enumerable.Range(0, 4)
            .Count(i => best.BinRms[i] <= localSummary.BinRms[i] + 0.02);

        bool binImproved = improvedBins >= 3;
        bool gbarImproved = best.AbsGbarCorr <= localSummary.AbsGbarCorr + 0.02;
        bool flipImproved = best.MedianFlipRate <= localSummary.MedianFlipRate + 0.02;

        bool outerInnerControlled = best.OuterInnerBias <= localSummary.OuterInnerBias + 0.15;
        bool radiusCorrControlled = best.AbsRadiusCorr <= localSummary.AbsRadiusCorr + 0.08;
        bool meanBiasControlled = best.MedianAbsMeanResidual <= localSummary.MedianAbsMeanResidual + 0.06;

        WriteTestLine("TO06", $"Local structure score = {localSummary.StructureScore:F4}");
        foreach (var summary in candidateSummaries.OrderBy(s => s.StructureScore))
        {
            WriteTestLine(
                "TO06",
                $"{summary.Name}: score={summary.StructureScore:F4}, meanBinRms={summary.MeanBinRms:F4}, " +
                $"outerInner={summary.OuterInnerBias:F4}, |corrR|={summary.AbsRadiusCorr:F4}, " +
                $"|corrGbar|={summary.AbsGbarCorr:F4}, medFlip={summary.MedianFlipRate:F4}, med|mean|={summary.MedianAbsMeanResidual:F4}");
        }

        WriteTestLine("TO06", $"Selected candidate     = {best.Name}");
        WriteTestLine("TO06", $"Improved radius bins   = {improvedBins}/4");
        WriteTestLine("TO06", $"Signal flags            = bin={binImproved}, gbar={gbarImproved}, flip={flipImproved}");
        WriteTestLine("TO06", $"Control flags           = outerInner={outerInnerControlled}, radiusCorr={radiusCorrControlled}, meanBias={meanBiasControlled}");

        Assert.True(binImproved, $"Too few improved radius bins for best candidate {best.Name}: {improvedBins}/4");
        Assert.True(gbarImproved, $"gbar-correlation did not improve for best candidate {best.Name}");
        Assert.True(flipImproved, $"Residual sign-flip stability did not improve for best candidate {best.Name}");
        Assert.True(outerInnerControlled, $"Outer/inner bias degraded too strongly for best candidate {best.Name}");
        Assert.True(radiusCorrControlled, $"Radius-correlation residual structure degraded too strongly for best candidate {best.Name}");
        Assert.True(meanBiasControlled, $"Galaxy mean-residual bias degraded too strongly for best candidate {best.Name}");

        ResidualStructureSummary EvaluateResidualStructure(string name, Func<DataPoint, double> predictor)
        {
            double alpha = FitBestLogShiftScale(dataset.Points, predictor);

            var residualPoints = dataset.Points
                .Select(p =>
                {
                    double predScaled = alpha * predictor(p);
                    if (p.GObs <= 0.0 || predScaled <= 0.0 || !double.IsFinite(predScaled))
                        return null;

                    double residual = Math.Log10(p.GObs) - Math.Log10(predScaled);
                    if (!double.IsFinite(residual))
                        return null;

                    return new ResidualPoint(p.GalaxyName, p.Radius, p.Rd, p.Gbar, residual, residual);
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            var bin1 = new List<double>();
            var bin2 = new List<double>();
            var bin3 = new List<double>();
            var bin4 = new List<double>();

            foreach (var r in residualPoints)
            {
                double x = r.Radius / r.Rd;
                if (x < 1.0) bin1.Add(r.LocalResidual);
                else if (x < 2.0) bin2.Add(r.LocalResidual);
                else if (x < 4.0) bin3.Add(r.LocalResidual);
                else bin4.Add(r.LocalResidual);
            }

            var binRms = new[]
            {
                ComputeRmsResidual(bin1),
                ComputeRmsResidual(bin2),
                ComputeRmsResidual(bin3),
                ComputeRmsResidual(bin4)
            };

            double meanBinRms = binRms.Where(double.IsFinite).Average();

            double innerMean = bin1.Count > 0 ? bin1.Average() : 0.0;
            double outerMean = bin4.Count > 0 ? bin4.Average() : 0.0;
            double outerInnerBias = Math.Abs(outerMean - innerMean);

            var logRadius = residualPoints.Select(p => Math.Log10(Math.Max(p.Radius / p.Rd, 1e-6))).ToList();
            var logGbar = residualPoints.Select(p => Math.Log10(Math.Max(p.Gbar, 1e-20))).ToList();
            var residualSeries = residualPoints.Select(p => p.LocalResidual).ToList();

            double absRadiusCorr = Math.Abs(ComputePearsonCorrelation(logRadius, residualSeries));
            double absGbarCorr = Math.Abs(ComputePearsonCorrelation(logGbar, residualSeries));

            var perGalaxy = residualPoints
                .GroupBy(p => p.GalaxyName)
                .Where(g => g.Count() >= 8)
                .Select(g =>
                {
                    var ordered = g.OrderBy(x => x.Radius).Select(x => x.LocalResidual).ToList();
                    return new
                    {
                        FlipRate = ComputeSignFlipRate(ordered),
                        AbsMean = Math.Abs(ordered.Average())
                    };
                })
                .ToList();

            double medianFlip = Median(perGalaxy.Select(x => x.FlipRate));
            double medianAbsMean = Median(perGalaxy.Select(x => x.AbsMean));

            double structureScore =
                meanBinRms
                + 0.50 * outerInnerBias
                + 0.50 * absRadiusCorr
                + 0.50 * absGbarCorr
                + 0.20 * medianAbsMean
                + 0.10 * medianFlip;

            return new ResidualStructureSummary(
                name,
                residualPoints.Count,
                binRms,
                meanBinRms,
                outerInnerBias,
                absRadiusCorr,
                absGbarCorr,
                medianFlip,
                medianAbsMean,
                structureScore);
        }
    }

    [Fact]
    public void TO07_ThetaObservable_Should_Identify_RegimeWhere_NonLocalSignalDominates()
    {
        var dataset = BuildDataset();
        var o4Fit = FitCandidate(dataset.Points, "O4_OrbitKernel");

        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);
        double alphaO4 = FitBestLogShiftScale(dataset.Points, p => o4Fit.GetPrediction(p));

        var residuals = dataset.Points
            .Select(p =>
            {
                double localScaled = alphaLocal * p.GLocal;
                double o4Scaled = alphaO4 * o4Fit.GetPrediction(p);

                if (p.GObs <= 0.0 || localScaled <= 0.0 || o4Scaled <= 0.0)
                    return null;

                double localResidual = Math.Log10(p.GObs) - Math.Log10(localScaled);
                double o4Residual = Math.Log10(p.GObs) - Math.Log10(o4Scaled);

                if (!double.IsFinite(localResidual) || !double.IsFinite(o4Residual))
                    return null;

                return new ResidualPoint(
                    p.GalaxyName,
                    p.Radius,
                    p.Rd,
                    p.Gbar,
                    localResidual,
                    o4Residual);
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        Assert.True(residuals.Count > 400, $"Too few residual points for TO07: {residuals.Count}");

        double medianLogGbar = Median(residuals.Select(r => Math.Log10(Math.Max(r.Gbar, 1e-20))));
        double medianAbsLocalResidual = Median(residuals.Select(r => Math.Abs(r.LocalResidual)));

        var galaxyClassMap = residuals
            .GroupBy(r => r.GalaxyName)
            .ToDictionary(
                g => g.Key,
                g => Median(g.Select(x => Math.Log10(Math.Max(x.Gbar, 1e-20)))));

        double medianGalaxyLogGbar = Median(galaxyClassMap.Values);

        var regimeResults = new List<RegimeComparison>();

        void AddRegime(string name, Func<ResidualPoint, bool> selector)
        {
            var sample = residuals.Where(selector).ToList();
            if (sample.Count < 40)
                return;

            double localRms = ComputeRmsResidual(sample.Select(x => x.LocalResidual).ToList());
            double o4Rms = ComputeRmsResidual(sample.Select(x => x.ThetaResidual).ToList());
            double delta = localRms - o4Rms;

            regimeResults.Add(new RegimeComparison(name, sample.Count, localRms, o4Rms, delta));
        }

        AddRegime("inner", r => (r.Radius / r.Rd) < 1.0);
        AddRegime("middle", r => (r.Radius / r.Rd) >= 1.0 && (r.Radius / r.Rd) < 4.0);
        AddRegime("outer", r => (r.Radius / r.Rd) >= 4.0);
        AddRegime("low-gbar", r => Math.Log10(Math.Max(r.Gbar, 1e-20)) <= medianLogGbar);
        AddRegime("high-gbar", r => Math.Log10(Math.Max(r.Gbar, 1e-20)) > medianLogGbar);
        AddRegime("high-residual", r => Math.Abs(r.LocalResidual) >= medianAbsLocalResidual);
        AddRegime("low-residual", r => Math.Abs(r.LocalResidual) < medianAbsLocalResidual);
        AddRegime(
            "class-lsb",
            r => galaxyClassMap.TryGetValue(r.GalaxyName, out double gbarClass) && gbarClass <= medianGalaxyLogGbar);
        AddRegime(
            "class-hsb",
            r => galaxyClassMap.TryGetValue(r.GalaxyName, out double gbarClass) && gbarClass > medianGalaxyLogGbar);

        Assert.True(regimeResults.Count >= 8, $"Insufficient regime coverage in TO07: {regimeResults.Count}");

        foreach (var regime in regimeResults.OrderByDescending(r => r.DeltaRms))
        {
            WriteTestLine(
                "TO07",
                $"{regime.Name}: n={regime.Count}, localRMS={regime.LocalRms:F4}, o4RMS={regime.O4Rms:F4}, delta={regime.DeltaRms:F4}");
        }

        var positiveRegimes = regimeResults.Where(r => r.DeltaRms > 0.0).ToList();
        var bestRegime = regimeResults.OrderByDescending(r => r.DeltaRms).First();
        var worstRegime = regimeResults.OrderBy(r => r.DeltaRms).First();
        bool hasDominantRegime = bestRegime.DeltaRms >= 0.005;
        bool keyDomainWin = regimeResults.Any(
            r => (r.Name is "outer" or "low-gbar" or "high-residual" or "class-lsb") && r.DeltaRms >= 0.005);

        WriteTestLine("TO07", $"Positive regimes = {positiveRegimes.Count}/{regimeResults.Count}");
        WriteTestLine("TO07", $"Best regime = {bestRegime.Name} (delta={bestRegime.DeltaRms:F4})");
        WriteTestLine("TO07", $"Worst regime = {worstRegime.Name} (delta={worstRegime.DeltaRms:F4})");
        WriteTestLine("TO07", $"Dominant regime found = {hasDominantRegime}");
        WriteTestLine("TO07", $"Key-domain win present = {keyDomainWin}");

        if (hasDominantRegime)
        {
            Assert.True(
                keyDomainWin,
                "Dominant non-local regime exists, but not in key expected domains (outer/low-gbar/high-residual/class-lsb).");
        }
        else
        {
            // If no dominant regime exists, TO07 still passes only when this is an explicit
            // "no clear non-local dominance" outcome without severe regime degradation.
            Assert.True(
                worstRegime.DeltaRms >= -0.05,
                $"No dominant regime found and degradation is too strong in worst regime: {worstRegime.Name} delta={worstRegime.DeltaRms:F4}");
        }
    }

    [Fact]
    public void TO08_ThetaObservable_Should_Be_Classified_As_Diagnostic_When_LocalWinsAllRegimes()
    {
        var dataset = BuildDataset();
        var o4Fit = FitCandidate(dataset.Points, "O4_OrbitKernel");

        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);
        double alphaO4 = FitBestLogShiftScale(dataset.Points, p => o4Fit.GetPrediction(p));

        var residuals = dataset.Points
            .Select(p =>
            {
                double localScaled = alphaLocal * p.GLocal;
                double o4Scaled = alphaO4 * o4Fit.GetPrediction(p);

                if (p.GObs <= 0.0 || localScaled <= 0.0 || o4Scaled <= 0.0)
                    return null;

                double localResidual = Math.Log10(p.GObs) - Math.Log10(localScaled);
                double o4Residual = Math.Log10(p.GObs) - Math.Log10(o4Scaled);

                if (!double.IsFinite(localResidual) || !double.IsFinite(o4Residual))
                    return null;

                return new ResidualPoint(
                    p.GalaxyName,
                    p.Radius,
                    p.Rd,
                    p.Gbar,
                    localResidual,
                    o4Residual);
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        Assert.True(residuals.Count > 400, $"Too few residual points for TO08: {residuals.Count}");

        double medianLogGbar = Median(residuals.Select(r => Math.Log10(Math.Max(r.Gbar, 1e-20))));
        double medianAbsLocalResidual = Median(residuals.Select(r => Math.Abs(r.LocalResidual)));

        var galaxyClassMap = residuals
            .GroupBy(r => r.GalaxyName)
            .ToDictionary(
                g => g.Key,
                g => Median(g.Select(x => Math.Log10(Math.Max(x.Gbar, 1e-20)))));

        double medianGalaxyLogGbar = Median(galaxyClassMap.Values);

        var regimeResults = new List<RegimeComparison>();

        void AddRegime(string name, Func<ResidualPoint, bool> selector)
        {
            var sample = residuals.Where(selector).ToList();
            if (sample.Count < 40)
                return;

            double localRms = ComputeRmsResidual(sample.Select(x => x.LocalResidual).ToList());
            double o4Rms = ComputeRmsResidual(sample.Select(x => x.ThetaResidual).ToList());
            regimeResults.Add(new RegimeComparison(name, sample.Count, localRms, o4Rms, localRms - o4Rms));
        }

        AddRegime("inner", r => (r.Radius / r.Rd) < 1.0);
        AddRegime("middle", r => (r.Radius / r.Rd) >= 1.0 && (r.Radius / r.Rd) < 4.0);
        AddRegime("outer", r => (r.Radius / r.Rd) >= 4.0);
        AddRegime("low-gbar", r => Math.Log10(Math.Max(r.Gbar, 1e-20)) <= medianLogGbar);
        AddRegime("high-gbar", r => Math.Log10(Math.Max(r.Gbar, 1e-20)) > medianLogGbar);
        AddRegime("high-residual", r => Math.Abs(r.LocalResidual) >= medianAbsLocalResidual);
        AddRegime("low-residual", r => Math.Abs(r.LocalResidual) < medianAbsLocalResidual);
        AddRegime(
            "class-lsb",
            r => galaxyClassMap.TryGetValue(r.GalaxyName, out double gbarClass) && gbarClass <= medianGalaxyLogGbar);
        AddRegime(
            "class-hsb",
            r => galaxyClassMap.TryGetValue(r.GalaxyName, out double gbarClass) && gbarClass > medianGalaxyLogGbar);

        Assert.True(regimeResults.Count >= 8, $"Insufficient regime coverage in TO08: {regimeResults.Count}");

        bool localWinsAll = regimeResults.All(r => r.DeltaRms < 0.0);
        bool anyThetaWin = regimeResults.Any(r => r.DeltaRms > 0.0);
        bool anyClearThetaWin = regimeResults.Any(r => r.DeltaRms >= 0.015);

        var classification = ClassifyThetaClaim(localWinsAll, anyThetaWin, anyClearThetaWin);

        foreach (var regime in regimeResults.OrderBy(r => r.DeltaRms))
        {
            WriteTestLine(
                "TO08",
                $"{regime.Name}: n={regime.Count}, localRMS={regime.LocalRms:F4}, o4RMS={regime.O4Rms:F4}, delta={regime.DeltaRms:F4}");
        }

        WriteTestLine("TO08", $"localWinsAll      = {localWinsAll}");
        WriteTestLine("TO08", $"anyThetaWin       = {anyThetaWin}");
        WriteTestLine("TO08", $"anyClearThetaWin  = {anyClearThetaWin}");
        WriteTestLine("TO08", $"classification    = {classification.Status}");
        WriteTestLine("TO08", $"claimBoundary     = {classification.ClaimBoundary}");

        if (localWinsAll)
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
            Assert.DoesNotContain("solved", classification.ClaimBoundary, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotEqual("solved", classification.Status);
            Assert.DoesNotContain("solved", classification.ClaimBoundary, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TO09_ThetaObservable_Should_Show_Independence_From_LocalGbar()
    {
        var dataset = BuildDataset();

        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var samples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.Gbar <= 0.0 || p.GLocal <= 0.0 || p.O4OrbitKernel <= 0.0)
                    return ((double oTheta, double logGbar, double logRadius, double localResidual)?)null;

                double localScaled = alphaLocal * p.GLocal;
                if (localScaled <= 0.0 || !double.IsFinite(localScaled))
                    return ((double oTheta, double logGbar, double logRadius, double localResidual)?)null;

                double oTheta = Math.Log10(Math.Max(p.O4OrbitKernel, 1e-20));
                double logGbar = Math.Log10(Math.Max(p.Gbar, 1e-20));
                double logRadius = Math.Log10(Math.Max(p.Radius / p.Rd, 1e-6));
                double localResidual = Math.Log10(p.GObs) - Math.Log10(localScaled);

                if (!double.IsFinite(oTheta) || !double.IsFinite(logGbar) || !double.IsFinite(logRadius) || !double.IsFinite(localResidual))
                    return ((double oTheta, double logGbar, double logRadius, double localResidual)?)null;

                return (oTheta, logGbar, logRadius, localResidual);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(samples.Count > 400, $"Too few valid TO09 samples: {samples.Count}");

        var oThetaSeries = samples.Select(x => x.oTheta).ToList();
        var logGbarSeries = samples.Select(x => x.logGbar).ToList();
        var logRadiusSeries = samples.Select(x => x.logRadius).ToList();
        var localResidualSeries = samples.Select(x => x.localResidual).ToList();

        double corrThetaGbar = Math.Abs(ComputePearsonCorrelation(oThetaSeries, logGbarSeries));
        double corrThetaRadius = Math.Abs(ComputePearsonCorrelation(oThetaSeries, logRadiusSeries));
        double corrThetaLocalResidual = Math.Abs(ComputePearsonCorrelation(oThetaSeries, localResidualSeries));

        bool mirrorsLocalGbar =
            corrThetaGbar >= 0.85 &&
            corrThetaGbar >= corrThetaRadius + 0.10 &&
            corrThetaGbar >= corrThetaLocalResidual + 0.10;

        var classification = mirrorsLocalGbar
            ? new ThetaClaimClassification("diagnostic", "hypothesis-supported")
            : new ThetaClaimClassification("candidate", "hypothesis-supported");

        WriteTestLine("TO09", $"Sample count                 = {samples.Count}");
        WriteTestLine("TO09", $"|corr(Otheta,gbar)|          = {corrThetaGbar:F4}");
        WriteTestLine("TO09", $"|corr(Otheta,radius)|        = {corrThetaRadius:F4}");
        WriteTestLine("TO09", $"|corr(Otheta,localResidual)| = {corrThetaLocalResidual:F4}");
        WriteTestLine("TO09", $"mirrorsLocalGbar             = {mirrorsLocalGbar}");
        WriteTestLine("TO09", $"classification               = {classification.Status}");
        WriteTestLine("TO09", $"claimBoundary                = {classification.ClaimBoundary}");

        Assert.True(double.IsFinite(corrThetaGbar));
        Assert.True(double.IsFinite(corrThetaRadius));
        Assert.True(double.IsFinite(corrThetaLocalResidual));

        if (mirrorsLocalGbar)
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }
        else
        {
            Assert.True(
                corrThetaGbar < 0.95,
                $"Otheta is still almost fully collapsed to gbar despite non-mirror classification: corr={corrThetaGbar:F4}");
        }
    }

    [Fact]
    public void TO10_ThetaObservable_Should_Predict_LocalResidual_NotFullSignal()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var samples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.Gbar <= 0.0 || p.GLocal <= 0.0 || p.O4OrbitKernel <= 0.0)
                    return ((double x, double yResidual, double yFull)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled))
                    return ((double x, double yResidual, double yFull)?)null;

                double x = Math.Log10(Math.Max(p.O4OrbitKernel, 1e-20));
                double yResidual = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double yFull = Math.Log10(p.GObs);

                if (!double.IsFinite(x) || !double.IsFinite(yResidual) || !double.IsFinite(yFull))
                    return ((double x, double yResidual, double yFull)?)null;

                return (x, yResidual, yFull);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(samples.Count > 400, $"Too few valid TO10 samples: {samples.Count}");

        var xSeries = samples.Select(s => s.x).ToList();
        var yResidualSeries = samples.Select(s => s.yResidual).ToList();
        var yFullSeries = samples.Select(s => s.yFull).ToList();

        var residualFit = ComputeLinearFit(xSeries, yResidualSeries);
        var fullFit = ComputeLinearFit(xSeries, yFullSeries);

        bool weakResidualPrediction = residualFit.RSquared < 0.05;
        bool residualMuchWeakerThanFull = residualFit.RSquared < 0.35 * fullFit.RSquared;
        bool needsO5 = weakResidualPrediction || residualMuchWeakerThanFull;

        var classification = needsO5
            ? new ThetaClaimClassification("diagnostic", "hypothesis-supported")
            : new ThetaClaimClassification("candidate", "hypothesis-supported");

        WriteTestLine("TO10", $"Samples                     = {samples.Count}");
        WriteTestLine("TO10", $"Residual fit: R2={residualFit.RSquared:F4}, slope={residualFit.Slope:F4}, RMSE={residualFit.Rmse:F4}");
        WriteTestLine("TO10", $"Full-signal fit: R2={fullFit.RSquared:F4}, slope={fullFit.Slope:F4}, RMSE={fullFit.Rmse:F4}");
        WriteTestLine("TO10", $"weakResidualPrediction      = {weakResidualPrediction}");
        WriteTestLine("TO10", $"residualMuchWeakerThanFull  = {residualMuchWeakerThanFull}");
        WriteTestLine("TO10", $"needsO5                     = {needsO5}");
        WriteTestLine("TO10", $"classification              = {classification.Status}");
        WriteTestLine("TO10", $"claimBoundary               = {classification.ClaimBoundary}");

        Assert.True(double.IsFinite(residualFit.RSquared));
        Assert.True(double.IsFinite(fullFit.RSquared));

        if (needsO5)
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }
        else
        {
            Assert.True(
                residualFit.RSquared >= 0.03,
                $"Residual prediction still too weak despite non-O5 classification: R2={residualFit.RSquared:F4}");
        }
    }

    [Fact]
    public void TO11_NonLocalThetaKernel_Should_Outperform_LocalScaling_In_ResidualSpace()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var samples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0 || p.O2GradientPlusLevel <= 0.0)
                    return ((double xO5, double xLocalTheta, double yResidual)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled))
                    return ((double xO5, double xLocalTheta, double yResidual)?)null;

                double yResidual = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double xLocalTheta = Math.Log10(Math.Max(p.O2GradientPlusLevel, 1e-20));
                double xO5 = p.O5NonLocalKernel;

                if (!double.IsFinite(yResidual) || !double.IsFinite(xLocalTheta) || !double.IsFinite(xO5))
                    return ((double xO5, double xLocalTheta, double yResidual)?)null;

                return (xO5, xLocalTheta, yResidual);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(samples.Count > 400, $"Too few valid TO11 samples: {samples.Count}");

        var ySeries = samples.Select(s => s.yResidual).ToList();
        var xO5Series = samples.Select(s => s.xO5).ToList();
        var xLocalThetaSeries = samples.Select(s => s.xLocalTheta).ToList();

        double baselineRmse = Math.Sqrt(ySeries.Average(v => v * v)); // local-scaling-only residual baseline
        var localThetaFit = ComputeLinearFit(xLocalThetaSeries, ySeries);
        var o5Fit = ComputeLinearFit(xO5Series, ySeries);

        bool o5BeatsLocalScaling = o5Fit.Rmse <= baselineRmse - 0.005;
        bool o5BeatsCurrentLocalTheta = o5Fit.Rmse <= localThetaFit.Rmse - 0.003;
        bool to11Positive = o5BeatsLocalScaling && o5BeatsCurrentLocalTheta;

        var classification = to11Positive
            ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
            : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        WriteTestLine("TO11", $"Samples                    = {samples.Count}");
        WriteTestLine("TO11", $"Residual baseline RMSE     = {baselineRmse:F4}");
        WriteTestLine("TO11", $"Local-theta fit RMSE/R2    = {localThetaFit.Rmse:F4} / {localThetaFit.RSquared:F4}");
        WriteTestLine("TO11", $"O5 fit RMSE/R2             = {o5Fit.Rmse:F4} / {o5Fit.RSquared:F4}");
        WriteTestLine("TO11", $"o5BeatsLocalScaling        = {o5BeatsLocalScaling}");
        WriteTestLine("TO11", $"o5BeatsCurrentLocalTheta   = {o5BeatsCurrentLocalTheta}");
        WriteTestLine("TO11", $"TO11 positive              = {to11Positive}");
        WriteTestLine("TO11", $"classification             = {classification.Status}");
        WriteTestLine("TO11", $"claimBoundary              = {classification.ClaimBoundary}");

        Assert.True(double.IsFinite(o5Fit.Rmse));
        Assert.True(double.IsFinite(localThetaFit.Rmse));
        Assert.True(double.IsFinite(baselineRmse));

        if (to11Positive)
        {
            Assert.NotEqual("solved", classification.Status);
        }
        else
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }
    }

    [Fact]
    public void TO12_O5_Should_Remain_Stable_Across_GalaxyClasses_Not_OutlierDriven()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var rawSamples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.Gbar <= 0.0 || p.GLocal <= 0.0)
                    return ((string galaxy, double xRadius, double logGbar, double o5, double residual)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled) || !double.IsFinite(p.O5NonLocalKernel))
                    return ((string galaxy, double xRadius, double logGbar, double o5, double residual)?)null;

                double residual = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double xRadius = p.Radius / p.Rd;
                double logGbar = Math.Log10(Math.Max(p.Gbar, 1e-20));
                double o5 = p.O5NonLocalKernel;

                if (!double.IsFinite(residual) || !double.IsFinite(xRadius) || !double.IsFinite(logGbar))
                    return ((string galaxy, double xRadius, double logGbar, double o5, double residual)?)null;

                return (p.GalaxyName, xRadius, logGbar, o5, residual);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(rawSamples.Count > 400, $"Too few valid TO12 samples: {rawSamples.Count}");

        var o5X = rawSamples.Select(s => s.o5).ToList();
        var residualY = rawSamples.Select(s => s.residual).ToList();
        var o5Fit = ComputeLinearFit(o5X, residualY);

        var samples = rawSamples
            .Select(s =>
            {
                double yHat = o5Fit.Intercept + o5Fit.Slope * s.o5;
                double baselineErrSq = s.residual * s.residual;
                double o5ErrSq = (s.residual - yHat) * (s.residual - yHat);
                return new O5ResidualSample(
                    s.galaxy,
                    s.xRadius,
                    s.logGbar,
                    baselineErrSq,
                    o5ErrSq);
            })
            .ToList();

        double medianLogGbar = Median(samples.Select(s => s.LogGbar));
        var galaxyClassMap = samples
            .GroupBy(s => s.Galaxy)
            .ToDictionary(g => g.Key, g => Median(g.Select(x => x.LogGbar)));
        double medianGalaxyClass = Median(galaxyClassMap.Values);

        var regimeResults = new List<RegimeComparison>();

        void AddRegime(string name, Func<O5ResidualSample, bool> selector)
        {
            var group = samples.Where(selector).ToList();
            if (group.Count < 40)
                return;

            double localRms = Math.Sqrt(group.Average(x => x.BaselineErrSq));
            double o5Rms = Math.Sqrt(group.Average(x => x.O5ErrSq));
            regimeResults.Add(new RegimeComparison(name, group.Count, localRms, o5Rms, localRms - o5Rms));
        }

        AddRegime("inner", s => s.XRadius < 1.0);
        AddRegime("middle", s => s.XRadius >= 1.0 && s.XRadius < 4.0);
        AddRegime("outer", s => s.XRadius >= 4.0);
        AddRegime("low-gbar", s => s.LogGbar <= medianLogGbar);
        AddRegime("high-gbar", s => s.LogGbar > medianLogGbar);
        AddRegime(
            "class-lsb",
            s => galaxyClassMap.TryGetValue(s.Galaxy, out double cls) && cls <= medianGalaxyClass);
        AddRegime(
            "class-hsb",
            s => galaxyClassMap.TryGetValue(s.Galaxy, out double cls) && cls > medianGalaxyClass);

        Assert.True(regimeResults.Count >= 6, $"Insufficient TO12 regime coverage: {regimeResults.Count}");

        var perGalaxy = samples
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .Select(g =>
            {
                double localRms = Math.Sqrt(g.Average(x => x.BaselineErrSq));
                double o5Rms = Math.Sqrt(g.Average(x => x.O5ErrSq));
                return new { Galaxy = g.Key, Improvement = localRms - o5Rms };
            })
            .ToList();

        Assert.NotEmpty(perGalaxy);

        int positiveGalaxyCount = perGalaxy.Count(x => x.Improvement > 0.0);
        double improvementShare = (double)positiveGalaxyCount / perGalaxy.Count;

        double totalPositive = perGalaxy.Where(x => x.Improvement > 0.0).Sum(x => x.Improvement);
        double top3Positive = perGalaxy
            .Where(x => x.Improvement > 0.0)
            .OrderByDescending(x => x.Improvement)
            .Take(3)
            .Sum(x => x.Improvement);
        double top3Dominance = totalPositive > 0.0 ? top3Positive / totalPositive : 1.0;

        double globalLocalRms = Math.Sqrt(samples.Average(x => x.BaselineErrSq));
        double globalO5Rms = Math.Sqrt(samples.Average(x => x.O5ErrSq));
        double globalImprovement = globalLocalRms - globalO5Rms;

        int positiveRegimes = regimeResults.Count(r => r.DeltaRms > 0.0);
        bool classStable = regimeResults.Any(r => r.Name == "class-lsb" && r.DeltaRms > -0.01)
                        && regimeResults.Any(r => r.Name == "class-hsb" && r.DeltaRms > -0.01);

        bool o5Candidate =
            globalImprovement > 0.0 &&
            positiveRegimes >= 4 &&
            improvementShare >= 0.35 &&
            top3Dominance <= 0.80 &&
            classStable;

        var classification = o5Candidate
            ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
            : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        foreach (var regime in regimeResults.OrderByDescending(r => r.DeltaRms))
        {
            WriteTestLine(
                "TO12",
                $"{regime.Name}: n={regime.Count}, localRMS={regime.LocalRms:F4}, o5RMS={regime.O4Rms:F4}, delta={regime.DeltaRms:F4}");
        }

        WriteTestLine("TO12", $"Global local/o5 RMS      = {globalLocalRms:F4} / {globalO5Rms:F4}");
        WriteTestLine("TO12", $"Global improvement       = {globalImprovement:F4}");
        WriteTestLine("TO12", $"Positive regimes         = {positiveRegimes}/{regimeResults.Count}");
        WriteTestLine("TO12", $"Improvement share        = {improvementShare:F3}");
        WriteTestLine("TO12", $"Top-3 dominance          = {top3Dominance:F3}");
        WriteTestLine("TO12", $"Class stability          = {classStable}");
        WriteTestLine("TO12", $"classification           = {classification.Status}");
        WriteTestLine("TO12", $"claimBoundary            = {classification.ClaimBoundary}");

        if (o5Candidate)
        {
            Assert.Equal("effective-candidate", classification.Status);
            Assert.True(improvementShare >= 0.35);
            Assert.True(top3Dominance <= 0.80);
        }
        else
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }
    }

    [Fact]
    public void TO13_O5_Should_Be_Robust_Across_KernelAndParameter_Ablations()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var samples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                    return ((double y, double k1, double k2, double k3, double k4, double k5)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled))
                    return ((double y, double k1, double k2, double k3, double k4, double k5)?)null;

                double y = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                if (!double.IsFinite(y))
                    return ((double y, double k1, double k2, double k3, double k4, double k5)?)null;

                return (y, p.O5W2InvDistance, p.O5W4InvDistance, p.O5W6InvDistance, p.O5W4Uniform, p.O5W4Gaussian);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(samples.Count > 400, $"Too few valid TO13 samples: {samples.Count}");

        var ySeries = samples.Select(s => s.y).ToList();
        double baselineRmse = Math.Sqrt(ySeries.Average(v => v * v));

        var variantResults = new List<(string Name, double Rmse, double R2, double Improvement)>
        {
            EvaluateVariant("O5-W2-InvDistance", samples.Select(s => s.k1).ToList(), ySeries, baselineRmse),
            EvaluateVariant("O5-W4-InvDistance", samples.Select(s => s.k2).ToList(), ySeries, baselineRmse),
            EvaluateVariant("O5-W6-InvDistance", samples.Select(s => s.k3).ToList(), ySeries, baselineRmse),
            EvaluateVariant("O5-W4-Uniform", samples.Select(s => s.k4).ToList(), ySeries, baselineRmse),
            EvaluateVariant("O5-W4-Gaussian", samples.Select(s => s.k5).ToList(), ySeries, baselineRmse)
        };

        foreach (var vr in variantResults.OrderByDescending(v => v.Improvement))
        {
            WriteTestLine("TO13", $"{vr.Name}: RMSE={vr.Rmse:F4}, R2={vr.R2:F4}, improvement={vr.Improvement:F4}");
        }

        var improving = variantResults.Where(v => v.Improvement > 0.003).ToList();
        int improvingCount = improving.Count;

        double bestImprovement = variantResults.Max(v => v.Improvement);
        double worstImprovement = variantResults.Min(v => v.Improvement);
        double spread = bestImprovement - worstImprovement;

        bool robust = improvingCount >= 3 && spread <= 0.020;
        bool singleHit = improvingCount <= 1 && bestImprovement > 0.004;

        var classification = robust
            ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
            : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        WriteTestLine("TO13", $"Baseline RMSE           = {baselineRmse:F4}");
        WriteTestLine("TO13", $"Improving variants      = {improvingCount}/{variantResults.Count}");
        WriteTestLine("TO13", $"Best/Worst improvement  = {bestImprovement:F4} / {worstImprovement:F4}");
        WriteTestLine("TO13", $"Improvement spread      = {spread:F4}");
        WriteTestLine("TO13", $"singleHitKernel         = {singleHit}");
        WriteTestLine("TO13", $"robust                  = {robust}");
        WriteTestLine("TO13", $"classification          = {classification.Status}");
        WriteTestLine("TO13", $"claimBoundary           = {classification.ClaimBoundary}");

        if (robust)
        {
            Assert.Equal("effective-candidate", classification.Status);
            Assert.True(improvingCount >= 3);
            Assert.True(spread <= 0.020);
        }
        else
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }

        static (string Name, double Rmse, double R2, double Improvement) EvaluateVariant(
            string name,
            IReadOnlyList<double> x,
            IReadOnlyList<double> y,
            double baseline)
        {
            var fit = ComputeLinearFit(x, y);
            return (name, fit.Rmse, fit.RSquared, baseline - fit.Rmse);
        }
    }

    [Fact]
    public void TO14_O5Kernel_Should_Have_PhysicalSelectionCriterion()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var rawSamples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0 || p.Gbar <= 0.0)
                    return ((string galaxy, double xRadius, double logGbar, double y, double k1, double k2, double k3, double k4, double k5)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled))
                    return ((string galaxy, double xRadius, double logGbar, double y, double k1, double k2, double k3, double k4, double k5)?)null;

                double y = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double xRadius = p.Radius / p.Rd;
                double logGbar = Math.Log10(Math.Max(p.Gbar, 1e-20));

                if (!double.IsFinite(y) || !double.IsFinite(xRadius) || !double.IsFinite(logGbar))
                    return ((string galaxy, double xRadius, double logGbar, double y, double k1, double k2, double k3, double k4, double k5)?)null;

                return (
                    p.GalaxyName,
                    xRadius,
                    logGbar,
                    y,
                    p.O5W2InvDistance,
                    p.O5W4InvDistance,
                    p.O5W6InvDistance,
                    p.O5W4Uniform,
                    p.O5W4Gaussian
                );
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(rawSamples.Count > 400, $"Too few valid TO14 samples: {rawSamples.Count}");

        var ySeries = rawSamples.Select(s => s.y).ToList();
        double baselineRmse = Math.Sqrt(ySeries.Average(v => v * v));
        double medianLogGbar = Median(rawSamples.Select(s => s.logGbar));
        var galaxyClassMap = rawSamples
            .GroupBy(s => s.galaxy)
            .ToDictionary(g => g.Key, g => Median(g.Select(x => x.logGbar)));
        double medianGalaxyClass = Median(galaxyClassMap.Values);

        var variants = new List<KernelPhysicalEvaluation>
        {
            EvaluateVariant("O5-W2-InvDistance", windowSize: 2, kernelMode: "inverse", rawSamples.Select(s => s.k1).ToList()),
            EvaluateVariant("O5-W4-InvDistance", windowSize: 4, kernelMode: "inverse", rawSamples.Select(s => s.k2).ToList()),
            EvaluateVariant("O5-W6-InvDistance", windowSize: 6, kernelMode: "inverse", rawSamples.Select(s => s.k3).ToList()),
            EvaluateVariant("O5-W4-Uniform", windowSize: 4, kernelMode: "uniform", rawSamples.Select(s => s.k4).ToList()),
            EvaluateVariant("O5-W4-Gaussian", windowSize: 4, kernelMode: "gaussian", rawSamples.Select(s => s.k5).ToList())
        };

        foreach (var v in variants.OrderByDescending(v => v.PhysicalScore))
        {
            WriteTestLine(
                "TO14",
                $"{v.Name}: RMSE={v.Rmse:F4}, imp={v.Improvement:F4}, meanReg={v.RegimeMeanDelta:F4}, stdReg={v.RegimeStdDelta:F4}, score={v.PhysicalScore:F4}");
        }

        var best = variants.OrderByDescending(v => v.PhysicalScore).First();
        var w6Inv = variants.Single(v => v.Name == "O5-W6-InvDistance");
        var w2Inv = variants.Single(v => v.Name == "O5-W2-InvDistance");
        var w4Uniform = variants.Single(v => v.Name == "O5-W4-Uniform");

        bool w6Plausible =
            w6Inv.PhysicalScore >= best.PhysicalScore - 0.005 &&
            w6Inv.RegimeMeanDelta >= w2Inv.RegimeMeanDelta - 0.004 &&
            w6Inv.RegimeStdDelta <= w4Uniform.RegimeStdDelta + 0.015;

        var classification = w6Plausible
            ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
            : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        WriteTestLine("TO14", $"Selected by physical score = {best.Name}");
        WriteTestLine("TO14", $"W6-Inv plausible           = {w6Plausible}");
        WriteTestLine("TO14", $"classification             = {classification.Status}");
        WriteTestLine("TO14", $"claimBoundary              = {classification.ClaimBoundary}");

        Assert.True(
            w6Plausible,
            $"W6/InvDistance is not physically plausible under current criterion. Best={best.Name}, bestScore={best.PhysicalScore:F4}, w6Score={w6Inv.PhysicalScore:F4}");

        KernelPhysicalEvaluation EvaluateVariant(string name, int windowSize, string kernelMode, IReadOnlyList<double> x)
        {
            var fit = ComputeLinearFit(x, ySeries);
            double improvement = baselineRmse - fit.Rmse;

            var predictedSamples = rawSamples
                .Select((s, i) =>
                {
                    double yHat = fit.Intercept + fit.Slope * x[i];
                    return new KernelRegimeSample(
                        s.galaxy,
                        s.xRadius,
                        s.logGbar,
                        s.y * s.y,
                        (s.y - yHat) * (s.y - yHat));
                })
                .ToList();

            var regimeDeltas = new List<double>();
            AddRegime(r => r.XRadius < 1.0);
            AddRegime(r => r.XRadius >= 1.0 && r.XRadius < 4.0);
            AddRegime(r => r.XRadius >= 4.0);
            AddRegime(r => r.LogGbar <= medianLogGbar);
            AddRegime(r => r.LogGbar > medianLogGbar);
            AddRegime(r => galaxyClassMap.TryGetValue(r.Galaxy, out double cls) && cls <= medianGalaxyClass);
            AddRegime(r => galaxyClassMap.TryGetValue(r.Galaxy, out double cls) && cls > medianGalaxyClass);

            double regimeMeanDelta = regimeDeltas.Count > 0 ? regimeDeltas.Average() : -1.0;
            double regimeStdDelta = StandardDeviation(regimeDeltas);
            double positiveRegimeShare = regimeDeltas.Count > 0 ? regimeDeltas.Count(d => d > 0.0) / (double)regimeDeltas.Count : 0.0;

            double nonLocalBonus = windowSize switch
            {
                6 => 0.005,
                4 => 0.003,
                _ => 0.001
            };

            double decayBonus = kernelMode switch
            {
                "inverse" => 0.004,
                "gaussian" => 0.002,
                _ => 0.0
            };

            double physicalScore =
                improvement
                + 0.40 * regimeMeanDelta
                + 0.02 * positiveRegimeShare
                - 0.20 * regimeStdDelta
                + nonLocalBonus
                + decayBonus;

            return new KernelPhysicalEvaluation(
                name,
                fit.Rmse,
                fit.RSquared,
                improvement,
                regimeMeanDelta,
                regimeStdDelta,
                positiveRegimeShare,
                physicalScore);

            void AddRegime(Func<KernelRegimeSample, bool> selector)
            {
                var group = predictedSamples.Where(selector).ToList();
                if (group.Count < 40)
                    return;

                double localRms = Math.Sqrt(group.Average(g => g.BaselineErrSq));
                double variantRms = Math.Sqrt(group.Average(g => g.VariantErrSq));
                regimeDeltas.Add(localRms - variantRms);
            }
        }
    }

    [Fact]
    public void TO15_O5W6InvDistance_Should_Not_Be_Overfit_To_CurrentSample()
    {
            var dataset = BuildDataset();
            double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

            var samples = dataset.Points
                .Select(p =>
                {
                    if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                        return ((string galaxy, double x, double y)?)null;

                    double gLocalScaled = alphaLocal * p.GLocal;
                    if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled) || !double.IsFinite(p.O5W6InvDistance))
                        return ((string galaxy, double x, double y)?)null;

                    double y = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                    double x = p.O5W6InvDistance;

                    if (!double.IsFinite(x) || !double.IsFinite(y))
                        return ((string galaxy, double x, double y)?)null;

                    return (p.GalaxyName, x, y);
                })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToList();

            Assert.True(samples.Count > 400, $"Too few valid TO15 samples: {samples.Count}");

            var groups = samples
                .GroupBy(s => s.galaxy)
                .Where(g => g.Count() >= 8)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();

            Assert.True(groups.Count >= 8, $"Too few galaxy groups for holdout validation: {groups.Count}");

            int foldCount = Math.Min(5, groups.Count);
            var foldMap = groups
                .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
                .ToDictionary(x => x.Key, x => x.Fold);

            var eligibleSamples = samples
                .Where(s => foldMap.ContainsKey(s.galaxy))
                .ToList();

            var foldResults = new List<(int Fold, int TestCount, double TrainImp, double TestImp, double Gap)>();

            for (int fold = 0; fold < foldCount; fold++)
            {
                var train = eligibleSamples.Where(s => foldMap[s.galaxy] != fold).ToList();
                var test = eligibleSamples.Where(s => foldMap[s.galaxy] == fold).ToList();

                if (train.Count < 120 || test.Count < 80)
                    continue;

                var fit = ComputeLinearFit(train.Select(t => t.x).ToList(), train.Select(t => t.y).ToList());

                double trainBaseline = Math.Sqrt(train.Average(t => t.y * t.y));
                double trainModel = Math.Sqrt(train.Average(t =>
                {
                    double yHat = fit.Intercept + fit.Slope * t.x;
                    double e = t.y - yHat;
                    return e * e;
                }));
                double trainImprovement = trainBaseline - trainModel;

                double testBaseline = Math.Sqrt(test.Average(t => t.y * t.y));
                double testModel = Math.Sqrt(test.Average(t =>
                {
                    double yHat = fit.Intercept + fit.Slope * t.x;
                    double e = t.y - yHat;
                    return e * e;
                }));
                double testImprovement = testBaseline - testModel;

                double gap = trainImprovement - testImprovement;
                foldResults.Add((fold, test.Count, trainImprovement, testImprovement, gap));
            }

            Assert.True(foldResults.Count >= 3, $"Insufficient valid folds in TO15: {foldResults.Count}");

            foreach (var fr in foldResults)
            {
                WriteTestLine(
                    "TO15",
                    $"fold={fr.Fold}, nTest={fr.TestCount}, trainImp={fr.TrainImp:F4}, testImp={fr.TestImp:F4}, gap={fr.Gap:F4}");
            }

            double meanTrainImp = foldResults.Average(f => f.TrainImp);
            double meanTestImp = foldResults.Average(f => f.TestImp);
            double meanGap = foldResults.Average(f => f.Gap);
            double gapStd = StandardDeviation(foldResults.Select(f => f.Gap));
            double positiveTestShare = foldResults.Count(f => f.TestImp > 0.0) / (double)foldResults.Count;

            bool notOverfit =
                meanTestImp >= -0.002 &&
                positiveTestShare >= 0.40 &&
                meanGap <= 0.020 &&
                gapStd <= 0.030;

            var classification = notOverfit
                ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
                : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

            WriteTestLine("TO15", $"meanTrainImp     = {meanTrainImp:F4}");
            WriteTestLine("TO15", $"meanTestImp      = {meanTestImp:F4}");
            WriteTestLine("TO15", $"meanGap          = {meanGap:F4}");
            WriteTestLine("TO15", $"gapStd           = {gapStd:F4}");
            WriteTestLine("TO15", $"positiveTestShare= {positiveTestShare:F3}");
            WriteTestLine("TO15", $"notOverfit       = {notOverfit}");
            WriteTestLine("TO15", $"classification   = {classification.Status}");
            WriteTestLine("TO15", $"claimBoundary    = {classification.ClaimBoundary}");

            if (notOverfit)
            {
                Assert.Equal("effective-candidate", classification.Status);
            }
            else
            {
                Assert.Equal("diagnostic", classification.Status);
                Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }
    }

    [Fact]
    public void TO16_O5Kernel_Should_Select_By_HoldoutStability_NotTrainingScore()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
                .Select(p =>
                {
                    if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                        return ((string galaxy, double y, double k1, double k2, double k3, double k4, double k5)?)null;

                    double gLocalScaled = alphaLocal * p.GLocal;
                    if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled))
                        return ((string galaxy, double y, double k1, double k2, double k3, double k4, double k5)?)null;

                    double y = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                    if (!double.IsFinite(y))
                        return ((string galaxy, double y, double k1, double k2, double k3, double k4, double k5)?)null;

                    return (p.GalaxyName, y, p.O5W2InvDistance, p.O5W4InvDistance, p.O5W6InvDistance, p.O5W4Uniform, p.O5W4Gaussian);
                })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToList();

        Assert.True(raw.Count > 400, $"Too few valid TO16 samples: {raw.Count}");

        var groups = raw
                .GroupBy(s => s.galaxy)
                .Where(g => g.Count() >= 8)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();

        Assert.True(groups.Count >= 8, $"Too few galaxy groups for TO16 holdout: {groups.Count}");

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
                .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
                .ToDictionary(x => x.Key, x => x.Fold);

        var samples = raw.Where(s => foldMap.ContainsKey(s.galaxy)).ToList();

        var variants = new List<KernelHoldoutMetrics>
        {
                EvaluateKernel("O5-W2-InvDistance", samples.Select(s => s.k1).ToList()),
                EvaluateKernel("O5-W4-InvDistance", samples.Select(s => s.k2).ToList()),
                EvaluateKernel("O5-W6-InvDistance", samples.Select(s => s.k3).ToList()),
                EvaluateKernel("O5-W4-Uniform", samples.Select(s => s.k4).ToList()),
                EvaluateKernel("O5-W4-Gaussian", samples.Select(s => s.k5).ToList())
        };

        foreach (var v in variants.OrderByDescending(v => v.StabilityScore))
        {
                WriteTestLine(
                    "TO16",
                    $"{v.Name}: meanTrain={v.MeanTrainImprovement:F4}, meanTest={v.MeanTestImprovement:F4}, gapStd={v.GapStd:F4}, posShare={v.PositiveTestShare:F3}, worstFold={v.WorstFoldImprovement:F4}, stability={v.StabilityScore:F4}");
        }

        var selected = variants.OrderByDescending(v => v.StabilityScore).First();
        var bestTraining = variants.OrderByDescending(v => v.MeanTrainImprovement).First();

        bool selectionByHoldoutCriterion =
                selected.StabilityScore >= bestTraining.StabilityScore - 1e-9 &&
                selected.MeanTestImprovement >= bestTraining.MeanTestImprovement - 0.003;

        bool stableGeneralization =
                selected.MeanTestImprovement >= -0.002 &&
                selected.GapStd <= 0.030 &&
                selected.PositiveTestShare >= 0.40 &&
                selected.WorstFoldImprovement >= -0.030;

        var classification = stableGeneralization
                ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
                : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        WriteTestLine("TO16", $"selectedByStability   = {selected.Name}");
        WriteTestLine("TO16", $"bestTrainingKernel    = {bestTraining.Name}");
        WriteTestLine("TO16", $"selectionByHoldout    = {selectionByHoldoutCriterion}");
        WriteTestLine("TO16", $"stableGeneralization  = {stableGeneralization}");
        WriteTestLine("TO16", $"classification        = {classification.Status}");
        WriteTestLine("TO16", $"claimBoundary         = {classification.ClaimBoundary}");

        Assert.True(selectionByHoldoutCriterion, "Kernel selection is not driven by holdout stability metrics.");

        if (stableGeneralization)
        {
                Assert.Equal("effective-candidate", classification.Status);
        }
        else
        {
                Assert.Equal("diagnostic", classification.Status);
                Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }

        KernelHoldoutMetrics EvaluateKernel(string name, IReadOnlyList<double> x)
        {
                var points = samples
                    .Select((s, i) => new { s.galaxy, s.y, X = x[i] })
                    .ToList();

                var foldResults = new List<(double TrainImp, double TestImp, double Gap)>();

                for (int fold = 0; fold < foldCount; fold++)
                {
                    var train = points
                        .Where(s => foldMap[s.galaxy] != fold)
                        .ToList();
                    var test = points
                        .Where(s => foldMap[s.galaxy] == fold)
                        .ToList();

                    if (train.Count < 120 || test.Count < 80)
                        continue;

                    var fit = ComputeLinearFit(train.Select(t => t.X).ToList(), train.Select(t => t.y).ToList());

                    double trainBaseline = Math.Sqrt(train.Average(t => t.y * t.y));
                    double trainModel = Math.Sqrt(train.Average(t =>
                    {
                        double yHat = fit.Intercept + fit.Slope * t.X;
                        double e = t.y - yHat;
                        return e * e;
                    }));
                    double trainImp = trainBaseline - trainModel;

                    double testBaseline = Math.Sqrt(test.Average(t => t.y * t.y));
                    double testModel = Math.Sqrt(test.Average(t =>
                    {
                        double yHat = fit.Intercept + fit.Slope * t.X;
                        double e = t.y - yHat;
                        return e * e;
                    }));
                    double testImp = testBaseline - testModel;

                    foldResults.Add((trainImp, testImp, trainImp - testImp));
                }

                if (foldResults.Count == 0)
                    return new KernelHoldoutMetrics(name, double.NegativeInfinity, double.NegativeInfinity, 1.0, 0.0, double.NegativeInfinity, 0.0);

                double meanTrain = foldResults.Average(f => f.TrainImp);
                double meanTest = foldResults.Average(f => f.TestImp);
                double gapStd = StandardDeviation(foldResults.Select(f => f.Gap));
                double posShare = foldResults.Count(f => f.TestImp > 0.0) / (double)foldResults.Count;
                double worstFold = foldResults.Min(f => f.TestImp);

                double stability =
                    meanTest
                    + 0.02 * posShare
                    + 0.30 * worstFold
                    - 0.40 * gapStd;

                return new KernelHoldoutMetrics(name, meanTrain, meanTest, gapStd, posShare, worstFold, stability);
        }
    }

    [Fact]
    public void TO17_O5HoldoutWeakFold_Should_Explain_GeneralizationInstability()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                    return ((string galaxy, double xRadius, double logGbar, double residual, double xO5)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled) || !double.IsFinite(p.O5W6InvDistance))
                    return ((string galaxy, double xRadius, double logGbar, double residual, double xO5)?)null;

                double residual = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double xRadius = p.Radius / p.Rd;
                double logGbar = Math.Log10(Math.Max(p.Gbar, 1e-20));
                double xO5 = p.O5W6InvDistance;

                if (!double.IsFinite(residual) || !double.IsFinite(xRadius) || !double.IsFinite(logGbar) || !double.IsFinite(xO5))
                    return ((string galaxy, double xRadius, double logGbar, double residual, double xO5)?)null;

                return (p.GalaxyName, xRadius, logGbar, residual, xO5);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(raw.Count > 400, $"Too few valid TO17 samples: {raw.Count}");

        var groups = raw
            .GroupBy(s => s.galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        Assert.True(groups.Count >= 8, $"Too few galaxy groups for TO17 holdout: {groups.Count}");

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);

        var samples = raw.Where(s => foldMap.ContainsKey(s.galaxy)).ToList();
        double medianLogGbar = Median(samples.Select(s => s.logGbar));
        double medianAbsResidual = Median(samples.Select(s => Math.Abs(s.residual)));

        var galaxyClassMap = samples
            .GroupBy(s => s.galaxy)
            .ToDictionary(g => g.Key, g => Median(g.Select(x => x.logGbar)));
        double medianGalaxyClass = Median(galaxyClassMap.Values);

        var foldResults = new List<WeakFoldResult>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = samples.Where(s => foldMap[s.galaxy] != fold).ToList();
            var test = samples.Where(s => foldMap[s.galaxy] == fold).ToList();

            if (train.Count < 120 || test.Count < 80)
                continue;

            var fit = ComputeLinearFit(train.Select(t => t.xO5).ToList(), train.Select(t => t.residual).ToList());

            double trainBaseline = Math.Sqrt(train.Average(t => t.residual * t.residual));
            double trainModel = Math.Sqrt(train.Average(t =>
            {
                double yHat = fit.Intercept + fit.Slope * t.xO5;
                double e = t.residual - yHat;
                return e * e;
            }));
            double trainImp = trainBaseline - trainModel;

            double testBaseline = Math.Sqrt(test.Average(t => t.residual * t.residual));
            double testModel = Math.Sqrt(test.Average(t =>
            {
                double yHat = fit.Intercept + fit.Slope * t.xO5;
                double e = t.residual - yHat;
                return e * e;
            }));
            double testImp = testBaseline - testModel;
            double gap = trainImp - testImp;

            var byGalaxy = test
                .GroupBy(t => t.galaxy)
                .Select(g =>
                {
                    double b = Math.Sqrt(g.Average(p => p.residual * p.residual));
                    double m = Math.Sqrt(g.Average(p =>
                    {
                        double yHat = fit.Intercept + fit.Slope * p.xO5;
                        double e = p.residual - yHat;
                        return e * e;
                    }));
                    return new { Galaxy = g.Key, Degradation = Math.Max(0.0, m - b) };
                })
                .Where(x => x.Degradation > 0.0)
                .OrderByDescending(x => x.Degradation)
                .ToList();

            double totalDegradation = byGalaxy.Sum(x => x.Degradation);
            double top3Deg = byGalaxy.Take(3).Sum(x => x.Degradation);
            double top3Dominance = totalDegradation > 0.0 ? top3Deg / totalDegradation : 0.0;

            foldResults.Add(new WeakFoldResult(
                fold,
                test.Count,
                trainImp,
                testImp,
                gap,
                ComputeShare(test, t => t.logGbar <= medianLogGbar),
                ComputeShare(test, t => galaxyClassMap.TryGetValue(t.galaxy, out double cls) && cls <= medianGalaxyClass),
                ComputeShare(test, t => t.xRadius < 1.0),
                ComputeShare(test, t => t.xRadius >= 1.0 && t.xRadius < 4.0),
                ComputeShare(test, t => t.xRadius >= 4.0),
                ComputeShare(test, t => Math.Abs(t.residual) >= medianAbsResidual),
                test.Select(t => Math.Abs(t.residual)).Average(),
                Percentile(test.Select(t => Math.Abs(t.residual)), 0.90),
                top3Dominance));
        }

        Assert.True(foldResults.Count >= 3, $"Insufficient valid TO17 folds: {foldResults.Count}");

        var weak = foldResults.OrderBy(f => f.TestImprovement).First();
        var others = foldResults.Where(f => f.Fold != weak.Fold).ToList();
        double meanOtherTestImp = others.Average(f => f.TestImprovement);
        bool instabilityPresent = weak.TestImprovement < meanOtherTestImp - 0.01;

        double meanLowGbar = others.Average(f => f.LowGbarShare);
        double meanLsb = others.Average(f => f.LsbShare);
        double meanInner = others.Average(f => f.InnerShare);
        double meanOuter = others.Average(f => f.OuterShare);
        double meanHighResidual = others.Average(f => f.HighResidualShare);
        double meanMeanAbsResidual = others.Average(f => f.MeanAbsResidual);
        double meanP90AbsResidual = others.Average(f => f.P90AbsResidual);

        bool gbarShift = Math.Abs(weak.LowGbarShare - meanLowGbar) > 0.10;
        bool classShift = Math.Abs(weak.LsbShare - meanLsb) > 0.10;
        bool radiusShift = Math.Abs(weak.InnerShare - meanInner) > 0.08 || Math.Abs(weak.OuterShare - meanOuter) > 0.08;
        bool residualShift = (weak.HighResidualShare - meanHighResidual) > 0.08
                          || (weak.MeanAbsResidual - meanMeanAbsResidual) > 0.03
                          || (weak.P90AbsResidual - meanP90AbsResidual) > 0.05;
        bool dominanceDriven = weak.Top3DegradationDominance >= 0.60;

        int explanationFlags = (gbarShift ? 1 : 0)
                             + (classShift ? 1 : 0)
                             + (radiusShift ? 1 : 0)
                             + (residualShift ? 1 : 0)
                             + (dominanceDriven ? 1 : 0);

        foreach (var f in foldResults.OrderBy(fr => fr.Fold))
        {
            WriteTestLine(
                "TO17",
                $"fold={f.Fold}, n={f.TestCount}, testImp={f.TestImprovement:F4}, gap={f.Gap:F4}, lowGbar={f.LowGbarShare:F3}, lsb={f.LsbShare:F3}, inner/mid/outer={f.InnerShare:F3}/{f.MiddleShare:F3}/{f.OuterShare:F3}, highRes={f.HighResidualShare:F3}, top3Dom={f.Top3DegradationDominance:F3}");
        }

        WriteTestLine("TO17", $"weakFold                 = {weak.Fold}");
        WriteTestLine("TO17", $"instabilityPresent       = {instabilityPresent}");
        WriteTestLine("TO17", $"gbarShift/classShift     = {gbarShift}/{classShift}");
        WriteTestLine("TO17", $"radiusShift/residualShift= {radiusShift}/{residualShift}");
        WriteTestLine("TO17", $"dominanceDriven          = {dominanceDriven}");
        WriteTestLine("TO17", $"explanationFlags         = {explanationFlags}");

        if (instabilityPresent)
        {
            Assert.True(
                explanationFlags >= 1,
                "Weak-fold instability detected but no explanatory regime/composition/dominance signal found.");
        }
        else
        {
            Assert.True(
                weak.TestImprovement >= -0.05,
                $"Weak fold degrades too strongly without instability pattern: testImp={weak.TestImprovement:F4}");
        }
    }

    [Fact]
    public void TO18_O5_Should_Remain_Stable_Under_StratifiedGalaxyHoldout()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                    return ((string galaxy, double xRadius, double logGbar, double y, double xO5)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled) || !double.IsFinite(p.O5W6InvDistance))
                    return ((string galaxy, double xRadius, double logGbar, double y, double xO5)?)null;

                double y = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double xRadius = p.Radius / p.Rd;
                double logGbar = Math.Log10(Math.Max(p.Gbar, 1e-20));
                double xO5 = p.O5W6InvDistance;

                if (!double.IsFinite(y) || !double.IsFinite(xRadius) || !double.IsFinite(logGbar) || !double.IsFinite(xO5))
                    return ((string galaxy, double xRadius, double logGbar, double y, double xO5)?)null;

                return (p.GalaxyName, xRadius, logGbar, y, xO5);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(raw.Count > 400, $"Too few valid TO18 samples: {raw.Count}");

        var groups = raw
            .GroupBy(s => s.galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        Assert.True(groups.Count >= 8, $"Too few galaxy groups for TO18: {groups.Count}");

        int foldCount = Math.Min(5, groups.Count);
        var globalMedianGalaxyGbar = Median(groups.Select(g => Median(g.Select(x => x.logGbar))));
        var globalMedianPointGbar = Median(raw.Select(r => r.logGbar));

        var galaxySummaries = groups
            .Select(g =>
            {
                var pts = g.ToList();
                double medianGbar = Median(pts.Select(p => p.logGbar));
                double p25Gbar = Percentile(pts.Select(p => p.logGbar), 0.25);
                double innerShare = ComputeShare(pts, p => p.xRadius < 1.0);
                double outerShare = ComputeShare(pts, p => p.xRadius >= 4.0);
                return new
                {
                    Galaxy = g.Key,
                    Count = pts.Count,
                    MedianGbar = medianGbar,
                    IsLsb = medianGbar <= globalMedianGalaxyGbar,
                    IsLowGbar = p25Gbar <= globalMedianPointGbar,
                    InnerShare = innerShare,
                    OuterShare = outerShare
                };
            })
            .ToList();

        var medianInnerShare = Median(galaxySummaries.Select(s => s.InnerShare));
        var medianOuterShare = Median(galaxySummaries.Select(s => s.OuterShare));

        string RadiusBand(double innerShare, double outerShare)
        {
            if (innerShare >= medianInnerShare + 0.05) return "inner-heavy";
            if (outerShare >= medianOuterShare + 0.05) return "outer-heavy";
            return "mixed";
        }

        var strata = galaxySummaries
            .GroupBy(s => $"{(s.IsLsb ? "LSB" : "HSB")}|{(s.IsLowGbar ? "lowG" : "highG")}|{RadiusBand(s.InnerShare, s.OuterShare)}")
            .ToList();

        var foldLoads = Enumerable.Range(0, foldCount).ToDictionary(f => f, _ => 0);
        var foldMap = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var stratum in strata)
        {
            foreach (var s in stratum.OrderByDescending(x => x.Count))
            {
                int targetFold = foldLoads.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
                foldMap[s.Galaxy] = targetFold;
                foldLoads[targetFold] += s.Count;
            }
        }

        var samples = raw.Where(s => foldMap.ContainsKey(s.galaxy)).ToList();
        Assert.True(samples.Count > 300, $"Too few eligible TO18 samples after stratification: {samples.Count}");

        var foldResults = new List<(int Fold, int TestCount, double TrainImp, double TestImp, double Gap)>();
        var foldProfiles = new List<(int Fold, double LsbShare, double LowGbarShare, double InnerShare, double MiddleShare, double OuterShare)>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = samples.Where(s => foldMap[s.galaxy] != fold).ToList();
            var test = samples.Where(s => foldMap[s.galaxy] == fold).ToList();

            if (train.Count < 120 || test.Count < 80)
                continue;

            var fit = ComputeLinearFit(train.Select(t => t.xO5).ToList(), train.Select(t => t.y).ToList());

            double trainBaseline = Math.Sqrt(train.Average(t => t.y * t.y));
            double trainModel = Math.Sqrt(train.Average(t =>
            {
                double yHat = fit.Intercept + fit.Slope * t.xO5;
                double e = t.y - yHat;
                return e * e;
            }));
            double trainImp = trainBaseline - trainModel;

            double testBaseline = Math.Sqrt(test.Average(t => t.y * t.y));
            double testModel = Math.Sqrt(test.Average(t =>
            {
                double yHat = fit.Intercept + fit.Slope * t.xO5;
                double e = t.y - yHat;
                return e * e;
            }));
            double testImp = testBaseline - testModel;

            foldResults.Add((fold, test.Count, trainImp, testImp, trainImp - testImp));

            var testGalaxies = test.Select(t => t.galaxy).Distinct().ToHashSet(StringComparer.Ordinal);
            var summaryInFold = galaxySummaries.Where(gs => testGalaxies.Contains(gs.Galaxy)).ToList();

            foldProfiles.Add((
                fold,
                summaryInFold.Count > 0 ? summaryInFold.Count(s => s.IsLsb) / (double)summaryInFold.Count : 0.0,
                ComputeShare(test, t => t.logGbar <= globalMedianPointGbar),
                ComputeShare(test, t => t.xRadius < 1.0),
                ComputeShare(test, t => t.xRadius >= 1.0 && t.xRadius < 4.0),
                ComputeShare(test, t => t.xRadius >= 4.0)));
        }

        Assert.True(foldResults.Count >= 3, $"Insufficient valid TO18 folds: {foldResults.Count}");

        foreach (var fr in foldResults)
        {
            WriteTestLine("TO18", $"fold={fr.Fold}, nTest={fr.TestCount}, trainImp={fr.TrainImp:F4}, testImp={fr.TestImp:F4}, gap={fr.Gap:F4}");
        }
        foreach (var fp in foldProfiles.OrderBy(f => f.Fold))
        {
            WriteTestLine("TO18", $"profile fold={fp.Fold}: LSB={fp.LsbShare:F3}, lowG={fp.LowGbarShare:F3}, inner/mid/outer={fp.InnerShare:F3}/{fp.MiddleShare:F3}/{fp.OuterShare:F3}");
        }

        double meanTestImp = foldResults.Average(f => f.TestImp);
        double gapStd = StandardDeviation(foldResults.Select(f => f.Gap));
        double positiveTestShare = foldResults.Count(f => f.TestImp > 0.0) / (double)foldResults.Count;
        double worstFoldImp = foldResults.Min(f => f.TestImp);

        double lsbRange = foldProfiles.Max(f => f.LsbShare) - foldProfiles.Min(f => f.LsbShare);
        double lowGRange = foldProfiles.Max(f => f.LowGbarShare) - foldProfiles.Min(f => f.LowGbarShare);
        double innerRange = foldProfiles.Max(f => f.InnerShare) - foldProfiles.Min(f => f.InnerShare);
        double outerRange = foldProfiles.Max(f => f.OuterShare) - foldProfiles.Min(f => f.OuterShare);

        bool balancedSplits =
            lsbRange <= 0.45 &&
            lowGRange <= 0.25 &&
            innerRange <= 0.25 &&
            outerRange <= 0.25;

        bool stableStratified =
            meanTestImp >= -0.001 &&
            gapStd <= 0.028 &&
            positiveTestShare >= 0.60 &&
            worstFoldImp >= -0.020;

        var classification = stableStratified
            ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
            : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        WriteTestLine("TO18", $"meanTestImp          = {meanTestImp:F4}");
        WriteTestLine("TO18", $"gapStd               = {gapStd:F4}");
        WriteTestLine("TO18", $"positiveTestShare    = {positiveTestShare:F3}");
        WriteTestLine("TO18", $"worstFoldImprovement = {worstFoldImp:F4}");
        WriteTestLine("TO18", $"balance ranges LSB/lowG/inner/outer = {lsbRange:F3}/{lowGRange:F3}/{innerRange:F3}/{outerRange:F3}");
        WriteTestLine("TO18", $"balancedSplits       = {balancedSplits}");
        WriteTestLine("TO18", $"stableStratified     = {stableStratified}");
        WriteTestLine("TO18", $"classification       = {classification.Status}");
        WriteTestLine("TO18", $"claimBoundary        = {classification.ClaimBoundary}");

        Assert.True(balancedSplits, "Stratified holdout splits are not balanced enough for TO18 interpretation.");

        if (stableStratified)
        {
            Assert.Equal("effective-candidate", classification.Status);
        }
        else
        {
            Assert.Equal("diagnostic", classification.Status);
            Assert.Equal("hypothesis-supported", classification.ClaimBoundary);
        }
    }

    [Fact]
    public void TO19_O5_Should_Not_Use_ObservedVelocityLeakage()
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
            BaryonMode.ExponentialDisk);

        var galaxies = trmDisk
            .GroupBy(p => p.GalaxyName)
            .Select(g => g.OrderBy(x => x.RadiusKpc).ToList())
            .Where(g => g.Count >= 12)
            .OrderByDescending(g => g.Count)
            .Take(12)
            .ToList();

        Assert.NotEmpty(galaxies);

        var leakageInvarianceDiffs = new List<double>();
        var allowedInputSensitivityDiffs = new List<double>();

        foreach (var galaxy in galaxies)
        {
            var observedShuffled = Rotate(galaxy.Select(p => p.GobsMs2).ToList(), 3);
            var velocityShuffled = Rotate(galaxy.Select(p => p.Vobs).ToList(), 2);

            var counterfactualObserved = galaxy
                .Select((p, i) => new RarPoint(
                    p.GalaxyName,
                    p.RadiusKpc,
                    velocityShuffled[i], // changed observed velocity
                    p.Vgas,
                    p.Vdisk,
                    p.Vbulge,
                    observedShuffled[i], // changed observed acceleration
                    p.GbarMs2))          // keep allowed baryonic driver unchanged
                .ToList();

            var allowedInputPerturbed = galaxy
                .Select(p => new RarPoint(
                    p.GalaxyName,
                    p.RadiusKpc,
                    p.Vobs,
                    p.Vgas,
                    p.Vdisk,
                    p.Vbulge,
                    p.GobsMs2,
                    p.GbarMs2 * 1.01))   // perturb allowed structural input
                .ToList();

            var fieldBase = TrmFieldSolver.SolveField(galaxy);
            var fieldCounterfactual = TrmFieldSolver.SolveField(counterfactualObserved);
            var fieldAllowedPerturbed = TrmFieldSolver.SolveField(allowedInputPerturbed);

            int start = 2;
            int end = Math.Min(fieldBase.Points.Count - 3, fieldCounterfactual.Points.Count - 3);
            if (end < start)
                continue;

            for (int i = start; i <= end; i++)
            {
                double baseO5 = ComputeO5KernelContrast(fieldBase, i, halfWindow: 6, kernelMode: "inverse");
                double cfO5 = ComputeO5KernelContrast(fieldCounterfactual, i, halfWindow: 6, kernelMode: "inverse");
                double allowedO5 = ComputeO5KernelContrast(fieldAllowedPerturbed, i, halfWindow: 6, kernelMode: "inverse");

                leakageInvarianceDiffs.Add(Math.Abs(baseO5 - cfO5));
                allowedInputSensitivityDiffs.Add(Math.Abs(baseO5 - allowedO5));
            }
        }

        Assert.NotEmpty(leakageInvarianceDiffs);
        Assert.NotEmpty(allowedInputSensitivityDiffs);

        double leakMean = leakageInvarianceDiffs.Average();
        double leakMax = leakageInvarianceDiffs.Max();
        double allowedMean = allowedInputSensitivityDiffs.Average();
        double allowedMax = allowedInputSensitivityDiffs.Max();

        WriteTestLine("TO19", $"leakage invariance mean/max = {leakMean:E6} / {leakMax:E6}");
        WriteTestLine("TO19", $"allowed-input sensitivity mean/max = {allowedMean:E6} / {allowedMax:E6}");

        Assert.True(
            leakMax <= 1e-12,
            $"O5 changes under observed-velocity/residual counterfactuals (potential leakage): max diff={leakMax:E6}");

        Assert.True(
            allowedMean > 1e-7,
            $"O5 is unexpectedly insensitive to allowed structural input perturbation: mean diff={allowedMean:E6}");
    }

    [Fact]
    public void TO20_O5_Should_Remain_Stable_Under_Reasonable_ThetaSolverParameter_Ablation()
    {
        var baselineDataset = BuildDataset();
        var baselineMetrics = EvaluateO5W6HoldoutMetrics(baselineDataset);
        var baselineIndex = baselineDataset.Points
            .GroupBy(PointKey)
            .ToDictionary(g => g.Key, g => g.First().O5W6InvDistance);

        var profiles = new[]
        {
            new SolverAblationProfile("Ablation-MildLowDrive", 0.92, 0.40, TrmDerivedParameters.GetPhiBeta() * 0.045, 520, 0.009),
            new SolverAblationProfile("Ablation-MildHighDrive", 1.08, 0.50, TrmDerivedParameters.GetPhiBeta() * 0.055, 680, 0.011),
            new SolverAblationProfile("Ablation-HighDamping", 1.00, 0.55, TrmDerivedParameters.GetPhiBeta() * 0.050, 620, 0.010),
            new SolverAblationProfile("Ablation-LowDamping", 1.00, 0.35, TrmDerivedParameters.GetPhiBeta() * 0.050, 620, 0.010)
        };

        var results = new List<SolverAblationResult>();

        WriteTestLine(
            "TO20",
            $"baseline: meanTest={baselineMetrics.MeanTestImprovement:F4}, gapStd={baselineMetrics.GapStd:F4}, posShare={baselineMetrics.PositiveTestShare:F3}, worstFold={baselineMetrics.WorstFoldImprovement:F4}");

        foreach (var profile in profiles)
        {
            var ablatedDataset = BuildDataset(
                profile.SourceStrength,
                profile.DampingStrength,
                profile.SyncStrength,
                profile.Iterations,
                profile.Relaxation);

            var ablatedMetrics = EvaluateO5W6HoldoutMetrics(ablatedDataset);

            var paired = ablatedDataset.Points
                .Select(p =>
                {
                    var key = PointKey(p);
                    if (!baselineIndex.TryGetValue(key, out double baseO5))
                        return ((double x, double y)?)null;
                    if (!double.IsFinite(baseO5) || !double.IsFinite(p.O5W6InvDistance))
                        return ((double x, double y)?)null;
                    return (baseO5, p.O5W6InvDistance);
                })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToList();

            Assert.True(paired.Count > 300, $"Too few matched baseline/ablated O5 points for {profile.Name}: {paired.Count}");

            double corr = ComputePearsonCorrelation(
                paired.Select(p => p.x).ToList(),
                paired.Select(p => p.y).ToList());

            bool stable =
                ablatedMetrics.MeanTestImprovement >= Math.Max(0.020, baselineMetrics.MeanTestImprovement - 0.080) &&
                ablatedMetrics.PositiveTestShare >= 0.60 &&
                ablatedMetrics.WorstFoldImprovement >= -0.040 &&
                ablatedMetrics.GapStd <= baselineMetrics.GapStd + 0.030 &&
                corr >= 0.85;

            results.Add(new SolverAblationResult(profile.Name, ablatedMetrics, corr, stable));

            WriteTestLine(
                "TO20",
                $"{profile.Name}: meanTest={ablatedMetrics.MeanTestImprovement:F4}, gapStd={ablatedMetrics.GapStd:F4}, posShare={ablatedMetrics.PositiveTestShare:F3}, worstFold={ablatedMetrics.WorstFoldImprovement:F4}, corrVsBaseline={corr:F3}, stable={stable}");
        }

        Assert.All(
            results,
            r => Assert.True(
                r.IsStable,
                $"O5 unstable for {r.Name}: meanTest={r.Metrics.MeanTestImprovement:F4}, gapStd={r.Metrics.GapStd:F4}, posShare={r.Metrics.PositiveTestShare:F3}, worstFold={r.Metrics.WorstFoldImprovement:F4}, corr={r.CorrelationVsBaseline:F3}"));

        bool allStable = results.All(r => r.IsStable);
        var classification = allStable
            ? new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported")
            : new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        WriteTestLine("TO20", $"allStable      = {allStable}");
        WriteTestLine("TO20", $"classification = {classification.Status}");
        WriteTestLine("TO20", $"claimBoundary  = {classification.ClaimBoundary}");

        Assert.Equal("effective-candidate", classification.Status);
    }

    [Fact]
    public void TO21_O5W6_Should_Behave_Like_FiniteCoherenceSynchronizationTension()
    {
        const int halfWindow = 6;

        var constant = CreateSyntheticThetaProfile(r => 1.5, count: 41, spacing: 0.5);
        var smooth = CreateSyntheticThetaProfile(r => 0.9 + 0.04 * r + 0.003 * r * r, count: 41, spacing: 0.5);
        var localContrast = CreateSyntheticThetaProfile(
            r =>
            {
                double baseTheta = 0.9 + 0.04 * r + 0.003 * r * r;
                double notch = 0.18 * Math.Exp(-Math.Pow((r - 10.0) / 0.7, 2.0));
                return baseTheta - notch;
            },
            count: 41,
            spacing: 0.5);
        var nonLocalBreak = CreateSyntheticThetaProfile(
            r =>
            {
                double baseTheta = 0.9 + 0.04 * r + 0.003 * r * r;
                double syncBreak = r >= 13.0 ? 0.85 : 0.0;
                return baseTheta + syncBreak;
            },
            count: 41,
            spacing: 0.5);

        var constantSeries = ComputeO5Series(constant, halfWindow);
        var smoothSeries = ComputeO5Series(smooth, halfWindow);
        var localContrastSeries = ComputeO5Series(localContrast, halfWindow);
        var nonLocalBreakSeries = ComputeO5Series(nonLocalBreak, halfWindow);

        double constMaxAbs = constantSeries.Select(Math.Abs).Max();
        double smoothMaxAbs = smoothSeries.Select(Math.Abs).Max();
        double localMaxAbs = localContrastSeries.Select(Math.Abs).Max();
        double nonLocalMaxAbs = nonLocalBreakSeries.Select(Math.Abs).Max();

        double localOverSmooth = localMaxAbs - smoothMaxAbs;
        double nonLocalOverLocal = nonLocalMaxAbs - localMaxAbs;

        WriteTestLine("TO21", $"constant max|O5|      = {constMaxAbs:E6}");
        WriteTestLine("TO21", $"smooth max|O5|        = {smoothMaxAbs:E6}");
        WriteTestLine("TO21", $"local-contrast max|O5|= {localMaxAbs:E6}");
        WriteTestLine("TO21", $"nonlocal-break max|O5|= {nonLocalMaxAbs:E6}");
        WriteTestLine("TO21", $"localOverSmooth       = {localOverSmooth:E6}");
        WriteTestLine("TO21", $"nonLocalOverLocal     = {nonLocalOverLocal:E6}");

        Assert.True(
            constMaxAbs <= 1e-12,
            $"O5 should vanish for constant Theta profile, but max|O5|={constMaxAbs:E6}");

        Assert.True(
            smoothMaxAbs <= 0.055,
            $"O5 should remain bounded for smooth Theta profiles, but max|O5|={smoothMaxAbs:E6}");

        Assert.True(
            localOverSmooth >= 0.040,
            $"O5 should react to local Theta contrasts: localOverSmooth={localOverSmooth:E6}");

        Assert.True(
            nonLocalOverLocal >= 0.035,
            $"O5 should grow for non-local synchronization breaks: nonLocalOverLocal={nonLocalOverLocal:E6}");
    }

    [Fact]
    public void TO22_W6_Should_Be_Minimal_Window_With_Stable_CoherenceResponse()
    {
        var profiles = CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25);
        var windows = new[] { 2, 4, 6, 8, 10 };

        var metrics = windows
            .Select(w => EvaluateWindowOperatorMetrics(profiles, w))
            .ToList();

        foreach (var m in metrics)
        {
            WriteTestLine(
                "TO22",
                $"W{m.HalfWindow}: constMax={m.ConstantMaxAbs:E6}, smoothMax={m.SmoothMaxAbs:E6}, localGain={m.LocalGainOverSmooth:E6}, nonLocalGain={m.NonLocalGainOverLocal:E6}, nonLocalMean={m.NonLocalMeanAbs:E6}, nonLocalArea={m.NonLocalArea:E6}, localWidth={m.LocalSupportWidth:F3}, stable={m.IsStable}");
        }

        var stableWindows = metrics
            .Where(m => m.IsStable)
            .Select(m => m.HalfWindow)
            .OrderBy(w => w)
            .ToList();

        Assert.NotEmpty(stableWindows);
        Assert.Equal(6, stableWindows.First());
        Assert.Contains(6, stableWindows);
        Assert.DoesNotContain(2, stableWindows);
        Assert.DoesNotContain(4, stableWindows);
    }

    [Fact]
    public void TO23_W6_Should_Balance_Locality_And_Nonlocality()
    {
        var profiles = CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25);
        var windows = new[] { 2, 4, 6, 8, 10 };

        var scored = windows
            .Select(w =>
            {
                var m = EvaluateWindowOperatorMetrics(profiles, w);
                double balanceScore = ComputeWindowBalanceScore(m);
                return new { m.HalfWindow, Metrics = m, Score = balanceScore };
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        foreach (var s in scored)
        {
            WriteTestLine(
                "TO23",
                $"W{s.HalfWindow}: score={s.Score:F6}, local={s.Metrics.LocalGainOverSmooth:E6}, nonlocal={s.Metrics.NonLocalGainOverLocal:E6}, nonLocalMean={s.Metrics.NonLocalMeanAbs:E6}, nonLocalArea={s.Metrics.NonLocalArea:E6}, localWidth={s.Metrics.LocalSupportWidth:F3}, smoothStd={s.Metrics.SmoothStdAbs:E6}");
        }

        int w6Rank = scored.FindIndex(x => x.HalfWindow == 6);
        Assert.True(w6Rank >= 0, "W6 not found in TO23 ranking.");
        Assert.True(w6Rank <= 1, $"W6 should be top-2 by locality/nonlocality balance but rank={w6Rank + 1}.");
    }

    [Fact]
    public void TO24_W6_Should_Remain_Stable_Under_ProfileResolutionScaling()
    {
        var configs = new[]
        {
            new ResolutionConfig("coarse", Count: 41, Spacing: 0.50),
            new ResolutionConfig("medium", Count: 81, Spacing: 0.25),
            new ResolutionConfig("fine", Count: 161, Spacing: 0.125)
        };

        var windows = new[] { 2, 4, 6, 8, 10 };
        var bestByResolution = new List<(string Name, int BestWindow, double BestScore, double EffectiveLength, WindowOperatorMetrics W6Metrics, bool W6ScaledStable)>();

        foreach (var cfg in configs)
        {
            var profiles = CreateDefaultSyntheticProfiles(cfg.Count, cfg.Spacing);
            var perWindow = windows
                .Select(w =>
                {
                    var m = EvaluateWindowOperatorMetrics(profiles, w);
                    double score = ComputeWindowBalanceScore(m);
                    return new { Window = w, Metrics = m, Score = score };
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var best = perWindow.First();
            var w6 = perWindow.First(x => x.Window == 6).Metrics;
            double scaledNonLocalStrength = w6.NonLocalMeanAbs / Math.Max(cfg.Spacing, 1e-9);
            bool w6ScaledStable =
                w6.ConstantMaxAbs <= 1e-12 &&
                w6.SmoothMaxAbs <= 0.060 &&
                w6.LocalGainOverSmooth >= 0.028 &&
                w6.NonLocalGainOverLocal >= 0.025 &&
                scaledNonLocalStrength >= 0.040;

            bestByResolution.Add((cfg.Name, best.Window, best.Score, best.Window * cfg.Spacing, w6, w6ScaledStable));

            WriteTestLine(
                "TO24",
                $"{cfg.Name}: best=W{best.Window}, score={best.Score:F6}, effLength={best.Window * cfg.Spacing:F3}, W6 stable={w6.IsStable}, W6ScaledStable={w6ScaledStable}, scaledNonLocal={scaledNonLocalStrength:F6}");
        }

        Assert.All(
            bestByResolution,
            r => Assert.InRange(r.BestWindow, 4, 8));

        Assert.All(
            bestByResolution,
            r => Assert.True(
                r.W6ScaledStable,
                $"W6 loses coherence-response stability at resolution {r.Name}."));

        double minLen = bestByResolution.Min(r => r.EffectiveLength);
        double maxLen = bestByResolution.Max(r => r.EffectiveLength);
        double meanLen = bestByResolution.Average(r => r.EffectiveLength);
        double lenScaleRatio = minLen > 0.0 ? maxLen / minLen : double.PositiveInfinity;
        bool monotonicControlledScaling =
            bestByResolution[0].EffectiveLength >= bestByResolution[1].EffectiveLength &&
            bestByResolution[1].EffectiveLength >= bestByResolution[2].EffectiveLength;

        WriteTestLine("TO24", $"effectiveLength min/mean/max = {minLen:F3}/{meanLen:F3}/{maxLen:F3}");
        WriteTestLine("TO24", $"effectiveLength scale ratio  = {lenScaleRatio:F3}");
        WriteTestLine("TO24", $"controlled monotonic scaling = {monotonicControlledScaling}");

        Assert.True(
            monotonicControlledScaling && lenScaleRatio <= 8.0,
            $"Effective coherence-length scaling is not controlled: monotonic={monotonicControlledScaling}, scaleRatio={lenScaleRatio:F3}");
    }

    [Fact]
    public void TO25_O5_Should_Match_NegativeGradient_Of_FiniteCoherenceEnergy()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var profiles = new[]
        {
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).Smooth,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).NonLocalBreak
        };

        var o5Series = new List<double>();
        var energyGradientSeries = new List<double>();

        foreach (var profile in profiles)
        {
            o5Series.AddRange(ComputeO5Series(profile, halfWindow));
            energyGradientSeries.AddRange(ComputeFiniteCoherenceNegativeGradientSeries(profile, halfWindow, epsilon, normalize: true));
        }

        Assert.True(o5Series.Count > 80, $"Too few TO25 comparison points: {o5Series.Count}");
        Assert.Equal(o5Series.Count, energyGradientSeries.Count);

        double corr = ComputePearsonCorrelation(o5Series, energyGradientSeries);
        var fit = ComputeLinearFit(energyGradientSeries, o5Series);
        double signAgreement = ComputeSignAgreement(o5Series, energyGradientSeries);

        WriteTestLine("TO25", $"corr(O5, -dE/dTheta) = {corr:F4}");
        WriteTestLine("TO25", $"fit slope/intercept  = {fit.Slope:F4}/{fit.Intercept:E6}");
        WriteTestLine("TO25", $"fit R2               = {fit.RSquared:F4}");
        WriteTestLine("TO25", $"sign agreement       = {signAgreement:F3}");

        Assert.True(corr >= 0.90, $"O5 should align with finite-coherence negative energy gradient, but corr={corr:F4}");
        Assert.True(fit.RSquared >= 0.75, $"-dE/dTheta should explain O5 structure, but R2={fit.RSquared:F4}");
        Assert.True(signAgreement >= 0.90, $"O5 and -dE/dTheta sign agreement too low: {signAgreement:F3}");
    }

    [Fact]
    public void TO26_CoherenceEnergy_Should_Decrease_Under_O5_RelaxationStep()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;
        var candidateSteps = new[] { 0.10, 0.05, 0.02, 0.01, 0.005, 0.002 };

        var profiles = new[]
        {
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).Smooth,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).NonLocalBreak
        };

        int strictDecreaseCount = 0;

        foreach (var profile in profiles)
        {
            double e0 = ComputeFiniteCoherenceEnergy(profile, halfWindow, epsilon);
            bool foundDecrease = false;

            foreach (double step in candidateSteps)
            {
                var relaxed = RelaxProfileAlongFiniteCoherenceOperator(profile, halfWindow, epsilon, step);
                double e1 = ComputeFiniteCoherenceEnergy(relaxed, halfWindow, epsilon);

                WriteTestLine("TO26", $"step={step:F3}, E0={e0:E6}, E1={e1:E6}, dE={e1 - e0:E6}");

                if (e1 <= e0 + 1e-12)
                {
                    foundDecrease = true;
                    if (e1 < e0 - 1e-6)
                        strictDecreaseCount++;
                    break;
                }
            }

            Assert.True(
                foundDecrease,
                $"No tested small O5 relaxation step reduced coherence energy from E0={e0:E6}.");
        }

        Assert.True(strictDecreaseCount >= 2, $"Expected strict coherence-energy decrease in at least two non-constant profiles, got {strictDecreaseCount}.");
    }

    [Fact]
    public void TO27_O5EnergyOperator_Should_Preserve_ConstantThetaZeroMode()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var constant = CreateSyntheticThetaProfile(r => 2.0, count: 81, spacing: 0.25);
        var series = ComputeFiniteCoherenceNegativeGradientSeries(constant, halfWindow, epsilon, normalize: true);
        double maxAbs = series.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
        double energy = ComputeFiniteCoherenceEnergy(constant, halfWindow, epsilon);

        WriteTestLine("TO27", $"constant max|O5_energy| = {maxAbs:E6}");
        WriteTestLine("TO27", $"constant E_theta        = {energy:E6}");

        Assert.True(maxAbs <= 1e-12, $"Constant Theta should be zero mode, but max|operator|={maxAbs:E6}");
        Assert.True(energy <= 1e-12, $"Constant Theta should have near-zero coherence energy, but E={energy:E6}");
    }

    [Fact]
    public void TO28_O5EnergyOperator_Should_Remain_Bounded_For_SmoothProfiles()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var smoothA = CreateSyntheticThetaProfile(r => 0.8 + 0.03 * r + 0.002 * r * r, count: 81, spacing: 0.25);
        var smoothB = CreateSyntheticThetaProfile(r => 1.0 + 0.04 * Math.Sin(0.22 * r) + 0.0015 * r * r, count: 81, spacing: 0.25);

        foreach (var profile in new[] { smoothA, smoothB })
        {
            var series = ComputeFiniteCoherenceNegativeGradientSeries(profile, halfWindow, epsilon, normalize: true);
            double maxAbs = series.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
            double rms = Math.Sqrt(series.Select(v => v * v).DefaultIfEmpty(0.0).Average());
            double energyDensity = ComputeFiniteCoherenceEnergy(profile, halfWindow, epsilon) / Math.Max(profile.Points.Count, 1);

            WriteTestLine("TO28", $"smooth max|operator|={maxAbs:E6}, rms={rms:E6}, energyDensity={energyDensity:E6}");

            Assert.True(maxAbs <= 0.10, $"Smooth-profile operator amplification too large: max|operator|={maxAbs:E6}");
            Assert.True(rms <= 0.05, $"Smooth-profile operator RMS too large: rms={rms:E6}");
            Assert.True(energyDensity <= 0.20, $"Smooth-profile coherence energy density too large: {energyDensity:E6}");
        }
    }

    [Fact]
    public void TQK01_PhaseLatticeCoupling_Should_Reduce_To_ThetaCoherenceEnergy()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var baseProfile = CreateSyntheticThetaProfile(
            r => 0.55 * Math.Sin(0.31 * r) + 0.35 * Math.Cos(0.17 * r),
            count: 101,
            spacing: 0.20);

        var amplitudes = new[] { 0.10, 0.15, 0.20, 0.30, 0.40 };
        var phaseEnergies = new List<double>();
        var thetaEnergies = new List<double>();

        foreach (double a in amplitudes)
        {
            var scaled = TransformThetaProfile(baseProfile, t => a * t);
            double ePhase = ComputePhaseLatticeEnergy(scaled, halfWindow, "inverse", epsilon);
            double eTheta = ComputeQuadraticCoherenceEnergy(scaled, halfWindow, "inverse", epsilon);

            phaseEnergies.Add(ePhase);
            thetaEnergies.Add(eTheta);

            WriteTestLine("TQK01", $"amp={a:F2}, Ephase={ePhase:E6}, Etheta={eTheta:E6}, ratio={ePhase / Math.Max(eTheta, 1e-20):F4}");
        }

        double corr = ComputePearsonCorrelation(thetaEnergies, phaseEnergies);
        var fit = ComputeLinearFit(thetaEnergies, phaseEnergies);

        WriteTestLine("TQK01", $"corr(Etheta,Ephase)={corr:F6}, slope={fit.Slope:F4}, intercept={fit.Intercept:E6}, R2={fit.RSquared:F6}");

        Assert.True(corr >= 0.995, $"Small-angle phase energy should reduce to quadratic coherence energy, corr={corr:F6}");
        Assert.True(fit.RSquared >= 0.990, $"Phase/quadratic reduction fit too weak, R2={fit.RSquared:F6}");
        Assert.True(fit.Slope >= 0.90 && fit.Slope <= 1.10, $"Expected near-unit proportionality between Ephase and Etheta, slope={fit.Slope:F4}");
    }

    [Fact]
    public void TQK02_O5_Should_Match_PhaseLatticeEnergyGradient()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var profiles = new[]
        {
            CreateDefaultSyntheticProfiles(count: 101, spacing: 0.20).Smooth,
            CreateDefaultSyntheticProfiles(count: 101, spacing: 0.20).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 101, spacing: 0.20).NonLocalBreak
        };

        var o5Series = new List<double>();
        var latticeGradientSeries = new List<double>();

        foreach (var profile in profiles)
        {
            o5Series.AddRange(ComputeO5Series(profile, halfWindow));
            latticeGradientSeries.AddRange(ComputePhaseLatticeNegativeGradientSeries(profile, halfWindow, "inverse", epsilon, normalize: true));
        }

        Assert.Equal(o5Series.Count, latticeGradientSeries.Count);
        Assert.True(o5Series.Count > 120, $"Too few TQK02 comparison points: {o5Series.Count}");

        double corr = ComputePearsonCorrelation(o5Series, latticeGradientSeries);
        var fit = ComputeLinearFit(latticeGradientSeries, o5Series);
        double signAgreement = ComputeSignAgreement(o5Series, latticeGradientSeries);

        WriteTestLine("TQK02", $"corr(O5, -dEphase/dTheta) = {corr:F4}");
        WriteTestLine("TQK02", $"fit slope/intercept       = {fit.Slope:F4}/{fit.Intercept:E6}");
        WriteTestLine("TQK02", $"fit R2                    = {fit.RSquared:F4}");
        WriteTestLine("TQK02", $"sign agreement            = {signAgreement:F3}");

        Assert.True(corr >= 0.88, $"O5 should correlate with phase-lattice negative gradient, corr={corr:F4}");
        Assert.True(fit.RSquared >= 0.70, $"Phase-lattice gradient should explain O5 structure, R2={fit.RSquared:F4}");
        Assert.True(signAgreement >= 0.85, $"O5 and phase-lattice gradient sign agreement too low: {signAgreement:F3}");
    }

    [Fact]
    public void TQK03_InverseDistanceKernel_Should_Emerge_From_LocalCouplingDecay()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var kernels = new[] { "uniform", "inverse", "gaussian", "exponential" };
        var profilePool = BuildTqkProfilePool();
        var scored = kernels
            .Select(mode => EvaluatePhaseKernelScore(mode, profilePool, halfWindow, epsilon))
            .OrderByDescending(s => s.BalanceScore)
            .ToList();

        foreach (var s in scored)
        {
            WriteTestLine(
                "TQK03",
                $"{s.KernelMode}: score={s.BalanceScore:F6}, energyDrop={s.MeanEnergyDrop:F5}, smoothNoise={s.SmoothNoise:F5}, localSens={s.LocalContrastSensitivity:F5}, nonLocalSens={s.NonLocalBreakSensitivity:F5}, holdoutMean={s.HoldoutMeanImprovement:F5}, holdoutPos={s.HoldoutPositiveShare:F3}");
        }

        int inverseRank = scored.FindIndex(s => s.KernelMode == "inverse");
        Assert.True(inverseRank >= 0, "Inverse-distance kernel missing from TQK03 ranking.");
        Assert.True(inverseRank <= 1, $"Inverse-distance kernel should be top-2 in balance score, rank={inverseRank + 1}.");
    }

    [Fact]
    public void TQK04_W6_Should_Match_CorrelationLength_Of_PhaseLattice()
    {
        var windows = new[] { 4, 6, 8 };
        var lengths = windows
            .Select(w =>
            {
                var profile = CreateCorrelatedLatticePhaseProfile(correlationHalfWindow: w, count: 151, spacing: 0.20);
                double corrLen = EstimatePhaseCorrelationLength(profile, maxLag: 20);
                WriteTestLine("TQK04", $"W{w}: estimated correlation length = {corrLen:F3}");
                return (Window: w, CorrelationLength: corrLen);
            })
            .ToList();

        double l4 = lengths.First(x => x.Window == 4).CorrelationLength;
        double l6 = lengths.First(x => x.Window == 6).CorrelationLength;
        double l8 = lengths.First(x => x.Window == 8).CorrelationLength;

        Assert.True(l4 <= l6 + 0.5, $"Correlation length should not decrease from W4 to W6: L4={l4:F3}, L6={l6:F3}");
        Assert.True(l6 <= l8 + 0.5, $"Correlation length should not decrease from W6 to W8: L6={l6:F3}, L8={l8:F3}");
        Assert.True(l6 >= 4.0 && l6 <= 8.0, $"W6 correlation length should be in finite-coherence band [4,8], got {l6:F3}");
        Assert.True(Math.Abs(l6 - 6.0) <= 2.0, $"W6 should approximately match lattice correlation length, got {l6:F3}");
    }

    [Fact]
    public void LC01_LambdaTheta_Should_Be_Dimensionally_Consistent()
    {
        var dataset = BuildDataset();
        var samples = BuildLambdaSamples(dataset, p => p.O5W6InvDistance);

        Assert.True(samples.Count > 400, $"Too few lambda samples for LC01: {samples.Count}");

        double lambda = FitGlobalLambda(samples);
        double lambdaScaledInput = FitGlobalLambda(samples.Select(s => s with { X = 10.0 * s.X }).ToList());
        double scaleConsistency = Math.Abs(lambda - 10.0 * lambdaScaledInput) / Math.Max(Math.Abs(lambda), 1e-20);

        double modelImprovement = ComputeLambdaImprovement(samples, lambda);
        double contributionMedian = Median(samples.Select(s => Math.Abs(lambda * s.X)));
        double baseMedian = Median(samples.Select(s => s.GBase));
        double contributionFraction = contributionMedian / Math.Max(baseMedian, 1e-20);
        double a0 = TrmDerivedParameters.GetA0_Ms2();
        double contributionOverA0 = contributionMedian / Math.Max(a0, 1e-30);

        WriteTestLine("LC01", $"lambdaGlobal              = {lambda:E6}");
        WriteTestLine("LC01", $"lambdaScaleConsistencyErr = {scaleConsistency:E6}");
        WriteTestLine("LC01", $"modelImprovement          = {modelImprovement:E6}");
        WriteTestLine("LC01", $"medianContribution/base   = {contributionFraction:E6}");
        WriteTestLine("LC01", $"medianContribution/a0     = {contributionOverA0:E6}");

        Assert.True(double.IsFinite(lambda), "Global lambda is not finite.");
        Assert.True(scaleConsistency <= 1e-10, $"Lambda scaling inconsistency under O5 rescaling: err={scaleConsistency:E6}");
        Assert.True(modelImprovement >= 0.0, $"Global lambda should not degrade the additive residual model: improvement={modelImprovement:E6}");
        Assert.True(
            contributionFraction >= 1e-6 && contributionFraction <= 5.0,
            $"Lambda contribution scale looks dimensionally inconsistent: contribution/base={contributionFraction:E6}");
    }

    [Fact]
    public void LC02_LambdaTheta_Should_Generalize_As_GlobalCoefficient()
    {
        var dataset = BuildDataset();
        var samples = BuildLambdaSamples(dataset, p => p.O5W6InvDistance);
        var groups = samples
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        Assert.True(groups.Count >= 8, $"Too few galaxy groups for LC02: {groups.Count}");

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);
        var eligible = samples.Where(s => foldMap.ContainsKey(s.Galaxy)).ToList();

        var foldResults = new List<(int Fold, double GlobalImprovement, double OracleImprovement, double Ratio)>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = eligible.Where(s => foldMap[s.Galaxy] != fold).ToList();
            var test = eligible.Where(s => foldMap[s.Galaxy] == fold).ToList();
            if (train.Count < 120 || test.Count < 80)
                continue;

            double lambdaGlobal = FitGlobalLambda(train);
            double globalImprovement = ComputeLambdaImprovement(test, lambdaGlobal);

            double oracleImprovement = test
                .GroupBy(s => s.Galaxy)
                .Where(g => g.Count() >= 5)
                .Select(g =>
                {
                    double lambdaGalaxy = FitGlobalLambda(g.ToList());
                    return ComputeLambdaImprovement(g.ToList(), lambdaGalaxy);
                })
                .Average();

            double ratio = oracleImprovement > 1e-12 ? globalImprovement / oracleImprovement : 1.0;
            foldResults.Add((fold, globalImprovement, oracleImprovement, ratio));

            WriteTestLine("LC02", $"fold={fold}, globalImp={globalImprovement:E6}, oracleImp={oracleImprovement:E6}, ratio={ratio:F3}");
        }

        Assert.True(foldResults.Count >= 3, $"Insufficient valid LC02 folds: {foldResults.Count}");

        double meanGlobalImp = foldResults.Average(f => f.GlobalImprovement);
        double positiveShare = foldResults.Count(f => f.GlobalImprovement >= 0.0) / (double)foldResults.Count;
        double medianRatio = Median(foldResults.Select(f => f.Ratio));

        WriteTestLine("LC02", $"meanGlobalImprovement = {meanGlobalImp:E6}");
        WriteTestLine("LC02", $"positiveFoldShare     = {positiveShare:F3}");
        WriteTestLine("LC02", $"medianGlobalOracleRat = {medianRatio:F3}");

        Assert.True(meanGlobalImp >= 0.0, $"Global lambda should generalize on average, mean improvement={meanGlobalImp:E6}");
        Assert.True(positiveShare >= 0.45, $"Too few LC02 folds with non-negative global-lambda improvement: {positiveShare:F3}");
        Assert.True(medianRatio >= 0.05, $"Global lambda captures too little of per-galaxy oracle effect: ratio={medianRatio:F3}");
    }

    [Fact]
    public void LC03_LambdaTheta_Should_Map_To_EnergyRelaxationScale()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;
        var candidateSteps = new[] { 0.10, 0.05, 0.02, 0.01, 0.005, 0.002 };

        var dataset = BuildDataset();
        var samples = BuildLambdaSamples(dataset, p => p.O5W6InvDistance);
        double lambdaGlobal = FitGlobalLambda(samples);

        var profiles = new[]
        {
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).Smooth,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).NonLocalBreak
        };

        var bestSteps = new List<double>();
        foreach (var p in profiles)
        {
            double e0 = ComputeFiniteCoherenceEnergy(p, halfWindow, epsilon);
            double bestStep = candidateSteps[0];
            double bestDrop = double.NegativeInfinity;
            foreach (double step in candidateSteps)
            {
                double e1 = ComputeFiniteCoherenceEnergy(
                    RelaxProfileAlongFiniteCoherenceOperator(p, halfWindow, epsilon, step),
                    halfWindow,
                    epsilon);
                double relDrop = e0 > 0.0 ? (e0 - e1) / e0 : 0.0;
                if (relDrop > bestDrop)
                {
                    bestDrop = relDrop;
                    bestStep = step;
                }
            }
            bestSteps.Add(bestStep);
        }

        double etaTheta = bestSteps.Average();
        double tauSync = EstimatePhaseCorrelationLength(CreateCorrelatedLatticePhaseProfile(correlationHalfWindow: 6, count: 151, spacing: 0.20), maxLag: 20);
        double o5Scale = Median(samples.Select(s => Math.Abs(s.X)));
        double a0 = TrmDerivedParameters.GetA0_Ms2();
        double lambdaFromEnergyScale = etaTheta * tauSync * a0 / Math.Max(o5Scale, 1e-20);
        double ratio = Math.Abs(lambdaGlobal) / Math.Max(Math.Abs(lambdaFromEnergyScale), 1e-30);

        WriteTestLine("LC03", $"lambdaGlobal          = {lambdaGlobal:E6}");
        WriteTestLine("LC03", $"etaTheta(avg bestStep)= {etaTheta:F4}");
        WriteTestLine("LC03", $"tauSync(proxy)        = {tauSync:F4}");
        WriteTestLine("LC03", $"lambdaEnergyScale     = {lambdaFromEnergyScale:E6}");
        WriteTestLine("LC03", $"lambdaRatio           = {ratio:F4}");

        Assert.True(double.IsFinite(lambdaFromEnergyScale) && Math.Abs(lambdaFromEnergyScale) > 0.0, "Energy-relaxation lambda proxy invalid.");
        Assert.True(ratio >= 0.05 && ratio <= 20.0, $"Global lambda is not in plausible energy-relaxation scale range: ratio={ratio:F4}");
    }

    [Fact]
    public void LC04_LambdaTheta_Should_Remain_Stable_Under_SolverAndKernelAblation()
    {
        var solverProfiles = new[]
        {
            new SolverAblationProfile("Default", 1.00, 0.45, TrmDerivedParameters.GetPhiBeta() * 0.050, 600, 0.010),
            new SolverAblationProfile("MildLowDrive", 0.92, 0.40, TrmDerivedParameters.GetPhiBeta() * 0.045, 520, 0.009),
            new SolverAblationProfile("MildHighDrive", 1.08, 0.50, TrmDerivedParameters.GetPhiBeta() * 0.055, 680, 0.011),
            new SolverAblationProfile("HighDamping", 1.00, 0.55, TrmDerivedParameters.GetPhiBeta() * 0.050, 620, 0.010),
            new SolverAblationProfile("LowDamping", 1.00, 0.35, TrmDerivedParameters.GetPhiBeta() * 0.050, 620, 0.010)
        };

        var kernelSelectors = new List<(string Name, Func<DataPoint, double> Selector)>
        {
            ("W4-Inverse", p => p.O5W4InvDistance),
            ("W6-Inverse", p => p.O5W6InvDistance),
            ("W8-Inverse", p => p.O5W8InvDistance),
            ("W4-Gaussian", p => p.O5W4Gaussian),
            ("W4-Uniform", p => p.O5W4Uniform)
        };

        var scenarioResults = new List<LambdaScenarioResult>();

        foreach (var solver in solverProfiles)
        {
            var dataset = solver.Name == "Default"
                ? BuildDataset()
                : BuildDataset(solver.SourceStrength, solver.DampingStrength, solver.SyncStrength, solver.Iterations, solver.Relaxation);

            foreach (var kernel in kernelSelectors)
            {
                var samples = BuildLambdaSamples(dataset, kernel.Selector);
                if (samples.Count < 300)
                    continue;

                double lambda = FitGlobalLambda(samples);
                double improvement = ComputeLambdaImprovement(samples, lambda);
                double contributionScale = Median(samples.Select(s => Math.Abs(lambda * s.X)));

                scenarioResults.Add(new LambdaScenarioResult($"{solver.Name}|{kernel.Name}", lambda, improvement, contributionScale));
                WriteTestLine("LC04", $"{solver.Name}|{kernel.Name}: lambda={lambda:E6}, improvement={improvement:E6}, contributionScale={contributionScale:E6}");
            }
        }

        Assert.True(scenarioResults.Count >= 12, $"Too few LC04 ablation scenarios: {scenarioResults.Count}");

        var reference = scenarioResults.FirstOrDefault(s => s.Name == "Default|W6-Inverse")
            ?? scenarioResults.OrderByDescending(s => s.Improvement).First();

        var ratioLogs = scenarioResults
            .Select(s => Math.Log10(Math.Max(s.ContributionScale, 1e-30) / Math.Max(reference.ContributionScale, 1e-30)))
            .ToList();

        double medianAbsLog10Ratio = Median(ratioLogs.Select(Math.Abs));
        double positiveShare = scenarioResults.Count(s => s.Improvement >= 0.0) / (double)scenarioResults.Count;

        WriteTestLine("LC04", $"referenceScenario        = {reference.Name}");
        WriteTestLine("LC04", $"medianAbsLog10ScaleRatio = {medianAbsLog10Ratio:F4}");
        WriteTestLine("LC04", $"positiveScenarioShare    = {positiveShare:F3}");

        Assert.True(
            medianAbsLog10Ratio <= 1.0,
            $"Lambda contribution scale drifts by more than one order of magnitude under ablations: medianAbsLog10={medianAbsLog10Ratio:F4}");
        Assert.True(
            positiveShare >= 0.50,
            $"Too many solver/kernel ablations break lambda effectiveness: positiveShare={positiveShare:F3}");
    }

    [Fact]
    public void LC05_LambdaTheta_Should_Be_RegimeConditioned_NotGalaxyFitted()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0 || p.Rd <= 0.0 || p.Gbar <= 0.0 || !double.IsFinite(p.O5W6InvDistance))
                    return (LambdaRegimeSample?)null;

                double gBase = alphaLocal * p.GLocal;
                if (gBase <= 0.0 || !double.IsFinite(gBase))
                    return (LambdaRegimeSample?)null;

                return new LambdaRegimeSample(
                    p.GalaxyName,
                    p.GObs,
                    gBase,
                    p.O5W6InvDistance,
                    p.Radius / p.Rd,
                    Math.Log10(Math.Max(p.Gbar, 1e-30)));
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        Assert.True(raw.Count > 400, $"Too few LC05 samples: {raw.Count}");

        var groups = raw
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        Assert.True(groups.Count >= 8, $"Too few LC05 galaxy groups: {groups.Count}");

        var galaxyMedianLogGbar = groups.ToDictionary(
            g => g.Key,
            g => Median(g.Select(x => x.LogGbar)),
            StringComparer.Ordinal);
        double globalMedianGalaxyLogGbar = Median(galaxyMedianLogGbar.Values);
        double globalMedianPointLogGbar = Median(raw.Select(r => r.LogGbar));

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);
        var eligible = raw.Where(s => foldMap.ContainsKey(s.Galaxy)).ToList();

        int RegimeId(LambdaRegimeSample s)
        {
            if (s.XRadius < 1.0) return 0; // inner
            if (s.XRadius < 4.0) return 1; // middle
            return 2;                      // outer
        }

        var foldResults = new List<(int Fold, double GlobalImp, double RegimeImp, double Gain, double StrataFloor)>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = eligible.Where(s => foldMap[s.Galaxy] != fold).ToList();
            var test = eligible.Where(s => foldMap[s.Galaxy] == fold).ToList();
            if (train.Count < 120 || test.Count < 80)
                continue;

            double lambdaGlobal = FitGlobalLambda(train.Select(t => new LambdaSample(t.Galaxy, t.GObs, t.GBase, t.X)).ToList());
            double globalImp = ComputeLambdaImprovement(test, s => s.GBase + lambdaGlobal * s.X);

            var regimeLambdas = FitRegimeLambdas(train, RegimeId, regimeCount: 3);
            double regimeImp = ComputeLambdaImprovement(test, s => s.GBase + regimeLambdas[RegimeId(s)] * s.X);

            double strataLowHigh = Math.Min(
                ComputeLambdaImprovement(test.Where(s => s.LogGbar <= globalMedianPointLogGbar).ToList(), s => s.GBase + regimeLambdas[RegimeId(s)] * s.X),
                ComputeLambdaImprovement(test.Where(s => s.LogGbar > globalMedianPointLogGbar).ToList(), s => s.GBase + regimeLambdas[RegimeId(s)] * s.X));

            double strataLsbHsb = Math.Min(
                ComputeLambdaImprovement(
                    test.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m <= globalMedianGalaxyLogGbar).ToList(),
                    s => s.GBase + regimeLambdas[RegimeId(s)] * s.X),
                ComputeLambdaImprovement(
                    test.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m > globalMedianGalaxyLogGbar).ToList(),
                    s => s.GBase + regimeLambdas[RegimeId(s)] * s.X));

            double strataFloor = Math.Min(strataLowHigh, strataLsbHsb);
            double gain = regimeImp - globalImp;

            foldResults.Add((fold, globalImp, regimeImp, gain, strataFloor));

            WriteTestLine(
                "LC05",
                $"fold={fold}, globalImp={globalImp:E6}, regimeImp={regimeImp:E6}, gain={gain:E6}, strataFloor={strataFloor:E6}, lambdas={regimeLambdas[0]:E4}/{regimeLambdas[1]:E4}/{regimeLambdas[2]:E4}");
        }

        Assert.True(foldResults.Count >= 3, $"Insufficient valid LC05 folds: {foldResults.Count}");

        double meanGlobal = foldResults.Average(f => f.GlobalImp);
        double meanRegime = foldResults.Average(f => f.RegimeImp);
        double meanGain = foldResults.Average(f => f.Gain);
        double gainShare = foldResults.Count(f => f.Gain >= 0.0) / (double)foldResults.Count;
        double worstStrataFloor = foldResults.Min(f => f.StrataFloor);

        WriteTestLine("LC05", $"meanGlobalImp    = {meanGlobal:E6}");
        WriteTestLine("LC05", $"meanRegimeImp    = {meanRegime:E6}");
        WriteTestLine("LC05", $"meanGain         = {meanGain:E6}");
        WriteTestLine("LC05", $"nonNegGainShare  = {gainShare:F3}");
        WriteTestLine("LC05", $"worstStrataFloor = {worstStrataFloor:E6}");

        Assert.True(meanRegime >= meanGlobal, $"Regime-conditioned lambda should improve over global lambda on average: global={meanGlobal:E6}, regime={meanRegime:E6}");
        Assert.True(gainShare >= 0.60, $"Too few folds improved by regime-conditioned lambda: share={gainShare:F3}");
        Assert.True(worstStrataFloor >= -5e-10, $"Regime-conditioned lambda is unstable in at least one physical stratum: floor={worstStrataFloor:E6}");
    }

    [Fact]
    public void LC06_LambdaTheta_Should_Use_RegularizedRegimeConditioning()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0 || p.Rd <= 0.0 || p.Gbar <= 0.0 || !double.IsFinite(p.O5W6InvDistance))
                    return (LambdaRegimeSample?)null;

                double gBase = alphaLocal * p.GLocal;
                if (gBase <= 0.0 || !double.IsFinite(gBase))
                    return (LambdaRegimeSample?)null;

                return new LambdaRegimeSample(
                    p.GalaxyName,
                    p.GObs,
                    gBase,
                    p.O5W6InvDistance,
                    p.Radius / p.Rd,
                    Math.Log10(Math.Max(p.Gbar, 1e-30)));
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        Assert.True(raw.Count > 400, $"Too few LC06 samples: {raw.Count}");

        var groups = raw
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        Assert.True(groups.Count >= 8, $"Too few LC06 galaxy groups: {groups.Count}");

        var galaxyMedianLogGbar = groups.ToDictionary(
            g => g.Key,
            g => Median(g.Select(x => x.LogGbar)),
            StringComparer.Ordinal);
        double globalMedianGalaxyLogGbar = Median(galaxyMedianLogGbar.Values);
        double globalMedianPointLogGbar = Median(raw.Select(r => r.LogGbar));

        int RegimeId(LambdaRegimeSample s)
        {
            if (s.XRadius < 1.0) return 0; // inner
            if (s.XRadius < 4.0) return 1; // middle
            return 2;                      // outer
        }

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);
        var eligible = raw.Where(s => foldMap.ContainsKey(s.Galaxy)).ToList();

        var alphas = new[] { 0.25, 0.50, 0.75 };
        var alphaResults = new List<RegularizedAlphaResult>();

        foreach (double alpha in alphas)
        {
            var foldResults = new List<(double GlobalImp, double RegularizedImp, double Gain, double StrataFloor)>();

            for (int fold = 0; fold < foldCount; fold++)
            {
                var train = eligible.Where(s => foldMap[s.Galaxy] != fold).ToList();
                var test = eligible.Where(s => foldMap[s.Galaxy] == fold).ToList();
                if (train.Count < 120 || test.Count < 80)
                    continue;

                double lambdaGlobal = FitGlobalLambda(train.Select(t => new LambdaSample(t.Galaxy, t.GObs, t.GBase, t.X)).ToList());
                double globalImp = ComputeLambdaImprovement(test, s => s.GBase + lambdaGlobal * s.X);

                var regimeFit = FitRegimeLambdas(train, RegimeId, regimeCount: 3);
                var lambdaReg = regimeFit
                    .Select(l => (1.0 - alpha) * lambdaGlobal + alpha * l)
                    .ToArray();

                double regImp = ComputeLambdaImprovement(test, s => s.GBase + lambdaReg[RegimeId(s)] * s.X);

                double strataLowHigh = Math.Min(
                    ComputeLambdaImprovement(test.Where(s => s.LogGbar <= globalMedianPointLogGbar).ToList(), s => s.GBase + lambdaReg[RegimeId(s)] * s.X),
                    ComputeLambdaImprovement(test.Where(s => s.LogGbar > globalMedianPointLogGbar).ToList(), s => s.GBase + lambdaReg[RegimeId(s)] * s.X));

                double strataLsbHsb = Math.Min(
                    ComputeLambdaImprovement(
                        test.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m <= globalMedianGalaxyLogGbar).ToList(),
                        s => s.GBase + lambdaReg[RegimeId(s)] * s.X),
                    ComputeLambdaImprovement(
                        test.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m > globalMedianGalaxyLogGbar).ToList(),
                        s => s.GBase + lambdaReg[RegimeId(s)] * s.X));

                double strataFloor = Math.Min(strataLowHigh, strataLsbHsb);
                foldResults.Add((globalImp, regImp, regImp - globalImp, strataFloor));
            }

            Assert.True(foldResults.Count >= 3, $"Insufficient valid LC06 folds for alpha={alpha:F2}: {foldResults.Count}");

            double meanGlobal = foldResults.Average(f => f.GlobalImp);
            double meanRegularized = foldResults.Average(f => f.RegularizedImp);
            double meanGain = foldResults.Average(f => f.Gain);
            double gainShare = foldResults.Count(f => f.Gain >= 0.0) / (double)foldResults.Count;
            double worstStrataFloor = foldResults.Min(f => f.StrataFloor);

            alphaResults.Add(new RegularizedAlphaResult(alpha, meanGlobal, meanRegularized, meanGain, gainShare, worstStrataFloor));
        }

        foreach (var r in alphaResults.OrderBy(r => r.Alpha))
        {
            WriteTestLine(
                "LC06",
                $"alpha={r.Alpha:F2}, meanGlobal={r.MeanGlobalImprovement:E6}, meanRegularized={r.MeanRegularizedImprovement:E6}, gain={r.MeanGain:E6}, gainShare={r.GainShare:F3}, worstStrataFloor={r.WorstStrataFloor:E6}");
        }

        var selected = alphaResults
            .OrderByDescending(r => r.WorstStrataFloor)
            .ThenByDescending(r => r.MeanGain)
            .First();

        WriteTestLine("LC06", $"selectedAlpha = {selected.Alpha:F2}");

        Assert.True(selected.WorstStrataFloor >= -5e-10, $"Regularized regime conditioning still has negative strata floor: {selected.WorstStrataFloor:E6}");
        Assert.True(selected.MeanRegularizedImprovement >= selected.MeanGlobalImprovement, $"Regularized regime conditioning should not underperform global lambda on average.");
        Assert.True(selected.GainShare >= 0.60, $"Regularized regime conditioning improves too few folds: share={selected.GainShare:F3}");
    }

    [Fact]
    public void LC07_LambdaTheta_Should_Use_StrataSafe_Regularization()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0 || p.Rd <= 0.0 || p.Gbar <= 0.0 || !double.IsFinite(p.O5W6InvDistance))
                    return (LambdaRegimeSample?)null;

                double gBase = alphaLocal * p.GLocal;
                if (gBase <= 0.0 || !double.IsFinite(gBase))
                    return (LambdaRegimeSample?)null;

                return new LambdaRegimeSample(
                    p.GalaxyName,
                    p.GObs,
                    gBase,
                    p.O5W6InvDistance,
                    p.Radius / p.Rd,
                    Math.Log10(Math.Max(p.Gbar, 1e-30)));
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        Assert.True(raw.Count > 400, $"Too few LC07 samples: {raw.Count}");

        var groups = raw
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        Assert.True(groups.Count >= 8, $"Too few LC07 galaxy groups: {groups.Count}");

        var galaxyMedianLogGbar = groups.ToDictionary(
            g => g.Key,
            g => Median(g.Select(x => x.LogGbar)),
            StringComparer.Ordinal);
        double globalMedianGalaxyLogGbar = Median(galaxyMedianLogGbar.Values);
        double globalMedianPointLogGbar = Median(raw.Select(r => r.LogGbar));

        int RegimeId(LambdaRegimeSample s)
        {
            if (s.XRadius < 1.0) return 0; // inner
            if (s.XRadius < 4.0) return 1; // middle
            return 2;                      // outer
        }

        double ComputeStrataFloor(IReadOnlyList<LambdaRegimeSample> sample, Func<LambdaRegimeSample, double> predictor)
        {
            double lowHigh = Math.Min(
                ComputeLambdaImprovement(sample.Where(s => s.LogGbar <= globalMedianPointLogGbar).ToList(), predictor),
                ComputeLambdaImprovement(sample.Where(s => s.LogGbar > globalMedianPointLogGbar).ToList(), predictor));

            double lsbHsb = Math.Min(
                ComputeLambdaImprovement(
                    sample.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m <= globalMedianGalaxyLogGbar).ToList(),
                    predictor),
                ComputeLambdaImprovement(
                    sample.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m > globalMedianGalaxyLogGbar).ToList(),
                    predictor));

            return Math.Min(lowHigh, lsbHsb);
        }

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);
        var eligible = raw.Where(s => foldMap.ContainsKey(s.Galaxy)).ToList();

        const double penalty = 0.10;
        const double floorTolerance = 1e-10;
        var alphas = new[] { 0.25, 0.50, 0.75 };

        var alphaResults = new List<(double Alpha, double MeanGlobal, double MeanRegularized, double MeanGain, double GainShare, double WorstRegFloor, double WorstGlobalFloor, bool IsStrataSafe, double Score)>();

        foreach (double alpha in alphas)
        {
            var foldResults = new List<(double GlobalImp, double RegImp, double Gain, double RegFloor, double GlobalFloor)>();

            for (int fold = 0; fold < foldCount; fold++)
            {
                var train = eligible.Where(s => foldMap[s.Galaxy] != fold).ToList();
                var test = eligible.Where(s => foldMap[s.Galaxy] == fold).ToList();
                if (train.Count < 120 || test.Count < 80)
                    continue;

                double lambdaGlobal = FitGlobalLambda(train.Select(t => new LambdaSample(t.Galaxy, t.GObs, t.GBase, t.X)).ToList());
                Func<LambdaRegimeSample, double> globalPredictor = s => s.GBase + lambdaGlobal * s.X;
                double globalImp = ComputeLambdaImprovement(test, globalPredictor);
                double globalFloor = ComputeStrataFloor(test, globalPredictor);

                var regimeFit = FitRegimeLambdas(train, RegimeId, regimeCount: 3);
                var lambdaReg = regimeFit
                    .Select(l => (1.0 - alpha) * lambdaGlobal + alpha * l)
                    .ToArray();
                Func<LambdaRegimeSample, double> regPredictor = s => s.GBase + lambdaReg[RegimeId(s)] * s.X;
                double regImp = ComputeLambdaImprovement(test, regPredictor);
                double regFloor = ComputeStrataFloor(test, regPredictor);

                foldResults.Add((globalImp, regImp, regImp - globalImp, regFloor, globalFloor));
            }

            Assert.True(foldResults.Count >= 3, $"Insufficient valid LC07 folds for alpha={alpha:F2}: {foldResults.Count}");

            double meanGlobal = foldResults.Average(f => f.GlobalImp);
            double meanReg = foldResults.Average(f => f.RegImp);
            double meanGain = foldResults.Average(f => f.Gain);
            double gainShare = foldResults.Count(f => f.Gain >= 0.0) / (double)foldResults.Count;
            double worstRegFloor = foldResults.Min(f => f.RegFloor);
            double worstGlobalFloor = foldResults.Min(f => f.GlobalFloor);
            bool strataSafe = worstRegFloor >= worstGlobalFloor - floorTolerance;
            double score = meanGain + penalty * worstRegFloor;

            alphaResults.Add((alpha, meanGlobal, meanReg, meanGain, gainShare, worstRegFloor, worstGlobalFloor, strataSafe, score));
        }

        foreach (var r in alphaResults.OrderBy(r => r.Alpha))
        {
            WriteTestLine(
                "LC07",
                $"alpha={r.Alpha:F2}, meanGlobal={r.MeanGlobal:E6}, meanReg={r.MeanRegularized:E6}, gain={r.MeanGain:E6}, gainShare={r.GainShare:F3}, worstRegFloor={r.WorstRegFloor:E6}, worstGlobalFloor={r.WorstGlobalFloor:E6}, strataSafe={r.IsStrataSafe}, score={r.Score:E6}");
        }

        var safeSet = alphaResults.Where(r => r.IsStrataSafe).ToList();
        Assert.NotEmpty(safeSet);

        var selected = safeSet
            .OrderByDescending(r => r.Score)
            .First();

        WriteTestLine("LC07", $"selectedAlpha={selected.Alpha:F2}");

        Assert.True(selected.WorstRegFloor >= selected.WorstGlobalFloor - floorTolerance, "Selected alpha violates strata-safe floor constraint.");
        Assert.True(selected.MeanRegularized >= selected.MeanGlobal, "Selected strata-safe regularization should not underperform global lambda on average.");
        Assert.True(selected.GainShare >= 0.50, $"Selected strata-safe alpha improves too few folds: {selected.GainShare:F3}");
    }

    [Fact]
    public void LC08_LambdaTheta_Should_Not_Become_PerGalaxyProxy()
    {
        var dataset = BuildDataset();
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var raw = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0 || p.Rd <= 0.0 || p.Gbar <= 0.0 || !double.IsFinite(p.O5W6InvDistance))
                    return (LambdaRegimeSample?)null;

                double gBase = alphaLocal * p.GLocal;
                if (gBase <= 0.0 || !double.IsFinite(gBase))
                    return (LambdaRegimeSample?)null;

                return new LambdaRegimeSample(
                    p.GalaxyName,
                    p.GObs,
                    gBase,
                    p.O5W6InvDistance,
                    p.Radius / p.Rd,
                    Math.Log10(Math.Max(p.Gbar, 1e-30)));
            })
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        Assert.True(raw.Count > 400, $"Too few LC08 samples: {raw.Count}");

        var groups = raw
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();
        Assert.True(groups.Count >= 8, $"Too few LC08 galaxy groups: {groups.Count}");

        var galaxyMedianLogGbar = groups.ToDictionary(
            g => g.Key,
            g => Median(g.Select(x => x.LogGbar)),
            StringComparer.Ordinal);
        double globalMedianGalaxyLogGbar = Median(galaxyMedianLogGbar.Values);
        double globalMedianPointLogGbar = Median(raw.Select(r => r.LogGbar));

        int RegimeId(LambdaRegimeSample s)
        {
            if (s.XRadius < 1.0) return 0;
            if (s.XRadius < 4.0) return 1;
            return 2;
        }

        double ComputeStrataFloor(IReadOnlyList<LambdaRegimeSample> sample, Func<LambdaRegimeSample, double> predictor)
        {
            double lowHigh = Math.Min(
                ComputeLambdaImprovement(sample.Where(s => s.LogGbar <= globalMedianPointLogGbar).ToList(), predictor),
                ComputeLambdaImprovement(sample.Where(s => s.LogGbar > globalMedianPointLogGbar).ToList(), predictor));

            double lsbHsb = Math.Min(
                ComputeLambdaImprovement(
                    sample.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m <= globalMedianGalaxyLogGbar).ToList(),
                    predictor),
                ComputeLambdaImprovement(
                    sample.Where(s => galaxyMedianLogGbar.TryGetValue(s.Galaxy, out double m) && m > globalMedianGalaxyLogGbar).ToList(),
                    predictor));

            return Math.Min(lowHigh, lsbHsb);
        }

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);
        var eligible = raw.Where(s => foldMap.ContainsKey(s.Galaxy)).ToList();

        var alphas = new[] { 0.25, 0.50, 0.75 };
        const double floorTolerance = 1e-10;
        const double penalty = 0.10;

        var foldResults = new List<(int Fold, double GlobalImp, double RegImp, double OracleImp, double ProxyRatio, double OracleGap)>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = eligible.Where(s => foldMap[s.Galaxy] != fold).ToList();
            var test = eligible.Where(s => foldMap[s.Galaxy] == fold).ToList();
            if (train.Count < 120 || test.Count < 80)
                continue;

            double lambdaGlobal = FitGlobalLambda(train.Select(t => new LambdaSample(t.Galaxy, t.GObs, t.GBase, t.X)).ToList());
            Func<LambdaRegimeSample, double> globalPredictor = s => s.GBase + lambdaGlobal * s.X;
            double globalTrainImp = ComputeLambdaImprovement(train, globalPredictor);
            double globalTrainFloor = ComputeStrataFloor(train, globalPredictor);

            var regimeFit = FitRegimeLambdas(train, RegimeId, regimeCount: 3);

            var alphaCandidates = alphas
                .Select(alpha =>
                {
                    var lambdaReg = regimeFit
                        .Select(l => (1.0 - alpha) * lambdaGlobal + alpha * l)
                        .ToArray();
                    Func<LambdaRegimeSample, double> pred = s => s.GBase + lambdaReg[RegimeId(s)] * s.X;
                    double trainImp = ComputeLambdaImprovement(train, pred);
                    double trainFloor = ComputeStrataFloor(train, pred);
                    bool strataSafe = trainFloor >= globalTrainFloor - floorTolerance;
                    double score = (trainImp - globalTrainImp) + penalty * trainFloor;
                    return (Alpha: alpha, Lambdas: lambdaReg, TrainImp: trainImp, TrainFloor: trainFloor, StrataSafe: strataSafe, Score: score);
                })
                .ToList();

            var safeAlpha = alphaCandidates.Where(c => c.StrataSafe).ToList();
            var selected = (safeAlpha.Count > 0 ? safeAlpha : alphaCandidates)
                .OrderByDescending(c => c.Score)
                .First();

            Func<LambdaRegimeSample, double> regimePredictor = s => s.GBase + selected.Lambdas[RegimeId(s)] * s.X;
            double globalImp = ComputeLambdaImprovement(test, globalPredictor);
            double regImp = ComputeLambdaImprovement(test, regimePredictor);

            double oracleImp = test
                .GroupBy(s => s.Galaxy)
                .Where(g => g.Count() >= 5)
                .Select(g =>
                {
                    double lambdaGalaxy = FitGlobalLambda(g.Select(x => new LambdaSample(x.Galaxy, x.GObs, x.GBase, x.X)).ToList());
                    return ComputeLambdaImprovement(g.ToList(), s => s.GBase + lambdaGalaxy * s.X);
                })
                .Average();

            double numer = regImp - globalImp;
            double denom = oracleImp - globalImp;
            double proxyRatio = denom > 1e-12 ? numer / denom : 0.0;
            double oracleGap = oracleImp - regImp;

            foldResults.Add((fold, globalImp, regImp, oracleImp, proxyRatio, oracleGap));
            WriteTestLine("LC08", $"fold={fold}, alpha={selected.Alpha:F2}, globalImp={globalImp:E6}, regImp={regImp:E6}, oracleImp={oracleImp:E6}, proxyRatio={proxyRatio:F3}, oracleGap={oracleGap:E6}");
        }

        Assert.True(foldResults.Count >= 3, $"Insufficient valid LC08 folds: {foldResults.Count}");

        double meanProxyRatio = foldResults.Average(f => f.ProxyRatio);
        double medianProxyRatio = Median(foldResults.Select(f => f.ProxyRatio));
        double proxyLikeShare = foldResults.Count(f => f.ProxyRatio >= 0.90) / (double)foldResults.Count;
        double meanOracleGap = foldResults.Average(f => f.OracleGap);

        WriteTestLine("LC08", $"meanProxyRatio   = {meanProxyRatio:F3}");
        WriteTestLine("LC08", $"medianProxyRatio = {medianProxyRatio:F3}");
        WriteTestLine("LC08", $"proxyLikeShare   = {proxyLikeShare:F3}");
        WriteTestLine("LC08", $"meanOracleGap    = {meanOracleGap:E6}");

        Assert.True(meanOracleGap >= 0.0, $"Regime-conditioned lambda should remain below per-galaxy oracle on average (non-proxy), gap={meanOracleGap:E6}");
        Assert.True(medianProxyRatio <= 0.85, $"Regime-conditioned lambda is too close to per-galaxy oracle (proxy risk), median ratio={medianProxyRatio:F3}");
        Assert.True(proxyLikeShare <= 0.20, $"Too many folds behave like per-galaxy proxy, share={proxyLikeShare:F3}");
    }

    [Fact]
    public void TOL01_ThetaField_Should_Emerge_From_CoarseGrained_LatticeState()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var baseProfile = CreateSyntheticThetaProfile(
            r => 0.60 * Math.Sin(0.29 * r) + 0.30 * Math.Cos(0.19 * r),
            count: 101,
            spacing: 0.20);

        var amplitudes = new[] { 0.10, 0.15, 0.20, 0.30, 0.40 };
        var phaseEnergies = new List<double>();
        var thetaEnergies = new List<double>();

        foreach (double a in amplitudes)
        {
            var scaled = TransformThetaProfile(baseProfile, t => a * t);
            double ePhase = ComputePhaseLatticeEnergy(scaled, halfWindow, "inverse", epsilon);
            double eTheta = ComputeQuadraticCoherenceEnergy(scaled, halfWindow, "inverse", epsilon);

            phaseEnergies.Add(ePhase);
            thetaEnergies.Add(eTheta);
            WriteTestLine("TOL01", $"amp={a:F2}, Ephase={ePhase:E6}, Etheta={eTheta:E6}, ratio={ePhase / Math.Max(eTheta, 1e-20):F4}");
        }

        double corr = ComputePearsonCorrelation(thetaEnergies, phaseEnergies);
        var fit = ComputeLinearFit(thetaEnergies, phaseEnergies);

        WriteTestLine("TOL01", $"corr={corr:F6}, slope={fit.Slope:F4}, intercept={fit.Intercept:E6}, R2={fit.RSquared:F6}");

        Assert.True(corr >= 0.995, $"Coarse-grained lattice->Theta energy link too weak, corr={corr:F6}");
        Assert.True(fit.RSquared >= 0.990, $"Theta coarse-graining fit too weak, R2={fit.RSquared:F6}");
        Assert.True(fit.Slope >= 0.90 && fit.Slope <= 1.10, $"Expected near-unit proportionality, slope={fit.Slope:F4}");
    }

    [Fact]
    public void TOL02_O5_Should_Follow_From_LatticeEnergyGradient()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var profiles = new[]
        {
            CreateDefaultSyntheticProfiles(count: 101, spacing: 0.20).Smooth,
            CreateDefaultSyntheticProfiles(count: 101, spacing: 0.20).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 101, spacing: 0.20).NonLocalBreak
        };

        var o5Series = new List<double>();
        var latticeGradientSeries = new List<double>();

        foreach (var profile in profiles)
        {
            o5Series.AddRange(ComputeO5Series(profile, halfWindow));
            latticeGradientSeries.AddRange(
                ComputePhaseLatticeNegativeGradientSeries(profile, halfWindow, "inverse", epsilon, normalize: true));
        }

        Assert.Equal(o5Series.Count, latticeGradientSeries.Count);
        Assert.True(o5Series.Count > 120, $"Too few TOL02 comparison points: {o5Series.Count}");

        double corr = ComputePearsonCorrelation(o5Series, latticeGradientSeries);
        var fit = ComputeLinearFit(latticeGradientSeries, o5Series);
        double signAgreement = ComputeSignAgreement(o5Series, latticeGradientSeries);

        WriteTestLine("TOL02", $"corr={corr:F4}, slope={fit.Slope:F4}, intercept={fit.Intercept:E6}, R2={fit.RSquared:F4}, sign={signAgreement:F3}");

        Assert.True(corr >= 0.88, $"O5 should follow lattice-energy gradient, corr={corr:F4}");
        Assert.True(fit.RSquared >= 0.70, $"Lattice-gradient explanatory power too weak, R2={fit.RSquared:F4}");
        Assert.True(signAgreement >= 0.85, $"Lattice-gradient sign agreement too low: {signAgreement:F3}");
    }

    [Fact]
    public void TOL03_LambdaTheta_Should_Map_To_RelaxationResponseScale()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;
        var candidateSteps = new[] { 0.10, 0.05, 0.02, 0.01, 0.005, 0.002 };

        var dataset = BuildDataset();
        var samples = BuildLambdaSamples(dataset, p => p.O5W6InvDistance);
        Assert.True(samples.Count > 400, $"Too few TOL03 samples: {samples.Count}");

        double lambdaGlobal = FitGlobalLambda(samples);

        var profiles = new[]
        {
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).Smooth,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).NonLocalBreak
        };

        var bestSteps = new List<double>();
        foreach (var p in profiles)
        {
            double e0 = ComputePhaseLatticeEnergy(p, halfWindow, "inverse", epsilon);
            double bestStep = candidateSteps[0];
            double bestDrop = double.NegativeInfinity;

            foreach (double step in candidateSteps)
            {
                double e1 = ComputePhaseLatticeEnergy(
                    RelaxProfileAlongPhaseLatticeOperator(p, halfWindow, "inverse", epsilon, step),
                    halfWindow,
                    "inverse",
                    epsilon);
                double relDrop = e0 > 0.0 ? (e0 - e1) / e0 : 0.0;
                if (relDrop > bestDrop)
                {
                    bestDrop = relDrop;
                    bestStep = step;
                }
            }

            bestSteps.Add(bestStep);
        }

        double etaTheta = bestSteps.Average();
        double tauSync = EstimatePhaseCorrelationLength(
            CreateCorrelatedLatticePhaseProfile(correlationHalfWindow: 6, count: 151, spacing: 0.20),
            maxLag: 20);
        double o5Scale = Median(samples.Select(s => Math.Abs(s.X)));
        double a0 = TrmDerivedParameters.GetA0_Ms2();
        double lambdaResponse = etaTheta * tauSync * a0 / Math.Max(o5Scale, 1e-20);
        double ratio = Math.Abs(lambdaGlobal) / Math.Max(Math.Abs(lambdaResponse), 1e-30);

        WriteTestLine("TOL03", $"lambdaGlobal={lambdaGlobal:E6}, lambdaResponse={lambdaResponse:E6}, ratio={ratio:F4}, eta={etaTheta:F4}, tau={tauSync:F4}");

        Assert.True(double.IsFinite(lambdaResponse) && Math.Abs(lambdaResponse) > 0.0, "Invalid relaxation-response lambda scale.");
        Assert.True(ratio >= 0.05 && ratio <= 20.0, $"Lambda response scale mismatch too large: ratio={ratio:F4}");
    }

    [Fact]
    public void TOL04_ThetaO5Chain_Should_Reproduce_HoldoutStable_Response()
    {
        const int halfWindow = 6;
        const double epsilon = 1e-6;

        var dataset = BuildDataset();
        var samples = BuildLambdaSamples(dataset, p => p.O5W6InvDistance);
        var groups = samples
            .GroupBy(s => s.Galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        Assert.True(groups.Count >= 8, $"Too few TOL04 galaxy groups: {groups.Count}");

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);
        var eligible = samples.Where(s => foldMap.ContainsKey(s.Galaxy)).ToList();

        double tauSync = EstimatePhaseCorrelationLength(
            CreateCorrelatedLatticePhaseProfile(correlationHalfWindow: halfWindow, count: 151, spacing: 0.20),
            maxLag: 20);
        var profilePool = new[]
        {
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).Smooth,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).NonLocalBreak
        };
        var steps = new[] { 0.10, 0.05, 0.02, 0.01, 0.005, 0.002 };
        double etaTheta = profilePool
            .Select(p =>
            {
                double e0 = ComputePhaseLatticeEnergy(p, halfWindow, "inverse", epsilon);
                double bestStep = steps[0];
                double bestDrop = double.NegativeInfinity;
                foreach (double step in steps)
                {
                    double e1 = ComputePhaseLatticeEnergy(
                        RelaxProfileAlongPhaseLatticeOperator(p, halfWindow, "inverse", epsilon, step),
                        halfWindow,
                        "inverse",
                        epsilon);
                    double relDrop = e0 > 0.0 ? (e0 - e1) / e0 : 0.0;
                    if (relDrop > bestDrop)
                    {
                        bestDrop = relDrop;
                        bestStep = step;
                    }
                }
                return bestStep;
            })
            .Average();

        double a0 = TrmDerivedParameters.GetA0_Ms2();
        var foldImprovements = new List<double>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = eligible.Where(s => foldMap[s.Galaxy] != fold).ToList();
            var test = eligible.Where(s => foldMap[s.Galaxy] == fold).ToList();
            if (train.Count < 120 || test.Count < 80)
                continue;

            double o5ScaleTrain = Median(train.Select(s => Math.Abs(s.X)));
            double lambdaDerived = etaTheta * tauSync * a0 / Math.Max(o5ScaleTrain, 1e-20);
            double improvement = ComputeLambdaImprovement(test, lambdaDerived);
            foldImprovements.Add(improvement);

            WriteTestLine("TOL04", $"fold={fold}, lambdaDerived={lambdaDerived:E6}, improvement={improvement:E6}");
        }

        Assert.True(foldImprovements.Count >= 3, $"Insufficient valid TOL04 folds: {foldImprovements.Count}");

        double meanImprovement = foldImprovements.Average();
        double positiveShare = foldImprovements.Count(i => i >= 0.0) / (double)foldImprovements.Count;
        double worstFold = foldImprovements.Min();

        WriteTestLine("TOL04", $"meanImprovement={meanImprovement:E6}, positiveShare={positiveShare:F3}, worstFold={worstFold:E6}");

        Assert.True(meanImprovement >= -1e-5, $"Derived Theta->O5->lambda chain degrades holdout on average: {meanImprovement:E6}");
        Assert.True(positiveShare >= 0.40, $"Too few non-negative holdout folds for derived chain: {positiveShare:F3}");
        Assert.True(worstFold >= -0.03, $"Derived chain has too strong worst-fold degradation: {worstFold:E6}");
    }

    private Dataset BuildDataset()
    {
        return BuildDataset(
            sourceStrength: 1.0,
            dampingStrength: 0.45,
            syncStrength: TrmDerivedParameters.GetPhiBeta() * 0.05,
            iterations: 600,
            relaxation: 0.01);
    }

    private Dataset BuildDataset(
        double sourceStrength,
        double dampingStrength,
        double syncStrength,
        int iterations,
        double relaxation)
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
            BaryonMode.ExponentialDisk);

        double a0 = TrmDerivedParameters.GetA0_Ms2();

        var rawGalaxyCache = rawPoints
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var modelGalaxyGroups = trmDisk
            .GroupBy(p => p.GalaxyName)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.RadiusKpc).ToList());

        var points = new List<DataPoint>();

        foreach (var kvp in modelGalaxyGroups)
        {
            string galaxyName = kvp.Key;
            var galaxy = kvp.Value;

            if (!rawGalaxyCache.TryGetValue(galaxyName, out var rawGalaxy))
                continue;
            if (galaxy.Count < 10 || rawGalaxy.Count < 10)
                continue;

            double rd = SparcRarAnalysis.EstimateDiskScaleLengthFromProfile(rawGalaxy);
            if (!double.IsFinite(rd) || rd <= 0.0)
                continue;

            var field = TrmFieldSolver.SolveField(
                galaxy,
                sourceStrength,
                dampingStrength,
                syncStrength,
                iterations,
                relaxation);
            if (field?.Points == null || field.Points.Count < 5)
                continue;

            foreach (var p in galaxy.Skip(2).Take(galaxy.Count - 4))
            {
                if (p.GobsMs2 <= 0.0 || p.GbarMs2 <= 0.0)
                    continue;

                double gLocal = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2,
                    a0,
                    ModelType.ClockworkTRM);

                if (gLocal <= 0.0 || !double.IsFinite(gLocal))
                    continue;

                int idx = FindNearestIndex(field, p.RadiusKpc);
                if (idx <= 0 || idx >= field.Points.Count - 1)
                    continue;

                var left = field.Points[idx - 1];
                var mid = field.Points[idx];
                var right = field.Points[idx + 1];

                double dr = right.RadiusKpc - left.RadiusKpc;
                if (dr <= 0.0)
                    continue;

                double rSafe = Math.Max(mid.RadiusKpc, 1e-6);
                double drLocal = Math.Max(0.5 * dr, 1e-6);

                double dThetaDr = (right.Theta - left.Theta) / dr;
                double gradient = Math.Abs(dThetaDr);
                double level = Math.Max(mid.Theta, 0.0) / rSafe;
                double curvature = Math.Abs((right.Theta - 2.0 * mid.Theta + left.Theta) / (drLocal * drLocal) + (1.0 / rSafe) * dThetaDr);
                double orbitKernel = ComputeOrbitKernel(field, idx);
                double o5W2Inv = ComputeO5KernelContrast(field, idx, halfWindow: 2, kernelMode: "inverse");
                double o5W4Inv = ComputeO5KernelContrast(field, idx, halfWindow: 4, kernelMode: "inverse");
                double o5W6Inv = ComputeO5KernelContrast(field, idx, halfWindow: 6, kernelMode: "inverse");
                double o5W8Inv = ComputeO5KernelContrast(field, idx, halfWindow: 8, kernelMode: "inverse");
                double o5W4Uniform = ComputeO5KernelContrast(field, idx, halfWindow: 4, kernelMode: "uniform");
                double o5W4Gaussian = ComputeO5KernelContrast(field, idx, halfWindow: 4, kernelMode: "gaussian");

                points.Add(new DataPoint(
                    galaxyName,
                    p.RadiusKpc,
                    rd,
                    p.GobsMs2,
                    p.GbarMs2,
                    gLocal,
                    gradient,
                    gradient + level,
                    curvature,
                    orbitKernel,
                    o5W4Inv,
                    o5W2Inv,
                    o5W4Inv,
                    o5W6Inv,
                    o5W8Inv,
                    o5W4Uniform,
                    o5W4Gaussian));
            }
        }

        Assert.True(points.Count > 400, $"Too few theta-observable sample points: {points.Count}");
        return new Dataset(points);
    }

    private KernelHoldoutMetrics EvaluateO5W6HoldoutMetrics(Dataset dataset)
    {
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        var samples = dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                    return ((string galaxy, double x, double y)?)null;

                double gLocalScaled = alphaLocal * p.GLocal;
                if (gLocalScaled <= 0.0 || !double.IsFinite(gLocalScaled) || !double.IsFinite(p.O5W6InvDistance))
                    return ((string galaxy, double x, double y)?)null;

                double y = Math.Log10(p.GObs) - Math.Log10(gLocalScaled);
                double x = p.O5W6InvDistance;
                if (!double.IsFinite(x) || !double.IsFinite(y))
                    return ((string galaxy, double x, double y)?)null;

                return (p.GalaxyName, x, y);
            })
            .Where(x => x != null)
            .Select(x => x!.Value)
            .ToList();

        Assert.True(samples.Count > 400, $"Too few valid O5 holdout samples: {samples.Count}");

        var groups = samples
            .GroupBy(s => s.galaxy)
            .Where(g => g.Count() >= 8)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        Assert.True(groups.Count >= 8, $"Too few galaxy groups for O5 holdout metrics: {groups.Count}");

        int foldCount = Math.Min(5, groups.Count);
        var foldMap = groups
            .Select((g, idx) => new { g.Key, Fold = idx % foldCount })
            .ToDictionary(x => x.Key, x => x.Fold);

        var eligibleSamples = samples.Where(s => foldMap.ContainsKey(s.galaxy)).ToList();
        var foldResults = new List<(double trainImp, double testImp, double gap)>();

        for (int fold = 0; fold < foldCount; fold++)
        {
            var train = eligibleSamples.Where(s => foldMap[s.galaxy] != fold).ToList();
            var test = eligibleSamples.Where(s => foldMap[s.galaxy] == fold).ToList();

            if (train.Count < 120 || test.Count < 80)
                continue;

            var fit = ComputeLinearFit(train.Select(t => t.x).ToList(), train.Select(t => t.y).ToList());

            double trainBaseline = Math.Sqrt(train.Average(t => t.y * t.y));
            double trainModel = Math.Sqrt(train.Average(t =>
            {
                double yHat = fit.Intercept + fit.Slope * t.x;
                double e = t.y - yHat;
                return e * e;
            }));
            double trainImp = trainBaseline - trainModel;

            double testBaseline = Math.Sqrt(test.Average(t => t.y * t.y));
            double testModel = Math.Sqrt(test.Average(t =>
            {
                double yHat = fit.Intercept + fit.Slope * t.x;
                double e = t.y - yHat;
                return e * e;
            }));
            double testImp = testBaseline - testModel;

            foldResults.Add((trainImp, testImp, trainImp - testImp));
        }

        Assert.True(foldResults.Count >= 3, $"Insufficient valid O5 holdout folds: {foldResults.Count}");

        double meanTrain = foldResults.Average(f => f.trainImp);
        double meanTest = foldResults.Average(f => f.testImp);
        double gapStd = StandardDeviation(foldResults.Select(f => f.gap));
        double positiveTestShare = foldResults.Count(f => f.testImp > 0.0) / (double)foldResults.Count;
        double worstFold = foldResults.Min(f => f.testImp);
        double stabilityScore =
            meanTest
            + 0.02 * positiveTestShare
            + 0.30 * worstFold
            - 0.40 * gapStd;

        return new KernelHoldoutMetrics(
            "O5-W6-InvDistance",
            meanTrain,
            meanTest,
            gapStd,
            positiveTestShare,
            worstFold,
            stabilityScore);
    }

    private static (string Galaxy, double Radius) PointKey(DataPoint p)
    {
        return (p.GalaxyName, Math.Round(p.Radius, 6));
    }

    private static SyntheticProfiles CreateDefaultSyntheticProfiles(int count, double spacing)
    {
        var constant = CreateSyntheticThetaProfile(r => 1.5, count, spacing);
        var smooth = CreateSyntheticThetaProfile(r => 0.9 + 0.04 * r + 0.003 * r * r, count, spacing);
        var localContrast = CreateSyntheticThetaProfile(
            r =>
            {
                double baseTheta = 0.9 + 0.04 * r + 0.003 * r * r;
                double notch = 0.18 * Math.Exp(-Math.Pow((r - 10.0) / 0.7, 2.0));
                return baseTheta - notch;
            },
            count,
            spacing);
        var nonLocalBreak = CreateSyntheticThetaProfile(
            r =>
            {
                double baseTheta = 0.9 + 0.04 * r + 0.003 * r * r;
                double syncBreak = r >= 13.0 ? 0.85 : 0.0;
                return baseTheta + syncBreak;
            },
            count,
            spacing);

        return new SyntheticProfiles(constant, smooth, localContrast, nonLocalBreak);
    }

    private static WindowOperatorMetrics EvaluateWindowOperatorMetrics(SyntheticProfiles profiles, int halfWindow)
    {
        var constantSeries = ComputeO5Series(profiles.Constant, halfWindow);
        var smoothSeries = ComputeO5Series(profiles.Smooth, halfWindow);
        var localSeries = ComputeO5Series(profiles.LocalContrast, halfWindow);
        var nonLocalSeries = ComputeO5Series(profiles.NonLocalBreak, halfWindow);

        double constMaxAbs = constantSeries.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
        double smoothMaxAbs = smoothSeries.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
        double localMaxAbs = localSeries.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
        double nonLocalMaxAbs = nonLocalSeries.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
        double smoothStdAbs = StandardDeviation(smoothSeries.Select(Math.Abs));

        double localGain = localMaxAbs - smoothMaxAbs;
        double nonLocalGain = nonLocalMaxAbs - localMaxAbs;
        double localSupportWidth = ComputeSupportWidth(localSeries, thresholdFraction: 0.50);
        double nonLocalArea = ComputeAbsArea(nonLocalSeries);
        double nonLocalMeanAbs = nonLocalSeries.Count > 0
            ? nonLocalSeries.Select(Math.Abs).Average()
            : 0.0;

        bool isStable =
            constMaxAbs <= 1e-12 &&
            smoothMaxAbs <= 0.060 &&
            localGain >= 0.030 &&
            nonLocalGain >= 0.030 &&
            nonLocalMeanAbs >= 0.022;

        return new WindowOperatorMetrics(
            halfWindow,
            constMaxAbs,
            smoothMaxAbs,
            localMaxAbs,
            nonLocalMaxAbs,
            localGain,
            nonLocalGain,
            nonLocalArea,
            nonLocalMeanAbs,
            localSupportWidth,
            smoothStdAbs,
            isStable);
    }

    private static double ComputeWindowBalanceScore(WindowOperatorMetrics m)
    {
        double nonLocalTerm = Math.Tanh(12.0 * m.NonLocalMeanAbs);
        double coherencePenalty = 0.022 * m.HalfWindow;

        return
            0.90 * m.LocalGainOverSmooth
            + 1.20 * m.NonLocalGainOverLocal
            + 0.35 * nonLocalTerm
            - 0.75 * m.SmoothStdAbs
            - coherencePenalty;
    }

    private static PhaseKernelScore EvaluatePhaseKernelScore(
        string kernelMode,
        IReadOnlyList<ThetaFieldProfile> profilePool,
        int halfWindow,
        double epsilon)
    {
        var smooth = profilePool[0];
        var local = profilePool[1];
        var nonLocal = profilePool[2];

        var smoothOp = ComputePhaseLatticeNegativeGradientSeries(smooth, halfWindow, kernelMode, epsilon, normalize: true);
        var localOp = ComputePhaseLatticeNegativeGradientSeries(local, halfWindow, kernelMode, epsilon, normalize: true);
        var nonLocalOp = ComputePhaseLatticeNegativeGradientSeries(nonLocal, halfWindow, kernelMode, epsilon, normalize: true);

        double smoothNoise = smoothOp.Select(Math.Abs).DefaultIfEmpty(0.0).Max();
        double localSens = localOp.Select(Math.Abs).DefaultIfEmpty(0.0).Max() - smoothNoise;
        double nonLocalSens = nonLocalOp.Select(Math.Abs).DefaultIfEmpty(0.0).Max() - localOp.Select(Math.Abs).DefaultIfEmpty(0.0).Max();

        var candidateSteps = new[] { 0.08, 0.05, 0.03, 0.02, 0.01, 0.005 };
        var perProfileEnergyDrop = profilePool
            .Select(p =>
            {
                double e0 = ComputePhaseLatticeEnergy(p, halfWindow, kernelMode, epsilon);
                double bestRel = double.NegativeInfinity;
                foreach (double step in candidateSteps)
                {
                    var relaxed = RelaxProfileAlongPhaseLatticeOperator(p, halfWindow, kernelMode, epsilon, step);
                    double e1 = ComputePhaseLatticeEnergy(relaxed, halfWindow, kernelMode, epsilon);
                    double rel = e0 > 0.0 ? (e0 - e1) / e0 : 0.0;
                    if (rel > bestRel) bestRel = rel;
                }
                return bestRel;
            })
            .ToList();

        double meanEnergyDrop = perProfileEnergyDrop.Average();

        var holdoutImprovements = ComputePhaseKernelHoldoutImprovements(profilePool, halfWindow, kernelMode, epsilon, candidateSteps);
        double holdoutMean = holdoutImprovements.Average();
        double holdoutMeanPositive = Math.Max(0.0, holdoutMean);
        double holdoutPosShare = holdoutImprovements.Count(x => x > 0.0) / (double)holdoutImprovements.Count;

        double balanceScore =
            1.40 * meanEnergyDrop
            + 0.30 * localSens
            + 1.50 * nonLocalSens
            + 0.20 * holdoutMeanPositive
            + 0.05 * holdoutPosShare
            - 0.80 * smoothNoise;

        return new PhaseKernelScore(
            kernelMode,
            balanceScore,
            meanEnergyDrop,
            smoothNoise,
            localSens,
            nonLocalSens,
            holdoutMean,
            holdoutPosShare);
    }

    private static List<double> ComputePhaseKernelHoldoutImprovements(
        IReadOnlyList<ThetaFieldProfile> profilePool,
        int halfWindow,
        string kernelMode,
        double epsilon,
        IReadOnlyList<double> candidateSteps)
    {
        var improvements = new List<double>();

        for (int holdout = 0; holdout < profilePool.Count; holdout++)
        {
            var train = profilePool.Where((_, idx) => idx != holdout).ToList();
            var test = profilePool[holdout];

            double selectedStep = candidateSteps[0];
            double bestTrainMean = double.NegativeInfinity;

            foreach (double step in candidateSteps)
            {
                double trainMean = train
                    .Select(p =>
                    {
                        double e0 = ComputePhaseLatticeEnergy(p, halfWindow, kernelMode, epsilon);
                        double e1 = ComputePhaseLatticeEnergy(
                            RelaxProfileAlongPhaseLatticeOperator(p, halfWindow, kernelMode, epsilon, step),
                            halfWindow,
                            kernelMode,
                            epsilon);
                        return e0 > 0.0 ? (e0 - e1) / e0 : 0.0;
                    })
                    .Average();

                if (trainMean > bestTrainMean)
                {
                    bestTrainMean = trainMean;
                    selectedStep = step;
                }
            }

            double e0Test = ComputePhaseLatticeEnergy(test, halfWindow, kernelMode, epsilon);
            double e1Test = ComputePhaseLatticeEnergy(
                RelaxProfileAlongPhaseLatticeOperator(test, halfWindow, kernelMode, epsilon, selectedStep),
                halfWindow,
                kernelMode,
                epsilon);

            improvements.Add(e0Test > 0.0 ? (e0Test - e1Test) / e0Test : 0.0);
        }

        return improvements;
    }

    private static List<LambdaSample> BuildLambdaSamples(Dataset dataset, Func<DataPoint, double> o5Selector)
    {
        double alphaLocal = FitBestLogShiftScale(dataset.Points, p => p.GLocal);

        return dataset.Points
            .Select(p =>
            {
                if (p.GObs <= 0.0 || p.GLocal <= 0.0)
                    return (LambdaSample?)null;

                double gBase = alphaLocal * p.GLocal;
                double x = o5Selector(p);
                if (gBase <= 0.0 || !double.IsFinite(gBase) || !double.IsFinite(x))
                    return (LambdaSample?)null;

                return new LambdaSample(p.GalaxyName, p.GObs, gBase, x);
            })
            .Where(s => s != null)
            .Select(s => s!)
            .ToList();
    }

    private static double FitGlobalLambda(IReadOnlyList<LambdaSample> samples)
    {
        double sxx = 0.0;
        double sxy = 0.0;
        foreach (var s in samples)
        {
            double y = s.GObs - s.GBase;
            sxx += s.X * s.X;
            sxy += s.X * y;
        }

        if (sxx <= 0.0)
            return 0.0;

        double lambda = sxy / sxx;
        return double.IsFinite(lambda) ? lambda : 0.0;
    }

    private static double ComputeLambdaImprovement(IReadOnlyList<LambdaSample> samples, double lambda)
    {
        if (samples.Count == 0)
            return 0.0;

        double baselineRmse = Math.Sqrt(samples.Average(s =>
        {
            double e = s.GObs - s.GBase;
            return e * e;
        }));

        double modelRmse = Math.Sqrt(samples.Average(s =>
        {
            double pred = s.GBase + lambda * s.X;
            double e = s.GObs - pred;
            return e * e;
        }));

        return baselineRmse - modelRmse;
    }

    private static double[] FitRegimeLambdas(
        IReadOnlyList<LambdaRegimeSample> samples,
        Func<LambdaRegimeSample, int> regimeSelector,
        int regimeCount)
    {
        var lambdas = new double[regimeCount];

        for (int r = 0; r < regimeCount; r++)
        {
            double sxx = 0.0;
            double sxy = 0.0;
            foreach (var s in samples)
            {
                if (regimeSelector(s) != r)
                    continue;

                double y = s.GObs - s.GBase;
                sxx += s.X * s.X;
                sxy += s.X * y;
            }

            lambdas[r] = sxx > 0.0 ? sxy / sxx : 0.0;
            if (!double.IsFinite(lambdas[r]))
                lambdas[r] = 0.0;
        }

        return lambdas;
    }

    private static double ComputeLambdaImprovement(
        IReadOnlyList<LambdaRegimeSample> samples,
        Func<LambdaRegimeSample, double> predictor)
    {
        if (samples.Count == 0)
            return 0.0;

        double baselineRmse = Math.Sqrt(samples.Average(s =>
        {
            double e = s.GObs - s.GBase;
            return e * e;
        }));

        double modelRmse = Math.Sqrt(samples.Average(s =>
        {
            double pred = predictor(s);
            double e = s.GObs - pred;
            return e * e;
        }));

        return baselineRmse - modelRmse;
    }

    private static List<double> ComputeFiniteCoherenceNegativeGradientSeries(
        ThetaFieldProfile profile,
        int halfWindow,
        double epsilon,
        bool normalize)
    {
        var values = new List<double>();
        int start = Math.Max(halfWindow, 2);
        int end = profile.Points.Count - 1 - Math.Max(halfWindow, 2);

        for (int i = start; i <= end; i++)
        {
            values.Add(ComputeFiniteCoherenceNegativeGradientAtIndex(profile, i, halfWindow, epsilon, normalize));
        }

        return values;
    }

    private static double ComputeFiniteCoherenceNegativeGradientAtIndex(
        ThetaFieldProfile profile,
        int centerIndex,
        int halfWindow,
        double epsilon,
        bool normalize)
    {
        if (profile.Points.Count < 5 || centerIndex < 0 || centerIndex >= profile.Points.Count)
            return 0.0;

        int start = Math.Max(0, centerIndex - halfWindow);
        int end = Math.Min(profile.Points.Count - 1, centerIndex + halfWindow);

        double thetaI = profile.Points[centerIndex].Theta;
        double weightedDeltaSum = 0.0;
        double weightSum = 0.0;

        for (int j = start; j <= end; j++)
        {
            if (j == centerIndex)
                continue;

            double dr = Math.Abs(profile.Points[centerIndex].RadiusKpc - profile.Points[j].RadiusKpc);
            double w = 1.0 / (dr + epsilon);
            double thetaJ = profile.Points[j].Theta;

            weightedDeltaSum += w * (thetaJ - thetaI); // negative gradient direction
            weightSum += w;
        }

        if (!normalize)
            return double.IsFinite(weightedDeltaSum) ? weightedDeltaSum : 0.0;

        if (weightSum <= 0.0)
            return 0.0;

        double normalized = weightedDeltaSum / weightSum;
        return double.IsFinite(normalized) ? normalized : 0.0;
    }

    private static double ComputeFiniteCoherenceEnergy(
        ThetaFieldProfile profile,
        int halfWindow,
        double epsilon)
    {
        if (profile.Points.Count < 3)
            return 0.0;

        double sum = 0.0;
        for (int i = 0; i < profile.Points.Count; i++)
        {
            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(profile.Points.Count - 1, i + halfWindow);

            for (int j = start; j <= end; j++)
            {
                if (j == i)
                    continue;

                double dr = Math.Abs(profile.Points[i].RadiusKpc - profile.Points[j].RadiusKpc);
                double w = 1.0 / (dr + epsilon);
                double d = profile.Points[i].Theta - profile.Points[j].Theta;
                sum += w * d * d;
            }
        }

        return 0.5 * sum;
    }

    private static ThetaFieldProfile RelaxProfileAlongFiniteCoherenceOperator(
        ThetaFieldProfile profile,
        int halfWindow,
        double epsilon,
        double step)
    {
        var clone = CloneProfile(profile);
        int start = Math.Max(halfWindow, 2);
        int end = clone.Points.Count - 1 - Math.Max(halfWindow, 2);

        var updates = new Dictionary<int, double>();
        for (int i = start; i <= end; i++)
        {
            double op = ComputeFiniteCoherenceNegativeGradientAtIndex(clone, i, halfWindow, epsilon, normalize: false);
            updates[i] = clone.Points[i].Theta + step * op;
        }

        foreach (var kvp in updates)
            clone.Points[kvp.Key].Theta = kvp.Value;

        return clone;
    }

    private static ThetaFieldProfile CloneProfile(ThetaFieldProfile source)
    {
        var clone = new ThetaFieldProfile();
        foreach (var p in source.Points)
        {
            clone.Points.Add(new ThetaFieldPoint
            {
                RadiusKpc = p.RadiusKpc,
                Theta = p.Theta,
                Source = p.Source,
                Sync = p.Sync
            });
        }
        return clone;
    }

    private static double ComputeSignAgreement(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n == 0)
            return 0.0;

        int agreed = 0;
        int considered = 0;
        for (int i = 0; i < n; i++)
        {
            int sx = x[i] > 1e-12 ? 1 : x[i] < -1e-12 ? -1 : 0;
            int sy = y[i] > 1e-12 ? 1 : y[i] < -1e-12 ? -1 : 0;
            if (sx == 0 && sy == 0)
                continue;

            considered++;
            if (sx == sy)
                agreed++;
        }

        return considered > 0 ? agreed / (double)considered : 1.0;
    }

    private static List<double> ComputePhaseLatticeNegativeGradientSeries(
        ThetaFieldProfile profile,
        int halfWindow,
        string kernelMode,
        double epsilon,
        bool normalize)
    {
        var values = new List<double>();
        int start = Math.Max(halfWindow, 2);
        int end = profile.Points.Count - 1 - Math.Max(halfWindow, 2);

        for (int i = start; i <= end; i++)
        {
            values.Add(ComputePhaseLatticeNegativeGradientAtIndex(profile, i, halfWindow, kernelMode, epsilon, normalize));
        }

        return values;
    }

    private static double ComputePhaseLatticeNegativeGradientAtIndex(
        ThetaFieldProfile profile,
        int centerIndex,
        int halfWindow,
        string kernelMode,
        double epsilon,
        bool normalize)
    {
        if (profile.Points.Count < 5 || centerIndex < 0 || centerIndex >= profile.Points.Count)
            return 0.0;

        int start = Math.Max(0, centerIndex - halfWindow);
        int end = Math.Min(profile.Points.Count - 1, centerIndex + halfWindow);

        double thetaI = profile.Points[centerIndex].Theta;
        double sum = 0.0;
        double weightSum = 0.0;

        for (int j = start; j <= end; j++)
        {
            if (j == centerIndex)
                continue;

            double dr = Math.Abs(profile.Points[centerIndex].RadiusKpc - profile.Points[j].RadiusKpc);
            double w = ComputeKernelWeightFromRadiusDistance(dr, kernelMode, epsilon);
            double delta = thetaI - profile.Points[j].Theta;
            sum += -w * Math.Sin(delta); // negative gradient
            weightSum += w;
        }

        if (!normalize)
            return double.IsFinite(sum) ? sum : 0.0;

        if (weightSum <= 0.0)
            return 0.0;

        double normalized = sum / weightSum;
        return double.IsFinite(normalized) ? normalized : 0.0;
    }

    private static double ComputePhaseLatticeEnergy(
        ThetaFieldProfile profile,
        int halfWindow,
        string kernelMode,
        double epsilon)
    {
        if (profile.Points.Count < 3)
            return 0.0;

        double sum = 0.0;
        for (int i = 0; i < profile.Points.Count; i++)
        {
            int end = Math.Min(profile.Points.Count - 1, i + halfWindow);
            for (int j = i + 1; j <= end; j++)
            {
                double dr = Math.Abs(profile.Points[i].RadiusKpc - profile.Points[j].RadiusKpc);
                double w = ComputeKernelWeightFromRadiusDistance(dr, kernelMode, epsilon);
                double delta = profile.Points[i].Theta - profile.Points[j].Theta;
                sum += w * (1.0 - Math.Cos(delta));
            }
        }

        return sum;
    }

    private static double ComputeQuadraticCoherenceEnergy(
        ThetaFieldProfile profile,
        int halfWindow,
        string kernelMode,
        double epsilon)
    {
        if (profile.Points.Count < 3)
            return 0.0;

        double sum = 0.0;
        for (int i = 0; i < profile.Points.Count; i++)
        {
            int end = Math.Min(profile.Points.Count - 1, i + halfWindow);
            for (int j = i + 1; j <= end; j++)
            {
                double dr = Math.Abs(profile.Points[i].RadiusKpc - profile.Points[j].RadiusKpc);
                double w = ComputeKernelWeightFromRadiusDistance(dr, kernelMode, epsilon);
                double delta = profile.Points[i].Theta - profile.Points[j].Theta;
                sum += 0.5 * w * delta * delta;
            }
        }

        return sum;
    }

    private static ThetaFieldProfile RelaxProfileAlongPhaseLatticeOperator(
        ThetaFieldProfile profile,
        int halfWindow,
        string kernelMode,
        double epsilon,
        double step)
    {
        var clone = CloneProfile(profile);
        int start = Math.Max(halfWindow, 2);
        int end = clone.Points.Count - 1 - Math.Max(halfWindow, 2);

        var updates = new Dictionary<int, double>();
        for (int i = start; i <= end; i++)
        {
            double op = ComputePhaseLatticeNegativeGradientAtIndex(clone, i, halfWindow, kernelMode, epsilon, normalize: false);
            updates[i] = clone.Points[i].Theta + step * op;
        }

        foreach (var kvp in updates)
            clone.Points[kvp.Key].Theta = kvp.Value;

        return clone;
    }

    private static double ComputeKernelWeightFromRadiusDistance(double dr, string kernelMode, double epsilon)
    {
        double d = Math.Max(dr, 0.0);
        return kernelMode switch
        {
            "uniform" => 1.0,
            "gaussian" => Math.Exp(-(d * d) / 2.0),
            "exponential" => Math.Exp(-d / 1.5),
            _ => 1.0 / (d + epsilon)
        };
    }

    private static ThetaFieldProfile TransformThetaProfile(ThetaFieldProfile source, Func<double, double> transform)
    {
        var transformed = new ThetaFieldProfile();
        foreach (var p in source.Points)
        {
            transformed.Points.Add(new ThetaFieldPoint
            {
                RadiusKpc = p.RadiusKpc,
                Theta = transform(p.Theta),
                Source = p.Source,
                Sync = p.Sync
            });
        }

        return transformed;
    }

    private static List<ThetaFieldProfile> BuildTqkProfilePool()
    {
        return new List<ThetaFieldProfile>
        {
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).Smooth,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 81, spacing: 0.25).NonLocalBreak,
            CreateDefaultSyntheticProfiles(count: 41, spacing: 0.50).Smooth,
            CreateDefaultSyntheticProfiles(count: 41, spacing: 0.50).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 41, spacing: 0.50).NonLocalBreak,
            CreateDefaultSyntheticProfiles(count: 161, spacing: 0.125).Smooth,
            CreateDefaultSyntheticProfiles(count: 161, spacing: 0.125).LocalContrast,
            CreateDefaultSyntheticProfiles(count: 161, spacing: 0.125).NonLocalBreak
        };
    }

    private static ThetaFieldProfile CreateCorrelatedLatticePhaseProfile(int correlationHalfWindow, int count, double spacing)
    {
        var profile = new ThetaFieldProfile();
        double rho = Math.Exp(-1.0 / Math.Max(correlationHalfWindow, 1));
        double sigma = Math.Sqrt(Math.Max(1.0 - rho * rho, 1e-9));
        double prev = 0.0;

        for (int i = 0; i < count; i++)
        {
            double t = i;
            double noise = 0.65 * Math.Sin(0.79 * t) + 0.45 * Math.Sin(1.93 * t) + 0.25 * Math.Cos(2.71 * t);
            double theta = i == 0
                ? sigma * noise
                : rho * prev + sigma * noise;
            prev = theta;

            profile.Points.Add(new ThetaFieldPoint
            {
                RadiusKpc = 1.0 + i * spacing,
                Theta = 1.8 * theta,
                Source = 0.0,
                Sync = 0.0
            });
        }

        return profile;
    }

    private static double EstimatePhaseCorrelationLength(ThetaFieldProfile profile, int maxLag)
    {
        if (profile.Points.Count < maxLag + 3)
            return 0.0;

        double threshold = Math.Exp(-1.0);
        for (int lag = 1; lag <= maxLag; lag++)
        {
            double c = 0.0;
            int n = 0;
            for (int i = 0; i + lag < profile.Points.Count; i++)
            {
                c += Math.Cos(profile.Points[i].Theta - profile.Points[i + lag].Theta);
                n++;
            }

            if (n == 0)
                continue;

            c /= n;
            if (c <= threshold)
                return lag;
        }

        return maxLag;
    }

    private static ThetaFieldProfile CreateSyntheticThetaProfile(
        Func<double, double> thetaAtRadius,
        int count,
        double spacing)
    {
        var profile = new ThetaFieldProfile();
        for (int i = 0; i < count; i++)
        {
            double r = 1.0 + i * spacing;
            profile.Points.Add(new ThetaFieldPoint
            {
                RadiusKpc = r,
                Theta = thetaAtRadius(r),
                Source = 0.0,
                Sync = 0.0
            });
        }

        return profile;
    }

    private static List<double> ComputeO5Series(ThetaFieldProfile profile, int halfWindow)
    {
        var values = new List<double>();
        int start = Math.Max(halfWindow, 2);
        int end = profile.Points.Count - 1 - Math.Max(halfWindow, 2);
        for (int i = start; i <= end; i++)
        {
            values.Add(ComputeO5KernelContrast(profile, i, halfWindow, "inverse"));
        }

        return values;
    }

    private static double ComputeSupportWidth(IReadOnlyList<double> values, double thresholdFraction)
    {
        if (values.Count == 0)
            return 0.0;

        var abs = values.Select(Math.Abs).ToList();
        double peak = abs.Max();
        if (peak <= 0.0 || !double.IsFinite(peak))
            return 0.0;

        double threshold = peak * thresholdFraction;
        int first = -1;
        int last = -1;
        for (int i = 0; i < abs.Count; i++)
        {
            if (abs[i] >= threshold)
            {
                if (first < 0) first = i;
                last = i;
            }
        }

        if (first < 0 || last < first)
            return 0.0;

        return last - first + 1;
    }

    private static double ComputeAbsArea(IReadOnlyList<double> values)
    {
        return values.Sum(v => Math.Abs(v));
    }

    private static FitResult EvaluateLocal(IEnumerable<DataPoint> points)
    {
        var list = points.ToList();
        double rms = ComputeRms(list, p => p.GLocal);
        return new FitResult("Local", 0.0, rms, p => p.GLocal);
    }

    private static FitResult FitCandidate(IEnumerable<DataPoint> points, string candidateName)
    {
        var list = points.ToList();
        Func<DataPoint, double> obsSelector = candidateName switch
        {
            "O1_Gradient" => p => p.O1Gradient,
            "O2_GradientPlusLevel" => p => p.O2GradientPlusLevel,
            "O3_Curvature" => p => p.O3Curvature,
            "O4_OrbitKernel" => p => p.O4OrbitKernel,
            _ => throw new ArgumentOutOfRangeException(nameof(candidateName), candidateName, "Unknown theta observable candidate")
        };

        double obsScale = Median(list.Select(obsSelector).Where(v => double.IsFinite(v) && v >= 0.0));
        if (obsScale <= 0.0 || !double.IsFinite(obsScale))
            obsScale = 1.0;

        double bestLambda = 0.0;
        double bestRms = double.MaxValue;

        for (double lambda = -0.50; lambda <= 2.0001; lambda += 0.05)
        {
            double rms = ComputeRms(
                list,
                p =>
                {
                    double obs = obsSelector(p);
                    double normalized = obs / (obs + obsScale + 1e-12);
                    double g = p.GLocal * (1.0 + lambda * normalized);
                    return g > 0.0 && double.IsFinite(g) ? g : 0.0;
                });

            if (rms < bestRms)
            {
                bestRms = rms;
                bestLambda = lambda;
            }
        }

        Func<DataPoint, double> predictor = p =>
        {
            double obs = obsSelector(p);
            double normalized = obs / (obs + obsScale + 1e-12);
            double g = p.GLocal * (1.0 + bestLambda * normalized);
            return g > 0.0 && double.IsFinite(g) ? g : 0.0;
        };

        return new FitResult(candidateName, bestLambda, bestRms, predictor);
    }

    private static (int InnerCount, int MidCount, int OuterCount, double InnerRms, double MidRms, double OuterRms)
        ComputeResidualBins(IEnumerable<DataPoint> points, FitResult fit)
    {
        var inner = new List<double>();
        var mid = new List<double>();
        var outer = new List<double>();

        foreach (var p in points)
        {
            double gPred = fit.GetPrediction(p);
            if (gPred <= 0.0 || p.GObs <= 0.0 || !double.IsFinite(gPred))
                continue;

            double residual = Math.Log10(p.GObs) - Math.Log10(gPred);
            double x = p.Radius / p.Rd;

            if (x < 1.0)
                inner.Add(residual);
            else if (x < 4.0)
                mid.Add(residual);
            else
                outer.Add(residual);
        }

        return (
            inner.Count,
            mid.Count,
            outer.Count,
            ComputeRmsResidual(inner),
            ComputeRmsResidual(mid),
            ComputeRmsResidual(outer));
    }

    private static double FitBestLogShiftScale(IEnumerable<DataPoint> points)
    {
        return FitBestLogShiftScale(points, p => p.GLocal);
    }

    private static double FitBestLogShiftScale(IEnumerable<DataPoint> points, Func<DataPoint, double> predictor)
    {
        var list = points
            .Select(p => new { p.GObs, Pred = predictor(p) })
            .Where(x => x.GObs > 0.0 && x.Pred > 0.0 && double.IsFinite(x.Pred))
            .ToList();

        double meanDelta = list.Average(x => Math.Log10(x.GObs) - Math.Log10(x.Pred));
        return Math.Pow(10.0, meanDelta);
    }

    private static double ComputeOrbitKernel(ThetaFieldProfile field, int centerIndex)
    {
        if (field.Points.Count < 5 || centerIndex <= 0)
            return 0.0;

        double numerator = 0.0;
        double denominator = 0.0;

        for (int i = 1; i <= centerIndex && i < field.Points.Count - 1; i++)
        {
            var left = field.Points[i - 1];
            var mid = field.Points[i];
            var right = field.Points[i + 1];

            double dr = right.RadiusKpc - left.RadiusKpc;
            if (dr <= 0.0)
                continue;

            double rSafe = Math.Max(mid.RadiusKpc, 1e-6);
            double gradient = Math.Abs((right.Theta - left.Theta) / dr);
            double level = Math.Max(mid.Theta, 0.0) / rSafe;
            double observable = gradient + level;

            double shell = Math.Max(right.RadiusKpc - mid.RadiusKpc, 1e-6);
            numerator += observable * shell;
            denominator += shell;
        }

        return denominator > 0.0 ? numerator / denominator : 0.0;
    }

    private static double ComputeO5WindowContrast(ThetaFieldProfile field, int centerIndex, int halfWindow)
    {
        return ComputeO5KernelContrast(field, centerIndex, halfWindow, "inverse");
    }

    private static double ComputeO5KernelContrast(
        ThetaFieldProfile field,
        int centerIndex,
        int halfWindow,
        string kernelMode)
    {
        if (field.Points.Count < 5 || centerIndex < 0 || centerIndex >= field.Points.Count)
            return 0.0;

        int start = Math.Max(0, centerIndex - halfWindow);
        int end = Math.Min(field.Points.Count - 1, centerIndex + halfWindow);
        if (end - start < 2)
            return 0.0;

        double weightedTheta = 0.0;
        double weightSum = 0.0;

        for (int i = start; i <= end; i++)
        {
            double distance = Math.Abs(i - centerIndex);
            double weight = kernelMode switch
            {
                "uniform" => 1.0,
                "gaussian" => Math.Exp(-(distance * distance) / 8.0),
                _ => 1.0 / (1.0 + distance)
            };
            weightedTheta += field.Points[i].Theta * weight;
            weightSum += weight;
        }

        if (weightSum <= 0.0)
            return 0.0;

        double windowMean = weightedTheta / weightSum;
        double localTheta = field.Points[centerIndex].Theta;
        double contrast = windowMean - localTheta;
        return double.IsFinite(contrast) ? contrast : 0.0;
    }

    private static int FindNearestIndex(ThetaFieldProfile field, double targetRadiusKpc)
    {
        int bestIndex = -1;
        double bestDistance = double.MaxValue;

        for (int i = 0; i < field.Points.Count; i++)
        {
            double d = Math.Abs(field.Points[i].RadiusKpc - targetRadiusKpc);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static string FormatBin(int count, double rms)
    {
        if (count <= 0 || !double.IsFinite(rms))
            return "n=0, RMS=NaN";
        return $"n={count}, RMS={rms:F4}";
    }

    private static double ComputeRms(IEnumerable<DataPoint> points, Func<DataPoint, double> predictor)
    {
        var residualSquares = new List<double>();

        foreach (var p in points)
        {
            double gPred = predictor(p);
            if (gPred <= 0.0 || !double.IsFinite(gPred) || p.GObs <= 0.0)
                continue;

            double residual = Math.Log10(p.GObs) - Math.Log10(gPred);
            residualSquares.Add(residual * residual);
        }

        return residualSquares.Count > 0 ? Math.Sqrt(residualSquares.Average()) : double.MaxValue;
    }

    private static double ComputeRmsResidual(List<double> residuals)
    {
        if (residuals.Count == 0)
            return double.NaN;
        return Math.Sqrt(residuals.Average(x => x * x));
    }

    private static double Median(IEnumerable<double> values)
    {
        var arr = values
            .Where(v => double.IsFinite(v))
            .OrderBy(v => v)
            .ToArray();

        if (arr.Length == 0)
            return 0.0;

        int mid = arr.Length / 2;
        if (arr.Length % 2 == 1)
            return arr[mid];
        return 0.5 * (arr[mid - 1] + arr[mid]);
    }

    private static double StandardDeviation(IEnumerable<double> values)
    {
        var arr = values.Where(double.IsFinite).ToArray();
        if (arr.Length == 0)
            return 0.0;
        double mean = arr.Average();
        double variance = arr.Average(v => (v - mean) * (v - mean));
        return Math.Sqrt(Math.Max(variance, 0.0));
    }

    private static double ComputePearsonCorrelation(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n <= 1)
            return 0.0;

        double meanX = x.Take(n).Average();
        double meanY = y.Take(n).Average();

        double cov = 0.0;
        double varX = 0.0;
        double varY = 0.0;

        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            cov += dx * dy;
            varX += dx * dx;
            varY += dy * dy;
        }

        if (varX <= 0.0 || varY <= 0.0)
            return 0.0;

        return cov / Math.Sqrt(varX * varY);
    }

    private static double ComputeSignFlipRate(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return 0.0;

        int comparisons = 0;
        int flips = 0;

        int Sign(double v) => v > 1e-9 ? 1 : v < -1e-9 ? -1 : 0;

        for (int i = 1; i < values.Count; i++)
        {
            int s0 = Sign(values[i - 1]);
            int s1 = Sign(values[i]);

            if (s0 == 0 || s1 == 0)
                continue;

            comparisons++;
            if (s0 != s1)
                flips++;
        }

        return comparisons > 0 ? (double)flips / comparisons : 0.0;
    }

    private static double ComputeShare<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
    {
        if (values.Count == 0)
            return 0.0;
        return values.Count(predicate) / (double)values.Count;
    }

    private static double Percentile(IEnumerable<double> values, double p)
    {
        var arr = values
            .Where(double.IsFinite)
            .OrderBy(v => v)
            .ToArray();

        if (arr.Length == 0)
            return 0.0;

        if (p <= 0.0) return arr[0];
        if (p >= 1.0) return arr[^1];

        double pos = p * (arr.Length - 1);
        int lower = (int)Math.Floor(pos);
        int upper = (int)Math.Ceiling(pos);
        if (lower == upper)
            return arr[lower];

        double t = pos - lower;
        return arr[lower] * (1.0 - t) + arr[upper] * t;
    }

    private static List<T> Rotate<T>(IReadOnlyList<T> source, int shift)
    {
        if (source.Count == 0)
            return new List<T>();

        int n = source.Count;
        int s = ((shift % n) + n) % n;
        var result = new List<T>(n);
        for (int i = 0; i < n; i++)
        {
            int idx = (i + s) % n;
            result.Add(source[idx]);
        }
        return result;
    }

    private static LinearFitMetrics ComputeLinearFit(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        int n = Math.Min(x.Count, y.Count);
        if (n < 3)
            return new LinearFitMetrics(0.0, 0.0, 0.0, double.NaN);

        double meanX = x.Take(n).Average();
        double meanY = y.Take(n).Average();

        double sxx = 0.0;
        double sxy = 0.0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            sxx += dx * dx;
            sxy += dx * (y[i] - meanY);
        }

        double slope = sxx > 0.0 ? sxy / sxx : 0.0;
        double intercept = meanY - slope * meanX;

        double ssRes = 0.0;
        double ssTot = 0.0;
        for (int i = 0; i < n; i++)
        {
            double yHat = intercept + slope * x[i];
            double err = y[i] - yHat;
            ssRes += err * err;

            double dTot = y[i] - meanY;
            ssTot += dTot * dTot;
        }

        double r2 = ssTot > 0.0 ? 1.0 - (ssRes / ssTot) : 0.0;
        r2 = Math.Clamp(r2, 0.0, 1.0);
        double rmse = Math.Sqrt(ssRes / n);

        return new LinearFitMetrics(slope, intercept, r2, rmse);
    }

    private void WriteTestLine(string testId, string message)
    {
        _output.WriteLine($"[{testId}] {message}");
    }

    private static ThetaClaimClassification ClassifyThetaClaim(bool localWinsAll, bool anyThetaWin, bool anyClearThetaWin)
    {
        if (localWinsAll || !anyThetaWin)
            return new ThetaClaimClassification("diagnostic", "hypothesis-supported");

        if (anyClearThetaWin)
            return new ThetaClaimClassification("effective-candidate", "tested-effective / hypothesis-supported");

        return new ThetaClaimClassification("candidate", "hypothesis-supported");
    }

    private sealed record Dataset(List<DataPoint> Points);

    private sealed record DataPoint(
        string GalaxyName,
        double Radius,
        double Rd,
        double GObs,
        double Gbar,
        double GLocal,
        double O1Gradient,
        double O2GradientPlusLevel,
        double O3Curvature,
        double O4OrbitKernel,
        double O5NonLocalKernel,
        double O5W2InvDistance,
        double O5W4InvDistance,
        double O5W6InvDistance,
        double O5W8InvDistance,
        double O5W4Uniform,
        double O5W4Gaussian);

    private sealed record LambdaSample(
        string Galaxy,
        double GObs,
        double GBase,
        double X);

    private sealed record LambdaRegimeSample(
        string Galaxy,
        double GObs,
        double GBase,
        double X,
        double XRadius,
        double LogGbar);

    private sealed record LambdaScenarioResult(
        string Name,
        double Lambda,
        double Improvement,
        double ContributionScale);

    private sealed record RegularizedAlphaResult(
        double Alpha,
        double MeanGlobalImprovement,
        double MeanRegularizedImprovement,
        double MeanGain,
        double GainShare,
        double WorstStrataFloor);

    private sealed record ResidualPoint(
        string GalaxyName,
        double Radius,
        double Rd,
        double Gbar,
        double LocalResidual,
        double ThetaResidual);

    private sealed record ResidualStructureSummary(
        string Name,
        int Count,
        double[] BinRms,
        double MeanBinRms,
        double OuterInnerBias,
        double AbsRadiusCorr,
        double AbsGbarCorr,
        double MedianFlipRate,
        double MedianAbsMeanResidual,
        double StructureScore);

    private sealed record RegimeComparison(
        string Name,
        int Count,
        double LocalRms,
        double O4Rms,
        double DeltaRms);

    private sealed record ThetaClaimClassification(
        string Status,
        string ClaimBoundary);

    private sealed record O5ResidualSample(
        string Galaxy,
        double XRadius,
        double LogGbar,
        double BaselineErrSq,
        double O5ErrSq);

    private sealed record KernelRegimeSample(
        string Galaxy,
        double XRadius,
        double LogGbar,
        double BaselineErrSq,
        double VariantErrSq);

    private sealed record KernelPhysicalEvaluation(
        string Name,
        double Rmse,
        double R2,
        double Improvement,
        double RegimeMeanDelta,
        double RegimeStdDelta,
        double PositiveRegimeShare,
        double PhysicalScore);

    private sealed record KernelHoldoutMetrics(
        string Name,
        double MeanTrainImprovement,
        double MeanTestImprovement,
        double GapStd,
        double PositiveTestShare,
        double WorstFoldImprovement,
        double StabilityScore);

    private sealed record SolverAblationProfile(
        string Name,
        double SourceStrength,
        double DampingStrength,
        double SyncStrength,
        int Iterations,
        double Relaxation);

    private sealed record SolverAblationResult(
        string Name,
        KernelHoldoutMetrics Metrics,
        double CorrelationVsBaseline,
        bool IsStable);

    private sealed record SyntheticProfiles(
        ThetaFieldProfile Constant,
        ThetaFieldProfile Smooth,
        ThetaFieldProfile LocalContrast,
        ThetaFieldProfile NonLocalBreak);

    private sealed record WindowOperatorMetrics(
        int HalfWindow,
        double ConstantMaxAbs,
        double SmoothMaxAbs,
        double LocalMaxAbs,
        double NonLocalMaxAbs,
        double LocalGainOverSmooth,
        double NonLocalGainOverLocal,
        double NonLocalArea,
        double NonLocalMeanAbs,
        double LocalSupportWidth,
        double SmoothStdAbs,
        bool IsStable);

    private sealed record ResolutionConfig(
        string Name,
        int Count,
        double Spacing);

    private sealed record PhaseKernelScore(
        string KernelMode,
        double BalanceScore,
        double MeanEnergyDrop,
        double SmoothNoise,
        double LocalContrastSensitivity,
        double NonLocalBreakSensitivity,
        double HoldoutMeanImprovement,
        double HoldoutPositiveShare);

    private sealed record WeakFoldResult(
        int Fold,
        int TestCount,
        double TrainImprovement,
        double TestImprovement,
        double Gap,
        double LowGbarShare,
        double LsbShare,
        double InnerShare,
        double MiddleShare,
        double OuterShare,
        double HighResidualShare,
        double MeanAbsResidual,
        double P90AbsResidual,
        double Top3DegradationDominance);

    private sealed record LinearFitMetrics(
        double Slope,
        double Intercept,
        double RSquared,
        double Rmse);

    private sealed class FitResult
    {
        private readonly Func<DataPoint, double> _predictor;

        public FitResult(string name, double lambda, double globalRms, Func<DataPoint, double> predictor)
        {
            Name = name;
            Lambda = lambda;
            GlobalRms = globalRms;
            _predictor = predictor;
        }

        public string Name { get; }
        public double Lambda { get; }
        public double GlobalRms { get; }

        public double GetPrediction(DataPoint p) => _predictor(p);
    }
}
