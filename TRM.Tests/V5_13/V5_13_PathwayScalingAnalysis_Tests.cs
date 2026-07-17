using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_13;

[Trait("Category","V5_13"),Trait("Category","V5_13_HVA"),Trait("Category","LongRunning")]
public class V5_13_PathwayScalingAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,dh0;public string cls;}
    struct Trial{public int seed;public string proto,cls;public double d0,dT1,dT2,omT1,omT2;public bool immHi,persist;}

    public V5_13_PathwayScalingAnalysis_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HVA_01_CandidateQualityAndDegradation(){
        _o.WriteLine("═══ HVA: Candidate quality and holdout degradation analysis ═══");

        foreach(var n in new[]{67,71,72,75,80}){
            _o.WriteLine($"\n─── N={n} ───");
            var hi=PCent(n,true);var lo=PCent(n,false);
            _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4}  Lo: dm={lo.dm:F4} km={lo.km:F4}");

            // Profile both cohorts
            var train=ProfileCohort(n,0,99,hi,lo,50);
            var holdout=ProfileCohort(n,100,199,hi,lo,50);

            // ── Class frequencies ──
            _o.WriteLine(string.Format("\n{0,-10} {1,4} {2,4} {3,4} {4,4} {5,4} {6,4}","Cohort","P1b","P1","P2","P3","P4","N"));
            PrintFreqs("Train",train);PrintFreqs("Holdout",holdout);

            // ── Candidate quality: d0 distribution per class ──
            _o.WriteLine(string.Format("\n{0,-10} {1,6} {2,8} {3,8} {4,8} {5,8}","Cohort","Class","d0(mean)","d0(med)","km0(mean)","dh0(mean)"));
            foreach(var c in new[]{"P1b","P1","P2"}){
                PrintQuality("Train",c,train);
                PrintQuality("Holdout",c,holdout);
            }

            // ── Per-class matched success ──
            _o.WriteLine(string.Format("\n{0,-10} {1,6} {2,4} {3,6} {4,7} {5,8} {6,8}","Cohort","Class","N","Strict","Rate","dT1","dReb"));
            foreach(var c in new[]{"P1b","P1","P2"}){
                PrintClassRate("Train",c,train,n,hi,lo);
                PrintClassRate("Holdout",c,holdout,n,hi,lo);
            }
        }
    }

    [Fact]public void HVA_02_N75vsN80Comparison(){
        _o.WriteLine("═══ HVA: N=75 vs N=80 candidate comparison ═══");
        foreach(var n in new[]{75,80}){
            var hi=PCent(n,true);var lo=PCent(n,false);
            var cohort=ProfileCohort(n,0,99,hi,lo,50);
            var p1=cohort.Where(b=>b.cls=="P1"||b.cls=="P1b").ToArray();
            var p2=cohort.Where(b=>b.cls=="P2").ToArray();
            _o.WriteLine($"\nN={n}: P1={p1.Length} (d0 mean={p1.Average(b=>b.d0):F4})  P2={p2.Length} (d0 mean={p2.Average(b=>b.d0):F4})");

            // Per-class matched rate
            var trials=new ConcurrentBag<Trial>();
            foreach(var b in p1)trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"MATCHED"));
            foreach(var b in p2)trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"MATCHED"));
            var all=trials.ToArray();
            int p1St=p1.Length>0?all.Count(t=>(t.cls=="P1"||t.cls=="P1b")&&t.immHi&&t.persist):0;
            int p2St=p2.Length>0?all.Count(t=>t.cls=="P2"&&t.immHi&&t.persist):0;
            _o.WriteLine($"P1 strict: {p1St}/{p1.Length} ({(p1.Length>0?(double)p1St/p1.Length*100:0):F0}%)  P2 strict: {p2St}/{p2.Length} ({(p2.Length>0?(double)p2St/p2.Length*100:0):F0}%)");
        }
    }

    [Fact]public void HVA_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (model scales): HVA_01 per-class holdout rates");
        _o.WriteLine("Gate B (N=75 strongest): HVA_02 N75 vs N80");
        _o.WriteLine("Gate C (N=80 saturation): HVA_02 N80 analysis");
        _o.WriteLine("Gate D (degradation explained): HVA_01 candidate quality");
        _o.WriteLine("Gate F (N-conditioned required): N-dependent class behavior");
        _o.WriteLine("═══ MODEL STATUS ═══");
        _o.WriteLine("Evaluate: robust / partially transferable / N-conditioned / degraded / local.");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("Frozen V5.12 rules. No threshold tuning. No physical claims.");
    }

    // ── Profiling ──
    List<SBase> ProfileCohort(int n,int start,int end,P3 hi,P3 lo,int max){
        var loSeeds=new List<int>();
        for(int s=start;s<=end&&loSeeds.Count<max;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var bases=new ConcurrentBag<SBase>();
        Parallel.ForEach(loSeeds.ToArray(),s=>{bases.Add(Classify(ProfileB(n,s,hi),hi));});
        return bases.ToList();
    }

    void PrintFreqs(string lbl,List<SBase> b){
        int p1b=b.Count(x=>x.cls=="P1b"),p1=b.Count(x=>x.cls=="P1"),p2=b.Count(x=>x.cls=="P2");
        int p3=b.Count(x=>x.cls=="P3"),p4=b.Count(x=>x.cls=="P4");
        _o.WriteLine($"{lbl,-10} {p1b,4} {p1,4} {p2,4} {p3,4} {p4,4} {b.Count,4}");
    }

    void PrintQuality(string lbl,string cls,List<SBase> b){
        var g=b.Where(x=>x.cls==cls).ToArray();
        if(g.Length==0)return;
        var sorted=g.OrderBy(x=>x.d0).ToArray();
        _o.WriteLine($"{lbl,-10} {cls,6} {g.Average(x=>x.d0),8:F4} {sorted[g.Length/2].d0,8:F4} {g.Average(x=>x.km0),8:F4} {g.Average(x=>x.dh0),8:F4}");
    }

    void PrintClassRate(string lbl,string cls,List<SBase> b,int n,P3 hi,P3 lo){
        var g=b.Where(x=>x.cls==cls).ToArray();
        if(g.Length==0)return;
        var trials=new ConcurrentBag<Trial>();
        double tgt=cls=="P2"?g[0].d0*0.90:g[0].d0*0.50;
        Parallel.ForEach(g,s=>{trials.Add(RunTrial(n,s.seed,cls=="P2"?s.d0*0.90:s.d0*0.50,s,hi,lo,"MATCHED"));});
        var all=trials.ToArray();
        int st=all.Count(t=>t.immHi&&t.persist);
        double dT1=all.Average(t=>t.dT1),dReb=all.Average(t=>t.dT2-t.dT1);
        _o.WriteLine($"{lbl,-10} {cls,6} {g.Length,4} {st,6} {(double)st/g.Length*100,6:F1}% {dT1,8:F4} {dReb,8:F4}");
    }

    // ── Classification (frozen V5.12) ──
    static SBase Classify(SBase b,P3 hi){
        string cls;
        if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";
        else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";
        else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";
        return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,dh0=b.dh0,cls=cls};
    }
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}

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
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        return new SBase{seed=seed,d0=Dm(d3E,n),km0=Km(K3E,n),ks0=Ks(K3E,n),dh0=Dist(Dm(d3E,n),hi.dm),cls=""};
    }
    static double Dist(double dm,double hdm){return Math.Abs(dm-hdm);}

    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,string proto){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K,n,S,seed+100);double om1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);
        var hT2=Sim(kT1,n,S,seed+200);double om2=Of(hT2,n).Average();
        double dT2=Dm(DL(Nm(RP(hT2,n),n),n),n);
        return new Trial{seed=seed,proto=proto,cls=b.cls,d0=b.d0,dT1=Dm(dT1,n),dT2=dT2,omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};
    }
}
