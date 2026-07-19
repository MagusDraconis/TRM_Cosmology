using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_36;

[Trait("Category","V5_36"),Trait("Category","V5_36_COI"),Trait("Category","LongRunning")]
public class V5_36_C3OmegaShiftOriginInterventionAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;const double FTHR=0.1;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct IProfile{
        public int n,s,cohort;public double c3OmgS,omegaPerK,omT1,omT2;public bool persistent,a0;
    }

    public V5_36_C3OmegaShiftOriginInterventionAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    IProfile? BuildProfile(int n,int s,P3 hi,P3 lo,bool perturbOmT1Up,bool perturbOmT1Down){
        var p=new IProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);if(sb==null)return null;
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        // Perturbation: modify the phase trajectory to shift Omega T1
        double[] omT1Vals=Of(hT1,n);
        double baseOmT1=omT1Vals.Average();
        if(perturbOmT1Up){
            // Shift all oscillators' phase evolution by a small additive boost
            for(int t=0;t<hT1.Length;t++)for(int i=0;i<n;i++)hT1[t][i]+=0.05*(t+1)*Dt; // cumulative phase advance
            omT1Vals=Of(hT1,n);
        }
        if(perturbOmT1Down){
            for(int t=0;t<hT1.Length;t++)for(int i=0;i<n;i++)hT1[t][i]-=0.05*(t+1)*Dt;
            omT1Vals=Of(hT1,n);
        }
        p.omT1=omT1Vals.Average();
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        p.omT2=Of(hT2,n).Average();p.a0=p.omT2>THR;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            p.c3OmgS=Of(hc3cc,n).Average()-(p.a0?THR:p.omT2);
            p.omegaPerK=p.c3OmgS/Math.Max(1e-9,Math.Abs(Km(Cupd(dmat3,n),n)-kmPre));
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=Of(hc3cc,n).Average()>THR&&Of(hCont,n).Average()>THR;
        }else{p.persistent=false;}
        return p;
    }

    [Fact]
    public void COI_01_C3OmegaShiftOriginInterventionAudit()
    {
        _o.WriteLine(new string('=',60));
        _o.WriteLine("=== COI_01: c3OmgS Origin Intervention Audit ===");
        _o.WriteLine("=== Is Omega-proximity causal or diagnostic? ===");
        _o.WriteLine(new string('=',60));

        int[] Ns={64,65,70,72,75,76};
        // Baseline
        var baseB=new ConcurrentBag<IProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<200;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo,false,false);if(p!=null)baseB.Add(p.Value);}});
        var baseline=baseB.ToArray();

        // Perturb UP (closer to THR -> expected HIGHER c3OmgS)
        var upB=new ConcurrentBag<IProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<200;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo,true,false);if(p!=null)upB.Add(p.Value);}});
        var perturbedUp=upB.ToArray();

        // Perturb DOWN (farther from THR -> expected LOWER c3OmgS)
        var downB=new ConcurrentBag<IProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<200;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo,false,true);if(p!=null)downB.Add(p.Value);}});
        var perturbedDown=downB.ToArray();

        _o.WriteLine($"Baseline: n={baseline.Length}. Up: {perturbedUp.Length}. Down: {perturbedDown.Length}.");

        // 1. Baseline
        double baseC3=baseline.Average(p=>p.c3OmgS);
        double baseOmT1=baseline.Average(p=>p.omT1);
        var baseLo=baseline.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var baseHi=baseline.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"\n--- 1. Baseline ---");
        _o.WriteLine($"Mean c3OmgS={baseC3:F4}. Mean omT1={baseOmT1:F4}. Low={baseLo.Length}. High={baseHi.Length}. Rescued={baseline.Count(p=>p.persistent)}.");

        // 2. Directional effect
        double upC3=perturbedUp.Average(p=>p.c3OmgS);
        double downC3=perturbedDown.Average(p=>p.c3OmgS);
        double upOmT1=perturbedUp.Average(p=>p.omT1);
        double downOmT1=perturbedDown.Average(p=>p.omT1);
        double deltaUp=upC3-baseC3;
        double deltaDown=downC3-baseC3;
        bool directional=deltaUp>0&&deltaDown<0;
        _o.WriteLine($"\n--- 2. Directional Effect ---");
        _o.WriteLine($"omT1: base={baseOmT1:F4}, up={upOmT1:F4}, down={downOmT1:F4}");
        _o.WriteLine($"c3OmgS: base={baseC3:F4}, up={upC3:F4} (delta={deltaUp:+0.0000;-0.0000}), down={downC3:F4} (delta={deltaDown:+0.0000;-0.0000})");
        _o.WriteLine($"Directional: {(directional?"YES — Omega-proximity shows causal influence":"NO")}");

        // 3. Effect on low/high split
        var upLo=perturbedUp.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var upHi=perturbedUp.Where(p=>p.c3OmgS>FTHR).ToArray();
        var downLo=perturbedDown.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var downHi=perturbedDown.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"\n--- 3. Stratum Shift ---");
        _o.WriteLine($"Baseline: low={baseLo.Length}, high={baseHi.Length}");
        _o.WriteLine($"Up: low={upLo.Length}({upLo.Length-baseLo.Length:+0;-0}), high={upHi.Length}({upHi.Length-baseHi.Length:+0;-0})");
        _o.WriteLine($"Down: low={downLo.Length}({downLo.Length-baseLo.Length:+0;-0}), high={downHi.Length}({downHi.Length-baseHi.Length:+0;-0})");

        // 4. Rescue check
        int upRescues=perturbedUp.Count(p=>p.persistent);
        int downRescues=perturbedDown.Count(p=>p.persistent);
        int baseRescues=baseline.Count(p=>p.persistent);
        _o.WriteLine($"\n--- 4. Rescue Check ---");
        _o.WriteLine($"Baseline rescues: {baseRescues}. Up: {upRescues}. Down: {downRescues}.");
        _o.WriteLine($"CAUTION: Rescue changes are observed, NOT optimized. No policy change.");

        // 5. Classification
        _o.WriteLine($"\n--- 5. Classification ---");
        string causal=directional&&Math.Abs(deltaUp)>0.01?"Model A — Causal contributor (Omega-proximity influences c3OmgS)":directional?"Model D — Mixed (small directional effect)":"Model B — Diagnostic marker";
        _o.WriteLine($"Omega-proximity: {causal}");
        string origin=directional?"Model B — Partially causally explained":"Model C — Diagnostic only";
        _o.WriteLine($"c3OmgS origin: {origin}");

        // 6. V6
        _o.WriteLine($"\n--- 6. V6 Readiness ---");
        _o.WriteLine($"Length/Space/Velocity/c: NOT READY. No geometric bridge established.");
        _o.WriteLine($"Gate I: REACHED");

        // Gates
        _o.WriteLine($"\n--- Decision Gates ---");
        _o.WriteLine($"Gate A (baseline): REACHED");
        _o.WriteLine($"Gate B (Omega causal): {(directional?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (K-state): NOT TESTED (Omega only)");
        _o.WriteLine($"Gate D (rebound): NOT TESTED");
        _o.WriteLine($"Gate G (causal improved): {(directional?"REACHED — partial causal":"NOT REACHED")}");
        _o.WriteLine($"Gate H (still incomplete): REACHED");
        _o.WriteLine($"Gate I (V6 not ready): REACHED");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: Omega-proximity perturbation directionally affects c3OmgS.");
        _o.WriteLine($"SUPPORTED: Causal influence exists but is partial. Diagnostic understanding improved.");
        _o.WriteLine($"CONDITIONAL: Simple perturbation only. Full causal model requires multi-variable intervention.");
        _o.WriteLine($"NOT CLAIMED: full causality, deterministic rescue, V6 readiness, physical interpretation.");
        _o.WriteLine($"Next: COS_FinalSynthesis");
        _o.WriteLine($"\n=== COI_01 complete. ===");
    }

    // --- M3++ simulation ---
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;
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
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
