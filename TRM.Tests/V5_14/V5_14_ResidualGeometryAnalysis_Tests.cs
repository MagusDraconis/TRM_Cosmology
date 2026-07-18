using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_14;

[Trait("Category","V5_14"),Trait("Category","V5_14_RGA"),Trait("Category","LongRunning")]
public class V5_14_ResidualGeometryAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct Trial{public int seed,cohort;public string cls;public double omT1,omT2,dT1;public bool immHi,persist;}

    struct RProfile{
        public int seed,cohort,n;public string cls;
        public double projHiVec,dMean,dStd,dTailWidth,lambda2,spectralGap;
        public double orthHiVec,dVelocity,top1ES,kFrob,kVelocity;
        public bool immHi,persist,selM3;public double omT1,omT2,dT1;
    }

    public V5_14_ResidualGeometryAnalysis_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void RGA_01_SingleFeatureResidualValidation(){
        _o.WriteLine("═══ RGA_01: Single-feature residual validation ═══");
        var profiles=CollectProfiles();

        // Candidate residual features with their extractors
        var candidates=new[]{
            ("dStd",F(p=>p.dStd)),("lambda2",F(p=>p.lambda2)),
            ("dTailWidth",F(p=>p.dTailWidth)),("spectralGap",F(p=>p.spectralGap)),
            ("kVelocity",F(p=>p.kVelocity)),("orthHiVec",F(p=>p.orthHiVec)),
            ("dVelocity",F(p=>p.dVelocity)),("top1%",F(p=>p.top1ES)),
            ("kFrob",F(p=>p.kFrob)),("dMean",F(p=>p.dMean))
        };

        _o.WriteLine("\n─── M3 Baseline (Frozen) ───");
        PrintM3Baseline(profiles);

        _o.WriteLine("\n─── Single-feature residual selector validation ───");
        _o.WriteLine("Training on 0-99, testing on 100-199. Threshold via best balanced accuracy.");
        _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,8} {4,8} {5,6} {6,8} {7,10}",
            "Feature","Thresh","TrAcc","S0Rate","S+Rate","S+Cnt","HoldLift","Verdict"));

        foreach(var (name,extract) in candidates){
            // Train on reference only (0-99, P1/P1b + M3 selected)
            var train=profiles.Where(p=>p.cohort==0&&p.selM3).ToArray();
            if(train.Length<4){_o.WriteLine($"{name,-14} {"-",8} {"-",8} {"-",8} {"-",8} {"-",6} {"-",8} {"SKIP",10}");continue;}

            // Find best threshold
            double bestThresh=0,bestAcc=0;
            var sorted=train.OrderBy(p=>extract(p)).ToArray();
            for(int i=1;i<sorted.Length;i++){
                double t=(extract(sorted[i-1])+extract(sorted[i]))/2;
                int a=sorted.Count(p=>extract(p)>t&&p.persist);
                int b=sorted.Count(p=>extract(p)<=t&&!p.persist);
                double acc=(double)(a+b)/sorted.Length;
                if(acc>bestAcc){bestAcc=acc;bestThresh=t;}
            }

            // Test on holdout (100-199, selected by M3 AND residual feature)
            var hold=profiles.Where(p=>p.cohort==1&&p.selM3).ToArray();
            var sel=hold.Where(p=>extract(p)>bestThresh).ToArray();
            double s0rate=hold.Length>0?(double)hold.Count(p=>p.persist)/hold.Length:0;
            double sRate=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;

            string verdict;
            if(sRate>s0rate+0.05)verdict="IMPROVED";
            else if(sRate>s0rate+0.02)verdict="MARGINAL";
            else if(sRate>=s0rate-0.02)verdict="NEUTRAL";
            else verdict="HARMFUL";

            _o.WriteLine($"{name,-14} {bestThresh,8:F4} {bestAcc,7:P0} {s0rate,8:P0} {sRate,8:P0} {sel.Length,6} {sRate-s0rate,8:+0.0%} {verdict,10}");
        }

        // N-specific: N=71 and N=72 separately
        _o.WriteLine("\n─── Per-N holdout lift ───");
        _o.WriteLine(string.Format("{0,-14} {1,8} {2,8} {3,8}","Feature","N=71Lift","N=72Lift","N=75Lift"));
        foreach(var (name,extract) in candidates.Take(6)){
            var train=profiles.Where(p=>p.cohort==0&&p.selM3).ToArray();
            if(train.Length<4)continue;
            double bestThresh=FindBestThresh(train,extract);
            string lifts="";
            foreach(var n in new[]{71,72,75}){
                var hold=profiles.Where(p=>p.cohort==1&&p.selM3&&p.n==n).ToArray();
                var sel=hold.Where(p=>extract(p)>bestThresh).ToArray();
                double s0=hold.Length>0?(double)hold.Count(p=>p.persist)/hold.Length:0;
                double sr=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
                lifts+=$"{(sel.Length>0?sr-s0:0),7:+0.0%} ";
            }
            _o.WriteLine($"{name,-14} {lifts}");
        }

        _profiles=profiles;
    }

    [Fact]public void RGA_02_CrossNStability(){
        _o.WriteLine("═══ RGA_02: Cross-N stability ═══");
        var profiles=EnsureProfiles();

        var feats=new[]{("dStd",F(p=>p.dStd)),("lambda2",F(p=>p.lambda2)),
            ("dTailWidth",F(p=>p.dTailWidth)),("orthHiVec",F(p=>p.orthHiVec)),
            ("dVelocity",F(p=>p.dVelocity))};

        _o.WriteLine(string.Format("\n{0,-14} {1,6} {2,8} {3,8} {4,8} {5,8} {6,10}",
            "Feature","N","TrThresh","TrAcc","HoldLift","SelCnt","Stable"));
        foreach(var (name,extract) in feats){
            var train=profiles.Where(p=>p.cohort==0&&p.selM3).ToArray();
            double t=FindBestThresh(train,extract);
            foreach(var n in new[]{71,72,75}){
                var hold=profiles.Where(p=>p.cohort==1&&p.selM3&&p.n==n).ToArray();
                var sel=hold.Where(p=>extract(p)>t).ToArray();
                double s0=hold.Length>0?(double)hold.Count(p=>p.persist)/hold.Length:0;
                double sr=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
                double acc=train.Length>0?(double)train.Count(p=>(extract(p)>t)==p.persist)/train.Length:0;
                bool stable=sel.Length>=2&&Math.Abs(sr-s0)<0.15;
                _o.WriteLine($"{name,-14} {n,6} {t,8:F4} {acc,7:P0} {sr-s0,8:+0.0%} {sel.Length,8} {(stable?"OK":"UNSTABLE"),10}");
            }
        }
    }

    [Fact]public void RGA_03_N75ResidualAnalysis(){
        _o.WriteLine("═══ RGA_03: N=75 residual analysis ═══");
        var profiles=EnsureProfiles();

        var n75Train=profiles.Where(p=>p.n==75&&p.cohort==0&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        var n75Hold=profiles.Where(p=>p.n==75&&p.cohort==1&&(p.cls=="P1"||p.cls=="P1b")).ToArray();

        var feats75=new[]{
            ("dMean",F(p=>p.dMean),false),("dVelocity",F(p=>p.dVelocity),false),
            ("top1%",F(p=>p.top1ES),true),("orthHiVec",F(p=>p.orthHiVec),true),
            ("dStd",F(p=>p.dStd),true),("lambda2",F(p=>p.lambda2),true)};

        _o.WriteLine("\n─── N=75 specific residual selectors ───");
        _o.WriteLine("Trained on N=75 reference (0-99), tested on N=75 holdout (100-199).");
        _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,8} {4,8} {5,8} {6,10}",
            "Feature","Thresh","TrAcc","S0Rate","S+Rate","S+Cnt","Lift"));
        double n75s0=n75Hold.Length>0?(double)n75Hold.Count(p=>p.persist)/n75Hold.Length:0;

        foreach(var (name,extract,invert) in feats75){
            if(n75Train.Length<4)continue;
            double bestThresh=0,bestAcc=0;
            var sorted=n75Train.OrderBy(p=>extract(p)).ToArray();
            for(int i=1;i<sorted.Length;i++){
                double t=(extract(sorted[i-1])+extract(sorted[i]))/2;
                int a=invert?sorted.Count(p=>extract(p)<t&&p.persist):sorted.Count(p=>extract(p)>t&&p.persist);
                int b=invert?sorted.Count(p=>extract(p)>=t&&!p.persist):sorted.Count(p=>extract(p)<=t&&!p.persist);
                double acc=(double)(a+b)/sorted.Length;
                if(acc>bestAcc){bestAcc=acc;bestThresh=t;}
            }
            var sel=invert?n75Hold.Where(p=>extract(p)<bestThresh).ToArray():
                n75Hold.Where(p=>extract(p)>bestThresh).ToArray();
            double sr=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
            _o.WriteLine($"{name,-14} {bestThresh,8:F4} {bestAcc,7:P0} {n75s0,8:P0} {sr,8:P0} {sel.Length,8} {sr-n75s0,9:+0.0%}");
        }
    }

    [Fact]public void RGA_04_OverfitAudit(){
        _o.WriteLine("═══ RGA_04: Overfit audit ═══");
        var profiles=EnsureProfiles();

        var candidates=new[]{
            ("dStd",F(p=>p.dStd)),("lambda2",F(p=>p.lambda2)),
            ("dTailWidth",F(p=>p.dTailWidth)),("orthHiVec",F(p=>p.orthHiVec)),
            ("dVelocity",F(p=>p.dVelocity)),("kVelocity",F(p=>p.kVelocity)),
            ("spectralGap",F(p=>p.spectralGap))};

        _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,8} {4,8} {5,10}",
            "Feature","TrLift","HoLift","Gap","N-Harm","Verdict"));
        foreach(var (name,extract) in candidates){
            var train=profiles.Where(p=>p.cohort==0&&p.selM3).ToArray();
            if(train.Length<4)continue;
            double t=FindBestThresh(train,extract);
            var trSel=train.Where(p=>extract(p)>t).ToArray();
            double trLift=train.Length>0?((double)trSel.Count(p=>p.persist)/Math.Max(1,trSel.Length)-(double)train.Count(p=>p.persist)/train.Length):0;

            bool anyHarm=false;
            string nHarm="";
            foreach(var n in new[]{71,72,75}){
                var hold=profiles.Where(p=>p.cohort==1&&p.selM3&&p.n==n).ToArray();
                var sel=hold.Where(p=>extract(p)>t).ToArray();
                double s0=hold.Length>0?(double)hold.Count(p=>p.persist)/hold.Length:0;
                double sr=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
                if(sel.Length>0&&sr<s0-0.03){anyHarm=true;nHarm+=$"N{n} ";}
            }

            var hoAll=profiles.Where(p=>p.cohort==1&&p.selM3).ToArray();
            var hoSel=hoAll.Where(p=>extract(p)>t).ToArray();
            double hoLift=hoAll.Length>0?((double)hoSel.Count(p=>p.persist)/Math.Max(1,hoSel.Length)-(double)hoAll.Count(p=>p.persist)/hoAll.Length):0;
            double gap=trLift-hoLift;

            string verdict=anyHarm?"OVERFIT":(gap>0.15?"OVERFIT_WARN":(gap>0.08?"MARGINAL":"OK"));
            _o.WriteLine($"{name,-14} {trLift,7:+0.0%} {hoLift,8:+0.0%} {gap,7:+0.0%} {nHarm,8} {verdict,10}");
        }
    }

    [Fact]public void RGA_05_ModelComparison(){
        _o.WriteLine("═══ RGA_05: Model comparison ═══");
        var profiles=EnsureProfiles();

        _o.WriteLine("\n─── Model candidates ───");
        _o.WriteLine("M3: P1/P1b + projHiVec > -0.3281 (frozen)");
        _o.WriteLine("M3+: M3 + best residual feature (if validated)");

        // Find best residual feature
        var candidates=new[]{
            ("dStd",F(p=>p.dStd)),("lambda2",F(p=>p.lambda2)),
            ("dTailWidth",F(p=>p.dTailWidth)),("orthHiVec",F(p=>p.orthHiVec)),
            ("dVelocity",F(p=>p.dVelocity))};
        string bestFeat="";double bestLift=0;double bestT=0;Func<RProfile,double>? bestF=null;
        foreach(var (name,extract) in candidates){
            var train=profiles.Where(p=>p.cohort==0&&p.selM3).ToArray();
            if(train.Length<4)continue;
            double t=FindBestThresh(train,extract);
            var ho=profiles.Where(p=>p.cohort==1&&p.selM3).ToArray();
            var sel=ho.Where(p=>extract(p)>t).ToArray();
            double s0=ho.Length>0?(double)ho.Count(p=>p.persist)/ho.Length:0;
            double sr=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
            if(sr-s0>bestLift&&sel.Length>=3){bestLift=sr-s0;bestFeat=name;bestT=t;bestF=extract;}
        }

        _o.WriteLine($"Best residual: {bestFeat} (holdout lift={bestLift:+.0%}, selCount>0)");

        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,8} {3,8} {4,8} {5,10}",
            "N","Cohort","M3","M3+","SelCnt","Better"));
        foreach(var n in new[]{71,72,75}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh&&p.selM3).ToArray();
                double m3=sub.Length>0?(double)sub.Count(p=>p.persist)/sub.Length:0;
                var selPlus=bestF!=null?sub.Where(p=>bestF(p)>bestT).ToArray():sub;
                double m3p=selPlus.Length>0?(double)selPlus.Count(p=>p.persist)/selPlus.Length:0;
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {m3,7:P0} {m3p,8:P0} {selPlus.Length,8} {(m3p>=m3?"M3+":"M3"),10}");
            }
        }

        _o.WriteLine("\n─── Conclusion ───");
        if(bestLift>0.03)_o.WriteLine($"M3+ with {bestFeat} improves holdout. Model can be refined.");
        else _o.WriteLine("No residual feature materially improves holdout. M3 is best current model.");
    }

    [Fact]public void RGA_06_ControlLimitAndGates(){
        _o.WriteLine("═══ RGA_06: Control limit assessment and decision gates ═══");
        var profiles=EnsureProfiles();

        // Evaluate all features
        var candidates=new[]{
            ("dStd",F(p=>p.dStd)),("lambda2",F(p=>p.lambda2)),
            ("dTailWidth",F(p=>p.dTailWidth)),("orthHiVec",F(p=>p.orthHiVec)),
            ("dVelocity",F(p=>p.dVelocity)),("kVelocity",F(p=>p.kVelocity)),
            ("spectralGap",F(p=>p.spectralGap)),("kFrob",F(p=>p.kFrob)),
            ("top1%",F(p=>p.top1ES))};
        bool anyImprove=false,anyOverfit=false,anyN75=false;
        double bestHoLift=0;string bestName="";

        foreach(var (name,extract) in candidates){
            var train=profiles.Where(p=>p.cohort==0&&p.selM3).ToArray();
            if(train.Length<4)continue;
            double t=FindBestThresh(train,extract);
            var ho=profiles.Where(p=>p.cohort==1&&p.selM3).ToArray();
            var sel=ho.Where(p=>extract(p)>t).ToArray();
            double s0=ho.Length>0?(double)ho.Count(p=>p.persist)/ho.Length:0;
            double sr=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
            if(sr>s0+0.03&&sel.Length>=3){anyImprove=true;if(sr-s0>bestHoLift){bestHoLift=sr-s0;bestName=name;}}

            var trSel=train.Where(p=>extract(p)>t).ToArray();
            double trLift=train.Length>0?((double)trSel.Count(p=>p.persist)/Math.Max(1,trSel.Length)-(double)train.Count(p=>p.persist)/train.Length):0;
            if(trLift-hoLift(ho,extract,t)>0.15)anyOverfit=true;

            var n75h=profiles.Where(p=>p.n==75&&p.cohort==1&&p.selM3).ToArray();
            var n75s=n75h.Where(p=>extract(p)>t).ToArray();
            if(n75h.Length>0&&n75s.Length>=2&&(double)n75s.Count(p=>p.persist)/n75s.Length>(double)n75h.Count(p=>p.persist)/n75h.Length+0.03)anyN75=true;
        }

        _o.WriteLine($"\nGate A — Residual Feature Improves: {(anyImprove?$"REACHED — {bestName} (+{bestHoLift:.0%})":"NOT REACHED")}");
        _o.WriteLine($"Gate B — dStd Validated: {(anyImprove&&bestName=="dStd"?"REACHED":"SEE ABOVE")}");
        _o.WriteLine($"Gate C — Spectral Validated: {(anyImprove&&(bestName=="lambda2"||bestName=="spectralGap")?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D — N=75 Specific: {(anyN75?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E — Overfit Warning: {(anyOverfit?"REACHED — some features overfit":"NOT REACHED")}");
        _o.WriteLine($"Gate F — Control Ceiling: {(!anyImprove?$"REACHED — no residual feature improves holdout":"NOT REACHED")}");
        _o.WriteLine($"Gate G — Residual Model: {(anyImprove&&!anyOverfit?$"REACHED — M3 + {bestName}":"NOT REACHED")}");

        _o.WriteLine("\n─── Control Limit Assessment ───");
        if(anyImprove)_o.WriteLine($"M3 can be refined with {bestName} (+{bestHoLift:.0%} holdout lift).");
        else _o.WriteLine("M3 is at the practical explanatory limit. No residual feature improves holdout >3%.");
        _o.WriteLine("Current model: "+(anyImprove?$"M3 + {bestName}":"M3 (unchanged)")+". Proceed to RGI limit audit.");

        _o.WriteLine("\n─── Claim Discipline ───");
        _o.WriteLine("No physical interpretation. No new hidden variables unless holdout supports.");
        _o.WriteLine("Thresholds trained on 0-99 only. No holdout tuning.");
        _o.WriteLine((anyImprove?"Residual geometry may exist → Gate A evidence.":"No residual geometry found → control ceiling hypothesis."));
        _o.WriteLine("\n─── Next: RGI_ResidualInterventionLimitAudit ───");
    }

    // ══════════════════════════════════════════════════════════════
    // ── Profile collection ──
    // ══════════════════════════════════════════════════════════════
    static RProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hiC,_loC;
    P3 Hi(int n){_hiC??=new();return _hiC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_loC??=new();return _loC.GetOrAdd(n,k=>PCent(k,false));}

    RProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();_profiles=p;return p;}
    RProfile[] CollectProfiles(){
        var bag=new ConcurrentBag<RProfile>();
        foreach(var n in new[]{67,71,72,75,80}){var hi=Hi(n);var lo=Lo(n);ProfileC(n,0,99,hi,lo,0,bag);ProfileC(n,100,199,hi,lo,1,bag);}
        return bag.ToArray();
    }
    void ProfileC(int n,int start,int end,P3 hi,P3 lo,int coh,ConcurrentBag<RProfile> bag){
        var seeds=new List<int>();for(int s=start;s<=end&&seeds.Count<MaxSeedsPerCohort;s++)if(!IsHi(n,s))seeds.Add(s);
        Parallel.ForEach(seeds.ToArray(),s=>{bag.Add(ProfileOne(n,s,hi,lo,coh));});
    }
    RProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh){
        var K=KS(n,seed);var dHist=new List<double>();var kHist=new List<double>();
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);dHist.Add(Dm(d,n));kHist.Add(Km(K,n));}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,cls=""};
        sb=Classify(sb,hi);
        var rp=new RProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0};
        // F1
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        rp.projHiVec=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double proj=rp.projHiVec,distLo2=((d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks));
        rp.orthHiVec=Math.Sqrt(Math.Max(0,distLo2-proj*proj));
        // F2-F3 from K3E
        var edges=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K3E[i,j]);
        edges.Sort();int ec=edges.Count;
        double avg=edges.Average();rp.dStd=Math.Sqrt(edges.Sum(x=>(x-avg)*(x-avg))/ec);
        rp.dTailWidth=edges[(int)(ec*0.95)]-edges[(int)(ec*0.75)];
        edges.Sort((a,b)=>b.CompareTo(a));double tot=edges.Sum();
        rp.top1ES=tot>0?edges.Take(Math.Max(1,(int)(ec*0.01))).Sum()/tot:0;
        // F4
        rp.lambda2=PowerIterationDeflated(K3E,n,PowerIteration(K3E,n,200),200);
        rp.spectralGap=PowerIteration(K3E,n,200)-rp.lambda2;
        double f=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)f+=K3E[i,j]*K3E[i,j];rp.kFrob=Math.Sqrt(f);
        // F5
        rp.dVelocity=dHist.Count>=2?dHist[^1]-dHist[^2]:0;
        rp.kVelocity=kHist.Count>=2?kHist[^1]-kHist[^2]:0;
        // Intervention
        bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var trial=RunTrial(n,seed,tgt,sb,hi,lo,coh);
        rp.immHi=trial.immHi;rp.persist=trial.persist;rp.omT1=trial.omT1;rp.omT2=trial.omT2;rp.dT1=trial.dT1;
        rp.selM3=(rp.cls=="P1"||rp.cls=="P1b")&&rp.projHiVec>PHV;
        return rp;
    }

    // ── Selector helpers ──
    static double FindBestThresh(RProfile[] train,Func<RProfile,double> extract){
        double best=0,acc=0;var s=train.OrderBy(p=>extract(p)).ToArray();
        for(int i=1;i<s.Length;i++){double t=(extract(s[i-1])+extract(s[i]))/2;int a=s.Count(p=>extract(p)>t&&p.persist),b=s.Count(p=>extract(p)<=t&&!p.persist);double ac=(double)(a+b)/s.Length;if(ac>acc){acc=ac;best=t;}}
        return best;
    }
    static Func<RProfile,double> F(Func<RProfile,double> f)=>f;
    static double hoLift(RProfile[] ho,Func<RProfile,double> extract,double t){var sel=ho.Where(p=>extract(p)>t).ToArray();return ho.Length>0?((double)sel.Count(p=>p.persist)/Math.Max(1,sel.Length)-(double)ho.Count(p=>p.persist)/ho.Length):0;}

    void PrintM3Baseline(RProfile[] profiles){
        _o.WriteLine(string.Format("{0,4} {1,10} {2,8} {3,8}","N","Cohort","S0","S1"));
        foreach(var n in new[]{71,72,75})foreach(var coh in new[]{0,1}){
            var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
            var p1=sub.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
            var sel=sub.Where(p=>p.selM3).ToArray();
            double s0=p1.Length>0?(double)p1.Count(p=>p.persist)/p1.Length:0;
            double s1=sel.Length>0?(double)sel.Count(p=>p.persist)/sel.Length:0;
            _o.WriteLine($"{n,4} {(coh==0?"Train":"Hold"),10} {s0,7:P0} {s1,8:P0}");
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ── ENGINE (frozen) ──
    // ══════════════════════════════════════════════════════════════
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
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<NE;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,int cohort){var K=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);var h5=Sim(K,n,S,seed+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);var hT1=Sim(K,n,S,seed+100);double om1=Of(hT1,n).Average();var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average();return new Trial{seed=seed,cohort=cohort,cls=b.cls,dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};}
    static double PowerIteration(double[,]A,int n,int maxIter=200){var v=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v[i]=rng.NextDouble();double norm=Math.Sqrt(v.Sum(x=>x*x));if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];norm=Math.Sqrt(Av.Sum(x=>x*x));if(norm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/norm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
    static double PowerIterationDeflated(double[,]A,int n,double lambda1,int maxIter=200){var v1=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v1[i]=rng.NextDouble();double nrm=Math.Sqrt(v1.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v1[i]/=nrm;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v1[j];nrm=Math.Sqrt(Av.Sum(x=>x*x));if(nrm<1e-15)break;for(int i=0;i<n;i++)v1[i]=Av[i]/nrm;}var v=new double[n];rng=new Random(137);for(int i=0;i<n;i++)v[i]=rng.NextDouble();nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;double dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=dot*v1[i];nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];nrm=Math.Sqrt(Av.Sum(x=>x*x));if(nrm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/nrm;dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=dot*v1[i];nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
}
