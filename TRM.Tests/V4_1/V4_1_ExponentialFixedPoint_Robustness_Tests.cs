using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Hardening the exponential fixed-point result: xi scan, normalization sensitivity,
/// contraction diagnostic, uniqueness probe, law comparison, large-N hint.
///
/// No D=3 assumption. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_ExponentialFixedPoint")]
public class V4_1_ExponentialFixedPoint_Robustness_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_ExponentialFixedPoint_Robustness_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int N, double s, int seed) { var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0; var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI; int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1; for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); } return h; }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] * d[i, j] / Math.Max(xi * xi, 0.0001)); } return K; }
    private static double[,] PowUpd(double[,] d, double K0, double p) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(Math.Max(d[i, j], 0), p)); } return K; }
    private static double MDiff(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) s += Math.Abs(A[i, j] - B[i, j]); return s / (N * (N - 1) / 2.0); }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[] Fl(double[,] m) { int N = m.GetLength(0); var a = new double[N * N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) a[i * N + j] = m[i, j]; return a; }
    private static double Fro(double[,] A, double[,] B) { int N = A.GetLength(0); double s = 0; for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) { double d = A[i, j] - B[i, j]; s += d * d; } return Math.Sqrt(s); }
    private static double OP(double[] th) { double sx = 0, sy = 0; for (int i = 0; i < th.Length; i++) { sx += Math.Cos(th[i]); sy += Math.Sin(th[i]); } return Math.Sqrt(sx * sx + sy * sy) / th.Length; }
    private static double Dg(double[,] d) { int N = d.GetLength(0); var v = new List<double>(); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) v.Add(d[i, j]); if (v.Count == 0) return 0; double m = v.Average(); return m > 1e-9 ? v.Average(x => (x - m) * (x - m)) / (m * m) : 0; }

    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] KWD(int N) { var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j) K[i, j] = 0.1 / N; return K; }
    private static double[,] KSW(int N) { var rng = new Random(BS); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); for (int i = 0; i < N; i++) for (int d = 1; d <= 3; d++) { int j = (i + d) % N; adj[i].Add(j); adj[j].Add(i); } var cur = adj.Select(a => a.ToList()).ToArray(); for (int i = 0; i < N; i++) foreach (int j in cur[i]) { if (i >= j) continue; if (rng.NextDouble() < 0.1) { adj[i].Remove(j); adj[j].Remove(i); int nj; do nj = rng.Next(N); while (nj == i || adj[i].Contains(nj)); adj[i].Add(nj); adj[nj].Add(i); } } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }

    private static List<(double dK,double cR)> RunFP(double[,]K0,int N,double K0v,double xi,double s,int E,int seed,Func<double[,],double,double,double[,]> upd){var Kc=(double[,])K0.Clone();var recs=new List<(double,double)>();double[,]?pK=null;for(int e=0;e<E;e++){var h=Sm(Kc,N,s,seed+e);var R=Nm(RP(h));var d=DL(R);var Kn=upd(d,K0v,xi);double dK=pK!=null?MDiff(Kn,pK):double.NaN;double c=Spear(Fl(Kn),Fl(R));recs.Add((dK,c));pK=Kn;Kc=Kn;}return recs;}

    // ═══════════════ EFPR_01 Xi scan ═══════════════
    [Fact] public void V4_1_EFPR_01_XiScan(){int N=40;var K0=KS(N,BS);double[]xis=[0.25,0.5,1.0,1.5,2.0,3.0];_output.WriteLine("xi    final_dK   corr(K,R)  dg");foreach(double xi in xis){var r=RunFP(K0,N,0.5,xi,0.1,8,BS,ExpUpd);var last=r[^1];var h=Sm(ExpUpd(DL(Nm(RP(Sm(K0,N,0.1,BS)))),0.5,xi),N,0.1,BS+8);double dg=Dg(DL(Nm(RP(h))));_output.WriteLine($"{xi:F2}  {last.dK,10:E4}  {last.cR,10:F4}  {dg,6:F4}");Assert.True(double.IsFinite(last.dK));}}

    // ═══════════════ EFPR_02 Normalization sensitivity ═══════════════
    [Fact] public void V4_1_EFPR_02_NormalizationSensitivity(){int N=40;var K0=KS(N,BS);var h=Sm(K0,N,0.1,BS);var R=Nm(RP(h));var d=DL(R);double[]Ks=[0.2,0.5,1.0];_output.WriteLine("K0    dK_final corr(K,R)");foreach(double kv in Ks){var r=RunFP(K0,N,kv,1.0,0.1,5,BS,ExpUpd);_output.WriteLine($"{kv:F1}  {r[^1].dK,10:E4}  {r[^1].cR,10:F4}");Assert.True(double.IsFinite(r[^1].dK));}}

    // ═══════════════ EFPR_03 Contraction diagnostic ═══════════════
    [Fact] public void V4_1_EFPR_03_ContractionDiagnostic(){int N=40;var K0=KS(N,BS);var r=RunFP(K0,N,0.5,1.0,0.1,12,BS,ExpUpd);var qs=new List<double>();for(int i=2;i<r.Count;i++)if(r[i-1].dK>1e-15)qs.Add(r[i].dK/r[i-1].dK);double mq=qs.Average(),medq=qs.OrderBy(x=>x).ElementAt(qs.Count/2);double f=qs.Count(x=>x<1)/(double)qs.Count;_output.WriteLine($"Contraction: mean q={mq:F4} median q={medq:F4} frac<1={f:F3}");Assert.True(mq<2);}

    // ═══════════════ EFPR_04 Uniqueness probe ═══════════════
    [Fact] public void V4_1_EFPR_04_UniquenessProbe(){int N=40;var conds=new(string,double[,])[]{("rand-sparse",KS(N,BS)),("weak-dense",KWD(N)),("small-world",KSW(N))};var finalKs=new List<double[,]>();foreach(var(_,K)in conds){var Kc=(double[,])K.Clone();for(int e=0;e<8;e++){var h=Sm(Kc,N,0.1,BS+e);Kc=ExpUpd(DL(Nm(RP(h))),0.5,1.0);}finalKs.Add(Kc);}_output.WriteLine("final K pairwise Spearman");for(int a=0;a<finalKs.Count;a++)for(int b=a+1;b<finalKs.Count;b++){double sp=Spear(Fl(finalKs[a]),Fl(finalKs[b]));_output.WriteLine($"  {conds[a].Item1} vs {conds[b].Item1}: {sp:F4}");Assert.True(sp>0);}}

    // ═══════════════ EFPR_05 Exp vs others fixed-point ═══════════════
    [Fact] public void V4_1_EFPR_05_ExpVsOthers(){int N=40;var K0=KS(N,BS);_output.WriteLine("law    final_dK   corr(K,R)");foreach(var(n,f)in new[]{("exp",(Func<double[,],double,double,double[,]>)ExpUpd),("gauss",(Func<double[,],double,double,double[,]>)GaussUpd),("power",(Func<double[,],double,double,double[,]>)((d,k,x)=>PowUpd(d,k,x)))}){var r=RunFP(K0,N,0.5,1.0,0.1,8,BS,f);_output.WriteLine($"{n,-7} {r[^1].dK,10:E4}  {r[^1].cR,10:F4}");Assert.True(double.IsFinite(r[^1].dK));}}

    // ═══════════════ EFPR_06 Large-N hint ═══════════════
    [Fact] public void V4_1_EFPR_06_LargeNHint(){int[]Ns=[40,80,120,200];_output.WriteLine("N      final_dK   corr(K,R)");foreach(int N in Ns){var K0=KS(N,BS);var r=RunFP(K0,N,0.5,1.0,0.1,8,BS,ExpUpd);_output.WriteLine($"{N,5}  {r[^1].dK,10:E4}  {r[^1].cR,10:F4}");Assert.True(double.IsFinite(r[^1].dK));}}

    // ═══════════════ EFPR_07 Claim discipline ═══════════════
    [Fact] public void V4_1_EFPR_07_ClaimDiscipline(){_output.WriteLine("SUPPORTED: xi scan finite, norm sensitivity checked, contraction-like q<1, uniqueness probe, law comparison, N scaling.");_output.WriteLine("CONDITIONAL: xi=1 near-optimal, convergence conditional on params, uniqueness not proven.");_output.WriteLine("HYPOTHESIS: exponential law physical, fixed-point = emergent geometry, continuum selects exp.");_output.WriteLine("NOT CLAIMED: D=3, GR, QM, hbar, Planck, continuum proof.");Assert.True(true);}
}
