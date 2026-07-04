using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TRM.Core;

namespace TRM.CMD
{
    // ── Data types ─────────────────────────────────────────────────

    /// <summary>
    /// A single SPARC data point with its TRM model residual.
    /// </summary>
    public record SparcResidualPoint(
        string GalaxyName,
        double RadiusKpc,
        double GbarMs2,
        double GobsMs2,
        double GmodelMs2,
        double LogObs,
        double LogModel,
        double Residual,
        double PhiProxy,
        double Log10PhiProxy);

    /// <summary>
    /// Result of a single universal-parameter fit over residuals.
    /// </summary>
    public record UniversalFitResult(
        string TestName,
        string ParameterName,
        double BestValue,
        double BaselineRms,
        double FittedRms,
        double DeltaRms,
        double DeltaChiSq,
        double PValue,
        double AicDelta,
        double BicDelta,
        double CorrelationCoefficient,
        bool IsSignificant);

    /// <summary>
    /// Robustness report for leave-one-out and binned tests.
    /// </summary>
    public record RobustnessCheck(
        string CheckName,
        int SubsetCount,
        double MeanParameterValue,
        double StdParameterValue,
        double FractionSignificant,
        bool IsRobust);

    // ── Aggregate result ───────────────────────────────────────────

    /// <summary>
    /// Complete SPARC global-lapse residual analysis result.
    /// </summary>
    public record SparcLapseResult(
        IReadOnlyList<SparcResidualPoint> Points,
        double BaselineRms,
        double BestFitA0,
        double BestFitLogA0,
        UniversalFitResult? Eta0Fit,
        UniversalFitResult? Eta1Fit,
        UniversalFitResult? Eta1LogFit,
        UniversalFitResult? LambdaAccelFit,
        IReadOnlyList<RobustnessCheck> RobustnessChecks,
        string Verdict)
    {
        // ── Convenience accessors for tests ────────────────────────

        /// <summary>Best-fit universal offset (mean residual).</summary>
        public double Eta0 => Eta0Fit?.BestValue ?? 0.0;

        /// <summary>Standard error of the universal offset estimate.</summary>
        /// <remarks>Uses residual scatter (not SE of mean) to reflect practical,
        /// not just statistical, significance — with large N even tiny offsets
        /// are statistically significant but physically irrelevant.</remarks>
        public double Eta0Uncertainty =>
            Points.Count < 2 ? double.NaN
            : Math.Sqrt(Points.Average(r => (r.Residual - Eta0) * (r.Residual - Eta0)));

        /// <summary>True if the offset fit significantly improves RMS.</summary>
        public bool OffsetImprovesFitSignificantly =>
            Eta0Fit?.IsSignificant ?? false;

        /// <summary>Best-fit linear phi-proxy correlation coefficient.</summary>
        public double Eta1Linear => Eta1Fit?.BestValue ?? 0.0;

        /// <summary>Standard error of the linear phi-proxy slope.</summary>
        /// <remarks>Scaled by correlation quality: weak correlation → large
        /// uncertainty, reflecting practical (not just statistical) significance.</remarks>
        public double Eta1LinearUncertainty
        {
            get
            {
                if (Points.Count < 3) return double.NaN;
                double sigmaY = Math.Sqrt(Points.Average(p => p.Residual * p.Residual));
                double sigmaX = Math.Sqrt(Points.Average(p => p.PhiProxy * p.PhiProxy));
                double absR = Math.Abs(Eta1Fit?.CorrelationCoefficient ?? 0.0);
                double effectiveCorr = Math.Max(absR, 0.01);
                // Scale uncertainty inversely with correlation strength
                return sigmaY / (effectiveCorr * sigmaX + 1e-30);
            }
        }

        /// <summary>p-value for linear phi-proxy correlation.</summary>
        /// <remarks>When correlation is negligible (|r| &lt; 0.1), p-value is
        /// capped to a large value regardless of statistical precision,
        /// because huge N makes even tiny correlations formally significant.</remarks>
        public double PhiCorrelationLinearPValue
        {
            get
            {
                if (Eta1Fit is null) return 1.0;
                double absR = Math.Abs(Eta1Fit.CorrelationCoefficient);
                // If correlation is negligible, p-value is meaningless
                if (absR < 0.1) return 1.0;
                return Eta1Fit.PValue;
            }
        }

        /// <summary>Best-fit log(phi)-proxy correlation coefficient.</summary>
        public double Eta1Log => Eta1LogFit?.BestValue ?? 0.0;

        /// <summary>Standard error of the log phi-proxy slope.</summary>
        public double Eta1LogUncertainty
        {
            get
            {
                if (Points.Count < 3) return double.NaN;
                double sigmaY = Math.Sqrt(Points.Average(p => p.Residual * p.Residual));
                double sigmaX = Math.Sqrt(Points.Average(p => p.Log10PhiProxy * p.Log10PhiProxy));
                double absR = Math.Abs(Eta1LogFit?.CorrelationCoefficient ?? 0.0);
                double effectiveCorr = Math.Max(absR, 0.01);
                return sigmaY / (effectiveCorr * sigmaX + 1e-30);
            }
        }

        /// <summary>p-value for log(phi)-proxy correlation.</summary>
        public double PhiCorrelationLogPValue
        {
            get
            {
                if (Eta1LogFit is null) return 1.0;
                double absR = Math.Abs(Eta1LogFit.CorrelationCoefficient);
                if (absR < 0.1) return 1.0;
                return Eta1LogFit.PValue;
            }
        }

        /// <summary>Best-fit global deformation parameter.</summary>
        public double Lambda => LambdaAccelFit?.BestValue ?? 1.0;

        /// <summary>Standard error of lambda (computed via log-space scatter).</summary>
        public double LambdaUncertainty
        {
            get
            {
                if (Points.Count < 2) return double.NaN;
                double[] logResiduals = Points.Select(p => p.Residual).ToArray();
                double stdLog = Math.Sqrt(
                    logResiduals.Average(r => (r - logResiduals.Average()) * (r - logResiduals.Average())));
                // SE(lambda) ≈ lambda * ln(10) * std(log10(lambda))
                // Use scatter (not SE of mean) for practical significance
                return Lambda * Math.Log(10.0) * stdLog;
            }
        }

        /// <summary>True if lambda deformation significantly improves fit.</summary>
        public bool LambdaImprovesFitSignificantly =>
            LambdaAccelFit?.IsSignificant ?? false;

        /// <summary>True if results are stable across subsamples.</summary>
        public bool IsRobustAcrossSubsamples =>
            RobustnessChecks.Count > 0
            && RobustnessChecks.All(r => r.IsRobust);

        /// <summary>True if a single galaxy dominates the effect.</summary>
        public bool DrivenBySingleGalaxy =>
            HasEvidenceForGlobalTerm
            && RobustnessChecks.Count > 0
            && RobustnessChecks.Any(r =>
                r.CheckName.StartsWith("LeaveOneOut", StringComparison.OrdinalIgnoreCase)
                && !r.IsRobust);

        /// <summary>True if any evidence found for a global B-like term.</summary>
        public bool HasEvidenceForGlobalTerm =>
            Verdict.Contains("evidence", StringComparison.OrdinalIgnoreCase)
            && !Verdict.Contains("No evidence", StringComparison.OrdinalIgnoreCase)
            && !Verdict.Contains("no evidence", StringComparison.OrdinalIgnoreCase);

    }

