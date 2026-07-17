using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBJ"),Trait("Category","LongRunning")]
public class V5_12_RelativeCompressionControlAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EV{public double DD,DK,DKs;}
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,om0;}
    struct Trial{public int seed;public double frac,d0,dT1,dT2,kT1,kT2,omT1,omT2;public bool immHi,persist;}

    public V5_12_RelativeCompressionControlAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBJ_01_DenseFractionSweep(){
        _o.WriteLine("═══ HBJ: Dense relative compression sweep 5-70% at N=71 ═══");
        int n=71,nSeeds=50;
        var hi=PCent(n,true);var lo=PCent(n,false);
        _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4}  Lo: dm={lo.dm:F4} km={lo.km:F4}");

        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<nSeeds;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Baseline profiling
        var bases=new ConcurrentBag<SBase>();
        Parallel.ForEach(seeds,s=>{bases.Add(ProfileB(n,s));});
        var bDict=bases.ToDictionary(b=>b.seed);

        // Seed groups
        var g1=bases.Where(b=>b.d0>0.50).Select(b=>b.seed).ToList(); // high-room
        var g2=bases.Where(b=>b.d0<0.40&&b.km0>0.98).Select(b=>b.seed).ToList(); // crypto-Hi
        var g3=bases.Where(b=>b.d0<=0.40&&b.km0<=0.98).Select(b=>b.seed).ToList(); // low-room
        _o.WriteLine($"\n─── Seed Groups ───");
        _o.WriteLine($"G1 High-Room (d0>0.50): {g1.Count} seeds [{string.Join(",",g1.Take(10))}{((g1.Count>10)?"...":"")}]");
        _o.WriteLine($"G2 Crypto-Hi (d0<0.40, km0>0.98): {g2.Count} seeds [{string.Join(",",g2)}]");
        _o.WriteLine($"G3 Low-Room (d0<=0.40, km0<=0.98): {g3.Count} seeds");

        // Dense fraction sweep: 5-70%
        double[] fracs={0.05,0.10,0.20,0.30,0.40,0.50,0.60,0.70};
        var trials=new ConcurrentBag<Trial>();
        foreach(var f in fracs)
            Parallel.ForEach(seeds,s=>{var b=bDict[s];trials.Add(RunTrial(n,s,b.d0*(1.0-f),b,hi,lo));});

        var allT=trials.ToArray();

        // ── Fraction sweep table ──
        _o.WriteLine($"\n─── Compression Fraction Sweep ───");
        _o.WriteLine($"{"Frac",6} {"N",4} {"ImmHi",5} {"Persist",7} {"Strict",6} {"dT1",8} {"ΔdRel",8} {"dReb",8} {"kCol",8}");
        foreach(var f in fracs){
            var tr=allT.Where(t=>Math.Abs(t.frac-f)<0.001).ToArray();
            int imm=tr.Count(t=>t.immHi),per=tr.Count(t=>t.persist),stp=tr.Count(t=>t.immHi&&t.persist);
            double dT1=tr.Average(t=>t.dT1),dRel=tr.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10));
            double dReb=tr.Average(t=>t.dT2-t.dT1),kCol=tr.Average(t=>t.kT2-t.kT1);
            _o.WriteLine($"{f*100,5:F0}% {tr.Length,4} {imm,5} {per,7} {stp,6} {dT1,8:F4} {dRel,8:F4} {dReb,8:F4} {kCol,8:F4}");
        }

        // ── Group analysis at best fractions (50%, 60%) ──
        _o.WriteLine($"\n─── Group Analysis at 50% and 60% ───");
        foreach(var f in new[]{0.50,0.60}){
            _o.WriteLine($"\n{f*100:F0}% compression:");
            foreach(var g in new[]{ (seeds:g1, label:"G1 HighRm"), (seeds:g2, label:"G2 CryptHi"), (seeds:g3, label:"G3 LowRm") }){
                var tr=allT.Where(t=>g.seeds.Contains(t.seed)&&Math.Abs(t.frac-f)<0.001).ToArray();
                int stp=tr.Count(t=>t.immHi&&t.persist);
                _o.WriteLine($"  {g.label,-12}: N={tr.Length,2} ImmHi={tr.Count(t=>t.immHi),2} Persist={tr.Count(t=>t.persist),2} Strict={stp,2}");
            }
        }

        // ── ΔdRel threshold analysis ──
        _o.WriteLine($"\n─── ΔdRel Threshold Analysis ───");
        foreach(var thr in new[]{-0.30,-0.40,-0.50,-0.60}){
            var above=allT.Where(t=>t.immHi&&(t.dT1-t.d0)/Math.Max(t.d0,1e-10)<=thr).ToArray();
            var below=allT.Where(t=>t.immHi&&(t.dT1-t.d0)/Math.Max(t.d0,1e-10)>thr).ToArray();
            _o.WriteLine($"ΔdRel ≤ {thr:F2}: N={above.Length,3} strict={above.Count(t=>t.persist),2} rate={((double)above.Count(t=>t.persist)/Math.Max(above.Length,1))*100:F0}%");
            _o.WriteLine($"ΔdRel > {thr:F2}: N={below.Length,3} strict={below.Count(t=>t.persist),2} rate={((double)below.Count(t=>t.persist)/Math.Max(below.Length,1))*100:F0}%");
        }

        // ── Persistence by ΔdRel bucket ──
        var immHiAll=allT.Where(t=>t.immHi).ToArray();
        _o.WriteLine($"\n─── Strict Rate by ΔdRel Bucket ───");
        double[] edges={-1.0,-0.60,-0.50,-0.40,-0.30,-0.20,0.0};
        for(int i=0;i<edges.Length-1;i++){
            var bucket=immHiAll.Where(t=>{double r=(t.dT1-t.d0)/Math.Max(t.d0,1e-10);return r>edges[i]&&r<=edges[i+1];}).ToArray();
            if(bucket.Length>0)
                _o.WriteLine($"[{edges[i],5:F2},{edges[i+1],5:F2}]: N={bucket.Length,3} strict={bucket.Count(t=>t.persist),2}");
        }

        // ── Seed 57 anomaly ──
        _o.WriteLine($"\n─── Seed 57 Anomaly Audit ──");
        var s57=allT.Where(t=>t.seed==57).OrderBy(t=>t.frac).ToArray();
        foreach(var t in s57)
            _o.WriteLine($"  {t.frac*100,3:F0}%: dT1={t.dT1:F4} dT2={t.dT2:F4} ΔdRel={(t.dT1-t.d0)/Math.Max(t.d0,1e-10):F4} OmT1={t.omT1:F2} OmT2={t.omT2:F2} imm={t.immHi} pers={t.persist}");

        // ── Seed 39 audit ──
        _o.WriteLine($"\n─── Seed 39 Audit ──");
        var s39=allT.Where(t=>t.seed==39).OrderBy(t=>t.frac).ToArray();
        foreach(var t in s39)
            _o.WriteLine($"  {t.frac*100,3:F0}%: dT1={t.dT1:F4} dT2={t.dT2:F4} ΔdRel={(t.dT1-t.d0)/Math.Max(t.d0,1e-10):F4} OmT1={t.omT1:F2} OmT2={t.omT2:F2} imm={t.immHi} pers={t.persist}");
    }

    [Fact]public void HBJ_02_CrossNValidation(){
        _o.WriteLine("═══ HBJ: Cross-N relative compression at 50-60% ═══");
        foreach(var n in new[]{67,71,72}){
            _o.WriteLine($"\n─── N={n} ───");
            var hi=PCent(n,true);var lo=PCent(n,false);
            var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<20;s++)if(!IsHi(n,s))loSeeds.Add(s);
            var seeds=loSeeds.ToArray();

            var bases=new ConcurrentBag<SBase>();
            Parallel.ForEach(seeds,s=>{bases.Add(ProfileB(n,s));});
            var bDict=bases.ToDictionary(b=>b.seed);

            foreach(var f in new[]{0.50,0.60}){
                var trials=new ConcurrentBag<Trial>();
                Parallel.ForEach(seeds,s=>{var b=bDict[s];trials.Add(RunTrial(n,s,b.d0*(1.0-f),b,hi,lo));});
                var tr=trials.ToArray();
                int imm=tr.Count(t=>t.immHi),pers=tr.Count(t=>t.persist),stp=tr.Count(t=>t.immHi&&t.persist);
                double dRel=tr.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10));
                _o.WriteLine($"  {f*100:F0}%: ImmHi={imm,2} Persist={pers,2} Strict={stp,2} ΔdRel={dRel:F4}");
            }
        }
    }

    [Fact]public void HBJ_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (relative compression control): HBJ_01 sweep");
        _o.WriteLine("Gate B (compression threshold): HBJ_01 ΔdRel threshold");
        _o.WriteLine("Gate C (high-room seeds persist): HBJ_01 group analysis");
        _o.WriteLine("Gate F (anomaly second pathway): HBJ_01 seed 57");
        _o.WriteLine("Gate H (N-dependent): HBJ_02 cross-N");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("N=71 primary, N=67/72 validation.");
        _o.WriteLine("Relative compression = dT1 = d0*(1-frac). No physical claims.");
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
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}

    static SBase ProfileB(int n,int seed){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);double om3=Of(h3E,n).Average();
        var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        return new SBase{seed=seed,d0=Dm(d3E,n),km0=Km(K3E,n),ks0=Ks(K3E,n),om0=om3};
    }

    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);

        var hT1=Sim(K,n,S,seed+100);double om1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);

        var hT2=Sim(kT1,n,S,seed+200);double om2=Of(hT2,n).Average();
        var dT2=DL(Nm(RP(hT2,n),n),n);var kT2=Cupd(dT2,n);

        return new Trial{seed=seed,frac=(b.d0-target)/Math.Max(b.d0,1e-10),d0=b.d0,
            dT1=Dm(dT1,n),dT2=Dm(dT2,n),kT1=Km(kT1,n),kT2=Km(kT2,n),omT1=om1,omT2=om2,
            immHi=om1>THR,persist=om2>THR};
    }
}
