using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBL"),Trait("Category","LongRunning")]
public class V5_12_PathwaySufficiencyAndSelectionAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,om0,dh0;public string cls;}
    struct Trial{public int seed,clsN;public string proto;public double d0,dT1,dT2,omT1,omT2;public bool immHi,persist;}

    public V5_12_PathwaySufficiencyAndSelectionAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBL_01_ProspectivePathwaySelection(){
        _o.WriteLine("═══ HBL: Prospective pathway selection at N=71 ═══");
        var r=RunN(71,50);
        PrintResults(r);
    }

    [Fact]public void HBL_02_CrossNPathwaySelection(){
        _o.WriteLine("═══ HBL: Cross-N pathway selection ═══");
        foreach(var n in new[]{67,71,72}){
            _o.WriteLine($"\n─── N={n} ───");
            var r=RunN(n,30);
            _o.WriteLine($"Classification: P1={r.p1} P1b={r.p1b} P2={r.p2} P3={r.p3} P4={r.p4}");
            _o.WriteLine($"Matched: {r.matchedTrials} trials, {r.matchedStrict} strict ({r.matchedRate*100:F0}%)");
            _o.WriteLine($"Univ Strong: {r.univSTrials} trials, {r.univSStrict} strict ({r.univSRate*100:F0}%)");
            _o.WriteLine($"Univ Mild: {r.univMTrials} trials, {r.univMStrict} strict ({r.univMRate*100:F0}%)");
        }
    }

    [Fact]public void HBL_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (pathway selection improves): HBL_01 matched vs universal");
        _o.WriteLine("Gate B (compression-room candidate): HBL_01 P1+matched rate");
        _o.WriteLine("Gate C (crypto-Hi candidate): HBL_01 P2+matched rate");
        _o.WriteLine("Gate D (pathway matching matters): HBL_01 matched vs mismatched");
        _o.WriteLine("Gate E (N-window): HBL_02 cross-N");
        _o.WriteLine("Gate G (sparse): global strict count assessment");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("Pre-registered classes, no post-hoc tuning. No physical claims.");
    }

    // ── Result struct ──
    struct NResult{public int p1,p1b,p2,p3,p4;public int matchedTrials,matchedStrict;public double matchedRate;
        public int mismatchedTrials,mismatchedStrict;public double mismatchedRate;
        public int univSTrials,univSStrict;public double univSRate;
        public int univMTrials,univMStrict;public double univMRate;}

    NResult RunN(int n,int maxSeeds){
        var hi=PCent(n,true);var lo=PCent(n,false);
        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<maxSeeds;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Baseline + classify
        var bases=new ConcurrentBag<SBase>();
        Parallel.ForEach(seeds,s=>{bases.Add(Classify(ProfileB(n,s,hi),hi,n));});
        var bDict=bases.ToDictionary(b=>b.seed);

        int p1=bases.Count(b=>b.cls=="P1"),p1b=bases.Count(b=>b.cls=="P1b");
        int p2=bases.Count(b=>b.cls=="P2"),p3=bases.Count(b=>b.cls=="P3"),p4=bases.Count(b=>b.cls=="P4");

        // Matched: P1/P1b→50%, P2→10%
        var trials=new ConcurrentBag<Trial>();
        foreach(var b in bases.Where(b=>b.cls=="P1"||b.cls=="P1b"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"MATCHED",1));
        foreach(var b in bases.Where(b=>b.cls=="P2"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"MATCHED",2));

        // Mismatched: P1/P1b→10%, P2→50%
        foreach(var b in bases.Where(b=>b.cls=="P1"||b.cls=="P1b"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"MISMATCHED",1));
        foreach(var b in bases.Where(b=>b.cls=="P2"))
            trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"MISMATCHED",2));

        // Universal strong: all→50%
        foreach(var b in bases)
            trials.Add(RunTrial(n,b.seed,b.d0*0.50,b,hi,lo,"UNIV_S",0));

        // Universal mild: all→10%
        foreach(var b in bases)
            trials.Add(RunTrial(n,b.seed,b.d0*0.90,b,hi,lo,"UNIV_M",0));

        var allT=trials.ToArray();

        var matched=allT.Where(t=>t.proto=="MATCHED").ToArray();
        var mismatched=allT.Where(t=>t.proto=="MISMATCHED").ToArray();
        var univS=allT.Where(t=>t.proto=="UNIV_S").ToArray();
        var univM=allT.Where(t=>t.proto=="UNIV_M").ToArray();

        return new NResult{p1=p1,p1b=p1b,p2=p2,p3=p3,p4=p4,
            matchedTrials=matched.Length,matchedStrict=matched.Count(t=>t.immHi&&t.persist),
            matchedRate=(double)matched.Count(t=>t.immHi&&t.persist)/Math.Max(matched.Length,1),
            mismatchedTrials=mismatched.Length,mismatchedStrict=mismatched.Count(t=>t.immHi&&t.persist),
            mismatchedRate=(double)mismatched.Count(t=>t.immHi&&t.persist)/Math.Max(mismatched.Length,1),
            univSTrials=univS.Length,univSStrict=univS.Count(t=>t.immHi&&t.persist),
            univSRate=(double)univS.Count(t=>t.immHi&&t.persist)/Math.Max(univS.Length,1),
            univMTrials=univM.Length,univMStrict=univM.Count(t=>t.immHi&&t.persist),
            univMRate=(double)univM.Count(t=>t.immHi&&t.persist)/Math.Max(univM.Length,1)};
    }

    void PrintResults(NResult r){
        _o.WriteLine($"Classification: P1={r.p1} P1b={r.p1b} P2={r.p2} P3={r.p3} P4={r.p4}");
        _o.WriteLine($"\n─── Protocol Comparison ───");
        _o.WriteLine($"{"Protocol",-12} {"Trials",6} {"Strict",6} {"Rate",7}");
        _o.WriteLine($"{"MATCHED",-12} {r.matchedTrials,6} {r.matchedStrict,6} {r.matchedRate*100,6:F1}%");
        _o.WriteLine($"{"MISMATCHED",-12} {r.mismatchedTrials,6} {r.mismatchedStrict,6} {r.mismatchedRate*100,6:F1}%");
        _o.WriteLine($"{"UNIV Strong",-12} {r.univSTrials,6} {r.univSStrict,6} {r.univSRate*100,6:F1}%");
        _o.WriteLine($"{"UNIV Mild",-12} {r.univMTrials,6} {r.univMStrict,6} {r.univMRate*100,6:F1}%");

        _o.WriteLine($"\n─── Gates ───");
        _o.WriteLine($"Matched vs Univ Strong: {(r.matchedRate>r.univSRate?"IMPROVES":"NO IMPROVEMENT")} ({r.matchedRate*100:F1}% vs {r.univSRate*100:F1}%)");
        _o.WriteLine($"Matched vs Univ Mild:   {(r.matchedRate>r.univMRate?"IMPROVES":"NO IMPROVEMENT")} ({r.matchedRate*100:F1}% vs {r.univMRate*100:F1}%)");
        _o.WriteLine($"Matched vs Mismatched:  {(r.matchedRate>r.mismatchedRate?"IMPROVES":"NO IMPROVEMENT")} ({r.matchedRate*100:F1}% vs {r.mismatchedRate*100:F1}%)");
    }

    // ── Classification ──
    static SBase Classify(SBase b,P3 hi,int n){
        string cls;
        if(b.d0>0.65)cls="P1b";
        else if(b.d0>0.50)cls="P1";
        else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";
        else if(b.d0>0.40&&b.d0<=0.50)cls="P3";
        else cls="P4";
        return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,om0=b.om0,dh0=b.dh0,cls=cls};
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
        var h3E=Sim(K3,n,S,seed+50);double om3=Of(h3E,n).Average();
        var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        return new SBase{seed=seed,d0=Dm(d3E,n),km0=Km(K3E,n),ks0=Ks(K3E,n),om0=om3,dh0=Dist(d3E,hi),cls=""};
    }
    static double Dist(double[,]d,P3 hi){int n=d.GetLength(0);return Math.Sqrt((Dm(d,n)-hi.dm)*(Dm(d,n)-hi.dm));}

    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,string proto,int clsN){
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
        return new Trial{seed=seed,clsN=clsN,proto=proto,d0=b.d0,
            dT1=Dm(dT1,n),dT2=Dm(DL(Nm(RP(hT2,n),n),n),n),omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};
    }
}