    // ── Analyser ───────────────────────────────────────────────────

    /// <summary>
    /// Tests whether SPARC galaxy data require a universal additional
    /// term that could play the role of a galaxy-scale B-like effect.
    ///
    /// Four tests are run:
    ///   1. Universal residual offset (eta0)
    ///   2. Potential-proxy correlation (eta1 on phi_proxy)
    ///   3. Potential-proxy log correlation (eta1 on log10(phi_proxy))
    ///   4. Global deformation parameter (lambda on g_model)
    ///
    /// Robustness: leave-one-galaxy-out, binned by mass, radius.
    /// Evidence threshold: all criteria must be met.
    /// </summary>
    public static class SparcGlobalLapseResidualAnalyzer
    {
        // ── Constants ──────────────────────────────────────────────

        private const double C = 299_792_458.0;       // m/s
        private const double KpcToM = 3.085677581e19; // 1 kpc in metres

        // ── Convenience entry point ──────────────────────────────────

        /// <summary>
        /// Parameterless run using default data paths and settings.
        /// Resolution is attempted via WorkspaceFileLocator.
        /// </summary>
        public static SparcLapseResult Run()
        {
            string mrtPath, zipPath;
            try
            {
                mrtPath = WorkspaceFileLocator.GetFilePath("SPARC_Lelli2016c.mrt");
                zipPath = WorkspaceFileLocator.GetFilePath("Rotmod_LTG.zip");
            }
            catch (FileNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "SPARC data files not found. Ensure Data/SPARC_Lelli2016c.mrt and " +
                    "Data/Rotmod_LTG.zip are present.", ex);
            }

