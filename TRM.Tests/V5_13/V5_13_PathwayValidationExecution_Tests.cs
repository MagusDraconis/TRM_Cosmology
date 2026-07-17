using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_13;

[Trait("Category","V5_13"),Trait("Category","V5_13_HVE"),Trait("Category","LongRunning")]
public class V5_13_PathwayValidationExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct Trial{public int seed,cohort;public string proto;public double d0,dT1,omT1,omT2;public bool immHi,persist;}

    public V5_13_PathwayValidationExecution_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HVE_01_OutOfSampleValidation(){
        _o.WriteLine("═══ HVE: Out-of-sample pathway validation ═══");
        // Full test: N=67,71,72 with seeds 0-199
        foreach(var n in new[]{67,71,72}){
            _o.WriteLine($"\n─── N={n} ───");
            var hi=PCent(n,true);var lo=PCent(n,false);
            _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4}  Lo: dm={lo.dm:F4} km={lo.km:F4}");

            var r0=RunCohort(n,0,99,hi,lo,"Train(0-99)");
            var r1=RunCohort(n,100,199,hi,lo,"Holdout(100-199)");

            _o.WriteLine(string.Format("\n{0,-16} {1,4} {2,4} {3,8} {4,6} {5,6} {6,6}","Cohort","P1","P2","Matched","UnivS","UnivM","Mis"));
            PrintRow(r0);PrintRow(r1);
        }

        // Extended N validation seeds 0-99 only
        _o.WriteLine($"\n─── Extended N Validation (seeds 0-99) ───");
        _o.WriteLine(string.Format("\n{0,4} {1,4} {2,4} {3,8} {4,6} {5,6} {6,6}","N","P1","P2","Matched","UnivS","UnivM","Mis"));
        foreach(var n in new[]{70,75,80}){
            var hi=PCent(n,true);var lo=PCent(n,false);
            var r=RunCohort(n,0,99,hi,lo,$"N={n}");
            _o.WriteLine($"{n,4} {r.p1,4} {r.p2,4} {r.matchedRate*100,7:F1}% {r.univSRate*100,5:F1}% {r.univMRate*100,5:F1}% {r.mismatchedRate*100,5:F1}%");
        }
    }

    [Fact]public void HVE_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (out-of-sample reproduction): HVE_01 holdout vs train");
        _o.WriteLine("Gate B (pathway transfer): HVE_01 P1/P2 counts on holdout");
        _o.WriteLine("Gate C (N-window): HVE_01 extended N");
        _o.WriteLine("Gate D (model degradation): HVE_01 holdout rate comparison");
        _o.WriteLine("Gate E (N=67 inaccessible): HVE_01 N=67 results");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("Frozen V5.12 classification. No threshold tuning. No physical claims.");
    }

    struct CohortResult{public int nSeeds,p1,p2;public double matchedRate,univSRate,univMRate,mismatchedRate;public string label;}

    CohortResult RunCohort(int n,int seedStart,int seedEnd,P3 hi,P3 lo,string label){
        var loSeeds=new List<int>();
        for(int s=seedStart;s<=seedEnd&&loSeeds.Count<50;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Baseline + classify with FROZEN V5.12 rules
        var bases=new ConcurrentBag<SBase>();
        Parallel.ForEach(seeds,s=>{bases.Add(Classify(ProfileB(n,s,hi),hi));});
        var bDict=bases.ToDictionary(b=>b.seed);
        int p1=bases.Count(b=>b.cls=="P1"||b.cls=="P1b");
        int p2=bases.Count(b=>b.cls=="P2");

        var trials=new ConcurrentBag<Trial>();
        int cohort=seedStart>=100?1:0;

        // MATCHED: P1/P1b → 50%, P2 → 10%
        foreach(var b in bases.Where(b=>b.cls=="P1"||b.cls=="P1b"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"MATCHED",cohort));
        foreach(var b in bases.Where(b=>b.cls=="P2"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"MATCHED",cohort));

        // UNIVERSAL strong (50%)
        foreach(var b in bases)
            trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"UNIV_S",cohort));

        // UNIVERSAL mild (10%)
        foreach(var b in bases)
            trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"UNIV_M",cohort));

        // MISMATCHED: P1→10%, P2→50%
        foreach(var b in bases.Where(b=>b.cls=="P1"||b.cls=="P1b"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"MISMATCHED",cohort));
        foreach(var b in bases.Where(b=>b.cls=="P2"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"MISMATCHED",cohort));

        var allT=trials.ToArray();
        double Rate(string p)=>allT.Where(t=>t.proto==p).ToArray() is var tr && tr.Length>0?(double)tr.Count(t=>t.immHi&&t.persist)/tr.Length:0;

        return new CohortResult{nSeeds=seeds.Length,p1=p1,p2=p2,
            matchedRate=Rate("MATCHED"),univSRate=Rate("UNIV_S"),univMRate=Rate("UNIV_M"),mismatchedRate=Rate("MISMATCHED"),label=label};
    }

    void PrintRow(CohortResult r){
        _o.WriteLine($"{r.label,-16} {r.p1,4} {r.p2,4} {r.matchedRate*100,7:F1}% {r.univSRate*100,5:F1}% {r.univMRate*100,5:F1}% {r.mismatchedRate*100,5:F1}%");
    }

    // ── Frozen V5.12 classification ──
    static SBase Classify(SBase b,P3 hi){
        string cls;
        if(b.d0>0.65)cls="P1b";
        else if(b.d0>0.50)cls="P1";
        else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";
        else if(b.d0>0.40&&b.d0<=0.50)cls="P3";
        else cls="P4";
        return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};
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
        return new SBase{seed=seed,d0=Dm(d3E,n),km0=Km(K3E,n),ks0=Ks(K3E,n),cls=""};
    }

    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,string proto,int cohort){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K,n,S,seed+100);double om1=Of(hT1,n).Average();
        var kT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(kT1,n,S,seed+200);double om2=Of(hT2,n).Average();
        return new Trial{seed=seed,cohort=cohort,proto=proto,d0=b.d0,dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};
    }
}
