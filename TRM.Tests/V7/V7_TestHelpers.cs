using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TRM.Core.Geometry.V6;

namespace TRM.Tests.V7_3_and_4;

// ============================================================
// Shared types and helpers for V7_3 and V7_4 tests
// ============================================================

#region Records

public sealed record SweepPoint(
    double P,
    double A,
    double CupdR,
    double Covariance,
    double DistanceDiscrimination,
    double GeometryQuality,
    double Functionality,
    double MeanK,
    double CurvatureEnergy,
    double BalanceObjective);

public sealed record BspPoint(
    double P,
    double Suppression,
    double Discrimination,
    double R,
    double OrderingQuality,
    double StructureQuality,
    double SystemQuality,
    double BalanceQ);

public sealed record VariantSpec(
    string Name,
    VcFamily Family,
    double XiScale,
    double K0Scale,
    double Alpha,
    double Beta,
    double Gamma);

public sealed record BerPoint(
    double P,
    double B,
    double Suppression,
    double Discrimination,
    double OrderingQuality,
    double Covariance,
    double Stability,
    double Structure,
    double Geometry,
    double InformationRetention,
    double Quality);

public sealed record CciPoint(
    double P,
    double B,
    double CovarianceAbs,
    double CovarianceSigned,
    double R,
    double VarI1,
    double VarTerms,
    double ConservationQuality,
    double Ordering,
    double Structure,
    double Geometry,
    double Quality);

public sealed record LatentFit(
    double[] Scores,
    double[] Loadings,
    double ExplainedVarianceRatio);

public sealed record ResidualPoint
{
    public VcFamily Family { get; init; }
    public string VariantName { get; init; } = "";
    public double P { get; init; }
    public double S { get; init; }
    public double D { get; init; }
    public double CovActual { get; init; }
    public double CovPred { get; init; }
    public double CovRes { get; init; }
    public double L { get; init; }
    public double DOVariance { get; init; }
    public double DOSkew { get; init; }
    public double DOKurtosis { get; init; }
    public double HierarchyDepth { get; init; }
    public double ChannelCount { get; init; }
    public double GeometryQuality { get; init; }
    public double Quality { get; init; }
    public double Ordering { get; init; }
    public double Structure { get; init; }
    public double BalanceB { get; init; }
}

public sealed record PriPoint
{
    public VcFamily Family { get; init; }
    public double P { get; init; }
    public double S { get; init; }
    public double D { get; init; }
    public double CovActual { get; init; }
    public double DOVariance { get; init; }
    public double DOSkew { get; init; }
    public double DOKurtosis { get; init; }
    public double HierarchyDepth { get; init; }
    public double ChannelCount { get; init; }
    public double GeometryQuality { get; init; }
    public double HalfMaxDist { get; init; }
    public double SlopeAtHalf { get; init; }
    public double CurvatureAtHalf { get; init; }
    public double CouplingBudget { get; init; }
    public double CouplingWidth { get; init; }
    public double Quality { get; init; }
    public double BalanceB { get; init; }
}

public enum VcFamily
{
    SAC,
    GAN,
    RCS,
    ICS,
    CNS
}

#endregion

public static class V7TestHelpers
{
    public static double Quantile(double[] sorted, double q)
    {
        if (sorted.Length == 0) return 0;
        if (q <= 0) return sorted[0];
        if (q >= 1) return sorted[^1];
        double pos = q * (sorted.Length - 1);
        int i0 = (int)Math.Floor(pos);
        int i1 = (int)Math.Ceiling(pos);
        if (i0 == i1) return sorted[i0];
        double t = pos - i0;
        return sorted[i0] * (1.0 - t) + sorted[i1] * t;
    }

    public static double SampleVariance(double[] x, double mean)
    {
        if (x.Length < 2) return 0;
        double sum = 0;
        for (int i = 0; i < x.Length; i++) { double d = x[i] - mean; sum += d * d; }
        return sum / (x.Length - 1);
    }

    public static double SampleCovariance(double[] x, double[] y, double mx, double my)
    {
        int n = Math.Min(x.Length, y.Length);
        if (n < 2) return 0;
        double sum = 0;
        for (int i = 0; i < n; i++) sum += (x[i] - mx) * (y[i] - my);
        return sum / (n - 1);
    }