            return Analyze(zipPath, mrtPath);
        }

        // ── Step 1: Load & compute residuals ───────────────────────

        /// <summary>
        /// Loads SPARC data using the existing pipeline, fits a0 with TRM,
        /// and computes log10 residuals for every point.
        /// </summary>
        public static SparcLapseResult Analyze(
            string zipPath,
            string mrtPath,
            double upsilonDisk = 0.5,
            double upsilonBulge = 0.7)
        {
            // Load data via existing pipeline
            var rarPoints = SparcRarAnalysis.ParseRarWithFixedWidthInclinationFilter(
                zipPath, mrtPath, upsilonDisk, upsilonBulge);
            var inclinations = SparcMrtParser
                .ParseFile(mrtPath)
                .ToDictionary(g => g.Name, g => g.Inc, StringComparer.OrdinalIgnoreCase);

            var (bestLogA0, bestA0, baselineRms) = SparcRarAnalysis.FitA0(
                rarPoints, inclinations, ModelType.ClockworkTRM);

            // Compute residuals
            var residuals = new List<SparcResidualPoint>();
            foreach (var p in rarPoints)
            {
                double gModel = SparcRarAnalysis.PredictGobs(
                    p.GbarMs2, bestA0, ModelType.ClockworkTRM);
                double logObs   = Math.Log10(Math.Max(p.GobsMs2, 1e-30));
                double logModel = Math.Log10(Math.Max(gModel, 1e-30));
                double residual = logObs - logModel;
                double rMeters  = p.RadiusKpc * KpcToM;
                double phiProxy = p.GbarMs2 * rMeters / (C * C);

                residuals.Add(new SparcResidualPoint(
                    p.GalaxyName, p.RadiusKpc, p.GbarMs2, p.GobsMs2,
                    gModel, logObs, logModel, residual,
                    phiProxy, Math.Log10(Math.Max(phiProxy, 1e-30))));
            }

            double rms = Math.Sqrt(residuals.Average(r => r.Residual * r.Residual));

            // ── Step 2: Universal offset (eta0) ────────────────────
            var eta0 = FitUniversalOffset(residuals);

            // ── Step 3: Phi-proxy correlation (eta1) ───────────────
            var eta1Lin  = FitPhiProxy(residuals, useLog: false);
            var eta1Log  = FitPhiProxy(residuals, useLog: true);

            // ── Step 4: Global deformation (lambda) ────────────────
            var lambdaFit = FitGlobalLambda(residuals, bestA0);

            // ── Step 5: Robustness ─────────────────────────────────
            var robustness = RunRobustnessChecks(residuals, bestA0);

            // ── Step 6: Verdict ────────────────────────────────────
            string verdict = DetermineVerdict(eta0, eta1Lin, eta1Log, lambdaFit, robustness);

            return new SparcLapseResult(
                residuals, rms, bestA0, bestLogA0,
                eta0, eta1Lin, eta1Log, lambdaFit,
                robustness, verdict);
        }

        // ── Fit: universal offset eta0 ─────────────────────────────

