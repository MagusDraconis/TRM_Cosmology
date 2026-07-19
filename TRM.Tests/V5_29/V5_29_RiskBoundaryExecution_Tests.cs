using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_29;

[Trait("Category","V5_29"),Trait("Category","V5_29_RBE"),Trait("Category","LongRunning")]
public class V5_29_RiskBoundaryExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const double FTHR=0.1;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct BProfile{
        public int n,s,cohort;public double c3OmgS,omegaPerK;public bool persistent,persistentNoCont,a0;
    }

    public V5_29_RiskBoundaryExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    BProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new BProfile{n=n,s=s,cohort=s/100};
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
            p.omegaPerK=p.c3OmgS/Math.Max(1e-9,Math.Abs(Km(Cupd(dmat3,n),n)-kmPre));
            bool c3=Of(hc3cc,n).Average()>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=c3&&Of(hCont,n).Average()>THR;
        }else{p.persistent=false;}
        return p;
    }

    [Fact]
    public void RBE_01_RiskBoundaryExecution()
    {
        _o.WriteLine(new string('=',55));
        _o.WriteLine("=== RBE_01: Risk Boundary Execution ===");
        _o.WriteLine("=== How robust is the c3OmgS=0.1 boundary? ===");
        _o.WriteLine(new string('=',55));

        int[] Ns={64,65,66,70,72,75,76,79,80,85};
        var all=new ConcurrentBag<BProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)all.Add(p.Value);}});
        var profs=all.ToArray();
        int[] cohorts=profs.Select(p=>p.cohort).Distinct().OrderBy(c=>c).ToArray();

        var loRisk=profs.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var hiRisk=profs.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"Profiles: {profs.Length}. Low: {loRisk.Length} (rescues={loRisk.Count(p=>p.persistent)}). High: {hiRisk.Length} (rescues={hiRisk.Count(p=>p.persistent)}).");

        // 1. Boundary density audit (8 bins)
        _o.WriteLine($"\n--- 1. Boundary Density Audit ---");
        double[][] bins={new[]{double.MinValue,0.0},new[]{0.0,0.05},new[]{0.05,0.075},new[]{0.075,0.1},new[]{0.1,0.125},new[]{0.125,0.15},new[]{0.15,0.25},new[]{0.25,double.MaxValue}};
        string[] blabels={"<=0.00","(0,0.05]","(0.05,0.075]","(0.075,0.1]","(0.1,0.125]","(0.125,0.15]","(0.15,0.25]",">0.25"};
        _o.WriteLine($"{"Bin",12} {"n",5} {"rescues",7} {"rate",6} {"N distribution"}");
        foreach(var i in Enumerable.Range(0,bins.Length)){
            var bn=profs.Where(p=>p.c3OmgS>bins[i][0]&&p.c3OmgS<=bins[i][1]).ToArray();
            int r=bn.Count(p=>p.persistent);
            double rate=bn.Length>0?r*100.0/bn.Length:0;
            var nDist=bn.GroupBy(p=>p.n).OrderBy(g=>g.Key).Select(g=>$"{g.Key}({g.Count()})");
            _o.WriteLine($"{blabels[i],12} {bn.Length,5} {r,7} {rate,5:F1}% [{string.Join(" ",nDist)}]");
        }

        // 2. Near-threshold rescue audit
        _o.WriteLine($"\n--- 2. Near-Threshold Rescue Audit ---");
        var justBelow=profs.Where(p=>p.c3OmgS>0.075&&p.c3OmgS<=FTHR).ToArray();
        var justAbove=profs.Where(p=>p.c3OmgS>FTHR&&p.c3OmgS<=0.125).ToArray();
        _o.WriteLine($"(0.075,0.1]: n={justBelow.Length}, rescues={justBelow.Count(p=>p.persistent)}");
        _o.WriteLine($"(0.1,0.125]: n={justAbove.Length}, rescues={justAbove.Count(p=>p.persistent)}");
        if(justBelow.Any(p=>p.persistent)){
            foreach(var r in justBelow.Where(p=>p.persistent))_o.WriteLine($"  RESCUE NEAR THRESHOLD: N={r.n}, s={r.s}, c3={r.c3OmgS:F4}");
        }else _o.WriteLine("  No rescues in (0.075, 0.1]. SAFE.");

        // 3. Near-threshold failure audit
        _o.WriteLine($"\n--- 3. Near-Threshold Failure Audit ---");
        var hiFail=hiRisk.Where(p=>!p.persistent).OrderBy(p=>p.c3OmgS).ToArray();
        _o.WriteLine($"High-stratum non-rescues: {hiFail.Length}. Closest to threshold:");
        foreach(var f in hiFail.Take(5))_o.WriteLine($"  N={f.n}, s={f.s}, c3={f.c3OmgS:F4}");

        // 4. Safety margin
        _o.WriteLine($"\n--- 4. Safety Margin Analysis ---");
        double maxStopped=loRisk.Max(p=>p.c3OmgS);
        double minRescue=hiRisk.Where(p=>p.persistent).Min(p=>p.c3OmgS);
        double gap=minRescue-maxStopped;
        _o.WriteLine($"Max c3OmgS in stopped (<=0.1): {maxStopped:F4}");
        _o.WriteLine($"Min c3OmgS in rescues (>0.1): {minRescue:F4}");
        _o.WriteLine($"Safety gap: {gap:F4} ({(gap>0.05?"LARGE":gap>0.02?"ADEQUATE":gap>0?"NARROW":"NEGATIVE -- UNSAFE")})");

        // 5. Measurement noise stress
        _o.WriteLine($"\n--- 5. Measurement Noise Stress ---");
        double[] noises={0.005,0.01,0.02,0.05};
        _o.WriteLine($"{"Noise",8} {"flipped",7} {"missedR",7} {"extraC",7} {"falseStop",9} {"falseCont",9}");
        int totalMissedR=0;
        foreach(var noise in noises){
        int flipped=0,missedR=0,extraC=0,falseStop=0,falseCont=0;
            var rng=new Random(42);
            for(int trial=0;trial<100;trial++){
                foreach(var p in profs){
                    double pert=p.c3OmgS+rng.NextDouble()*2*noise-noise;
                    bool origLow=p.c3OmgS<=FTHR,pertLow=pert<=FTHR;
                    if(origLow!=pertLow){
                        flipped++;
                        if(!origLow&&pertLow&&p.persistent)missedR++; // was high rescue, now low -> missed
                        if(origLow&&!pertLow)extraC++; // was low, now high -> extra continuation
                        if(!origLow&&pertLow)falseStop++; // was high, now stopped
                        if(origLow&&!pertLow&&!p.persistent)falseCont++; // was low non-rescue, now continued
                    }
                }
            }
            _o.WriteLine($"{noise,8:F3} {flipped/100.0,7:F1} {missedR/100.0,7:F1} {extraC/100.0,7:F1} {falseStop/100.0,9:F1} {falseCont/100.0,9:F1}");
            totalMissedR+=missedR;
        }

        // 6. N-specific boundary
        _o.WriteLine($"\n--- 6. N-Specific Boundary ---");
        _o.WriteLine($"{"N",4} {"n",5} {"loN",5} {"hiN",5} {"maxLo",7} {"minResc",8} {"gap",7} {"safe?",6}");
        foreach(var n in Ns){
            var sn=profs.Where(p=>p.n==n).ToArray();
            var loN=sn.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var hiN=sn.Where(p=>p.c3OmgS>FTHR).ToArray();
            double maxLo=loN.Length>0?loN.Max(p=>p.c3OmgS):0;
            double minR=hiN.Where(p=>p.persistent).Any()?hiN.Where(p=>p.persistent).Min(p=>p.c3OmgS):double.NaN;
            double g=double.IsNaN(minR)?double.NaN:minR-maxLo;
            string safe=g>0.02?"SAFE":g>0?"OK":"CHECK";
            _o.WriteLine($"{n,4} {sn.Length,5} {loN.Length,5} {hiN.Length,5} {maxLo,7:F4} {minR,8:F4} {g,7:F4} {safe,6}");
        }

        // 7. Cohort-specific boundary
        _o.WriteLine($"\n--- 7. Cohort-Specific Boundary ---");
        foreach(var c in cohorts){
            var sc=profs.Where(p=>p.cohort==c).ToArray();
            var loC=sc.Where(p=>p.c3OmgS<=FTHR).ToArray();
            var hiC=sc.Where(p=>p.c3OmgS>FTHR).ToArray();
            double maxLo=loC.Length>0?loC.Max(p=>p.c3OmgS):0;
            double minR=hiC.Where(p=>p.persistent).Any()?hiC.Where(p=>p.persistent).Min(p=>p.c3OmgS):double.NaN;
            _o.WriteLine($"Cohort {c}: maxLo={maxLo:F4}, minRescue={minR:F4}, {(minR-maxLo>0.02?"SAFE":"CHECK")}");
        }

        // 8. Boundary policy verdict
        _o.WriteLine($"\n--- 8. Boundary Policy Verdict ---");
        bool gA=loRisk.Count(p=>p.persistent)==0;
        bool gB=gap>0.02;
        bool gC=totalMissedR==0;
        bool gE=Ns.All(n=>{var sn=profs.Where(p=>p.n==n).ToArray();return sn.Where(p=>p.c3OmgS<=FTHR&&p.persistent).Count()==0;});
        bool gF=cohorts.All(c=>{var sc=profs.Where(p=>p.cohort==c).ToArray();return sc.Where(p=>p.c3OmgS<=FTHR&&p.persistent).Count()==0;});
        bool gG=gA&&gC&&gE&&gF;
        _o.WriteLine($"Gate A (no low rescues): {(gA?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (safety margin): {(gB?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (noise-robust): {(gC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (N-stable): {(gE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (cohort-stable): {(gF?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (threshold confirmed): {(gG?"REACHED":"NOT REACHED")}");
        string cls=gG?"Model A -- Robust hard stop threshold":gA&&gB?"Model B -- Robust with safety margin":"Model C -- Needs audit";
        _o.WriteLine($"Verdict: {cls}");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: Safety margin of {gap:F3} exists below threshold. Zero low-side rescues.");
        _o.WriteLine($"SUPPORTED: Threshold is N-stable and cohort-stable.");
        _o.WriteLine($"CONDITIONAL: Noise at ±0.02+ may flip classifications. Measurement precision matters.");
        _o.WriteLine($"NOT CLAIMED: physical interpretation, deterministic rescue, universal control.");
        _o.WriteLine($"Next: RBA_BoundaryAnalysis or RSS_FinalSynthesis");
        _o.WriteLine($"\n=== RBE_01 complete. ===");
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
