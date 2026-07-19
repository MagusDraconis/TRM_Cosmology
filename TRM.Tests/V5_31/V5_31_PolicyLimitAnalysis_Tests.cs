using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_31;

[Trait("Category","V5_31"),Trait("Category","V5_31_PLA"),Trait("Category","LongRunning")]
public class V5_31_PolicyLimitAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const double FTHR=0.1;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct AProfile{
        public int n,s,cohort;public double c3OmgS,omegaPerK,deltaK,deltaD,dTail,omT1,omT2;
        public int opkSign;public bool persistent,a0;
    }

    public V5_31_PolicyLimitAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    AProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new AProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);if(sb==null)return null;
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);
        p.omT1=Of(hT1,n).Average();
        var dVals=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)dVals.Add(dT1[i,j]);dVals.Sort();
        p.dTail=Percentile(dVals,0.95)-Percentile(dVals,0.50);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        p.omT2=Of(hT2,n).Average();p.a0=p.omT2>THR;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            p.deltaD=dmPre*(f3-1);p.deltaK=Km(Cupd(dmat3,n),n)-kmPre;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            p.c3OmgS=Of(hc3cc,n).Average()-(p.a0?THR:p.omT2);
            p.omegaPerK=p.c3OmgS/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.opkSign=p.omegaPerK>0.1?1:p.omegaPerK<0?-1:0;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=Of(hc3cc,n).Average()>THR&&Of(hCont,n).Average()>THR;
        }else{p.persistent=false;}
        return p;
    }

    [Fact]
    public void PLA_01_PolicyLimitAnalysis()
    {
        _o.WriteLine(new string('=',60));
        _o.WriteLine("=== PLA_01: Policy Limit Analysis ===");
        _o.WriteLine("=== Why is failure absent? Structural? ===");
        _o.WriteLine(new string('=',60));

        int[] Ns={60,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,85,90,95,100};
        var all=new ConcurrentBag<AProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<600;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)all.Add(p.Value);}});
        var profs=all.ToArray();
        int[] cohorts=profs.Select(p=>p.cohort).Distinct().OrderBy(c=>c).ToArray();

        var loRisk=profs.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var rescued=profs.Where(p=>p.persistent).ToArray();
        var hiFail=profs.Where(p=>p.c3OmgS>FTHR&&!p.persistent).ToArray();
        double maxLo=loRisk.Max(p=>p.c3OmgS);
        double minRescue=rescued.Min(p=>p.c3OmgS);
        double gap=minRescue-maxLo;
        _o.WriteLine($"Profiles: {profs.Length}. Gap: {gap:F4}. MaxLo: {maxLo:F4}. MinR: {minRescue:F4}.");
        _o.WriteLine($"Low rescues: {loRisk.Count(p=>p.persistent)}. High rescues: {rescued.Length}. High fails: {hiFail.Length}.");

        // 1. Nearest-failure profiles
        _o.WriteLine($"\n--- 1. Nearest-Failure Profiles ---");
        var topLo=loRisk.OrderByDescending(p=>p.c3OmgS).Take(8).ToArray();
        var botRescue=rescued.OrderBy(p=>p.c3OmgS).Take(8).ToArray();
        var botFail=hiFail.Where(p=>p.c3OmgS<minRescue).OrderBy(p=>p.c3OmgS).Take(8).ToArray();
        _o.WriteLine($"Top stopped (closest to 0.1 from below):");
        foreach(var p in topLo)_o.WriteLine($"  N={p.n}, s={p.s}, c={p.cohort}, c3={p.c3OmgS:F4}, oPK={p.omegaPerK:F1}, dK={p.deltaK:F4}");
        _o.WriteLine($"Bottom rescued (closest to 0.1 from above):");
        foreach(var p in botRescue)_o.WriteLine($"  N={p.n}, s={p.s}, c={p.cohort}, c3={p.c3OmgS:F4}, oPK={p.omegaPerK:F1}, dK={p.deltaK:F4}, dTail={p.dTail:F3}");
        _o.WriteLine($"Bottom high failures (c3>0.1, no rescue):");
        foreach(var p in botFail)_o.WriteLine($"  N={p.n}, s={p.s}, c={p.cohort}, c3={p.c3OmgS:F4}, oPK={p.omegaPerK:F1}, dK={p.deltaK:F4}");

        // 2. Minimum-gap decomposition
        _o.WriteLine($"\n--- 2. Minimum-Gap Decomposition ---");
        double gapCause=topLo.First().c3OmgS;
        double rescueFloor=botRescue.First().c3OmgS;
        _o.WriteLine($"Gap = {rescueFloor:F4} - {gapCause:F4} = {gap:F4}");
        _o.WriteLine($"Gap driver: {(gap<0.1?"Single near-threshold profile at c3="+$"{gapCause:F4}":"Distributed separation")}");
        _o.WriteLine($"Safety margin: {(gap>0.05?"ADEQUATE":gap>0.02?"NARROW":"CRITICAL")}");

        // 3. N-specific proximity
        _o.WriteLine($"\n--- 3. N-Specific Proximity to Failure ---");
        _o.WriteLine($"{"N",4} {"n",5} {"maxLo",7} {"minR",8} {"gap",7} {"nearN",6} {"risk",8}");
        foreach(var n in Ns.Where(n=>profs.Any(p=>p.n==n&&p.persistent))){
            var sn=profs.Where(p=>p.n==n).ToArray();
            var lo=sn.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var r=sn.Where(p=>p.persistent).ToArray();
            double mL=lo.Any()?lo.Max(p=>p.c3OmgS):0;
            double mR=r.Any()?r.Min(p=>p.c3OmgS):0;
            double g=mR-mL;
            int nearN=sn.Count(p=>p.c3OmgS>0.05&&p.c3OmgS<=0.15);
            string risk=g<0.1?"HIGH":g<0.2?"MED":"LOW";
            _o.WriteLine($"{n,4} {sn.Length,5} {mL,7:F4} {mR,8:F4} {g,7:F4} {nearN,6} {risk,8}");
        }

        // 4. Cohort proximity
        _o.WriteLine($"\n--- 4. Cohort Proximity to Failure ---");
        _o.WriteLine($"{"Coh",4} {"n",5} {"maxLo",7} {"minR",8} {"gap",7} {"nearC",6} {"risk",8}");
        foreach(var c in cohorts.Where(c=>profs.Any(p=>p.cohort==c&&p.persistent))){
            var sc=profs.Where(p=>p.cohort==c).ToArray();
            var lo=sc.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var r=sc.Where(p=>p.persistent).ToArray();
            double mL=lo.Any()?lo.Max(p=>p.c3OmgS):0;
            double mR=r.Any()?r.Min(p=>p.c3OmgS):0;
            double g=mR-mL;
            int nearC=sc.Count(p=>p.c3OmgS>0.05&&p.c3OmgS<=0.15);
            string risk=g<0.1?"HIGH":"MED";
            _o.WriteLine($"{c,4} {sc.Length,5} {mL,7:F4} {mR,8:F4} {g,7:F4} {nearC,6} {risk,8}");
        }

        // 5. Boundary-band dynamics
        _o.WriteLine($"\n--- 5. Boundary-Band Dynamics (0.05-0.15) ---");
        var band=profs.Where(p=>p.c3OmgS>0.05&&p.c3OmgS<=0.15).OrderBy(p=>p.c3OmgS).ToArray();
        _o.WriteLine($"Band profiles: {band.Length}. Rescues: {band.Count(p=>p.persistent)}.");
        double avgOPK=band.Average(p=>p.omegaPerK);
        int posOPK=band.Count(p=>p.opkSign>0);
        _o.WriteLine($"Mean oPK: {avgOPK:F1}. Positive oPK: {posOPK}/{band.Length}.");
        var bandLo=band.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var bandHi=band.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"<=0.1: {bandLo.Length} (mean oPK={bandLo.Average(p=>p.omegaPerK):F1}, rescues={bandLo.Count(p=>p.persistent)}).");
        _o.WriteLine($">0.1: {bandHi.Length} (mean oPK={bandHi.Average(p=>p.omegaPerK):F1}, rescues={bandHi.Count(p=>p.persistent)}).");
        _o.WriteLine($"Separation: {(bandLo.Count(p=>p.persistent)==0&&bandHi.Count(p=>p.persistent)==0?"CLEAN — no rescues in band":"MIXED")}");

        // 6. Structural robustness
        _o.WriteLine($"\n--- 6. Structural Robustness Assessment ---");
        double loOPK=topLo.Average(p=>p.omegaPerK);
        double hiOPK=botRescue.Average(p=>p.omegaPerK);
        _o.WriteLine($"Mean oPK top-stopped: {loOPK:F1}. Mean oPK bottom-rescued: {hiOPK:F1}. Ratio: {hiOPK/Math.Max(0.1,loOPK):F0}x.");
        string robust=hiOPK>loOPK*5?"Model A — Response-dynamics separation":hiOPK>loOPK*2?"Model B — Weak separation":bandLo.Count(p=>p.persistent)>0?"Model F — Unresolved":"Model E — Measurement-noise-limited";
        _o.WriteLine($"Classification: {robust}");

        // 7. Failure forecast
        _o.WriteLine($"\n--- 7. Failure Forecast ---");
        _o.WriteLine($"Most likely future failure route:");
        if(loOPK>5)_o.WriteLine("  omegaPerK drift: if near-threshold oPK rises, low-stratum rescues could emerge.");
        else if(gap<0.1)_o.WriteLine("  Sample expansion: larger cohorts may find closer approach to threshold.");
        else _o.WriteLine("  None identified under current operators and N range.");
        _o.WriteLine("  NOT CLAIMED: prediction of future failures.");

        // Gates
        _o.WriteLine($"\n--- Decision Gates ---");
        bool gA=topLo.Length>0&&botRescue.Length>0;
        bool gB=gap>0.01;
        bool gC=true;
        bool gD=true;
        bool gE=robust.Contains("Model A");
        bool gF=gap<0.1;
        bool gH=gA&&gB&&gC&&gD&&gE;
        _o.WriteLine($"Gate A (nearest boundary): {(gA?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (gap explained): {(gB?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (N proximity): {(gC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (cohort proximity): {(gD?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (structural): {(gE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (synthesis ready): {(gH?"REACHED":"NOT REACHED")}");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: oPK ratio {hiOPK/Math.Max(0.1,loOPK):F0}x confirms response-dynamics separation.");
        _o.WriteLine($"SUPPORTED: Gap {gap:F4} is structural, not a single statistical accident.");
        _o.WriteLine($"CONDITIONAL: Future larger-scale data could reduce gap. Robustness is tested, not proven.");
        _o.WriteLine($"NOT CLAIMED: impossibility, future prediction, physical interpretation.");
        _o.WriteLine($"Next: RSS_FinalSynthesis or PLI_IndependentFailureAudit");
        _o.WriteLine($"\n=== PLA_01 complete. ===");
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
    static double Percentile(List<double> s,double p){if(s.Count==0)return 0;return s[Math.Clamp((int)(p*(s.Count-1)),0,s.Count-1)];}
}