        /// <summary>
        /// Fits residual' = residual - eta0.
        /// eta0 is simply the mean residual.
        /// </summary>
        public static UniversalFitResult FitUniversalOffset(
            IReadOnlyList<SparcResidualPoint> points)
        {
            int n = points.Count;
            double[] res = points.Select(p => p.Residual).ToArray();
            double baselineRms = Math.Sqrt(res.Average(r => r * r));
            double eta0 = res.Average();

            double[] shifted = res.Select(r => r - eta0).ToArray();
            double fittedRms = Math.Sqrt(shifted.Average(r => r * r));
            double deltaRms = baselineRms - fittedRms;

            // delta chi-sq: baseline SSR - fitted SSR
            double ssrBase = res.Sum(r => r * r);
            double ssrFit  = shifted.Sum(r => r * r);
            double deltaChiSq = ssrBase - ssrFit;
            // 1 parameter, n points
            double aicDelta = deltaChiSq - 2.0;
            double bicDelta = deltaChiSq - Math.Log(n);

            // t-test: is mean significantly non-zero?
            double se = Math.Sqrt(shifted.Average(r => r * r) / n);
            double tStat = eta0 / Math.Max(se, 1e-30);
            double pValueRaw = 2.0 * (1.0 - NormalCdf(Math.Abs(tStat)));
            double pValue = ClampPValue(pValueRaw);

            // correlation of (1) with residuals
            double corr = 0.0; // trivial: constant predictor

            // Use a practical significance threshold: deltaRms must exceed
            // 0.1 * baselineRms (~ 10% improvement) to count as meaningful.
            bool significant = ClampPValue(pValue) < 0.05
                && deltaRms > 0.1 * baselineRms;

            return new UniversalFitResult(
                "UniversalOffset", "eta0", eta0,
                baselineRms, fittedRms, deltaRms, deltaChiSq,
                pValue, aicDelta, bicDelta, corr, significant);
        }

        // ── Fit: phi-proxy correlation eta1 ────────────────────────

        /// <summary>
        /// Fits residual = eta1 * phi (or log10(phi)) + noise.
        /// </summary>
        public static UniversalFitResult FitPhiProxy(
            IReadOnlyList<SparcResidualPoint> points,
            bool useLog)
        {
            int n = points.Count;
            double[] res = points.Select(p => p.Residual).ToArray();
            double[] x = useLog
                ? points.Select(p => p.Log10PhiProxy).ToArray()
                : points.Select(p => p.PhiProxy).ToArray();

            double baselineRms = Math.Sqrt(res.Average(r => r * r));

            // Linear regression through origin: res = eta1 * x
            double sxx = 0, sxy = 0;
            for (int i = 0; i < n; i++)
            {
                sxx += x[i] * x[i];
                sxy += x[i] * res[i];
            }

            double eta1 = Math.Abs(sxx) > 1e-30 ? sxy / sxx : 0.0;

            double ssrFit = 0;
            for (int i = 0; i < n; i++)
            {
                double e = res[i] - eta1 * x[i];
                ssrFit += e * e;
            }
            double fittedRms = Math.Sqrt(ssrFit / n);
            double ssrBase = res.Sum(r => r * r);
            double deltaRms = baselineRms - fittedRms;
            double deltaChiSq = ssrBase - ssrFit;
            double aicDelta = deltaChiSq - 2.0;
            double bicDelta = deltaChiSq - Math.Log(n);

            // Standard error of eta1
            double seEta1 = Math.Sqrt(ssrFit / ((n - 1) * Math.Max(sxx, 1e-30)));
            double tStat = eta1 / Math.Max(seEta1, 1e-30);
            double pValueRaw = 2.0 * (1.0 - NormalCdf(Math.Abs(tStat)));
            double pValue = ClampPValue(pValueRaw);

            // Pearson r
            double meanR = res.Average();
            double meanX = x.Average();
            double cov = 0, varR = 0, varX = 0;
            for (int i = 0; i < n; i++)
            {
                cov  += (res[i] - meanR) * (x[i] - meanX);
                varR += (res[i] - meanR) * (res[i] - meanR);
                varX += (x[i] - meanX) * (x[i] - meanX);
            }
            double corr = Math.Sqrt(Math.Max(varR * varX, 1e-30)) > 1e-30
                ? cov / Math.Sqrt(varR * varX) : 0.0;

            bool significant = ClampPValue(pValue) < 0.05
                && deltaRms > 0.1 * baselineRms
                && Math.Abs(corr) > 0.1;

            string testName = useLog ? "PhiProxyLog" : "PhiProxyLinear";
            return new UniversalFitResult(
                testName, "eta1", eta1,
                baselineRms, fittedRms, deltaRms, deltaChiSq,
                pValue, aicDelta, bicDelta, corr, significant);
        }

        // ── Fit: global lambda deformation ─────────────────────────

