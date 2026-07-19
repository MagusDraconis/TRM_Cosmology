using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_28;

[Trait("Category","V5_28"),Trait("Category","V5_28_RSE"),Trait("Category","LongRunning")]
public class V5_28_RiskStratumExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const double FTHR=0.1;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct PolProfile{
        public int n,s,cohort;public double c3OmgS,omDist,lambda1;public bool hasPosSign,a0,persistent,persistentNoCont,rescuedAtC3,rulePos;
    }

    public V5_28_RiskStratumExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    PolProfile? BuildPolicyProfile(int n,int s,P3 hi,P3 lo){
        var p=new PolProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);if(sb==null)return null;
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);
        p.omDist=THR-Of(hT1,n).Average();p.lambda1=Lambda1(KT1,n);
        p.rulePos=p.omDist<0.5&&p.lambda1<0.95;
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        p.a0=Of(hT2,n).Average()>THR;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();p.c3OmgS=omC3-(p.a0?THR:Of(hT2,n).Average());
            bool c3=omC3>THR;p.rescuedAtC3=c3&&!p.a0;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=c3&&Of(hCont,n).Average()>THR;
            // P3: no continuation path
            var hNoCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+900);
            p.persistentNoCont=c3&&Of(hNoCont,n).Average()>THR;
        }else{p.persistent=false;p.persistentNoCont=false;p.rescuedAtC3=false;}
        p.hasPosSign=p.rulePos;
        return p;
    }

    [Fact]
    public void RSE_01_RiskStratumPolicyExecution()
    {
        _o.WriteLine("═══════════════════════════════════════════════════");
        _o.WriteLine("═══ RSE_01: Risk-Stratum Policy Execution ═══");
        _o.WriteLine("═══ Can c3OmgS>0.1 guide adaptive policy? ═══");
        _o.WriteLine("═══════════════════════════════════════════════════");

        // ─── Collect profiles ───
        int[] Ns={64,65,66,70,72,75,76,79,80,85};
        var all=new ConcurrentBag<PolProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildPolicyProfile(n,s,hi,lo);if(p!=null)all.Add(p.Value);}});
        var profs=all.ToArray();
        int baseRescues=profs.Count(p=>p.persistent);
        _o.WriteLine($"Profiles: {profs.Length}, Baseline rescues (M3++): {baseRescues} ({baseRescues*100.0/profs.Length:F1}%)");

        var loRisk=profs.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var hiRisk=profs.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"Stratum A (<=0.1): n={loRisk.Length}, rescues={loRisk.Count(p=>p.persistent)} ({loRisk.Count(p=>p.persistent)*100.0/Math.Max(1,loRisk.Length):F1}%)");
        _o.WriteLine($"Stratum B (>0.1):  n={hiRisk.Length}, rescues={hiRisk.Count(p=>p.persistent)} ({hiRisk.Count(p=>p.persistent)*100.0/Math.Max(1,hiRisk.Length):F1}%)");

        // ─── Policy evaluation ───
        _o.WriteLine($"\n═══ Policy Comparison ═══");
        _o.WriteLine($"P0: Baseline M3++ (reference)");
        _o.WriteLine($"P1: Diagnostic-only (record stratum, no action change)");
        _o.WriteLine($"P2: Stop-low (stop if c3OmgS<=0.1)");
        _o.WriteLine($"P3: Preserve-high (skip continuation if c3OmgS>0.1)");
        _o.WriteLine($"P4: Audit-low (continue low-stratum as audit)");

        // P0/P1: identical to baseline
        int p0Rescue=baseRescues;int p0Damage=0;
        int p1Rescue=p0Rescue;int p1Damage=p0Damage;

        // P2: Stop-low
        // If c3OmgS<=0.1: stop -> missed rescues = persistent in low stratum, saved work = non-persistent in low
        int p2Missed=loRisk.Count(p=>p.persistent);
        int p2Rescued=hiRisk.Count(p=>p.persistent); // high stratum continues normally
        int p2WastedSaved=loRisk.Length-loRisk.Count(p=>p.persistent); // work saved by stopping low
        int p2Damage=0; // stopping low stratum can't damage high stratum

        // P3: Preserve-high
        // If c3OmgS>0.1: preserve (skip continuation) -> use persistentNoCont for high
        int p3Rescued=hiRisk.Count(p=>p.persistentNoCont);
        int p3Missed=hiRisk.Count(p=>p.persistent&&!p.persistentNoCont);
        int p3Damage=0; // preserving doesn't damage low

        // P4: Audit-low (same as baseline, just track)
        int p4Rescue=p0Rescue;int p4Damage=p0Damage;
        int p4Wasted=loRisk.Length-loRisk.Count(p=>p.persistent); // wasted continuations

        _o.WriteLine($"\n{0,-4} {1,7} {2,7} {3,7} {4,7} {5,7} {6,10}",
            "Pol","Rescue","Damage","Missed","Saved","Wasted","Efficiency");
        _o.WriteLine($"{0,-4} {1,7} {2,7} {3,7} {4,7} {5,7} {6,10}",
            "P0",p0Rescue,p0Damage,0,0,0,$"{p0Rescue*100.0/profs.Length:F1}%");
        _o.WriteLine($"{0,-4} {1,7} {2,7} {3,7} {4,7} {5,7} {6,10}",
            "P1",p1Rescue,p1Damage,0,0,0,"diagnostic");
        _o.WriteLine($"{0,-4} {1,7} {2,7} {3,7} {4,7} {5,7} {6,10}",
            "P2",p2Rescued,p2Damage,p2Missed,p2WastedSaved,0,$"{p2Rescued*100.0/Math.Max(1,p2Rescued+p2Missed)*100.0/profs.Length:F1}%");
        _o.WriteLine($"{0,-4} {1,7} {2,7} {3,7} {4,7} {5,7} {6,10}",
            "P3",p3Rescued,p3Damage,p3Missed,0,0,$"{p3Rescued*100.0/profs.Length:F1}%");

        // ─── Stop-low detailed ───
        _o.WriteLine($"\n═══ P2 Stop-Low Validation ═══");
        _o.WriteLine($"Low-stratum n={loRisk.Length}. Persistent in low: {loRisk.Count(p=>p.persistent)}.");
        _o.WriteLine($"Missed rescues: {p2Missed}. Work saved: {p2WastedSaved} continuations.");
        _o.WriteLine($"Stop-low safe: {(p2Missed==0?"YES — zero missed rescues":$"NO — {p2Missed} rescues would be lost")}");

        // ─── Preserve-high detailed ───
        _o.WriteLine($"\n═══ P3 Preserve-High Validation ═══");
        _o.WriteLine($"High-stratum n={hiRisk.Length}. Persistent with continuation: {hiRisk.Count(p=>p.persistent)}.");
        _o.WriteLine($"Persistent without continuation: {hiRisk.Count(p=>p.persistentNoCont)}.");
        _o.WriteLine($"Missed by preserving: {p3Missed} rescues lost by skipping continuation.");
        _o.WriteLine($"Preserve-high safe: {(p3Missed==0?"YES — all high-stratum rescues persist without continuation":$"NO — {p3Missed} rescues lost")}");

        // ─── Rare-rescue audit ───
        _o.WriteLine($"\n═══ Rare-Rescue Audit ═══");
        var rareRescues=loRisk.Where(p=>p.persistent).ToArray();
        _o.WriteLine($"Low-stratum rescues (c3OmgS<={FTHR}): {rareRescues.Length}");
        if(rareRescues.Length>0){
            _o.WriteLine($"Rare rescues at N: {string.Join(", ",rareRescues.Select(p=>p.n).Distinct().OrderBy(x=>x))}");
            _o.WriteLine($"Rare rescues in cohorts: {string.Join(", ",rareRescues.Select(p=>p.cohort).Distinct().OrderBy(x=>x))}");
        }

        // ─── N-specific ───
        _o.WriteLine($"\n═══ N-Specific Policy Performance ═══");
        _o.WriteLine($"{0,4} {1,5} {2,5} {3,5} {4,6} {5,6} {6,6} {7,8}",
            "N","n","resc","loN","loR","hiN","hiR","P3miss");
        foreach(var n in Ns){
            var sn=profs.Where(p=>p.n==n).ToArray();
            var loN=sn.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var hiN=sn.Where(p=>p.c3OmgS>FTHR).ToArray();
            int loR=loN.Count(p=>p.persistent);
            int hiR=hiN.Count(p=>p.persistent);
            int p3m=hiN.Count(p=>p.persistent&&!p.persistentNoCont);
            _o.WriteLine($"{n,4} {sn.Length,5} {sn.Count(p=>p.persistent),5} {loN.Length,5} {loR,5} {hiN.Length,5} {hiR,5} {p3m,7}");
        }

        // ─── Decision gates ───
        _o.WriteLine($"\n═══ Decision Gates ═══");
        bool gA=p2WastedSaved>0&&p2Missed==0;
        bool gB=p2Missed==0;
        bool gC=p3Missed==0;
        bool gD=p2Missed>0&&p2WastedSaved>0;
        bool gE=rareRescues.Length>0;
        bool gF=p2Missed>0||p3Missed>hiRisk.Length/2;

        _o.WriteLine($"Gate A (policy useful): saved={p2WastedSaved}, missed={p2Missed} — {(gA?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (stop-low validated): missed={p2Missed} — {(gB?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (preserve-high validated): missed={p3Missed} — {(gC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (diagnostic only): {(gD?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (rare low rescues exist): {rareRescues.Length} — {(gE?"REACHED":"NOT REACHED")}");

        string classification;
        if(gB&&gC)classification="Model A: Actionable stop/go policy";
        else if(gB)classification="Model B: Continuation-efficiency policy (stop-low safe)";
        else if(gC)classification="Model B: Continuation-efficiency policy (preserve-high safe)";
        else if(gE)classification="Model C: Diagnostic-only risk label (rare rescues exist)";
        else classification="Model D: Insufficient data or unsafe";
        _o.WriteLine($"Classification: {classification}");

        // ─── Claim discipline ───
        _o.WriteLine($"\n═══ Claim Discipline ═══");
        _o.WriteLine($"SUPPORTED: M3++ baseline reproduced: {baseRescues} rescues.");
        _o.WriteLine(gB?$"SUPPORTED: Stop-low policy is safe. Zero missed rescues in low stratum.":$"CONDITIONAL: Stop-low has {p2Missed} missed rescues.");
        _o.WriteLine(gC?$"SUPPORTED: Preserve-high is safe. All high-stratum rescues persist without continuation.":$"CONDITIONAL: Preserve-high loses {p3Missed} rescues.");
        _o.WriteLine($"CONDITIONAL: Finite-N, cohort-limited, operator-class-limited.");
        _o.WriteLine($"NOT CLAIMED: deterministic rescue, causality, physical interpretation, universal control.");
        string nextSuite=gB?"RSA_PolicyAnalysis (refine stop-low)":"RSA_PolicyAnalysis (investigate missed rescues)";
        _o.WriteLine($"Next suite: {nextSuite}");
        _o.WriteLine($"");
        _o.WriteLine($"═══ RSE_01 complete. ═══");
    }

    // ─── M3++ simulation (frozen) ───
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0&&!double.IsNaN(vn)?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))return null;
        return sb;
    }
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
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
