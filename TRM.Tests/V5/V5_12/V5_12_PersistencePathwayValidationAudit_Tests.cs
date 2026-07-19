using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBK"),Trait("Category","LongRunning")]
public class V5_12_PersistencePathwayValidationAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,om0,dh0;}
    struct Trial{public int seed;public string proto;public double frac,d0,dT1,dT2,kT1,omT1,omT2;public bool immHi,persist;}

    public V5_12_PersistencePathwayValidationAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBK_01_PathwayValidation(){
        _o.WriteLine("═══ HBK: Persistence pathway validation at N=71 ═══");
        int n=71,nSeeds=50;
        var hi=PCent(n,true);var lo=PCent(n,false);
        _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4} ks={hi.ks:F4}");

        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<nSeeds;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Baseline + classify
        var bases=new ConcurrentBag<SBase>();
        Parallel.ForEach(seeds,s=>{bases.Add(ProfileB(n,s,hi));});
        var bDict=bases.ToDictionary(b=>b.seed);
        var g1=bases.Where(b=>b.d0>0.50).Select(b=>b.seed).ToList();
        var g2=bases.Where(b=>b.d0>0.65).Select(b=>b.seed).ToList();
        var g3=bases.Where(b=>b.d0<0.40&&b.km0>0.98).Select(b=>b.seed).ToList();
        _o.WriteLine($"\nG1 High-Room (d0>0.50): {g1.Count} seeds");
        _o.WriteLine($"G2 Extreme-Room (d0>0.65): {g2.Count} seeds");
        _o.WriteLine($"G3 Crypto-Hi (d0<0.40,km>0.98): {g3.Count} seeds [{string.Join(",",g3)}]");

        // ── Pathway 1: strong comp on G1+G2 ──
        var trials=new ConcurrentBag<Trial>();
        double[] strongFracs={0.30,0.40,0.50,0.60};
        double[] mildFracs={0.05,0.10,0.15,0.20};

        // P1: strong comp on high-room
        foreach(var f in strongFracs)
            Parallel.ForEach(g1.ToArray(),s=>{var b=bDict[s];trials.Add(RunTrial(n,s,b.d0*(1.0-f),b,hi,lo,"P1_Strong"));});

        // P2: mild comp on crypto-Hi
        foreach(var f in mildFracs)
            Parallel.ForEach(g3.ToArray(),s=>{var b=bDict[s];trials.Add(RunTrial(n,s,b.d0*(1.0-f),b,hi,lo,"P2_Mild"));});

        // Wrong protocol: strong on crypto-Hi
        foreach(var f in new[]{0.40,0.50})
            Parallel.ForEach(g3.ToArray(),s=>{var b=bDict[s];trials.Add(RunTrial(n,s,b.d0*(1.0-f),b,hi,lo,"WRONG_Strong"));});

        // Wrong protocol: mild on high-room
        foreach(var f in new[]{0.10,0.15})
            Parallel.ForEach(g1.ToArray(),s=>{var b=bDict[s];trials.Add(RunTrial(n,s,b.d0*(1.0-f),b,hi,lo,"WRONG_Mild"));});

        var allT=trials.ToArray();

        // ── Results by protocol ──
        _o.WriteLine($"\n─── Pathway Validation Results ───");
        _o.WriteLine($"{"Protocol",-14} {"N",5} {"ImmHi",5} {"Persist",7} {"Strict",6} {"ΔdRel",8} {"dReb",8}");
        foreach(var proto in new[]{"P1_Strong","P2_Mild","WRONG_Strong","WRONG_Mild"}){
            var tr=allT.Where(t=>t.proto==proto).ToArray();
            if(tr.Length==0)continue;
            int imm=tr.Count(t=>t.immHi),per=tr.Count(t=>t.persist),stp=tr.Count(t=>t.immHi&&t.persist);
            double dRel=tr.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10));
            double dReb=tr.Average(t=>t.dT2-t.dT1);
            _o.WriteLine($"{proto,-14} {tr.Length,5} {imm,5} {per,7} {stp,6} {dRel,8:F4} {dReb,8:F4}");
        }

        // ── P1 detail by fraction ──
        _o.WriteLine($"\n─── P1 Strong Comp Detail ───");
        foreach(var f in strongFracs){
            var tr=allT.Where(t=>t.proto=="P1_Strong"&&Math.Abs(t.frac-f)<0.001).ToArray();
            int stp=tr.Count(t=>t.immHi&&t.persist);
            _o.WriteLine($"  {f*100,3:F0}%: N={tr.Length,2} ImmHi={tr.Count(t=>t.immHi),2} Strict={stp,2} ΔdRel={tr.Average(t=>(t.dT1-t.d0)/Math.Max(t.d0,1e-10)):F4}");
        }

        // ── P2 detail by fraction ──
        _o.WriteLine($"\n─── P2 Mild Comp Detail (Crypto-Hi) ───");
        foreach(var f in mildFracs){
            var tr=allT.Where(t=>t.proto=="P2_Mild"&&Math.Abs(t.frac-f)<0.001).ToArray();
            int stp=tr.Count(t=>t.immHi&&t.persist),pers=tr.Count(t=>t.persist);
            _o.WriteLine($"  {f*100,3:F0}%: N={tr.Length,2} ImmHi={tr.Count(t=>t.immHi),2} Delayed={pers-stp,2} Strict={stp,2}");
        }

        // ── Seed 57 vs G3 peers ──
        _o.WriteLine($"\n─── Seed 57 vs Crypto-Hi Peers ───");
        foreach(var sd in g3.OrderBy(s=>s)){
            var tr=allT.Where(t=>t.seed==sd&&t.proto=="P2_Mild").ToArray();
            int stp=tr.Count(t=>t.immHi&&t.persist),pers=tr.Count(t=>t.persist);
            var b=bDict[sd];
            _o.WriteLine($"Seed {sd,3}: d0={b.d0:F4} km0={b.km0:F4} dh0={b.dh0:F4}  anyStrict={stp>0} anyPersist={pers>0}");
        }
    }

    [Fact]public void HBK_02_CrossNPathwayValidation(){
        _o.WriteLine("═══ HBK: Cross-N pathway validation ═══");
        foreach(var n in new[]{67,71,72}){
            _o.WriteLine($"\n─── N={n} ───");
            var hi=PCent(n,true);var lo=PCent(n,false);
            var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<25;s++)if(!IsHi(n,s))loSeeds.Add(s);
            var seeds=loSeeds.ToArray();

            var bases=new ConcurrentBag<SBase>();
            Parallel.ForEach(seeds,s=>{bases.Add(ProfileB(n,s,hi));});
            var bDict=bases.ToDictionary(b=>b.seed);
            var g1=bases.Where(b=>b.d0>0.50).Select(b=>b.seed).ToList();
            var g3=bases.Where(b=>b.d0<0.40&&b.km0>0.98).Select(b=>b.seed).ToList();

            // P1 at 50% on G1
            var p1T=new ConcurrentBag<Trial>();
            Parallel.ForEach(g1.ToArray(),s=>{var b=bDict[s];p1T.Add(RunTrial(n,s,b.d0*0.50,b,hi,lo,"P1"));});
            var p1=p1T.ToArray();
            int p1St=p1.Count(t=>t.immHi&&t.persist);

            // P2 at 10% on G3
            var p2T=new ConcurrentBag<Trial>();
            Parallel.ForEach(g3.ToArray(),s=>{var b=bDict[s];p2T.Add(RunTrial(n,s,b.d0*0.90,b,hi,lo,"P2"));});
            var p2=p2T.ToArray();
            int p2St=p2.Count(t=>t.immHi&&t.persist);

            _o.WriteLine($"G1(N={g1.Count}): 50% → Strict={p1St}  G3(N={g3.Count}): 10% → Strict={p2St}");
        }
    }

    [Fact]public void HBK_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (compression-room validated): HBK_01 P1_Strong");
        _o.WriteLine("Gate B (crypto-Hi validated): HBK_01 P2_Mild");
        _o.WriteLine("Gate C (Seed 57 is outlier): HBK_01 seed 57 vs peers");
        _o.WriteLine("Gate D (pathway matching): HBK_01 WRONG protocols");
        _o.WriteLine("Gate F (N-dependent): HBK_02 cross-N");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("N=71 primary, N=67/72 validation. No physical claims.");
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

    static SBase ProfileB(int n,int seed,P3 hi){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);double om3=Of(h3E,n).Average();
        var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double dm3=Dm(d3E,n),km3=Km(K3E,n),ks3=Ks(K3E,n);
        return new SBase{seed=seed,d0=dm3,km0=km3,ks0=ks3,om0=om3,dh0=Dist(dm3,km3,ks3,hi)};
    }

    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,string proto){
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
        var dT2=DL(Nm(RP(hT2,n),n),n);
        return new Trial{seed=seed,proto=proto,frac=(b.d0-target)/Math.Max(b.d0,1e-10),d0=b.d0,
            dT1=Dm(dT1,n),dT2=Dm(dT2,n),kT1=Km(kT1,n),omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};
    }
    static double Dist(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