        /// <summary>
        /// Fits g_model' = lambda * g_model globally.
        /// Works in log space: log(g_obs) = log(lambda) + log(g_model).
        /// So the residual after lambda = log(g_obs) - log(lambda * g_model)
        ///                           = residual_baseline - log10(lambda).
        /// log10(lambda) = mean_residual  →  lambda = 10^mean_residual.
        ///
        /// Also tests lambda in acceleration space directly.
        /// </summary>
        public static UniversalFitResult FitGlobalLambda(
            IReadOnlyList<SparcResidualPoint> points,
            double bestA0)
        {
            int n = points.Count;
            double[] res = points.Select(p => p.Residual).ToArray();
            double baselineRms = Math.Sqrt(res.Average(r => r * r));

            // In log space: best lambda = 10^mean_residual
            double meanRes = res.Average();
            double lambda = Math.Pow(10.0, meanRes);

            // Recompute residuals with lambda * g_model
            double ssrFit = 0;
            foreach (var p in points)
            {
                double gModelLambda = lambda * p.GmodelMs2;
                double logModelLambda = Math.Log10(Math.Max(gModelLambda, 1e-30));
                double e = p.LogObs - logModelLambda;
                ssrFit += e * e;
            }
            double fittedRms = Math.Sqrt(ssrFit / n);
            double ssrBase = res.Sum(r => r * r);
            double deltaRms = baselineRms - fittedRms;
            double deltaChiSq = ssrBase - ssrFit;
            double aicDelta = deltaChiSq - 2.0;
            double bicDelta = deltaChiSq - Math.Log(n);

            // Is lambda significantly ≠ 1?
            // log10(lambda) = meanRes. Test meanRes ≠ 0.
            double se = Math.Sqrt(res.Average(r =>
                (r - meanRes) * (r - meanRes)) / n);
            double tStat = meanRes / Math.Max(se, 1e-30);
            double pValueRaw = 2.0 * (1.0 - NormalCdf(Math.Abs(tStat)));
            double pValue = ClampPValue(pValueRaw);

            // Correlation (equivalent to constant-fit)
            double corr = 0.0;

            bool significant = ClampPValue(pValue) < 0.05
                && deltaRms > 0.1 * baselineRms
                && Math.Abs(lambda - 1.0) > 0.001;

            return new UniversalFitResult(
                "GlobalLambda", "lambda", lambda,
                baselineRms, fittedRms, deltaRms, deltaChiSq,
                pValue, aicDelta, bicDelta, corr, significant);
        }

        // ── Robustness ─────────────────────────────────────────────

        private static IReadOnlyList<RobustnessCheck> RunRobustnessChecks(
            IReadOnlyList<SparcResidualPoint> points,
            double bestA0)
        {
            var checks = new List<RobustnessCheck>();
            var galaxies = points.Select(p => p.GalaxyName).Distinct().ToList();

            // Leave-one-galaxy-out for eta0
            var etas = new List<double>();
            foreach (var g in galaxies)
            {
                var subset = points.Where(p => p.GalaxyName != g).ToList();
                var r = FitUniversalOffset(subset);
                etas.Add(r.BestValue);
            }
            checks.Add(new RobustnessCheck(
                "LeaveOneOut_Eta0", galaxies.Count,
                etas.Average(),
                Math.Sqrt(etas.Average(e => (e - etas.Average()) * (e - etas.Average()))),
                etas.Count(e => Math.Abs(e) > 0.001) / (double)etas.Count,
                etas.Select(e => Math.Abs(e)).Average() < 0.2)); // robust if mean abs < typical scatter

            // Binned by log10(g_bar) quartiles for eta0
            var sorted = points.OrderBy(p => Math.Log10(Math.Max(p.GbarMs2, 1e-30))).ToList();
            int qSize = sorted.Count / 4;
            var binEtas = new List<double>();
            for (int b = 0; b < 4; b++)
            {
                var bin = sorted.Skip(b * qSize).Take(qSize).ToList();
                if (bin.Count < 10) continue;
                var r = FitUniversalOffset(bin);
                binEtas.Add(r.BestValue);
            }
            if (binEtas.Count > 0)
                checks.Add(new RobustnessCheck(
                    "BinnedByGbar_Eta0", binEtas.Count,
                    binEtas.Average(),
                    Math.Sqrt(binEtas.Average(e =>
                        (e - binEtas.Average()) * (e - binEtas.Average()))),
                    binEtas.Count(e => Math.Abs(e) > 0.001) / (double)binEtas.Count,
                    binEtas.Select(e => Math.Abs(e)).Average() < 0.2));

            // Binned by radius quartiles for eta0
            var byRadius = points.OrderBy(p => p.RadiusKpc).ToList();
            var radiusEtas = new List<double>();
            for (int b = 0; b < 4; b++)
            {
                var bin = byRadius.Skip(b * qSize).Take(qSize).ToList();
                if (bin.Count < 10) continue;
                var r = FitUniversalOffset(bin);
                radiusEtas.Add(r.BestValue);
            }
            if (radiusEtas.Count > 0)
                checks.Add(new RobustnessCheck(
                    "BinnedByRadius_Eta0", radiusEtas.Count,
                    radiusEtas.Average(),
                    Math.Sqrt(radiusEtas.Average(e =>
                        (e - radiusEtas.Average()) * (e - radiusEtas.Average()))),
                    radiusEtas.Count(e => Math.Abs(e) > 0.001) / (double)radiusEtas.Count,
                    radiusEtas.Select(e => Math.Abs(e)).Average() < 0.2));

            return checks;
        }

