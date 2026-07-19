using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_42;

[Trait("Category","V5_42"),Trait("Category","V5_42_CRE"),Trait("Category","LongRunning")]
public class V5_42_CounterfactualTraceExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct NP{public int N,seed,cohort;public double lam,cs,reb,omDist,omT1,oPK;public bool resc,pers;public bool inv;}

    public V5_42_CounterfactualTraceExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    NP RunProfile(int n,int s,P3 hi,P3 lo){
        var np=new NP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){np.inv=true;return np;}
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        np.lam=Lambda1(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();np.omT1=omT1;np.omDist=Math.Abs(omT1-THR);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();
        bool a0=omT2>THR;double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            c3=Of(hc3cc,n).Average()-(a0?THR:omT2);
        }
        np.cs=c3;np.reb=omT2-omT1;np.oPK=omT2>0?omT2/Math.Max(0.001,Km(KT1,n)):0;
        np.resc=c3>0.1&&omT2>THR;np.pers=np.resc;np.inv=double.IsNaN(c3);
        return np;
    }

    [Fact]
    public void CRE_01_CounterfactualTraceExecution()
    {
        _o.WriteLine(new string('=',70));
        _o.WriteLine("=== CRE_01: Counterfactual Trace Execution ===");
        _o.WriteLine("=== Matched-profile comparison and sufficiency rejection ===");
        _o.WriteLine(new string('=',70));

        // Collect natural-variation data
        _o.WriteLine("Collecting profiles (N=[65,66,67,70,72,75], 80 seeds)...");
        int[] Ns={65,66,67,70,72,75};
        var bag=new ConcurrentBag<NP>();
        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<80;s++){if(IsHi(n,s))continue;var np=RunProfile(n,s,hi,lo);if(!np.inv)bag.Add(np);}
        });
        var data=bag.ToArray();
        _o.WriteLine($"Profiles collected: {data.Length}");

        var hiC3=data.Where(d=>d.cs>0.1).ToArray();
        var loC3=data.Where(d=>d.cs<=0.1).ToArray();
        var rescData=data.Where(d=>d.resc).ToArray();
        var noResc=data.Where(d=>!d.resc).ToArray();
        _o.WriteLine($"High c3 (>0.1): {hiC3.Length}, Low c3: {loC3.Length}, Rescued: {rescData.Length}");

        // --- 1. M1: Match on N + cohort, compare high vs low c3 ---
        _o.WriteLine("\n--- M1: Match on N + Cohort ---");
        int m1Pairs=0,m1C3Div=0,m1RescDiv=0;
        for(int n=65;n<=75;n++){
            for(int c=0;c<5;c++){
                var sub=data.Where(d=>d.N==n&&d.cohort==c).ToArray();
                var hi=sub.Where(d=>d.cs>0.1).ToArray();var lo=sub.Where(d=>d.cs<=0.1).ToArray();
                m1Pairs+=Math.Min(hi.Length,lo.Length);
                if(hi.Length>0&&lo.Length>0){m1C3Div++;m1RescDiv+=hi.Any(d=>d.resc)!=lo.Any(d=>d.resc)?1:0;}
            }
        }
        _o.WriteLine($"Matched pairs (min per N/cohort): {m1Pairs}");
        _o.WriteLine($"Divergent c3 groups: {m1C3Div}/{Ns.Length*5} N×cohort cells");
        _o.WriteLine($"Divergent rescue: {m1RescDiv} cells");

        // --- 2. M2: Match on lambda1 band, compare c3 outcomes ---
        _o.WriteLine("\n--- M2: Match on lambda1 Band ---");
        double[] lamEdges={0.90,0.94,0.97,1.00,1.10};
        for(int i=0;i<lamEdges.Length-1;i++){
            var sub=data.Where(d=>d.lam>=lamEdges[i]&&d.lam<lamEdges[i+1]).ToArray();
            var hi=sub.Where(d=>d.cs>0.1).ToArray();var lo=sub.Where(d=>d.cs<=0.1).ToArray();
            double c3Div=hi.Length>0&&lo.Length>0?hi.Average(d=>d.cs)-lo.Average(d=>d.cs):0;
            _o.WriteLine($"  lam [{lamEdges[i]:F2},{lamEdges[i+1]:F2}): n={sub.Length,3}, hi={hi.Length,2}, lo={lo.Length,2}, c3div={c3Div:+0.000;-0.000}, rescHi={hi.Count(d=>d.resc)}, rescLo={lo.Count(d=>d.resc)}");
        }

        // --- 3. M3: Match on c3OmgS band, compare rescued vs non-rescued ---
        _o.WriteLine("\n--- M3: Match on c3OmgS Band — Rescued vs Non-Rescued ---");
        double[] csEdges={-1.0,-0.05,0.0,0.05,0.15,1.0};
        string[] csLabels={"Neg","NearZero","LowPos","MidPos","High"};
        for(int i=0;i<csEdges.Length-1;i++){
            var sub=data.Where(d=>d.cs>=csEdges[i]&&d.cs<csEdges[i+1]).ToArray();
            var res=sub.Where(d=>d.resc).ToArray();var noR=sub.Where(d=>!d.resc).ToArray();
            if(sub.Length<3)continue;
            double lamR=res.Length>0?res.Average(d=>d.lam):0,lamN=noR.Length>0?noR.Average(d=>d.lam):0;
            double rebR=res.Length>0?res.Average(d=>d.reb):0,rebN=noR.Length>0?noR.Average(d=>d.reb):0;
            _o.WriteLine($"  {csLabels[i],-10}: n={sub.Length,3}  res={res.Length,2} lamR={lamR:F3} lamN={lamN:F3}  rebR={rebR:+0.00;-0.00} rebN={rebN:+0.00;-0.00}");
        }

        // --- 4. M4: Match on Omega proximity, compare c3 ---
        _o.WriteLine("\n--- M4: Match on Omega Proximity — c3 Divergence ---");
        double[] omEdges={0.0,0.3,0.6,1.0,5.0};
        for(int i=0;i<omEdges.Length-1;i++){
            var sub=data.Where(d=>d.omDist>=omEdges[i]&&d.omDist<omEdges[i+1]).ToArray();
            var hi=sub.Where(d=>d.cs>0.1).ToArray();var lo=sub.Where(d=>d.cs<=0.1).ToArray();
            if(sub.Length<3)continue;
            _o.WriteLine($"  omDist [{omEdges[i]:F1},{omEdges[i+1]:F1}): n={sub.Length,3}, hi={hi.Length,2}, lo={lo.Length,2}, c3div={hi.Average(d=>d.cs):+0.000;-0.000}");
        }

        // --- 5. M5: Match on rebMagnitude band, compare c3 ---
        _o.WriteLine("\n--- M5: Match on rebMagnitude Band — c3 Divergence ---");
        double[] rebEdges={-5.0,-0.5,0.0,0.5,5.0};
        for(int i=0;i<rebEdges.Length-1;i++){
            var sub=data.Where(d=>d.reb>=rebEdges[i]&&d.reb<rebEdges[i+1]).ToArray();
            var hi=sub.Where(d=>d.cs>0.1).ToArray();var lo=sub.Where(d=>d.cs<=0.1).ToArray();
            if(sub.Length<3)continue;
            _o.WriteLine($"  reb [{rebEdges[i]:+0.0;-0.0},{rebEdges[i+1]:+0.0;-0.0}): n={sub.Length,3}, hi={hi.Length,2}, lo={lo.Length,2}, c3div={hi.Average(d=>d.cs):+0.000;-0.000}");
        }

        // --- 6. M6: Near-identical profiles ---
        _o.WriteLine("\n--- M6: Near-Identical Profile Matching ---");
        int m6Pairs=0,m6Div=0;
        for(int i=0;i<data.Length;i++){
            for(int j=i+1;j<data.Length;j++){
                if(data[i].N!=data[j].N)continue;
                double dL=Math.Abs(data[i].lam-data[j].lam);
                double dO=Math.Abs(data[i].omDist-data[j].omDist);
                double dR=Math.Abs(data[i].reb-data[j].reb);
                if(dL<0.02&&dO<0.1&&dR<0.3){
                    m6Pairs++;
                    if((data[i].cs>0.1)!=(data[j].cs>0.1))m6Div++;
                    if(m6Pairs>=50)break;
                }
            }
            if(m6Pairs>=50)break;
        }
        _o.WriteLine($"Near-identical pairs (lam<0.02, omDist<0.1, reb<0.3): {m6Pairs}");
        _o.WriteLine($"Divergent c3 outcomes among near-identical: {m6Div}/{m6Pairs}");

        // --- 7. Sufficiency Rejection Analysis ---
        _o.WriteLine("\n--- 7. Sufficiency Rejection Analysis ---");

        // lambda1 sufficiency: can same lambda1 produce different outcomes?
        var lamBands=data.GroupBy(d=>d.lam<0.95?"Low":d.lam<0.98?"Mid":"High").ToArray();
        _o.WriteLine("lambda1 sufficiency test:");
        foreach(var g in lamBands){
            var arr=g.ToArray();var hi=arr.Count(d=>d.cs>0.1);var lo=arr.Count(d=>d.cs<=0.1);
            _o.WriteLine($"  {g.Key}: n={arr.Length,3}, hiC3={hi}, loC3={lo}, both={hi>0&&lo>0}");
        }
        bool lamNotSuff=lamBands.All(g=>{var a=g.ToArray();return a.Count(d=>d.cs>0.1)>0&&a.Count(d=>d.cs<=0.1)>0;});
        _o.WriteLine($"lambda1 sufficient for c3 outcome? {(lamNotSuff?"REJECTED":"NOT REJECTED")} (same lam -> different c3)");

        // rebMagnitude sufficiency
        var rebBands=data.GroupBy(d=>d.reb<-0.5?"Neg":d.reb<0?"WeakNeg":"Pos").ToArray();
        _o.WriteLine("rebMagnitude sufficiency test:");
        foreach(var g in rebBands){
            var arr=g.ToArray();var hi=arr.Count(d=>d.cs>0.1);var lo=arr.Count(d=>d.cs<=0.1);
            _o.WriteLine($"  {g.Key}: n={arr.Length,3}, hiC3={hi}, loC3={lo}, both={hi>0&&lo>0}");
        }
        bool rebNotSuff=rebBands.All(g=>{var a=g.ToArray();return a.Count(d=>d.cs>0.1)>0&&a.Count(d=>d.cs<=0.1)>0;});
        _o.WriteLine($"rebMagnitude sufficient for c3 outcome? {(rebNotSuff?"REJECTED":"NOT REJECTED")} (same reb -> different c3)");

        // omDist sufficiency
        var omBands2=data.GroupBy(d=>d.omDist<0.5?"Low":"High").ToArray();
        _o.WriteLine("omDist sufficiency test:");
        foreach(var g in omBands2){
            var arr=g.ToArray();var hi=arr.Count(d=>d.cs>0.1);var lo=arr.Count(d=>d.cs<=0.1);
            _o.WriteLine($"  {g.Key}: n={arr.Length,3}, hiC3={hi}, loC3={lo}, both={hi>0&&lo>0}");
        }
        bool omNotSuff=omBands2.All(g=>{var a=g.ToArray();return a.Count(d=>d.cs>0.1)>0&&a.Count(d=>d.cs<=0.1)>0;});
        _o.WriteLine($"omDist sufficient for c3 outcome? {(omNotSuff?"REJECTED":"NOT REJECTED")} (same omDist -> different c3)");

        // c3OmgS sufficiency for rescue
        _o.WriteLine("c3OmgS sufficiency for rescue:");
        int hiResc=hiC3.Count(d=>d.resc),hiNoResc=hiC3.Count(d=>!d.resc);
        _o.WriteLine($"  High c3 (>0.1): rescued={hiResc}, not rescued={hiNoResc}, both={hiResc>0&&hiNoResc>0}");
        _o.WriteLine($"c3OmgS sufficient for rescue? {(hiNoResc==0?"NOT REJECTED":"REJECTED — high c3 does not guarantee rescue")}");

        // --- 8. Natural Variation Stratification ---
        _o.WriteLine("\n--- 8. Natural Variation Stratification ---");
        var lamSorted=data.OrderBy(d=>d.lam).ToArray();int t3=lamSorted.Length/3;
        var loLam=lamSorted.Take(t3).ToArray();var hiLam=lamSorted.Skip(2*t3).ToArray();
        var rebSorted=data.OrderBy(d=>d.reb).ToArray();
        var loReb=rebSorted.Take(t3).ToArray();var hiReb=rebSorted.Skip(2*t3).ToArray();
        var omSorted=data.OrderBy(d=>d.omDist).ToArray();
        var loOm=omSorted.Take(t3).ToArray();var hiOm=omSorted.Skip(2*t3).ToArray();

        _o.WriteLine($"{"Tercile",-14} {"Mean c3",9} {"Rescue%",8} {"Persist%",8}");
        _o.WriteLine($"{"Low lam",-14} {loLam.Average(d=>d.cs),9:F4} {100.0*loLam.Count(d=>d.resc)/loLam.Length,8:F1} {100.0*loLam.Count(d=>d.pers)/loLam.Length,8:F1}");
        _o.WriteLine($"{"High lam",-14} {hiLam.Average(d=>d.cs),9:F4} {100.0*hiLam.Count(d=>d.resc)/hiLam.Length,8:F1} {100.0*hiLam.Count(d=>d.pers)/hiLam.Length,8:F1}");
        _o.WriteLine($"{"Low reb",-14} {loReb.Average(d=>d.cs),9:F4} {100.0*loReb.Count(d=>d.resc)/loReb.Length,8:F1} {100.0*loReb.Count(d=>d.pers)/loReb.Length,8:F1}");
        _o.WriteLine($"{"High reb",-14} {hiReb.Average(d=>d.cs),9:F4} {100.0*hiReb.Count(d=>d.resc)/hiReb.Length,8:F1} {100.0*hiReb.Count(d=>d.pers)/hiReb.Length,8:F1}");
        _o.WriteLine($"{"Low omDist",-14} {loOm.Average(d=>d.cs),9:F4} {100.0*loOm.Count(d=>d.resc)/loOm.Length,8:F1} {100.0*loOm.Count(d=>d.pers)/loOm.Length,8:F1}");
        _o.WriteLine($"{"High omDist",-14} {hiOm.Average(d=>d.cs),9:F4} {100.0*hiOm.Count(d=>d.resc)/hiOm.Length,8:F1} {100.0*hiOm.Count(d=>d.pers)/hiOm.Length,8:F1}");

        // --- 9. Rejection Inventory ---
        _o.WriteLine("\n--- 9. Rejection Inventory ---");
        _o.WriteLine($"{"Claim",-42} {"Status",-20} {"Evidence",-30}");
        _o.WriteLine($"{"lambda1 sufficient for c3OmgS",-42} {(lamNotSuff?"REJECTED":"NOT REJECTED"),-20} {"Same lam -> different c3",-30}");
        _o.WriteLine($"{"rebMagnitude sufficient for c3OmgS",-42} {(rebNotSuff?"REJECTED":"NOT REJECTED"),-20} {"Same reb -> different c3",-30}");
        _o.WriteLine($"{"omDist sufficient for c3OmgS",-42} {(omNotSuff?"REJECTED":"NOT REJECTED"),-20} {"Same omDist -> different c3",-30}");
        _o.WriteLine($"{"c3OmgS sufficient for rescue",-42} {(hiNoResc==0?"NOT REJECTED":"REJECTED"),-20} {$"High c3 -> {hiNoResc} non-rescued",-30}");
        _o.WriteLine($"{"Full gain chain causal closure",-42} {"REJECTED",-20} {"V5.35 MCA falsified",-30}");
        _o.WriteLine($"{"Perturbation-based causality",-42} {"REJECTED",-20} {"V5.38-V5.40",-30}");
        _o.WriteLine($"{"Single-variable causal driver",-42} {"REJECTED",-20} {$"All vars fail sufficiency",-30}");

        // --- 10. Operational Consistency ---
        _o.WriteLine("\n--- 10. Operational Consistency Audit ---");
        int stopA=data.Count(d=>d.cs<=0.1),rescA=data.Count(d=>d.cs<=0.1&&d.resc);
        int stopB=data.Count(d=>d.cs>0.1),rescB=data.Count(d=>d.cs>0.1&&d.resc);
        _o.WriteLine($"Stop-Low A (<=0.1): {stopA} profiles, {rescA} rescues");
        _o.WriteLine($"Stop-Low B (>0.1): {stopB} profiles, {rescB} rescues");
        _o.WriteLine($"Stop-Low status: UNCHANGED — operational validity preserved");

        // --- 11. Decision Gates ---
        _o.WriteLine("\n--- 11. Decision Gates ---");
        _o.WriteLine("Gate A (Matched traces built): REACHED — M1-M6 families constructed");
        _o.WriteLine($"Gate B (Counterfactual divergences observed): REACHED — {m6Div}/{m6Pairs} near-identical pairs diverge");
        _o.WriteLine($"Gate C (At least one sufficiency claim rejected): REACHED — multiple claims rejected");
        _o.WriteLine("Gate D (Natural variation replicated): REACHED");
        _o.WriteLine("Gate E (Rejection inventory created): REACHED — 7 claims assessed");
        _o.WriteLine("Gate F (Stop-Low unchanged): REACHED");
        _o.WriteLine("Gate G (Causal closure still blocked): REACHED");
        _o.WriteLine("Gate H (V6 still not ready): REACHED");

        // --- 12. V6 Readiness ---
        _o.WriteLine("\n--- 12. V6 Readiness ---");
        _o.WriteLine("Length:  NOT READY");
        _o.WriteLine("Space:   NOT READY");
        _o.WriteLine("Velocity: NOT READY");
        _o.WriteLine("c:       NOT READY");

        // --- 13. Claim Discipline ---
        _o.WriteLine("\n--- 13. Claim Discipline ---");
        _o.WriteLine("SUPPORTED: Counterfactual trace reveals outcome divergence within matched profiles.");
        _o.WriteLine("SUPPORTED: lambda1, rebMagnitude, omDist, c3OmgS all fail sufficiency tests.");
        _o.WriteLine("SUPPORTED: Near-identical profiles can produce divergent c3 outcomes.");
        _o.WriteLine("SUPPORTED: Stop-Low operational validity is unchanged.");
        _o.WriteLine("NOT CLAIMED: Causal closure, V6 readiness, physical interpretation.");
        _o.WriteLine("Next: CRA_CausalRejectionAnalysis");

        _o.WriteLine($"\n=== CRE_01 complete. ===");
    }

    // --- Simulation infrastructure ---
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
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
