using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBH"),Trait("Category","LongRunning")]
public class V5_12_CompressionRoomAndRelativeDisplacementAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EV{public double DD,DK,DKs;}
    struct P3{public double dm,km,ks;}
    struct SeedBase{public int seed;public double d0,km0,ks0,om0,dh0,dl0,es0,natDD;}
    struct Trial{public int seed;public double frac,d0,dT1,dT2,kT1,kT2,omT1,omT2,esT1;public bool immHi,persist;}

    public V5_12_CompressionRoomAndRelativeDisplacementAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBH_01_RelativeCompressionSweep(){
        _o.WriteLine("═══ HBH: Relative compression sweep at N=71 ═══");
        int n=71,nSeeds=50;
        var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
        _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4}  Lo: dm={lo.dm:F4} km={lo.km:F4}");

        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<nSeeds;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Baseline profiling
        var bases=new ConcurrentBag<SeedBase>();
        Parallel.ForEach(seeds,s=>{bases.Add(ProfileBase(n,s,hi,lo,ev));});
        var baseDict=bases.ToDictionary(b=>b.seed);
        _o.WriteLine($"\n─── Baseline Summary ({nSeeds} seeds) ───");
        _o.WriteLine($"d0: mean={bases.Average(b=>b.d0):F4} range=[{bases.Min(b=>b.d0):F3},{bases.Max(b=>b.d0):F3}]");
        _o.WriteLine($"km0: mean={bases.Average(b=>b.km0):F4}");

        // ── Relative compression sweep: 10%-50% ──
        double[] fracs={0.10,0.20,0.30,0.40,0.50};
        var trials=new ConcurrentBag<Trial>();
        foreach(var f in fracs){
            Parallel.ForEach(seeds,s=>{
                var b=baseDict[s];
                double tgt=b.d0*(1.0-f);
                var t=RunTrial(n,s,tgt,b,hi,lo,ev);
                trials.Add(t);
            });
        }

        // ── Group by fraction ──
        _o.WriteLine($"\n─── Results by Compression Fraction ───");
        _o.WriteLine($"{"Frac",6} {"N",4} {"ImmHi",5} {"Persist",7} {"Strict",6} {"d0",8} {"dT1",8} {"dT2",8} {"ΔdAbs",8} {"ΔdRel",8} {"dReb",8}");
        foreach(var f in fracs){
            var tr=trials.Where(t=>Math.Abs(t.frac-f)<0.001).ToArray();
            int imm=tr.Count(t=>t.immHi),per=tr.Count(t=>t.persist),stp=tr.Count(t=>t.immHi&&t.persist);
            double d0=tr.Average(t=>t.d0),dT1=tr.Average(t=>t.dT1),dT2=tr.Average(t=>t.dT2);
            double dAbs=tr.Average(t=>t.dT1-t.d0),dRel=tr.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10));
            double dReb=tr.Average(t=>t.dT2-t.dT1);
            _o.WriteLine($"{f*100,5:F0}% {tr.Length,4} {imm,5} {per,7} {stp,6} {d0,8:F4} {dT1,8:F4} {dT2,8:F4} {dAbs,8:F4} {dRel,8:F4} {dReb,8:F4}");
        }

        // ── Persistence predictor: pooled across fractions ──
        var allTr=trials.ToArray();
        var strict=allTr.Where(t=>t.immHi&&t.persist).ToArray();
        var hiProxNP=allTr.Where(t=>!t.immHi||!t.persist).ToArray(); // non-strict
        var immHiOnly=allTr.Where(t=>t.immHi&&!t.persist).ToArray();

        _o.WriteLine($"\n─── Persistence Predictor: Strict ({strict.Length}) vs ImmHi-NonPersist ({immHiOnly.Length}) ───");
        if(strict.Length>0&&immHiOnly.Length>0){
            double sDRel=strict.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10));
            double nDRel=immHiOnly.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10));
            double sDAbs=strict.Average(t=>t.dT1-t.d0);
            double nDAbs=immHiOnly.Average(t=>t.dT1-t.d0);
            double sD0=strict.Average(t=>t.d0),nD0=immHiOnly.Average(t=>t.d0);
            double sDT1=strict.Average(t=>t.dT1),nDT1=immHiOnly.Average(t=>t.dT1);
            double sDReb=strict.Average(t=>t.dT2-t.dT1),nDReb=immHiOnly.Average(t=>t.dT2-t.dT1);
            _o.WriteLine($"  d0:           {sD0,8:F4} vs {nD0,8:F4}  diff={sD0-nD0:+0.0000;-0.0000}");
            _o.WriteLine($"  dT1:          {sDT1,8:F4} vs {nDT1,8:F4}  diff={sDT1-nDT1:+0.0000;-0.0000}");
            _o.WriteLine($"  Δd_abs:       {sDAbs,8:F4} vs {nDAbs,8:F4}  diff={sDAbs-nDAbs:+0.0000;-0.0000}");
            _o.WriteLine($"  Δd_rel:       {sDRel,8:F4} vs {nDRel,8:F4}  diff={sDRel-nDRel:+0.0000;-0.0000}");
            _o.WriteLine($"  dRebound:     {sDReb,8:F4} vs {nDReb,8:F4}  diff={sDReb-nDReb:+0.0000;-0.0000}");
        }

        // ── Seed 36 vs 39 forensic ──
        _o.WriteLine($"\n─── Seed 36 vs Seed 39 ───");
        foreach(var sd in new[]{36,39}){
            if(!baseDict.ContainsKey(sd))continue;
            var b=baseDict[sd];
            _o.WriteLine($"\nSeed {sd}: d0={b.d0:F4} km0={b.km0:F4} ks0={b.ks0:F4} Om0={b.om0:F4} Es0={b.es0:F4} natDD={b.natDD:F4}");
            foreach(var f in fracs){
                var tr=allTr.Where(t=>t.seed==sd&&Math.Abs(t.frac-f)<0.001).ToArray();
                if(tr.Length==0)continue;
                var t=tr[0];
                double dRel=(t.dT1-t.d0)/Math.Max(t.d0,1e-10);
                _o.WriteLine($"  {f*100,3:F0}%: dT1={t.dT1:F4} dT2={t.dT2:F4} ΔdRel={dRel:F4} OmT1={t.omT1:F2} OmT2={t.omT2:F2} imm={t.immHi} pers={t.persist}");
            }
        }

        // ── Compression-room hypothesis ──
        _o.WriteLine($"\n─── Compression-Room Hypothesis ───");
        var bySeed=allTr.GroupBy(t=>t.seed);
        int seedsWithStrict=bySeed.Count(g=>g.Any(t=>t.immHi&&t.persist));
        _o.WriteLine($"Seeds achieving strict persistence at any fraction: {seedsWithStrict}/{nSeeds}");
        // Seed-level comparison
        var seedMaxRel=bySeed.Where(g=>g.Any(t=>t.immHi&&t.persist)).Select(g=>{
            var s=g.First(t=>t.immHi&&t.persist);
            return new{seed=g.Key,d0=s.d0,dRel=(s.dT1-s.d0)/Math.Max(s.d0,1e-10),frac=s.frac};
        }).ToArray();
        if(seedMaxRel.Length>0){
            _o.WriteLine($"Persistent seeds and their best compression:");
            foreach(var s in seedMaxRel.OrderBy(s=>s.seed))
                _o.WriteLine($"  Seed {s.seed,3}: d0={s.d0:F4} best frac={s.frac*100:F0}% ΔdRel={s.dRel:F4}");
        }
    }

    [Fact]public void HBH_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (relative displacement predicts): HBH_01 Δd_rel comparison");
        _o.WriteLine("Gate B (extreme-Lo baseline predicts): HBH_01 d0 comparison");
        _o.WriteLine("Gate C (crypto-Hi fails from small displacement): HBH_01 seed 39 profile");
        _o.WriteLine("Gate D (relative intervention improves): HBH_01 strict count vs HBF");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("N=71 primary. Relative compression = dT1 = d0*(1-frac).");
        _o.WriteLine("No physical claims. No cross-N (HBH_01 N=71 only).");
    }

    // ── Engine ──
    static double[][]Sim(double[,]K,int n,double s,int seed){
        var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;
        var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;
        int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();
        int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;
    }
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}

    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}

    static P3 PCent(int n,bool hi){
        double d=0,k=0,ks=0;int c=0;
        for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<NE;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}
        return new P3{dm=d/c,km=k/c,ks=ks/c};
    }
    static EV GetEV(int n){var hi=PCent(n,true);var lo=PCent(n,false);return new EV{DD=hi.dm-lo.dm,DK=hi.km-lo.km,DKs=hi.ks-lo.ks};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}

    static SeedBase ProfileBase(int n,int seed,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);double om3=Of(h3E,n).Average();
        var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double dm3=Dm(d3E,n),km3=Km(K3E,n),ks3=Ks(K3E,n);

        var hNat=Sim(K3,n,S,seed+200);var dNat=DL(Nm(RP(hNat,n),n),n);var KNat=Cupd(dNat,n);
        double dmNat=Dm(dNat,n);

        return new SeedBase{seed=seed,d0=dm3,km0=km3,ks0=ks3,om0=om3,
            dh0=Dist(dm3,km3,ks3,hi),dl0=Dist(dm3,km3,ks3,lo),
            es0=ES(dm3,km3,ks3,hi,lo),natDD=dmNat-dm3};
    }

    static Trial RunTrial(int n,int seed,double target,SeedBase b,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);

        var hT1=Sim(K,n,S,seed+100);double omT1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);
        double dt1=Dm(dT1,n),kt1=Km(kT1,n);

        var hT2=Sim(kT1,n,S,seed+200);double omT2=Of(hT2,n).Average();
        var dT2=DL(Nm(RP(hT2,n),n),n);var kT2=Cupd(dT2,n);
        double dt2=Dm(dT2,n),kt2=Km(kT2,n);

        return new Trial{seed=seed,frac=(b.d0-target)/Math.Max(b.d0,1e-10),d0=b.d0,
            dT1=dt1,dT2=dt2,kT1=kt1,kT2=kt2,omT1=omT1,omT2=omT2,
            esT1=ES(dt1,kt1,Ks(kT1,n),hi,lo),immHi=omT1>THR,persist=omT2>THR};
    }

    static double ES(double dm,double km,double ks,P3 hi,P3 lo)=>Dist(dm,km,ks,lo)-Dist(dm,km,ks,hi);
    static double Dist(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
