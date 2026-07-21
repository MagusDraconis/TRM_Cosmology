using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_57;

[Trait("Category","V5_57"),Trait("Category","V5_57_PIPE"),Trait("Category","LongRunning")]
public class V5_57_DiscriminationOriginAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_57_DiscriminationOriginAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void PIPE_01_DiscriminationOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== PIPE_01: Discrimination Origin Audit ===");
        _o.WriteLine("=== V5.57. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Where does the discriminator first appear? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var stageBag=new ConcurrentBag<(int N,int seed,double riqr,double rmean,string stage,string outcome)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                // Stage 0: Generation
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                var wo=w.OrderBy(v=>v).ToArray();
                double ri=Q(wo,0.75)-Q(wo,0.25),rm=wo.Average();
                stageBag.Add((n,s,ri,rm,"gen","all"));

                // Stage 1: IsHi
                bool ih=IsHi(n,s);
                if(ih)stageBag.Add((n,s,ri,rm,"IsHi","pass"));
                else{stageBag.Add((n,s,ri,rm,"IsHi","fail"));return;}

                // Stage 2: SAC
                var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                stageBag.Add((n,s,ri,rm,"SAC",cls));
            });});
        var sd=stageBag.ToArray();

        // ============================================================
        // PART A+B — Stage-by-stage tracing
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: Stage-by-Stage Tracing ===");
        _o.WriteLine($"{"Stage",-8} {"Outcome",-10} {"n",6} {"rawIQR",10} {"rawMean",10} {"IsHi%",7} {"SAC%",7}");
        _o.WriteLine(new string('-',65));

        foreach(var(stage,outcome)in new[]{("gen","all"),("IsHi","pass"),("IsHi","fail"),("SAC","P1"),("SAC","P1b"),("SAC","reject")}){
            var ss=sd.Where(d=>d.stage==stage&&d.outcome==outcome).ToArray();
            if(ss.Length<1)continue;
            double ttl=sd.Count(d=>d.stage=="gen");
            _o.WriteLine($"{stage,-8} {outcome,-10} {ss.Length,6} {ss.Average(d=>d.riqr),10:F5} {ss.Average(d=>d.rmean),10:F5} {ss.Length*100.0/ttl,7:F1}%");
        }

        // Separation metrics
        var gen=sd.Where(d=>d.stage=="gen").ToArray();
        var ihP=sd.Where(d=>d.stage=="IsHi"&&d.outcome=="pass").ToArray();
        var ihF=sd.Where(d=>d.stage=="IsHi"&&d.outcome=="fail").ToArray();
        var sacP1=sd.Where(d=>d.stage=="SAC"&&d.outcome=="P1").ToArray();
        var sacP1b=sd.Where(d=>d.stage=="SAC"&&d.outcome=="P1b").ToArray();
        var sacRej=sd.Where(d=>d.stage=="SAC"&&d.outcome=="reject").ToArray();

        _o.WriteLine($"\nSeparation chain:");
        double ihIqrDelta=Math.Abs(ihP.Average(d=>d.riqr)-ihF.Average(d=>d.riqr));
        double ihMeanDelta=Math.Abs(ihP.Average(d=>d.rmean)-ihF.Average(d=>d.rmean));
        double sacP1IqrDelta=Math.Abs(sacP1.DefaultIfEmpty().Average(d=>d.riqr)-sacP1b.DefaultIfEmpty().Average(d=>d.riqr));
        double sacP1MeanDelta=Math.Abs(sacP1.DefaultIfEmpty().Average(d=>d.rmean)-sacP1b.DefaultIfEmpty().Average(d=>d.rmean));
        double sacRetIqrDelta=Math.Abs((sacP1.Concat(sacP1b)).DefaultIfEmpty().Average(d=>d.riqr)-sacRej.Average(d=>d.riqr));

        _o.WriteLine($"IsHi gate: rawIQR delta={ihIqrDelta:F5}, rawMean delta={ihMeanDelta:F5} → {(ihIqrDelta>0.002||ihMeanDelta>0.002?"GATE CREATES SEPARATION":"GATE IS NEUTRAL")}");
        _o.WriteLine($"SAC P1→P1b: rawIQR delta={sacP1IqrDelta:F5}, rawMean delta={sacP1MeanDelta:F5}");
        _o.WriteLine($"SAC ret→rej: rawIQR delta={sacRetIqrDelta:F5}");
        _o.WriteLine($"SAC gating: {(sacRetIqrDelta>0.002?"CREATES separation":"PASSES separation through")}");

        // ============================================================
        // PART C — Transformation audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Transformation Audit ===");
        // Track how descriptors change through stages
        double genIqrStd=Sd(gen.Select(d=>d.riqr).ToArray());
        double ihIqrStd=Sd(sd.Where(d=>d.stage=="IsHi").Select(d=>d.riqr).ToArray());
        double sacIqrStd=Sd(sd.Where(d=>d.stage=="SAC").Select(d=>d.riqr).ToArray());
        double genMeanStd=Sd(gen.Select(d=>d.rmean).ToArray());
        double ihMeanStd=Sd(sd.Where(d=>d.stage=="IsHi").Select(d=>d.rmean).ToArray());
        double sacMeanStd=Sd(sd.Where(d=>d.stage=="SAC").Select(d=>d.rmean).ToArray());

        _o.WriteLine($"rawIQR std: gen={genIqrStd:F5} → IsHi={ihIqrStd:F5} ({(ihIqrStd/genIqrStd-1)*100:+0.#;-0.#}%) → SAC={sacIqrStd:F5} ({(sacIqrStd/genIqrStd-1)*100:+0.#;-0.#}%)");
        _o.WriteLine($"rawMean std: gen={genMeanStd:F5} → IsHi={ihMeanStd:F5} → SAC={sacMeanStd:F5}");
        _o.WriteLine($"Information: {(sacIqrStd/genIqrStd>1.05?"AMPLIFIED":sacIqrStd/genIqrStd<0.95?"REDUCED":"PRESERVED")} through pipeline");

        // ============================================================
        // PART D — Counterfactual stage removal
        // ============================================================
        _o.WriteLine($"\n=== PART D: Counterfactual Stage Removal ===");
        // If IsHi didn't exist: compare all profiles vs SAC outcomes
        _o.WriteLine($"Without IsHi: all {gen.Length} profiles would enter SAC directly.");
        _o.WriteLine($"Without SAC: IsHi-pass {ihP.Length} profiles would be the final retained set.");
        double onlyIsHi=Math.Abs(ihP.Average(d=>d.riqr)-ihF.Average(d=>d.riqr));
        _o.WriteLine($"IsHi-only discrimination: rawIQR delta={onlyIsHi:F5} → {(onlyIsHi>0.002?"IsHi ALONE discriminates":"IsHi alone does NOT discriminate")}");
        _o.WriteLine($"SAC contribution: {(sacRetIqrDelta>onlyIsHi*2?"SAC is the PRIMARY discriminator":"SAC refines IsHi")}");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");
        var rng2=new Random(42);int sacWins=0,isHiWins=0;
        for(int sp=0;sp<50;sp++){
            var shuf=sd.Where(d=>d.stage=="SAC").OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            double s1sac=Math.Abs(s1.Where(d=>d.outcome=="P1").DefaultIfEmpty().Average(d=>d.riqr)-s1.Where(d=>d.outcome=="P1b").DefaultIfEmpty().Average(d=>d.riqr));
            if(s1sac>0.002)sacWins++;
        }
        _o.WriteLine($"SAC discrimination stable: {sacWins}/50 splits");
        _o.WriteLine($"Gate location: {(sacWins>25?"STABLE at SAC":"UNSTABLE")}");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(ihIqrDelta<0.001&&sacRetIqrDelta>0.002)decision="Model B: SAC-created origin. IsHi is neutral; SAC introduces the main discrimination.";
        else if(ihIqrDelta>0.001&&sacRetIqrDelta>ihIqrDelta*2)decision="Model C: Multi-stage origin. IsHi starts separation; SAC amplifies.";
        else if(ihIqrDelta>sacRetIqrDelta)decision="Model C: IsHi is the primary discriminator; SAC passes through.";
        else decision="Model D: Unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: IsHi delta={ihIqrDelta:F5}, SAC ret-rej delta={sacRetIqrDelta:F5}, P1-P1b delta={sacP1IqrDelta:F5}");
        _o.WriteLine("CLAIMS: Origin traced. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== PIPE_01 complete. Commit: PIPE_01_DiscriminationOriginAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
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
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
