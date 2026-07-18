using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_21;

[Trait("Category","V5_21"),Trait("Category","V5_21_LRI"),Trait("Category","LongRunning")]
public class V5_21_LowNTransferredEntryReferenceAudit_Tests
{
    private readonly ITestOutputHelper _o;
    // Frozen V5.20 constants
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    // Reduced reference profile from a source N
    struct RefProfile{
        public int sourceN;public string label;
        public double refDMean,refKMean,refKSMean;
        public double vecD,vecK; // Hi-Lo entry vector in reduced space
        public double hiDMean,hiKMean,loDMean,loKMean;
    }

    // Audit diagnostic
    struct LriDiag{
        public int n,s,cohort,sourceN;public string intervention,refLabel,result;
        public double dPre,kPre,omPre,dPost,kPost,omPost,dT1,kT1,omT1,dT2,kT2,omT2;
        public double rebMag,distToRef,distToLo,projRef,orthRef,deltaD,deltaK;
        public bool a0,immInduced,strictPersist,basinSuccess,invalid;
    }

    public V5_21_LowNTransferredEntryReferenceAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    static ConcurrentDictionary<int,RefProfile>? _refCache;
    RefProfile GetRef(int sourceN){
        _refCache??=new();
        return _refCache.GetOrAdd(sourceN,k=>BuildRefProfile(k));
    }

    // ═══════════════════════════════════════════════
    // LRI_01 — Baseline reproduction
    // ═══════════════════════════════════════════════
    [Fact]public void LRI_01_BaselineReproduction(){
        _o.WriteLine("═══ LRI_01: Baseline reproduction ═══");
        _o.WriteLine("Confirm local M3++ inaccessibility at N<65.");

        int[] Ns={50,55,60,63,64,65};
        var results=new ConcurrentBag<LriDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){
                if(IsHi(n,s))continue;
                var d=RunLocalBaseline(n,s,hi,lo,"I0-local");
                if(d==null)continue;
                results.Add(d.Value);
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nBaseline: {all.Length} seeds");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,6} {4,6} {5,6}",
            "N","Seeds","A0%","A1%","Resc","dPre"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            int a0=sub.Count(r=>r.a0),resc=sub.Count(r=>r.strictPersist&&!r.a0);
            _o.WriteLine($"{n,4} {sub.Length,6} {a0*100.0/sub.Length,5:F0}% {sub.Count(r=>r.strictPersist)*100.0/sub.Length,5:F0}% {resc,6} {sub.Average(r=>r.dPre),6:F3}");
        }

        bool lowImmune=all.Where(r=>r.n<=64).All(r=>!r.strictPersist);
        bool n65Onset=all.Any(r=>r.n==65&&r.strictPersist);
        _o.WriteLine($"\nLow-N immune: {(lowImmune?"CONFIRMED":"FAILED")}");
        _o.WriteLine($"N=65 onset: {(n65Onset?"CONFIRMED":"FAILED")}");

