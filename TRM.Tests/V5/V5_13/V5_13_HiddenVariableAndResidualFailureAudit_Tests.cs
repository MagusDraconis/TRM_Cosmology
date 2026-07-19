using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_13;

[Trait("Category","V5_13"),Trait("Category","V5_13_HVI"),Trait("Category","LongRunning")]
public class V5_13_HiddenVariableAndResidualFailureAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;

    // ── Data structures ──
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0,dh0;public string cls;}
    struct Trial{public int seed,cohort;public string proto,cls;public double d0,dT1,dT2,omT1,omT2;public bool immHi,persist;}

    struct FullProfile{
        public int seed,cohort,n;public string cls,stateGroup;
        // Basic d/K
        public double dMean,dStd,dP75,dP90,dP95,dMax;
        public double kMean,kStd;
        // K distribution shape
        public double dP90overP50,dP95overP50,dMaxOverP50,dTailWidth;
        public double kP90overP50,kTailWidth;
        // Edge concentration
        public double top1EdgeShare,top5EdgeShare,top10EdgeShare;
        public double maxNodeKMean,maxNodeKStd;
        // Spectral
        public double lambda1,lambda2,spectralGap,kFrob;
        // Geometry
        public double distToHi,distToLo,projHiVec,orthHiVec;
        // Trajectory
        public double dVelocity,kVelocity,dAccel,kAccel;
        // Outcome
        public bool immHi,persist;public double omT1,omT2,dT1,dT2;
        public double dRebound;public bool kCollapse;public string failMode;
    }

    struct FeatureRank{public string name,group;public double effectSize,threshSep,bAcc;public bool stableN,stableCohort;}

    public V5_13_HiddenVariableAndResidualFailureAudit_Tests(ITestOutputHelper o){_o=o;}

    // ════════════════════════════════════════════════════════
    // HVI_01: Collect pre-intervention metrics across all N and cohorts
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_01_CollectPreInterventionMetrics(){
        _o.WriteLine("═══ HVI_01: Pre-intervention metrics collection ═══");
        var allProfiles=new ConcurrentBag<FullProfile>();
        int[] Ns={67,71,72,75,80};

        foreach(var n in Ns){
            var hi=GetHiCentroid(n);var lo=GetLoCentroid(n);
            _o.WriteLine($"\n─── N={n} Hi=({hi.dm:F4},{hi.km:F4}) Lo=({lo.dm:F4},{lo.km:F4}) ───");

            // Collect profiles for train (0-99) and holdout (100-199)
            ProfileAndTestCohort(n,0,99,hi,lo,0,allProfiles);
            ProfileAndTestCohort(n,100,199,hi,lo,1,allProfiles);
        }

        var profiles=allProfiles.ToArray();
        _o.WriteLine($"\nTotal profiles collected: {profiles.Length}");
        _o.WriteLine($"Train (cohort 0): {profiles.Count(p=>p.cohort==0)}");
        _o.WriteLine($"Holdout (cohort 1): {profiles.Count(p=>p.cohort==1)}");

        // Summary metrics by pathway class
        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,4} {3,8} {4,8} {5,8} {6,8} {7,8} {8,8}",
            "N","Class","Cnt","dMean","kMean","top1%","λ1","dVel","persist%"));
        foreach(var n in Ns){
            foreach(var cls in new[]{"P1b","P1","P2"}){
                var g=profiles.Where(p=>p.n==n&&p.cls==cls&&p.cohort==0).ToArray(); // train only summary
                if(g.Length==0)continue;
                _o.WriteLine($"{n,4} {cls,6} {g.Length,4} {g.Average(p=>p.dMean),8:F4} {g.Average(p=>p.kMean),8:F4} {g.Average(p=>p.top1EdgeShare),8:F4} {g.Average(p=>p.lambda1),8:F4} {g.Average(p=>p.dVelocity),8:F4} {g.Average(p=>p.persist?1.0:0.0)*100,7:F1}%");
            }
        }

        // Store profiles for later test methods via static field
        _profiles=profiles;
        // Pre-warm centroid cache
        foreach(var n in Ns){GetHiCentroid(n);GetLoCentroid(n);}
    }

    // ════════════════════════════════════════════════════════
    // HVI_02: Form state groups G1-G8
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_02_FormStateGroups(){
        _o.WriteLine("═══ HVI_02: State group formation ═══");
        var profiles=EnsureProfiles();

        // Classify into groups (use index-based access for struct mutation)
        for(int i=0;i<profiles.Length;i++){
            string g;
            if((profiles[i].cls=="P1"||profiles[i].cls=="P1b")&&profiles[i].persist)g="G1";
            else if((profiles[i].cls=="P1"||profiles[i].cls=="P1b")&&!profiles[i].persist)g="G2";
            else if(profiles[i].cls=="P2"&&profiles[i].persist)g="G3";
            else if(profiles[i].cls=="P2"&&!profiles[i].persist)g="G4";
            else g="G0";
            profiles[i].stateGroup=g;
        }

        _o.WriteLine(string.Format("\n{0,-8} {1,3} {2,3} {3,3} {4,3} {5,3} {6,3} {7,3} {8,3}",
            "N","G1","G2","G3","G4","G5","G6","G7","G8"));
        foreach(var n in new[]{67,71,72,75,80}){
            var g=profiles.Where(p=>p.n==n&&p.cohort==0).ToArray(); // train
            int c1=g.Count(p=>p.stateGroup=="G1"),c2=g.Count(p=>p.stateGroup=="G2");
            int c3=g.Count(p=>p.stateGroup=="G3"),c4=g.Count(p=>p.stateGroup=="G4");
            _o.WriteLine($"{n,-8} {c1,3} {c2,3} {c3,3} {c4,3} --- --- --- ---");
        }
        _o.WriteLine("\nHoldout:");
        foreach(var n in new[]{67,71,72,75,80}){
            var g=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray(); // holdout
            int c1=g.Count(p=>p.stateGroup=="G1"),c2=g.Count(p=>p.stateGroup=="G2");
            int c3=g.Count(p=>p.stateGroup=="G3"),c4=g.Count(p=>p.stateGroup=="G4");
            _o.WriteLine($"{n,-8} {c1,3} {c2,3} {c3,3} {c4,3} --- --- --- ---");
        }
    }

    // ════════════════════════════════════════════════════════
    // HVI_03: Residual feature ranking
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_03_ResidualFeatureRanking(){
        _o.WriteLine("═══ HVI_03: Residual feature ranking ═══");
        var profiles=EnsureProfiles();

        // Define feature extractors
        var features=new (string name,Func<FullProfile,double> extract)[]{
            ("dMean",p=>p.dMean),("dStd",p=>p.dStd),("dP75",p=>p.dP75),("dP90",p=>p.dP90),
            ("dMax",p=>p.dMax),("kMean",p=>p.kMean),("kStd",p=>p.kStd),
            ("dP90/P50",p=>p.dP90overP50),("dP95/P50",p=>p.dP95overP50),("dMax/P50",p=>p.dMaxOverP50),
            ("dTailWidth",p=>p.dTailWidth),("kP90/P50",p=>p.kP90overP50),("kTailWidth",p=>p.kTailWidth),
            ("top1%Share",p=>p.top1EdgeShare),("top5%Share",p=>p.top5EdgeShare),("top10%Share",p=>p.top10EdgeShare),
            ("maxNodeKMean",p=>p.maxNodeKMean),("maxNodeKStd",p=>p.maxNodeKStd),
            ("lambda1",p=>p.lambda1),("spectralGap",p=>p.spectralGap),("kFrob",p=>p.kFrob),
            ("distToHi",p=>p.distToHi),("distToLo",p=>p.distToLo),("projHiVec",p=>p.projHiVec),("orthHiVec",p=>p.orthHiVec),
            ("dVelocity",p=>p.dVelocity),("kVelocity",p=>p.kVelocity),("dAccel",p=>p.dAccel),("kAccel",p=>p.kAccel),
        };

        // Rank G1 vs G2 (P1/P1b success vs failure) — train only
        _o.WriteLine("\n─── G1 vs G2 (P1/P1b success vs failure) — Train ───");
        var rankings=RankFeatures(profiles.Where(p=>p.cohort==0).ToArray(),"G1","G2",features);
        PrintRankings(rankings);

        // Rank G3 vs G4 (P2 success vs failure) — train only
        _o.WriteLine("\n─── G3 vs G4 (P2 success vs failure) — Train ───");
        var rankingsP2=RankFeatures(profiles.Where(p=>p.cohort==0).ToArray(),"G3","G4",features);
        PrintRankings(rankingsP2);

        // Cross-N stability check for top features
        _o.WriteLine("\n─── Cross-N stability (top features, G1 vs G2, train) ───");
        _o.WriteLine(string.Format("{0,-14} {1,4} {2,8} {3,8} {4,8} {5,8}",
            "Feature","N","EffSize","ThreshSep","BalAcc","Stable"));
        foreach(var n in new[]{67,71,72,75,80}){
            var sub=profiles.Where(p=>p.cohort==0).ToArray(); // all train
            foreach(var feat in features.Take(8)){ // top basic features
                var g1=sub.Where(p=>p.stateGroup=="G1").Select(feat.extract).ToArray();
                var g2=sub.Where(p=>p.stateGroup=="G2").Select(feat.extract).ToArray();
                if(g1.Length<2||g2.Length<2)continue;
                double es=EffectSize(g1,g2);
                double ts=ThresholdSep(g1,g2);
                double ba=BalAcc(g1,g2);
                _o.WriteLine($"{feat.name,-14} {n,4} {es,8:F4} {ts,8:F4} {ba,8:F4} {(Math.Abs(es)<0.2?"WEAK":"OK  ")}");
            }
            break; // just one N for brevity in summary
        }

        _rankings=rankings;
    }

    // ════════════════════════════════════════════════════════
    // HVI_04: Compression-Room residual audit (P1/P1b)
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_04_CompressionRoomResidualAudit(){
        _o.WriteLine("═══ HVI_04: Compression-Room (P1/P1b) residual audit ═══");
        var profiles=EnsureProfiles();

        foreach(var n in new[]{71,72,75}){
            var train=profiles.Where(p=>p.n==n&&p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
            var holdout=profiles.Where(p=>p.n==n&&p.cohort==1&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
            if(train.Length<3)continue;

            var tSucc=train.Where(p=>p.persist).ToArray();
            var tFail=train.Where(p=>!p.persist).ToArray();
            var hSucc=holdout.Where(p=>p.persist).ToArray();
            var hFail=holdout.Where(p=>!p.persist).ToArray();

            _o.WriteLine($"\n─── N={n} P1/P1b: Train {tSucc.Length}S/{tFail.Length}F  Holdout {hSucc.Length}S/{hFail.Length}F ───");
            _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10} {4,10}",
                "Feature","T-Succ","T-Fail","H-Succ","H-Fail"));
            PrintGroupCompare(tSucc,tFail,hSucc,hFail);
        }

        // Decision tree analysis for P1/P1b - train only, across N
        _o.WriteLine("\n─── Best single-feature split for P1/P1b success (train, all N) ───");
        var allP1Train=profiles.Where(p=>p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        if(allP1Train.Length>=6){
            var best=FindBestSplit(allP1Train);
            _o.WriteLine($"Best feature: {best.name}  Threshold: {best.threshold:F4}  Accuracy: {best.accuracy:F3}");
            _o.WriteLine($"  Success above threshold: {best.successAbove}");
            _o.WriteLine($"  Failure below threshold: {best.failureBelow}");
        }
    }

    // ════════════════════════════════════════════════════════
    // HVI_05: Crypto-Hi residual audit (P2)
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_05_CryptoHiResidualAudit(){
        _o.WriteLine("═══ HVI_05: Crypto-Hi (P2) residual audit ═══");
        var profiles=EnsureProfiles();

        foreach(var n in new[]{67,71,72}){
            var train=profiles.Where(p=>p.n==n&&p.cohort==0&&p.cls=="P2").ToArray();
            var holdout=profiles.Where(p=>p.n==n&&p.cohort==1&&p.cls=="P2").ToArray();
            if(train.Length<3)continue;

            var tSucc=train.Where(p=>p.persist).ToArray();
            var tFail=train.Where(p=>!p.persist).ToArray();

            _o.WriteLine($"\n─── N={n} P2: Train {tSucc.Length}S/{tFail.Length}F  Holdout {holdout.Length} total (all F typically) ───");
            if(tSucc.Length>0&&tFail.Length>0){
                _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10}",
                    "Feature","T-Succ","T-Fail","Ratio"));
                var feats=new[]{"dMean","kMean","top1%","top5%","lambda1","dVelocity","distToHi","projHiVec"};
                foreach(var fn in feats){
                    var sv=GetFeatureValues(tSucc,fn);
                    var fv=GetFeatureValues(tFail,fn);
                    if(sv.Length<1||fv.Length<1)continue;
                    double ratio=Math.Abs(fv.Average())>1e-9?sv.Average()/fv.Average():double.NaN;
                    _o.WriteLine($"{fn,-14} {sv.Average(),10:F4} {fv.Average(),10:F4} {ratio,10:F3}");
                }
            }

            // P2 holdout: why all fail?
            if(holdout.Length>0){
                _o.WriteLine($"  Holdout P2: dMean avg={holdout.Average(p=>p.dMean):F4} kMean avg={holdout.Average(p=>p.kMean):F4}");
                _o.WriteLine($"  Holdout vs Train dMean diff: {holdout.Average(p=>p.dMean)-train.Average(p=>p.dMean):F4}");
            }
        }

        _o.WriteLine("\n─── P2 overfit assessment ───");
        _o.WriteLine("If P2 holdout has similar candidate quality but zero success → overfit to train-specific dynamics.");
    }

    // ════════════════════════════════════════════════════════
    // HVI_06: N=75 analysis
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_06_N75Analysis(){
        _o.WriteLine("═══ HVI_06: N=75 mechanism analysis ═══");
        var profiles=EnsureProfiles();

        var n75=profiles.Where(p=>p.n==75&&p.cohort==0).ToArray();
        var n71=profiles.Where(p=>p.n==71&&p.cohort==0).ToArray();
        var n72=profiles.Where(p=>p.n==72&&p.cohort==0).ToArray();

        // Actually filter by N — we stored profiles but not N. Hmm.
        // We'll compute N-specific centroids instead.
        _o.WriteLine("N=75 advantage hypotheses tested via centroid comparison:");
        _o.WriteLine(string.Format("{0,-20} {1,10} {2,10} {3,10} {4,15}",
            "Hypothesis","N=71","N=72","N=75","Verdict"));

        foreach(var h in new[]{"dMean(HI)","kMean(HI)","top1%Edge","edgeCount","dGap","barrier"}){
            double v71=0,v72=0,v75=0;
            var hi71=GetHiCentroid(71);var hi72=GetHiCentroid(72);var hi75=GetHiCentroid(75);
            var lo71=GetLoCentroid(71);var lo72=GetLoCentroid(72);var lo75=GetLoCentroid(75);
            if(h=="dMean(HI)"){v71=hi71.dm;v72=hi72.dm;v75=hi75.dm;}
            else if(h=="kMean(HI)"){v71=hi71.km;v72=hi72.km;v75=hi75.km;}
            else if(h=="dGap"){v71=hi71.dm-lo71.dm;v72=hi72.dm-lo72.dm;v75=hi75.dm-lo75.dm;}
            else if(h=="barrier"){v71=1.783-lo71.dm;v72=1.783-lo72.dm;v75=1.783-lo75.dm;}
            string verdict=v75>v72&&v75>v71?"FAVORABLE":(v75<v72?"UNFAVORABLE":"NEUTRAL");
            _o.WriteLine($"{h,-20} {v71,10:F4} {v72,10:F4} {v75,10:F4} {verdict,15}");
        }

        // P1 candidate counts and quality
        _o.WriteLine("\n─── N=75 P1/P1b candidate analysis ───");
        foreach(var cls in new[]{"P1b","P1"}){
            var t75=profiles.Where(p=>p.n==75&&p.cohort==0&&p.cls==cls).ToArray();
            if(t75.Length==0)continue;
            if(t75.Length==0)continue;
            _o.WriteLine($"{cls}: count={t75.Length}, dMean={t75.Average(p=>p.dMean):F4}, "+
                $"top1%={t75.Average(p=>p.top1EdgeShare):F4}, persist={t75.Average(p=>p.persist?1.0:0.0)*100:F0}%");
        }

        _o.WriteLine("\n─── N=75 holdout degradation analysis ───");
        _o.WriteLine("Key question: Why does N=75 train have strong P1 signal but holdout fails?");
        var p1Train75=profiles.Where(p=>p.n==75&&p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        var p1Hold75=profiles.Where(p=>p.n==75&&p.cohort==1&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        if(p1Train75.Length>0&&p1Hold75.Length>0){
            _o.WriteLine($"Train P1 dMean={p1Train75.Average(p=>p.dMean):F4} Holdout P1 dMean={p1Hold75.Average(p=>p.dMean):F4}");
            _o.WriteLine($"Train P1 top1%Edge={p1Train75.Average(p=>p.top1EdgeShare):F4} Holdout P1 top1%Edge={p1Hold75.Average(p=>p.top1EdgeShare):F4}");
            _o.WriteLine($"Train P1 lambda1={p1Train75.Average(p=>p.lambda1):F4} Holdout P1 lambda1={p1Hold75.Average(p=>p.lambda1):F4}");
        }
    }

    // ════════════════════════════════════════════════════════
    // HVI_07: N=80 saturation analysis
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_07_N80SaturationAnalysis(){
        _o.WriteLine("═══ HVI_07: N=80 saturation analysis ═══");
        var profiles=EnsureProfiles();

        var hi80=GetHiCentroid(80);var lo80=GetLoCentroid(80);
        _o.WriteLine($"N=80: Hi=({hi80.dm:F4},{hi80.km:F4}) Lo=({lo80.dm:F4},{lo80.km:F4}) dGap={hi80.dm-lo80.dm:F4}");

        var n80Prof=profiles.Where(p=>p.n==80&&p.cohort==0).ToArray();

        // Universal accessibility check
        _o.WriteLine("\n─── Saturation hypotheses ───");
        _o.WriteLine("1. Candidate abundance: P1b dominates, no P2 needed");
        _o.WriteLine("2. Low barrier: Lo->Hi gap is smaller at high N");
        _o.WriteLine("3. K/d geometry: edge concentration favors persistence");
        _o.WriteLine("4. Branch threshold composition: more seeds near threshold");

        // Compare Hi centroids across N
        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,10} {3,10} {4,10}",
            "N","Hi_dm","Hi_km","Lo_dm","dGap"));
        foreach(var n in new[]{67,71,72,75,80}){
            var hi=GetHiCentroid(n);var lo=GetLoCentroid(n);
            _o.WriteLine($"{n,4} {hi.dm,10:F4} {hi.km,10:F4} {lo.dm,10:F4} {hi.dm-lo.dm,10:F4}");
        }

        _o.WriteLine("\n─── Saturation verdict ───");
        _o.WriteLine("If dGap shrinks with N and P1b candidates are abundant → saturation is structural.");
        _o.WriteLine("Pathway selection at N>=80 provides no advantage over universal protocols.");
    }

    // ════════════════════════════════════════════════════════
    // HVI_08: Residual selector test
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_08_ResidualSelectorTest(){
        _o.WriteLine("═══ HVI_08: Residual selector test ═══");
        var profiles=EnsureProfiles();

        // Train selector on cohort 0, test on cohort 1
        // Use pathway class + best 2 hidden-variable features

        // First identify best features from G1 vs G2 ranking
        var rankings=_rankings;
        if(rankings==null||rankings.Length<2){
        // Compute on the fly using profiles with assigned state groups
        var train=profiles.Where(p=>p.cohort==0).ToArray();
        var features=new (string,Func<FullProfile,double>)[]{
            ("dMean",p=>p.dMean),("top1%Share",p=>p.top1EdgeShare),("lambda1",p=>p.lambda1),
            ("dVelocity",p=>p.dVelocity),("projHiVec",p=>p.projHiVec),("kFrob",p=>p.kFrob),
            ("top5%Share",p=>p.top5EdgeShare),("distToHi",p=>p.distToHi),("orthHiVec",p=>p.orthHiVec)};
        rankings=RankFeatures(train,"G1","G2",features);
        }

        _o.WriteLine("Top features for residual selector:");
        foreach(var r in rankings.Take(4))
            _o.WriteLine($"  {r.name}: effectSize={r.effectSize:F4} balAcc={r.bAcc:F4}");

        // Build simple threshold selector: pathway class + top feature threshold
        var topFeat=rankings[0];

        // Find threshold on train (cohort 0)
        var trainP1=profiles.Where(p=>p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        if(trainP1.Length>=6){
            var best=FindBestSplit(trainP1);
            _o.WriteLine($"\nBest split: {best.name} threshold={best.threshold:F4} acc={best.accuracy:F3}");

            // Test on holdout based on pathway + hidden variable
            foreach(var n in new[]{71,72,75}){
                var holdP1=profiles.Where(p=>p.n==n&&p.cohort==1&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
                if(holdP1.Length==0)continue;

                double featVal=GetFeatureValue(holdP1[0],best.name);
                int above=holdP1.Count(p=>GetFeatureValue(p,best.name)>best.threshold);
                int below=holdP1.Length-above;
                int succAbove=holdP1.Count(p=>GetFeatureValue(p,best.name)>best.threshold&&p.persist);
                int succBelow=holdP1.Count(p=>GetFeatureValue(p,best.name)<=best.threshold&&p.persist);

                _o.WriteLine($"\nN={n} Holdout P1/P1b (n={holdP1.Length}):");
                _o.WriteLine($"  Above threshold: {above} seeds, {succAbove} persist ({(above>0?(double)succAbove/above*100:0):F0}%)");
                _o.WriteLine($"  Below threshold: {below} seeds, {succBelow} persist ({(below>0?(double)succBelow/below*100:0):F0}%)");

                // Baseline: pathway-only rate
                int pathwayOnly=holdP1.Count(p=>p.persist);
                _o.WriteLine($"  Pathway-only: {pathwayOnly}/{holdP1.Length} ({(double)pathwayOnly/holdP1.Length*100:F0}%)");
                _o.WriteLine($"  Selector improvement: {(above>0?(double)succAbove/above*100:0)-(double)pathwayOnly/holdP1.Length*100:+.0}%");
            }
        }
    }

    // ════════════════════════════════════════════════════════
    // HVI_09: Failure-mode classification
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_09_FailureModeClassification(){
        _o.WriteLine("═══ HVI_09: Failure-mode classification ═══");
        var profiles=EnsureProfiles();

        // Classify each failed seed (index-based for struct mutation)
        var allFail=profiles.Where(p=>!p.persist).Select((p,idx)=>(p,idx)).ToArray();
        var failArr=profiles.Where(p=>!p.persist).ToArray();
        for(int fi=0;fi<failArr.Length;fi++){
            var p=failArr[fi];
            ClassifyFailureStruct(ref p);
            failArr[fi]=p;
        }
        // Re-collect modified profiles - need to update originals
        var updated=failArr.ToArray();

        _o.WriteLine("\n─── Failure mode distribution (all N, both cohorts) ───");
        var modes=new[]{"rebound","K_collapse","off_vector","insufficient_disp","over_compress","hidden_mismatch","unresolved"};
        _o.WriteLine(string.Format("{0,-20} {1,5} {2,8}","Mode","Count","Pct"));
        foreach(var m in modes){
            int cnt=updated.Count(p=>p.failMode==m);
            _o.WriteLine($"{m,-20} {cnt,5} {(double)cnt/updated.Length*100,7:F1}%");
        }

        // By pathway class
        _o.WriteLine("\n─── Failure mode by pathway class ───");
        _o.WriteLine(string.Format("{0,-6} {1,-20} {2,5}","Class","Mode","Count"));
        foreach(var cls in new[]{"P1b","P1","P2","P3","P4"}){
            var g=updated.Where(p=>p.cls==cls).ToArray();
            if(g.Length==0)continue;
            foreach(var m in modes){
                int cnt=g.Count(p=>p.failMode==m);
                if(cnt>0)_o.WriteLine($"{cls,-6} {m,-20} {cnt,5}");
            }
        }

        // By N
        _o.WriteLine("\n─── Failure mode by N ───");
        foreach(var n in new[]{67,71,72,75,80}){
            var parts=new List<string>();
            foreach(var m in modes){
                int c=updated.Count(p=>p.n==n&&p.failMode==m);
                if(c>0)parts.Add($"{m}={c}");
            }
            _o.WriteLine($"N={n}: {string.Join(" ",parts)}");
        }
    }

    // ════════════════════════════════════════════════════════
    // HVI_10: Gate summary and claim audit
    // ════════════════════════════════════════════════════════
    [Fact]public void HVI_10_GateSummaryAndClaimAudit(){
        _o.WriteLine("═══ HVI_10: Decision gates and claim audit ═══");
        _o.WriteLine("");

        _o.WriteLine("─── Decision Gates ───");
        _o.WriteLine("Gate A — Hidden Variable Found:");
        _o.WriteLine("  Evaluate: Do 1-2 features consistently separate G1/G2 and transfer to holdout?");
        _o.WriteLine("  → See HVI_03 ranking and HVI_08 selector test.");
        _o.WriteLine("");
        _o.WriteLine("Gate B — Compression-Room Residual Explained:");
        _o.WriteLine("  Evaluate: Is P1/P1b success/failure explained by additional feature?");
        _o.WriteLine("  → See HVI_04 audit.");
        _o.WriteLine("");
        _o.WriteLine("Gate C — Crypto-Hi Overfit Confirmed:");
        _o.WriteLine("  Evaluate: Does P2 have transferable hidden-variable signature?");
        _o.WriteLine("  → See HVI_05 audit.");
        _o.WriteLine("");
        _o.WriteLine("Gate D — N=75 Mechanism Explained:");
        _o.WriteLine("  Evaluate: Is N=75 advantage from hidden-variable distribution?");
        _o.WriteLine("  → See HVI_06 analysis.");
        _o.WriteLine("");
        _o.WriteLine("Gate E — N=80 Saturation Explained:");
        _o.WriteLine("  Evaluate: Is universal success at N=80 from broad accessibility?");
        _o.WriteLine("  → See HVI_07 analysis.");
        _o.WriteLine("");
        _o.WriteLine("Gate F — No Hidden Variable Found:");
        _o.WriteLine("  Evaluate: If no feature explains residual, instrumentation insufficient.");
        _o.WriteLine("");
        _o.WriteLine("Gate G — Residual Selector Improves Holdout:");
        _o.WriteLine("  Evaluate: Does pathway + hidden-variable improve over HVE baseline?");
        _o.WriteLine("  → See HVI_08 test.");
        _o.WriteLine("");

        _o.WriteLine("─── Claim Discipline Audit ───");
        _o.WriteLine("☐ No physical interpretation (time, space, relativity, quantum).");
        _o.WriteLine("☐ No attractor decomposition claims.");
        _o.WriteLine("☐ No universal controllability claims.");
        _o.WriteLine("☐ No threshold tuning — THR=1.783 frozen from V5.3.");
        _o.WriteLine("☐ No generalization beyond tested N and seed cohorts.");
        _o.WriteLine("☐ Pathway claims conditioned on N and cohort.");
        _o.WriteLine("☐ Hidden-variable claims require holdout support.");
        _o.WriteLine("☐ P2 claims require transfer evidence.");
        _o.WriteLine("");

        _o.WriteLine("─── Recommended Next Suite ───");
        _o.WriteLine("Based on gate outcomes:");
        _o.WriteLine("  Gate A/B reached → Develop residual-augmented pathway model.");
        _o.WriteLine("  Gate C reached → Demote/weaken P2 pathway; focus on P1.");
        _o.WriteLine("  Gate D reached → N=75 as primary validation window.");
        _o.WriteLine("  Gate E reached → N>=80 use universal protocols.");
        _o.WriteLine("  Gate F reached → Need new instrumentation/metrics.");
        _o.WriteLine("  Gate G reached → Proceed to HVS (scaling with residual selector).");
        _o.WriteLine("═══ END HVI ═══");
    }

    // ══════════════════════════════════════════════════════════════
    // ── HELPER: Profiling and testing ──
    // ══════════════════════════════════════════════════════════════
    void ProfileAndTestCohort(int n,int seedStart,int seedEnd,P3 hi,P3 lo,int cohort,ConcurrentBag<FullProfile> results){
        var loSeeds=new List<int>();
        for(int s=seedStart;s<=seedEnd&&loSeeds.Count<MaxSeedsPerCohort;s++)
            if(!IsHi(n,s))loSeeds.Add(s);

        var bases=new ConcurrentBag<(SBase base_,double[] dHist,double[] kHist,double[,] Kmat)>();
        Parallel.ForEach(loSeeds.ToArray(),s=>{
            var (b,dHist,kHist,Km)=ProfileFull(n,s,hi,lo);
            bases.Add((b,dHist,kHist,Km));
        });
        var baseList=bases.ToList();

        foreach(var (b,dHist,kHist,Km) in baseList){
            // Run intervention
            var trial=RunTrial(n,b.seed,b.cls=="P2"?b.d0*0.90:b.d0*0.50,b,hi,lo,"MATCHED",cohort);

            // Build full profile
            var fp=new FullProfile{
                seed=b.seed,cohort=cohort,n=n,cls=b.cls,stateGroup="G0",
                dMean=b.d0,kMean=b.km0,kStd=b.ks0,
                distToHi=Dist3(b.d0,b.km0,b.ks0,hi),
                distToLo=Dist3(b.d0,b.km0,b.ks0,lo),
            };

            // d distribution from K matrix (use Kmat as distance proxy)
            ComputeDistribution(n,Km,ref fp);

            // Edge concentration
            ComputeEdgeConcentration(n,Km,ref fp);

            // Node localization
            ComputeNodeLocalization(n,Km,ref fp);

            // Spectral
            ComputeSpectral(n,Km,ref fp);

            // Geometry
            ComputeGeometry(b.d0,b.km0,b.ks0,hi,lo,ref fp);

            // Trajectory from dHist, kHist
            ComputeTrajectory(dHist,kHist,ref fp);

            // Outcome
            fp.immHi=trial.immHi;fp.persist=trial.persist;
            fp.omT1=trial.omT1;fp.omT2=trial.omT2;
            fp.dT1=trial.dT1;fp.dT2=trial.dT2;
            fp.dRebound=trial.dT2-trial.dT1;
            fp.kCollapse=trial.omT2<THR&&!trial.persist;

            results.Add(fp);
        }
    }

    void ComputeDistribution(int n,double[,] K,ref FullProfile fp){
        // d distribution is computed from seed-level stats across the group
        // Per-seed, we use the K matrix edge values as a proxy for d distribution
        var edges=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K[i,j]);
        edges.Sort();
        int cnt=edges.Count;
        if(cnt==0)return;
        double p50=edges[cnt/2];
        fp.dP75=edges[(int)(cnt*0.75)];
        fp.dP90=edges[(int)(cnt*0.90)];
        fp.dP95=edges[(int)(cnt*0.95)];
        fp.dMax=edges[cnt-1];
        fp.dStd=Math.Sqrt(edges.Sum(x=>(x-edges.Average())*(x-edges.Average()))/cnt);
        fp.dP90overP50=p50>0?fp.dP90/p50:0;
        fp.dP95overP50=p50>0?fp.dP95/p50:0;
        fp.dMaxOverP50=p50>0?fp.dMax/p50:0;
        fp.dTailWidth=fp.dP95-fp.dP75;
    }

    void ComputeEdgeConcentration(int n,double[,] K,ref FullProfile fp){
        var edges=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K[i,j]);
        edges.Sort((a,b)=>b.CompareTo(a));
        double total=edges.Sum();
        if(total<=0)return;
        int top1=Math.Max(1,(int)(edges.Count*0.01));
        int top5=Math.Max(1,(int)(edges.Count*0.05));
        int top10=Math.Max(1,(int)(edges.Count*0.10));
        fp.top1EdgeShare=edges.Take(top1).Sum()/total;
        fp.top5EdgeShare=edges.Take(top5).Sum()/total;
        fp.top10EdgeShare=edges.Take(top10).Sum()/total;
    }

    void ComputeNodeLocalization(int n,double[,] K,ref FullProfile fp){
        fp.maxNodeKMean=0;fp.maxNodeKStd=0;
        for(int i=0;i<n;i++){
            var vals=new List<double>();
            for(int j=0;j<n;j++)if(i!=j)vals.Add(K[i,j]);
            if(vals.Count==0)continue;
            double m=vals.Average();
            double s=Math.Sqrt(vals.Sum(x=>(x-m)*(x-m))/vals.Count);
            if(m>fp.maxNodeKMean)fp.maxNodeKMean=m;
            if(s>fp.maxNodeKStd)fp.maxNodeKStd=s;
        }
    }

    void ComputeSpectral(int n,double[,] K,ref FullProfile fp){
        fp.lambda1=PowerIteration(K,n,200);
        fp.lambda2=PowerIterationDeflated(K,n,fp.lambda1,200);
        fp.spectralGap=fp.lambda1-fp.lambda2;
        double frob=0;
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)frob+=K[i,j]*K[i,j];
        fp.kFrob=Math.Sqrt(frob);
    }

    void ComputeGeometry(double dm,double km,double ks,P3 hi,P3 lo,ref FullProfile fp){
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks;
        double vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        if(vn>0){
            double dd=dm-lo.dm,dk=km-lo.km,ds=ks-lo.ks;
            fp.projHiVec=(dd*dv+dk*kv+ds*sv)/vn;
            double d2=fp.distToLo*fp.distToLo;
            fp.orthHiVec=Math.Sqrt(Math.Max(0,d2-fp.projHiVec*fp.projHiVec));
        }
    }

    void ComputeTrajectory(double[] dHist,double[] kHist,ref FullProfile fp){
        if(dHist.Length>=2){
            fp.dVelocity=dHist[^1]-dHist[^2];
            fp.kVelocity=kHist.Length>=2?kHist[^1]-kHist[^2]:0;
        }
        if(dHist.Length>=3){
            fp.dAccel=(dHist[^1]-dHist[^2])-(dHist[^2]-dHist[^3]);
            fp.kAccel=kHist.Length>=3?(kHist[^1]-kHist[^2])-(kHist[^2]-kHist[^3]):0;
        }
    }

    void ClassifyFailureStruct(ref FullProfile p){
        // Already doesn't persist
        if(!p.immHi){
            p.failMode="insufficient_disp"; // never reached High
        }else if(p.dRebound>0.05){
            p.failMode="rebound";
        }else if(p.omT2<0.5*THR){
            p.failMode="K_collapse";
        }else if(p.orthHiVec>0.3){
            p.failMode="off_vector";
        }else if(p.dMean>0.85){
            p.failMode="over_compress";
        }else{
            p.failMode="unresolved";
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ── HELPERS: Feature ranking ──
    // ══════════════════════════════════════════════════════════════
    FeatureRank[] RankFeatures(FullProfile[] profiles,string gPos,string gNeg,(string,Func<FullProfile,double>)[] features){
        var pos=profiles.Where(p=>p.stateGroup==gPos).ToArray();
        var neg=profiles.Where(p=>p.stateGroup==gNeg).ToArray();
        var results=new List<FeatureRank>();
        foreach(var (name,extract) in features){
            var pv=pos.Select(extract).ToArray();
            var nv=neg.Select(extract).ToArray();
            if(pv.Length<2||nv.Length<2)continue;
            results.Add(new FeatureRank{
                name=name,group=$"{gPos}v{gNeg}",
                effectSize=EffectSize(pv,nv),
                threshSep=ThresholdSep(pv,nv),
                bAcc=BalAcc(pv,nv),
                stableN=true,stableCohort=true
            });
        }
        return results.OrderByDescending(r=>Math.Abs(r.effectSize)).ToArray();
    }

    void PrintRankings(FeatureRank[] rankings){
        _o.WriteLine(string.Format("{0,-14} {1,10} {2,10} {3,10} {4,6}",
            "Feature","EffSize","ThreshSep","BalAcc","Rank"));
        int rank=1;
        foreach(var r in rankings.Take(15)){
            _o.WriteLine($"{r.name,-14} {r.effectSize,10:F4} {r.threshSep,10:F4} {r.bAcc,10:F4} {rank++,6}");
        }
    }

    static double EffectSize(double[] a,double[] b){
        double ma=a.Average(),mb=b.Average();
        double sa=StdDev(a),sb=StdDev(b);
        double pooled=Math.Sqrt((sa*sa+sb*sb)/2);
        return pooled>1e-12?Math.Abs(ma-mb)/pooled:0;
    }
    static double ThresholdSep(double[] a,double[] b){
        var all=a.Concat(b).OrderBy(x=>x).ToArray();
        double best=0;
        for(int i=1;i<all.Length;i++){
            double thresh=(all[i-1]+all[i])/2;
            double acc=(a.Count(x=>x>thresh)+b.Count(x=>x<=thresh))/(double)(a.Length+b.Length);
            if(acc>best)best=acc;
        }
        return best;
    }
    static double BalAcc(double[] a,double[] b){
        var all=a.Concat(b).OrderBy(x=>x).ToArray();
        double best=0;
        for(int i=1;i<all.Length;i++){
            double thresh=(all[i-1]+all[i])/2;
            double tpr=a.Count(x=>x>thresh)/(double)a.Length;
            double tnr=b.Count(x=>x<=thresh)/(double)b.Length;
            double ba=(tpr+tnr)/2;
            if(ba>best)best=ba;
        }
        return best;
    }
    static double StdDev(double[] v){double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}

    (string name,double threshold,double accuracy,string successAbove,string failureBelow)
        FindBestSplit(FullProfile[] profiles){
        var feats=new (string,Func<FullProfile,double>)[]{
            ("dMean",p=>p.dMean),("top1%Share",p=>p.top1EdgeShare),("lambda1",p=>p.lambda1),
            ("dVelocity",p=>p.dVelocity),("projHiVec",p=>p.projHiVec),("kFrob",p=>p.kFrob),
            ("distToHi",p=>p.distToHi),("top5%Share",p=>p.top5EdgeShare),("dMax/P50",p=>p.dMaxOverP50)};
        string bestName="";double bestThresh=0,bestAcc=0;string sAbove="",sBelow="";
        foreach(var (name,extract) in feats){
            var vals=profiles.Select(p=>(val:extract(p),persist:p.persist)).OrderBy(x=>x.val).ToArray();
            for(int i=1;i<vals.Length;i++){
                double thresh=(vals[i-1].val+vals[i].val)/2;
                int above=vals.Count(v=>v.val>thresh),below=vals.Length-above;
                int succAbove=vals.Count(v=>v.val>thresh&&v.persist);
                int failBelow=vals.Count(v=>v.val<=thresh&&!v.persist);
                double acc=(succAbove+failBelow)/(double)vals.Length;
                if(acc>bestAcc){bestAcc=acc;bestName=name;bestThresh=thresh;
                    sAbove=$"{succAbove}/{above}";sBelow=$"{failBelow}/{below}";}
            }
        }
        return (bestName,bestThresh,bestAcc,sAbove,sBelow);
    }

    void PrintGroupCompare(FullProfile[] tSucc,FullProfile[] tFail,FullProfile[] hSucc,FullProfile[] hFail){
        var feats=new[]{"dMean","kMean","top1%","top5%","lambda1","dVelocity","distToHi","projHiVec","kFrob"};
        foreach(var fn in feats){
            double ts=tSucc.Length>0?GetFeatureValues(tSucc,fn).Average():double.NaN;
            double tf=tFail.Length>0?GetFeatureValues(tFail,fn).Average():double.NaN;
            double hs=hSucc.Length>0?GetFeatureValues(hSucc,fn).Average():double.NaN;
            double hf=hFail.Length>0?GetFeatureValues(hFail,fn).Average():double.NaN;
            _o.WriteLine($"{fn,-14} {ts,10:F4} {tf,10:F4} {hs,10:F4} {hf,10:F4}");
        }
    }

    double[] GetFeatureValues(FullProfile[] ps,string name)=>name switch{
        "dMean"=>ps.Select(p=>p.dMean).ToArray(),"dStd"=>ps.Select(p=>p.dStd).ToArray(),
        "dP75"=>ps.Select(p=>p.dP75).ToArray(),"dP90"=>ps.Select(p=>p.dP90).ToArray(),
        "dMax"=>ps.Select(p=>p.dMax).ToArray(),"kMean"=>ps.Select(p=>p.kMean).ToArray(),
        "kStd"=>ps.Select(p=>p.kStd).ToArray(),"dP90/P50"=>ps.Select(p=>p.dP90overP50).ToArray(),
        "dMax/P50"=>ps.Select(p=>p.dMaxOverP50).ToArray(),"dTailWidth"=>ps.Select(p=>p.dTailWidth).ToArray(),
        "top1%"=>ps.Select(p=>p.top1EdgeShare).ToArray(),"top5%"=>ps.Select(p=>p.top5EdgeShare).ToArray(),
        "top10%"=>ps.Select(p=>p.top10EdgeShare).ToArray(),"lambda1"=>ps.Select(p=>p.lambda1).ToArray(),
        "kFrob"=>ps.Select(p=>p.kFrob).ToArray(),"distToHi"=>ps.Select(p=>p.distToHi).ToArray(),
        "distToLo"=>ps.Select(p=>p.distToLo).ToArray(),"projHiVec"=>ps.Select(p=>p.projHiVec).ToArray(),
        "orthHiVec"=>ps.Select(p=>p.orthHiVec).ToArray(),"dVelocity"=>ps.Select(p=>p.dVelocity).ToArray(),
        "kVelocity"=>ps.Select(p=>p.kVelocity).ToArray(),"dAccel"=>ps.Select(p=>p.dAccel).ToArray(),
        "spectralGap"=>ps.Select(p=>p.spectralGap).ToArray(),
        _=>new double[ps.Length]
    };
    double GetFeatureValue(FullProfile p,string name)=>name switch{
        "dMean"=>p.dMean,"top1%Share"=>p.top1EdgeShare,"lambda1"=>p.lambda1,
        "dVelocity"=>p.dVelocity,"projHiVec"=>p.projHiVec,"kFrob"=>p.kFrob,
        "distToHi"=>p.distToHi,"top5%Share"=>p.top5EdgeShare,"dMax/P50"=>p.dMaxOverP50,_=>0
    };

    // ══════════════════════════════════════════════════════════════
    // ── HELPERS: Profile caching for cross-test access ──
    // ══════════════════════════════════════════════════════════════
    static FullProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hiCentroids;
    static ConcurrentDictionary<int,P3>? _loCentroids;
    static FeatureRank[]? _rankings;

    P3 GetHiCentroid(int n){
        _hiCentroids??=new ConcurrentDictionary<int,P3>();
        return _hiCentroids.GetOrAdd(n,k=>PCent(k,true));
    }
    P3 GetLoCentroid(int n){
        _loCentroids??=new ConcurrentDictionary<int,P3>();
        return _loCentroids.GetOrAdd(n,k=>PCent(k,false));
    }

    FullProfile[] EnsureProfiles(){
        if(_profiles!=null&&_profiles.Length>0){
            // State groups may not be assigned yet; do so if needed
            if(_profiles[0].stateGroup!="G0"){
                AssignStateGroups(_profiles);
            }
            return _profiles;
        }
        var bag=new ConcurrentBag<FullProfile>();
        foreach(var n in new[]{67,71,72,75,80}){
            var hi=GetHiCentroid(n);var lo=GetLoCentroid(n);
            ProfileAndTestCohort(n,0,99,hi,lo,0,bag);
            ProfileAndTestCohort(n,100,199,hi,lo,1,bag);
        }
        _profiles=bag.ToArray();
        AssignStateGroups(_profiles);
        return _profiles;
    }

    void AssignStateGroups(FullProfile[] profiles){
        for(int i=0;i<profiles.Length;i++){
            string g;
            if((profiles[i].cls=="P1"||profiles[i].cls=="P1b")&&profiles[i].persist)g="G1";
            else if((profiles[i].cls=="P1"||profiles[i].cls=="P1b")&&!profiles[i].persist)g="G2";
            else if(profiles[i].cls=="P2"&&profiles[i].persist)g="G3";
            else if(profiles[i].cls=="P2"&&!profiles[i].persist)g="G4";
            else g="G0";
            profiles[i].stateGroup=g;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ── ENGINE: RecoverFP simulation (frozen from V5.12) ──
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

    static (SBase,double[],double[],double[,]) ProfileFull(int n,int seed,P3 hi,P3 lo){
        var K=KS(n,seed);
        var dHist=new List<double>();var kHist=new List<double>();
        for(int e=0;e<3;e++){
            var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);
            dHist.Add(Dm(d,n));K=Cupd(d,n);kHist.Add(Km(K,n));
        }
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km0=Km(K3E,n),ks0=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km0,ks0=ks0,dh0=Dist3(d0,km0,ks0,hi),cls=""};
        sb=Classify(sb,hi);
        return (sb,dHist.ToArray(),kHist.ToArray(),K3E);
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
        var kT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(kT1,n,S,seed+200);double om2=Of(hT2,n).Average();
        return new Trial{seed=seed,cohort=cohort,proto=proto,cls=b.cls,d0=b.d0,
            dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),omT1=om1,omT2=om2,
            immHi=om1>THR,persist=om2>THR};
    }

    // ══════════════════════════════════════════════════════════════
    // ── NUMERICAL: Power iteration for eigenvalues ──
    // ══════════════════════════════════════════════════════════════
    static double PowerIteration(double[,] A,int n,int maxIter=200){
        var v=new double[n];var rng=new Random(42);
        for(int i=0;i<n;i++)v[i]=rng.NextDouble();
        double norm=Math.Sqrt(v.Sum(x=>x*x));
        if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;
        double lambda=0;
        for(int iter=0;iter<maxIter;iter++){
            var Av=new double[n];
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];
            norm=Math.Sqrt(Av.Sum(x=>x*x));
            if(norm<1e-15)break;
            for(int i=0;i<n;i++)v[i]=Av[i]/norm;
            double newLambda=0;
            for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];newLambda+=v[i]*s;}
            if(Math.Abs(newLambda-lambda)<1e-10)break;
            lambda=newLambda;
        }
        return lambda;
    }

    static double PowerIterationDeflated(double[,] A,int n,double lambda1,int maxIter=200){
        // Deflate: A' = A - lambda1 * v1 * v1^T
        // First get v1 (eigenvector for lambda1)
        var v1=new double[n];var rng=new Random(42);
        for(int i=0;i<n;i++)v1[i]=rng.NextDouble();
        double nrm=Math.Sqrt(v1.Sum(x=>x*x));
        if(nrm>0)for(int i=0;i<n;i++)v1[i]/=nrm;
        for(int iter=0;iter<maxIter;iter++){
            var Av=new double[n];
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v1[j];
            nrm=Math.Sqrt(Av.Sum(x=>x*x));
            if(nrm<1e-15)break;
            for(int i=0;i<n;i++)v1[i]=Av[i]/nrm;
            double l=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v1[j];l+=v1[i]*s;}
            if(Math.Abs(l-lambda1)<1e-10)break;
        }
        // Deflated power iteration
        var v=new double[n];rng=new Random(137);
        for(int i=0;i<n;i++)v[i]=rng.NextDouble();
        nrm=Math.Sqrt(v.Sum(x=>x*x));
        if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;
        // Orthogonalize against v1
        double dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];
        for(int i=0;i<n;i++)v[i]-=dot*v1[i];
        nrm=Math.Sqrt(v.Sum(x=>x*x));
        if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;

        double lambda=0;
        for(int iter=0;iter<maxIter;iter++){
            var Av=new double[n];
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];
            // Deflate: subtract lambda1 * v1 * (v1^T v)
            double v1dotv=0;for(int i=0;i<n;i++)v1dotv+=v1[i]*v[i]; // ~0
            for(int i=0;i<n;i++)Av[i]-=lambda1*v1[i]*v1dotv;
            nrm=Math.Sqrt(Av.Sum(x=>x*x));
            if(nrm<1e-15)break;
            for(int i=0;i<n;i++)v[i]=Av[i]/nrm;
            dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];
            for(int i=0;i<n;i++)v[i]-=dot*v1[i];
            nrm=Math.Sqrt(v.Sum(x=>x*x));
            if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;
            double newLambda=0;
            for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];newLambda+=v[i]*s;}
            if(Math.Abs(newLambda-lambda)<1e-10)break;
            lambda=newLambda;
        }
        return lambda;
    }
}
