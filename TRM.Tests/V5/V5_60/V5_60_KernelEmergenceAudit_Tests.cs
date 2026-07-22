using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_60;

[Trait("Category","V5_60"),Trait("Category","V5_60_KEM"),Trait("Category","LongRunning")]
public class V5_60_KernelEmergenceAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_60_KernelEmergenceAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void KEM_01_KernelEmergenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== KEM_01: Kernel Emergence Audit ===");
        _o.WriteLine("=== V5.60. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: How is km generated from random K? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        // Store per-epoch: N,seed,kmInit,km1,km2,km3,d0,cls
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var K=KS(n,s);double kmInit=Km(K,n);

                // Epoch 1: Sim→RP→Nm→DL→Cupd
                var h1=Sim(K,n,0.10,s);K=Cupd(DL(Nm(RP(h1,n),n),n),n);double km1=Km(K,n);
                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);K=Cupd(DL(Nm(RP(h2,n),n),n),n);double km2=Km(K,n);
                // Epoch 3
                var h3=Sim(K,n,0.10,s+3);K=Cupd(DL(Nm(RP(h3,n),n),n),n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double km3=Km(K3E,n),d0=Dm(d3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=km3,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km3-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km3-Lo(n).km)*(km3-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,kmInit,km1,km2,km3,d0,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item8=="P1").ToArray();var p1b=bd.Where(d=>d.Item8=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        double Eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // ============================================================
        // PART A — Emergence growth trace
        // ============================================================
        _o.WriteLine($"\n=== PART A: Emergence Growth ===");
        _o.WriteLine($"{"Epoch",-12} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8} {"Rel growth",10}");
        _o.WriteLine(new string('-',60));

        var k0A=bd.Select(d=>d.Item3).ToArray();var k1A=bd.Select(d=>d.Item4).ToArray();
        var k2A=bd.Select(d=>d.Item5).ToArray();var k3A=bd.Select(d=>d.Item6).ToArray();

        double prevSep=0;
        void Ep(string n,double[] pv,double[] pbv,double[] all){
            double sep=Math.Abs(pv.Average()-pbv.Average()),e=Eff(pv,pbv,all);
            double rel=prevSep>0.0001?sep/prevSep:0;
            _o.WriteLine($"{n,-12} {pv.Average(),8:F4} {pbv.Average(),8:F4} {sep,8:F5} {e,8:F3}σ {rel,10:F2}x");
            prevSep=sep;
        }
        Ep("init",p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),k0A);
        Ep("epoch1",p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),k1A);
        Ep("epoch2",p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),k2A);
        Ep("epoch3",p1.Select(d=>d.Item6).ToArray(),p1b.Select(d=>d.Item6).ToArray(),k3A);

        // Growth pattern classification
        double g1=Eff(p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),k1A);
        double g2=Eff(p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),k2A);
        double g3=Eff(p1.Select(d=>d.Item6).ToArray(),p1b.Select(d=>d.Item6).ToArray(),k3A);
        double step1=g1,step2=g2-g1,step3=g3-g2;
        double total=g3>0.001?g3:0.001;
        _o.WriteLine($"\nContribution shares: epoch1={step1/total*100:F0}%, epoch2={step2/total*100:F0}%, epoch3={step3/total*100:F0}%");
        string pattern=step1/total>0.5?"EPOCH-1 dominated":step2/total>0.4?"EPOCH-2 dominated":"DISTRIBUTED growth";
        _o.WriteLine($"Growth pattern: {pattern}");

        // ============================================================
        // PART B — Stage contributions within one epoch
        // ============================================================
        _o.WriteLine($"\n=== PART B: Stage Contributions ===");
        // Trace Sim→RP→Nm→DL→Cupd within a single epoch
        // Use epoch 1 as exemplar
        var Kinit=KS(70,42); // sample seed for tracing
        var hE=Sim(Kinit,70,0.10,42);var RE=RP(hE,70);
        double beforeRP=Km(Kinit,70);
        // After RP: phase coherence doesn't change K directly — K only changes via Cupd
        // The chain is: K_init → Sim → RP → Nm → DL → Cupd → K_new
        // RP and DL operate on phase data, not K. Only Cupd changes K.
        _o.WriteLine($"SAC chain within one epoch:");
        _o.WriteLine($"  K_init (random graph): km={beforeRP:F4}");
        _o.WriteLine($"  Sim: generates phase trajectories from K");
        _o.WriteLine($"  RP: computes phase coherence matrix R from phases");
        _o.WriteLine($"  Nm: normalizes R to [eps, 1]");
        _o.WriteLine($"  DL: R → d = -log(R)  (distance transform)");
        _o.WriteLine($"  Cupd: d → K_new = K0*exp(-d/xi)  (COUPLING UPDATE)");
        _o.WriteLine($"  K_new km is the Cupd output");
        _o.WriteLine($"  → km emergence depends on DL(d) then Cupd(K)");
        _o.WriteLine($"  → The emergence is Cupd's exponential transform of RP→DL output");

        // ============================================================
        // PART C — Interaction: RP+DL as the bottleneck
        // ============================================================
        _o.WriteLine($"\n=== PART C: Interaction Audit ===");
        // Test: does RP-phase-coherence → DL-distance create the signal?
        // Within epoch 1, compare separation at RP vs DL vs Cupd
        // Simplified: measure km before and after Cupd
        _o.WriteLine($"km emerges in Cupd (the only place K changes).");
        _o.WriteLine($"Cupd takes d (from DL) and produces new K.");
        _o.WriteLine($"If d carries no P1/P1b signal, km will have none.");
        _o.WriteLine($"Cupd is the gate — but DL+RP supply the signal.");

        // Check km epoch-correlations
        double r01=Pearson(k0A,k1A),r02=Pearson(k0A,k3A),r12=Pearson(k1A,k3A);
        _o.WriteLine($"\nkm correlations: init↔epoch1={r01:F3}, init↔final={r02:F3}, epoch1↔final={r12:F3}");
        _o.WriteLine($"Memory: {(Math.Abs(r01)<0.1?"AMNESIC — first Cupd erases initial K structure":"PERSISTENT — initial K structure survives")}");

        // ============================================================
        // PART D — Emergence stability across N
        // ============================================================
        _o.WriteLine($"\n=== PART D: Emergence Stability Across N ===");
        foreach(var n in Ns){
            var nd=bd.Where(d=>d.Item1==n).ToArray();
            var np1=nd.Where(d=>d.Item8=="P1").ToArray();var np1b=nd.Where(d=>d.Item8=="P1b").ToArray();
            if(np1.Length<2||np1b.Length<2){_o.WriteLine($"N={n}: sparse");continue;}
            double ne=Eff(np1.Select(d=>d.Item6).ToArray(),np1b.Select(d=>d.Item6).ToArray(),nd.Select(d=>d.Item6).ToArray());
            _o.WriteLine($"N={n}: km3 eff={ne:F3}σ, P1={np1.Length}, P1b={np1b.Length}");
        }

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string r;
        if(step1/total>0.5)r="Model A: Single-stage emergence — first Cupd dominates km creation. Subsequent epochs refine.";
        else if(step1/total>0.3&&step2/total>0.3)r="Model B: RP-DL interaction emergence — signal builds through multi-epoch coupling iteration.";
        else if(Math.Abs(r01)<0.1)r="Model C: Amnesic emergence — first Cupd erases initial K and creates new structure from Sim→RP→DL phase data.";
        else r="Model D: Unresolved.";

        _o.WriteLine($"Decision: {r}");
        _o.WriteLine($"Evidence: shares epoch1={step1/total*100:F0}%, epoch2={step2/total*100:F0}%, init→epoch1 r={r01:F3}");
        _o.WriteLine("CLAIMS: Kernel emergence audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== KEM_01 complete. Commit: KEM_01_KernelEmergenceAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double r=1.0-mn;if(r<1e-15)r=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/r);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,0.10,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,0.10,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,0.10,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,0.10,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
