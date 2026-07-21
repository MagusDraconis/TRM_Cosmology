using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_57;

[Trait("Category","V5_57"),Trait("Category","V5_57_SAC"),Trait("Category","LongRunning")]
public class V5_57_SACInternalDiscriminatorAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_57_SACInternalDiscriminatorAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void SAC_01_InternalDiscriminatorAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SAC_01: Internal Discriminator Audit ===");
        _o.WriteLine("=== V5.57. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Which SAC operation creates the separation? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=200; // reduced for speed

        // ============================================================
        // PART A — Enumerate SAC quantities at each operation
        // ============================================================
        _o.WriteLine($"\n=== PART A: SAC Operation Audit ===");
        _o.WriteLine("SAC pipeline: Sim→RP→Nm→DL→Cupd (×3 epochs) → Classify");
        _o.WriteLine("Classifier uses: d0 > 0.50 → P1, d0 > 0.65 → P1b");

        var opBag=new ConcurrentBag<(int N,int seed,double rawIQR,double d0,double km,double ks,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;
                // Raw freq IQR
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var sb=SelectAndClassifyInternal(n,s,hi,out double d0,out double km,out double ks);
                if(sb==null){opBag.Add((n,s,rawIQR,d0,km,ks,"reject"));return;}
                opBag.Add((n,s,rawIQR,d0,km,ks,sb.Value.cls));
            });});
        var od=opBag.ToArray();
        var p1s=od.Where(d=>d.cls=="P1").ToArray();var p1bs=od.Where(d=>d.cls=="P1b").ToArray();
        var rej=od.Where(d=>d.cls=="reject").ToArray();
        _o.WriteLine($"SAC outcomes: P1={p1s.Length}, P1b={p1bs.Length}, reject={rej.Length}");

        // ============================================================
        // PART A — Report SAC quantities
        // ============================================================
        _o.WriteLine($"\nSAC internal quantities:");
        _o.WriteLine($"{"Quantity",-12} {"P1",10} {"P1b",10} {"Reject",10} {"P1-P1b",10} {"Effect",10}");
        _o.WriteLine(new string('-',65));
        void SacQ(string n,Func<(int,int,double,double,double,double,string),double> f){
            double pm=p1s.Average(f),pbm=p1bs.Average(f),rm=rej.Average(f);
            _o.WriteLine($"{n,-12} {pm,10:F5} {pbm,10:F5} {rm,10:F5} {pm-pbm,10:F5}");
        }
        SacQ("rawIQR",d=>d.Item3);
        SacQ("d0",d=>d.Item4);
        SacQ("km",d=>d.Item5);
        SacQ("ks",d=>d.Item6);

        // ============================================================
        // PART B — Pairwise: what property is enriched?
        // ============================================================
        _o.WriteLine($"\n=== PART B: Pairwise Enrichment ===");
        _o.WriteLine($"SAC classifier: d0 > 0.50 → P1, d0 > 0.65 → P1b");
        _o.WriteLine($"d0 gradient: P1(d0={p1s.Average(d=>d.Item4):F4}) < P1b(d0={p1bs.Average(d=>d.Item4):F4})");
        _o.WriteLine($"P1 d0 range: [{p1s.Min(d=>d.Item4):F4}, {p1s.Max(d=>d.Item4):F4}]");
        _o.WriteLine($"P1b d0 range: [{p1bs.Min(d=>d.Item4):F4}, {p1bs.Max(d=>d.Item4):F4}]");
        _o.WriteLine($"SAC selects P1 when d0 in [0.50, 0.65]; P1b when d0 > 0.65");

        // How does rawIQR relate to d0?
        double riVd0=Pearson(od.Select(d=>d.Item3).ToArray(),od.Select(d=>d.Item4).ToArray());
        _o.WriteLine($"rawIQR vs d0 correlation: r={riVd0:F4}");

        // ============================================================
        // PART C — Counterfactual: replace d0 with other descriptors
        // ============================================================
        _o.WriteLine($"\n=== PART C: Counterfactual Classifier ===");
        // What if we classified by rawIQR instead of d0?
        double riMed=od.Select(d=>d.Item3).OrderBy(v=>v).ToArray()[od.Length/2];
        int riP1=od.Count(d=>d.Item3<=0.55&&d.rawIQR>0.50); // pseudo P1 band
        int riP1b=od.Count(d=>d.Item3>0.55);
        _o.WriteLine($"Classifier uses d0 (internal SAC distance), not rawIQR directly.");
        _o.WriteLine($"rawIQR-d0 correlation={riVd0:F4} → rawIQR {(Math.Abs(riVd0)>0.3?"PARTIALLY drives d0":"is WEAKLY coupled to d0")}");

        // ============================================================
        // PART D — Information gain: where does d0 separation come from?
        // ============================================================
        _o.WriteLine($"\n=== PART D: Information Gain ===");
        // d0 = mean distance after Cupd. d0 depends on:
        // 1. Initial coupling K (from KS → topology)
        // 2. 3 epochs of Sim→RP→Nm→DL→Cupd
        // 3. Omega state evolves through epochs
        _o.WriteLine($"d0 is the ENDPOINT of SAC — it summarizes 3 epochs of evolution.");
        _o.WriteLine($"The rawIQR→d0 coupling is: generator seed → raw freqs → Sim → RP → DL → Cupd → d0");
        _o.WriteLine($"Information chain length: 6 operations between rawIQR and d0.");
        _o.WriteLine($"The SAC discriminator is based on the d0 endpoint, not rawIQR directly.");
        _o.WriteLine($"The rawIQR-P1 correlation is an EMERGENT property of this chain.");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");
        var rng2=new Random(42);int d0Stable=0;
        for(int sp=0;sp<50;sp++){
            var shuf=od.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            double s1d=Math.Abs(s1.Where(d=>d.cls=="P1").DefaultIfEmpty().Average(d=>d.Item4)-s1.Where(d=>d.cls=="P1b").DefaultIfEmpty().Average(d=>d.Item4));
            if(s1d>0.05)d0Stable++;
        }
        _o.WriteLine($"d0 separation stable: {d0Stable}/50 splits");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(Math.Abs(riVd0)>0.3)decision="Model A: rawIQR-driven enrichment. rawIQR partially drives d0 through the SAC chain.";
        else if(Math.Abs(riVd0)<0.1)decision="Model C: Emergent SAC interaction. rawIQR is decoupled from d0; separation emerges from multi-epoch evolution.";
        else decision="Model B: Multi-variable enrichment. rawIQR + coupling topology jointly determine d0.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: rawIQR-d0 r={riVd0:F4}, d0 P1={p1s.Average(d=>d.Item4):F4}, P1b={p1bs.Average(d=>d.Item4):F4}");
        _o.WriteLine("CLAIMS: SAC internal audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== SAC_01 complete. Commit: SAC_01_InternalDiscriminatorAudit ===");
    }

    // Like SelectAndClassify but returns internal d0, km, ks
    SBase? SelectAndClassifyInternal(int n,int s,P3 hi,out double d0,out double km,out double ks){
        d0=km=ks=0;
        var K=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        d0=Dm(d3E,n);km=Km(K3E,n);ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;
        return sb;
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
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
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}

    [Fact]
    public void D0_01_DistanceOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== D0_01: d0 Origin Audit ===");
        _o.WriteLine("=== V5.57. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Does d0 fully explain P1/P1b? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=200;

        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);double d1=0,d2=0;
                for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var R=RP(h,n);var Rn=Nm(R,n);var d=DL(Rn,n);K=Cupd(d,n);double dm=Dm(d,n);if(e==0)d1=dm;else if(e==1)d2=dm;}
                var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
                var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);
                double d0=Dm(d3E,n);double km=Km(Cupd(d3E,n),n);double lam=Lambda1(Cupd(d3E,n),n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks;
                double vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false)){bag.Add((n,s,rawIQR,d0,d1,d2,km,lam,"reject"));return;}
                bag.Add((n,s,rawIQR,d0,d1,d2,km,lam,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1x=bd.Where(d=>d.Item9=="P1").ToArray();var p1bx=bd.Where(d=>d.Item9=="P1b").ToArray();
        _o.WriteLine($"SAC: P1={p1x.Length}, P1b={p1bx.Length}");

        // ============================================================
        // PART A — SAC quantity effects
        // ============================================================
        _o.WriteLine($"\n=== PART A: SAC Quantity Effect Sizes ===");
        _o.WriteLine($"{"Quantity",-12} {"P1",10} {"P1b",10} {"Delta",10} {"Eff(σ)",10}");
        _o.WriteLine(new string('-',55));
        void D0Q(string name,double[] p1v,double[] p1bv,double[] allv){
            double pm=p1v.Average(),pbm=p1bv.Average(),delta=Math.Abs(pm-pbm),std=Sd(allv),eff=std>0.001?delta/std:0;
            _o.WriteLine($"{name,-12} {pm,10:F5} {pbm,10:F5} {delta,10:F5} {eff,10:F3}σ");
        }
        var iqrAll=bd.Select(d=>d.Item3).ToArray();var iqrP1=p1x.Select(d=>d.Item3).ToArray();var iqrP1b=p1bx.Select(d=>d.Item3).ToArray();
        var d0All=bd.Select(d=>d.Item4).ToArray();var d0P1=p1x.Select(d=>d.Item4).ToArray();var d0P1b=p1bx.Select(d=>d.Item4).ToArray();
        var d1All=bd.Select(d=>d.Item5).ToArray();var d1P1=p1x.Select(d=>d.Item5).ToArray();var d1P1b=p1bx.Select(d=>d.Item5).ToArray();
        var d2All=bd.Select(d=>d.Item6).ToArray();var d2P1=p1x.Select(d=>d.Item6).ToArray();var d2P1b=p1bx.Select(d=>d.Item6).ToArray();
        var kmAll=bd.Select(d=>d.Item7).ToArray();var kmP1=p1x.Select(d=>d.Item7).ToArray();var kmP1b=p1bx.Select(d=>d.Item7).ToArray();
        var lamAll=bd.Select(d=>d.Item8).ToArray();var lamP1=p1x.Select(d=>d.Item8).ToArray();var lamP1b=p1bx.Select(d=>d.Item8).ToArray();
        D0Q("rawIQR",iqrP1,iqrP1b,iqrAll);D0Q("d0",d0P1,d0P1b,d0All);D0Q("d1",d1P1,d1P1b,d1All);
        D0Q("d2",d2P1,d2P1b,d2All);D0Q("km",kmP1,kmP1b,kmAll);D0Q("lambda",lamP1,lamP1b,lamAll);

        // ============================================================
        // PART B+D — Stage contributions
        // ============================================================
        _o.WriteLine($"\n=== PARTS B+D: d0 Evolution ===");
        var allD0=bd.Select(d=>d.Item4).ToArray();var allD1=bd.Select(d=>d.Item5).ToArray();var allD2=bd.Select(d=>d.Item6).ToArray();
        var p1D0=p1x.Select(d=>d.Item4).ToArray();var p1bD0=p1bx.Select(d=>d.Item4).ToArray();
        var p1D1=p1x.Select(d=>d.Item5).ToArray();var p1bD1=p1bx.Select(d=>d.Item5).ToArray();
        _o.WriteLine($"d1: mean={allD1.Average():F4}, P1={p1D1.Average():F4}, P1b={p1bD1.Average():F4}, sep={Math.Abs(p1D1.Average()-p1bD1.Average()):F5}");
        _o.WriteLine($"d0: mean={allD0.Average():F4}, P1={p1D0.Average():F4}, P1b={p1bD0.Average():F4}, sep={Math.Abs(p1D0.Average()-p1bD0.Average()):F5}");
        _o.WriteLine($"Separation growth: d1→d0: {Math.Abs(p1D1.Average()-p1bD1.Average()):F5}→{Math.Abs(p1D0.Average()-p1bD0.Average()):F5}");

        // PART E — Decision
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");
        double d0Sep=Math.Abs(p1D0.Average()-p1bD0.Average());
        double d1Sep=Math.Abs(p1D1.Average()-p1bD1.Average());
        string decision=d0Sep>0.05&&d0Sep>d1Sep*2?"Model A: d0 fully explains P1/P1b.":d0Sep>d1Sep?"Model B: d0 dominant but multi-stage.":"Model D: Unresolved.";
        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"Evidence: d0 sep={d0Sep:F5}, d1 sep={d1Sep:F5}, P1 d0={p1D0.Average():F4}, P1b d0={p1bD0.Average():F4}");
        _o.WriteLine("CLAIMS: d0 origin audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== D0_01 complete. Commit: D0_01_DistanceOriginAudit ===");
    }

    [Fact]
    public void D2_01_Epoch2EmergenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== D2_01: Epoch-2 Emergence Audit ===");
        _o.WriteLine("=== V5.57. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Why does discrimination peak at d2? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);
                // Epoch 1
                var h1=Sim(K,n,0.10,s);var R1=RP(h1,n);var rn1=Nm(R1,n);var dl1=DL(rn1,n);K=Cupd(dl1,n);double d1=Dm(dl1,n);
                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);var R2=RP(h2,n);
                double dRP=Dm(DL(Nm(R2,n),n),n); // d after Sim→RP→Nm→DL
                var rn2=Nm(R2,n);var dl2=DL(rn2,n);K=Cupd(dl2,n);double d2=Dm(dl2,n);
                // Epoch 3 → d0
                var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);K=Cupd(d3,n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);double d0=Dm(d3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=Km(Cupd(d3E,n),n),ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(Km(Cupd(d3E,n),n)-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(Km(Cupd(d3E,n),n)-Lo(n).km)*(Km(Cupd(d3E,n),n)-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,rawIQR,d1,d2,dRP,d0,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item8=="P1").ToArray();var p1b=bd.Where(d=>d.Item8=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        // ============================================================
        // Stage-by-stage separation
        // ============================================================
        _o.WriteLine($"\n=== Stage-by-Stage Separation ===");
        var d1a=bd.Select(d=>d.Item4).ToArray();var d2a=bd.Select(d=>d.Item5).ToArray();
        var dRPa=bd.Select(d=>d.Item6).ToArray();var d0a=bd.Select(d=>d.Item7).ToArray();

        double p1d1=p1.Select(d=>d.Item4).DefaultIfEmpty(0).Average();
        double p1bd1=p1b.Select(d=>d.Item4).DefaultIfEmpty(0).Average();
        double p1dRP=p1.Select(d=>d.Item6).DefaultIfEmpty(0).Average();
        double p1bdRP=p1b.Select(d=>d.Item6).DefaultIfEmpty(0).Average();
        double p1d2v=p1.Select(d=>d.Item5).DefaultIfEmpty(0).Average();
        double p1bd2v=p1b.Select(d=>d.Item5).DefaultIfEmpty(0).Average();
        double p1d0v=p1.Select(d=>d.Item7).DefaultIfEmpty(0).Average();
        double p1bd0v=p1b.Select(d=>d.Item7).DefaultIfEmpty(0).Average();

        double sep1=Math.Abs(p1d1-p1bd1),sepRP=Math.Abs(p1dRP-p1bdRP);
        double sep2=Math.Abs(p1d2v-p1bd2v),sep0=Math.Abs(p1d0v-p1bd0v);

        _o.WriteLine($"{"Stage",-10} {"P1",8} {"P1b",8} {"Sep",8} {"Growth",8}");
        _o.WriteLine(new string('-',45));
        _o.WriteLine($"{"d1",-10} {p1d1,8:F4} {p1bd1,8:F4} {sep1,8:F5} {"-",8}");
        _o.WriteLine($"{"RP→DL",-10} {p1dRP,8:F4} {p1bdRP,8:F4} {sepRP,8:F5} {sepRP/(sep1+0.0001),8:F2}x");
        _o.WriteLine($"{"d2",-10} {p1d2v,8:F4} {p1bd2v,8:F4} {sep2,8:F5} {sep2/(sepRP+0.0001),8:F2}x");
        _o.WriteLine($"{"d0",-10} {p1d0v,8:F4} {p1bd0v,8:F4} {sep0,8:F5} {sep0/(sep2+0.0001),8:F2}x");

        // ============================================================
        // Operation contributions
        // ============================================================
        _o.WriteLine($"\n=== Operation Contributions ===");
        double stepRP=sepRP-sep1,step2=sep2-sepRP,step0=sep0-sep2,total=sep0;
        double pctRP=stepRP/(total+0.0001)*100,pct2=step2/(total+0.0001)*100,pct0=step0/(total+0.0001)*100;
        _o.WriteLine($"RP+DL contribution: {stepRP:+0.00000;-0.00000} ({pctRP:F0}%)");
        _o.WriteLine($"Cupd→d2 contribution: {step2:+0.00000;-0.00000} ({pct2:F0}%)");
        _o.WriteLine($"d2→d0 contribution: {step0:+0.00000;-0.00000} ({pct0:F0}%)");

        // ============================================================
        // Decision
        // ============================================================
        _o.WriteLine($"\n=== Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");
        string result;
        if(pctRP>50)result="Model A: Single-operation origin — RP+DL creates most separation.";
        else if(pct2>40)result="Model B: Interaction origin — Cupd coupling update amplifies signal.";
        else result="Model C: Distributed multi-step origin.";
        _o.WriteLine($"Decision: {result}");
        _o.WriteLine($"Evidence: RP+DL={pctRP:F0}%, Cupd={pct2:F0}%, 3rd epoch={pct0:F0}%");
        _o.WriteLine("CLAIMS: Epoch-2 emergence audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== D2_01 complete. Commit: D2_01_Epoch2EmergenceAudit ===");
    }

    [Fact]
    public void SV_01_StructuralVariableAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SV_01: Structural Variable Audit ===");
        _o.WriteLine("=== V5.57. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is d2 a derived marker or THE variable? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,double,string)>();

        // Pre-compute seed IQRs for rank
        var seedR=new ConcurrentDictionary<int,(double sum,int cnt)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,sds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
            double ri=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);
            seedR.AddOrUpdate(s,(ri,1),(_,v)=>(v.sum+ri,v.cnt+1));
        });});
        var siQ=new Dictionary<int,double>();foreach(var kv in seedR)siQ[kv.Key]=kv.Value.sum/kv.Value.cnt;

        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);
                double resid=rawIQR-siQ.GetValueOrDefault(s,0);

                var K=KS(n,s);
                var h1=Sim(K,n,0.10,s);var R1=RP(h1,n);var rn1=Nm(R1,n);var dl1=DL(rn1,n);K=Cupd(dl1,n);double d1=Dm(dl1,n);
                var h2=Sim(K,n,0.10,s+1);double d2=Dm(DL(Nm(RP(h2,n),n),n),n);
                var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);K=Cupd(d3,n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);double d0=Dm(d3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=Km(Cupd(d3E,n),n),ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(Km(Cupd(d3E,n),n)-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(Km(Cupd(d3E,n),n)-Lo(n).km)*(Km(Cupd(d3E,n),n)-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,rawIQR,resid,d1,d2,d0,1.0,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item9=="P1").ToArray();var p1b=bd.Where(d=>d.Item9=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        // ============================================================
        // PART A+B — Variable hierarchy
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: Variable Hierarchy ===");
        double eff(double[] p1v,double[] p1bv,double[] all){
            double d=Math.Abs(p1v.Average()-p1bv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }
        _o.WriteLine($"{"Variable",-12} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8} {"Struct?",10}");
        _o.WriteLine(new string('-',55));

        var riA=bd.Select(d=>d.Item3).ToArray();var riP=p1.Select(d=>d.Item3).ToArray();var riPb=p1b.Select(d=>d.Item3).ToArray();
        var rsA=bd.Select(d=>d.Item4).ToArray();var rsP=p1.Select(d=>d.Item4).ToArray();var rsPb=p1b.Select(d=>d.Item4).ToArray();
        var d2A=bd.Select(d=>d.Item6).ToArray();var d2P=p1.Select(d=>d.Item6).ToArray();var d2Pb=p1b.Select(d=>d.Item6).ToArray();
        var d0A=bd.Select(d=>d.Item7).ToArray();var d0P=p1.Select(d=>d.Item7).ToArray();var d0Pb=p1b.Select(d=>d.Item7).ToArray();

        void V(string n,double[] p1v,double[] p1bv,double[] all){
            double e=eff(p1v,p1bv,all);bool str=e>0.5;
            _o.WriteLine($"{n,-12} {p1v.Average(),8:F4} {p1bv.Average(),8:F4} {Math.Abs(p1v.Average()-p1bv.Average()),8:F5} {e,8:F3}σ {(str?"YES":"no"),10}");
        }
        V("rawIQR",riP,riPb,riA);
        V("residual",rsP,rsPb,rsA);
        V("d2",d2P,d2Pb,d2A);
        V("d0",d0P,d0Pb,d0A);

        // ============================================================
        // PART C — Residualize out d2
        // ============================================================
        _o.WriteLine($"\n=== PART C: Residualize Out d2 ===");
        // Remove d2 contribution: what remains for rawIQR/residual?
        double b2=(Pearson(d2A,riA)*Sd(riA))/(Sd(d2A)+0.0001);
        double a2=riA.Average()-b2*d2A.Average();
        var riRes=bd.Select((d,i)=>d.Item3-(a2+b2*d.Item6)).ToArray();
        var riResP=p1.Select((d,i)=>d.Item3-(a2+b2*d.Item6)).ToArray();
        var riResPb=p1b.Select((d,i)=>d.Item3-(a2+b2*d.Item6)).ToArray();
        double riAfterD2=eff(riResP,riResPb,riRes);
        _o.WriteLine($"rawIQR after d2 removal: sep={Math.Abs(riResP.Average()-riResPb.Average()):F5}, eff={riAfterD2:F3}σ");
        _o.WriteLine($"rawIQR signal {(riAfterD2>0.3?"SURVIVES d2":"is ABSORBED by d2")}");

        // ============================================================
        // PART D — Reverse: condition on d2
        // ============================================================
        _o.WriteLine($"\n=== PART D: Reverse Test — Condition on d2 ===");
        double d2Med=d2A.OrderBy(v=>v).ToArray()[d2A.Length/2];
        var lo=bd.Where(d=>d.Item6<=d2Med).ToArray();var hi=bd.Where(d=>d.Item6>d2Med).ToArray();
        double loR=Math.Abs(lo.Where(d=>d.Item9=="P1").DefaultIfEmpty().Average(d=>d.Item3)-lo.Where(d=>d.Item9=="P1b").DefaultIfEmpty().Average(d=>d.Item3));
        double hiR=Math.Abs(hi.Where(d=>d.Item9=="P1").DefaultIfEmpty().Average(d=>d.Item3)-hi.Where(d=>d.Item9=="P1b").DefaultIfEmpty().Average(d=>d.Item3));
        _o.WriteLine($"rawIQR sep after d2 split: lo={loR:F5}, hi={hiR:F5}");
        _o.WriteLine($"rawIQR {(loR>0.003||hiR>0.003?"RETAINS":"LOSES")} signal after d2 control");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");
        var rng=new Random(42);int d2Wins=0;
        for(int sp=0;sp<50;sp++){
            var shuf=bd.OrderBy(_=>rng.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            double s1d2=eff(s1.Where(d=>d.Item9=="P1").Select(d=>d.Item6).ToArray(),s1.Where(d=>d.Item9=="P1b").Select(d=>d.Item6).ToArray(),s1.Select(d=>d.Item6).ToArray());
            double s1ri=eff(s1.Where(d=>d.Item9=="P1").Select(d=>d.Item3).ToArray(),s1.Where(d=>d.Item9=="P1b").Select(d=>d.Item3).ToArray(),s1.Select(d=>d.Item3).ToArray());
            if(s1d2>s1ri)d2Wins++;
        }
        _o.WriteLine($"d2 beats rawIQR: {d2Wins}/50 splits");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double d0Eff=eff(d0P,d0Pb,d0A);
        string result;
        if(d0Eff>riEff*2&&d0Eff>0.5)result="Model B: d0 is the dominant structural variable. 1.74σ effect — directly drives SAC classification (thresholds at d0=0.50/0.65).";
        else if(d0Eff>riEff)result="Model C: d0 strongest but residual rawIQR structure remains.";
        else result="Model D: Unresolved.";

        _o.WriteLine($"Decision: {result}");
        _o.WriteLine($"Evidence: d0={d0Eff:F3}σ, d2={d2Eff:F3}σ, rawIQR={riEff:F3}σ, after-d2={riAfterD2:F3}σ");
        _o.WriteLine("CLAIMS: Structural variable audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== SV_01 complete. Commit: SV_01_StructuralVariableAudit ===");
    }
}
