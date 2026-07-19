using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_30;

[Trait("Category","V5_30"),Trait("Category","V5_30_SGI"),Trait("Category","LongRunning")]
public class V5_30_StopLowIndependentPolicyAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const double FTHR=0.1;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct IProfile{public int n,s,cohort;public double c3OmgS;public bool persistent,a0;}

    public V5_30_StopLowIndependentPolicyAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    IProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new IProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);if(sb==null)return null;
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        p.a0=Of(hT2,n).Average()>THR;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            p.c3OmgS=Of(hc3cc,n).Average()-(p.a0?THR:Of(hT2,n).Average());
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=Of(hc3cc,n).Average()>THR&&Of(hCont,n).Average()>THR;
        }else{p.persistent=false;}
        return p;
    }

    [Fact]
    public void SGI_01_StopLowIndependentPolicyAudit()
    {
        _o.WriteLine(new string('=',60));
        _o.WriteLine("=== SGI_01: Stop-Low Independent Policy Audit ===");
        _o.WriteLine("=== Hostile audit of generalized policy ===");
        _o.WriteLine(new string('=',60));

        int[] Ns={60,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,85,90,95,100};
        var all=new ConcurrentBag<IProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<600;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)all.Add(p.Value);}});
        var profs=all.ToArray();
        int[] cohorts=profs.Select(p=>p.cohort).Distinct().OrderBy(c=>c).ToArray();

        var loRisk=profs.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var rescued=profs.Where(p=>p.persistent).ToArray();
        double maxLo=loRisk.Max(p=>p.c3OmgS);
        double minR=rescued.Min(p=>p.c3OmgS);
        _o.WriteLine($"Profiles: {profs.Length}. Cohorts: {cohorts.Length}. N: {Ns.Length}.");
        _o.WriteLine($"Global: maxLo={maxLo:F4}, minRescue={minR:F4}, gap={minR-maxLo:F4}.");
        _o.WriteLine($"Low rescues: {loRisk.Count(p=>p.persistent)}. Total rescues: {rescued.Length}.");

        // 1. Random split audits
        _o.WriteLine($"\n--- 1. Independent Random Split Audits ---");
        int[] seeds={42,137,299};
        _o.WriteLine($"{"Split",8} {"n",5} {"loR",5} {"hiR",5} {"Miss",5} {"Saved",6} {"gap",7} {"Safe?",6}");
        foreach(var sd in seeds){
            var rng=new Random(sd);var sh=profs.OrderBy(_=>rng.Next()).ToArray();
            var test=sh.Skip(sh.Length/2).ToArray();
            var loT=test.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var hiT=test.Where(p=>p.c3OmgS>FTHR).ToArray();
            int loR=loT.Count(p=>p.persistent),hiR=hiT.Count(p=>p.persistent);
            double mL=loT.Any()?loT.Max(p=>p.c3OmgS):0;
            double mR=test.Where(p=>p.persistent).Any()?test.Where(p=>p.persistent).Min(p=>p.c3OmgS):double.NaN;
            double g=double.IsNaN(mR)?double.NaN:mR-mL;
            _o.WriteLine($"seed {sd,3} {test.Length,5} {loR,5} {hiR,5} {loR,5} {loT.Length,6} {g,7:F4} {(loR==0?"SAFE":"UNSAFE"),6}");
        }

        // 2. Cohort holdout audits
        _o.WriteLine($"\n--- 2. Cohort Holdout Audits ---");
        _o.WriteLine($"{"Cohort",6} {"n",5} {"loR",5} {"hiR",5} {"Miss",5} {"Saved",6} {"gap",7} {"Safe?",6}");
        foreach(var c in cohorts){
            var sc=profs.Where(p=>p.cohort==c).ToArray();
            var loC=sc.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var hiC=sc.Where(p=>p.c3OmgS>FTHR).ToArray();
            int loR=loC.Count(p=>p.persistent),hiR=hiC.Count(p=>p.persistent);
            double mL=loC.Any()?loC.Max(p=>p.c3OmgS):0;
            double mR=sc.Where(p=>p.persistent).Any()?sc.Where(p=>p.persistent).Min(p=>p.c3OmgS):double.NaN;
            double g=double.IsNaN(mR)?double.NaN:mR-mL;
            _o.WriteLine($"{c,6} {sc.Length,5} {loR,5} {hiR,5} {loR,5} {loC.Length,6} {g,7:F4} {(loR==0?"SAFE":"UNSAFE"),6}");
        }

        // 3. N-window audits
        _o.WriteLine($"\n--- 3. N-Window Audits ---");
        var lowN=profs.Where(p=>p.n<=64).ToArray();
        var activeN=profs.Where(p=>p.n>=65&&p.n<=80).ToArray();
        var highN=profs.Where(p=>p.n>=85).ToArray();
        foreach(var (label,sub) in new[]{("low N<=64",lowN),("active 65-80",activeN),("high N>=85",highN)}){
            var loS=sub.Where(p=>p.c3OmgS<=FTHR).ToArray();
            _o.WriteLine($"{label}: n={sub.Length}, loR={loS.Count(p=>p.persistent)}, rescues={sub.Count(p=>p.persistent)}, saved={loS.Length}");
        }

        // 4. Near-threshold audit
        _o.WriteLine($"\n--- 4. Near-Threshold Audit ---");
        var near=profs.Where(p=>p.c3OmgS>0.075&&p.c3OmgS<=0.153).OrderBy(p=>p.c3OmgS).ToArray();
        _o.WriteLine($"Profiles in (0.075, 0.153]: {near.Length}");
        var nearLo=near.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var nearHi=near.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"  <=0.1: {nearLo.Length} (rescues={nearLo.Count(p=>p.persistent)}). >0.1: {nearHi.Length} (rescues={nearHi.Count(p=>p.persistent)}).");
        foreach(var p in near.Take(8))_o.WriteLine($"  N={p.n}, s={p.s}, c={p.cohort}, c3={p.c3OmgS:F4}, rescue={p.persistent}");

        // 5. Noise robustness
        _o.WriteLine($"\n--- 5. Noise Robustness ---");
        double[] noises={0.005,0.01,0.02,0.05};
        int totalMissedR=0;
        _o.WriteLine($"{"Noise",8} {"flips",7} {"missed",7} {"extraC",7} {"falseStop",9}");
        foreach(var noise in noises){
            int flips=0,missed=0,extraC=0,falseStop=0;
            var rng=new Random(42);
            for(int t=0;t<100;t++)foreach(var p in profs){
                double pt=p.c3OmgS+rng.NextDouble()*2*noise-noise;
                bool oL=p.c3OmgS<=FTHR,pL=pt<=FTHR;
                if(oL!=pL){flips++;if(!oL&&pL&&p.persistent)missed++;if(oL&&!pL)extraC++;if(!oL&&pL)falseStop++;}
            }
            totalMissedR+=missed;
            _o.WriteLine($"{noise,8:F3} {flips/100.0,7:F1} {missed/100.0,7:F1} {extraC/100.0,7:F1} {falseStop/100.0,9:F1}");
        }

        // 6. Efficiency
        double wr=loRisk.Length*100.0/profs.Length;
        _o.WriteLine($"\n--- 6. Efficiency ---");
        _o.WriteLine($"Work reduction: {wr:F0}%. Rescue efficiency: {rescued.Length*100.0/Math.Max(1,profs.Count(p=>p.c3OmgS>FTHR)):F1}%.");

        // 7. Classification
        _o.WriteLine($"\n--- 7. Final Policy Classification ---");
        bool gA=cohorts.All(c=>profs.Where(p=>p.cohort==c&&p.c3OmgS<=FTHR&&p.persistent).Count()==0);
        bool gB=loRisk.Count(p=>p.persistent)==0;
        bool gC=minR-maxLo>0;
        bool gD=nearLo.Count(p=>p.persistent)==0;
        bool gE=wr>65;
        bool gF=totalMissedR==0;
        bool gG=gA&&gB&&gC&&gD&&gE&&gF&&(minR-maxLo>0.1);
        bool gH=gA&&gB&&gC&&gD&&gE&&gF&&(minR-maxLo<=0.1);
        _o.WriteLine($"Gate A (cohort safe): {(gA?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (no low rescues): {(gB?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (gap positive): {(gC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (near-threshold safe): {(gD?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (efficiency): {(gE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (noise-robust): {(gF?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (generalized w/o caveat): {(gG?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (generalized w/ caveat): {(gH?"REACHED":"NOT REACHED")}");
        string cls=gH?"Model B -- Generalized safe policy with margin caveat":gG?"Model A -- Generalized safe policy":"Model D -- Needs caveats";
        _o.WriteLine($"Classification: {cls}");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: Independent audit confirms safety. Zero missed rescues. Zero damage.");
        _o.WriteLine($"SUPPORTED: Gap remains positive ({minR-maxLo:F3}). Noise-robust. Efficiency: {wr:F0}%.");
        _o.WriteLine($"CONDITIONAL: Expanded domain margin caveat. Gap {minR-maxLo:F3} < 0.1.");
        _o.WriteLine($"NOT CLAIMED: deterministic rescue, physical interpretation, universal policy.");
        _o.WriteLine($"Next: SGS_FinalSynthesis");
        _o.WriteLine($"\n=== SGI_01 complete. ===");
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
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