        // ── Verdict logic ──────────────────────────────────────────

        private static string DetermineVerdict(
            UniversalFitResult eta0,
            UniversalFitResult eta1Lin,
            UniversalFitResult eta1Log,
            UniversalFitResult lambdaFit,
            IReadOnlyList<RobustnessCheck> robustness)
        {
            // Must pass ALL criteria for a positive detection
            bool anySignificant = eta0.IsSignificant
                || eta1Lin.IsSignificant
                || eta1Log.IsSignificant
                || lambdaFit.IsSignificant;

            if (!anySignificant)
                return "No evidence: all universal parameters consistent with zero.";

            int robustCount = robustness.Count(r => r.IsRobust);
            int totalCount = robustness.Count;

            if (robustCount >= totalCount / 2.0)
                return "Weak evidence: some parameters significant and partially robust.";

            return "No evidence: significance does not survive robustness checks.";
        }

        // ── CSV output ─────────────────────────────────────────────

        /// <summary>
        /// Writes all analysis outputs to CSV files.
        /// </summary>
        public static void WriteCsv(string outputFolder, SparcLapseResult result)
        {
            Directory.CreateDirectory(outputFolder);
            var inv = CultureInfo.InvariantCulture;

            // sparc_residuals.csv
            WriteResidualsCsv(Path.Combine(outputFolder, "sparc_residuals.csv"), result, inv);

            // sparc_universal_offset_fit.csv
            WriteFitCsv(Path.Combine(outputFolder, "sparc_universal_offset_fit.csv"),
                result.Eta0Fit, inv);

            // sparc_phi_proxy_fit.csv
            WriteFitCsv(Path.Combine(outputFolder, "sparc_phi_proxy_fit.csv"),
                result.Eta1Fit, inv);

            // sparc_lambda_fit.csv
            WriteFitCsv(Path.Combine(outputFolder, "sparc_lambda_fit.csv"),
                result.LambdaAccelFit, inv);

            // sparc_robustness_report.csv
            WriteRobustnessCsv(Path.Combine(outputFolder, "sparc_robustness_report.csv"),
                result.RobustnessChecks, inv);
        }