    public static double PearsonCorrelation(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2) return 0.0;
        double mx = x.Average();
        double my = y.Average();
        double num = 0.0, dx2 = 0.0, dy2 = 0.0;
        for (int i = 0; i < x.Length; i++)
        {
            double dx = x[i] - mx, dy = y[i] - my;
            num += dx * dy;
            dx2 += dx * dx;
            dy2 += dy * dy;
        }
        double den = Math.Sqrt(dx2 * dy2);
        return den > 1e-15 ? num / den : 0.0;
    }

    public static double R2SinglePredictor(double[] target, double[] predictor)
    {
        if (target.Length < 4 || predictor.Length != target.Length) return 0.0;
        double my = target.Average(), mx = predictor.Average();
        double num = 0.0, den = 0.0;
        for (int i = 0; i < target.Length; i++) { num += (target[i] - my) * (predictor[i] - mx); den += (predictor[i] - mx) * (predictor[i] - mx); }
        double slope = den > 1e-15 ? num / den : 0.0;
        double intercept = my - slope * mx;
        double sse = 0.0, sst = 0.0;
        for (int i = 0; i < target.Length; i++)
        {
            double pred = intercept + slope * predictor[i];
            sse += (target[i] - pred) * (target[i] - pred);
            sst += (target[i] - my) * (target[i] - my);
        }
        return sst < 1e-15 ? 1.0 : 1.0 - sse / sst;
    }

    public static double FitModelR2(double[] target, double[][] predictors)
    {
        int n = target.Length, nPred = predictors.Length, nCols = nPred + 1;
        var xtx = new double[nCols, nCols];
        var xty = new double[nCols];
        for (int i = 0; i < n; i++)
        {
            xty[0] += target[i];
            xtx[0, 0] += 1.0;
            for (int a = 0; a < nPred; a++)
            {
                double xa = predictors[a][i];
                xty[a + 1] += xa * target[i];
                xtx[0, a + 1] += xa;
                xtx[a + 1, 0] += xa;
                for (int b = 0; b < nPred; b++)
                    xtx[a + 1, b + 1] += xa * predictors[b][i];
            }
        }
        const double ridge = 1e-6;
        for (int j = 1; j < nCols; j++) xtx[j, j] += ridge;
        var beta = SolveLinearSystemN(xtx, xty, nCols);
        if (!beta.All(double.IsFinite)) { beta = new double[nCols]; beta[0] = target.Average(); }
        double sse = 0.0, sst = 0.0;
        double my = target.Average();
        for (int i = 0; i < n; i++)
        {
            double px = beta[0];
            for (int a = 0; a < nPred; a++) px += beta[a + 1] * predictors[a][i];
            double diff = target[i] - px;
            sse += diff * diff;
            double dm = target[i] - my;
            sst += dm * dm;
        }
        return sst < 1e-15 ? 1.0 : 1.0 - sse / sst;
    }

    public static (double r2, double[] pred) FitModelWithPred(double[] target, double[][] predictors)
    {
        int n = target.Length, nPred = predictors.Length, nCols = nPred + 1;
        var xtx = new double[nCols, nCols];
        var xty = new double[nCols];
        for (int i = 0; i < n; i++)
        {
            xty[0] += target[i];
            xtx[0, 0] += 1.0;
            for (int a = 0; a < nPred; a++)
            {
                double xa = predictors[a][i];
                xty[a + 1] += xa * target[i];
                xtx[0, a + 1] += xa;
                xtx[a + 1, 0] += xa;
                for (int b = 0; b < nPred; b++)
                    xtx[a + 1, b + 1] += xa * predictors[b][i];
            }
        }
        const double ridge = 1e-6;
        for (int j = 1; j < nCols; j++) xtx[j, j] += ridge;
        var beta = SolveLinearSystemN(xtx, xty, nCols);
        if (!beta.All(double.IsFinite)) { beta = new double[nCols]; beta[0] = target.Average(); }
        var pred = new double[n];
        double sse = 0.0, sst = 0.0;
        double my = target.Average();
        for (int i = 0; i < n; i++)
        {
            double px = beta[0];
            for (int a = 0; a < nPred; a++) px += beta[a + 1] * predictors[a][i];
            pred[i] = px;
            double diff = target[i] - px;
            sse += diff * diff;
            double dm = target[i] - my;
            sst += dm * dm;
        }
        double r2 = sst < 1e-15 ? 1.0 : 1.0 - sse / sst;
        return (r2, pred);
    }

    public static double[] PredictFromModel(double[] target, double[][] predictors)
    {
        var (_, pred) = FitModelWithPred(target, predictors);
        return pred;
    }

    public static (double mi, double hx, double hy, double nmi) MutualInformationBinned(double[] x, double[] y, int bins)
    {
        if (x.Length != y.Length || x.Length < 2) return (0, 0, 0, 0);
        int n = x.Length;
        double xMin = x.Min(), xMax = x.Max(), yMin = y.Min(), yMax = y.Max();
        double dx = Math.Max(1e-12, xMax - xMin), dy = Math.Max(1e-12, yMax - yMin);
        var px = new double[bins]; var py = new double[bins]; var pxy = new double[bins, bins];
        for (int i = 0; i < n; i++)
        {
            int bx = (int)Math.Floor((x[i] - xMin) / dx * bins);
            int by = (int)Math.Floor((y[i] - yMin) / dy * bins);
            bx = Math.Clamp(bx, 0, bins - 1);
            by = Math.Clamp(by, 0, bins - 1);
            px[bx] += 1.0; py[by] += 1.0; pxy[bx, by] += 1.0;
        }
        for (int i = 0; i < bins; i++) { px[i] /= n; py[i] /= n; for (int j = 0; j < bins; j++) pxy[i, j] /= n; }
        static double H(double[] p) { double h = 0.0; foreach (var pi in p) if (pi > 1e-15) h -= pi * Math.Log(pi); return h; }
        double hx = H(px), hy = H(py), mi = 0.0;
        for (int i = 0; i < bins; i++)
            for (int j = 0; j < bins; j++)
            {
                double pij = pxy[i, j];
                if (pij <= 1e-15) continue;
                mi += pij * Math.Log(pij / (px[i] * py[j] + 1e-15));
            }
        double nmi = (hx > 1e-12 && hy > 1e-12) ? mi / Math.Sqrt(hx * hy) : 0.0;
        return (mi, hx, hy, Math.Clamp(nmi, 0.0, 1.0));
    }

    public static (double pc1, double pc2) FirstPrincipalExplainedVariance(double[] x, double[] y)
    {
        if (x.Length != y.Length || x.Length < 2) return (0.5, 0.5);
        double mx = x.Average(), my = y.Average(), vx = 0.0, vy = 0.0, cxy = 0.0;
        int n = x.Length;
        for (int i = 0; i < n; i++) { double dx = x[i] - mx, dy = y[i] - my; vx += dx * dx; vy += dy * dy; cxy += dx * dy; }
        vx /= Math.Max(1, n - 1); vy /= Math.Max(1, n - 1); cxy /= Math.Max(1, n - 1);
        double trace = vx + vy;
        if (trace < 1e-15) return (1.0, 0.0);
        double det = vx * vy - cxy * cxy;
        double disc = Math.Sqrt(Math.Max(0.0, trace * trace - 4.0 * det));
        double l1 = 0.5 * (trace + disc), l2 = 0.5 * (trace - disc);
        return (l1 / trace, l2 / trace);
    }

    public static double HarmonicMean(double a, double b) => 2.0 * a * b / (a + b + 1e-15);

    public static double[] SolveLinearSystemN(double[,] a, double[] b, int n)
    {
        var aug = new double[n, n + 1];
        for (int i = 0; i < n; i++) { for (int j = 0; j < n; j++) aug[i, j] = a[i, j]; aug[i, n] = b[i]; }
        for (int col = 0; col < n; col++)
        {
            int maxRow = col;
            for (int row = col + 1; row < n; row++)
                if (Math.Abs(aug[row, col]) > Math.Abs(aug[maxRow, col])) maxRow = row;
            for (int j = col; j <= n; j++) { double tmp = aug[col, j]; aug[col, j] = aug[maxRow, j]; aug[maxRow, j] = tmp; }
            if (Math.Abs(aug[col, col]) < 1e-15) continue;
            for (int row = col + 1; row < n; row++)
            {
                double factor = aug[row, col] / aug[col, col];
                for (int j = col; j <= n; j++) aug[row, j] -= factor * aug[col, j];
            }
        }
        var result = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            if (Math.Abs(aug[i, i]) < 1e-15) { result[i] = 0.0; continue; }
            double sum = aug[i, n];
            for (int j = i + 1; j < n; j++) sum -= aug[i, j] * result[j];
            result[i] = sum / aug[i, i];
        }
        return result;
    }

    public static double[] BuildDistanceEnsemble(int seed, int systems, int nodesPerSystem)
    {
        var all = new List<double>(systems * nodesPerSystem * nodesPerSystem / 2);
        for (int s = 0; s < systems; s++)
        {
            var rng = new Random(seed + s * 7919);
            var pts = new (double x, double y)[nodesPerSystem];
            for (int i = 0; i < nodesPerSystem; i++) pts[i] = (rng.NextDouble(), rng.NextDouble());
            for (int i = 0; i < nodesPerSystem; i++)
                for (int j = i + 1; j < nodesPerSystem; j++)
                {
                    double dx = pts[i].x - pts[j].x, dy = pts[i].y - pts[j].y;
                    all.Add(4.0 * Math.Sqrt(dx * dx + dy * dy));
                }
        }
        return all.ToArray();
    }

    public static List<VariantSpec> BuildAsymmetryVariants(int seed)
    {
        var list = BuildBalanceVariants(seed);
        foreach (var fam in new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS })
        {
            list.Add(new VariantSpec($"{fam}_LOW_A", fam, XiScale: 1.70, K0Scale: 1.00,
                Alpha: fam == VcFamily.RCS ? 0.40 : 0.30,
                Beta: fam switch { VcFamily.GAN => 0.97, VcFamily.ICS => 0.08, VcFamily.CNS => 0.97, _ => 0.0 },
                Gamma: fam switch { VcFamily.GAN => 0.03, VcFamily.CNS => 0.03, _ => 0.0 }));
            list.Add(new VariantSpec($"{fam}_LOW_B", fam, XiScale: 1.95, K0Scale: 1.00,
                Alpha: fam == VcFamily.RCS ? 0.35 : 0.24,
                Beta: fam switch { VcFamily.GAN => 0.98, VcFamily.ICS => 0.05, VcFamily.CNS => 0.98, _ => 0.0 },
                Gamma: fam switch { VcFamily.GAN => 0.02, VcFamily.CNS => 0.02, _ => 0.0 }));
            list.Add(new VariantSpec($"{fam}_HIGH_A", fam, XiScale: 0.42, K0Scale: 1.00,
                Alpha: fam == VcFamily.RCS ? 2.30 : 2.10,
                Beta: fam switch { VcFamily.GAN => 0.90, VcFamily.ICS => 0.45, VcFamily.CNS => 0.90, _ => 0.0 },
                Gamma: fam switch { VcFamily.GAN => 0.09, VcFamily.CNS => 0.08, _ => 0.0 }));
            list.Add(new VariantSpec($"{fam}_HIGH_B", fam, XiScale: 0.33, K0Scale: 1.00,
                Alpha: fam == VcFamily.RCS ? 2.90 : 2.65,
                Beta: fam switch { VcFamily.GAN => 0.88, VcFamily.ICS => 0.55, VcFamily.CNS => 0.88, _ => 0.0 },
                Gamma: fam switch { VcFamily.GAN => 0.10, VcFamily.CNS => 0.10, _ => 0.0 }));
        }
        return list;
    }

    public static List<VariantSpec> BuildBalanceVariants(int seed)
    {
        var rng = new Random(seed);
        var list = new List<VariantSpec>();
        foreach (var fam in new[] { VcFamily.SAC, VcFamily.GAN, VcFamily.RCS, VcFamily.ICS, VcFamily.CNS })
        {
            for (int i = 0; i < 12; i++)
            {
                double xiScale = 0.70 + 0.60 * rng.NextDouble();
                double k0Scale = 0.80 + 0.40 * rng.NextDouble();
                double alpha = fam == VcFamily.RCS ? 0.75 + 0.90 * rng.NextDouble() : 0.80 + 0.70 * rng.NextDouble();
                double beta = fam switch { VcFamily.GAN => 0.85 + 0.12 * rng.NextDouble(), VcFamily.ICS => 0.20 + 0.30 * rng.NextDouble(), VcFamily.CNS => 0.82 + 0.14 * rng.NextDouble(), _ => 0.0 };
                double gamma = fam switch { VcFamily.GAN => 0.05 + 0.09 * rng.NextDouble(), VcFamily.CNS => 0.06 + 0.08 * rng.NextDouble(), _ => 0.0 };
                list.Add(new VariantSpec($"{fam}_{i + 1:00}", fam, xiScale, k0Scale, alpha, beta, gamma));
            }
        }
        return list;
    }

    public static BspPoint EvaluateVariantAtP(double[] distances, double[] sortedDistances, double xiBase, double k0Base, double p, VariantSpec variant)
    {
        int n = distances.Length;
        double[] k = new double[n];
        double xi = xiBase * variant.XiScale;
        double k0 = k0Base * variant.K0Scale;
        double md = distances.Average();
        for (int i = 0; i < n; i++)
        {
            double x = distances[i] / (xi + 1e-15);
            double kp = variant.Family switch
            {
                VcFamily.SAC => k0 * Math.Exp(-variant.Alpha * Math.Pow(x, p)),
                VcFamily.GAN => k0 * Math.Exp(-variant.Alpha * Math.Pow(x, p)) * (variant.Beta + variant.Gamma * Math.Cos(1.15 * x)),
                VcFamily.RCS => k0 / (1.0 + variant.Alpha * Math.Pow(x, p)),
                VcFamily.ICS => k0 * Math.Exp(-Math.Pow(x, variant.Alpha * p + variant.Beta)),
                VcFamily.CNS => (k0 * Math.Exp(-variant.Alpha * Math.Pow(x, p)) * (variant.Beta - variant.Gamma * Math.Exp(-1.6 * x))) + 0.03 * k0,
                _ => k0 * Math.Exp(-Math.Pow(x, p))
            };
            if (kp < 0) kp = 0;
            if (kp > k0) kp = k0;
            k[i] = kp;
        }
        double mk = k.Average(), vk = SampleVariance(k, mk), vd = SampleVariance(distances, md);
        double cov = SampleCovariance(k, distances, mk, md);
        double r = 0.42 * Math.Abs(cov) / (0.49 * vk + 0.09 * vd + 1e-15);
        double qNear = Quantile(sortedDistances, 0.25), qFar = Quantile(sortedDistances, 0.75);
        double nearMean = 0, farMean = 0; int nearN = 0, farN = 0;
        for (int i = 0; i < n; i++) { if (distances[i] <= qNear) { nearMean += k[i]; nearN++; } if (distances[i] >= qFar) { farMean += k[i]; farN++; } }
        nearMean /= Math.Max(1, nearN); farMean /= Math.Max(1, farN);
        double suppression = Math.Max(0.0, 1.0 - mk / (k0 + 1e-15));
        double separation = Math.Max(0.0, (nearMean - farMean) / (Math.Abs(nearMean) + 1e-15));
        double retention = 4.0 * (mk / (k0 + 1e-15)) * (1.0 - mk / (k0 + 1e-15));
        retention = Math.Clamp(retention, 0.0, 1.0);
        double discrimination = separation * retention;
        double q = suppression * discrimination;
        double ordering = 2.0 * r * q / (r + q + 1e-15);
        double structure = 2.0 * suppression * discrimination / (suppression + discrimination + 1e-15);
        double systemQuality = 2.0 * ordering * structure / (ordering + structure + 1e-15);
        return new BspPoint(p, suppression, discrimination, r, ordering, structure, systemQuality, q);
    }

    public static CciPoint EvaluateCciVariantAtP(double[] distances, double[] sortedDistances, double xiBase, double k0Base, double p, VariantSpec variant)
    {
        int n = distances.Length;
        double[] k = new double[n], i1 = new double[n];
        double xi = xiBase * variant.XiScale, k0 = k0Base * variant.K0Scale, md = distances.Average();
        for (int i = 0; i < n; i++)
        {
            double x = distances[i] / (xi + 1e-15);
            double kp = variant.Family switch
            {
                VcFamily.SAC => k0 * Math.Exp(-variant.Alpha * Math.Pow(x, p)),
                VcFamily.GAN => k0 * Math.Exp(-variant.Alpha * Math.Pow(x, p)) * (variant.Beta + variant.Gamma * Math.Cos(1.15 * x)),
                VcFamily.RCS => k0 / (1.0 + variant.Alpha * Math.Pow(x, p)),
                VcFamily.ICS => k0 * Math.Exp(-Math.Pow(x, variant.Alpha * p + variant.Beta)),
                VcFamily.CNS => (k0 * Math.Exp(-variant.Alpha * Math.Pow(x, p)) * (variant.Beta - variant.Gamma * Math.Exp(-1.6 * x))) + 0.03 * k0,
                _ => k0 * Math.Exp(-Math.Pow(x, p))
            };
            if (kp < 0) kp = 0; if (kp > k0) kp = k0;
            k[i] = kp; i1[i] = V6Geometry.ComputeI1(kp, distances[i]);
        }
        double mk = k.Average(), vk = SampleVariance(k, mk), vd = SampleVariance(distances, md);
        double covSigned = SampleCovariance(k, distances, mk, md), covAbs = Math.Abs(covSigned);
        double vt = 0.49 * vk + 0.09 * vd, r = 0.42 * covAbs / (vt + 1e-15);
        double i1Mean = i1.Average(), varI1 = SampleVariance(i1, i1Mean);
        double conservationQuality = 1.0 - varI1 / (vt + 1e-15);
        double qNear = Quantile(sortedDistances, 0.25), qFar = Quantile(sortedDistances, 0.75);
        double nearMean = 0.0, farMean = 0.0; int nearN = 0, farN = 0;
        for (int i = 0; i < n; i++) { if (distances[i] <= qNear) { nearMean += k[i]; nearN++; } if (distances[i] >= qFar) { farMean += k[i]; farN++; } }
        nearMean /= Math.Max(nearN, 1); farMean /= Math.Max(farN, 1);
        double suppression = Math.Max(0.0, 1.0 - mk / (k0 + 1e-15));
        double separation = Math.Max(0.0, (nearMean - farMean) / (Math.Abs(nearMean) + 1e-15));
        double retention = 4.0 * (mk / (k0 + 1e-15)) * (1.0 - mk / (k0 + 1e-15));
        retention = Math.Clamp(retention, 0.0, 1.0);
        double discrimination = separation * retention;
        double b = suppression / (suppression + discrimination + 1e-15);
        double ordering = 2.0 * r * (suppression * discrimination) / (r + suppression * discrimination + 1e-15);
        double structure = 2.0 * suppression * discrimination / (suppression + discrimination + 1e-15);
        double geometry = r * discrimination;
        double quality = HarmonicMean(HarmonicMean(ordering + 1e-12, structure + 1e-12), geometry + 1e-12);
        return new CciPoint(p, b, covAbs, covSigned, r, varI1, vt, conservationQuality, ordering, structure, geometry, quality);
    }

    public static double GammaApprox(double x)
    {
        if (x <= 0) return 1.0;
        if (x < 0.5) return Math.PI / (Math.Sin(Math.PI * x) * GammaApprox(1.0 - x));
        double z = x - 1.0;
        double[] pp = { 1.000000000190015, 76.18009172947146, -86.50532032941677, 24.01409824083091, -1.231739572450155, 1.208650973866179e-3, -5.395239384953e-6 };
        double sum = pp[0]; double w = z + 5.5;
        for (int i = 1; i < pp.Length; i++) sum += pp[i] / (z + i);
        return Math.Sqrt(2.0 * Math.PI) * Math.Pow(w, z + 0.5) * Math.Exp(-w) * sum;
    }
}
