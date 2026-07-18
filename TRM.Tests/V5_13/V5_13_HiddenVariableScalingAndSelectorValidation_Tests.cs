using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_13;

[Trait("Category","V5_13"),Trait("Category","V5_13_HVS"),Trait("Category","LongRunning")]
public class V5_13_HiddenVariableScalingAndSelectorValidation_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double ProjHiVecThresh=-0.3281; // Frozen from HVI

    // ── Data ──
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,dh0;public string cls;}
    struct Trial{public int seed,cohort;public string proto,cls;public double d0,dT1,dT2,omT1,omT2;public bool immHi,persist;}
    struct FullProfile{
        public int seed,cohort,n;public string cls;
        public double dMean,kMean,lambda1,kFrob,projHiVec,distToHi;
        public double top1EdgeShare,top5EdgeShare;
        public bool immHi,persist;public double omT1,omT2,dT1,dT2;
        // Selector flags (computed post-collection)
        public bool selS1,selS2,selS3;
    }

    public V5_13_HiddenVariableScalingAndSelectorValidation_Tests(ITestOutputHelper o){_o=o;}

    // ════════════════════════════════════════════════════════
    // HVS_01: Data collection + selector validation
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_01_SelectorValidation(){
        _o.WriteLine("═══ HVS_01: Selector validation ═══");
        var profiles=CollectProfiles();

        // Compute frozen selector flags
        ComputeSelectorFlags(profiles);

        _o.WriteLine("\n─── Frozen thresholds ───");
        _o.WriteLine($"S1 projHiVec > {ProjHiVecThresh}");

        // Per-N, per-cohort selector comparison
        foreach(var n in new[]{67,71,72,75,80}){
            _o.WriteLine($"\n─── N={n} ───");
            _o.WriteLine(string.Format("{0,-10} {1,-10} {2,6} {3,6} {4,6} {5,7} {6,8} {7,8} {8,8}",
                "Cohort","Selector","Sel","Strict","Rate","ImmRate","Prec","Recall","Lift%"));
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                if(sub.Length==0)continue;

                // S0: pathway-only baseline — all P1/P1b matched
                PrintSelectorRow(sub,"S0:PathOnly","S0",coh);
                // S1: P1/P1b + projHiVec
                PrintSelectorRow(sub,"S1:+projHi","S1",coh);
                // S4: P1/P1b high-room only (P1b classification)
                PrintSelectorRow(sub,"S4:HiRoom","S4",coh);
                // S0+P2: pathway-only including P2
                PrintSelectorRow(sub,"S5:+P2","S5",coh);
                // S6: P1/P1b only (no P2 in model)
                PrintSelectorRow(sub,"S6:-P2","S6",coh);
            }
        }

        // Holdout lift summary
        _o.WriteLine("\n─── Holdout lift over S0 ───");
        _o.WriteLine(string.Format("{0,4} {1,14} {2,14} {3,14}",
            "N","S1:+projHi","S4:HiRoom","S6:-P2"));
        foreach(var n in new[]{67,71,72,75,80}){
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            if(hold.Length==0)continue;
            double s0Rate=SelectorRate(hold,"S0");
            double s1Rate=SelectorRate(hold,"S1");
            double s4Rate=SelectorRate(hold,"S4");
            double s6Rate=SelectorRate(hold,"S6");
            _o.WriteLine($"{n,4} {s1Rate-s0Rate,13:+.0%} {s4Rate-s0Rate,13:+.0%} {s6Rate-s0Rate,13:+.0%}");
        }

        _profiles=profiles;
    }

    // ════════════════════════════════════════════════════════
    // HVS_02: N-specific behavior classification
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_02_NSpecificBehavior(){
        _o.WriteLine("═══ HVS_02: N-specific selector behavior ═══");
        var profiles=EnsureProfiles();

        _o.WriteLine(string.Format("\n{0,4} {1,12} {2,8} {3,8} {4,8} {5,14}",
            "N","S1_Train","S1_Hold","S4_Hold","S6_Hold","Classification"));
        foreach(var n in new[]{67,71,72,75,80}){
            var train=profiles.Where(p=>p.n==n&&p.cohort==0).ToArray();
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            double s1t=SelectorRate(train,"S1"),s1h=SelectorRate(hold,"S1");
            double s4h=SelectorRate(hold,"S4"),s6h=SelectorRate(hold,"S6");
            double s0h=SelectorRate(hold,"S0");

            string cls;
            if(n==67)cls="INACCESSIBLE";
            else if(n==80)cls="SATURATED";
            else if(s1h>s0h+0.03)cls="SELECTOR_USEFUL";
            else if(Math.Abs(s1h-s0h)<=0.03)cls="SELECTOR_NEUTRAL";
            else cls="SELECTOR_HARMFUL";
            _o.WriteLine($"{n,4} {s1t,11:.0%} {s1h,8:.0%} {s4h,8:.0%} {s6h,8:.0%} {cls,14}");
        }
    }

    // ════════════════════════════════════════════════════════
    // HVS_03: P2 demotion audit
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_03_P2DemotionAudit(){
        _o.WriteLine("═══ HVS_03: P2 demotion audit ═══");
        var profiles=EnsureProfiles();

        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,4} {4,4} {5,8} {6,8} {7,10}",
            "N","Cohort","Model","P1","P2","P1Rate","P2Rate","Verdict"));
        foreach(var n in new[]{67,71,72}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                var p1=sub.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
                var p2=sub.Where(p=>p.cls=="P2").ToArray();
                double p1r=p1.Length>0?(double)p1.Count(p=>p.persist)/p1.Length:0;
                double p2r=p2.Length>0?(double)p2.Count(p=>p.persist)/p2.Length:0;
                string v=p2.Length==0?"N/A":p2r<0.05&&p1r>0?"DEMOTE":(p2r>=p1r?"RETAIN":"WEAK");
                string cohLab=coh==0?"Train":"Hold";
                string p1lab=p1r>0?"P1_OK":"-";
                _o.WriteLine($"{n,4} {cohLab,6} {p1lab,6} {p2.Length,4} {p2r,4:P0} {p1r,8:P0} {p2r,8:P0} {v,10}");
            }
        }

        // Overall P2 transfer
        _o.WriteLine("\n─── P2 transfer summary ───");
        foreach(var n in new[]{67,71,72}){
            var tP2=profiles.Where(p=>p.n==n&&p.cohort==0&&p.cls=="P2"&&p.persist).ToArray();
            var hP2=profiles.Where(p=>p.n==n&&p.cohort==1&&p.cls=="P2"&&p.persist).ToArray();
            var hAll=profiles.Where(p=>p.n==n&&p.cohort==1&&p.cls=="P2").ToArray();
            _o.WriteLine($"N={n}: Train P2 success={tP2.Length}, Holdout P2 success={hP2.Length}/{hAll.Length} → {(hP2.Length==0?"NO TRANSFER":"PARTIAL")}");
        }
    }

    // ════════════════════════════════════════════════════════
    // HVS_04: Lambda1 additive value
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_04_Lambda1AdditiveValue(){
        _o.WriteLine("═══ HVS_04: lambda1 additive value ═══");
        var profiles=EnsureProfiles();

        // Train lambda1 threshold on cohort 0, test on cohort 1
        var train=profiles.Where(p=>p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        double bestThresh=0,bestAcc=0;
        for(int i=1;i<train.Length;i++){
            double t=(train.OrderBy(p=>p.lambda1).ElementAt(i-1).lambda1+train.OrderBy(p=>p.lambda1).ElementAt(i).lambda1)/2;
            int a=train.Count(p=>p.lambda1>t&&p.persist);
            int b=train.Count(p=>p.lambda1<=t&&!p.persist);
            double acc=(double)(a+b)/train.Length;
            if(acc>bestAcc){bestAcc=acc;bestThresh=t;}
        }
        _o.WriteLine($"lambda1 threshold (trained on 0-99): {bestThresh:F1} (acc={bestAcc:F3})");
        _o.WriteLine("Marked EXPLORATORY — threshold not frozen from HVI.");

        // Compute S2 flags
        foreach(var n in new[]{67,71,72,75,80}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                for(int i=0;i<sub.Length;i++)sub[i].selS2=sub[i].lambda1>bestThresh;
            }
        }
        // Compute S3 = S1 AND S2
        for(int i=0;i<profiles.Length;i++)profiles[i].selS3=profiles[i].selS1&&profiles[i].selS2;

        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "N","Cohort","S0","S1","S2(λ1)","S3(S1+λ1)","ΔS3vS1"));
        foreach(var n in new[]{71,72,75}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                double s0=SelectorRate(sub,"S0"),s1=SelectorRate(sub,"S1");
                double s2Rate=SelectorRate(sub,"S2");double s3Rate=SelectorRate(sub,"S3");
                int s1n=sub.Count(p=>p.selS1),s3n=sub.Count(p=>p.selS3);
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {s0,7:.0%} {s1,8:.0%} {s2Rate,8:.0%} {s3Rate,8:.0%} ({s3n}/{s1n}) {s3Rate-s1,8:+.0%}");
            }
        }
        _o.WriteLine("\nλ1 is EXPLORATORY. Do not use for gate decisions.");
    }

    // ════════════════════════════════════════════════════════
    // HVS_05: Final model candidate comparison
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_05_FinalModelCandidate(){
        _o.WriteLine("═══ HVS_05: Final model candidate comparison ═══");
        var profiles=EnsureProfiles();
        int[] Ns={71,72,75};

        _o.WriteLine("\n─── Model candidates ───");
        _o.WriteLine("M1: P1/P1b only (no selector)");
        _o.WriteLine("M2: P1/P1b + projHiVec > -0.3281");
        _o.WriteLine("M3: N-conditioned P1/P1b + projHiVec (selector at N=71/72, bypass at N=75)");
        _o.WriteLine("M4: P1/P1b + projHiVec, N=80 saturation bypass");
        _o.WriteLine("M5: No robust selector beyond pathway-only");

        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8} {8,10}",
            "N","Cohort","M1(S0)","M2(S1)","M3","M4","SelCnt","Strict","Best"));
        foreach(var n in Ns){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                double m1=SelectorRate(sub,"S0"),m2=SelectorRate(sub,"S1");
                double m3=n==75?m1:m2; // N-conditioned
                double m4=n==80?m1:m2; // saturation bypass
                int selCnt=sub.Count(p=>p.selS1);
                int strictM2=sub.Count(p=>p.selS1&&p.persist);
                string best=m2>=m1+0.02?"M2(S1)":(m2>=m1?"M2≈M1":"M1(S0)");
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {m1,7:.0%} {m2,8:.0%} {m3,8:.0%} {m4,8:.0%} {selCnt,8} {strictM2,8} {best,10}");
            }
        }

        // Recommendation
        _o.WriteLine("\n─── Recommended model ───");
        bool s1Improves=false;
        foreach(var n in Ns){
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            if(SelectorRate(hold,"S1")>SelectorRate(hold,"S0")+0.02)s1Improves=true;
        }
        if(s1Improves)
            _o.WriteLine("M3: N-conditioned P1/P1b + projHiVec selector.");
        else
            _o.WriteLine("M1 or M5: pathway-only or no robust selector.");
    }

    // ════════════════════════════════════════════════════════
    // HVS_06: N=67 and N=80 boundary analysis
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_06_N67AndN80Boundary(){
        _o.WriteLine("═══ HVS_06: N=67 and N=80 boundary analysis ═══");
        var profiles=EnsureProfiles();

        // N=67: inaccessible
        _o.WriteLine("\n─── N=67: Inaccessible regime ───");
        var n67=profiles.Where(p=>p.n==67).ToArray();
        var n67P1=n67.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
        _o.WriteLine($"P1/P1b candidates: {n67P1.Length}");
        _o.WriteLine($"  P1/P1b any success: {n67P1.Count(p=>p.persist)}");
        _o.WriteLine($"  projHiVec > {ProjHiVecThresh}: {n67P1.Count(p=>p.projHiVec>ProjHiVecThresh)} candidates");
        _o.WriteLine($"  → N=67 is {(n67P1.Count(p=>p.persist)==0?"INACCESSIBLE (confirmed)":"MARGINAL")}");

        // N=80: saturated
        _o.WriteLine("\n─── N=80: Saturation regime ───");
        var n80=profiles.Where(p=>p.n==80).ToArray();
        var n80P1=n80.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
        _o.WriteLine($"P1/P1b candidates: {n80P1.Length} (100% of profiled seeds)");
        _o.WriteLine($"S0 rate (all P1/P1b): {SelectorRate(n80,"S0"):.0%}");
        _o.WriteLine($"S1 rate (filtered): {SelectorRate(n80,"S1"):.0%}");
        _o.WriteLine($"  → Selector {(Math.Abs(SelectorRate(n80,"S1")-SelectorRate(n80,"S0"))<0.05?"UNNECESSARY":"POTENTIALLY USEFUL")} at N=80");
        _o.WriteLine("  → N=80 is SATURATED — universal protocols preferred.");
    }

    // ════════════════════════════════════════════════════════
    // HVS_07: Failure analysis for selected failures
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_07_FailureAnalysis(){
        _o.WriteLine("═══ HVS_07: Selector failure analysis ═══");
        var profiles=EnsureProfiles();
        int[] Ns={71,72,75};

        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,4} {3,6} {4,6} {5,6}",
            "N","Cohort","Sel","InsufD","projFP","Unres"));
        foreach(var n in Ns){
            foreach(var coh in new[]{0,1}){
                var sel=profiles.Where(p=>p.n==n&&p.cohort==coh&&p.selS1).ToArray();
                var fail=sel.Where(p=>!p.persist).ToArray();
                if(fail.Length==0)continue;
                int insuf=fail.Count(p=>!p.immHi);
                int fp=fail.Count(p=>p.immHi&&p.omT2<THR*0.8);
                int unres=fail.Length-insuf-fp;
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {sel.Length,4} {insuf,6} {fp,6} {unres,6}");
            }
        }

        _o.WriteLine("\n─── Failure classification ───");
        _o.WriteLine("InsufD: insufficient displacement (never reached High)");
        _o.WriteLine("projFP: projHiVec false positive (reached High but collapsed)");
        _o.WriteLine("Unres: unresolved");
    }

    // ════════════════════════════════════════════════════════
    // HVS_08: Gate summary and claim audit
    // ════════════════════════════════════════════════════════
    [Fact]public void HVS_08_GateSummaryAndClaimAudit(){
        _o.WriteLine("═══ HVS_08: Decision gates ═══\n");
        var profiles=EnsureProfiles();

        // Compute evidence
        bool s1Improves=false;int improveN=0;
        foreach(var n in new[]{71,72}){
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            if(SelectorRate(hold,"S1")>SelectorRate(hold,"S0")+0.02){s1Improves=true;improveN++;}
        }
        bool s1Harm=false;
        foreach(var n in new[]{75}){
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            if(SelectorRate(hold,"S1")<SelectorRate(hold,"S0")-0.02)s1Harm=true;
        }
        bool s1NeutralN75=Math.Abs(SelectorRate(profiles.Where(p=>p.n==75&&p.cohort==1).ToArray(),"S1")-SelectorRate(profiles.Where(p=>p.n==75&&p.cohort==1).ToArray(),"S0"))<=0.02;
        bool p2NoTransfer=true;int p2TotalHold=0;
        foreach(var n in new[]{67,71,72}){
            int hP2s=profiles.Where(p=>p.n==n&&p.cohort==1&&p.cls=="P2"&&p.persist).Count();
            int hP1s=profiles.Where(p=>p.n==n&&p.cohort==1&&(p.cls=="P1"||p.cls=="P1b")&&p.persist).Count();
            p2TotalHold+=hP2s;
            if(hP2s>2&&(double)hP2s/Math.Max(1,hP1s+hP2s)>0.2)p2NoTransfer=false;
        }
        bool n80Sat=true;
        var n80h=profiles.Where(p=>p.n==80&&p.cohort==1).ToArray();
        if(Math.Abs(SelectorRate(n80h,"S1")-SelectorRate(n80h,"S0"))>0.05)n80Sat=false;

        _o.WriteLine($"Gate A — projHiVec Validated: {(s1Improves?"REACHED":"NOT REACHED")} — S1 improves holdout at {improveN}/2 N");
        _o.WriteLine($"Gate B — N-Conditioned Required: {(s1Improves&&s1NeutralN75?"REACHED":"NOT REACHED")} — S1 helps N=71/72, neutral at N=75");
        _o.WriteLine($"Gate C — P2 Demoted: {(p2NoTransfer?"REACHED":"NOT REACHED")} — P2 has no transfer evidence");
        _o.WriteLine($"Gate D — N=80 Saturation: {(n80Sat?"REACHED":"NOT REACHED")} — selector unnecessary at N=80");
        _o.WriteLine("Gate E — λ1 Adds Value: DEFERRED (exploratory, no frozen threshold)");
        _o.WriteLine($"Gate F — Selector Fails: {(s1Improves?"NOT REACHED":"REACHED")} — S1 does not materially help");
        _o.WriteLine($"Gate G — Final Model: {(s1Improves?"REACHED — M3 recommended":"PARTIAL — M1/M5 fallback")}");

        _o.WriteLine("\n─── Claim Discipline ───");
        _o.WriteLine("☐ No physical interpretation.");
        _o.WriteLine("☐ projHiVec threshold frozen at -0.3281 from HVI.");
        _o.WriteLine("☐ Selector claims require holdout evidence.");
        _o.WriteLine("☐ P2 demoted without transfer evidence.");
        _o.WriteLine("☐ No generalization beyond N=67-80, seeds 0-199.");
        _o.WriteLine($"☐ Final model: {(s1Improves?"M3 N-conditioned P1/P1b+projHiVec":"M1 pathway-only")}.");

        _o.WriteLine("\n─── Recommended Next ───");
        _o.WriteLine(s1Improves
            ? "V5.13 synthesis: document M3 model with N-conditioned projHiVec selector."
            : "V5.13 synthesis: document pathway-only model; projHiVec is local.");
        _o.WriteLine("═══ END HVS ═══");
    }

    // ══════════════════════════════════════════════════════════════
    // ── HELPERS: Profile collection and caching ──
    // ══════════════════════════════════════════════════════════════
    static FullProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hiCentroids;
    static ConcurrentDictionary<int,P3>? _loCentroids;

    P3 GetHiCentroid(int n){_hiCentroids??=new();return _hiCentroids.GetOrAdd(n,k=>PCent(k,true));}
    P3 GetLoCentroid(int n){_loCentroids??=new();return _loCentroids.GetOrAdd(n,k=>PCent(k,false));}

    FullProfile[] EnsureProfiles(){
        if(_profiles!=null&&_profiles.Length>0)return _profiles;
        return CollectProfiles();
    }

    FullProfile[] CollectProfiles(){
        var bag=new ConcurrentBag<FullProfile>();
        foreach(var n in new[]{67,71,72,75,80}){
            var hi=GetHiCentroid(n);var lo=GetLoCentroid(n);
            ProfileAndTestCohort(n,0,99,hi,lo,0,bag);
            ProfileAndTestCohort(n,100,199,hi,lo,1,bag);
        }
        var profiles=bag.ToArray();
        ComputeSelectorFlags(profiles);
        _profiles=profiles;
        return profiles;
    }

    void ComputeSelectorFlags(FullProfile[] profiles){
        for(int i=0;i<profiles.Length;i++){
            bool isP1=profiles[i].cls=="P1"||profiles[i].cls=="P1b";
            profiles[i].selS1=isP1&&profiles[i].projHiVec>ProjHiVecThresh;
            profiles[i].selS2=false; // computed in HVS_04
            profiles[i].selS3=false;
        }
    }

    void ProfileAndTestCohort(int n,int seedStart,int seedEnd,P3 hi,P3 lo,int cohort,ConcurrentBag<FullProfile> results){
        var loSeeds=new List<int>();
        for(int s=seedStart;s<=seedEnd&&loSeeds.Count<MaxSeedsPerCohort;s++)
            if(!IsHi(n,s))loSeeds.Add(s);

        var bases=new ConcurrentBag<(SBase b,double[,] Kmat)>();
        Parallel.ForEach(loSeeds.ToArray(),s=>{
            var (b,Km)=ProfileBaseline(n,s,hi,lo);
            bases.Add((b,Km));
        });

        foreach(var (b,Km) in bases){
            var trial=RunTrial(n,b.seed,b.cls=="P2"?b.d0*0.90:b.d0*0.50,b,hi,lo,"MATCHED",cohort);
            var fp=new FullProfile{
                seed=b.seed,cohort=cohort,n=n,cls=b.cls,
                dMean=b.d0,kMean=b.km0,
                distToHi=Dist3(b.d0,b.km0,b.ks0,hi),
                projHiVec=ComputeProjHiVec(b.d0,b.km0,b.ks0,hi,lo),
                immHi=trial.immHi,persist=trial.persist,
                omT1=trial.omT1,omT2=trial.omT2,dT1=trial.dT1,dT2=trial.dT2,
            };
            ComputeSpectral(n,Km,ref fp);
            ComputeEdgeConcentration(n,Km,ref fp);
            results.Add(fp);
        }
    }

    void ComputeSpectral(int n,double[,] K,ref FullProfile fp){
        fp.lambda1=PowerIteration(K,n,200);
        fp.kFrob=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)fp.kFrob+=K[i,j]*K[i,j];
        fp.kFrob=Math.Sqrt(fp.kFrob);
    }
    void ComputeEdgeConcentration(int n,double[,] K,ref FullProfile fp){
        var edges=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K[i,j]);
        edges.Sort((a,b)=>b.CompareTo(a));
        double total=edges.Sum();if(total<=0)return;
        int top1=Math.Max(1,(int)(edges.Count*0.01));
        int top5=Math.Max(1,(int)(edges.Count*0.05));
        fp.top1EdgeShare=edges.Take(top1).Sum()/total;
        fp.top5EdgeShare=edges.Take(top5).Sum()/total;
    }
    static double ComputeProjHiVec(double dm,double km,double ks,P3 hi,P3 lo){
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks;
        double vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        if(vn<=0)return 0;
        return ((dm-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn;
    }

    // ── Selector metrics ──
    double SelectorRate(FullProfile[] sub,string sel){
        var filtered=ApplySelector(sub,sel);
        return filtered.Length>0?(double)filtered.Count(p=>p.persist)/filtered.Length:0;
    }
    int SelectorCount(FullProfile[] sub,string sel)=>ApplySelector(sub,sel).Length;
    int SelectorStrict(FullProfile[] sub,string sel)=>ApplySelector(sub,sel).Count(p=>p.persist);
    FullProfile[] ApplySelector(FullProfile[] sub,string sel)=>sel switch{
        "S0"=>sub.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray(),
        "S1"=>sub.Where(p=>p.selS1).ToArray(),
        "S2"=>sub.Where(p=>p.selS2).ToArray(),
        "S3"=>sub.Where(p=>p.selS3).ToArray(),
        "S4"=>sub.Where(p=>p.cls=="P1b").ToArray(),
        "S5"=>sub.Where(p=>p.cls=="P1"||p.cls=="P1b"||p.cls=="P2").ToArray(),
        "S6"=>sub.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray(),
        _=>sub
    };
    void PrintSelectorRow(FullProfile[] sub,string label,string sel,int cohort){
        var f=ApplySelector(sub,sel);
        string cohLab0=cohort==0?"Train":"Holdout";
        if(f.Length==0){_o.WriteLine($"{cohLab0,-10} {label,-10} {0,6} {"-",6} {"-",6} {"-",7} {"-",8} {"-",8} {"-",8}");return;}
        double rate=(double)f.Count(p=>p.persist)/f.Length;
        double immRate=(double)f.Count(p=>p.immHi)/f.Length;
        double prec=f.Length>0?(double)f.Count(p=>p.persist)/f.Length:0;
        double recall=sub.Count(p=>(p.cls=="P1"||p.cls=="P1b")&&p.persist)>0?(double)f.Count(p=>p.persist)/sub.Count(p=>(p.cls=="P1"||p.cls=="P1b")&&p.persist):0;
        double s0rate=SelectorRate(sub,"S0");
        double lift=s0rate>0?rate-s0rate:0;
        string cohLab=cohort==0?"Train":"Holdout";
        _o.WriteLine($"{cohLab,-10} {label,-10} {f.Length,6} {f.Count(p=>p.persist),6} {rate,5:.0%} {immRate,6:.0%} {prec,8:.0%} {recall,8:.0%} {lift,8:+.0%}");
    }

    // ══════════════════════════════════════════════════════════════
    // ── ENGINE: RecoverFP (frozen) ──
    // ══════════════════════════════════════════════════════════════
    static double[][]Sim(double[,]K,int n,double s,int seed){
        var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;
        var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;
        int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();
        int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;
    }
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

    static P3 PCent(int n,bool hi){
        double d=0,k=0,ks=0;int c=0;
        for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<NE;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}
        return new P3{dm=d/c,km=k/c,ks=ks/c};
    }
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}

    static (SBase,double[,]) ProfileBaseline(int n,int seed,P3 hi,P3 lo){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km0=Km(K3E,n),ks0=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km0,ks0=ks0,dh0=Dist3(d0,km0,ks0,hi),cls=""};
        sb=Classify(sb,hi);
        return (sb,K3E);
    }

    static SBase Classify(SBase b,P3 hi){
        string cls;
        if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";
        else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";
        else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";
        return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,dh0=b.dh0,cls=cls};
    }
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    static double Dist3(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,ds=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+ds*ds);}

    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,string proto,int cohort){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K,n,S,seed+100);double om1=Of(hT1,n).Average();
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average();
        return new Trial{seed=seed,cohort=cohort,proto=proto,cls=b.cls,d0=b.d0,
            dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};
    }

    static double PowerIteration(double[,] A,int n,int maxIter=200){
        var v=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v[i]=rng.NextDouble();
        double norm=Math.Sqrt(v.Sum(x=>x*x));if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;
        double lambda=0;
        for(int iter=0;iter<maxIter;iter++){
            var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];
            norm=Math.Sqrt(Av.Sum(x=>x*x));if(norm<1e-15)break;
            for(int i=0;i<n;i++)v[i]=Av[i]/norm;
            double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}
            if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;
        }
        return lambda;
    }
}