        if(!lowImmune||!n65Onset){_o.WriteLine("STOP — baseline failed.");return;}
        _o.WriteLine($"\n─── Next: LRI_02 Transferred reference ───");
    }

    // ═══════════════════════════════════════════════
    // LRI_02 — Transferred reference test
    // ═══════════════════════════════════════════════
    [Fact]public void LRI_02_TransferredReferenceTest(){
        _o.WriteLine("═══ LRI_02: Transferred reference — does external direction help? ═══");

        int[] lowN={50,55,60,63,64};
        int[] refSources={65,70,72};
        var results=new ConcurrentBag<LriDiag>();

        // Pre-build references
        foreach(var src in refSources)GetRef(src);

        Parallel.ForEach(lowN,n=>{
            var lo=Lo(n);
            var hiLocal=Hi(n);
            foreach(var src in refSources){
                var rp=GetRef(src);
                for(int s=0;s<99;s++){
                    if(IsHi(n,s))continue;
                    var sb=SelectAndClassify(n,s,hiLocal);
                    if(sb==null)continue;

                    // I0: local baseline
                    var d0=RunLocalBaseline(n,s,hiLocal,lo,"I0-local");
                    if(d0!=null){var d=d0.Value;d.sourceN=src;results.Add(d);}

                    // I1-50%: transferred alignment
                    var d1=RunTransferredAlign(n,s,hiLocal,lo,rp,0.5,"I1-50%");
                    if(d1!=null){var d=d1.Value;d.sourceN=src;results.Add(d);}

                    // I1-75%: transferred alignment
                    var d1b=RunTransferredAlign(n,s,hiLocal,lo,rp,0.75,"I1-75%");
                    if(d1b!=null){var d=d1b.Value;d.sourceN=src;results.Add(d);}

                    // I1-100%: full transferred alignment
                    var d1c=RunTransferredAlign(n,s,hiLocal,lo,rp,1.0,"I1-100%");
                    if(d1c!=null){var d=d1c.Value;d.sourceN=src;results.Add(d);}

                    // I3: transferred C3
                    var d3=RunTransferredC3(n,s,hiLocal,lo,rp,"I3-xC3");
                    if(d3!=null){var d=d3.Value;d.sourceN=src;results.Add(d);}

                    // I4: transferred + K-preserve
                    var d4=RunTransferredKPreserve(n,s,hiLocal,lo,rp,0.5,"I4-t+K");
                    if(d4!=null){var d=d4.Value;d.sourceN=src;results.Add(d);}
                }
            }
        });

        // Also add I0 for onset N=65 as reference point
        var n65Results=new ConcurrentBag<LriDiag>();
        Parallel.ForEach(new[]{65},n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){
                if(IsHi(n,s))continue;
                var d=RunLocalBaseline(n,s,hi,lo,"I0-local");
                if(d!=null)n65Results.Add(d.Value);
            }
        });

        var all=results.ToArray();
        var n65=all.Where(r=>r.n==65||n65Results.Any(x=>x.n==65&&x.strictPersist)).ToArray();

        _o.WriteLine($"\nTransferred reference results: {all.Length} entries");
        _o.WriteLine(string.Format("\n{0,4} {1,-6} {2,-10} {3,6} {4,6} {5,6} {6,6} {7,6}",
            "N","Ref","Int","A0%","Imm%","Str%","Inv%","dPre"));
        foreach(var n in lowN.Concat(new[]{65})){
            foreach(var src in new[]{"local","R65","R70","R72"}){
                var sub=src=="local"
                    ?all.Where(r=>r.n==n&&r.intervention=="I0-local").ToArray()
                    :all.Where(r=>r.n==n&&r.refLabel==src).ToArray();
                if(sub.Length==0)continue;
                var anyImm=sub.Where(r=>r.intervention!="I0-local").ToArray();
                string intLabel=anyImm.Length>0?anyImm[0].intervention:"I0";
                double immPct=anyImm.Length>0?anyImm.Count(r=>r.immInduced)*100.0/anyImm.Length:sub.Count(r=>r.strictPersist)*100.0/sub.Length;
                double strPct=anyImm.Length>0?anyImm.Count(r=>r.strictPersist)*100.0/anyImm.Length:0;
                double a0Pct=sub.Count(r=>r.a0)*100.0/Math.Max(1,sub.Length);
                double invPct=sub.Count(r=>r.invalid)*100.0/Math.Max(1,sub.Length);
                double dPre=sub.Average(r=>r.dPre);
                _o.WriteLine($"{n,4} {src,-6} {intLabel,-10} {a0Pct,5:F0}% {immPct,5:F0}% {strPct,5:F0}% {invPct,5:F0}% {dPre,6:F3}");
            }
        }

        // Gates
        bool anyLowBreak=all.Where(r=>r.n<=64&&r.intervention!="I0-local").Any(r=>r.strictPersist);
        bool n64Break=all.Where(r=>r.n==64&&r.intervention!="I0-local").Any(r=>r.strictPersist);

        _o.WriteLine($"\nGate A (Transferred opens low-N): {(anyLowBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (N=64 opens only): {(n64Break&&!anyLowBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (Low-N remains immune): {(!anyLowBreak?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: LRI_03 Reference-source comparison ───");
    }

    // ═══════════════════════════════════════════════
    // LRI_03 — Reference-source comparison
    // ═══════════════════════════════════════════════
    [Fact]public void LRI_03_ReferenceSourceComparison(){
        _o.WriteLine("═══ LRI_03: Reference-source comparison ═══");

        int[] lowN={63,64};
        int[] refSources={65,70,72};
        var results=new ConcurrentBag<LriDiag>();

        Parallel.ForEach(lowN,n=>{
            var lo=Lo(n);var hiLocal=Hi(n);
            foreach(var src in refSources){
                var rp=GetRef(src);
                for(int s=0;s<199;s++){
                    if(IsHi(n,s))continue;
                    var sb=SelectAndClassify(n,s,hiLocal);
                    if(sb==null)continue;

                    var d1=RunTransferredAlign(n,s,hiLocal,lo,rp,0.75,$"R{src}-75%");
                    if(d1!=null){var d=d1.Value;d.sourceN=src;results.Add(d);}
                }
            }
        });

        // Compute averaged reference (R4)
        var r65=GetRef(65);var r70=GetRef(70);var r72=GetRef(72);
        var r4=new RefProfile{
            sourceN=0,label="R4-avg",
            refDMean=(r65.refDMean+r70.refDMean+r72.refDMean)/3,
            refKMean=(r65.refKMean+r70.refKMean+r72.refKMean)/3,
            refKSMean=(r65.refKSMean+r70.refKSMean+r72.refKSMean)/3,
            vecD=(r65.vecD+r70.vecD+r72.vecD)/3,
            vecK=(r65.vecK+r70.vecK+r72.vecK)/3,
            hiDMean=(r65.hiDMean+r70.hiDMean+r72.hiDMean)/3,
            hiKMean=(r65.hiKMean+r70.hiKMean+r72.hiKMean)/3,
            loDMean=(r65.loDMean+r70.loDMean+r72.loDMean)/3,
            loKMean=(r65.loKMean+r70.loKMean+r72.loKMean)/3
        };

        // Test R4 on N=64
        Parallel.ForEach(new[]{64},n=>{
            var lo=Lo(n);var hiLocal=Hi(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hiLocal);
                if(sb==null)continue;
                var d4=RunTransferredAlign(n,s,hiLocal,lo,r4,0.75,"R4-avg-75%");
                if(d4!=null){var d=d4.Value;d.sourceN=0;results.Add(d);}
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nReference comparison: {all.Length} results");

        // Reference profiles
        _o.WriteLine($"\n─── Reference profiles ───");
        _o.WriteLine(string.Format("{0,6} {1,8} {2,8} {3,8} {4,8} {5,8}",
            "Ref","refD","refK","vecD","vecK","hiD"));
        _o.WriteLine($"{"R65",6} {r65.refDMean,8:F4} {r65.refKMean,8:F4} {r65.vecD,8:F4} {r65.vecK,8:F4} {r65.hiDMean,8:F4}");
        _o.WriteLine($"{"R70",6} {r70.refDMean,8:F4} {r70.refKMean,8:F4} {r70.vecD,8:F4} {r70.vecK,8:F4} {r70.hiDMean,8:F4}");
        _o.WriteLine($"{"R72",6} {r72.refDMean,8:F4} {r72.refKMean,8:F4} {r72.vecD,8:F4} {r72.vecK,8:F4} {r72.hiDMean,8:F4}");
        _o.WriteLine($"{"R4-avg",6} {r4.refDMean,8:F4} {r4.refKMean,8:F4} {r4.vecD,8:F4} {r4.vecK,8:F4} {r4.hiDMean,8:F4}");

        // Per-source results
        _o.WriteLine($"\n─── Per-source induction ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,6} {4,8} {5,8}",
            "N","Ref","Imm%","Str%","distRef","projRef"));
        foreach(var n in lowN){
            foreach(var src in new[]{"R65","R70","R72","R4-avg"}){
                var sub=all.Where(r=>r.n==n&&r.refLabel==src).ToArray();
                if(sub.Length==0)continue;
                _o.WriteLine($"{n,4} {src,6} {sub.Count(r=>r.immInduced)*100.0/sub.Length,5:F0}% {sub.Count(r=>r.strictPersist)*100.0/sub.Length,5:F0}% {sub.Average(r=>r.distToRef),8:F4} {sub.Average(r=>r.projRef),8:F4}");
            }
        }

        // Gate D
        var r65Result=all.Where(r=>r.n<=64&&r.refLabel=="R65").Any(r=>r.strictPersist);
        var r70Result=all.Where(r=>r.n<=64&&r.refLabel=="R70").Any(r=>r.strictPersist);
        var r72Result=all.Where(r=>r.n<=64&&r.refLabel=="R72").Any(r=>r.strictPersist);
        bool srcMatters=(r65Result||r70Result||r72Result)&&!(r65Result&&r70Result&&r72Result);
        _o.WriteLine($"\nGate D (Reference source matters): {(srcMatters?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: LRI_04 Failure-mode ───");
    }

    // ═══════════════════════════════════════════════
    // LRI_04 — Failure-mode classification under transferred references
    // ═══════════════════════════════════════════════
    [Fact]public void LRI_04_FailureModeClassification(){
        _o.WriteLine("═══ LRI_04: Failure-mode under transferred references ═══");

        int[] lowN={50,55,60,63,64};
        int[] refSources={65,70,72};
        var results=new ConcurrentBag<LriDiag>();

        Parallel.ForEach(lowN,n=>{
            var lo=Lo(n);var hiLocal=Hi(n);
            foreach(var src in refSources){
                var rp=GetRef(src);
                for(int s=0;s<99;s++){
                    if(IsHi(n,s))continue;
                    var sb=SelectAndClassify(n,s,hiLocal);
                    if(sb==null)continue;

                    // I1-100%: strongest transfer
                    var d=RunTransferredAlign(n,s,hiLocal,lo,rp,1.0,"I1-100%");
                    if(d!=null){var dr=d.Value;dr.sourceN=src;results.Add(dr);}
                }
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nFailure analysis: {all.Length} transferred interventions");

        // Classify failures
        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,6} {5,6} {6,6} {7,6} {8,6}",
            "N","Total","OffVec","InsDisp","RefFar","KCol","dReb","Inv","Unres"));
        foreach(var n in lowN){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            int offVec=sub.Count(r=>r.result=="still-off-vector");
            int insDisp=sub.Count(r=>r.result=="insufficient-disp");
            int refFar=sub.Count(r=>r.result=="ref-too-far");
            int kCol=sub.Count(r=>r.result=="K-collapse");
            int dReb=sub.Count(r=>r.result=="d-rebound");
            int inv=sub.Count(r=>r.invalid);
            int unres=sub.Length-offVec-insDisp-refFar-kCol-dReb-inv;
            _o.WriteLine($"{n,4} {sub.Length,6} {offVec,6} {insDisp,6} {refFar,6} {kCol,6} {dReb,6} {inv,6} {unres,6}");
        }

        // Average metrics for failed vs any success
        var failed=all.Where(r=>!r.strictPersist).ToArray();
        var succeeded=all.Where(r=>r.strictPersist).ToArray();
        _o.WriteLine($"\nFailed: {failed.Length}, Succeeded: {succeeded.Length}");
        if(failed.Length>0){
            _o.WriteLine($"Failed avg distToRef: {failed.Average(r=>r.distToRef):F4} projRef: {failed.Average(r=>r.projRef):F4} dPre: {failed.Average(r=>r.dPre):F4}");
        }
        if(succeeded.Length>0){
            _o.WriteLine($"Success avg distToRef: {succeeded.Average(r=>r.distToRef):F4} projRef: {succeeded.Average(r=>r.projRef):F4} dPre: {succeeded.Average(r=>r.dPre):F4}");
        }

        _o.WriteLine($"\n─── Next: LRI_05 Boundary interpretation ───");
    }

    // ═══════════════════════════════════════════════
    // LRI_05 — Boundary interpretation
    // ═══════════════════════════════════════════════
    [Fact]public void LRI_05_BoundaryInterpretation(){
        _o.WriteLine("═══ LRI_05: Boundary interpretation ═══");

        // Collect comprehensive data
        int[] lowN={50,55,60,63,64};
        int[] refSources={65,70,72};
        var results=new ConcurrentBag<LriDiag>();

        Parallel.ForEach(lowN,n=>{
            var lo=Lo(n);var hiLocal=Hi(n);
            foreach(var src in refSources){
                var rp=GetRef(src);
                for(int s=0;s<99;s++){
                    if(IsHi(n,s))continue;
                    var sb=SelectAndClassify(n,s,hiLocal);
                    if(sb==null)continue;

                    var d0=RunLocalBaseline(n,s,hiLocal,lo,"I0-local");
                    if(d0!=null)results.Add(d0.Value);

                    var d1=RunTransferredAlign(n,s,hiLocal,lo,rp,0.75,"I1-75%");
                    if(d1!=null)results.Add(d1.Value);

                    var d3=RunTransferredC3(n,s,hiLocal,lo,rp,"I3-xC3");
                    if(d3!=null)results.Add(d3.Value);

                    var d4=RunTransferredKPreserve(n,s,hiLocal,lo,rp,0.75,"I4-t+K");
                    if(d4!=null)results.Add(d4.Value);
                }
            }
        });

        var all=results.ToArray();
        bool anyTransferredBreak=all.Where(r=>r.n<=64&&r.intervention!="I0-local").Any(r=>r.strictPersist);
        bool n64TransferredBreak=all.Where(r=>r.n==64&&r.intervention!="I0-local").Any(r=>r.strictPersist);

        // ─── Full metric panel at N=64 ───
        _o.WriteLine($"\n─── N=64 detail: local vs transferred ───");
        var n64Local=all.Where(r=>r.n==64&&r.intervention=="I0-local").ToArray();
        var n64Trans=all.Where(r=>r.n==64&&r.intervention!="I0-local").ToArray();
        _o.WriteLine(string.Format("{0,-12} {1,10} {2,10} {3,10}",
            "Metric","Local","Transferred","Ratio"));
        string[] metrics={"dPre","dT1","dT2","rebMag","kT1","kCollapse","omT1","omT2"};
        foreach(var m in metrics){
            double lv=n64Local.Length>0?GetMetricVal(n64Local,m):0;
            double tv=n64Trans.Length>0?GetMetricVal(n64Trans,m):0;
            _o.WriteLine($"{m,-12} {lv,10:F4} {tv,10:F4} {tv/Math.Max(1e-9,lv),10:F3}x");
        }

        // ─── Reference source profiles ───
        _o.WriteLine($"\n─── Reference profiles ───");
        foreach(var src in refSources){
            var rp=GetRef(src);
            _o.WriteLine($"R{src}: refD={rp.refDMean:F4} refK={rp.refKMean:F4} hiD={rp.hiDMean:F4} loD={rp.loDMean:F4}");
        }

        // ─── Low-N d-space vs reference ───
        _o.WriteLine($"\n─── Low-N candidate d_mean vs references ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,10} {3,10} {4,10}",
            "N","localD","R65-refD","R70-refD","R72-refD"));
        var r65d=GetRef(65).refDMean;var r70d=GetRef(70).refDMean;var r72d=GetRef(72).refDMean;
        foreach(var n in lowN){
            var sub=all.Where(r=>r.n==n&&r.intervention=="I0-local").ToArray();
            if(sub.Length==0)continue;
            double localD=sub.Average(r=>r.dPre);
            _o.WriteLine($"{n,4} {localD,10:F4} {r65d,10:F4} {r70d,10:F4} {r72d,10:F4}");
        }

        // ─── Boundary classification ───
        _o.WriteLine($"\n─── Boundary interpretation ───");
        if(anyTransferredBreak){
            _o.WriteLine($"INTERPRETATION: Transferred reference opens low-N → lower boundary was partly caused by absent local High reference.");
        }else if(n64TransferredBreak){
            _o.WriteLine($"INTERPRETATION: Only N=64 opens under transferred reference → N=64 is near-boundary, N=50-63 are truly inaccessible.");
        }else{
            _o.WriteLine($"INTERPRETATION: Low-N inaccessibility is true non-inducibility under current and transferred-reference operator classes.");
        }

        // Gates
        _o.WriteLine($"\n─── Gates ───");
        _o.WriteLine($"Gate A (Transferred opens low-N): {(anyTransferredBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (N=64 opens only): {(n64TransferredBreak&&!anyTransferredBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (Low-N remains immune): {(!anyTransferredBreak?"REACHED":"NOT REACHED")}");
        bool anyInvalid=all.Where(r=>r.n<=64&&r.intervention!="I0-local").Any(r=>r.invalid);
        _o.WriteLine($"Gate F (Invalid/unsafe): {(anyInvalid?"REACHED":"NOT REACHED")}");

        string finalBoundary=anyTransferredBreak?"Boundary was partly a local-reference artifact.":
            n64TransferredBreak?"N=64 near-boundary; N<64 truly inaccessible.":
            "Low-N inaccessibility is true non-inducibility — NOT a missing reference artifact.";
        _o.WriteLine($"\nFinal boundary model: {finalBoundary}");

        _o.WriteLine($"\n─── Audit complete ───");
    }

    // ═══════════════════════════════════════════════
    // Intervention implementations
    // ═══════════════════════════════════════════════

    LriDiag? RunLocalBaseline(int n,int s,P3 hi,P3 lo,string label){
        var diag=new LriDiag{n=n,s=s,intervention=label,refLabel="local",cohort=s/100};

        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        // Frozen M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);diag.dT1=Dm(dT1,n);diag.kT1=Km(Cupd(dT1,n),n);diag.omT1=Of(hT1,n).Average();
        diag.dPre=diag.dT1;diag.kPre=diag.kT1;diag.omPre=diag.omT1;

        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);diag.dT2=Dm(dT2,n);diag.kT2=Km(Cupd(dT2,n),n);diag.omT2=Of(hT2,n).Average();
        diag.rebMag=diag.dT2-diag.dT1;

        diag.a0=diag.omT2>THR;
        diag.dPost=diag.dT2;diag.kPost=diag.kT2;diag.omPost=diag.omT2;

        // Local C3
        if(diag.rebMag<RebT&&!diag.a0){
            var dmat=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat,n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            if(!double.IsNaN(hi.dm)){
                double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat[i,j]*=f3;
                if(ValidD(dmat,n)){
                    var hc3=Sim(Cupd(dmat,n),n,S,s+300);
                    var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
                    var hc3cc=Sim(Kc3c,n,S,s+400);
                    double omC3=Of(hc3cc,n).Average();
                    diag.dPost=Dm(dmat,n);diag.kPost=Km(Cupd(dmat,n),n);diag.omPost=omC3;
                    diag.immInduced=omC3>THR;
                    var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
                    diag.strictPersist=diag.immInduced&&Of(hCont,n).Average()>THR;
                }
            }
        }else{
            diag.immInduced=diag.a0;diag.strictPersist=diag.a0;
        }

        // Distances
        if(!double.IsNaN(hi.dm)){
            diag.distToRef=Math.Sqrt((diag.dPost-hi.dm)*(diag.dPost-hi.dm)+(diag.kPost-hi.km)*(diag.kPost-hi.km));
            diag.distToLo=Math.Sqrt((diag.dPost-lo.dm)*(diag.dPost-lo.dm)+(diag.kPost-lo.km)*(diag.kPost-lo.km));
            diag.basinSuccess=diag.strictPersist&&diag.distToRef<diag.distToLo&&!diag.invalid;
        }

        diag.result=diag.basinSuccess?"basin-success":diag.strictPersist?"induced":"failed";
        return diag;
    }

    LriDiag? RunTransferredAlign(int n,int s,P3 hiLocal,P3 lo,RefProfile rp,double strength,string label){
        var diag=new LriDiag{n=n,s=s,intervention=label,refLabel=rp.label,cohort=s/100,sourceN=rp.sourceN};
        var sb=SelectAndClassify(n,s,hiLocal);
        if(sb==null)return null;
        double d0=sb.Value.d0;

        // Relative position in reduced (d,K) space
        double dCur=d0,savedKm=sb.Value.km0;
        diag.dPre=dCur;diag.kPre=savedKm;

        // Target: move strength% from candidate position toward refD in reduced space
        double targetD=dCur+(rp.refDMean-dCur)*strength;
        // Clamp to avoid extreme shifts
        targetD=Math.Clamp(targetD,dCur*0.25,dCur*4.0);

        // Run M3++ prep
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);

        // Scale d-matrix to push toward target d_mean
        double frac=Math.Clamp((targetD+1e-9)/(curDm+1e-9),0.25,4.0);
        var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;

        if(!ValidD(dM,n)){diag.invalid=true;diag.result="invalid-d";return diag;}
        var Kt=Cupd(dM,n);
        var ht=Sim(Kt,n,S,s+4);Kt=Cupd(DL(Nm(RP(ht,n),n),n),n);
        var hT1=Sim(Kt,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);diag.dT1=Dm(dT1,n);diag.kT1=Km(Cupd(dT1,n),n);diag.omT1=Of(hT1,n).Average();

        // T2 continuation
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);diag.dT2=Dm(dT2,n);diag.kT2=Km(Cupd(dT2,n),n);diag.omT2=Of(hT2,n).Average();
        diag.rebMag=diag.dT2-diag.dT1;
        diag.deltaD=diag.dT1-dCur;diag.deltaK=diag.kT1-savedKm;
        diag.distToRef=Math.Sqrt((diag.dT2-rp.refDMean)*(diag.dT2-rp.refDMean)+(diag.kT2-rp.refKMean)*(diag.kT2-rp.refKMean));
        diag.distToLo=Math.Sqrt((diag.dT2-lo.dm)*(diag.dT2-lo.dm)+(diag.kT2-lo.km)*(diag.kT2-lo.km));
        diag.projRef=((diag.dT2-lo.dm)*rp.vecD+(diag.kT2-lo.km)*rp.vecK)/Math.Max(1e-9,Math.Sqrt(rp.vecD*rp.vecD+rp.vecK*rp.vecK));
        double d2Ref=(diag.dT2-lo.dm)*(diag.dT2-lo.dm)+(diag.kT2-lo.km)*(diag.kT2-lo.km);
        double pRef=diag.projRef*diag.projRef;
        diag.orthRef=Math.Sqrt(Math.Max(0,d2Ref-pRef));

        diag.a0=diag.omT2>THR;
        diag.immInduced=diag.a0;

        // Persistence
        var hCont=Sim(Cupd(DL(Nm(RP(hT2,n),n),n),n),n,S,s+300);
        double omCont=Of(hCont,n).Average();
        diag.strictPersist=diag.immInduced&&omCont>THR;
        diag.basinSuccess=diag.strictPersist&&diag.distToRef<diag.distToLo&&!diag.invalid;

        diag.result=diag.basinSuccess?"basin-success":diag.strictPersist?"induced":
            Math.Abs(diag.distToRef)>0.5?"ref-too-far":
            diag.deltaD<0.01?"insufficient-disp":"still-off-vector";
        return diag;
    }

    LriDiag? RunTransferredC3(int n,int s,P3 hiLocal,P3 lo,RefProfile rp,string label){
        var diag=RunLocalBaseline(n,s,hiLocal,lo,"I0-local");
        if(diag==null)return null;
        var d=diag.Value;d.intervention=label;d.refLabel=rp.label;d.sourceN=rp.sourceN;

        if(d.strictPersist||d.rebMag>=RebT){d.result="no-C3-needed";return d;}

        // Re-run prep for fresh T1 state
        var sb=SelectAndClassify(n,s,hiLocal);
        if(sb==null)return d;
        double d0=sb.Value.d0;
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var km=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(km,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);

        // C3 using transferred reference direction
        var dmat=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat,n);
        double nudged=dmPre+(rp.refDMean-dmPre)*0.2;
        double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.25,4.0);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat[i,j]*=f3;

        if(!ValidD(dmat,n)){d.invalid=true;d.result="invalid-C3";return d;}
        var hc3=Sim(Cupd(dmat,n),n,S,s+300);
        var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
        var hc3cc=Sim(Kc3c,n,S,s+400);
        double omC3=Of(hc3cc,n).Average();
        d.dPost=Dm(dmat,n);d.kPost=Km(Cupd(dmat,n),n);d.omPost=omC3;
        d.immInduced=omC3>THR;
        d.deltaD=d.dPost-dmPre;d.deltaK=d.kPost-Km(Cupd(dmat,n),n);
        d.distToRef=Math.Sqrt((d.dPost-rp.refDMean)*(d.dPost-rp.refDMean)+(d.kPost-rp.refKMean)*(d.kPost-rp.refKMean));

        var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
        d.strictPersist=d.immInduced&&Of(hCont,n).Average()>THR;
        d.basinSuccess=d.strictPersist&&d.distToRef<d.distToLo&&!d.invalid;
        d.result=d.basinSuccess?"basin-success":d.strictPersist?"induced":"still-off-vector";
        return d;
    }

    LriDiag? RunTransferredKPreserve(int n,int s,P3 hiLocal,P3 lo,RefProfile rp,double strength,string label){
        var diag=RunTransferredAlign(n,s,hiLocal,lo,rp,strength,label);
        if(diag==null)return null;
        var d=diag.Value;
        d.intervention=label;
        d.refLabel=rp.label;d.sourceN=rp.sourceN;

        if(d.strictPersist||d.invalid){return d;}

        // Re-run transferred align prep and preserve K
        var sb=SelectAndClassify(n,s,hiLocal);
        if(sb==null)return d;
        double d0=sb.Value.d0;
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
        double targetD=curDm+(rp.refDMean-curDm)*strength;
        targetD=Math.Clamp(targetD,curDm*0.25,curDm*4.0);
        double frac=Math.Clamp((targetD+1e-9)/(curDm+1e-9),0.25,4.0);
        var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        if(!ValidD(dM,n)){d.invalid=true;d.result="invalid-KPres";return d;}

        var Kpres=Cupd(dM,n);
        var hPres=Sim(Kpres,n,S,s+4);Kpres=Cupd(DL(Nm(RP(hPres,n),n),n),n);
        var hT1=Sim(Kpres,n,S,s+100);

        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var Kt2=Cupd(DL(Nm(RP(hT2,n),n),n),n);
        // Blend 50% preserved K with 50% evolved K
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)Kt2[i,j]=(Kt2[i,j]+Kpres[i,j])/2;

        var hBlend=Sim(Kt2,n,S,s+300);
        var Kblend=Cupd(DL(Nm(RP(hBlend,n),n),n),n);
        var hCont=Sim(Kblend,n,S,s+400);
        double omCont=Of(hCont,n).Average();
        d.dPost=Dm(DL(Nm(RP(hBlend,n),n),n),n);d.kPost=Km(Kblend,n);d.omPost=omCont;
        d.immInduced=omCont>THR;
        d.strictPersist=d.immInduced;
        d.basinSuccess=d.strictPersist&&d.distToRef<d.distToLo&&!d.invalid;
        d.result=d.basinSuccess?"basin-success":d.strictPersist?"induced":"K-preserve-failed";
        return d;
    }

    // ═══════════════════════════════════════════════
    // Reference profile builder
    // ═══════════════════════════════════════════════
    RefProfile BuildRefProfile(int sourceN){
        var hi=Hi(sourceN);var lo=Lo(sourceN);
        double sumD=0,sumK=0,sumKS=0;int cnt=0;

        // Sample Hi-branch seeds from source N
        for(int s=0;s<200&&cnt<10;s++){
            if(!IsHi(sourceN,s))continue;
            // Get stable d,K metrics
            var K=KS(sourceN,s);
            for(int e=0;e<5;e++){var h=Sim(K,sourceN,S,s+e);var d=DL(Nm(RP(h,sourceN),sourceN),sourceN);K=Cupd(d,sourceN);}
            var hFinal=Sim(K,sourceN,S,s+5);
            double om=Of(hFinal,sourceN).Average();
            if(om<=THR)continue;
            var dFinal=DL(Nm(RP(hFinal,sourceN),sourceN),sourceN);
            sumD+=Dm(dFinal,sourceN);sumK+=Km(Cupd(dFinal,sourceN),sourceN);
            sumKS+=Ks(Cupd(dFinal,sourceN),sourceN);
            cnt++;
        }

        // Fallback: use Hi centroid if available
        if(cnt==0&&!double.IsNaN(hi.dm)){
            sumD=hi.dm;sumK=hi.km;sumKS=hi.ks;cnt=1;
        }
        // Fallback: use Lo+offset
        if(cnt==0){
            sumD=lo.dm+0.1;sumK=lo.km+0.02;sumKS=lo.ks+0.01;cnt=1;
        }

        double refD=sumD/cnt,refK=sumK/cnt,refKS=sumKS/cnt;
        double vecD=hi.dm-lo.dm,vecK=hi.km-lo.km;
        if(double.IsNaN(vecD)||double.IsNaN(vecK)){vecD=refD-lo.dm;vecK=refK-lo.km;}

        return new RefProfile{
            sourceN=sourceN,label=$"R{sourceN}",
            refDMean=refD,refKMean=refK,refKSMean=refKS,
            vecD=vecD,vecK=vecK,
            hiDMean=hi.dm,hiKMean=hi.km,loDMean=lo.dm,loKMean=lo.km
        };
    }

    // ═══════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════

    static double GetMetricVal(LriDiag[] profiles,string metric){
        return metric switch{
            "dPre"=>profiles.Average(p=>p.dPre),"dT1"=>profiles.Average(p=>p.dT1),
            "dT2"=>profiles.Average(p=>p.dT2),"rebMag"=>profiles.Average(p=>p.rebMag),
            "kT1"=>profiles.Average(p=>p.kT1),"omT1"=>profiles.Average(p=>p.omT1),
            "omT2"=>profiles.Average(p=>p.omT2),
            "kCollapse"=>profiles.Sum(p=>p.kT2-p.kT1)/profiles.Length,
            _=>0
        };
    }

    static bool ValidD(double[,]d,int n){
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)
            if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return false;
        return true;
    }

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

    // ─── Frozen M3++ helpers ───
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
}
