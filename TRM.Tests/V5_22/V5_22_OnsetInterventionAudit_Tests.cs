using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_22;

[Trait("Category","V5_22"),Trait("Category","V5_22_IOI"),Trait("Category","LongRunning")]
public class V5_22_OnsetInterventionAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct IoiDiag{
        public int n,s,cohort;public string intervention,result,failureMode;
        public double dT1,kT1,omT1,dT2,kT2,omT2,rebMag;
        public double alignPre,alignPost,deltaAlign,c3OmegaShift,c3Effectiveness;
        public double dPre,dPost,kPre,kPost,omPre,omPost;
        public double distHi,distLo,projHiVec,offVecAngle;
        public double maxC3OmgShift; // max achieved across interventions
        public bool a0,c3,immInduced,strictPersist,basinSuccess,invalid;
    }

    public V5_22_OnsetInterventionAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    // ═══════════════════════════════════════════════
    // IOI_01 — Baseline reproduction
    // ═══════════════════════════════════════════════
    [Fact]public void IOI_01_BaselineReproduction(){
        _o.WriteLine("═══ IOI_01: Baseline — N=64 failure, N=65 onset ═══");

        int[] Ns={63,64,65,66};
        var results=new ConcurrentBag<IoiDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var d=RunBaseline(n,s,hi,lo);
                if(d==null)continue;
                results.Add(d.Value);
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nBaseline: {all.Length} seeds");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,6} {4,8} {5,8} {6,8} {7,8}",
            "N","n","A0%","Resc%","c3OmgS","dT1","alignPre","distHi"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            int a0=sub.Count(r=>r.a0),resc=sub.Count(r=>r.strictPersist&&!r.a0);
            _o.WriteLine($"{n,4} {sub.Length,6} {a0*100.0/sub.Length,5:F0}% {resc*100.0/sub.Length,5:F0}% {sub.Average(r=>r.c3OmegaShift),8:F4} {sub.Average(r=>r.dT1),8:F4} {sub.Average(r=>r.alignPre),8:F4} {sub.Average(r=>r.distHi),8:F4}");
        }

        // Verify N=64 rescue-immune
        var n64=all.Where(r=>r.n==64).ToArray();
        bool n64Immune=n64.Length>0&&n64.All(r=>!r.strictPersist);
        _o.WriteLine($"\nN=64 immune: {(n64Immune?"CONFIRMED":"FAILED")}");
        _o.WriteLine($"N=64 max c3OmegaShift: {n64.Max(r=>r.c3OmegaShift):F4}");
        _o.WriteLine($"N=64 max dT1: {n64.Max(r=>r.dT1):F4}");

        if(!n64Immune){_o.WriteLine("STOP — baseline failed.");return;}
        _o.WriteLine($"\n─── Next: IOI_02 Component limits ───");
    }

    // ═══════════════════════════════════════════════
    // IOI_02 — Component limitation audit
    // ═══════════════════════════════════════════════
    [Fact]public void IOI_02_ComponentLimitationAudit(){
        _o.WriteLine("═══ IOI_02: Component limitation audit at N=64 ═══");
        _o.WriteLine("Testing whether boosting individual RR components enables C3.");

        int[] Ns={64,65}; // N=64 primary, N=65 reference
        var results=new ConcurrentBag<IoiDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;

                // I0: baseline
                var d0=RunBaseline(n,s,hi,lo);
                if(d0!=null)results.Add(d0.Value);

                // I1: dT1 amplification (1.5x, 2.0x)
                var d1a=RunDT1Amp(n,s,hi,lo,1.5,"I1-dT1x1.5");
                if(d1a!=null)results.Add(d1a.Value);
                var d1b=RunDT1Amp(n,s,hi,lo,2.0,"I1-dT1x2.0");
                if(d1b!=null)results.Add(d1b.Value);

                // I2: anti-alignment push (toward more negative alignPre)
                var d2a=RunAntiAlign(n,s,hi,lo,0.5,"I2-antiAl50%");
                if(d2a!=null)results.Add(d2a.Value);
                var d2b=RunAntiAlign(n,s,hi,lo,1.0,"I2-antiAl100%");
                if(d2b!=null)results.Add(d2b.Value);

                // I3: deltaAlign boost (C3 ×1.5, ×2.0)
                var d3a=RunDeltaAlignBoost(n,s,hi,lo,1.5,"I3-dAlx1.5");
                if(d3a!=null)results.Add(d3a.Value);
                var d3b=RunDeltaAlignBoost(n,s,hi,lo,2.0,"I3-dAlx2.0");
                if(d3b!=null)results.Add(d3b.Value);

                // I4: K-response support
                var d4=RunKSupport(n,s,hi,lo,"I4-Ksupport");
                if(d4!=null)results.Add(d4.Value);

                // I5: Omega gain audit
                var d5=RunOmegaGainAudit(n,s,hi,lo);
                if(d5!=null)results.Add(d5.Value);
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nComponent audit: {all.Length} results");

        // N=64 component table
        _o.WriteLine($"\n─── N=64: component-by-component ───");
        _o.WriteLine(string.Format("{0,-14} {1,6} {2,6} {3,6} {4,8} {5,8} {6,8} {7,6}",
            "Intervention","n","Imm%","Str%","c3OmgS","dT1","alignPre","Inv%"));
        var n64Results=all.Where(r=>r.n==64).ToArray();
        var interventions=n64Results.Select(r=>r.intervention).Distinct().ToArray();
        foreach(var iv in interventions){
            var sub=n64Results.Where(r=>r.intervention==iv).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{iv,-14} {sub.Length,6} {sub.Count(r=>r.immInduced)*100.0/sub.Length,5:F0}% {sub.Count(r=>r.strictPersist)*100.0/sub.Length,5:F0}% {sub.Average(r=>r.c3OmegaShift),8:F4} {sub.Average(r=>r.dT1),8:F4} {sub.Average(r=>r.alignPre),8:F4} {sub.Count(r=>r.invalid)*100.0/sub.Length,5:F0}%");
        }

        // N=65 reference
        _o.WriteLine($"\n─── N=65 reference ───");
        var n65Results=all.Where(r=>r.n==65).ToArray();
        var n65Base=n65Results.Where(r=>r.intervention=="I0-baseline").ToArray();
        if(n65Base.Length>0)
            _o.WriteLine($"Baseline: c3OmgS={n65Base.Average(r=>r.c3OmegaShift):F4} dT1={n65Base.Average(r=>r.dT1):F4} alignPre={n65Base.Average(r=>r.alignPre):F4} resc%={n65Base.Count(r=>r.strictPersist)*100.0/n65Base.Length:F0}%");

        // Max c3OmegaShift per intervention at N=64
        _o.WriteLine($"\n─── N=64 max c3OmegaShift per intervention ───");
        foreach(var iv in interventions){
            var sub=n64Results.Where(r=>r.intervention==iv).ToArray();
            if(sub.Length==0)continue;
            double max=sub.Max(r=>r.c3OmegaShift);
            _o.WriteLine($"{iv,-14} max c3OmgS={max:F4}");
        }

        // Gates
        bool anyDT1=n64Results.Where(r=>r.intervention.StartsWith("I1")).Any(r=>r.strictPersist);
        bool anyAlign=n64Results.Where(r=>r.intervention.StartsWith("I2")).Any(r=>r.strictPersist);
        bool anyDelta=n64Results.Where(r=>r.intervention.StartsWith("I3")).Any(r=>r.strictPersist);
        bool anyK=n64Results.Where(r=>r.intervention=="I4-Ksupport").Any(r=>r.strictPersist);

        _o.WriteLine($"\nGate C (dT1 limits): {(anyDT1?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (Alignment limits): {(anyAlign?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (K/Omega gain limits): {(!anyDT1&&!anyAlign&&!anyDelta?"assessing":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: IOI_03 Full package ───");
    }

    // ═══════════════════════════════════════════════
    // IOI_03 — Full resonant reversal package
    // ═══════════════════════════════════════════════
    [Fact]public void IOI_03_FullPackageAudit(){
        _o.WriteLine("═══ IOI_03: Full resonant reversal package at N=64 ═══");
        _o.WriteLine("Upper-bound test: combine dT1 amp + anti-align + deltaAlign + K support.");

        int n=64;
        var hi=Hi(n);var lo=Lo(n);
        var results=new ConcurrentBag<IoiDiag>();

        Parallel.For(0,399,s=>{
            if(IsHi(n,s))return;
            var sb=SelectAndClassify(n,s,hi);
            if(sb==null)return;

            // I0 baseline
            var d0=RunBaseline(n,s,hi,lo);
            if(d0!=null)results.Add(d0.Value);

            // I6: Full package
            var d6=RunFullPackage(n,s,hi,lo);
            if(d6!=null)results.Add(d6.Value);

            // C2: over-amplification control
            var c2=RunOverAmpControl(n,s,hi,lo);
            if(c2!=null)results.Add(c2.Value);
        });

        var all=results.ToArray();
        var baseLine=all.Where(r=>r.intervention=="I0-baseline").ToArray();
        var fullPkg=all.Where(r=>r.intervention=="I6-fullPKG").ToArray();
        var overAmp=all.Where(r=>r.intervention=="C2-overAmp").ToArray();

        _o.WriteLine($"\nN=64: baseline={baseLine.Length}, fullPkg={fullPkg.Length}, overAmp={overAmp.Length}");

        _o.WriteLine($"\n─── Full package results ───");
        _o.WriteLine(string.Format("{0,-14} {1,6} {2,6} {3,8} {4,8} {5,8} {6,6} {7,6}",
            "Intervention","n","Inv%","c3OmgS","dT1","alignPre","Imm%","Str%"));
        foreach(var grp in new[]{baseLine,fullPkg,overAmp}){
            if(grp.Length==0)continue;
            _o.WriteLine($"{grp[0].intervention,-14} {grp.Length,6} {grp.Count(r=>r.invalid)*100.0/grp.Length,5:F0}% {grp.Average(r=>r.c3OmegaShift),8:F4} {grp.Average(r=>r.dT1),8:F4} {grp.Average(r=>r.alignPre),8:F4} {grp.Count(r=>r.immInduced)*100.0/grp.Length,5:F0}% {grp.Count(r=>r.strictPersist)*100.0/grp.Length,5:F0}%");
        }

        // Best N=64 seeds under full package
        if(fullPkg.Length>0){
            _o.WriteLine($"\n─── Top 5 full-package seeds by c3OmegaShift ───");
            _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8}",
                "seed","c3OmgS","dT1","alignPre","deltaAl"));
            foreach(var p in fullPkg.OrderByDescending(x=>x.c3OmegaShift).Take(5))
                _o.WriteLine($"{p.s,4} {p.c3OmegaShift,8:F4} {p.dT1,8:F4} {p.alignPre,8:F4} {p.deltaAlign,8:F4}");
        }

        // Comparison to N=65 natural RR minimum
        double rrMin=0.765; // from IOA
        bool anyAboveRR=fullPkg.Any(r=>r.c3OmegaShift>rrMin);
        double maxFull=fullPkg.Length>0?fullPkg.Max(r=>r.c3OmegaShift):0;
        _o.WriteLine($"\nN=64 max c3OmegaShift (full pkg): {maxFull:F4} vs RR min: {rrMin:F4}");
        _o.WriteLine($"Any above RR threshold: {(anyAboveRR?"YES":"NO")}");

        _o.WriteLine($"\nGate A (N=64 breaks): {(fullPkg.Any(r=>r.strictPersist)?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Full package required): {(anyAboveRR&&!fullPkg.Any(r=>r.strictPersist)?"assessing":"NOT REACHED")}");
        _o.WriteLine($"Gate G (Below C3 threshold): {(!anyAboveRR?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: IOI_04 N=64 vs N=65 comparison ───");
    }

    // ═══════════════════════════════════════════════
    // IOI_04 — N=64 vs N=65 comparison
    // ═══════════════════════════════════════════════
    [Fact]public void IOI_04_N64vsN65Comparison(){
        _o.WriteLine("═══ IOI_04: N=64 vs N=65: how close can interventions get? ═══");

        int[] Ns={64,65};
        var results=new ConcurrentBag<IoiDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<199;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;

                var d0=RunBaseline(n,s,hi,lo);
                if(d0!=null)results.Add(d0.Value);

                var d1=RunDT1Amp(n,s,hi,lo,2.0,"I1-dT1x2.0");
                if(d1!=null)results.Add(d1.Value);

                var d2=RunAntiAlign(n,s,hi,lo,1.0,"I2-antiAl100%");
                if(d2!=null)results.Add(d2.Value);
            }
        });

        var all=results.ToArray();
        var n64Baseline=all.Where(r=>r.n==64&&r.intervention=="I0-baseline").ToArray();
        var n64Best=all.Where(r=>r.n==64&&r.intervention!="I0-baseline").ToArray();
        var n65Baseline=all.Where(r=>r.n==65&&r.intervention=="I0-baseline").ToArray();

        _o.WriteLine($"\n─── Head-to-head: N=64 best vs N=65 natural ───");
        _o.WriteLine(string.Format("{0,-16} {1,10} {2,10} {3,10} {4,10}",
            "Metric","N=64 base","N=64 best","N=65 base","64b→65"));
        string[] metrics={"c3OmegaShift","dT1","alignPre","deltaAlign","omT1","kT1","distHi","rebMag","c3Effectiveness"};
        foreach(var m in metrics){
            double v64b=GetMean(n64Baseline,m),v64best=GetMean(n64Best,m),v65=GetMean(n65Baseline,m);
            double gap=v65/Math.Max(1e-9,Math.Abs(v64best));
            _o.WriteLine($"{m,-16} {v64b,10:F4} {v64best,10:F4} {v65,10:F4} {gap,10:F1}x");
        }

        // Best single N=64 seed vs N=65 rescued profile
        var best64Seed=n64Best.OrderByDescending(r=>r.c3OmegaShift).FirstOrDefault();
        if(best64Seed.c3OmegaShift>0){
            _o.WriteLine($"\n─── Best N=64 seed (s={best64Seed.s}) vs N=65 RR min ───");
            _o.WriteLine($"N=64 best c3OmegaShift: {best64Seed.c3OmegaShift:F4} (RR min: 0.765)");
            _o.WriteLine($"N=64 best dT1: {best64Seed.dT1:F4} (RR min: 0.584)");
            _o.WriteLine($"N=64 best alignPre: {best64Seed.alignPre:F4} (RR range: [-0.56,-0.14])");
            _o.WriteLine($"N=64 best deltaAlign: {best64Seed.deltaAlign:F4} (RR min: 0.036)");
        }

        _o.WriteLine($"\n─── Next: IOI_05 C3 gain threshold ───");
    }

    // ═══════════════════════════════════════════════
    // IOI_05 — C3 gain threshold
    // ═══════════════════════════════════════════════
    [Fact]public void IOI_05_C3GainThreshold(){
        _o.WriteLine("═══ IOI_05: C3 gain threshold — can N=64 ever produce RR-level Omega shift? ═══");

        int[] Ns={64,65,66,70,72};
        var results=new ConcurrentBag<IoiDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<299;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;

                var d0=RunBaseline(n,s,hi,lo);
                if(d0!=null)results.Add(d0.Value);

                if(n==64){
                    // Run all boost interventions for N=64
                    var d1=RunDT1Amp(n,s,hi,lo,2.0,"I1-max");
                    if(d1!=null)results.Add(d1.Value);
                    var d6=RunFullPackage(n,s,hi,lo);
                    if(d6!=null)results.Add(d6.Value);
                }
            }
        });

        var all=results.ToArray();

        // c3OmegaShift distribution
        _o.WriteLine($"\n─── c3OmegaShift distribution by N ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,6}",
            "N","n","Mean","Max","P90","P95",">0.1%"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n&&r.intervention=="I0-baseline").ToArray();
            if(sub.Length==0)continue;
            var vals=sub.Select(r=>r.c3OmegaShift).OrderBy(x=>x).ToArray();
            _o.WriteLine($"{n,4} {sub.Length,6} {vals.Average(),8:F4} {vals.Max(),8:F4} {Percentile(vals,0.9),8:F4} {Percentile(vals,0.95),8:F4} {vals.Count(x=>x>0.1)*100.0/vals.Length,5:F0}%");
        }

        // N=64 boosted distribution
        var n64Boosted=all.Where(r=>r.n==64&&r.intervention!="I0-baseline").ToArray();
        if(n64Boosted.Length>0){
            var bVals=n64Boosted.Select(r=>r.c3OmegaShift).OrderBy(x=>x).ToArray();
            _o.WriteLine($"\nN=64 boosted (all interventions):");
            _o.WriteLine($"  Mean={bVals.Average():F4} Max={bVals.Max():F4} P90={Percentile(bVals,0.9):F4} P95={Percentile(bVals,0.95):F4}");
            _o.WriteLine($"  Seeds with c3OmegaShift>0.1: {bVals.Count(x=>x>0.1)*100.0/bVals.Length:F0}%");
            _o.WriteLine($"  Seeds with c3OmegaShift>0.5: {bVals.Count(x=>x>0.5)*100.0/bVals.Length:F0}%");
            _o.WriteLine($"  Seeds above RR min (0.765): {bVals.Count(x=>x>0.765)}/{bVals.Length}");
        }

        // Omega gain vs dT1 scatter for N=64
        var n64All=all.Where(r=>r.n==64).ToArray();
        _o.WriteLine($"\n─── N=64: c3OmegaShift vs dT1 (best per seed) ───");
        var bestPerSeed=n64All.GroupBy(r=>r.s).Select(g=>g.OrderByDescending(r=>r.c3OmegaShift).First()).OrderBy(r=>r.s).ToArray();
        foreach(var p in bestPerSeed.Where(r=>r.dT1>0.4||r.c3OmegaShift>0.02).Take(15))
            _o.WriteLine($"  s={p.s,4} c3OmgS={p.c3OmegaShift,8:F4} dT1={p.dT1,8:F4} alignPre={p.alignPre,8:F4} int={p.intervention}");

        _o.WriteLine($"\nGate G (Below C3 threshold): {(!bestPerSeed.Any(r=>r.c3OmegaShift>0.765)?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: IOI_06 Failure classification ───");
    }

    // ═══════════════════════════════════════════════
    // IOI_06 — Failure classification
    // ═══════════════════════════════════════════════
    [Fact]public void IOI_06_FailureClassification(){
        _o.WriteLine("═══ IOI_06: N=64 failure classification ═══");

        int n=64;
        var hi=Hi(n);var lo=Lo(n);
        var results=new ConcurrentBag<IoiDiag>();

        Parallel.For(0,299,s=>{
            if(IsHi(n,s))return;
            var sb=SelectAndClassify(n,s,hi);
            if(sb==null)return;

            var d0=RunBaseline(n,s,hi,lo);
            if(d0!=null)results.Add(d0.Value);
            var d1=RunDT1Amp(n,s,hi,lo,2.0,"I1-dT1x2.0");
            if(d1!=null)results.Add(d1.Value);
            var d2=RunAntiAlign(n,s,hi,lo,1.0,"I2-antiAl100%");
            if(d2!=null)results.Add(d2.Value);
            var d6=RunFullPackage(n,s,hi,lo);
            if(d6!=null)results.Add(d6.Value);
            var d5=RunOmegaGainAudit(n,s,hi,lo);
            if(d5!=null)results.Add(d5.Value);
        });

        var all=results.ToArray();

        // Classify each seed by best result
        var bestPerSeed=all.GroupBy(r=>r.s).Select(g=>{
            var best=g.OrderByDescending(r=>r.c3OmegaShift).First();
            return best;
        }).ToArray();

        _o.WriteLine($"\nN=64 seeds: {bestPerSeed.Length}");

        // Failure classification
        int insuffDT1=bestPerSeed.Count(r=>r.c3OmegaShift<0.05&&r.dT1<0.5);
        int insuffAlign=bestPerSeed.Count(r=>r.c3OmegaShift<0.05&&r.alignPre>-0.1);
        int omegaGainFail=bestPerSeed.Count(r=>r.c3OmegaShift<0.1&&r.dT1>0.5&&r.alignPre<-0.1);
        int invalid=bestPerSeed.Count(r=>r.invalid);
        int nearBreak=bestPerSeed.Count(r=>r.c3OmegaShift>0.1);
        int unresolved=bestPerSeed.Length-insuffDT1-insuffAlign-omegaGainFail-invalid-nearBreak;

        _o.WriteLine($"\n─── Failure classification ───");
        _o.WriteLine($"Insufficient dT1: {insuffDT1} ({insuffDT1*100.0/bestPerSeed.Length:F0}%)");
        _o.WriteLine($"Insufficient anti-align: {insuffAlign} ({insuffAlign*100.0/bestPerSeed.Length:F0}%)");
        _o.WriteLine($"Omega gain failure (precond met): {omegaGainFail} ({omegaGainFail*100.0/bestPerSeed.Length:F0}%) ← KEY");
        _o.WriteLine($"Near-break (c3OmgS>0.1): {nearBreak} ({nearBreak*100.0/bestPerSeed.Length:F0}%)");
        _o.WriteLine($"Invalid: {invalid} ({invalid*100.0/bestPerSeed.Length:F0}%)");
        _o.WriteLine($"Unresolved: {unresolved} ({unresolved*100.0/bestPerSeed.Length:F0}%)");

        // Omega gain failure detail
        _o.WriteLine($"\n─── Omega gain failures (preconditions met, c3OmgS<0.1) ───");
        _o.WriteLine($"These seeds have dT1>0.5 AND alignPre<-0.1 but c3OmgS<0.1");
        _o.WriteLine($"Count: {omegaGainFail} — evidence of C3 response-gain block at N=64");

        _o.WriteLine($"\n─── Audit complete ───");
    }

    // ═══════════════════════════════════════════════
    // Intervention implementations
    // ═══════════════════════════════════════════════

    IoiDiag? RunBaseline(int n,int s,P3 hi,P3 lo){
        var d=new IoiDiag{n=n,s=s,intervention="I0-baseline",cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0v=sb.Value.d0;
        d.projHiVec=ComputeProjHiVec(d0v,sb.Value.km0,sb.Value.ks0,hi,lo);
        d.offVecAngle=ComputeOffVecAngle(d0v,sb.Value.km0,sb.Value.ks0,hi,lo);
        d.distHi=ComputeDistHi(d0v,sb.Value.km0,sb.Value.ks0,hi);
        d.distLo=ComputeDistLo(d0v,sb.Value.km0,sb.Value.ks0,lo);

        // M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0v*0.90:d0v*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);d.dT1=Dm(dT1,n);d.kT1=Km(Cupd(dT1,n),n);d.omT1=Of(hT1,n).Average();
        d.alignPre=ComputeAlignPre(d.dT1,d.kT1,Ks(Cupd(dT1,n),n),hi,lo);

        // T2
        var Kt2=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(Kt2,n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);d.dT2=Dm(dT2,n);d.kT2=Km(Cupd(dT2,n),n);d.omT2=Of(hT2,n).Average();
        d.rebMag=d.dT2-d.dT1;d.a0=d.omT2>THR;

        // C3
        if(!double.IsNaN(hi.dm)&&d.rebMag<RebT&&!d.a0){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            if(!ValidD(dmat3,n)){d.invalid=true;return d;}
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);
            d.c3OmegaShift=omC3-d.omT2;d.deltaAlign=ComputeAlignPre(dmPost,kmPost,Ks(Cupd(dmat3,n),n),hi,lo)-d.alignPre;
            d.c3Effectiveness=d.c3OmegaShift/Math.Max(1e-9,Math.Abs(d.alignPre)+0.001);
            d.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;
            d.immInduced=d.c3;
        }else{d.c3OmegaShift=0;d.c3=d.a0;d.strictPersist=d.a0;d.immInduced=d.a0;}

        d.basinSuccess=d.strictPersist&&d.distHi<d.distLo&&!d.invalid;
        d.result=d.basinSuccess?"basin-success":d.strictPersist?"induced":"failed";
        d.failureMode=d.c3OmegaShift>0.1?"near-break":d.dT1<0.5?"insufficient-dT1":d.alignPre>-0.1?"insufficient-align":"omega-gain-fail";
        d.maxC3OmgShift=d.c3OmegaShift;
        return d;
    }

    IoiDiag? RunDT1Amp(int n,int s,P3 hi,P3 lo,double amp,string label){
        var d=new IoiDiag{n=n,s=s,intervention=label,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0v=sb.Value.d0;
        d.distHi=ComputeDistHi(d0v,sb.Value.km0,sb.Value.ks0,hi);
        d.distLo=ComputeDistLo(d0v,sb.Value.km0,sb.Value.ks0,lo);

        // Stronger probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0v*0.90:d0v*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        // Amplify probe: scale toward more aggressive target
        double ampTgt=Math.Clamp(tgt*amp,cur*0.25,cur*4.0);
        double frac=Math.Clamp((ampTgt+1e-9)/(cur+1e-9),0.25,4.0);
        var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        if(!ValidD(dM,n)){d.invalid=true;return d;}
        K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);d.dT1=Dm(dT1,n);d.kT1=Km(Cupd(dT1,n),n);d.omT1=Of(hT1,n).Average();
        d.alignPre=ComputeAlignPre(d.dT1,d.kT1,Ks(Cupd(dT1,n),n),hi,lo);

        // T2 + C3
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);d.dT2=Dm(dT2,n);d.kT2=Km(Cupd(dT2,n),n);d.omT2=Of(hT2,n).Average();
        d.rebMag=d.dT2-d.dT1;d.a0=d.omT2>THR;

        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            if(!ValidD(dmat3,n)){d.invalid=true;return d;}
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);
            d.c3OmegaShift=omC3-d.omT2;d.deltaAlign=ComputeAlignPre(dmPost,kmPost,Ks(Cupd(dmat3,n),n),hi,lo)-d.alignPre;
            d.c3Effectiveness=d.c3OmegaShift/Math.Max(1e-9,Math.Abs(d.alignPre)+0.001);
            d.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;d.immInduced=d.c3;
        }
        d.basinSuccess=d.strictPersist&&d.distHi<d.distLo&&!d.invalid;
        d.result=d.basinSuccess?"basin-success":d.strictPersist?"induced":"failed";
        d.maxC3OmgShift=d.c3OmegaShift;
        return d;
    }

    IoiDiag? RunAntiAlign(int n,int s,P3 hi,P3 lo,double strength,string label){
        var d=new IoiDiag{n=n,s=s,intervention=label,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0v=sb.Value.d0;
        d.distHi=ComputeDistHi(d0v,sb.Value.km0,sb.Value.ks0,hi);
        d.distLo=ComputeDistLo(d0v,sb.Value.km0,sb.Value.ks0,lo);

        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0v*0.90:d0v*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);d.dT1=Dm(dT1,n);d.kT1=Km(Cupd(dT1,n),n);d.omT1=Of(hT1,n).Average();

        // Push toward more negative alignPre: nudge d_mean away from Hi
        double dmPreC3=Dm(dT1,n);
        double pushAway=dmPreC3-(hi.dm-dmPreC3)*strength; // push further from Hi
        double fPush=Math.Clamp((pushAway+1e-9)/(dmPreC3+1e-9),0.5,2.0);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dT1[i,j]*=fPush;
        if(!ValidD(dT1,n)){d.invalid=true;return d;}
        d.alignPre=ComputeAlignPre(Dm(dT1,n),Km(Cupd(dT1,n),n),Ks(Cupd(dT1,n),n),hi,lo);
        d.dT1=Dm(dT1,n);d.kT1=Km(Cupd(dT1,n),n);

        // T2 + C3
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);d.dT2=Dm(dT2,n);d.kT2=Km(Cupd(dT2,n),n);d.omT2=Of(hT2,n).Average();
        d.rebMag=d.dT2-d.dT1;d.a0=d.omT2>THR;

        if(!double.IsNaN(hi.dm)){
            double nudged=dmPreC3+(hi.dm-dmPreC3)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPreC3+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dT1[i,j]*=f3;
            if(!ValidD(dT1,n)){d.invalid=true;return d;}
            var hc3=Sim(Cupd(dT1,n),n,S,s+300);var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dT1,n),kmPost=Km(Cupd(dT1,n),n);
            d.c3OmegaShift=omC3-d.omT2;d.deltaAlign=ComputeAlignPre(dmPost,kmPost,Ks(Cupd(dT1,n),n),hi,lo)-d.alignPre;
            d.c3Effectiveness=d.c3OmegaShift/Math.Max(1e-9,Math.Abs(d.alignPre)+0.001);
            d.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;d.immInduced=d.c3;
        }
        d.result=d.strictPersist?"induced":"failed";d.maxC3OmgShift=d.c3OmegaShift;
        return d;
    }

    IoiDiag? RunDeltaAlignBoost(int n,int s,P3 hi,P3 lo,double boost,string label){
        var d=new IoiDiag{n=n,s=s,intervention=label,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0v=sb.Value.d0;
        d.distHi=ComputeDistHi(d0v,sb.Value.km0,sb.Value.ks0,hi);
        d.distLo=ComputeDistLo(d0v,sb.Value.km0,sb.Value.ks0,lo);

        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0v*0.90:d0v*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);d.dT1=Dm(dT1,n);d.kT1=Km(Cupd(dT1,n),n);d.omT1=Of(hT1,n).Average();
        d.alignPre=ComputeAlignPre(d.dT1,d.kT1,Ks(Cupd(dT1,n),n),hi,lo);

        // Boosted C3
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2*boost;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.25,4.0);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            if(!ValidD(dmat3,n)){d.invalid=true;return d;}
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+400);double omC3=Of(hc3cc,n).Average();
            d.c3OmegaShift=omC3-d.omT2;d.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;d.immInduced=d.c3;
        }
        d.result=d.strictPersist?"induced":"failed";d.maxC3OmgShift=d.c3OmegaShift;
        return d;
    }

    IoiDiag? RunKSupport(int n,int s,P3 hi,P3 lo,string label){
        var baseD=RunBaseline(n,s,hi,lo);
        if(baseD==null||baseD.Value.invalid||baseD.Value.strictPersist)return baseD;
        var d=baseD.Value;d.intervention=label;

        // Re-run to get K-preserved state
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return d;
        double d0v=sb.Value.d0;
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0v*0.90:d0v*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM4=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM4[i,j]*=frac;K2=Cupd(dM4,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);

        var Kpres=Cupd(dT1,n);
        var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
        double nudged=dmPre+(hi.dm-dmPre)*0.2;
        double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
        if(!ValidD(dmat3,n)){d.invalid=true;return d;}
        var Kc3=Cupd(dmat3,n);
        // Blend K
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)Kc3[i,j]=(Kc3[i,j]+Kpres[i,j])/2;
        var hc3=Sim(Kc3,n,S,s+300);var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
        var hc3cc=Sim(Kc3c,n,S,s+400);double omC3=Of(hc3cc,n).Average();
        d.c3OmegaShift=omC3-d.omT2;d.c3=omC3>THR;
        var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
        d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;d.immInduced=d.c3;
        d.maxC3OmgShift=Math.Max(d.c3OmegaShift,d.maxC3OmgShift);
        d.result=d.strictPersist?"induced":"failed";
        return d;
    }

    IoiDiag? RunOmegaGainAudit(int n,int s,P3 hi,P3 lo){
        var baseD=RunBaseline(n,s,hi,lo);
        if(baseD==null)return null;
        var d=baseD.Value;d.intervention="I5-OmegaAudit";
        // Already computed in baseline — just tag
        d.failureMode=d.c3OmegaShift<0.1?"omega-gain-block":"omega-gain-ok";
        return d;
    }

    IoiDiag? RunFullPackage(int n,int s,P3 hi,P3 lo){
        var d=new IoiDiag{n=n,s=s,intervention="I6-fullPKG",cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0v=sb.Value.d0;
        d.distHi=ComputeDistHi(d0v,sb.Value.km0,sb.Value.ks0,hi);
        d.distLo=ComputeDistLo(d0v,sb.Value.km0,sb.Value.ks0,lo);

        // I1: dT1 amp x2.0
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0v*0.90:d0v*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double ampTgt=Math.Clamp(tgt*2.0,cur*0.25,cur*4.0);
        double frac=Math.Clamp((ampTgt+1e-9)/(cur+1e-9),0.25,4.0);
        var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        if(!ValidD(dM,n)){d.invalid=true;return d;}
        K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);

        // I2: anti-alignment push
        var hT1pre=Sim(K2,n,S,s+100);
        var dT1pre=DL(Nm(RP(hT1pre,n),n),n);double dmPrePush=Dm(dT1pre,n);
        double pushAway=dmPrePush-(hi.dm-dmPrePush)*0.5;
        double fPush=Math.Clamp((pushAway+1e-9)/(dmPrePush+1e-9),0.5,2.0);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dT1pre[i,j]*=fPush;
        if(!ValidD(dT1pre,n)){d.invalid=true;return d;}
        K2=Cupd(dT1pre,n);
        var hT1=Sim(K2,n,S,s+150);
        var dT1=DL(Nm(RP(hT1,n),n),n);d.dT1=Dm(dT1,n);d.kT1=Km(Cupd(dT1,n),n);d.omT1=Of(hT1,n).Average();
        d.alignPre=ComputeAlignPre(d.dT1,d.kT1,Ks(Cupd(dT1,n),n),hi,lo);
        var Kpres=Cupd(dT1,n);

        // T2
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+250);
        var dT2=DL(Nm(RP(hT2,n),n),n);d.dT2=Dm(dT2,n);d.kT2=Km(Cupd(dT2,n),n);d.omT2=Of(hT2,n).Average();
        d.rebMag=d.dT2-d.dT1;d.a0=d.omT2>THR;

        // I3+I4: boosted C3 + K support
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            double nudged=dmPre+(hi.dm-dmPre)*0.4; // x2.0 C3
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.25,4.0);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            if(!ValidD(dmat3,n)){d.invalid=true;return d;}
            var Kc3=Cupd(dmat3,n);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)Kc3[i,j]=(Kc3[i,j]+Kpres[i,j])/2;
            var hc3=Sim(Kc3,n,S,s+350);var Kc3c=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3cc=Sim(Kc3c,n,S,s+450);double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);
            d.c3OmegaShift=omC3-d.omT2;d.deltaAlign=ComputeAlignPre(dmPost,kmPost,Ks(Cupd(dmat3,n),n),hi,lo)-d.alignPre;
            d.c3Effectiveness=d.c3OmegaShift/Math.Max(1e-9,Math.Abs(d.alignPre)+0.001);
            d.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+550);
            d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;d.immInduced=d.c3;
        }
        d.maxC3OmgShift=d.c3OmegaShift;
        d.result=d.strictPersist?"induced":"failed";
        return d;
    }

    IoiDiag? RunOverAmpControl(int n,int s,P3 hi,P3 lo){
        var d=RunDT1Amp(n,s,hi,lo,4.0,"C2-overAmp");
        if(d!=null){var dv=d.Value;dv.intervention="C2-overAmp";return dv;}
        return d;
    }

    // ─── Metric helpers ───
    static double ComputeAlignPre(double dm,double km,double ks,P3 hi,P3 lo){
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,vn=Math.Sqrt(dv*dv+kv*kv);
        return vn>0?((dm-lo.dm)*dv+(km-lo.km)*kv)/vn:0;
    }
    static double ComputeDistHi(double dm,double km,double ks,P3 hi)=>Math.Sqrt((dm-hi.dm)*(dm-hi.dm)+(km-hi.km)*(km-hi.km)+(ks-hi.ks)*(ks-hi.ks));
    static double ComputeDistLo(double dm,double km,double ks,P3 lo)=>Math.Sqrt((dm-lo.dm)*(dm-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks));
    static double ComputeProjHiVec(double dm,double km,double ks,P3 hi,P3 lo){
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        return vn>0?((dm-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
    }
    static double ComputeOffVecAngle(double dm,double km,double ks,P3 hi,P3 lo){
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((dm-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double d2=proj*proj+(dm-lo.dm)*(dm-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks)-proj*proj;
        double norm=Math.Sqrt((dm-lo.dm)*(dm-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks));
        return norm>1e-9&&vn>1e-9?Math.Acos(Math.Clamp(Math.Abs(proj)/norm,-1,1)):Math.PI/2;
    }
    static double GetMean(IoiDiag[] ps,string m)=>ps.Length==0?0:m switch{
        "c3OmegaShift"=>ps.Average(p=>p.c3OmegaShift),"dT1"=>ps.Average(p=>p.dT1),
        "alignPre"=>ps.Average(p=>p.alignPre),"deltaAlign"=>ps.Average(p=>p.deltaAlign),
        "omT1"=>ps.Average(p=>p.omT1),"kT1"=>ps.Average(p=>p.kT1),
        "distHi"=>ps.Average(p=>p.distHi),"rebMag"=>ps.Average(p=>p.rebMag),
        "c3Effectiveness"=>ps.Average(p=>p.c3Effectiveness),_=>0
    };
    static double Percentile(double[] sorted,double pct){
        if(sorted.Length==0)return 0;
        return sorted[Math.Clamp((int)(pct*(sorted.Length-1)),0,sorted.Length-1)];
    }

    // ─── Frozen M3++ helpers ───
    static bool ValidD(double[,]d,int n){for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return false;return true;}
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
}
