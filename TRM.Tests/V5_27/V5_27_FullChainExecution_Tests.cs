using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_27;

[Trait("Category","V5_27"),Trait("Category","V5_27_FCE"),Trait("Category","LongRunning")]
public class V5_27_FullChainExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct ChainProfile{
        public int n,s,cohort;
        public double omDist,lambda1,kStd,dTail,dT1,deltaD,deltaK,kSens,c3OmgS,omegaPerK,rebMag,deltaAlign,omT1,omT2;
        public int opkSign;public bool rulePos,rescued,persistent,a0,hasPosSign,hasC3Gain,hasLargeDeltaD,hasLargeDeltaK;
        // Normalized variables for calibration
        public double normDeltaD,normDeltaK,normTail,normC3,normOmegaPK;
    }

    public V5_27_FullChainExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    ChainProfile? BuildChainProfile(int n,int s,P3 hi,P3 lo){
        var p=new ChainProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0=sb.Value.d0;
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);
        p.dT1=Dm(dT1,n);p.lambda1=Lambda1(KT1,n);p.kStd=Ks(KT1,n);
        p.omT1=Of(hT1,n).Average();p.omDist=THR-p.omT1;
        p.rulePos=p.omDist<0.5&&p.lambda1<0.95;
        var dVals=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)dVals.Add(dT1[i,j]);dVals.Sort();
        p.dTail=Percentile(dVals,0.95)-Percentile(dVals,0.50);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        double omT2=Of(hT2,n).Average();p.omT2=omT2;p.a0=omT2>THR;
        p.rebMag=Dm(DL(Nm(RP(hT2,n),n),n),n)-p.dT1;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);
            double dv2=hi.dm-lo.dm,kv2=hi.km-lo.km,vn2=Math.Sqrt(dv2*dv2+kv2*kv2);
            p.deltaD=dmPost-dmPre;p.deltaK=kmPost-kmPre;
            p.deltaAlign=vn2>0?(((dmPost-lo.dm)*dv2+(kmPost-lo.km)*kv2)/vn2-((dmPre-lo.dm)*dv2+(kmPre-lo.km)*kv2)/vn2):0;
            p.kSens=p.deltaK/Math.Max(1e-9,Math.Abs(p.deltaD));
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            p.c3OmgS=omC3-omT2;p.omegaPerK=p.c3OmgS/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.opkSign=p.omegaPerK>0.1?1:p.omegaPerK<-0.1?-1:0;
            bool c3=omC3>THR;p.rescued=c3&&!p.a0;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=c3&&Of(hCont,n).Average()>THR;
        }else{p.rescued=false;p.persistent=false;}
        p.hasPosSign=p.opkSign>0;p.hasC3Gain=p.c3OmgS>0.05;
        p.hasLargeDeltaD=Math.Abs(p.deltaD)>0.02;p.hasLargeDeltaK=Math.Abs(p.deltaK)>0.01;
        return p;
    }

    [Fact]
    public void FCE_01_BaselineInformationAudit()
    {
        _o.WriteLine("═══════════════════════════════════════════════════");
        _o.WriteLine("═══ FCE_01: Baseline Information Audit ═══");
        _o.WriteLine("═══ Does information accumulate along the chain? ═══");
        _o.WriteLine("═══════════════════════════════════════════════════");

        // ─── Collect profiles ───
        int[] Ns={64,65,66,67,70,72,75,78,80};
        var profiles=new ConcurrentBag<ChainProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildChainProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();
        _o.WriteLine($"\nProfiles collected: {all.Length}");
        _o.WriteLine($"Rescued (persistent): {all.Count(p=>p.persistent)} ({all.Count(p=>p.persistent)*100.0/Math.Max(1,all.Length):F1}%)");
        _o.WriteLine($"Sign+: {all.Count(p=>p.hasPosSign)}");
        _o.WriteLine($"C3 gain present: {all.Count(p=>p.hasC3Gain)}");

        // ─── Normalize for calibration ───
        Normalize(all);

        // ─── Global baseline ───
        double baseRate=all.Count(p=>p.persistent)*100.0/Math.Max(1,all.Length);

        // ─── Holdout split ───
        var rng=new Random(42);
        var shuffled=all.OrderBy(_=>rng.Next()).ToArray();
        int splitIdx=shuffled.Length/2;
        var train=shuffled.Take(splitIdx).ToArray();
        var test=shuffled.Skip(splitIdx).ToArray();

        _o.WriteLine($"\nHoldout: train={train.Length}, test={test.Length}");
        _o.WriteLine($"Baseline rescue rate (all): {baseRate:F1}%");

        // ─── Define stage evaluators ───
        _o.WriteLine($"\n═══ STAGE 1: Single-component information ═══");

        // Stage 1a: omegaPerK sign only
        var s1a=EvaluateSingle(test,"omegaPerK sign only",p=>p.hasPosSign,baseRate);
        ReportStage(_o,"1a",s1a);

        // Stage 1b: c3OmegaShift only (threshold)
        var s1b=EvaluateSingle(test,"c3OmegaShift > 0.1",p=>p.c3OmgS>0.1,baseRate);
        ReportStage(_o,"1b",s1b);

        _o.WriteLine($"\n═══ STAGE 2: Two-component information ═══");

        // Stage 2a: omegaPerK sign + c3OmegaShift
        var s2a=EvaluateCombination(test,"sign+ & c3OmgS>0.1",p=>p.hasPosSign&&p.c3OmgS>0.1,baseRate);
        ReportStage(_o,"2a",s2a);

        // Stage 2b: omegaPerK sign + c3OmegaShift (strict)
        var s2b=EvaluateCombination(test,"sign+ & c3OmgS>0.5",p=>p.hasPosSign&&p.c3OmgS>0.5,baseRate);
        ReportStage(_o,"2b",s2b);

        _o.WriteLine($"\n═══ STAGE 3: Mechanism variables (d→K only) ═══");

        // Stage 3a: d_tail + deltaD + deltaK combined as binary
        var s3a=EvaluateCombination(test,"d-tail + deltaD + deltaK (any 2/3)",p=>{
            int c=0;if(p.dTail>PercentileOf(all,a=>a.dTail,0.5))c++;if(Math.Abs(p.deltaD)>PercentileOf(all,a=>Math.Abs(a.deltaD),0.5))c++;if(Math.Abs(p.deltaK)>PercentileOf(all,a=>Math.Abs(a.deltaK),0.5))c++;return c>=2&&p.hasPosSign;
        },baseRate);
        ReportStage(_o,"3a",s3a);

        // Stage 3b: d_tail + deltaD + deltaK continuous calibration
        var s3b=EvaluateContinuous(test,"d-tail+deltaD+deltaK cont.",p=>{
            double s=0;s+=p.normTail;s+=Math.Abs(p.normDeltaD);s+=Math.Abs(p.normDeltaK);return s/3.0;
        },baseRate);
        ReportStage(_o,"3b",s3b);

        _o.WriteLine($"\n═══ STAGE 4: Full chain (no sign-rule filter) ═══");

        // Stage 4a: Full chain binary (sign+ + c3OmgS + mechanism)
        var s4a=EvaluateCombination(test,"full chain (sign+ & c3>0.1 & 2/3 mech)",p=>{
            int c=0;if(p.dTail>PercentileOf(all,a=>a.dTail,0.5))c++;if(Math.Abs(p.deltaD)>PercentileOf(all,a=>Math.Abs(a.deltaD),0.5))c++;if(Math.Abs(p.deltaK)>PercentileOf(all,a=>Math.Abs(a.deltaK),0.5))c++;return p.hasPosSign&&p.c3OmgS>0.1&&c>=2;
        },baseRate);
        ReportStage(_o,"4a",s4a);

        // Stage 4b: Full chain continuous calibration
        var s4b=EvaluateContinuous(test,"full chain continuous",p=>{
            double s=0;s+=p.normTail;s+=Math.Abs(p.normDeltaD);s+=Math.Abs(p.normDeltaK);s+=p.hasPosSign?0.5:0;s+=Math.Clamp(p.normC3,0,1)*0.5;return s/Math.Max(1e-9,2.5);
        },baseRate);
        ReportStage(_o,"4b",s4b);

        // ─── Information accumulation summary ───
        _o.WriteLine($"\n═══════════════════════════════════════════════════");
        _o.WriteLine($"═══ INFORMATION ACCUMULATION SUMMARY ═══");
        _o.WriteLine($"═══════════════════════════════════════════════════");
        _o.WriteLine($"Baseline rescue rate: {baseRate:F1}%");

        var allStages=new[]{
            ("1a","omegaPerK sign only",s1a),
            ("1b","c3OmegaShift only",s1b),
            ("2a","sign + c3OmgS>0.1",s2a),
            ("2b","sign + c3OmgS>0.5",s2b),
            ("3a","d-chains (binary)",s3a),
            ("3b","d-chains (continuous)",s3b),
            ("4a","full chain (binary)",s4a),
            ("4b","full chain (continuous)",s4b),
        };

        _o.WriteLine($"\n{0,-4} {1,-24} {2,6} {3,7} {4,7} {5,9} {6,9} {7,9}",
            "ID","Stage","n","Rescue%","Enrich","Brier","BS Base","Delta");

        foreach(var (id,name,st) in allStages){
            _o.WriteLine($"{id,-4} {name,-24} {st.count,6} {st.rescueRate,6:F1}% {st.enrichment,6:F1}x {st.brierScore,8:F4} {st.brierBase,8:F4} {st.brierDelta,8:F4}");
        }

        // ─── Inter-stage delta analysis ───
        _o.WriteLine($"\n═══ INCREMENTAL INFORMATION DELTA ═══");
        _o.WriteLine($"Delta = rescue rate improvement over best prior stage");

        var stagesOrdered=allStages.OrderBy(s=>s.Item3.rescueRate).ToArray();
        double bestSoFar=0;
        foreach(var (id,name,st) in stagesOrdered){
            double delta=st.rescueRate-bestSoFar;
            _o.WriteLine($"{id,-4} {name,-24} rescue={st.rescueRate,5:F1}% bestPrev={bestSoFar,5:F1}% delta={delta,+5:F1}%");
            if(st.rescueRate>bestSoFar)bestSoFar=st.rescueRate;
        }

        // ─── Cross-N dominance ───
        _o.WriteLine($"\n═══ CROSS-N DOMINANCE ═══");
        _o.WriteLine($"Does the full chain outperform sign-rule at every N?");
        _o.WriteLine($"{0,4} {1,6} {2,6} {3,8} {4,8} {5,8}",
            "N","sign+","selected","sign%","chain%","delta");

        var chainSelector=new Func<ChainProfile,bool>(p=>{
            int c=0;if(p.dTail>PercentileOf(all,a=>a.dTail,0.5))c++;if(Math.Abs(p.deltaD)>PercentileOf(all,a=>Math.Abs(a.deltaD),0.5))c++;if(Math.Abs(p.deltaK)>PercentileOf(all,a=>Math.Abs(a.deltaK),0.5))c++;return p.hasPosSign&&p.c3OmgS>0.1&&c>=2;
        });

        foreach(var n in Ns){
            var subN=all.Where(p=>p.n==n).ToArray();
            var signSub=subN.Where(p=>p.hasPosSign).ToArray();
            var chainSub=subN.Where(chainSelector).ToArray();
            if(signSub.Length==0)continue;
            double signR=signSub.Count(p=>p.persistent)*100.0/Math.Max(1,signSub.Length);
            double chainR=chainSub.Length>0?chainSub.Count(p=>p.persistent)*100.0/Math.Max(1,chainSub.Length):0;
            _o.WriteLine($"{n,4} {signSub.Length,6} {chainSub.Length,6} {signR,7:F1}% {chainR,7:F1}% {chainR-signR,+7:F1}%");
        }

        // ─── Uncertainty: bootstrap CI for full chain ───
        _o.WriteLine($"\n═══ BOOTSTRAP UNCERTAINTY (1000 resamples) ═══");

        var bootstrapRng=new Random(123);
        int B=1000;
        var signResamples=new double[B];var chainResamples=new double[B];
        var allArr=all;
        for(int b=0;b<B;b++){
            var sample=new ChainProfile[allArr.Length];
            for(int i=0;i<allArr.Length;i++)sample[i]=allArr[bootstrapRng.Next(allArr.Length)];
            var signSub=sample.Where(p=>p.hasPosSign).ToArray();
            var chainSub=sample.Where(chainSelector).ToArray();
            signResamples[b]=signSub.Length>0?signSub.Count(p=>p.persistent)*100.0/Math.Max(1,signSub.Length):0;
            chainResamples[b]=chainSub.Length>0?chainSub.Count(p=>p.persistent)*100.0/Math.Max(1,chainSub.Length):0;
        }
        Array.Sort(signResamples);Array.Sort(chainResamples);

        _o.WriteLine($"{"",-14} {"Mean",6} {"2.5%",7} {"50%",7} {"97.5%",7}");
        _o.WriteLine($"{"sign+",-14} {signResamples.Average(),6:F1}% {signResamples[25],6:F1}% {signResamples[500],6:F1}% {signResamples[974],6:F1}%");
        _o.WriteLine($"{"full chain",-14} {chainResamples.Average(),6:F1}% {chainResamples[25],6:F1}% {chainResamples[500],6:F1}% {chainResamples[974],6:F1}%");

        // ─── Final determination ───
        bool chainBeatsSign=chainResamples.Average()>signResamples.Average();
        bool chainCINonOverlap=chainResamples[25]>signResamples[974]||chainResamples[974]<signResamples[25];
        double maxStageRescue=stagesOrdered.Last().Item3.rescueRate;
        double minStageRescue=stagesOrdered.First().Item3.rescueRate;

        _o.WriteLine($"\n═══════════════════════════════════════════════════");
        _o.WriteLine($"═══ FCE_01 DETERMINATION ═══");
        _o.WriteLine($"═══════════════════════════════════════════════════");
        _o.WriteLine($"Full chain beats sign-only: {(chainBeatsSign?"YES":"NO")}");
        _o.WriteLine($"95% CI non-overlapping: {(chainCINonOverlap?"YES":"NO")}");
        _o.WriteLine($"Rescue rate range across stages: {minStageRescue:F1}% – {maxStageRescue:F1}%");
        _o.WriteLine($"Best stage: {stagesOrdered.Last().Item2} ({stagesOrdered.Last().Item3.rescueRate:F1}%)");
        _o.WriteLine($"Information accumulates: {(maxStageRescue>stagesOrdered.First().Item3.rescueRate+3?"YES (rescue rate improves by "+$"{maxStageRescue-stagesOrdered.First().Item3.rescueRate:F1}pp)":"MARGINAL")}");

        _o.WriteLine($"\n═══ Analysis complete. See report for SUPPORTED/CONDITIONAL/HYPOTHESIS/NOT CLAIMED. ═══");
    }

    // ─── Analysis helpers ───

    struct StageResult{
        public string name;public int count;public double rescueRate,enrichment,brierScore,brierBase,brierDelta;
    }

    static StageResult EvaluateSingle(ChainProfile[] test,string name,Func<ChainProfile,bool> selector,double baseRate){
        var sub=test.Where(selector).ToArray();
        double rate=sub.Length>0?sub.Count(p=>p.persistent)*100.0/Math.Max(1,sub.Length):0;
        double enrich=baseRate>0?rate/baseRate:0;
        double brier=sub.Length>0?sub.Average(p=>Math.Pow((p.persistent?1.0:0.0)-rate/100.0,2)):0;
        double brierBase=test.Length>0?test.Average(p=>Math.Pow((p.persistent?1.0:0.0)-baseRate/100.0,2)):0;
        return new StageResult{name=name,count=sub.Length,rescueRate=rate,enrichment=enrich,brierScore=brier,brierBase=brierBase,brierDelta=brier-brierBase};
    }

    static StageResult EvaluateCombination(ChainProfile[] test,string name,Func<ChainProfile,bool> selector,double baseRate){
        var sub=test.Where(selector).ToArray();
        double rate=sub.Length>0?sub.Count(p=>p.persistent)*100.0/Math.Max(1,sub.Length):0;
        double enrich=baseRate>0?rate/baseRate:0;
        double brier=sub.Length>0?sub.Average(p=>Math.Pow((p.persistent?1.0:0.0)-rate/100.0,2)):0;
        double brierBase=test.Length>0?test.Average(p=>Math.Pow((p.persistent?1.0:0.0)-baseRate/100.0,2)):0;
        return new StageResult{name=name,count=sub.Length,rescueRate=rate,enrichment=enrich,brierScore=brier,brierBase=brierBase,brierDelta=brier-brierBase};
    }

    static StageResult EvaluateContinuous(ChainProfile[] test,string name,Func<ChainProfile,double> score,double baseRate){
        // Split at median score
        var scores=test.Select(p=>(p,score(p))).Where(x=>!double.IsNaN(x.Item2)).ToArray();
        if(scores.Length==0)return new StageResult{name=name,count=0,rescueRate=0,enrichment=0};
        double median=scores.OrderBy(x=>x.Item2).ElementAt(scores.Length/2).Item2;
        var high=scores.Where(x=>x.Item2>=median).Select(x=>x.p).ToArray();
        double rate=high.Length>0?high.Count(p=>p.persistent)*100.0/Math.Max(1,high.Length):0;
        double enrich=baseRate>0?rate/baseRate:0;
        double brier=high.Length>0?high.Average(p=>Math.Pow((p.persistent?1.0:0.0)-rate/100.0,2)):0;
        double brierBase=test.Length>0?test.Average(p=>Math.Pow((p.persistent?1.0:0.0)-baseRate/100.0,2)):0;
        return new StageResult{name=name,count=high.Length,rescueRate=rate,enrichment=enrich,brierScore=brier,brierBase=brierBase,brierDelta=brier-brierBase};
    }

    static void ReportStage(ITestOutputHelper o,string id,StageResult s){
        o.WriteLine($"{id}: {s.name} | n={s.count} rescue={s.rescueRate:F1}% enrich={s.enrichment:F1}x brier={s.brierScore:F4} Δbrier={s.brierDelta:+0.0000;-0.0000}");
    }

    static void Normalize(ChainProfile[] all){
        double dtMax=all.Max(p=>p.dTail);double ddMax=all.Max(p=>Math.Abs(p.deltaD));
        double dkMax=all.Max(p=>Math.Abs(p.deltaK));double c3Max=all.Max(p=>p.c3OmgS);
        double opkMax=all.Max(p=>Math.Abs(p.omegaPerK));
        for(int i=0;i<all.Length;i++){
            all[i].normTail=dtMax>0?all[i].dTail/dtMax:0;
            all[i].normDeltaD=ddMax>0?all[i].deltaD/ddMax:0;
            all[i].normDeltaK=dkMax>0?all[i].deltaK/dkMax:0;
            all[i].normC3=c3Max>0?all[i].c3OmgS/c3Max:0;
            all[i].normOmegaPK=opkMax>0?all[i].omegaPerK/opkMax:0;
        }
    }

    static double PercentileOf(ChainProfile[] ps,Func<ChainProfile,double> f,double p){
        var vals=ps.Select(f).Where(v=>!double.IsNaN(v)&&!double.IsInfinity(v)).OrderBy(v=>v).ToArray();
        if(vals.Length==0)return 0;
        return vals[Math.Clamp((int)(p*(vals.Length-1)),0,vals.Length-1)];
    }

    // ─── Frozen M3++ simulation ───

    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
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
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))return null;
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
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    static double Percentile(List<double> s,double p){if(s.Count==0)return 0;return s[Math.Clamp((int)(p*(s.Count-1)),0,s.Count-1)];}
}
