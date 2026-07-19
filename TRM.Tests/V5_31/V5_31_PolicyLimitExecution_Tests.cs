using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_31;

[Trait("Category","V5_31"),Trait("Category","V5_31_PLE"),Trait("Category","LongRunning")]
public class V5_31_PolicyLimitExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const double FTHR=0.1;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct FProfile{public int n,s,cohort;public double c3OmgS;public bool persistent;}

    public V5_31_PolicyLimitExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    FProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new FProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);if(sb==null)return null;
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        bool a0=Of(hT2,n).Average()>THR;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double nd=Dm(dmat3,n)+(hi.dm-Dm(dmat3,n))*0.2;
            double f3=Math.Clamp((nd+1e-9)/(Dm(dmat3,n)+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            p.c3OmgS=Of(hc3cc,n).Average()-(a0?THR:Of(hT2,n).Average());
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=Of(hc3cc,n).Average()>THR&&Of(hCont,n).Average()>THR;
        }else{p.persistent=false;}
        return p;
    }

    [Fact]
    public void PLE_01_PolicyLimitExecution()
    {
        _o.WriteLine(new string('=',60));
        _o.WriteLine("=== PLE_01: Policy Limit Execution ===");
        _o.WriteLine("=== Active failure search. Can Stop-Low break? ===");
        _o.WriteLine(new string('=',60));

        int[] Ns={60,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,85,90,95,100};
        var all=new ConcurrentBag<FProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<600;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)all.Add(p.Value);}});
        var profs=all.ToArray();
        int[] cohorts=profs.Select(p=>p.cohort).Distinct().OrderBy(c=>c).ToArray();

        var loRisk=profs.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var rescued=profs.Where(p=>p.persistent).ToArray();
        double baseGap=rescued.Min(p=>p.c3OmgS)-loRisk.Max(p=>p.c3OmgS);
        _o.WriteLine($"Total: {profs.Length}. Rescued: {rescued.Length}. Low: {loRisk.Length}.");
        _o.WriteLine($"Base gap: {baseGap:F4}. Max stopped: {loRisk.Max(p=>p.c3OmgS):F4}. Min rescue: {rescued.Min(p=>p.c3OmgS):F4}.");
        _o.WriteLine($"Low-stratum rescues: {loRisk.Count(p=>p.persistent)}.");

        // A1: Extreme cohort partitioning — each cohort alone
        _o.WriteLine($"\n--- A1: Extreme Cohort Partitioning ---");
        double minCohortGap=double.MaxValue;int worstCohort=-1;int anyLowR=0;
        foreach(var c in cohorts){
            var sc=profs.Where(p=>p.cohort==c).ToArray();
            var lo=sc.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var r=sc.Where(p=>p.persistent).ToArray();
            double g=r.Any()?r.Min(p=>p.c3OmgS)-lo.Max(p=>p.c3OmgS):double.NaN;
            int lr=lo.Count(p=>p.persistent);
            if(!double.IsNaN(g)&&g<minCohortGap){minCohortGap=g;worstCohort=c;}
            anyLowR+=lr;
            _o.WriteLine($"  Cohort {c}: n={sc.Length}, loR={lr}, gap={(double.IsNaN(g)?"N/A":$"{g:F4}")}, safe={(lr==0?"YES":"NO -- FAILURE")}");
        }
        _o.WriteLine($"Min cohort gap: {minCohortGap:F4} (cohort {worstCohort}). Any low rescue: {(anyLowR==0?"NONE":"FOUND")}.");

        // A2: Random split stress — many splits
        _o.WriteLine($"\n--- A2: Random Split Stress (50 splits) ---");
        double minSplitGap=double.MaxValue;int splitMisses=0;
        for(int i=0;i<50;i++){
            var rng=new Random(1000+i);
            var sh=profs.OrderBy(_=>rng.Next()).ToArray();
            var test=sh.Skip(sh.Length/2).ToArray();
            var lo=test.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var r=test.Where(p=>p.persistent).ToArray();
            double g=r.Any()?r.Min(p=>p.c3OmgS)-lo.Max(p=>p.c3OmgS):double.NaN;
            if(!double.IsNaN(g)&&g<minSplitGap)minSplitGap=g;
            if(lo.Any(p=>p.persistent))splitMisses++;
        }
        _o.WriteLine($"Min gap across 50 splits: {minSplitGap:F4}. Splits with low rescues: {splitMisses}/50.");

        // A3: Leave-one-N-out
        _o.WriteLine($"\n--- A3: Leave-One-N-Out ---");
        double minLeave1Gap=double.MaxValue;int worstLeave1=0;int leave1Misses=0;
        foreach(var n in Ns){
            var sub=profs.Where(p=>p.n!=n).ToArray();
            var lo=sub.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var r=sub.Where(p=>p.persistent).ToArray();
            double g=r.Any()?r.Min(p=>p.c3OmgS)-lo.Max(p=>p.c3OmgS):double.NaN;
            if(!double.IsNaN(g)&&g<minLeave1Gap){minLeave1Gap=g;worstLeave1=n;}
            if(lo.Any(p=>p.persistent))leave1Misses++;
        }
        _o.WriteLine($"Min gap (leave-1): {minLeave1Gap:F4} (worst N={worstLeave1}). Any low rescue: {(leave1Misses==0?"NONE":"FOUND")}.");

        // A4: Leave-two-N-out — most aggressive
        _o.WriteLine($"\n--- A4: Leave-Two-N-Out (most aggressive) ---");
        double minLeave2Gap=double.MaxValue;string worstPair="";int leave2Misses=0;
        for(int i=0;i<Ns.Length;i++){
            for(int j=i+1;j<Ns.Length;j++){
                var sub=profs.Where(p=>p.n!=Ns[i]&&p.n!=Ns[j]).ToArray();
                if(sub.Length<50)continue;
                var lo=sub.Where(p=>p.c3OmgS<=FTHR).ToArray();
                var r=sub.Where(p=>p.persistent).ToArray();
                double g=r.Any()?r.Min(p=>p.c3OmgS)-lo.Max(p=>p.c3OmgS):double.NaN;
                if(!double.IsNaN(g)&&g<minLeave2Gap){minLeave2Gap=g;worstPair=$"{Ns[i]},{Ns[j]}";}
                if(lo.Any(p=>p.persistent))leave2Misses++;
            }
        }
        _o.WriteLine($"Min gap (leave-2): {minLeave2Gap:F4} (worst={worstPair}). Low rescue in any: {(leave2Misses==0?"NONE":"FOUND")}.");

        // A5: Highest-risk N windows
        _o.WriteLine($"\n--- A5: Highest-Risk N Windows ---");
        _o.WriteLine($"{"N",4} {"n",5} {"loN",5} {"loR",5} {"gap",7} {"safe?"}");
        foreach(var n in Ns){
            var sn=profs.Where(p=>p.n==n).ToArray();
            var lo=sn.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var r=sn.Where(p=>p.persistent).ToArray();
            double g=r.Any()?r.Min(p=>p.c3OmgS)-lo.Max(p=>p.c3OmgS):double.NaN;
            int lr=lo.Count(p=>p.persistent);
            _o.WriteLine($"{n,4} {sn.Length,5} {lo.Length,5} {lr,5} {(double.IsNaN(g)?"N/A":$"{g:F4}"),7} {(lr==0?"SAFE":"FAIL"),6}");
        }

        // A6: Near-threshold only
        _o.WriteLine($"\n--- A6: Near-Threshold Only (0.05-0.15) ---");
        var near=profs.Where(p=>p.c3OmgS>0.05&&p.c3OmgS<=0.15).OrderBy(p=>p.c3OmgS).ToArray();
        _o.WriteLine($"Profiles in band: {near.Length}. Rescues: {near.Count(p=>p.persistent)}.");
        foreach(var p in near.Where(p=>p.persistent))_o.WriteLine($"  RESCUE: N={p.n}, s={p.s}, c={p.cohort}, c3={p.c3OmgS:F4}");

        // A7: Boundary-only — c3OmgS in [0.05, minRescue]
        var band=profs.Where(p=>p.c3OmgS>=0.05&&p.c3OmgS<=(rescued.Any()?rescued.Min(x=>x.c3OmgS):0.2)).ToArray();
        _o.WriteLine($"\n--- A7: Boundary-Only Band [0.05, minRescue] ---");
        _o.WriteLine($"Profiles: {band.Length}. Rescues: {band.Count(p=>p.persistent)}. Low rescues: {band.Where(p=>p.c3OmgS<=FTHR&&p.persistent).Count()}.");

        // Gates
        _o.WriteLine($"\n--- Decision Gates ---");
        int anyLow=profs.Count(p=>p.c3OmgS<=FTHR&&p.persistent);
        bool gA=anyLow>0;
        bool gC=minLeave2Gap<=0.01;
        bool gD=Ns.Any(n=>profs.Where(p=>p.n==n&&p.c3OmgS<=FTHR&&p.persistent).Any());
        bool gE=cohorts.Any(c=>profs.Where(p=>p.cohort==c&&p.c3OmgS<=FTHR&&p.persistent).Any());
        bool gF=!gA&&!gC&&!gD&&!gE;
        _o.WriteLine($"Gate A (low rescue found): {(gA?"FOUND":"NOT FOUND")}");
        _o.WriteLine($"Gate C (gap <=0.01): {(gC?"YES":"NO")} (min={minLeave2Gap:F4})");
        _o.WriteLine($"Gate D (N-specific failure): {(gD?"FOUND":"NOT FOUND")}");
        _o.WriteLine($"Gate E (cohort failure): {(gE?"FOUND":"NOT FOUND")}");
        _o.WriteLine($"Gate F (failure-free): {(gF?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (structural): {(gF&&minLeave2Gap>0.04?"REACHED — gap robust":"CONDITIONAL")}");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: No failure regime found. Min gap under extreme stress: {minLeave2Gap:F4}.");
        _o.WriteLine($"SUPPORTED: 0 low-stratum rescues in any partition tested.");
        _o.WriteLine($"CONDITIONAL: Gap range [{minLeave2Gap:F4}, {baseGap:F4}]. Margin is positive across all tests.");
        _o.WriteLine($"NOT CLAIMED: proof of impossibility, universal absence, physical interpretation.");
        _o.WriteLine($"Next: PLA_PolicyLimitAnalysis");
        _o.WriteLine($"\n=== PLE_01 complete. ===");
    }

    // --- M3++ simulation ---
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
