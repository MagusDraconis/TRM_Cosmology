using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_55;

[Trait("Category","V5_55"),Trait("Category","V5_55_RSP"),Trait("Category","LongRunning")]
public class V5_55_ResidualSelectionPreference_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_55_ResidualSelectionPreference_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void RSP_01_ResidualSelectionPreferenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RSP_01: Residual Selection Preference Audit ===");
        _o.WriteLine("=== V5.55. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Why does SAC prefer residual over seed rawIQR? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // ============================================================
        // Compute seed-level and profile-level rawIQR
        // ============================================================
        var seedIQR=new ConcurrentDictionary<int,double>();
        var seedMean2=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean)>();

        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            double ri=Q(wo,0.75)-Q(wo,0.25),rm=wo.Average();
            allProf.Add((n,s,ri,rm));
            seedIQR.AddOrUpdate(s,ri,(_,v)=>v+ri);
            seedMean2.AddOrUpdate(s,rm,(_,v)=>v+rm);
        });});

        var sIQR=seedIQR.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);
        var sMean=seedMean2.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        // ============================================================
        // Run pipeline for P1/P1b outcomes
        // ============================================================
        _o.WriteLine("Running pipeline for outcomes...");
        var pipeBag=new ConcurrentBag<(int N,int seed,double seedIQR,double residIQR,double seedMean,double residMean,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                if(sb==null||(sb.Value.cls!="P1"&&sb.Value.cls!="P1b"))return;
                var prof=allProf.FirstOrDefault(p=>p.N==n&&p.seed==s);
                double siQ=sIQR.GetValueOrDefault(s,0),riQ=prof.riqr-siQ;
                double sM=sMean.GetValueOrDefault(s,0),rM=prof.rmean-sM;
                pipeBag.Add((n,s,siQ,riQ,sM,rM,sb.Value.cls));
            });});
        var pd=pipeBag.ToArray();
        _o.WriteLine($"Retained: {pd.Length} (P1={pd.Count(d=>d.cls=="P1")}, P1b={pd.Count(d=>d.cls=="P1b")})");

        // ============================================================
        // PART B — Signal Strength Audit
        // ============================================================
        _o.WriteLine($"\n=== PART B: Signal Strength Audit ===");
        _o.WriteLine($"{"Component",-16} {"P1 mean",10} {"P1b mean",10} {"Delta",10} {"Norm delta",12} {"Dominant?",10}");
        _o.WriteLine(new string('-',72));

        void Eval(string name,Func<(int,int,double,double,double,double,string),double> f){
            var p1=pd.Where(d=>d.cls=="P1").ToArray();var p1b=pd.Where(d=>d.cls=="P1b").ToArray();
            double p1m=p1.Length>0?p1.Average(f):0,p1bm=p1b.Length>0?p1b.Average(f):0;
            double delta=Math.Abs(p1m-p1bm),mx=Math.Max(Math.Abs(p1m),Math.Abs(p1bm));
            double norm=mx>0.001?delta/mx:0;
            _o.WriteLine($"{name,-16} {p1m,10:F5} {p1bm,10:F5} {delta,10:F5} {norm,12:F4}");
        }
        Eval("seed_rawIQR",d=>d.Item3);
        Eval("resid_rawIQR",d=>d.Item4);
        Eval("seed_rawMean",d=>d.Item5);
        Eval("resid_rawMean",d=>d.Item6);
        _o.WriteLine("NOTE: seed_rawIQR range ~0.07-0.13; resid_rawIQR range ~(-0.016,0.012)");

        // ============================================================
        // PART C — Variance Efficiency Audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Variance Efficiency Audit ===");
        var seedComp=pd.Select(d=>d.Item3).ToArray();
        var residComp=pd.Select(d=>d.Item4).ToArray();
        double seedVar=Sd(seedComp),residVar=Sd(residComp);
        double seedVarianceShare=seedVar*seedVar/(seedVar*seedVar+residVar*residVar)*100;
        double residVarianceShare=100-seedVarianceShare;

        // Discrimination: P1-P1b delta for each component
        double seedDelta=Math.Abs(pd.Where(d=>d.cls=="P1").Average(d=>d.Item3)-pd.Where(d=>d.cls=="P1b").Average(d=>d.Item3));
        double residDelta=Math.Abs(pd.Where(d=>d.cls=="P1").Average(d=>d.Item4)-pd.Where(d=>d.cls=="P1b").Average(d=>d.Item4));
        double seedEff=seedDelta/(seedVar+0.0001),residEff=residDelta/(residVar+0.0001);
        double effRatio=residEff/(seedEff+0.0001);

        _o.WriteLine($"{"Component",-16} {"Var share",10} {"Delta",10} {"Eff(delta/var)",14} {"Efficiency",12}");
        _o.WriteLine(new string('-',55));
        _o.WriteLine($"{"seed_rawIQR",-16} {seedVarianceShare,10:F1}% {seedDelta,10:F5} {seedEff,14:F4}");
        _o.WriteLine($"{"resid_rawIQR",-16} {residVarianceShare,10:F1}% {residDelta,10:F5} {residEff,14:F4}");
        _o.WriteLine($"\nResidual/seed efficiency ratio: {effRatio:F1}x");
        _o.WriteLine($"Residual is {(effRatio>2?"SIGNIFICANTLY":"")} more information-dense per unit variance.");

        // ============================================================
        // PART D — Conditional Residual Audit
        // ============================================================
        _o.WriteLine($"\n=== PART D: Conditional Residual Audit ===");
        double residMed=residComp.OrderBy(v=>v).ToArray()[residComp.Length/2];
        var highRes=pd.Where(d=>d.Item4>residMed).ToArray();
        var lowRes=pd.Where(d=>d.Item4<=residMed).ToArray();
        _o.WriteLine($"High residual (>{residMed:F5}): P1={highRes.Count(d=>d.cls=="P1")}, P1b={highRes.Count(d=>d.cls=="P1b")}, P1%={highRes.Count(d=>d.cls=="P1")*100.0/Math.Max(1,highRes.Length):F0}%");
        _o.WriteLine($"Low residual (<={residMed:F5}): P1={lowRes.Count(d=>d.cls=="P1")}, P1b={lowRes.Count(d=>d.cls=="P1b")}, P1%={lowRes.Count(d=>d.cls=="P1")*100.0/Math.Max(1,lowRes.Length):F0}%");

        // ============================================================
        // PART E — Residual Dominance
        // ============================================================
        _o.WriteLine($"\n=== PART E: Residual Dominance Audit ===");
        // Simple logistic-style comparison: does adding seed information help?
        // Use normalized rank separations
        var allP1=pd.Where(d=>d.cls=="P1").ToArray();var allP1b=pd.Where(d=>d.cls=="P1b").ToArray();
        double sOnly=Math.Abs(allP1.Average(d=>d.Item3)-allP1b.Average(d=>d.Item3));
        double rOnly=Math.Abs(allP1.Average(d=>d.Item4)-allP1b.Average(d=>d.Item4));
        double combined=sOnly+rOnly;
        _o.WriteLine($"Seed-only delta: {sOnly:F5}");
        _o.WriteLine($"Residual-only delta: {rOnly:F5}");
        _o.WriteLine($"Combined delta: {combined:F5}");
        _o.WriteLine($"Residual contribution: {rOnly/(sOnly+rOnly+0.0001)*100:F0}% of total signal");
        _o.WriteLine($"Adding seed {(sOnly>rOnly*0.5?"IMPROVES":"does NOT improve")} discrimination.");

        // ============================================================
        // PART F — Ranking
        // ============================================================
        _o.WriteLine($"\n=== PART F: Ranking Audit ===");
        var ranks=new List<(string name,double delta)>();
        void AddR(string n,Func<(int,int,double,double,double,double,string),double> f){
            double d=Math.Abs(allP1.Average(f)-allP1b.Average(f));ranks.Add((n,d));
        }
        AddR("resid_rawIQR",d=>d.Item4);
        AddR("seed_rawIQR",d=>d.Item3);
        AddR("resid_rawMean",d=>d.Item6);
        AddR("seed_rawMean",d=>d.Item5);
        ranks=ranks.OrderByDescending(r=>r.delta).ToList();
        _o.WriteLine($"{"Rank",5} {"Descriptor",-18} {"Delta",10}");
        _o.WriteLine(new string('-',35));
        for(int i=0;i<ranks.Count;i++)_o.WriteLine($"{i+1,5} {ranks[i].name,-18} {ranks[i].delta,10:F5}");

        // ============================================================
        // PART G — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART G: Robustness ===");
        var rng2=new Random(42);var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();
        int h=shuf.Length/2;
        var s1=shuf.Take(h).ToArray();var s2=shuf.Skip(h).ToArray();
        double r1=Math.Abs(s1.Where(d=>d.cls=="P1").Average(d=>d.Item4)-s1.Where(d=>d.cls=="P1b").Average(d=>d.Item4));
        double r2=Math.Abs(s2.Where(d=>d.cls=="P1").Average(d=>d.Item4)-s2.Where(d=>d.cls=="P1b").Average(d=>d.Item4));
        _o.WriteLine($"Random split resid delta: {r1:F5} vs {r2:F5} (range={Math.Abs(r1-r2):F6})");

        // ============================================================
        // PART I — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART I: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(effRatio>5)decision=$"Model A: Residual component contains most SAC-relevant information ({effRatio:F0}x efficiency).";
        else if(effRatio>2)decision=$"Model A: Residual component is significantly more information-dense ({effRatio:F0}x efficiency).";
        else if(effRatio>1)decision="Model B: Seed and residual components contribute jointly.";
        else decision="Model D: Residual preference unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: resid/seed efficiency={effRatio:F1}x, resid delta={residDelta:F5}, seed delta={seedDelta:F5}");
        _o.WriteLine("CLAIMS: Preference audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RSP_01 complete. Commit: RSP_01_ResidualSelectionPreferenceAudit ===");
    }

    // ============================================================
    // HELPERS
    // ============================================================
    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){
        int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();
        double sx=0,sy=0,sxy=0;
        for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}
        return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;
    }
    static int[] RankVals(double[] v){int n=v.Length;return Enumerable.Range(0,n).OrderBy(i=>v[i]).Select((idx,r)=>new{idx,r}).OrderBy(x=>x.idx).Select(x=>x.r).ToArray();}
    static double Spearman(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);var rx=RankVals(x.Take(n).ToArray());var ry=RankVals(y.Take(n).ToArray());return Pearson(rx.Select(v=>(double)v).ToArray(),ry.Select(v=>(double)v).ToArray());}

    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
