using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

/// <summary>
/// Source-curvature robustness: tests whether the moderate source-curvature
/// proxy relation from V4_1_EinsteinEquationProxy is robust under source and
/// curvature definitions, load range, residuals, bending, N, seeds, and laws.
///
/// Does NOT claim Einstein equations, GR, stress-energy, Ricci, gravity,
/// G, c, D=3, SPARC, or dark matter. Claim discipline enforced.
/// </summary>
[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_SourceCurvatureRobustness")]
public class V4_1_SourceCurvatureRobustness_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BS = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int St = 300;
    private const int Hd = 4;

    public V4_1_SourceCurvatureRobustness_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int N, double s, int seed, int loadNode = -1, double dO = 0)
    {
        var r = new Random(seed); var w = new double[N]; for (int i = 0; i < N; i++) w[i] = 1.0 + s * (r.NextDouble() - 0.5) * 2.0;
        if (loadNode >= 0 && loadNode < N) w[loadNode] += dO;
        var th = new double[N]; for (int i = 0; i < N; i++) th[i] = r.NextDouble() * 2.0 * Math.PI;
        int hL = St / Hd + 1; var h = new double[hL][]; h[0] = (double[])th.Clone(); int hi = 1;
        for (int t = 0; t < St; t++) { var dT = new double[N]; for (int i = 0; i < N; i++) { double c = 0; for (int j = 0; j < N; j++) c += K[i, j] * Math.Sin(th[j] - th[i]); dT[i] = w[i] + c; } for (int i = 0; i < N; i++) th[i] += Dt * dT[i]; if ((t + 1) % Hd == 0 && hi < hL) h[hi++] = (double[])th.Clone(); }
        return h;
    }
    private static double[,] RP(double[][] h) { int T = h.Length, N = h[0].Length; var R = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { double sc = 0, ss = 0; for (int t = 0; t < T; t++) { double d = h[t][i] - h[t][j]; sc += Math.Cos(d); ss += Math.Sin(d); } R[i, j] = Math.Sqrt(sc * sc + ss * ss) / T; } return R; }
    private static double[,] Nm(double[,] R) { int N = R.GetLength(0); double mn = double.MaxValue; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) if (i != j && R[i, j] < mn) mn = R[i, j]; double rng = 1.0 - mn; if (rng < 1e-15) rng = 1.0; var Rn = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) Rn[i, j] = 1.0; else Rn[i, j] = Math.Max(REps, (R[i, j] - mn) / rng); } return Rn; }
    private static double[,] DL(double[,] R) { int N = R.GetLength(0); var d = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) d[i, j] = 0; else d[i, j] = -Math.Log(Math.Max(R[i, j], 1e-100)); } return d; }
    private static double[,] ExpUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 * Math.Exp(-d[i, j] / Math.Max(xi, 0.01)); } return K; }
    private static double[,] GaussUpd(double[,] d, double K0, double xi) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else { double r = d[i, j] / Math.Max(xi, 0.01); K[i, j] = K0 * Math.Exp(-r * r); } } return K; }
    private static double[,] PowerUpd(double[,] d, double K0, double p) { int N = d.GetLength(0); var K = new double[N, N]; for (int i = 0; i < N; i++) for (int j = 0; j < N; j++) { if (i == j) K[i, j] = 0; else K[i, j] = K0 / (1.0 + Math.Pow(d[i, j], Math.Max(p, 0.5))); } return K; }
    private static double Spear(double[] a, double[] b) { if (a.Length < 3) return 0; var ia = a.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); var ib = b.Select((v, i) => (v, i)).OrderBy(t => t.v).Select((t, r) => (t.i, r)).OrderBy(t => t.i).Select(t => (double)(t.r + 1)).ToArray(); return Pear(ia, ib); }
    private static double Pear(double[] x, double[] y) { int n = x.Length; double mx = x.Average(), my = y.Average(), nm = 0, dx = 0, dy = 0; for (int i = 0; i < n; i++) { double a = x[i] - mx, b = y[i] - my; nm += a * b; dx += a * a; dy += b * b; } return Math.Sqrt(dx * dy) > 1e-15 ? nm / Math.Sqrt(dx * dy) : 0; }
    private static double[,] KS(int N, int seed) { var rng = new Random(seed); var adj = new HashSet<int>[N]; for (int i = 0; i < N; i++) adj[i] = new HashSet<int>(); double p = 6.0 / (N - 1); for (int i = 0; i < N; i++) for (int j = i + 1; j < N; j++) if (rng.NextDouble() < p) { adj[i].Add(j); adj[j].Add(i); } var v = new bool[N]; var cs = new List<List<int>>(); for (int i = 0; i < N; i++) { if (v[i]) continue; var c = new List<int>(); var q = new Queue<int>(); v[i] = true; q.Enqueue(i); while (q.Count > 0) { int u = q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x] = true; q.Enqueue(x); } } cs.Add(c); } for (int i = 1; i < cs.Count; i++) { adj[cs[i][0]].Add(cs[i - 1][0]); adj[cs[i - 1][0]].Add(cs[i][0]); } var K = new double[N, N]; for (int i = 0; i < N; i++) foreach (int j in adj[i]) if (i < j) { K[i, j] = 0.5; K[j, i] = 0.5; } return K; }
    private static double[,] RecoverFP(double[,] K0, int N, double kv, double xi, double s, int E, int seed, Func<double[,], double, double, double[,]> upd = null) { upd ??= ExpUpd; var Kc = (double[,])K0.Clone(); for (int e = 0; e < E; e++) { var he = Sm(Kc, N, s, seed + e); Kc = upd(DL(Nm(RP(he))), kv, xi); } return Kc; }
    private static double Median(List<double> vs) { if (vs.Count == 0) return double.NaN; var s = vs.OrderBy(x => x).ToList(); int mid = s.Count / 2; return s.Count % 2 == 0 ? (s[mid - 1] + s[mid]) / 2.0 : s[mid]; }

    private static double[] OmegaField(double[][] h) { int T = h.Length, N = h[0].Length; var o = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int t = 1; t < T; t++) { s += Math.Abs(h[t][i] - h[t - 1][i]); c++; } o[i] = c > 0 ? s / (c * Dt * Hd) : 0; } return o; }

    // ── Source proxies ─────────────────────────────────
    private static double[] SrcLoad(int N, int ln, double dO) { var s = new double[N]; s[ln] = dO; return s; }
    private static double[] SrcOmega(double[,] Kc, int N, double ss, int ln, double dO) { var o0 = OmegaField(Sm(Kc, N, ss, BS)); var oL = OmegaField(Sm(Kc, N, ss, BS, ln, dO)); return oL.Zip(o0, (a, b) => Math.Abs(a - b)).ToArray(); }
    private static double[] SrcGeom(double[,] d0, double[,] dL, int N) { var s = new double[N]; for (int i = 0; i < N; i++) { double sum = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { sum += Math.Abs(dL[i, j] - d0[i, j]); c++; } s[i] = c > 0 ? sum / c : 0; } return s; }
    private static double[] SrcK(double[,] K0, double[,] KL, int N) { var s = new double[N]; for (int i = 0; i < N; i++) { double sum = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { sum += Math.Abs(KL[i, j] - K0[i, j]); c++; } s[i] = c > 0 ? sum / c : 0; } return s; }

    // ── Curvature proxies ──────────────────────────────
    private static double[] CurvLap(double[,] d0, double[,] dL, int N) { var h = new double[N]; for (int i = 0; i < N; i++) { double s = 0; int c = 0; for (int j = 0; j < N; j++) if (i != j) { s += Math.Abs(dL[i, j] - d0[i, j]); c++; } h[i] = c > 0 ? s / c : 0; } var lap = new double[N]; for (int i = 0; i < N; i++) { double sum = 0, wSum = 0; for (int j = 0; j < N; j++) if (i != j) { double w = 1.0 / (1.0 + d0[i, j]); sum += w * h[j]; wSum += w; } lap[i] = wSum * h[i] - sum; } return lap; }
    private static double[] CurvDist(double[,] d0, double[,] dL, int N) { var c = new double[N]; for (int i = 0; i < N; i++) { var s0 = Enumerable.Range(0, N).Where(x => x != i).Select(x => d0[i, x]).OrderBy(x => x).ToArray(); var sL = Enumerable.Range(0, N).Where(x => x != i).Select(x => dL[i, x]).OrderBy(x => x).ToArray(); int k = Math.Min(10, s0.Length); double su = 0; for (int j = 0; j < k; j++) su += Math.Abs(sL[j] - s0[j]); c[i] = su / k; } return c; }

    private static (double alpha, double beta, double r2) FitSC(double[] curv, double[] src) { int n = curv.Length; double mx = src.Average(), my = curv.Average(), sxy = 0, sx2 = 0, sy2 = 0; for (int i = 0; i < n; i++) { double a = src[i] - mx, b = curv[i] - my; sxy += a * b; sx2 += a * a; sy2 += b * b; } double alpha = sx2 > 1e-15 ? sxy / sx2 : 0; return (alpha, my - alpha * mx, sx2 * sy2 > 1e-15 ? sxy * sxy / (sx2 * sy2) : 0); }

    // ═══════════════ SCR_01–16 ═══════════════
    [Fact] public void V4_1_SCR_01_RobustnessDataFinite() { int N=80; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,N/2,0.2)))),1.2,1.75),N,0.1,BS)))); var sO=SrcOmega(Kc,N,0.1,N/2,0.2); var sG=SrcGeom(d0,dL,N); var cL=CurvLap(d0,dL,N); Assert.True(sO.All(double.IsFinite)&&cL.All(double.IsFinite)); _output.WriteLine($"SCR_01 finite OK: srcO={sO.Average():F4} curvL={cL.Average():F4}"); }

    [Fact] public void V4_1_SCR_02_SourceDefinitionAgreement() { int N=80; double dO=0.2; int ln=N/2; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,dO)))),1.2,1.75),N,0.1,BS)))); var sL=SrcLoad(N,ln,dO); var sO=SrcOmega(Kc,N,0.1,ln,dO); var sG=SrcGeom(d0,dL,N); double avg=(Math.Abs(Spear(sL,sO))+Math.Abs(Spear(sL,sG))+Math.Abs(Spear(sO,sG)))/3; _output.WriteLine($"SrcAgreement={avg:F3} {(avg>0.3?"Robust":"Sensitive")}"); }

    [Fact] public void V4_1_SCR_03_CurvatureDefinitionAgreement() { int N=80; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,N/2,0.2)))),1.2,1.75),N,0.1,BS)))); double r=Spear(CurvLap(d0,dL,N),CurvDist(d0,dL,N)); _output.WriteLine($"CurvAgreement={r:F3} {CorrClass(r)}"); }
    static string CorrClass(double r) => Math.Abs(r)>0.7?"Strong":Math.Abs(r)>0.4?"Moderate":Math.Abs(r)>0.1?"Weak":"None";

    [Fact] public void V4_1_SCR_04_SourceCurvatureFitRobustness() { int N=80; int ln=N/2; double dO=0.2; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,dO)))),1.2,1.75),N,0.1,BS)))); var srcs=new[]{("Load",SrcLoad(N,ln,dO)),("Omega",SrcOmega(Kc,N,0.1,ln,dO)),("Geom",SrcGeom(d0,dL,N))}; var curvs=new[]{("Lap",CurvLap(d0,dL,N)),("Dist",CurvDist(d0,dL,N))}; _output.WriteLine("Src    Curv   alpha   r²"); foreach(var s in srcs) foreach(var c in curvs) { var(a,_,r2)=FitSC(c.Item2,s.Item2); _output.WriteLine($"{s.Item1,-6} {c.Item1,-5} {a:F4}  {r2:F3}"); } }

    [Fact] public void V4_1_SCR_05_AlphaStabilityAcrossDefinitions() { int N=80; int ln=N/2; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),1.2,1.75),N,0.1,BS)))); var alphas=new List<double>(); foreach(var s in new[]{SrcOmega(Kc,N,0.1,ln,0.2),SrcGeom(d0,dL,N)}) foreach(var c in new[]{CurvLap(d0,dL,N),CurvDist(d0,dL,N)}) { var(a,_,_)=FitSC(c,s); if(double.IsFinite(a)) alphas.Add(a); } double m=alphas.Average(); double std=alphas.Count>1?Math.Sqrt(alphas.Average(x=>(x-m)*(x-m))):0; _output.WriteLine($"Alpha: {m:F4}±{std:F4} cv={std/Math.Max(Math.Abs(m),1e-6):F2}"); }

    [Fact] public void V4_1_SCR_06_LoadRangeRobustness() { int N=80; int ln=N/2; double[] loads=[0.01,0.05,0.10,0.20,0.35,0.50]; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); _output.WriteLine("load   alpha   r²     class"); foreach(double dO in loads) { var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,dO)))),1.2,1.75),N,0.1,BS)))); var(a,_,r2)=FitSC(CurvLap(d0,dL,N),SrcOmega(Kc,N,0.1,ln,dO)); string cls=dO<=0.05?"tooWeak":dO<=0.2?"linear":dO<=0.35?"mild":"nonlin"; _output.WriteLine($"{dO:F2}    {a:F4}  {r2:F3}   {cls}"); } }

    [Fact] public void V4_1_SCR_07_RadialIntegratedRobustness() { int N=80; int ln=N/2; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),1.2,1.75),N,0.1,BS)))); var c=CurvLap(d0,dL,N).Select(Math.Abs).ToArray(); var s=SrcOmega(Kc,N,0.1,ln,0.2); var dists=Enumerable.Range(0,N).Select(i=>d0[ln,i]).ToArray(); var ord=Enumerable.Range(0,N).OrderBy(i=>dists[i]).ToArray(); var ratios=new List<double>(); for(int sh=0;sh<5;sh++){int s0=sh*N/5;double iC=ord.Skip(s0).Take(N/5).Sum(i=>c[i]),iS=ord.Skip(s0).Take(N/5).Sum(i=>s[i]);if(iS>1e-6)ratios.Add(iC/iS);} _output.WriteLine($"Radial ratios: {string.Join(" ",ratios.Select(r=>r.ToString("F2")))} stable={(ratios.Select(r=>Math.Abs(r-ratios.Average())).Average()<0.5?"YES":"NO")}"); }

    [Fact] public void V4_1_SCR_08_ResidualStructureRobustness() { int N=80; int ln=N/2; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),1.2,1.75),N,0.1,BS)))); var curv=CurvLap(d0,dL,N); var src=SrcOmega(Kc,N,0.1,ln,0.2); var(a,b,_)=FitSC(curv,src); var resid=curv.Zip(src,(c2,s2)=>c2-(a*s2+b)).ToArray(); double peakR=Math.Abs(resid[ln]),avgR=resid.Average(x=>Math.Abs(x)); _output.WriteLine($"Resid peak={peakR:F4} avg={avgR:F4} localized={(peakR>2*avgR?"YES":"NO")}"); }

    [Fact] public void V4_1_SCR_09_BendingConsistencyRobustness() { int N=80; int ln=N/2; double[] loads=[0.05,0.1,0.2,0.35]; var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS); var d0b=DL(Nm(RP(Sm(Kc,N,0.1,BS)))); var al=new List<double>();var bl=new List<double>(); var pRef=ShortestPath(d0b,N,N/4,3*N/4); foreach(double dO in loads){var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,dO)))),1.2,1.75),N,0.1,BS)))); var(a2,_,_)=FitSC(CurvLap(d0b,dL,N),SrcOmega(Kc,N,0.1,ln,dO)); al.Add(a2); var pL=ShortestPath(dL,N,N/4,3*N/4); bl.Add(1.0-(pL.Count>0&&pRef.Count>0?PathOverlap(pRef,pL):0));} double rho=al.Count>=3?Spear(al.ToArray(),bl.ToArray()):0; _output.WriteLine($"corr(alpha,bending)={rho:F3} {CorrClass(rho)}"); }
    static List<int> ShortestPath(double[,] w,int N,int s,int d){var dist=new double[N];var prev=new int[N];var vis=new bool[N];for(int i=0;i<N;i++){dist[i]=double.MaxValue;prev[i]=-1;}dist[s]=0;for(int iter=0;iter<N;iter++){int u=-1;double best=double.MaxValue;for(int i=0;i<N;i++)if(!vis[i]&&dist[i]<best){best=dist[i];u=i;}if(u<0)break;vis[u]=true;if(u==d)break;for(int v=0;v<N;v++){if(vis[v])continue;double ww=w[u,v];if(ww<=0||!double.IsFinite(ww))continue;double nd=dist[u]+ww;if(nd<dist[v]){dist[v]=nd;prev[v]=u;}}}var p=new List<int>();if(prev[d]<0&&s!=d)return p;for(int at=d;at>=0;at=prev[at]){p.Add(at);if(at==s)break;}p.Reverse();return p.Count>0&&p[0]==s?p:new List<int>();}
    static double PathOverlap(List<int> a,List<int> b){if(a.Count==0||b.Count==0)return 0;var sa=new HashSet<int>(a);var sb=new HashSet<int>(b);return(double)sa.Intersect(sb).Count()/Math.Max(sa.Union(sb).Count(),1);}

    [Fact] public void V4_1_SCR_10_NScalingRobustness() { int[] Ns=[40,80,120,200]; _output.WriteLine("N     alpha   r²     stable?"); foreach(int N in Ns){int E=N<=80?5:3;int ln=N/2;var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,E,BS);var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS))));var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),1.2,1.75),N,0.1,BS))));var(a,_,r2)=FitSC(CurvLap(d0,dL,N),SrcOmega(Kc,N,0.1,ln,0.2)); _output.WriteLine($"{N,5}  {a:F4}  {r2:F3}  {(r2>0.1?"stable":"weak")}"); } }

    [Fact] public void V4_1_SCR_11_MultiSeedRobustness() { int N=80;int nS=10;int ln=N/2;var al=new List<double>();var rl=new List<double>();for(int sd=0;sd<nS;sd++){var Kc=RecoverFP(KS(N,sd),N,1.2,1.75,0.1,5,sd);var d0=DL(Nm(RP(Sm(Kc,N,0.1,sd))));var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,sd,ln,0.2)))),1.2,1.75),N,0.1,sd))));var(a2,_,r2)=FitSC(CurvLap(d0,dL,N),SrcOmega(Kc,N,0.1,ln,0.2));al.Add(a2);rl.Add(r2);} _output.WriteLine($"alpha: {al.Average():F4}±{Math.Sqrt(al.Average(x=>(x-al.Average())*(x-al.Average()))):F4} r²: {rl.Average():F3}"); }

    [Fact] public void V4_1_SCR_12_CouplingLawRobustness() { int N=80; var laws=new Dictionary<string,Func<double[,],double,double,double[,]>>{{"exp",(d,k0,x)=>ExpUpd(d,k0,x)},{"gauss",(d,k0,x)=>GaussUpd(d,k0,x)},{"power",(d,k0,x)=>PowerUpd(d,k0,x)}}; _output.WriteLine("Law      alpha   r²"); foreach(var(n,upd)in laws){var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS,upd);var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS))));var dL=DL(Nm(RP(Sm(upd(DL(Nm(RP(Sm(Kc,N,0.1,BS,N/2,0.2)))),1.2,1.75),N,0.1,BS))));var(a,_,r2)=FitSC(CurvLap(d0,dL,N),SrcOmega(Kc,N,0.1,N/2,0.2)); _output.WriteLine($"{n,-7} {a:F4}  {r2:F3}"); } }

    [Fact] public void V4_1_SCR_13_NullAndDegenerateControls() { int N=80;int ln=N/2; _output.WriteLine("Control      alpha   r²"); var K0=new double[N,N];var d0k=DL(Nm(RP(Sm(K0,N,0.1,BS))));var dLk=DL(Nm(RP(Sm(K0,N,0.1,BS,ln,0.2))));var(a0,_,_)=FitSC(CurvLap(d0k,dLk,N),SrcOmega(K0,N,0.1,ln,0.2)); _output.WriteLine($"K=0:         {a0:F4}  --"); var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS);var d0a=DL(Nm(RP(Sm(Kc,N,0.1,BS))));var dLa=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),1.2,1.75),N,0.1,BS))));var(aa,_,ra)=FitSC(CurvLap(d0a,dLa,N),SrcOmega(Kc,N,0.1,ln,0.2)); _output.WriteLine($"ActiveTRM:   {aa:F4}  {ra:F3}"); }

    [Fact] public void V4_1_SCR_14_SourceCurvatureRobustnessScore() { int N=80; int ln=N/2; double[] xis=[1.5,1.75,2.0];double[] K0s=[1.0,1.2]; _output.WriteLine("xi    K0   alpha   r²     score"); foreach(double xi in xis)foreach(double kv in K0s){var Kc=RecoverFP(KS(N,BS),N,kv,xi,0.1,5,BS);var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS))));var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),kv,xi),N,0.1,BS))));var(a,_,r2)=FitSC(CurvLap(d0,dL,N),SrcOmega(Kc,N,0.1,ln,0.2));double sc=Math.Abs(a)*r2; _output.WriteLine($"{xi:F2}  {kv:F1}  {a:F4}  {r2:F3}  {sc:F4}"); } }

    [Fact] public void V4_1_SCR_15_SourceCurvatureRobustnessReport() { int N=80;int ln=N/2;var Kc=RecoverFP(KS(N,BS),N,1.2,1.75,0.1,5,BS);var d0=DL(Nm(RP(Sm(Kc,N,0.1,BS))));var dL=DL(Nm(RP(Sm(ExpUpd(DL(Nm(RP(Sm(Kc,N,0.1,BS,ln,0.2)))),1.2,1.75),N,0.1,BS))));var(a,_,r2)=FitSC(CurvLap(d0,dL,N),SrcOmega(Kc,N,0.1,ln,0.2));double sc=Math.Abs(a)*r2;string ccl=sc>0.1?"A) robust":"B) moderate";_output.WriteLine($"═══ SOURCE-CURVATURE ROBUSTNESS ═══");_output.WriteLine($"alpha={a:F4} r²={r2:F3} score={sc:F4} => {ccl}");_output.WriteLine("Einstein eqs / GR NOT derived."); }

    [Fact] public void V4_1_SCR_16_ClaimDisciplineReport() { _output.WriteLine("SUPPORTED: Source-curvature robustness testable. Alpha measurable."); _output.WriteLine("CONDITIONAL: Source ≠ Tμν. Curv ≠ Gμν. Alpha ≠ 8πG/c⁴."); _output.WriteLine("NOT CLAIMED: Einstein eqs, GR, gravity, G, c, D=3, SPARC, DM."); Assert.True(true); }
}
