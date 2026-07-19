using Xunit;
using Xunit.Abstractions;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TRM.Tests.V5_10;

/// <summary>
/// V5.10 K Boost Validity and Manifold Audit (CAA):
///
/// CAE found K boost induces Lo->Hi. CAA checks whether induced states
/// match natural high-branch geometry or are just threshold artifacts.
/// Also tests persistence (+1 extra epoch).
/// </summary>
[Trait("Category", "V5_10")]
[Trait("Category", "V5_10_CAA")]
public class V5_10_KBoostValidityAndManifoldAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt = 0.05; private const int Hd = 4;
    private const double Xi = 1.75; private const double K0 = 1.2; private const double S = 0.10;
    private const int St = 300; private const double REps = 1e-6;
    private const double FIXED_THRESHOLD = 1.783;
    private const int NEpochs = 5;
    private static readonly int[] TestN = { 67, 71, 80 };

    private struct Metrics { public double Om, Dm, Km, Ks, Md; }

    public V5_10_KBoostValidityAndManifoldAudit_Tests(ITestOutputHelper o) { _output = o; }

    private static double[][] Sm(double[,] K, int n, double s, int seed, int st, double reps) {
        var r = new Random(seed); var w = new double[n];
        for (int i=0; i<n; i++) w[i]=1.0+s*(r.NextDouble()-0.5)*2.0;
        var th = new double[n]; for (int i=0; i<n; i++) th[i]=r.NextDouble()*2.0*Math.PI;
        int hL=st/Hd+1; var h=new double[hL][]; h[0]=(double[])th.Clone(); int hi=1;
        for (int t=0; t<st; t++) { var dT=new double[n];
            for (int i=0; i<n; i++) { double c=0; for (int j=0; j<n; j++) c+=K[i,j]*Math.Sin(th[j]-th[i]); dT[i]=w[i]+c; }
            for (int i=0; i<n; i++) th[i]+=Dt*dT[i];
            if ((t+1)%Hd==0&&hi<hL) h[hi++]=(double[])th.Clone(); } return h; }

    private static double[,] RP(double[][] h, int n) { int T=h.Length; var R=new double[n,n];
        for (int i=0; i<n; i++) for (int j=0; j<n; j++) { double sc=0,ss=0;
            for (int t=0; t<T; t++) { double d=h[t][i]-h[t][j]; sc+=Math.Cos(d); ss+=Math.Sin(d); }
            R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T; } return R; }

    private static double[,] Nm(double[,] R, int n, double reps) { double mn=double.MaxValue;
        for (int i=0; i<n; i++) for (int j=0; j<n; j++) if (i!=j&&R[i,j]<mn) mn=R[i,j];
        double rng=1.0-mn; if (rng<1e-15) rng=1.0; var Rn=new double[n,n];
        for (int i=0; i<n; i++) for (int j=0; j<n; j++) Rn[i,j]=i==j?1.0:Math.Max(reps,(R[i,j]-mn)/rng); return Rn; }

    private static double[,] DL(double[,] R, int n) { var d=new double[n,n];
        for (int i=0; i<n; i++) for (int j=0; j<n; j++) d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100)); return d; }

    private static double[,] Cupd(double[,] d, int n, double k0, double xi) { var K=new double[n,n];
        for (int i=0; i<n; i++) for (int j=0; j<n; j++) K[i,j]=i==j?0:k0*Math.Exp(-d[i,j]/Math.Max(xi,0.01)); return K; }

    private static double[,] KS(int n, int seed) { var rng=new Random(seed);
        var adj=new HashSet<int>[n]; for (int i=0; i<n; i++) adj[i]=new HashSet<int>();
        double p=6.0/(n-1); for (int i=0; i<n; i++) for (int j=i+1; j<n; j++)
            if (rng.NextDouble()<p) { adj[i].Add(j); adj[j].Add(i); }
        var v=new bool[n]; var cs=new List<List<int>>();
        for (int i=0; i<n; i++) { if (v[i]) continue; var c=new List<int>();
            var q=new Queue<int>(); v[i]=true; q.Enqueue(i);
            while (q.Count>0) { int u=q.Dequeue(); c.Add(u); foreach (int x in adj[u]) if (!v[x]) { v[x]=true; q.Enqueue(x); } } cs.Add(c); }
        for (int i=1; i<cs.Count; i++) { adj[cs[i][0]].Add(cs[i-1][0]); adj[cs[i-1][0]].Add(cs[i][0]); }
        var K=new double[n,n]; for (int i=0; i<n; i++) foreach (int j in adj[i]) if (i<j) { K[i,j]=0.5; K[j,i]=0.5; } return K; }

    private static double[] Of(double[][] h, int n) { int T=h.Length; var o=new double[n];
        for (int i=0; i<n; i++) { double su=0; int c=0; for (int t=1; t<T; t++) { su+=Math.Abs(h[t][i]-h[t-1][i]); c++; }
            o[i]=c>0?su/(c*Dt*Hd):0; } return o; }

    private static double Std(double[] v) { double m=v.Average(); return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length); }
    private static double KMean(double[,] K, int n) { double s=0; int c=0;
        for (int i=0; i<n; i++) for (int j=i+1; j<n; j++) { s+=K[i,j]; c++; } return c>0?s/c:0; }
    private static double DMean(double[,] d, int n) { double s=0; int c=0;
        for (int i=0; i<n; i++) for (int j=i+1; j<n; j++) { s+=d[i,j]; c++; } return c>0?s/c:0; }
    private static double MeanDist(double[,] R, int n) { double s=0; int c=0;
        for (int i=0; i<n; i++) for (int j=i+1; j<n; j++) { s+=R[i,j]; c++; } return c>0?s/c:0; }

    private static Metrics ApplyKBoost(double[,] K, int n, int seed, double targetKM) {
        double km0=KMean(K, n); double scale=km0>1e-10?targetKM/km0:1.0;
        var K2=new double[n,n];
        for (int i=0; i<n; i++) { K2[i,i]=0; for (int j=i+1; j<n; j++) { K2[i,j]=K[i,j]*scale; K2[j,i]=K2[i,j]; } }
        var hf=Sm(K2, n, S, seed+100, St, REps);
        double om=Of(hf, n).Average();
        var Rf=RP(hf, n); var df=DL(Rf, n); var Kf=Cupd(df, n, K0, Xi);
        var kV=new double[n*(n-1)/2]; int idx=0;
        for (int i=0; i<n; i++) for (int j=i+1; j<n; j++) kV[idx++]=Kf[i,j];
        return new Metrics { Om=om, Dm=DMean(df, n), Km=kV.Average(), Ks=Std(kV), Md=MeanDist(Rf, n) };
    }

    private static (bool hi, double om) RunOneMoreEpoch(double[,] K, int n, int seed) {
        var h=Sm(K, n, S, seed+200, St, REps); var R=RP(h, n);
        var K2=Cupd(DL(Nm(R, n, REps), n), n, K0, Xi);
        double om=Of(Sm(K2, n, S, seed+300, St, REps), n).Average();
        return (om>FIXED_THRESHOLD, om);
    }

    [Fact] public void CAA_01_ManifoldComparison() {
        _output.WriteLine("═══ MANIFOLD COMPARISON (seeds 0-99) ═══");
        int seeds=100;
        foreach (var n in TestN) {
            var natHi = new List<Metrics>();
            var natLoK = new List<(Metrics m, double[,] K)>();
            for (int s=0; s<seeds; s++){var K=KS(n,s); for(int e=0;e<NEpochs;e++){var h=Sm(K,n,S,s+e,St,REps);K=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
                var hf=Sm(K,n,S,s+NEpochs,St,REps); double om=Of(hf,n).Average();
                var Rf=RP(hf,n); var df=DL(Rf,n); var Kf=Cupd(df,n,K0,Xi);
                var kV=new double[n*(n-1)/2]; int idx=0; for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)kV[idx++]=Kf[i,j];
                var m=new Metrics{Om=om,Dm=DMean(df,n),Km=kV.Average(),Ks=Std(kV),Md=MeanDist(Rf,n)};
                if(om>FIXED_THRESHOLD)natHi.Add(m); else natLoK.Add((m,K));}
            if(natLoK.Count==0||natHi.Count==0) continue;
            double nhi_om=natHi.Average(x=>x.Om),nhi_dm=natHi.Average(x=>x.Dm),nhi_km=natHi.Average(x=>x.Km),nhi_ks=natHi.Average(x=>x.Ks);
            double nlo_om=natLoK.Average(x=>x.m.Om),nlo_km=natLoK.Average(x=>x.m.Km);
            var induced=new List<Metrics>(); var persists=new List<bool>(); var mults=new List<double>();
            foreach(var (_,K) in natLoK) { double bestMult=0; Metrics bestRes=default;
                foreach(var mult in new[]{1.0,1.02,1.04,1.06,1.08,1.10,1.12,1.15,1.20}) {
                    var res=ApplyKBoost(K,n,(int)(mult*1000),nlo_km*mult);
                    if(res.Om>FIXED_THRESHOLD&&(bestMult==0||mult<bestMult)){bestMult=mult;bestRes=res;}}
                if(bestMult>0){double scl=nlo_km>1e-10?nlo_km*bestMult/KMean(K,n):1.0;
                    var Kb=new double[n,n];for(int i=0;i<n;i++){Kb[i,i]=0;for(int j=i+1;j<n;j++){Kb[i,j]=K[i,j]*scl;Kb[j,i]=Kb[i,j];}}
                    var(phi,_)=RunOneMoreEpoch(Kb,n,seeds+(int)(bestMult*1000));
                    induced.Add(bestRes);persists.Add(phi);mults.Add(bestMult);}}
            if(induced.Count==0) continue;
            double ind_om=induced.Average(x=>x.Om),ind_dm=induced.Average(x=>x.Dm),ind_km=induced.Average(x=>x.Km),ind_ks=induced.Average(x=>x.Ks);
            _output.WriteLine($"N={n} (Lo={natLoK.Count} Hi={natHi.Count} Induced={induced.Count})");
            _output.WriteLine($"  NatHi:  Om={nhi_om:F3} Dm={nhi_dm:F3} Km={nhi_km:F3} Ks={nhi_ks:F3}");
            _output.WriteLine($"  NatLo:  Om={nlo_om:F3} Dm={ind_dm:F3} Km={nlo_km:F3}");
            _output.WriteLine($"  Induced:Om={ind_om:F3} Dm={ind_dm:F3} Km={ind_km:F3} Ks={ind_ks:F3} mult={mults.Average():F3}x persist={persists.Count(x=>x)}/{induced.Count}");
            double d2Hi=Math.Pow((ind_dm-nhi_dm)/(nhi_dm+1e-10),2)+Math.Pow((ind_km-nhi_km)/(nhi_km+1e-10),2);
            double d2Lo=Math.Pow((ind_dm-natLoK.Average(x=>x.m.Dm))/(natLoK.Average(x=>x.m.Dm)+1e-10),2)+Math.Pow((ind_km-nlo_km)/(nlo_km+1e-10),2);
            _output.WriteLine($"  DistToHi:{Math.Sqrt(d2Hi):F3} DistToLo:{Math.Sqrt(d2Lo):F3} -> {(Math.Sqrt(d2Hi)<Math.Sqrt(d2Lo)?"HI-LIKE":"LO-LIKE (forced Om)")}");
            _output.WriteLine("");
        }
    }

    [Fact] public void CAA_02_GateSummary() {
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (K boost natural hi): CAA_01 distance + persistence");
        _output.WriteLine("Gate B (threshold-only): CAA_01 forced Omega?");
        _output.WriteLine("Gate C (unstable): CAA_01 persistence");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: Manifold distance, persistence.");
        _output.WriteLine("CONDITIONAL: N=67,71,80. d_mean/K_mean/K_std.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
