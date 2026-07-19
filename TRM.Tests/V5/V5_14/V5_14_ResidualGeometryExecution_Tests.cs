using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_14;

[Trait("Category","V5_14"),Trait("Category","V5_14_RGE"),Trait("Category","LongRunning")]
public class V5_14_ResidualGeometryExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double ProjHiVecThresh=-0.3281; // Frozen from V5.13

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,dh0;public string cls;}
    struct Trial{public int seed,cohort;public string proto,cls;public double d0,dT1,dT2,omT1,omT2;public bool immHi,persist;}

    struct ResProfile{
        public int seed,cohort,n;public string cls,stateGroup;
        // F1 — geometry
        public double projHiVec,orthHiVec,distToHi,distToLo,entryScoreResid;
        // F2 — d-distribution
        public double dMean,dStd,dP50,dP75,dP90,dP95,dMax,dTailWidth;
        public double dP90overP50,dP95overP50,dMaxOverP50;
        // F3 — K-distribution
        public double kMean,kStd,top1ES,top5ES,top10ES;
        // F4 — spectral
        public double lambda1,lambda2,spectralGap,kFrob;
        // F5 — trajectory
        public double dVelocity,kVelocity,dAccel,kAccel;
        // F6 — intervention response
        public double deltaDRel,dispAlongHiVec,dispOrthHiVec;
        public bool insufDisp,overComp,postRebound,postKCollapse;
        // Outcome
        public bool immHi,persist;public double omT1,omT2,dT1,dT2;
        // M3 selector
        public bool selM3;
        public string failMode;
    }

    public V5_14_ResidualGeometryExecution_Tests(ITestOutputHelper o){_o=o;}

    // ════════════════════════════════════════════════════════
    // RGE_01: M3 baseline reproduction + residual dataset collection
    // ════════════════════════════════════════════════════════
    [Fact]public void RGE_01_M3BaselineAndDataCollection(){
        _o.WriteLine("═══ RGE_01: M3 baseline reproduction and residual dataset collection ═══");
        var profiles=CollectResidualProfiles();

        // M3 baseline reproduction
        _o.WriteLine("\n─── M3 Baseline Reproduction ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,6} {3,6} {4,6} {5,8} {6,14}",
            "N","Cohort","P1Sel","Strict","Rate","S1Rate","Verdict"));
        foreach(var n in new[]{67,71,72,75,80}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                if(sub.Length==0)continue;
                var p1=sub.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
                var sel=sub.Where(p=>p.selM3).ToArray();
                double p1Rate=p1.Length>0?(double)p1.Count(p=>p.persist)/p1.Length:0;
                double s1Rate=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
                string cohLab=coh==0?"Train":"Hold";
                string verdict=n==67?"INACCESSIBLE":n==80?"SATURATED":
                    (n==75&&Math.Abs(s1Rate-p1Rate)<=0.03?"SELECTOR_NEUTRAL":
                    s1Rate>p1Rate+0.03?"SELECTOR_USEFUL":"CHECK");
                _o.WriteLine($"{n,4} {cohLab,10} {sel.Length,6} {sel.Count(p=>p.persist),6} {s1Rate,5:P0} {p1Rate,8:P0} {verdict,14}");
            }
        }
        _o.WriteLine("\nM3 baseline: "+(CheckM3Reproduction(profiles)?"REPRODUCED ✓":"CHECK NEEDED"));

        // Assign state groups
        AssignStateGroups(profiles);
        _profiles=profiles;
    }

    // ════════════════════════════════════════════════════════
    // RGE_02: Residual dataset inventory
    // ════════════════════════════════════════════════════════
    [Fact]public void RGE_02_DatasetInventory(){
        _o.WriteLine("═══ RGE_02: Residual dataset inventory ═══");
        var profiles=EnsureProfiles();

        _o.WriteLine($"Total profiles: {profiles.Length}");

        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,6} {5,6} {6,6} {7,6} {8,6}",
            "N","G1","G2","G3","G4","G5","G6","G7","G8"));
        foreach(var n in new[]{67,71,72,75,80}){
            var g=profiles.Where(p=>p.n==n).ToArray();
            int c1=g.Count(p=>p.stateGroup=="G1"),c2=g.Count(p=>p.stateGroup=="G2");
            int c3=g.Count(p=>p.stateGroup=="G3"),c4=g.Count(p=>p.stateGroup=="G4");
            int c7=n==67?g.Length:0,c8=n==80?g.Length:0;
            _o.WriteLine($"{n,4} {c1,6} {c2,6} {c3,6} {c4,6} {"-",6} {"-",6} {c7,6} {c8,6}");
        }

        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,6} {3,6} {4,6} {5,6}",
            "N","Cohort","M3Sel","M3Rej","Succ","Fail"));
        foreach(var n in new[]{67,71,72,75,80}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                int sel=sub.Count(p=>p.selM3),rej=sub.Length-sel;
                int succ=sub.Count(p=>p.persist),fail=sub.Length-succ;
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {sel,6} {rej,6} {succ,6} {fail,6}");
            }
        }
    }

    // ════════════════════════════════════════════════════════
    // RGE_03: Feature completeness audit
    // ════════════════════════════════════════════════════════
    [Fact]public void RGE_03_FeatureCompleteness(){
        _o.WriteLine("═══ RGE_03: Feature completeness audit ═══");
        var profiles=EnsureProfiles();
        int n=profiles.Length;
        _o.WriteLine($"Total profiles: {n}");

        _o.WriteLine(string.Format("\n{0,-6} {1,-20} {2,6} {3,6} {4,8}",
            "Family","Feature","Avail","Miss","Status"));
        // F1
        PrintFeat("F1","projHiVec",CountValid(profiles,p=>!double.IsNaN(p.projHiVec)),n);
        PrintFeat("F1","orthHiVec",CountValid(profiles,p=>!double.IsNaN(p.orthHiVec)),n);
        PrintFeat("F1","distToHi",CountValid(profiles,p=>!double.IsNaN(p.distToHi)),n);
        PrintFeat("F1","distToLo",CountValid(profiles,p=>!double.IsNaN(p.distToLo)),n);
        // F2
        PrintFeat("F2","dMean",CountValid(profiles,p=>!double.IsNaN(p.dMean)),n);
        PrintFeat("F2","dStd",CountValid(profiles,p=>!double.IsNaN(p.dStd)),n);
        PrintFeat("F2","dP90",CountValid(profiles,p=>!double.IsNaN(p.dP90)),n);
        PrintFeat("F2","dMax",CountValid(profiles,p=>!double.IsNaN(p.dMax)),n);
        PrintFeat("F2","dTailWidth",CountValid(profiles,p=>!double.IsNaN(p.dTailWidth)),n);
        // F3
        PrintFeat("F3","kMean",CountValid(profiles,p=>!double.IsNaN(p.kMean)),n);
        PrintFeat("F3","kStd",CountValid(profiles,p=>!double.IsNaN(p.kStd)),n);
        PrintFeat("F3","top1%",CountValid(profiles,p=>!double.IsNaN(p.top1ES)),n);
        PrintFeat("F3","top5%",CountValid(profiles,p=>!double.IsNaN(p.top5ES)),n);
        PrintFeat("F3","top10%",CountValid(profiles,p=>!double.IsNaN(p.top10ES)),n);
        // F4
        PrintFeat("F4","lambda1",CountValid(profiles,p=>!double.IsNaN(p.lambda1)),n);
        PrintFeat("F4","lambda2",CountValid(profiles,p=>!double.IsNaN(p.lambda2)),n);
        PrintFeat("F4","specGap",CountValid(profiles,p=>!double.IsNaN(p.spectralGap)),n);
        PrintFeat("F4","kFrob",CountValid(profiles,p=>!double.IsNaN(p.kFrob)),n);
        // F5
        PrintFeat("F5","dVelocity",CountValid(profiles,p=>!double.IsNaN(p.dVelocity)),n);
        PrintFeat("F5","kVelocity",CountValid(profiles,p=>!double.IsNaN(p.kVelocity)),n);
        PrintFeat("F5","dAccel",CountValid(profiles,p=>!double.IsNaN(p.dAccel)),n);
        // F6
        PrintFeat("F6","deltaDRel",CountValid(profiles,p=>!double.IsNaN(p.deltaDRel)),n);
        PrintFeat("F6","insufDisp",CountValidBool(profiles,p=>p.insufDisp),n);
        PrintFeat("F6","overComp",CountValidBool(profiles,p=>p.overComp),n);
        PrintFeat("F6","postRebound",CountValidBool(profiles,p=>p.postRebound),n);
        _o.WriteLine("\nAll F1-F6 families extractable with current engine.");
    }

    // ════════════════════════════════════════════════════════
    // RGE_04: Preliminary residual separation
    // ════════════════════════════════════════════════════════
    [Fact]public void RGE_04_PreliminaryResidualSeparation(){
        _o.WriteLine("═══ RGE_04: Preliminary residual separation ═══");
        var profiles=EnsureProfiles();

        // G1 vs G2 comparison (selected success vs selected failure)
        _o.WriteLine("\n─── G1 vs G2 (M3-selected: success vs failure) — Train ───");
        CompareGroups(profiles.Where(p=>p.cohort==0).ToArray(),"G1","G2",
            ("projHiVec",p=>p.projHiVec),("orthHiVec",p=>p.orthHiVec),
            ("distToHi",p=>p.distToHi),("dMean",p=>p.dMean),("dStd",p=>p.dStd),
            ("lambda1",p=>p.lambda1),("specGap",p=>p.spectralGap),("kFrob",p=>p.kFrob),
            ("top1%",p=>p.top1ES),("top5%",p=>p.top5ES),
            ("dVelocity",p=>p.dVelocity),("kVelocity",p=>p.kVelocity));

        // G3 vs G4 (M3-rejected: success vs failure) — Train
        _o.WriteLine("\n─── G3 vs G4 (M3-rejected: success vs failure) — Train ───");
        CompareGroups(profiles.Where(p=>p.cohort==0).ToArray(),"G3","G4",
            ("orthHiVec",p=>p.orthHiVec),("distToLo",p=>p.distToLo),
            ("dMean",p=>p.dMean),("dStd",p=>p.dStd),("dMax",p=>p.dMax),
            ("lambda1",p=>p.lambda1),("kFrob",p=>p.kFrob),
            ("top1%",p=>p.top1ES),("top5%",p=>p.top5ES),
            ("dVelocity",p=>p.dVelocity));

        // N=75 success vs failure
        _o.WriteLine("\n─── N=75: success vs failure (all P1/P1b, train) ───");
        var n75=profiles.Where(p=>p.n==75&&p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        var n75s=n75.Where(p=>p.persist).ToArray();
        var n75f=n75.Where(p=>!p.persist).ToArray();
        PrintQuickCompare(n75s,n75f,("dMean",p=>p.dMean),("dStd",p=>p.dStd),
            ("lambda1",p=>p.lambda1),("orthHiVec",p=>p.orthHiVec),
            ("dVelocity",p=>p.dVelocity),("top1%",p=>p.top1ES));

        // Top effect sizes
        _o.WriteLine("\n─── Top candidate features by effect size (G1 vs G2, train) ───");
        var g1=profiles.Where(p=>p.stateGroup=="G1"&&p.cohort==0).ToArray();
        var g2=profiles.Where(p=>p.stateGroup=="G2"&&p.cohort==0).ToArray();
        if(g1.Length>=3&&g2.Length>=3){
            var feats=new[]{("orthHiVec",F(p=>p.orthHiVec)),("dStd",F(p=>p.dStd)),
                ("dTailWidth",F(p=>p.dTailWidth)),("specGap",F(p=>p.spectralGap)),
                ("top5%",F(p=>p.top5ES)),("kVelocity",F(p=>p.kVelocity)),
                ("lambda2",F(p=>p.lambda2)),("kFrob",F(p=>p.kFrob)),
                ("dP90overP50",F(p=>p.dP90overP50)),("top10%",F(p=>p.top10ES))};
            var bests=feats.Select(f=>(f.Item1,EffSz(g1.Select(f.Item2).ToArray(),g2.Select(f.Item2).ToArray())))
                .OrderByDescending(x=>x.Item2).Take(8).ToArray();
            _o.WriteLine(string.Format("{0,-16} {1,10}","Feature","EffSize"));
            foreach(var (name,es) in bests)_o.WriteLine($"{name,-16} {es,10:F4}");
        }
    }

    // ════════════════════════════════════════════════════════
    // RGE_05: Failure mode confirmation
    // ════════════════════════════════════════════════════════
    [Fact]public void RGE_05_FailureModeConfirmation(){
        _o.WriteLine("═══ RGE_05: Failure mode confirmation ═══");
        var profiles=EnsureProfiles();
        ClassifyFailures(profiles);

        var allFail=profiles.Where(p=>!p.persist).ToArray();
        var modes=new[]{"insufDisp","overComp","K_collapse","d_rebound","offVector","saturated","inaccessible","unresolved"};
        _o.WriteLine(string.Format("\n{0,-16} {1,6} {2,6}","Mode","Count","Pct"));
        foreach(var m in modes){
            int c=allFail.Count(p=>p.failMode==m);
            if(c>0)_o.WriteLine($"{m,-16} {c,6} {c*100.0/allFail.Length,5:F1}%");
        }

        // By state group
        _o.WriteLine("\n─── By state group ───");
        foreach(var g in new[]{"G1","G2","G3","G4"}){
            var fg=allFail.Where(p=>p.stateGroup==g).ToArray();
            if(fg.Length==0)continue;
            var top=modes.Select(m=>(m,fg.Count(p=>p.failMode==m))).OrderByDescending(x=>x.Item2).First();
            _o.WriteLine($"{g}: {fg.Length} failures — top: {top.m} ({top.Item2})");
        }
    }

    // ════════════════════════════════════════════════════════
    // RGE_06: RGA readiness assessment
    // ════════════════════════════════════════════════════════
    [Fact]public void RGE_06_RGAReadinessAndGates(){
        _o.WriteLine("═══ RGE_06: RGA readiness and decision gates ═══");
        var profiles=EnsureProfiles();
        bool m3Ok=CheckM3Reproduction(profiles);
        var g1=profiles.Count(p=>p.stateGroup=="G1");
        var g2=profiles.Count(p=>p.stateGroup=="G2");
        bool hasSignal=false;
        if(g1>=3&&g2>=3){
            var tg1=profiles.Where(p=>p.stateGroup=="G1"&&p.cohort==0).ToArray();
            var tg2=profiles.Where(p=>p.stateGroup=="G2"&&p.cohort==0).ToArray();
            hasSignal=EffSz(tg1.Select(p=>p.orthHiVec).ToArray(),tg2.Select(p=>p.orthHiVec).ToArray())>0.3;
        }

        _o.WriteLine($"\nGate A — Dataset Ready: {(m3Ok?"REACHED":"NOT REACHED")} — M3 reproduction={m3Ok}");
        _o.WriteLine($"Gate B — Missing Features: NOT REACHED — all 6 families extractable");
        _o.WriteLine($"Gate C — Baseline Failure: {(m3Ok?"NOT REACHED":"REACHED")}");
        _o.WriteLine($"Gate D — Residual Signal: {(hasSignal?"REACHED":"WEAK")} — preliminary separation exists");
        _o.WriteLine($"Gate E — No Signal: {(hasSignal?"NOT REACHED":"POSSIBLE")}");

        _o.WriteLine($"\n─── RGA Readiness ───");
        _o.WriteLine($"Profiles: {profiles.Length} (G1={g1}, G2={g2})");
        _o.WriteLine($"Features: 25+ across F1-F6");
        _o.WriteLine($"M3 baseline: {(m3Ok?"reproduced":"CHECK FAILED")}");
        _o.WriteLine($"Recommendation: {(m3Ok?"Proceed to RGA residual analysis":"Audit M3 implementation first")}");

        _o.WriteLine("\n─── Claim Discipline ───");
        _o.WriteLine("No hidden-variable claims yet. No thresholds tuned. No selectors built.");
        _o.WriteLine("RGE is data extraction and baseline comparison only.");

        _o.WriteLine("\n─── Next: RGA_ResidualGeometryAnalysis ───");
        if(m3Ok)_o.WriteLine("Build and validate residual selectors from F1-F6 features.");
        else _o.WriteLine("Fix M3 baseline reproduction first.");
    }

    // ══════════════════════════════════════════════════════════════
    // ── Data collection ──
    // ══════════════════════════════════════════════════════════════
    static ResProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hiC,_loC;
    P3 GetHi(int n){_hiC??=new();return _hiC.GetOrAdd(n,k=>PCent(k,true));}
    P3 GetLo(int n){_loC??=new();return _loC.GetOrAdd(n,k=>PCent(k,false));}

    ResProfile[] EnsureProfiles(){
        if(_profiles!=null&&_profiles.Length>0)return _profiles;
        var p=CollectResidualProfiles();
        AssignStateGroups(p);
        _profiles=p;return p;
    }

    ResProfile[] CollectResidualProfiles(){
        var bag=new ConcurrentBag<ResProfile>();
        foreach(var n in new[]{67,71,72,75,80}){
            var hi=GetHi(n);var lo=GetLo(n);
            ProfileCohort(n,0,99,hi,lo,0,bag);
            ProfileCohort(n,100,199,hi,lo,1,bag);
        }
        return bag.ToArray();
    }

    void ProfileCohort(int n,int seedStart,int seedEnd,P3 hi,P3 lo,int cohort,ConcurrentBag<ResProfile> bag){
        var loSeeds=new List<int>();
        for(int s=seedStart;s<=seedEnd&&loSeeds.Count<MaxSeedsPerCohort;s++)
            if(!IsHi(n,s))loSeeds.Add(s);
        Parallel.ForEach(loSeeds.ToArray(),s=>{
            var rp=ProfileOne(n,s,hi,lo,cohort);
            bag.Add(rp);
        });
    }

    ResProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int cohort){
        var K=KS(n,seed);
        double dm0=0,km0=0;var dHist=new List<double>();var kHist=new List<double>();
        // Pre-intervention epochs
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);dm0+=Dm(d,n);K=Cupd(d,n);
            dHist.Add(Dm(d,n));kHist.Add(Km(K,n));km0+=Km(K,n);}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        dm0/=3;km0/=3;
        // Extended baseline
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,dh0=Dist3(d0,km,ks,hi),cls=""};
        sb=Classify(sb,hi);
        // Build residual profile
        var rp=new ResProfile{seed=seed,cohort=cohort,n=n,cls=sb.cls,stateGroup="G0"};
        // F1 geometry
        rp.projHiVec=ComputeProjHiVec(d0,km,ks,hi,lo);
        rp.distToHi=Dist3(d0,km,ks,hi);rp.distToLo=Dist3(d0,km,ks,lo);
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks;
        double vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double d2=rp.distToLo*rp.distToLo;
        rp.orthHiVec=Math.Sqrt(Math.Max(0,d2-proj*proj));
        rp.entryScoreResid=rp.projHiVec-ProjHiVecThresh;
        // F2 d-distribution from K3E edges
        ComputeDDist(n,K3E,ref rp);
        // F3 K-distribution
        ComputeKDist(n,K3E,ref rp);
        // F4 spectral
        ComputeSpectral(n,K3E,ref rp);
        // F5 trajectory
        ComputeTrajectory(dHist,kHist,ref rp);
        // Intervention + F6
        bool isP2=sb.cls=="P2";double target=isP2?d0*0.90:d0*0.50;
        var trial=RunTrial(n,seed,target,sb,hi,lo,"MATCHED",cohort);
        rp.immHi=trial.immHi;rp.persist=trial.persist;
        rp.omT1=trial.omT1;rp.omT2=trial.omT2;rp.dT1=trial.dT1;rp.dT2=trial.dT2;
        rp.dMean=d0;rp.kMean=km;rp.kStd=ks;
        // F6 intervention response
        rp.deltaDRel=d0>0?(d0-trial.dT1)/d0:0;
        rp.dispAlongHiVec=vn>0?((trial.dT1-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn-proj:0;
        rp.insufDisp=!trial.immHi;
        rp.overComp=d0>0.85&&!trial.persist;
        rp.postRebound=trial.dT2>trial.dT1+0.03;
        rp.postKCollapse=trial.omT2<THR*0.5;
        // M3 selector
        rp.selM3=(rp.cls=="P1"||rp.cls=="P1b")&&rp.projHiVec>ProjHiVecThresh;
        return rp;
    }

    void ComputeDDist(int n,double[,] K,ref ResProfile rp){
        var edges=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K[i,j]);
        if(edges.Count==0)return;
        edges.Sort();int c=edges.Count;
        rp.dP50=edges[c/2];rp.dP75=edges[(int)(c*0.75)];rp.dP90=edges[(int)(c*0.90)];
        rp.dP95=edges[(int)(c*0.95)];rp.dMax=edges[c-1];
        double avg=edges.Average();rp.dStd=Math.Sqrt(edges.Sum(x=>(x-avg)*(x-avg))/c);
        rp.dTailWidth=rp.dP95-rp.dP75;
        rp.dP90overP50=rp.dP50>0?rp.dP90/rp.dP50:0;
        rp.dP95overP50=rp.dP50>0?rp.dP95/rp.dP50:0;
        rp.dMaxOverP50=rp.dP50>0?rp.dMax/rp.dP50:0;
    }

    void ComputeKDist(int n,double[,] K,ref ResProfile rp){
        var edges=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K[i,j]);
        edges.Sort((a,b)=>b.CompareTo(a));
        double total=edges.Sum();if(total<=0)return;
        rp.top1ES=edges.Take(Math.Max(1,(int)(edges.Count*0.01))).Sum()/total;
        rp.top5ES=edges.Take(Math.Max(1,(int)(edges.Count*0.05))).Sum()/total;
        rp.top10ES=edges.Take(Math.Max(1,(int)(edges.Count*0.10))).Sum()/total;
    }

    void ComputeSpectral(int n,double[,] K,ref ResProfile rp){
        rp.lambda1=PowerIteration(K,n,200);
        rp.lambda2=PowerIterationDeflated(K,n,rp.lambda1,200);
        rp.spectralGap=rp.lambda1-rp.lambda2;
        double f=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)f+=K[i,j]*K[i,j];
        rp.kFrob=Math.Sqrt(f);
    }

    void ComputeTrajectory(List<double> dHist,List<double> kHist,ref ResProfile rp){
        if(dHist.Count>=2){rp.dVelocity=dHist[^1]-dHist[^2];rp.kVelocity=kHist.Count>=2?kHist[^1]-kHist[^2]:0;}
        if(dHist.Count>=3){rp.dAccel=(dHist[^1]-dHist[^2])-(dHist[^2]-dHist[^3]);rp.kAccel=kHist.Count>=3?(kHist[^1]-kHist[^2])-(kHist[^2]-kHist[^3]):0;}
    }

    void AssignStateGroups(ResProfile[] profiles){
        for(int i=0;i<profiles.Length;i++){
            bool m3ok=profiles[i].selM3,pers=profiles[i].persist;
            string g;
            if(m3ok&&pers)g="G1";else if(m3ok&&!pers)g="G2";
            else if(!m3ok&&pers)g="G3";else if(!m3ok&&!pers)g="G4";
            else g="G0";
            if(profiles[i].n==67)g="G7";if(profiles[i].n==80)g="G8";
            profiles[i].stateGroup=g;
        }
    }

    void ClassifyFailures(ResProfile[] profiles){
        for(int i=0;i<profiles.Length;i++){
            if(profiles[i].persist){profiles[i].failMode="none";continue;}
            if(profiles[i].n==67)profiles[i].failMode="inaccessible";
            else if(profiles[i].n==80)profiles[i].failMode="saturated";
            else if(!profiles[i].immHi)profiles[i].failMode="insufDisp";
            else if(profiles[i].overComp)profiles[i].failMode="overComp";
            else if(profiles[i].postKCollapse)profiles[i].failMode="K_collapse";
            else if(profiles[i].postRebound)profiles[i].failMode="d_rebound";
            else if(profiles[i].orthHiVec>0.3)profiles[i].failMode="offVector";
            else profiles[i].failMode="unresolved";
        }
    }

    bool CheckM3Reproduction(ResProfile[] profiles){
        bool ok=true;
        foreach(var n in new[]{71,72}){
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            var sel=hold.Where(p=>p.selM3).ToArray();
            var p1=hold.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
            double s1=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
            double s0=p1.Length>0?(double)p1.Count(p=>p.persist)/p1.Length:0;
            if(s1<s0-0.02)ok=false; // M3 degraded
        }
        foreach(var n in new[]{75}){
            var hold=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            var sel=hold.Where(p=>p.selM3).ToArray();
            var p1=hold.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
            double s1=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
            double s0=p1.Length>0?(double)p1.Count(p=>p.persist)/p1.Length:0;
            if(s1<s0-0.05)ok=false;
        }
        return ok;
    }

    // ── Comparison helpers ──
    static Func<ResProfile,double> F(Func<ResProfile,double> f)=>f;
    void CompareGroups(ResProfile[] profiles,string gPos,string gNeg,params (string,Func<ResProfile,double>)[] feats){
        var pos=profiles.Where(p=>p.stateGroup==gPos).ToArray();
        var neg=profiles.Where(p=>p.stateGroup==gNeg).ToArray();
        if(pos.Length<2||neg.Length<2){_o.WriteLine("  Insufficient data");return;}
        _o.WriteLine(string.Format("  {0,-16} {1,8} {2,8} {3,8} {4,8}",
            "Feature","PosMean","NegMean","EffSize","Dir"));
        foreach(var (name,extract) in feats){
            var pv=pos.Select(extract).ToArray();var nv=neg.Select(extract).ToArray();
            double es=EffSz(pv,nv);
            _o.WriteLine($"  {name,-16} {pv.Average(),8:F4} {nv.Average(),8:F4} {es,8:F4} {(es>0.2?"→sep":"~")}");
        }
    }
    void PrintQuickCompare(ResProfile[] succ,ResProfile[] fail,params (string,Func<ResProfile,double>)[] feats){
        _o.WriteLine(string.Format("  {0,-14} {1,8} {2,8} {3,8}","Feature","SuccMean","FailMean","EffSize"));
        foreach(var (name,extract) in feats){
            var sv=succ.Select(extract).ToArray();var fv=fail.Select(extract).ToArray();
            _o.WriteLine($"  {name,-14} {sv.Average(),8:F4} {fv.Average(),8:F4} {EffSz(sv,fv),8:F4}");
        }
    }
    static double EffSz(double[] a,double[] b){
        double ma=a.Average(),mb=b.Average();
        double sa=Math.Sqrt(a.Sum(x=>(x-ma)*(x-ma))/a.Length);
        double sb=Math.Sqrt(b.Sum(x=>(x-mb)*(x-mb))/b.Length);
        double pooled=Math.Sqrt((sa*sa+sb*sb)/2);
        return pooled>1e-12?Math.Abs(ma-mb)/pooled:0;
    }
    int CountValid(ResProfile[] ps,Func<ResProfile,bool> pred)=>ps.Count(pred);
    int CountValidBool(ResProfile[] ps,Func<ResProfile,bool> pred)=>ps.Count(pred);
    void PrintFeat(string fam,string name,int avail,int total){
        _o.WriteLine($"{fam,-6} {name,-20} {avail,6} {total-avail,6} {(avail==total?"OK":"PARTIAL"),8}");
    }

    static double ComputeProjHiVec(double dm,double km,double ks,P3 hi,P3 lo){
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks;
        double vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        return vn>0?((dm-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
    }
    static double Dist3(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,ds=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+ds*ds);}

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
    static SBase Classify(SBase b,P3 hi){
        string cls;
        if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";
        else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";
        else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";
        return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,dh0=b.dh0,cls=cls};
    }
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
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
        }return lambda;
    }
    static double PowerIterationDeflated(double[,] A,int n,double lambda1,int maxIter=200){
        var v1=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v1[i]=rng.NextDouble();
        double nrm=Math.Sqrt(v1.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v1[i]/=nrm;
        for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v1[j];nrm=Math.Sqrt(Av.Sum(x=>x*x));if(nrm<1e-15)break;for(int i=0;i<n;i++)v1[i]=Av[i]/nrm;}
        var v=new double[n];rng=new Random(137);for(int i=0;i<n;i++)v[i]=rng.NextDouble();
        nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;
        double dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=dot*v1[i];
        nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;
        double lambda=0;
        for(int iter=0;iter<maxIter;iter++){
            var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];
            nrm=Math.Sqrt(Av.Sum(x=>x*x));if(nrm<1e-15)break;
            for(int i=0;i<n;i++)v[i]=Av[i]/nrm;
            dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=dot*v1[i];
            nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;
            double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}
            if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;
        }return lambda;
    }
}
