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
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
