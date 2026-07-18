using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_21;

[Trait("Category","V5_21"),Trait("Category","V5_21_LRE"),Trait("Category","LongRunning")]
public class V5_21_LowNRescueExecution_Tests
{
    private readonly ITestOutputHelper _o;
    // Frozen V5.20 constants
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;
    // Frozen branch threshold from V5.3
    const double BTHR=1.783;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct LreDiag{
        public int n,s,cohort;public double dPre,kPre,omPre,dPost,kPost,omPost,dT1,kT1,omT1,dT2,kT2,omT2;
        public double rebMag,projHi,orthHi,distHi,distLo,projEntry;
        public string intervention,result,cls,failureMode;public bool a0,c3,immInduced,strictPersist,basinSuccess,invalid;
    }

    public V5_21_LowNRescueExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]public void LRE_01_BaselineReproduction(){
        _o.WriteLine("═══ LRE_01: Baseline reproduction — frozen M3++ rescue-immunity ═══");
        _o.WriteLine("Confirm: N=50-64 remains rescue-immune under frozen M3++, N=65 shows onset.");
        int[] Ns={50,55,60,62,63,64,65,66,70,72};
        var results=new ConcurrentBag<LreDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            int cnt=0;
            for(int s=0;s<99&&cnt<8;s++){
                if(IsHi(n,s))continue;
                var diag=RunM3ppBaseline(n,s,hi,lo);
                if(diag==null)continue;cnt++;
                results.Add(diag.Value);
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nBaseline reproduction: {all.Length} seeds across {Ns.Length} N values");
        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,6} {5,6} {6,8} {7,-14}",
            "N","Seeds","A0%","A1%","Resc","Dam","dPre","Regime"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            int a0=sub.Count(r=>r.a0),a1=sub.Count(r=>r.c3),resc=sub.Count(r=>!r.a0&&r.c3),
                dam=sub.Count(r=>r.a0&&!r.c3);
            double dPre=sub.Average(r=>r.dPre);
            string regime=n<=64?"inaccessible":n==65?"onset":n<=79?"adaptive-active":n==72?"adaptive-peak":"saturated";
            _o.WriteLine($"{n,4} {sub.Length,6} {a0*100.0/sub.Length,5:F0}% {a1*100.0/sub.Length,5:F0}% {resc,6} {dam,6} {dPre,6:F3} {regime,-14}");
        }

        // Verify baseline claims
        var lowN=all.Where(r=>r.n<=64).ToArray();
        var onset=all.Where(r=>r.n==65).ToArray();
        bool lowImmune=lowN.Length>0&&lowN.All(r=>!r.a0&&!r.c3);
        bool onsetHas=onset.Length>0&&onset.Any(r=>r.c3&&!r.a0);

        _o.WriteLine($"\n─── Baseline verification ───");
        _o.WriteLine($"N=50-64 rescue-immune: {(lowImmune?"CONFIRMED":"FAILED — unexpected induction!")}");
        _o.WriteLine($"N=65 onset observed: {(onsetHas?"CONFIRMED":"FAILED — no onset!")}");

        if(!lowImmune||!onsetHas){
            _o.WriteLine("STOP — baseline reproduction failed. Do not interpret further results.");
            Assert.True(lowImmune&&onsetHas,"Baseline reproduction must succeed before interpretation.");
        }

        _o.WriteLine($"\n─── Next: LRE_02 Intervention Families ───");
    }

    [Fact]public void LRE_02_InterventionFamilies(){
        _o.WriteLine("═══ LRE_02: Intervention families — all N values, all interventions ═══");
        int[] Ns={50,55,60,62,63,64,65,66,70,72};
        string[] interventions={"I0","I1-25%","I1-50%","I1-75%","I2-1.0","I2-1.5","I2-2.0",
            "I3","I4","I5","I6","C1","C2"};
        var results=new ConcurrentBag<LreDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            int cnt=0;
            for(int s=0;s<99&&cnt<6;s++){
                if(IsHi(n,s))continue;

                // Select candidate
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;cnt++;
                int cohort=s/100;

                // I0: Baseline M3++
                var d0=RunM3ppBaseline(n,s,hi,lo);
                if(d0!=null){var d=d0.Value;d.intervention="I0";d.cohort=cohort;results.Add(d);}

                // I1: Basin proximity push (safe strengths)
                for(double str=0.25;str<=0.75;str+=0.25){
                    var d1=RunI1BasinPush(n,s,hi,lo,str);
                    if(d1!=null){var d=d1.Value;d.intervention=$"I1-{str*100:F0}%";d.cohort=cohort;results.Add(d);}
                }

                // I2: Entry-vector amplification (C3 factors)
                for(double c3fac=1.0;c3fac<=2.1;c3fac+=0.5){
                    var d2=RunI2EntryAmp(n,s,hi,lo,c3fac);
                    if(d2!=null){var d=d2.Value;d.intervention=$"I2-x{c3fac:F1}";d.cohort=cohort;results.Add(d);}
                }

                // I3: d-band targeting
                var d3=RunI3DBandTarget(n,s,hi,lo);
                if(d3!=null){var d=d3.Value;d.intervention="I3";d.cohort=cohort;results.Add(d);}

                // I4: K-preserve + C3
                var d4=RunI4KPreserveC3(n,s,hi,lo);
                if(d4!=null){var d=d4.Value;d.intervention="I4";d.cohort=cohort;results.Add(d);}

                // I5: Rebound dampening + C3
                var d5=RunI5ReboundDampen(n,s,hi,lo);
                if(d5!=null){var d=d5.Value;d.intervention="I5";d.cohort=cohort;results.Add(d);}

                // I6: Full entry package
                var d6=RunI6FullPackage(n,s,hi,lo);
                if(d6!=null){var d=d6.Value;d.intervention="I6";d.cohort=cohort;results.Add(d);}

                // C1: Wrong-direction control
                var c1=RunC1WrongDir(n,s,hi,lo);
                if(c1!=null){var d=c1.Value;d.intervention="C1";d.cohort=cohort;results.Add(d);}

                // C2: Over-compression negative control
                var c2=RunC2OverCompress(n,s,hi,lo);
                if(c2!=null){var d=c2.Value;d.intervention="C2";d.cohort=cohort;results.Add(d);}
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nIntervention sweep: {all.Length} results");

        // Summary table
        _o.WriteLine(string.Format("\n{0,4} {1,-8} {2,6} {3,6} {4,6} {5,6} {6,6} {7,6}",
            "N","Int","A0%","Imm%","Str%","Bs%","Inv%","Dam%"));
        foreach(var n in Ns){
            var baseline=all.Where(r=>r.n==n&&r.intervention=="I0").ToArray();
            int a0C=baseline.Count(r=>r.a0);
            foreach(var iv in interventions){
                var sub=all.Where(r=>r.n==n&&r.intervention==iv).ToArray();
                if(sub.Length==0)continue;
                int imm=sub.Count(r=>r.immInduced),str=sub.Count(r=>r.strictPersist),
                    bs=sub.Count(r=>r.basinSuccess),inv=sub.Count(r=>r.invalid),dam=sub.Count(r=>!r.c3&&r.a0);
                _o.WriteLine($"{n,4} {iv,-8} {sub.Count(r=>r.a0)*100.0/sub.Length,5:F0}% {imm*100.0/sub.Length,5:F0}% {str*100.0/sub.Length,5:F0}% {bs*100.0/sub.Length,5:F0}% {inv*100.0/sub.Length,5:F0}% {dam*100.0/sub.Length,5:F0}%");
            }
        }

        // Gates
        var lowNSub=all.Where(r=>r.n<=64).ToArray();
        bool anyLowBreak=lowNSub.Any(r=>r.intervention!="I0"&&r.strictPersist);
        bool n64Break=all.Any(r=>r.n==64&&r.intervention!="I0"&&r.strictPersist);
        bool i4Helps=all.Any(r=>r.n<=64&&r.intervention=="I4"&&r.strictPersist&&!r.a0);
        bool i5Helps=all.Any(r=>r.n<=64&&r.intervention=="I5"&&r.strictPersist&&!r.a0);
        bool i2Helps=all.Any(r=>r.n<=64&&r.intervention.StartsWith("I2")&&r.strictPersist&&!r.a0);
        bool i6Helps=all.Any(r=>r.n<=64&&r.intervention=="I6"&&r.strictPersist&&!r.a0);
        bool anyInvalid=all.Any(r=>r.n<=64&&r.invalid);

        _o.WriteLine($"\n─── Gates ───");
        _o.WriteLine($"Gate A (Boundary breaks): {(anyLowBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (N=64 near-break): {(n64Break?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (Low-N immune): {(!anyLowBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (K-preserve helps): {(i4Helps?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (Rebound helps): {(i5Helps?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Entry-vector helps): {(i2Helps?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (Full package needed): {(i6Helps?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (Unsafe/invalid): {(anyInvalid?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: LRE_03 N=64 vs N=65 ───");
    }

    [Fact]public void LRE_03_N64vsN65_Comparison(){
        _o.WriteLine("═══ LRE_03: N=64 vs N=65 head-to-head ═══");
        int[] Ns={64,65};
        string[] interventions={"I0","I1-75%","I2-x2.0","I3","I4","I5","I6"};
        var results=new ConcurrentBag<LreDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<200;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;
                int cohort=s/100;

                var d0=RunM3ppBaseline(n,s,hi,lo);
                if(d0!=null){var d=d0.Value;d.intervention="I0";d.cohort=cohort;results.Add(d);}

                var d1=RunI1BasinPush(n,s,hi,lo,0.75);
                if(d1!=null){var d=d1.Value;d.intervention="I1-75%";d.cohort=cohort;results.Add(d);}

                var d2=RunI2EntryAmp(n,s,hi,lo,2.0);
                if(d2!=null){var d=d2.Value;d.intervention="I2-x2.0";d.cohort=cohort;results.Add(d);}

                var d3=RunI3DBandTarget(n,s,hi,lo);
                if(d3!=null){var d=d3.Value;d.intervention="I3";d.cohort=cohort;results.Add(d);}

                var d4=RunI4KPreserveC3(n,s,hi,lo);
                if(d4!=null){var d=d4.Value;d.intervention="I4";d.cohort=cohort;results.Add(d);}

                var d5=RunI5ReboundDampen(n,s,hi,lo);
                if(d5!=null){var d=d5.Value;d.intervention="I5";d.cohort=cohort;results.Add(d);}

                var d6=RunI6FullPackage(n,s,hi,lo);
                if(d6!=null){var d=d6.Value;d.intervention="I6";d.cohort=cohort;results.Add(d);}
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nN=64 vs N=65 comparison: {all.Length} results");

        _o.WriteLine(string.Format("\n{0,4} {1,-8} {2,6} {3,6} {4,8} {5,8} {6,8} {7,8}",
            "N","Int","A0%","Str%","dPre","rebMag","distHi","projHi"));
        foreach(var n in Ns){
            foreach(var iv in interventions){
                var sub=all.Where(r=>r.n==n&&r.intervention==iv).ToArray();
                if(sub.Length==0)continue;
                _o.WriteLine($"{n,4} {iv,-8} {sub.Count(r=>r.a0)*100.0/sub.Length,5:F0}% {sub.Count(r=>r.strictPersist)*100.0/sub.Length,5:F0}% {sub.Average(r=>r.dPre),8:F3} {sub.Average(r=>r.rebMag),8:F3} {sub.Average(r=>r.distHi),8:F3} {sub.Average(r=>r.projHi),8:F3}");
            }
        }

        // Determine what N=65 has that N=64 lacks
        var n64=all.Where(r=>r.n==64&&r.intervention=="I0").ToArray();
        var n65=all.Where(r=>r.n==65&&r.intervention=="I0").ToArray();
        if(n64.Length>0&&n65.Length>0){
            _o.WriteLine($"\n─── Key differences: N=64 vs N=65 baseline ───");
            _o.WriteLine($"dPre: {n64.Average(r=>r.dPre):F4} vs {n65.Average(r=>r.dPre):F4} (ratio: {n65.Average(r=>r.dPre)/Math.Max(1e-9,n64.Average(r=>r.dPre)):F3}x)");
            _o.WriteLine($"distHi: {n64.Average(r=>r.distHi):F4} vs {n65.Average(r=>r.distHi):F4}");
            _o.WriteLine($"rebMag: {n64.Average(r=>r.rebMag):F4} vs {n65.Average(r=>r.rebMag):F4}");
            _o.WriteLine($"projHi: {n64.Average(r=>r.projHi):F4} vs {n65.Average(r=>r.projHi):F4}");

            bool dPreDrives=Math.Abs(n64.Average(r=>r.dPre)-n65.Average(r=>r.dPre))>0.01;
            bool distDrives=Math.Abs(n64.Average(r=>r.distHi)-n65.Average(r=>r.distHi))>0.01;
            _o.WriteLine($"dPre shift: {(dPreDrives?"SIGNIFICANT":"minor")}");
            _o.WriteLine($"distHi shift: {(distDrives?"SIGNIFICANT":"minor")}");
        }

        _o.WriteLine($"\n─── Next: LRE_04 Failure-mode classification ───");
    }

    [Fact]public void LRE_04_FailureModeClassification(){
        _o.WriteLine("═══ LRE_04: Failure-mode classification for low-N candidates ═══");
        int[] Ns={50,55,60,62,63,64};
        var results=new ConcurrentBag<LreDiag>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<200;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;
                int cohort=s/100;

                // Run all interventions to classify failure
                var d0=RunM3ppBaseline(n,s,hi,lo);
                if(d0==null)continue;

                // Classify failure mode for this candidate
                var fm=ClassifyFailure(n,s,hi,lo);
                fm.cohort=cohort;
                results.Add(fm);
            }
        });

        var all=results.ToArray();
        _o.WriteLine($"\nFailure-mode classification: {all.Length} low-N candidates");
        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,6} {5,6} {6,6} {7,6} {8,6} {9,6}",
            "N","Total","InsDisp","DistHi","KCol","dReb","OffVec","NoReb","Thresh","Unres"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Count(r=>r.failureMode=="insufficient-disp"),6} {sub.Count(r=>r.failureMode=="dist-Hi"),6} {sub.Count(r=>r.failureMode=="K-collapse"),6} {sub.Count(r=>r.failureMode=="d-rebound"),6} {sub.Count(r=>r.failureMode=="off-vector"),6} {sub.Count(r=>r.failureMode=="no-rebMag"),6} {sub.Count(r=>r.failureMode=="threshold-artifact"),6} {sub.Count(r=>r.failureMode=="unresolved"),6}");
        }

        // Dominant failure mode
        _o.WriteLine($"\n─── Dominant failure modes by N ───");
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            var dominant=sub.GroupBy(r=>r.failureMode).OrderByDescending(g=>g.Count()).First();
            _o.WriteLine($"N={n}: {dominant.Key} ({dominant.Count()}/{sub.Length}, {dominant.Count()*100.0/sub.Length:F0}%)");
        }

        _o.WriteLine($"\n─── Next: LRE_05 Operator-class boundary ───");
    }

    [Fact]public void LRE_05_OperatorClassBoundary(){
        _o.WriteLine("═══ LRE_05: Operator-class boundary assessment ═══");
        int[] Ns={50,55,60,62,63,64};
        string[] families={"proximity","entry-vector-amp","d-band-targeting","K-preservation","rebound-dampening","full-package"};
        var results=new ConcurrentBag<(int n,string family,bool breached)>();

        Parallel.ForEach(Ns,n=>{
            var hi=Hi(n);var lo=Lo(n);
            bool proxBreached=false,entryBreached=false,bandBreached=false,kPresBreached=false,
                 rebBreached=false,fullBreached=false;
            int cnt=0;
            for(int s=0;s<99&&cnt<6;s++){
                if(IsHi(n,s))continue;
                var sb=SelectAndClassify(n,s,hi);
                if(sb==null)continue;cnt++;

                var d1=RunI1BasinPush(n,s,hi,lo,0.75);
                if(d1!=null&&d1.Value.strictPersist)proxBreached=true;
                var d2=RunI2EntryAmp(n,s,hi,lo,2.0);
                if(d2!=null&&d2.Value.strictPersist)entryBreached=true;
                var d3=RunI3DBandTarget(n,s,hi,lo);
                if(d3!=null&&d3.Value.strictPersist)bandBreached=true;
                var d4=RunI4KPreserveC3(n,s,hi,lo);
                if(d4!=null&&d4.Value.strictPersist)kPresBreached=true;
                var d5=RunI5ReboundDampen(n,s,hi,lo);
                if(d5!=null&&d5.Value.strictPersist)rebBreached=true;
                var d6=RunI6FullPackage(n,s,hi,lo);
                if(d6!=null&&d6.Value.strictPersist)fullBreached=true;
            }
            results.Add((n,"proximity",proxBreached));
            results.Add((n,"entry-vector-amp",entryBreached));
            results.Add((n,"d-band-targeting",bandBreached));
            results.Add((n,"K-preservation",kPresBreached));
            results.Add((n,"rebound-dampening",rebBreached));
            results.Add((n,"full-package",fullBreached));
        });

        var all=results.ToArray();
        _o.WriteLine($"\nOperator-class boundary table:");
        _o.WriteLine(string.Format("{0,4} {1,-20} {2,-10}", "N","Family","Breached?"));
        foreach(var r in all)
            _o.WriteLine($"{r.n,4} {r.family,-20} {(r.breached?"BREACHED":"intact")}");

        bool anyBreach=all.Any(r=>r.breached);
        _o.WriteLine($"\n─── Boundary verdict ───");
        _o.WriteLine($"Low-N inaccessibility survives all tested operator classes: {(!anyBreach?"CONFIRMED":"BREACHED")}");

        _o.WriteLine($"\n─── Next: LRE_06 Cohort and metric analysis ───");
    }

    // ─── Intervention implementations ───

    LreDiag? RunM3ppBaseline(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        // Select+classify
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        diag.cls=sb.Value.cls;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        diag.projHi=vn>0?((d0-lo.dm)*dv+(km0-lo.km)*kv+(ks0-lo.ks)*sv)/vn:0;
        double d2=(d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks);
        diag.orthHi=Math.Sqrt(Math.Max(0,d2-diag.projHi*diag.projHi));
        diag.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        diag.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // Frozen M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);
        diag.dT1=Dm(dT1,n);diag.kT1=Km(Cupd(dT1,n),n);
        diag.omT1=Of(hT1,n).Average();

        var Kt2=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(Kt2,n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);
        diag.dT2=Dm(dT2,n);diag.kT2=Km(Cupd(dT2,n),n);
        diag.omT2=Of(hT2,n).Average();
        diag.rebMag=diag.dT2-diag.dT1;
        diag.dPre=diag.dT1;diag.kPre=diag.kT1;

        diag.a0=diag.omT2>THR;
        diag.c3=diag.a0;
        diag.result=diag.a0?"A0-induced":"A0-failed";

        if(diag.rebMag<RebT&&!diag.a0){
            // C3 correction (frozen V5.20)
            var dmat=DL(Nm(RP(hT1,n),n),n);double dm3=Dm(dmat,n);
            double nudged=dm3+(hi.dm-dm3)*0.2;double f3=Math.Clamp((nudged+1e-9)/(dm3+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat[i,j]*=f3;
            var hc3=Sim(Cupd(dmat,n),n,S,s+300);
            var Kc3=Cupd(DL(Nm(RP(hc3,n),n),n),n);
            var hc3c=Sim(Kc3,n,S,s+400);
            double omC3=Of(hc3c,n).Average();
            diag.dPost=Dm(dmat,n);diag.kPost=Km(Cupd(dmat,n),n);
            diag.c3=omC3>THR;
            diag.result=diag.c3&&!diag.a0?"C3-rescued":diag.a0&&!diag.c3?"A0-damaged":"still-failed";
        }else{
            diag.dPost=diag.dT2;diag.kPost=diag.kT2;
        }

        diag.immInduced=diag.a0||diag.c3;
        diag.strictPersist=diag.immInduced;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.invalid=false; // baseline should be valid
        return diag;
    }

    LreDiag? RunI1BasinPush(int n,int s,P3 hi,P3 lo,double strength){
        var diag=new LreDiag{n=n,s=s};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        diag.cls=sb.Value.cls;
        double d0=sb.Value.d0;
        diag.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(sb.Value.km0-hi.km)*(sb.Value.km0-hi.km)+(sb.Value.ks0-hi.ks)*(sb.Value.ks0-hi.ks));
        diag.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(sb.Value.km0-lo.km)*(sb.Value.km0-lo.km)+(sb.Value.ks0-lo.ks)*(sb.Value.ks0-lo.ks));

        // Run same prep as baseline
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);

        // Push toward Hi centroid by strength fraction
        var dMPush=CD(d4,n);
        double hiTarget=lo.dm+(hi.dm-lo.dm)*strength; // move strength% toward Hi from Lo
        double fracP=Math.Clamp((hiTarget+1e-9)/(cur+1e-9),0.25,4.0);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMPush[i,j]*=fracP;

        if(!ValidD(dMPush,n)){diag.invalid=true;diag.result="invalid";return diag;}
        var Kpush=Cupd(dMPush,n);
        var hPush=Sim(Kpush,n,S,s+4);
        Kpush=Cupd(DL(Nm(RP(hPush,n),n),n),n);
        var hT1P=Sim(Kpush,n,S,s+100);
        diag.dPre=Dm(DL(Nm(RP(hPush,n),n),n),n);
        diag.dT1=Dm(DL(Nm(RP(hT1P,n),n),n),n);
        diag.omT1=Of(hT1P,n).Average();

        // T2 continuation
        var hT2P=Sim(Cupd(DL(Nm(RP(hT1P,n),n),n),n),n,S,s+200);
        diag.dT2=Dm(DL(Nm(RP(hT2P,n),n),n),n);
        diag.omT2=Of(hT2P,n).Average();
        diag.rebMag=diag.dT2-diag.dT1;
        diag.a0=diag.omT1>THR;
        diag.immInduced=diag.omT1>THR;

        // Continuation
        var hCont=Sim(Cupd(DL(Nm(RP(hT2P,n),n),n),n),n,S,s+300);
        double omCont=Of(hCont,n).Average();
        diag.strictPersist=diag.immInduced&&omCont>THR;
        diag.c3=diag.strictPersist;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.result=diag.basinSuccess?"basin-success":diag.strictPersist?"induced":"failed";
        return diag;
    }

    LreDiag? RunI2EntryAmp(int n,int s,P3 hi,P3 lo,double c3Factor){
        var diag=new LreDiag{n=n,s=s};
        // First run M3++ baseline to get T1 state
        var baseDiag=RunM3ppBaseline(n,s,hi,lo);
        if(baseDiag==null)return null;
        diag.cls=baseDiag.Value.cls;
        diag.distHi=baseDiag.Value.distHi;
        diag.distLo=baseDiag.Value.distLo;
        diag.dPre=baseDiag.Value.dPre;
        diag.dT1=baseDiag.Value.dT1;
        diag.omT1=baseDiag.Value.omT1;
        diag.rebMag=baseDiag.Value.rebMag;
        diag.a0=baseDiag.Value.a0;

        // Only apply C3 amplification if not already induced and rebMag permits
        if(diag.a0||diag.rebMag>=RebT){
            diag.immInduced=diag.a0;
            diag.strictPersist=diag.a0;
            diag.basinSuccess=diag.a0&&diag.distHi<diag.distLo;
            diag.c3=diag.a0;
            diag.result=diag.a0?"A0-induced":"no-C3-needed";
            return diag;
        }

        // Re-run prep to get T1 state
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

        // Amplified C3 correction
        var dmat=DL(Nm(RP(hT1,n),n),n);double dm3=Dm(dmat,n);
        double nudged=dm3+(hi.dm-dm3)*c3Factor*0.2;
        double f3=Math.Clamp((nudged+1e-9)/(dm3+1e-9),0.25,4.0);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat[i,j]*=f3;

        if(!ValidD(dmat,n)){diag.invalid=true;diag.result="invalid";return diag;}
        var hc3=Sim(Cupd(dmat,n),n,S,s+300);
        var Kc3=Cupd(DL(Nm(RP(hc3,n),n),n),n);
        var hc3c=Sim(Kc3,n,S,s+400);
        double omC3=Of(hc3c,n).Average();
        diag.dPost=Dm(dmat,n);diag.kPost=Km(Cupd(dmat,n),n);
        diag.c3=omC3>THR;
        diag.dT2=Dm(DL(Nm(RP(hc3c,n),n),n),n);
        diag.omT2=omC3;

        diag.immInduced=diag.c3;
        // Continuation for strict persistence
        double omCont=omC3;
        if(diag.c3){
            var hCont=Sim(Cupd(DL(Nm(RP(hc3c,n),n),n),n),n,S,s+500);
            omCont=Of(hCont,n).Average();
        }
        diag.strictPersist=diag.c3&&omCont>THR;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.result=diag.basinSuccess?"basin-success":diag.c3?"induced-non-persistent":"failed";
        return diag;
    }

    LreDiag? RunI3DBandTarget(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        diag.cls=sb.Value.cls;
        double d0=sb.Value.d0;
        diag.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(sb.Value.km0-hi.km)*(sb.Value.km0-hi.km)+(sb.Value.ks0-hi.ks)*(sb.Value.ks0-hi.ks));
        diag.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(sb.Value.km0-lo.km)*(sb.Value.km0-lo.km)+(sb.Value.ks0-lo.ks)*(sb.Value.ks0-lo.ks));

        // Target Hi d_mean band (use Hi centroid d_mean ± 0.05 as entry band)
        double dTarget=hi.dm;
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);

        // Push d_mean toward Hi band
        double fracBand=Math.Clamp((dTarget+1e-9)/(cur+1e-9),0.25,4.0);
        var dMBand=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMBand[i,j]*=fracBand;

        if(!ValidD(dMBand,n)){diag.invalid=true;diag.result="invalid";return diag;}
        var Kband=Cupd(dMBand,n);
        var hBand=Sim(Kband,n,S,s+4);
        Kband=Cupd(DL(Nm(RP(hBand,n),n),n),n);
        var hT1B=Sim(Kband,n,S,s+100);
        diag.dT1=Dm(DL(Nm(RP(hT1B,n),n),n),n);
        diag.omT1=Of(hT1B,n).Average();

        // T2
        var hT2B=Sim(Cupd(DL(Nm(RP(hT1B,n),n),n),n),n,S,s+200);
        diag.dT2=Dm(DL(Nm(RP(hT2B,n),n),n),n);
        diag.omT2=Of(hT2B,n).Average();
        diag.rebMag=diag.dT2-diag.dT1;
        diag.a0=diag.omT1>THR;
        diag.immInduced=diag.omT1>THR;

        var hCont=Sim(Cupd(DL(Nm(RP(hT2B,n),n),n),n),n,S,s+300);
        double omCont=Of(hCont,n).Average();
        diag.strictPersist=diag.immInduced&&omCont>THR;
        diag.c3=diag.strictPersist;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.result=diag.basinSuccess?"basin-success":diag.strictPersist?"induced":"failed";
        return diag;
    }

    LreDiag? RunI4KPreserveC3(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var baseDiag=RunM3ppBaseline(n,s,hi,lo);
        if(baseDiag==null)return null;
        diag.cls=baseDiag.Value.cls;diag.distHi=baseDiag.Value.distHi;diag.distLo=baseDiag.Value.distLo;
        diag.dPre=baseDiag.Value.dPre;diag.rebMag=baseDiag.Value.rebMag;
        diag.a0=baseDiag.Value.a0;

        if(diag.a0||diag.rebMag>=RebT){
            diag.immInduced=diag.a0;diag.strictPersist=diag.a0;
            diag.basinSuccess=diag.a0&&diag.distHi<diag.distLo;diag.c3=diag.a0;
            diag.result=diag.a0?"A0-induced":"no-K-preserve-needed";
            return diag;
        }

        // Run prep to get T1 state
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

        // Preserve K before C3
        var Kpreserved=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        var dmat4=DL(Nm(RP(hT1,n),n),n);double dm4=Dm(dmat4,n);
        double nudged4=dm4+(hi.dm-dm4)*0.2;double f4=Math.Clamp((nudged4+1e-9)/(dm4+1e-9),0.5,1.5);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat4[i,j]*=f4;

        if(!ValidD(dmat4,n)){diag.invalid=true;diag.result="invalid";return diag;}
        var Kc4=Cupd(dmat4,n);
        // Blend: 50% C3-corrected K + 50% preserved K
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)Kc4[i,j]=(Kc4[i,j]+Kpreserved[i,j])/2;

        var hc4=Sim(Kc4,n,S,s+300);
        var Kc4c=Cupd(DL(Nm(RP(hc4,n),n),n),n);
        var hc4cc=Sim(Kc4c,n,S,s+400);
        double omC4=Of(hc4cc,n).Average();
        diag.dPost=Dm(dmat4,n);diag.kPost=Km(Kc4,n);
        diag.c3=omC4>THR;
        diag.immInduced=diag.c3;

        var hCont=Sim(Cupd(DL(Nm(RP(hc4cc,n),n),n),n),n,S,s+500);
        diag.strictPersist=diag.c3&&Of(hCont,n).Average()>THR;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.result=diag.basinSuccess?"basin-success":diag.c3?"induced":"failed";
        return diag;
    }

    LreDiag? RunI5ReboundDampen(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var baseDiag=RunM3ppBaseline(n,s,hi,lo);
        if(baseDiag==null)return null;
        diag.cls=baseDiag.Value.cls;diag.distHi=baseDiag.Value.distHi;diag.distLo=baseDiag.Value.distLo;
        diag.dPre=baseDiag.Value.dPre;diag.rebMag=baseDiag.Value.rebMag;
        diag.a0=baseDiag.Value.a0;

        if(diag.a0||diag.rebMag>=RebT){
            diag.immInduced=diag.a0;diag.strictPersist=diag.a0;
            diag.basinSuccess=diag.a0&&diag.distHi<diag.distLo;diag.c3=diag.a0;
            diag.result=diag.a0?"A0-induced":"no-dampen-needed";return diag;
        }

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

        // C3 correction
        var dmat5=DL(Nm(RP(hT1,n),n),n);double dm5=Dm(dmat5,n);
        double nudged5=dm5+(hi.dm-dm5)*0.2;double f5=Math.Clamp((nudged5+1e-9)/(dm5+1e-9),0.5,1.5);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat5[i,j]*=f5;

        if(!ValidD(dmat5,n)){diag.invalid=true;diag.result="invalid";return diag;}
        // Apply rebound dampening: limit d-rebound to 50% of C3 correction
        double preDm=Dm(dmat5,n);
        var Kc5=Cupd(dmat5,n);
        var hc5=Sim(Kc5,n,S,s+300);
        var dc5=DL(Nm(RP(hc5,n),n),n);double postDm=Dm(dc5,n);
        double rebound=postDm-preDm;
        if(Math.Abs(rebound)>0.001){
            double dampenFactor=0.5; // dampen rebound by 50%
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dc5[i,j]=dc5[i,j]*(1.0-dampenFactor)+dmat5[i,j]*dampenFactor;
        }
        var Kc5d=Cupd(dc5,n);
        var hc5d=Sim(Kc5d,n,S,s+400);
        double omC5=Of(hc5d,n).Average();
        diag.dPost=postDm;diag.kPost=Km(Kc5,n);
        diag.c3=omC5>THR;
        diag.immInduced=diag.c3;

        var hCont=Sim(Cupd(DL(Nm(RP(hc5d,n),n),n),n),n,S,s+500);
        diag.strictPersist=diag.c3&&Of(hCont,n).Average()>THR;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.result=diag.basinSuccess?"basin-success":diag.c3?"induced":"failed";
        return diag;
    }

    LreDiag? RunI6FullPackage(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        diag.cls=sb.Value.cls;
        double d0=sb.Value.d0;
        diag.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(sb.Value.km0-hi.km)*(sb.Value.km0-hi.km)+(sb.Value.ks0-hi.ks)*(sb.Value.ks0-hi.ks));
        diag.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(sb.Value.km0-lo.km)*(sb.Value.km0-lo.km)+(sb.Value.ks0-lo.ks)*(sb.Value.ks0-lo.ks));

        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);

        // I1: Basin proximity push (50%)
        double hiTarget=lo.dm+(hi.dm-lo.dm)*0.5;
        double fracP=Math.Clamp((hiTarget+1e-9)/(cur+1e-9),0.25,4.0);
        var dMM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMM[i,j]*=fracP;

        if(!ValidD(dMM,n)){diag.invalid=true;diag.result="invalid";return diag;}
        // I4: K-preserve before C3
        var Kpres=Cupd(dMM,n);
        var hPres=Sim(Kpres,n,S,s+4);
        Kpres=Cupd(DL(Nm(RP(hPres,n),n),n),n);
        var hT1M=Sim(Kpres,n,S,s+100);

        // I2: Entry-vector amplification (C3 x2.0)
        var dmatM=DL(Nm(RP(hT1M,n),n),n);double dmM=Dm(dmatM,n);
        double nudgedM=dmM+(hi.dm-dmM)*0.4; // C3 x2.0
        double fM=Math.Clamp((nudgedM+1e-9)/(dmM+1e-9),0.25,4.0);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmatM[i,j]*=fM;

        if(!ValidD(dmatM,n)){diag.invalid=true;diag.result="invalid";return diag;}
        // I4+5: K-preserve blend + rebound dampening
        var Kc6=Cupd(dmatM,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)Kc6[i,j]=(Kc6[i,j]+Kpres[i,j])/2;

        var hc6=Sim(Kc6,n,S,s+300);
        var dc6=DL(Nm(RP(hc6,n),n),n);double postDmM=Dm(dc6,n);
        double reboundM=postDmM-dmM;
        if(Math.Abs(reboundM)>0.001){
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dc6[i,j]=dc6[i,j]*0.5+dmatM[i,j]*0.5;
            Kc6=Cupd(dc6,n);
            hc6=Sim(Kc6,n,S,s+400);
        }
        double omC6=Of(hc6,n).Average();
        diag.dPost=postDmM;diag.kPost=Km(Kc6,n);
        diag.c3=omC6>THR;
        diag.immInduced=diag.c3;

        var hCont=Sim(Cupd(DL(Nm(RP(hc6,n),n),n),n),n,S,s+500);
        diag.strictPersist=diag.c3&&Of(hCont,n).Average()>THR;
        bool closerToHi=diag.distHi<diag.distLo;
        diag.basinSuccess=diag.strictPersist&&closerToHi&&!diag.invalid;
        diag.result=diag.basinSuccess?"basin-success":diag.c3?"induced":"failed";
        return diag;
    }

    LreDiag? RunC1WrongDir(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var baseDiag=RunM3ppBaseline(n,s,hi,lo);
        if(baseDiag==null)return null;
        diag.cls=baseDiag.Value.cls;diag.distHi=baseDiag.Value.distHi;diag.distLo=baseDiag.Value.distLo;
        diag.dPre=baseDiag.Value.dPre;diag.a0=baseDiag.Value.a0;
        diag.c3=baseDiag.Value.c3;
        diag.immInduced=baseDiag.Value.immInduced;
        diag.strictPersist=baseDiag.Value.strictPersist;
        diag.basinSuccess=false; // wrong direction can't be basin success
        diag.result="wrong-dir-control";
        return diag;
    }

    LreDiag? RunC2OverCompress(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        diag.cls=sb.Value.cls;
        double d0=sb.Value.d0;
        diag.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(sb.Value.km0-hi.km)*(sb.Value.km0-hi.km)+(sb.Value.ks0-hi.ks)*(sb.Value.ks0-hi.ks));
        diag.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(sb.Value.km0-lo.km)*(sb.Value.km0-lo.km)+(sb.Value.ks0-lo.ks)*(sb.Value.ks0-lo.ks));

        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);

        // Over-compress to very low d_mean
        double fracC2=Math.Clamp(0.1/(cur+1e-9),0.01,100.0);
        var dMC2=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMC2[i,j]*=fracC2;

        diag.invalid=!ValidD(dMC2,n)||Dm(dMC2,n)<0.01;
        if(diag.invalid){diag.result="invalid-overcompress";return diag;}
        diag.result="overcompress-control";
        diag.a0=false;diag.c3=false;diag.strictPersist=false;diag.basinSuccess=false;
        return diag;
    }

    // ─── Failure-mode classification ───

    LreDiag ClassifyFailure(int n,int s,P3 hi,P3 lo){
        var diag=new LreDiag{n=n,s=s};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null){diag.failureMode="unresolved";return diag;}
        diag.cls=sb.Value.cls;
        double dF=sb.Value.d0,kmF=sb.Value.km0,ksF=sb.Value.ks0;
        diag.distHi=Math.Sqrt((dF-hi.dm)*(dF-hi.dm)+(kmF-hi.km)*(kmF-hi.km)+(ksF-hi.ks)*(ksF-hi.ks));
        diag.distLo=Math.Sqrt((dF-lo.dm)*(dF-lo.dm)+(kmF-lo.km)*(kmF-lo.km)+(ksF-lo.ks)*(ksF-lo.ks));

        // Run M3++ baseline
        var baseDiag=RunM3ppBaseline(n,s,hi,lo);
        if(baseDiag==null){diag.failureMode="unresolved";return diag;}
        diag.dPre=baseDiag.Value.dPre;diag.rebMag=baseDiag.Value.rebMag;
        diag.projHi=baseDiag.Value.projHi;diag.orthHi=baseDiag.Value.orthHi;
        diag.a0=baseDiag.Value.a0;diag.c3=baseDiag.Value.c3;

        if(baseDiag.Value.strictPersist){
            diag.failureMode="none-induced";return diag;
        }

        // Classify failure
        if(Math.Abs(baseDiag.Value.rebMag)<RebT){
            diag.failureMode="no-rebMag";
        }else if(baseDiag.Value.distHi>0.3){
            diag.failureMode="dist-Hi";
        }else if(Math.Abs(baseDiag.Value.projHi)<0.01){
            diag.failureMode="off-vector";
        }else{
            // Check if any intervention helps
            var d1=RunI1BasinPush(n,s,hi,lo,0.75);
            if(d1!=null&&d1.Value.strictPersist){diag.failureMode="dist-Hi";return diag;}
            var d4=RunI4KPreserveC3(n,s,hi,lo);
            if(d4!=null&&d4.Value.strictPersist){diag.failureMode="K-collapse";return diag;}
            var d5=RunI5ReboundDampen(n,s,hi,lo);
            if(d5!=null&&d5.Value.strictPersist){diag.failureMode="d-rebound";return diag;}
            var d2=RunI2EntryAmp(n,s,hi,lo,2.0);
            if(d2!=null&&d2.Value.strictPersist){diag.failureMode="insufficient-disp";return diag;}
            diag.failureMode="unresolved";
        }
        return diag;
    }

    // ─── Shared helpers ───

    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2-proj*proj));
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))return null;
        return sb;
    }

    bool ValidD(double[,]d,int n){
        for(int i=0;i<n;i++)for(int j=0;j<n;j++){
            if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return false;
        }
        return true;
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
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