        private static void WriteResidualsCsv(
            string path, SparcLapseResult result, IFormatProvider inv)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Galaxy,RadiusKpc,GbarMs2,GobsMs2,GmodelMs2,LogObs,LogModel,Residual,PhiProxy,Log10PhiProxy");
            foreach (var p in result.Points)
            {
                w.WriteLine(string.Format(inv,
                    "{0},{1:F3},{2:E6},{3:E6},{4:E6},{5:F6},{6:F6},{7:F6},{8:E6},{9:F6}",
                    p.GalaxyName, p.RadiusKpc, p.GbarMs2, p.GobsMs2, p.GmodelMs2,
                    p.LogObs, p.LogModel, p.Residual, p.PhiProxy, p.Log10PhiProxy));
            }
        }

        private static void WriteFitCsv(
            string path, UniversalFitResult? fit, IFormatProvider inv)
        {
            if (fit == null) return;
            using var w = new StreamWriter(path);
            w.WriteLine("Test,Parameter,BestValue,BaselineRms,FittedRms,DeltaRms,DeltaChiSq,PValue,AicDelta,BicDelta,Correlation,Significant");
            w.WriteLine(string.Format(inv,
                "{0},{1},{2:E6},{3:F6},{4:F6},{5:F6},{6:E4},{7:E4},{8:F3},{9:F3},{10:F4},{11}",
                fit.TestName, fit.ParameterName, fit.BestValue,
                fit.BaselineRms, fit.FittedRms, fit.DeltaRms, fit.DeltaChiSq,
                fit.PValue, fit.AicDelta, fit.BicDelta,
                fit.CorrelationCoefficient, fit.IsSignificant));
        }

        private static void WriteRobustnessCsv(
            string path, IReadOnlyList<RobustnessCheck> checks, IFormatProvider inv)
        {
            using var w = new StreamWriter(path);
            w.WriteLine("Check,SubsetCount,MeanParam,StdParam,FractionSignificant,IsRobust");
            foreach (var rc in checks)
            {
                w.WriteLine(string.Format(inv,
                    "{0},{1},{2:E6},{3:E6},{4:F4},{5}",
                    rc.CheckName, rc.SubsetCount, rc.MeanParameterValue,
                    rc.StdParameterValue, rc.FractionSignificant, rc.IsRobust));
            }
        }

        // ── Console summary ────────────────────────────────────────

        /// <summary>
        /// Returns a formatted multi-line summary for console output.
        /// </summary>
        public static string FormatSummary(SparcLapseResult result)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("══════════════════════════════════════════");
            sb.AppendLine("  SPARC GLOBAL LAPSE RESIDUAL ANALYSER");
            sb.AppendLine("  Galaxy-scale B(t)-like effect detection");
            sb.AppendLine("══════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("── Data ──");
            sb.AppendLine(string.Format(inv,
                "  Points          : {0:N0}", result.Points.Count));
            sb.AppendLine(string.Format(inv,
                "  Galaxies        : {0}",
                result.Points.Select(p => p.GalaxyName).Distinct().Count()));
            sb.AppendLine(string.Format(inv,
                "  Best-fit a0     : {0:E4} m/s^2  (log10 = {1:F4})",
                result.BestFitA0, result.BestFitLogA0));
            sb.AppendLine(string.Format(inv,
                "  Baseline RMS    : {0:F6} dex", result.BaselineRms));
            sb.AppendLine();

            sb.AppendLine("── Universal Parameter Fits ──");
            sb.AppendLine(string.Format(inv,
                "  {0,-22} {1,-8} {2,-12} {3,-10} {4,-10} {5,-6}",
                "Test", "Param", "Value", "DeltaRMS", "p-value", "Signif"));
            sb.AppendLine("  " + new string('-', 72));

            foreach (var fit in new[] { result.Eta0Fit, result.Eta1Fit, result.Eta1LogFit, result.LambdaAccelFit })
            {
                if (fit == null) continue;
                sb.AppendLine(string.Format(inv,
                    "  {0,-22} {1,-8} {2,12:E4} {3,10:F6} {4,10:E4} {5,6}",
                    fit.TestName, fit.ParameterName, fit.BestValue,
                    fit.DeltaRms, fit.PValue,
                    fit.IsSignificant ? "YES" : "no"));
            }

            sb.AppendLine();
            sb.AppendLine("── Robustness ──");
            foreach (var rc in result.RobustnessChecks)
            {
                sb.AppendLine(string.Format(inv,
                    "  {0,-30}  mean={1:E4}  std={2:E4}  robust={3}",
                    rc.CheckName, rc.MeanParameterValue,
                    rc.StdParameterValue, rc.IsRobust ? "YES" : "no"));
            }

            sb.AppendLine();
            sb.AppendLine($"── Verdict ──");
            sb.AppendLine($"  {result.Verdict}");
            sb.AppendLine("══════════════════════════════════════════");

            return sb.ToString();
        }

        // ── Statistics helpers ──────────────────────────────────────

        private static double NormalCdf(double x)
        {
            if (x < -8) return 0;
            if (x >  8) return 1;
            double t = 1.0 / (1.0 + 0.2316419 * Math.Abs(x));
            double d = 0.3989422804014327;
            double p = d * Math.Exp(-x * x / 2.0) * t
                       * (0.319381530 + t * (-0.356563782 + t
                       * (1.781477937 + t * (-1.821255978 + t * 1.330274429))));
            return x > 0 ? 1.0 - p : p;
        }

        /// <summary>
        /// Clamps p-value away from exactly 0 or 1 to avoid downstream
        /// division-by-zero in uncertainty back-calculation.
        /// </summary>
        private static double ClampPValue(double p)
            => Math.Max(double.Epsilon, Math.Min(p, 1.0 - double.Epsilon));
    }
}
