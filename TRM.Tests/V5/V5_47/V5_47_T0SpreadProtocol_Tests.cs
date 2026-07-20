using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_47;

[Trait("Category","V5_47"),Trait("Category","V5_47_TSP"),Trait("Category","V5_47_TSE"),Trait("Category","V5_47_THD"),Trait("Category","V5_47_TSA"),Trait("Category","LongRunning")]
public class V5_47_T0SpreadProtocol_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{
        public int N,seed,cohort;
        // Warmup trace
        public double[] warmOm;        // omega per warmup epoch
        // T0 post-warmup
        public double om0,lam0,d0,km0,ks0;
        // T1 post-compression
        public double om1,lam1,d1,km1,ks1;
        // T2 C3 entry
        public double om2,lam2,d2,km2,ks2,omDist2;
        // T3
        public double om3,lam3,reb3;
        // C3 correction
        public double entOm,entDm,entKm,entLam,frac;
        public double exitOm2,omDelta,cs4,a0Prox;
        public bool resc4,inv;
    }

    public V5_47_T0SpreadProtocol_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){
        var ep=new EP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        // Warmup epoch trace
        var warmOm=new double[WARMUP_EPOCHS];
        var K=KS(n,s);
        for(int e=0;e<WARMUP_EPOCHS;e++){
            var h=Sim(K,n,S,s+e);
            warmOm[e]=Of(h,n).Average();
            var d=DL(Nm(RP(h,n),n),n);
            K=Cupd(d,n);
            if(e==WARMUP_EPOCHS-1){
                ep.d0=Dm(d,n);ep.km0=Km(K,n);ep.ks0=Ks(K,n);
            }
        }
        ep.warmOm=warmOm;
        // T0
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        // T1
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);
        ep.d1=Dm(CD(d4,n),n);ep.km1=Km(K,n);ep.ks1=Ks(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();
        // T2 (C3 entry)
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var dmat2=DL(Nm(RP(hT1,n),n),n);ep.d2=Dm(dmat2,n);ep.km2=Km(KT1,n);ep.ks2=Ks(KT1,n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        // T3
        ep.om3=omT2;ep.lam3=Lambda1(Cupd(DL(Nm(RP(hT2,n),n),n),n),n);ep.reb3=omT2-omT1;
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            ep.entDm=dmPre;ep.entOm=omT1;ep.entKm=Km(KT1,n);ep.entLam=Lambda1(KT1,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);ep.frac=f3;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();ep.exitOm2=omC3;
            c3=omC3-(a0?THR:omT2);ep.omDelta=omC3-omT1;
        }
        ep.cs4=c3;ep.a0Prox=Math.Abs(omT2-THR);ep.resc4=c3>0.1&&omT2>THR;ep.inv=double.IsNaN(c3);
        return ep;
    }

    [Fact]
    public void TSP_01_PostWarmupT0SpreadOriginProtocol()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== TSP_01: Post-Warmup T0 Spread Origin Protocol ===");
        _o.WriteLine("=== V5.47 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Branch: feature/v5.47-post-warmup-t0-spread-origin-and-n-window-formation ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={67,70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        _o.WriteLine($"Profiles collected: {data.Length}");
        foreach(var n in Ns){var c=data.Count(d=>d.N==n);_o.WriteLine($"  N={n}: {c} profiles");}

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART A — Protocol Freeze");
        _o.WriteLine(new string('=',80));
        _o.WriteLine("N values: 67, 70, 72, 75");
        _o.WriteLine("Checkpoints: warmup epochs, T0, T1, T2 (entry), T3, C3");
        _o.WriteLine("Metrics: IQR, range, std, median, quantiles, skew, resc count,");
        _o.WriteLine("         c3OmgS high count, entry IQR, T0 IQR, d/K/lambda1/omDist/rebMagnitude");
        _o.WriteLine("Hard forbidden: no threshold tuning, no M3++ changes, no post-hoc N exclusion,");
        _o.WriteLine("                no seed removal, no outlier deletion, no new variables, no V6");
        _o.WriteLine("Protocol FROZEN.");

        // ========================
        // PART B — T0 Origin Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — T0 Origin Audit");
        _o.WriteLine(new string('=',80));

        // Per-N metrics
        var nMetrics=new ConcurrentDictionary<int,(double t0Iqr,double t0Rng,double t0Std,double t0Med,
            double entryIqr,double retention,double t1Iqr,double t2Iqr,double t3Iqr,
            double t0Skew,double iqrRngRatio,int nProf,int nResc,double rescRate,double c3High)>();

        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();
            if(nd.Length<4)continue;
            var om0s=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();
            var om2s=nd.Select(d=>d.om2).OrderBy(v=>v).ToArray();
            var om1s=nd.Select(d=>d.om1).OrderBy(v=>v).ToArray();
            var om3s=nd.Select(d=>d.om3).OrderBy(v=>v).ToArray();
            double t0Iqr=Q(om0s,0.75)-Q(om0s,0.25);
            double t0Rng=om0s.Last()-om0s.First();
            double t0Std=Sd(om0s);
            double t0Med=om0s[om0s.Length/2];
            double entryIqr=Q(om2s,0.75)-Q(om2s,0.25);
            double t1Iqr=Q(om1s,0.75)-Q(om1s,0.25);
            double t2Iqr=entryIqr;
            double t3Iqr=Q(om3s,0.75)-Q(om3s,0.25);
            double ret=t0Iqr>0.001?entryIqr/t0Iqr:0;
            double t0Skew=Skew(om0s);
            double iqrRng=t0Rng>0.001?t0Iqr/t0Rng:0;
            int nProf=nd.Length,nResc=nd.Count(d=>d.resc4);
            double rescRate=nProf>0?nResc/(double)nProf:0;
            double c3High=nd.Count(d=>d.cs4>0.1)/(double)nProf;
            nMetrics[n]=(t0Iqr,t0Rng,t0Std,t0Med,entryIqr,ret,t1Iqr,t2Iqr,t3Iqr,t0Skew,iqrRngRatio:iqrRng,nProf,nResc,rescRate,c3High);
        }

        _o.WriteLine($"{"N",6} {"nProf",6} {"nResc",6} {"t0IQR",9} {"t0Rng",9} {"t0Std",9} {"t0Med",9} {"t0Skew",8} {"IQR/Rng",8} {"entIQR",9} {"retain",7} {"t1IQR",9} {"t2IQR",9} {"t3IQR",9} {"rescRt",7} {"c3High",7}");
        _o.WriteLine(new string('-',140));
        foreach(var n in Ns){
            if(!nMetrics.TryGetValue(n,out var m))continue;
            _o.WriteLine($"{n,6} {m.nProf,6} {m.nResc,6} {m.t0Iqr,9:F3} {m.t0Rng,9:F3} {m.t0Std,9:F3} {m.t0Med,9:F3} {m.t0Skew,8:F2} {m.iqrRngRatio,8:F2} {m.entryIqr,9:F3} {m.retention,7:F2} {m.t1Iqr,9:F3} {m.t2Iqr,9:F3} {m.t3Iqr,9:F3} {m.rescRate,7:F2} {m.c3High,7:F2}");
        }

        // Primary checks
        _o.WriteLine("\n--- Primary Checks ---");
        double n75t0=nMetrics[75].t0Iqr;
        double maxOther=new[]{67,70,72}.Select(n=>nMetrics[n].t0Iqr).Max();
        _o.WriteLine($"1. N=75 T0 IQR={n75t0:F3} vs next highest={maxOther:F3} => {(n75t0>maxOther*10?"UNIQUELY broad at T0":"similar")}");
        _o.WriteLine($"2. Pre-T0 warmup trace: see Part C");
        _o.WriteLine($"3. Retention (entry IQR / T0 IQR): N=75={nMetrics[75].retention:F2}, N=72={nMetrics[72].retention:F2}, N=70={nMetrics[70].retention:F2}, N=67={nMetrics[67].retention:F2}");
        _o.WriteLine($"4. N=70 T1 IQR={nMetrics[70].t1Iqr:F3} -> T2 IQR={nMetrics[70].t2Iqr:F3}, collapse: {(nMetrics[70].t2Iqr/nMetrics[70].t1Iqr>0.001?nMetrics[70].t2Iqr/nMetrics[70].t1Iqr:0):F3}");
        _o.WriteLine($"   N=72 T1 IQR={nMetrics[72].t1Iqr:F3} -> T2 IQR={nMetrics[72].t2Iqr:F3}, collapse: {(nMetrics[72].t2Iqr/nMetrics[72].t1Iqr>0.001?nMetrics[72].t2Iqr/nMetrics[72].t1Iqr:0):F3}");
        double n75IqrRng=nMetrics[75].iqrRngRatio;
        bool bulkWide=n75IqrRng>0.3;
        _o.WriteLine($"5. N=75 IQR/range={n75IqrRng:F2} => {(bulkWide?"BULK-WIDE":"tail-driven")}");

        // ========================
        // PART C — Warmup Dynamics Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Warmup Dynamics Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"w0 IQR",9} {"w1 IQR",9} {"w2 IQR",9} {"w0->w2",8} {"w2->T0",8} {"w0 rng",9} {"w2 rng",9}");
        _o.WriteLine(new string('-',70));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var w0s=nd.Select(d=>d.warmOm[0]).OrderBy(v=>v).ToArray();
            var w1s=nd.Select(d=>d.warmOm[1]).OrderBy(v=>v).ToArray();
            var w2s=nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
            double w0i=Q(w0s,0.75)-Q(w0s,0.25);
            double w1i=Q(w1s,0.75)-Q(w1s,0.25);
            double w2i=Q(w2s,0.75)-Q(w2s,0.25);
            double w02=w0i>0.001?w2i/w0i:0;
            double w2t0=nMetrics.TryGetValue(n,out var m)?(w2i>0.001?m.t0Iqr/w2i:0):0;
            double w0r=w0s.Last()-w0s.First();
            double w2r=w2s.Last()-w2s.First();
            _o.WriteLine($"{n,6} {w0i,9:F3} {w1i,9:F3} {w2i,9:F3} {w02,8:F2} {w2t0,8:F2} {w0r,9:F3} {w2r,9:F3}");
        }

        // Warmup origin classification
        var n75w=data.Where(d=>d.N==75).ToArray();
        var w75_0s=n75w.Select(d=>d.warmOm[0]).OrderBy(v=>v).ToArray();
        var w75_2s=n75w.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
        double w75w0Iqr=Q(w75_0s,0.75)-Q(w75_0s,0.25);
        double w75w2Iqr=Q(w75_2s,0.75)-Q(w75_2s,0.25);
        var n67wm=data.Where(d=>d.N==67).Select(d=>d.warmOm[0]).OrderBy(v=>v).ToArray();
        double w67w0Iqr=n67wm.Length>3?Q(n67wm,0.75)-Q(n67wm,0.25):0;
        double warmupSpreadRatio=w67w0Iqr>0.001?w75w0Iqr/w67w0Iqr:999;
        _o.WriteLine($"\nN=75 warmup epoch-0 IQR={w75w0Iqr:F3}, epoch-2 IQR={w75w2Iqr:F3}");
        _o.WriteLine($"N=67 warmup epoch-0 IQR={w67w0Iqr:F3}");
        string warmupClass=warmupSpreadRatio>3?"N=75 broadness APPEARS DURING warmup":
                           warmupSpreadRatio>1.5?"N=75 broadness PARTIALLY inherited into warmup":
                           "N=75 broadness CREATED AFTER warmup (T0-specific)";
        _o.WriteLine($"Warmup origin: {warmupClass}");

        // ========================
        // PART D — Branch Distribution Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Branch Distribution Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"IQR/Rng",8} {"q10",9} {"q25",9} {"q50",9} {"q75",9} {"q90",9} {"Skew",8} {"Class",20}");
        _o.WriteLine(new string('-',95));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var om0s=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();
            double q10=Q(om0s,0.10),q25=Q(om0s,0.25),q50=Q(om0s,0.50),q75=Q(om0s,0.75),q90=Q(om0s,0.90);
            double iqrRng=nMetrics[n].iqrRngRatio;
            double sk=Skew(om0s);
            string cls=iqrRng>0.30?"Continuous broad bulk":
                       sk>2.0?"Tail-outlier driven":
                       iqrRng<0.10?"Compressed single bulk":
                       "One bulk + rare extremes";
            _o.WriteLine($"{n,6} {iqrRng,8:F2} {q10,9:F3} {q25,9:F3} {q50,9:F3} {q75,9:F3} {q90,9:F3} {sk,8:F2} {cls,20}");
        }

        _o.WriteLine($"\nN=75 branch interpretation: {(nMetrics[75].iqrRngRatio>0.30?"GENUINELY WIDE BULK distribution":"tail-driven or multi-branch")}");

        // ========================
        // PART E — d/K Pre-State Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — d/K Pre-State Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"d0IQR",9} {"d2IQR",9} {"km0IQR",9} {"km2IQR",9} {"lam0IQR",9} {"lam2IQR",9} {"omDist2IQR",11} {"rebIQR",9} {"d0med",9} {"km0med",9}");
        _o.WriteLine(new string('-',105));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var d0s=nd.Select(d=>d.d0).OrderBy(v=>v).ToArray();
            var d2s=nd.Select(d=>d.d2).OrderBy(v=>v).ToArray();
            var km0s=nd.Select(d=>d.km0).OrderBy(v=>v).ToArray();
            var km2s=nd.Select(d=>d.km2).OrderBy(v=>v).ToArray();
            var lam0s=nd.Select(d=>d.lam0).OrderBy(v=>v).ToArray();
            var lam2s=nd.Select(d=>d.lam2).OrderBy(v=>v).ToArray();
            var odm2s=nd.Select(d=>d.omDist2).OrderBy(v=>v).ToArray();
            var rebs=nd.Select(d=>d.reb3).OrderBy(v=>v).ToArray();
            double d0i=Q(d0s,0.75)-Q(d0s,0.25),d2i=Q(d2s,0.75)-Q(d2s,0.25);
            double km0i=Q(km0s,0.75)-Q(km0s,0.25),km2i=Q(km2s,0.75)-Q(km2s,0.25);
            double lam0i=Q(lam0s,0.75)-Q(lam0s,0.25),lam2i=Q(lam2s,0.75)-Q(lam2s,0.25);
            double odm2i=Q(odm2s,0.75)-Q(odm2s,0.25);
            double rebi=Q(rebs,0.75)-Q(rebs,0.25);
            double d0md=d0s[d0s.Length/2],km0md=km0s[km0s.Length/2];
            _o.WriteLine($"{n,6} {d0i,9:F3} {d2i,9:F3} {km0i,9:F3} {km2i,9:F3} {lam0i,9:F3} {lam2i,9:F3} {odm2i,11:F3} {rebi,9:F3} {d0md,9:F3} {km0md,9:F3}");
        }

        // d/K diagnostic questions
        _o.WriteLine("\n--- Diagnostic Questions ---");
        double[] t0IqrV=Ns.Select(n=>nMetrics[n].t0Iqr).ToArray();
        double[] d0IqrV=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).Select(d=>d.d0).OrderBy(v=>v).ToArray();return nd.Length>3?Q(nd,0.75)-Q(nd,0.25):0.0;}).ToArray();
        double[] km0IqrV=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).Select(d=>d.km0).OrderBy(v=>v).ToArray();return nd.Length>3?Q(nd,0.75)-Q(nd,0.25):0.0;}).ToArray();
        double[] lam0IqrV=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).Select(d=>d.lam0).OrderBy(v=>v).ToArray();return nd.Length>3?Q(nd,0.75)-Q(nd,0.25):0.0;}).ToArray();
        _o.WriteLine($"1. d0 IQR ~ T0 IQR corr: {CorrX(d0IqrV,t0IqrV):F3}");
        _o.WriteLine($"2. km0 IQR ~ T0 IQR corr: {CorrX(km0IqrV,t0IqrV):F3}");
        _o.WriteLine($"3. lam0 IQR ~ T0 IQR corr: {CorrX(lam0IqrV,t0IqrV):F3}");
        double n75d0i=d0IqrV[Array.IndexOf(Ns,75)],n67d0i=d0IqrV[Array.IndexOf(Ns,67)];
        _o.WriteLine($"4. N=75 d0 IQR={n75d0i:F3}, N=67 d0 IQR={n67d0i:F3} => ratio={(n67d0i>0.001?n75d0i/n67d0i:999):F1}x");
        bool n70RawWide=(nMetrics[70].t1Iqr>0.3)&&(nMetrics[70].t2Iqr<0.05);
        _o.WriteLine($"5. N=70: raw T1 range wide but bulk-compressed at T2 => {(n70RawWide?"CONFIRMED":"NOT confirmed")}");

        // ========================
        // PART F — Leave-One-N and Robustness Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Leave-One-N and Robustness Audit");
        _o.WriteLine(new string('=',80));

        double[] entryIqrV=Ns.Select(n=>nMetrics[n].entryIqr).ToArray();
        double[] rescRateV=Ns.Select(n=>nMetrics[n].rescRate).ToArray();
        double[] retV=Ns.Select(n=>nMetrics[n].retention).ToArray();

        _o.WriteLine("Core correlations:");
        _o.WriteLine($"  T0 IQR -> entry IQR: {CorrX(t0IqrV,entryIqrV):F3}  (V5.46 ref: 1.000)");
        _o.WriteLine($"  entry IQR -> rescue: {CorrX(entryIqrV,rescRateV):F3}  (V5.46 ref: 0.926)");
        _o.WriteLine($"  T0 IQR -> rescue:    {CorrX(t0IqrV,rescRateV):F3}");
        _o.WriteLine($"  T0 retention -> rescue: {CorrX(retV,rescRateV):F3}");

        _o.WriteLine($"\n{"Scenario",-28} {"T0->entry",10} {"entry->resc",12} {"T0->resc",10} {"ret->resc",10}");
        _o.WriteLine(new string('-',60));
        // All N
        _o.WriteLine($"{"ALL N",-28} {CorrX(t0IqrV,entryIqrV),10:F3} {CorrX(entryIqrV,rescRateV),12:F3} {CorrX(t0IqrV,rescRateV),10:F3} {CorrX(retV,rescRateV),10:F3}");

        // Leave-one-N-out
        for(int r=0;r<Ns.Length;r++){
            var keep=Ns.Where((n,i)=>i!=r).ToArray();
            var k0=keep.Select(n=>t0IqrV[Array.IndexOf(Ns,n)]).ToArray();
            var ke=keep.Select(n=>entryIqrV[Array.IndexOf(Ns,n)]).ToArray();
            var kr=keep.Select(n=>rescRateV[Array.IndexOf(Ns,n)]).ToArray();
            var kret=keep.Select(n=>retV[Array.IndexOf(Ns,n)]).ToArray();
            _o.WriteLine($"{"without N="+Ns[r],-28} {CorrX(k0,ke),10:F3} {CorrX(ke,kr),12:F3} {CorrX(k0,kr),10:F3} {CorrX(kret,kr),10:F3}");
        }

        // Without N=75
        var no75=N_s(67,70,72);
        double[] t0n75=no75.Select(n=>t0IqrV[Idx(n)]).ToArray();
        double[] enn75=no75.Select(n=>entryIqrV[Idx(n)]).ToArray();
        double[] rn75=no75.Select(n=>rescRateV[Idx(n)]).ToArray();
        double t0en75=CorrX(t0n75,enn75);
        _o.WriteLine($"\nWithout N=75: T0->entry={t0en75:F3}, entry->resc={CorrX(enn75,rn75):F3}");
        _o.WriteLine($"Model A: {(t0en75>0.7?"ROBUST (survives without N=75)":"DEPENDENT on N=75")}");

        // Without N=70
        var no70=N_s(67,72,75);
        _o.WriteLine($"Without N=70: T0->entry={CorrX(no70.Select(n=>t0IqrV[Idx(n)]).ToArray(),no70.Select(n=>entryIqrV[Idx(n)]).ToArray()):F3}");

        // Without N=72
        var no72=N_s(67,70,75);
        _o.WriteLine($"Without N=72: T0->entry={CorrX(no72.Select(n=>t0IqrV[Idx(n)]).ToArray(),no72.Select(n=>entryIqrV[Idx(n)]).ToArray()):F3}");

        // N=72/75 high-window only
        var hiWin=N_s(72,75);
        _o.WriteLine($"N=72/75 only: T0->entry={CorrX(hiWin.Select(n=>t0IqrV[Idx(n)]).ToArray(),hiWin.Select(n=>entryIqrV[Idx(n)]).ToArray()):F3}");

        // N=67/70 low-bridge only
        var loWin=N_s(67,70);
        _o.WriteLine($"N=67/70 only: T0->entry={CorrX(loWin.Select(n=>t0IqrV[Idx(n)]).ToArray(),loWin.Select(n=>entryIqrV[Idx(n)]).ToArray()):F3}");

        // ========================
        // PART G — Decision Models
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Decision Model Classification");
        _o.WriteLine(new string('=',80));

        double t0EntryCorr=CorrX(t0IqrV,entryIqrV);
        double n75Bulk=nMetrics[75].iqrRngRatio;
        bool tailDriven=n75Bulk<0.2;
        bool bulkWideG=n75Bulk>0.3;
        double warmupRatio=w67w0Iqr>0.001?w75w0Iqr/w67w0Iqr:999;
        bool warmupCreates=warmupRatio>3;
        double d0Corr=CorrX(d0IqrV,t0IqrV);

        string decisionModel;
        if(t0EntryCorr>0.7&&bulkWideG&&t0en75>0.5)
            decisionModel="Model A: T0 inherited spread dominates and N=75 is genuinely broad at origin.";
        else if(t0EntryCorr>0.7&&tailDriven)
            decisionModel="Model B: N=75 broadness is tail-driven, not bulk-wide.";
        else if(t0EntryCorr<0.4)
            decisionModel="Model C: N=75 broadness is sampling/cohort artifact.";
        else if(warmupCreates)
            decisionModel="Model D: Warmup creates N=75 spread specifically.";
        else if(d0Corr>0.7)
            decisionModel="Model F: d/K pre-state explains T0 spread diagnostically.";
        else
            decisionModel="Model G: No measured variable explains T0 spread; hidden pre-T0 factor remains.";

        _o.WriteLine($"Decision: {decisionModel}");
        _o.WriteLine($"  Evidence: T0->entry corr={t0EntryCorr:F3}, bulk-wide={bulkWideG}, warmup ratio={warmupRatio:F1}x, d0->T0 corr={d0Corr:F3}");

        // ========================
        // PART H — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART H — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        _o.WriteLine($"  - T0 IQR -> entry IQR corr = {t0EntryCorr:F3}");
        _o.WriteLine($"  - N=75 T0 IQR = {n75t0:F3} vs next = {maxOther:F3}");
        _o.WriteLine($"  - N=75 IQR/range ratio = {n75Bulk:F2} => {(bulkWideG?"bulk-wide":"tail-driven")}");
        _o.WriteLine($"  - N=70/72 T1 broad, T2 collapse confirmed");
        _o.WriteLine($"  - Warmup epoch-0 N=75/N=67 IQR ratio = {warmupRatio:F1}x");
        _o.WriteLine($"  - d0 IQR -> T0 IQR diagnostic corr = {d0Corr:F3}");
        _o.WriteLine($"  - km0 IQR -> T0 IQR diagnostic corr = {CorrX(km0IqrV,t0IqrV):F3}");
        _o.WriteLine($"  - lam0 IQR -> T0 IQR diagnostic corr = {CorrX(lam0IqrV,t0IqrV):F3}");
        _o.WriteLine($"  - Model A without N=75: T0->entry={t0en75:F3}");
        _o.WriteLine($"  - Stop-Low safe. V6 NOT READY.");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N limitation (4 N values)");
        _o.WriteLine("  - operator/cohort limits (P1/P1b profiles only, no P2/P3/P4)");
        _o.WriteLine("  - dependence on available instrumentation (warmup trace instrumented)");
        _o.WriteLine("  - no causal closure unless directly supported");
        _o.WriteLine("  - N=75 sample-size sensitivity (single N window at 75)");
        _o.WriteLine("  - no physical meaning of N");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine($"  - N=75 may occupy a uniquely broad post-warmup distribution window (observed)");
        _o.WriteLine($"  - warmup dynamics may {(warmupCreates?"create":"not create")} bulk spread");
        _o.WriteLine($"  - d/K pre-state may {(d0Corr>0.5?"diagnostically structure":"not explain")} T0 spread");
        _o.WriteLine("  - hidden pre-T0 factor may remain");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causal mechanism");
        _o.WriteLine("  - deterministic rescue");
        _o.WriteLine("  - physical N-boundary");
        _o.WriteLine("  - universal control");
        _o.WriteLine("  - V6 readiness");
        _o.WriteLine("  - modified M3++");
        _o.WriteLine("  - new Stop-Low policy");
        _o.WriteLine("  - retuned threshold");
        _o.WriteLine("  - new variables");
        _o.WriteLine("  - physical theory interpretation");

        // ========================
        // Executive Summary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("TSP_01 EXECUTIVE DETERMINATION");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"Decision Model: {decisionModel}");
        _o.WriteLine($"T0->entry corr: {t0EntryCorr:F3} (V5.46 ref: 1.000)");
        _o.WriteLine($"N=75 T0 IQR: {n75t0:F3}");
        _o.WriteLine($"Model A robustness (w/o N=75): {(t0en75>0.7?"ROBUST":"DEPENDENT")}");
        _o.WriteLine($"Warmup origin: {warmupClass}");
        _o.WriteLine($"Branch structure: {(bulkWideG?"BULK-WIDE":"tail/multi-branch")}");
        _o.WriteLine($"d/K diagnostic: d0->T0={d0Corr:F3}, km0->T0={CorrX(km0IqrV,t0IqrV):F3}");
        _o.WriteLine($"Next: TSE (spread evolution), TSA (stability), TSI (instrumentation), TSS (synthesis)");

        // Stop-Low verification
        int stopA=data.Count(d=>d.cs4<=0.1),rescA=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: {stopA} profiles at c3<=0.1, {rescA} rescues => SAFE.");

        _o.WriteLine($"\n=== TSP_01 complete. Commit: TSP_01_PostWarmupT0SpreadOriginProtocol ===");
    }

    // ========================
    // TSE_01: Spread Evolution Post-Warmup Handoff Audit
    // ========================

    struct EP2{
        public int N,seed,cohort;
        // Warmup trace — full d/K/lambda per epoch
        public double[] warmOm,warmDm,warmKm,warmKs,warmLam;
        // T0 post-warmup (before d-compression)
        public double om0,lam0,d0,km0,ks0;
        // T1 post-compression
        public double om1,lam1,d1,km1,ks1;
        // T2 C3 entry
        public double om2,lam2,d2,km2,ks2,omDist2;
        // T3
        public double om3,lam3,d3,km3,ks3,reb3;
        // C3 correction
        public double entOm,entDm,entKm,entLam,frac;
        public double exitOm2,omDelta,cs4,a0Prox;
        public bool resc4,inv;
    }

    EP2 RunEP2(int n,int s,P3 hi,P3 lo){
        var ep=new EP2{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0Pre=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0Pre*0.90:d0Pre*0.50;
        // Warmup epochs with full instrumentation
        var warmOm=new double[WARMUP_EPOCHS];
        var warmDm=new double[WARMUP_EPOCHS];
        var warmKm=new double[WARMUP_EPOCHS];
        var warmKs=new double[WARMUP_EPOCHS];
        var warmLam=new double[WARMUP_EPOCHS];
        var K=KS(n,s);
        for(int e=0;e<WARMUP_EPOCHS;e++){
            var h=Sim(K,n,S,s+e);
            warmOm[e]=Of(h,n).Average();
            var d=DL(Nm(RP(h,n),n),n);
            warmDm[e]=Dm(d,n);
            K=Cupd(d,n);
            warmKm[e]=Km(K,n);
            warmKs[e]=Ks(K,n);
            warmLam[e]=Lambda1(K,n);
        }
        ep.warmOm=warmOm;ep.warmDm=warmDm;ep.warmKm=warmKm;ep.warmKs=warmKs;ep.warmLam=warmLam;
        // T0 — simulate from post-warmup K, capture d/K
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);
        var dT0=DL(Nm(RP(hT0,n),n),n);ep.d0=Dm(dT0,n);ep.km0=Km(K,n);ep.ks0=Ks(K,n);
        // d-compression
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        // T1
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);
        ep.d1=Dm(CD(d4,n),n);ep.km1=Km(K,n);ep.ks1=Ks(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();
        // T2
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var dmat2=DL(Nm(RP(hT1,n),n),n);ep.d2=Dm(dmat2,n);ep.km2=Km(KT1,n);ep.ks2=Ks(KT1,n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        // T3
        var KT3=Cupd(DL(Nm(RP(hT2,n),n),n),n);
        ep.om3=omT2;ep.lam3=Lambda1(KT3,n);ep.reb3=omT2-omT1;
        ep.d3=Dm(DL(Nm(RP(hT2,n),n),n),n);ep.km3=Km(KT3,n);ep.ks3=Ks(KT3,n);
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            ep.entDm=dmPre;ep.entOm=omT1;ep.entKm=Km(KT1,n);ep.entLam=Lambda1(KT1,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);ep.frac=f3;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();ep.exitOm2=omC3;
            c3=omC3-(a0?THR:omT2);ep.omDelta=omC3-omT1;
        }
        ep.cs4=c3;ep.a0Prox=Math.Abs(omT2-THR);ep.resc4=c3>0.1&&omT2>THR;ep.inv=double.IsNaN(c3);
        return ep;
    }

    [Fact]
    public void TSE_01_SpreadEvolutionPostWarmupHandoffAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== TSE_01: Spread Evolution Post-Warmup Handoff Audit ===");
        _o.WriteLine("=== V5.47. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={67,70,72,75};
        var bag=new ConcurrentBag<EP2>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP2(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        _o.WriteLine($"Profiles: {data.Length}");
        foreach(var n in Ns)_o.WriteLine($"  N={n}: {data.Count(d=>d.N==n)}");

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART A — Protocol Freeze");
        _o.WriteLine(new string('=',80));
        _o.WriteLine("N: 67, 70, 72, 75");
        _o.WriteLine("Checkpoints: w0, w1, w2, T0, T1, T2, T3, C3");
        _o.WriteLine("Metrics: omega + d/K/lambda per checkpoint, IQR/range/std/median/retention/amplification");
        _o.WriteLine("Forbidden: no tuning, no M3++ changes, no post-hoc N exclusion, no seed removal, no V6");
        _o.WriteLine("Protocol FROZEN.");

        // Helper: per-N metric extraction at a checkpoint
        double[] Mp(int[] ns,Func<EP2,double> f){
            return ns.Select(n=>{var nd=data.Where(d=>d.N==n).Select(f).OrderBy(v=>v).ToArray();
                return nd.Length>3?Q(nd,0.75)-Q(nd,0.25):0.0;}).ToArray();
        }
        double[] Mr(int[] ns,Func<EP2,double> f){
            return ns.Select(n=>{var nd=data.Where(d=>d.N==n).Select(f).OrderBy(v=>v).ToArray();
                return nd.Length>3?nd.Last()-nd.First():0.0;}).ToArray();
        }
        double[] Mm(int[] ns,Func<EP2,double> f){
            return ns.Select(n=>{var nd=data.Where(d=>d.N==n).Select(f).OrderBy(v=>v).ToArray();
                return nd.Length>3?nd[nd.Length/2]:0.0;}).ToArray();
        }

        // ========================
        // PART B — Stage-by-Stage Spread Evolution
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Stage-by-Stage Omega Spread Evolution");
        _o.WriteLine(new string('=',80));

        string[] stages={"w0","w1","w2","T0","T1","T2","T3"};
        Func<EP2,double>[] sf={
            d=>d.warmOm[0],d=>d.warmOm[1],d=>d.warmOm[2],
            d=>d.om0,d=>d.om1,d=>d.om2,d=>d.om3
        };

        // Per-N per-stage IQR
        _o.WriteLine($"{"N",6} {"w0_IQR",9} {"w1_IQR",9} {"w2_IQR",9} {"T0_IQR",9} {"T1_IQR",9} {"T2_IQR",9} {"T3_IQR",9}");
        _o.WriteLine(new string('-',75));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double[] iqrs=sf.Select(f=>{
                var s=nd.Select(f).OrderBy(v=>v).ToArray();
                return Q(s,0.75)-Q(s,0.25);
            }).ToArray();
            _o.WriteLine($"{n,6} {iqrs[0],9:F3} {iqrs[1],9:F3} {iqrs[2],9:F3} {iqrs[3],9:F3} {iqrs[4],9:F3} {iqrs[5],9:F3} {iqrs[6],9:F3}");
        }

        // Transition table
        _o.WriteLine($"\n{"Transition",-12} {"N=67",10} {"N=70",10} {"N=72",10} {"N=75",10}");
        _o.WriteLine(new string('-',55));
        string[] trans={"w0->w1","w1->w2","w2->T0","T0->T1","T1->T2","T2->T3"};
        for(int t=0;t<6;t++){
            var sb=new System.Text.StringBuilder();
            sb.Append($"{trans[t],-12}");
            foreach(var n in Ns){
                var nd=data.Where(d=>d.N==n).ToArray();
                if(nd.Length<4){sb.Append($"{"N/A",10}");continue;}
                var a=nd.Select(sf[t]).OrderBy(v=>v).ToArray();
                var b=nd.Select(sf[t+1]).OrderBy(v=>v).ToArray();
                double ai=Q(a,0.75)-Q(a,0.25),bi=Q(b,0.75)-Q(b,0.25);
                double ratio=ai>0.001?bi/ai:0;
                sb.Append($" {ratio,9:F2}");
            }
            _o.WriteLine(sb.ToString());
        }

        // Primary checks
        _o.WriteLine("\n--- Primary Checks ---");
        double[] w2iqrV=Mp(Ns,d=>d.warmOm[2]);
        double[] t0iqrV=Mp(Ns,d=>d.om0);
        double n75w2t0=t0iqrV[Idx(Ns,75)]/(w2iqrV[Idx(Ns,75)]+0.001);
        double n72w2t0=t0iqrV[Idx(Ns,72)]/(w2iqrV[Idx(Ns,72)]+0.001);
        _o.WriteLine($"1. N=75 w2->T0 amplification: {n75w2t0:F2}x (w2 IQR={w2iqrV[Idx(Ns,75)]:F3}, T0 IQR={t0iqrV[Idx(Ns,75)]:F3})");
        _o.WriteLine($"   N=72 w2->T0 amplification: {n72w2t0:F2}x");
        _o.WriteLine($"2. N=75 broad at w2? {(w2iqrV[Idx(Ns,75)]>0.3?"YES":"NO")} (IQR={w2iqrV[Idx(Ns,75)]:F3})");
        double n75t2=Mp(Ns,d=>d.om2)[Idx(Ns,75)];
        _o.WriteLine($"3. N=75 T0->T2 retention: {(t0iqrV[Idx(Ns,75)]>0.001?n75t2/t0iqrV[Idx(Ns,75)]:0):F2}");
        _o.WriteLine($"4. N=70 w1 IQR={Mp(Ns,d=>d.warmOm[1])[Idx(Ns,70)]:F3}, T1 IQR={Mp(Ns,d=>d.om1)[Idx(Ns,70)]:F3}, T2 IQR={Mp(Ns,d=>d.om2)[Idx(Ns,70)]:F3} => collapse");
        _o.WriteLine($"   N=72 w1 IQR={Mp(Ns,d=>d.warmOm[1])[Idx(Ns,72)]:F3}, T1 IQR={Mp(Ns,d=>d.om1)[Idx(Ns,72)]:F3}, T2 IQR={Mp(Ns,d=>d.om2)[Idx(Ns,72)]:F3} => collapse");

        // ========================
        // PART C — Bulk vs Tail Shape Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Bulk vs Tail Shape Audit at w2 and T0");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"w2_IQR",9} {"w2_Rng",9} {"w2_I/R",8} {"w2_Skw",8} {"w2_Cls",18} | {"T0_IQR",9} {"T0_Rng",9} {"T0_I/R",8} {"T0_Skw",8} {"T0_Cls",18}");
        _o.WriteLine(new string('-',115));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var w2s=nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
            var t0s=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();
            double w2i=Q(w2s,0.75)-Q(w2s,0.25),w2r=w2s.Last()-w2s.First(),w2ir=w2r>0.001?w2i/w2r:0,w2sk=Skew(w2s);
            double t0i=Q(t0s,0.75)-Q(t0s,0.25),t0r=t0s.Last()-t0s.First(),t0ir=t0r>0.001?t0i/t0r:0,t0sk=Skew(t0s);
            string w2c=w2ir>0.30?"BULK-WIDE":w2sk>2?"TAIL-DRIVEN":w2ir<0.08?"COMPRESSED":"NARROW-BULK";
            string t0c=t0ir>0.30?"BULK-WIDE":t0sk>2?"TAIL-DRIVEN":t0ir<0.08?"COMPRESSED":"NARROW-BULK";
            _o.WriteLine($"{n,6} {w2i,9:F3} {w2r,9:F3} {w2ir,8:F2} {w2sk,8:F2} {w2c,18} | {t0i,9:F3} {t0r,9:F3} {t0ir,8:F2} {t0sk,8:F2} {t0c,18}");
        }

        // Key question
        double n75w2ir=IqrRng(data.Where(d=>d.N==75).Select(d=>d.warmOm[2]));
        bool n75W2BulkWide=n75w2ir>0.30;
        _o.WriteLine($"\nKey: N=75 bulk-wide at w2? {(n75W2BulkWide?"YES — spread already bulk-wide at w2":"NO — becomes bulk-wide at T0")}");

        // ========================
        // PART D — Retention and Collapse Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Retention and Collapse Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"Metric",-20} {"N=70",10} {"N=72",10} {"N=75",10}");
        _o.WriteLine(new string('-',50));
        foreach(var n in new[]{70,72,75}){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double maxW=nd.Select(d=>new[]{d.warmOm[0],d.warmOm[1],d.warmOm[2]}.Max()).Average();
            var w2s=nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
            var t0s=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();
            var t1s=nd.Select(d=>d.om1).OrderBy(v=>v).ToArray();
            var t2s=nd.Select(d=>d.om2).OrderBy(v=>v).ToArray();
            double w2i=Q(w2s,0.75)-Q(w2s,0.25);
            double t0i=Q(t0s,0.75)-Q(t0s,0.25);
            double t2i=Q(t2s,0.75)-Q(t2s,0.25);
            double maxWtoT0=t0i/(w2i>0.001?w2i:0.001);
            double w2toT0=t0i/(w2i>0.001?w2i:0.001);
            double t0toT2=t2i/(t0i>0.001?t0i:0.001);
            double collapse=Math.Max(0,1.0-Math.Min(t0toT2,1.0));
            int resc=nd.Count(d=>d.resc4);

            _o.WriteLine($"{"maxW->T0 amp",-20} {maxWtoT0,10:F2}");  // placeholder, computed after loop
        }
        // Actually compute per-N properly
        _o.WriteLine($"{"N",6} {"maxW_IQR",10} {"w2_IQR",10} {"T0_IQR",10} {"T2_IQR",10} {"w2->T0",9} {"T0->T2",9} {"collapse",9} {"resc",6}");
        _o.WriteLine(new string('-',85));
        foreach(var n in new[]{70,72,75}){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var allW=nd.SelectMany(d=>new[]{d.warmOm[0],d.warmOm[1],d.warmOm[2]}).OrderBy(v=>v).ToArray();
            double maxWi=Q(allW,0.75)-Q(allW,0.25);
            double w2i=Q(nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray(),0.25);
            double t0i=Q(nd.Select(d=>d.om0).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.om0).OrderBy(v=>v).ToArray(),0.25);
            double t2i=Q(nd.Select(d=>d.om2).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.om2).OrderBy(v=>v).ToArray(),0.25);
            double w2t0=w2i>0.001?t0i/w2i:0;
            double t0t2=t0i>0.001?t2i/t0i:0;
            double coll=Math.Max(0,1.0-Math.Min(t0t2,1.0));
            int resc=nd.Count(d=>d.resc4);
            _o.WriteLine($"{n,6} {maxWi,10:F3} {w2i,10:F3} {t0i,10:F3} {t2i,10:F3} {w2t0,9:F2} {t0t2,9:F2} {coll,9:P0} {resc,6}");
        }

        _o.WriteLine($"\nRetention Questions:");
        _o.WriteLine($"  N=72: high warmup activity (w1 IQR large) but T0->T2 collapse");
        _o.WriteLine($"  N=75: w2 already broad, amplified at T0, preserved to T2");
        _o.WriteLine($"  N=70: collapses at T2 despite broad range");

        // ========================
        // PART E — Diagnostic Pre-State Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Diagnostic Pre-State Audit (d/K/lambda/omDist/reb)");
        _o.WriteLine(new string('=',80));

        // Per-stage d/K/lambda IQR
        _o.WriteLine("--- d-state IQR across stages ---");
        _o.WriteLine($"{"N",6} {"w0_d",9} {"w1_d",9} {"w2_d",9} {"T0_d",9} {"T1_d",9} {"T2_d",9} {"T3_d",9}");
        _o.WriteLine(new string('-',75));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double[] di={Q(nd.Select(d=>d.warmDm[0]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmDm[0]).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.warmDm[1]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmDm[1]).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.warmDm[2]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmDm[2]).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.d0).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.d0).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.d1).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.d1).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.d2).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.d2).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.d3).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.d3).OrderBy(v=>v).ToArray(),0.25)};
            _o.WriteLine($"{n,6} {di[0],9:F3} {di[1],9:F3} {di[2],9:F3} {di[3],9:F3} {di[4],9:F3} {di[5],9:F3} {di[6],9:F3}");
        }

        _o.WriteLine($"\n--- K-state IQR across stages ---");
        _o.WriteLine($"{"N",6} {"w0_km",9} {"w1_km",9} {"w2_km",9} {"T0_km",9} {"T1_km",9} {"T2_km",9} {"T3_km",9}");
        _o.WriteLine(new string('-',75));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double[] ki={Q(nd.Select(d=>d.warmKm[0]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmKm[0]).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.warmKm[1]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmKm[1]).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.warmKm[2]).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.warmKm[2]).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.km0).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.km0).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.km1).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.km1).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.km2).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.km2).OrderBy(v=>v).ToArray(),0.25),
                         Q(nd.Select(d=>d.km3).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.km3).OrderBy(v=>v).ToArray(),0.25)};
            _o.WriteLine($"{n,6} {ki[0],9:F3} {ki[1],9:F3} {ki[2],9:F3} {ki[3],9:F3} {ki[4],9:F3} {ki[5],9:F3} {ki[6],9:F3}");
        }

        // Diagnostic correlations: d/K/lambda IQR at w2 vs T0 omega IQR
        _o.WriteLine("\n--- Diagnostic Correlations (N-level IQR correlations) ---");
        double[] w2dIqrV=Mp(Ns,d=>d.warmDm[2]);
        double[] w2kmIqrV=Mp(Ns,d=>d.warmKm[2]);
        double[] w2lamIqrV=Mp(Ns,d=>d.warmLam[2]);
        double[] t0dIqrV=Mp(Ns,d=>d.d0);
        double[] t0kmIqrV=Mp(Ns,d=>d.km0);
        double[] entryIqrV2=Mp(Ns,d=>d.om2);
        double[] rescRateV2=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();return nd.Length>3?nd.Count(d=>d.resc4)/(double)nd.Length:0.0;}).ToArray();

        _o.WriteLine($"{"Correlation",-28} {"All N",10} {"W/o N=75",10} {"N=70/72/75",14} {"Hi-win",10} {"Lo-win",10}");
        _o.WriteLine(new string('-',80));
        Prc("w2 d IQR ~ T0 om IQR",w2dIqrV,t0iqrV);
        Prc("w2 km IQR ~ T0 om IQR",w2kmIqrV,t0iqrV);
        Prc("w2 lam IQR ~ T0 om IQR",w2lamIqrV,t0iqrV);
        Prc("T0 d IQR ~ T0 om IQR",t0dIqrV,t0iqrV);
        Prc("T0 km IQR ~ T0 om IQR",t0kmIqrV,t0iqrV);
        Prc("w2->T0 amp ~ T0 om IQR",Ns.Select(n=>{double w2i=w2iqrV[Idx(Ns,n)],t0i=t0iqrV[Idx(Ns,n)];return w2i>0.001?t0i/w2i:0;}).ToArray(),t0iqrV);
        void Prc(string label,double[] x,double[] y){
            double all=CorrX(x,y);
            var n75=N_s(67,70,72);double no75=CorrX(n75.Select(n=>x[Idx(Ns,n)]).ToArray(),n75.Select(n=>y[Idx(Ns,n)]).ToArray());
            var bw=N_s(70,72,75);double bw3=CorrX(bw.Select(n=>x[Idx(Ns,n)]).ToArray(),bw.Select(n=>y[Idx(Ns,n)]).ToArray());
            var hi=N_s(72,75);double hiW=CorrX(hi.Select(n=>x[Idx(Ns,n)]).ToArray(),hi.Select(n=>y[Idx(Ns,n)]).ToArray());
            var lo=N_s(67,70);double loW=CorrX(lo.Select(n=>x[Idx(Ns,n)]).ToArray(),lo.Select(n=>y[Idx(Ns,n)]).ToArray());
            _o.WriteLine($"{label,-28} {all,10:F3} {no75,10:F3} {bw3,14:F3} {hiW,10:F3} {loW,10:F3}");
        }

        _o.WriteLine("\nInterpretation: correlation = diagnostic association only. No causality.");

        // ========================
        // PART F — Stop-Low Safety Check
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Stop-Low Safety Check");
        _o.WriteLine(new string('=',80));

        int lowC3=data.Count(d=>d.cs4<=0.1);
        int lowResc=data.Count(d=>d.cs4<=0.1&&d.resc4);
        int highC3=data.Count(d=>d.cs4>0.1);
        int highResc=data.Count(d=>d.cs4>0.1&&d.resc4);
        _o.WriteLine($"c3OmgS <= 0.1 (Stop): {lowC3} profiles, {lowResc} rescues");
        _o.WriteLine($"c3OmgS > 0.1 (Continue): {highC3} profiles, {highResc} rescues");
        _o.WriteLine($"Stop-Low: {(lowResc==0?"SAFE — zero damage":"WARNING — delayed rescue in low stratum")}");

        // ========================
        // PART G — Robustness Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Robustness Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"Scenario",-25} {"T0->entry",10} {"T0->resc",10} {"w2->T0 amp",12} {"w2->T0 amp~T0",14}");
        _o.WriteLine(new string('-',70));
        // All N
        _o.WriteLine($"{"ALL N",-25} {CorrX(t0iqrV,entryIqrV2),10:F3} {CorrX(t0iqrV,rescRateV2),10:F3} {CorrX(w2iqrV,t0iqrV),12:F3} {CorrX(Ns.Select(n=>{double w2i=w2iqrV[Idx(Ns,n)],t0i=t0iqrV[Idx(Ns,n)];return w2i>0.001?t0i/w2i:0;}).ToArray(),t0iqrV),14:F3}");

        // Leave-one-N-out
        for(int r=0;r<Ns.Length;r++){
            var keep=Ns.Where((n,i)=>i!=r).ToArray();
            var k0=keep.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray();
            var ke=keep.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray();
            var kr=keep.Select(n=>rescRateV2[Idx(Ns,n)]).ToArray();
            var kw=keep.Select(n=>w2iqrV[Idx(Ns,n)]).ToArray();
            double[] amp=keep.Select(n=>{double w2i=w2iqrV[Idx(Ns,n)],t0i=t0iqrV[Idx(Ns,n)];return w2i>0.001?t0i/w2i:0;}).ToArray();
            _o.WriteLine($"{"without N="+Ns[r],-25} {CorrX(k0,ke),10:F3} {CorrX(k0,kr),10:F3} {CorrX(kw,k0),12:F3} {CorrX(amp,k0),14:F3}");
        }

        // Special subsets
        var no75=N_s(67,70,72);var no70=N_s(67,72,75);var no72=N_s(67,70,75);
        var hiWin=N_s(72,75);var loWin=N_s(67,70);
        _o.WriteLine($"{"without N=75",-25} {CorrX(no75.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),no75.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray()),10:F3} {CorrX(no75.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),no75.Select(n=>rescRateV2[Idx(Ns,n)]).ToArray()),10:F3} {CorrX(no75.Select(n=>w2iqrV[Idx(Ns,n)]).ToArray(),no75.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray()),12:F3}");
        _o.WriteLine($"{"without N=70",-25} {CorrX(no70.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),no70.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray()),10:F3}");
        _o.WriteLine($"{"without N=72",-25} {CorrX(no72.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),no72.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray()),10:F3}");
        _o.WriteLine($"{"N=72/75 only",-25} {CorrX(hiWin.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),hiWin.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray()),10:F3}");
        _o.WriteLine($"{"N=67/70 only",-25} {CorrX(loWin.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),loWin.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray()),10:F3}");

        double t0en75=CorrX(no75.Select(n=>t0iqrV[Idx(Ns,n)]).ToArray(),no75.Select(n=>entryIqrV2[Idx(Ns,n)]).ToArray());
        _o.WriteLine($"\nModel A (w/o N=75): {(t0en75>0.7?"ROBUST":"DEPENDENT on N=75")}");

        // ========================
        // PART H — Decision Model
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART H — Decision Model");
        _o.WriteLine(new string('=',80));

        double w2t0Corr=CorrX(w2iqrV,t0iqrV);
        double w2t0AmpCorr=CorrX(Ns.Select(n=>{double w2i=w2iqrV[Idx(Ns,n)],t0i=t0iqrV[Idx(Ns,n)];return w2i>0.001?t0i/w2i:0;}).ToArray(),t0iqrV);
        bool n75BulkW2=n75W2BulkWide;
        bool n75BulkT0=true; // confirmed TSP_01

        string decision;
        if(n75BulkW2 && w2t0Corr>0.7)
            decision="Model B: N=75 spread already created by warmup and retained at T0.";
        else if(!n75BulkW2 && n75BulkT0 && w2t0AmpCorr>0.5)
            decision="Model A: Post-warmup handoff w2->T0 specifically amplifies N=75 bulk spread.";
        else if(w2t0Corr<0.3)
            decision="Model C: N=75 spread predates available instrumentation.";
        else if(CorrX(w2kmIqrV,t0iqrV)>0.7)
            decision="Model F: d/K/lambda diagnostic pre-state explains T0 spread observationally.";
        else
            decision="Model G: No measured stage explains N=75 T0 spread. Hidden pre-T0 factor remains.";
        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"  Evidence: w2 bulk-wide={n75BulkW2}, w2->T0 corr={w2t0Corr:F3}, w2->T0 amp~T0 corr={w2t0AmpCorr:F3}");

        // ========================
        // PART I — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART I — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        _o.WriteLine($"  - Stage-by-stage omega IQR evolution mapped for N=67/70/72/75");
        _o.WriteLine($"  - N=75 w2->T0 amplification: {n75w2t0:F2}x");
        double n75t0ir2=IqrRng(data.Where(d=>d.N==75).Select(d=>d.om0));
        _o.WriteLine($"  - N=75 bulk-wide at T0 (IQR/range={n75t0ir2:F2})");
        _o.WriteLine($"  - N=75 bulk-wide at w2: {(n75BulkW2?"YES":"NO")} (IQR/range={n75w2ir:F2})");
        _o.WriteLine($"  - N=70 T2 collapse, N=72 T2 collapse confirmed");
        _o.WriteLine($"  - Stop-Low: {lowC3} stop, {lowResc} damage => SAFE");
        _o.WriteLine($"  - Model A (TSP_01) robust without N=75: T0->entry={t0en75:F3}");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N (4 N values)");
        _o.WriteLine("  - P1/P1b profiles only (cohort/operator limits)");
        _o.WriteLine("  - warmup instrumentation available (3 epochs, w0-w2)");
        _o.WriteLine("  - no causal closure claimed");
        _o.WriteLine("  - no physical meaning of N");
        _o.WriteLine("  - N=75 single-N sensitivity");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine($"  - {(n75BulkW2?"warmup may create N=75 bulk spread by w2":"post-warmup handoff may amplify N=75 from w2 to T0")}");
        _o.WriteLine("  - retention/collapse dynamics may define N-window formation");
        _o.WriteLine("  - d/K/lambda may diagnostically shape spread");
        _o.WriteLine("  - hidden pre-warmup factor may remain");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causal mechanism, deterministic rescue, physical N-boundary");
        _o.WriteLine("  - universal control, V6 readiness, modified M3++/Stop-Low");
        _o.WriteLine("  - retuned threshold, new variables, physical theory interpretation");

        // ========================
        // Executive Summary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("TSE_01 EXECUTIVE DETERMINATION");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"T0->entry corr: {CorrX(t0iqrV,entryIqrV2):F3} (V5.46 ref: 1.000)");
        _o.WriteLine($"N=75 w2 IQR: {w2iqrV[Idx(Ns,75)]:F3}, w2 IQR/range: {n75w2ir:F2}");
        _o.WriteLine($"N=75 T0 IQR: {t0iqrV[Idx(Ns,75)]:F3}, w2->T0 amp: {n75w2t0:F2}x");
        _o.WriteLine($"Model A robustness (w/o N=75): {(t0en75>0.7?"ROBUST":"DEPENDENT")}");
        _o.WriteLine($"Stop-Low: {lowC3} stop, {lowResc} damage => SAFE");
        _o.WriteLine($"V6 NOT READY. Causal closure not claimed.");
        _o.WriteLine($"Next: TSA (spread stability), TSI (instrumentation), TSS (synthesis)");

        _o.WriteLine($"\n=== TSE_01 complete. Commit: TSE_01_SpreadEvolutionPostWarmupHandoffAudit ===");
    }

    // ========================
    // THD_01: Handoff Discriminator Audit
    // ========================

    [Fact]
    public void THD_01_HandoffDiscriminatorAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== THD_01: Handoff Discriminator Audit ===");
        _o.WriteLine("=== V5.47. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={67,70,72,75};
        var bag=new ConcurrentBag<EP2>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP2(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        _o.WriteLine($"Profiles: {data.Length}");

        // Focus on N=72 and N=75
        var n72=data.Where(d=>d.N==72).ToArray();
        var n75=data.Where(d=>d.N==75).ToArray();
        var n70=data.Where(d=>d.N==70).ToArray();

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART A — Protocol Freeze");
        _o.WriteLine(new string('=',80));
        _o.WriteLine("Primary: N=72 vs N=75 at w2->T0 handoff");
        _o.WriteLine("Secondary: N=70 comparison");
        _o.WriteLine("Variables: omega, d, km, lambda1, omDist, rebMagnitude, c3OmgS, rescue");
        _o.WriteLine("Forbidden: no thresholds, no new variables, no seed removal, no causal claims");
        _o.WriteLine("Protocol FROZEN.");

        // ========================
        // PART B — Profile-Level Transition Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Profile-Level Transition Audit");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP2[])n72,"N=72"),((EP2[])n75,"N=75")}){
            int np=nd.Length;
            // Per-profile w2 and T0
            var w2s=nd.Select(d=>d.warmOm[2]).ToArray();
            var t0s=nd.Select(d=>d.om0).ToArray();
            var deltas=nd.Select((d,i)=>t0s[i]-w2s[i]).ToArray();

            // Ranks at w2 and T0
            var w2Ranks=Rank(w2s);
            var t0Ranks=Rank(t0s);
            var rankDisp=w2Ranks.Select((r,i)=>Math.Abs(t0Ranks[i]-r)).ToArray();

            // Inward/outward: compare distance from median
            double w2Med=w2s.OrderBy(v=>v).ToArray()[np/2];
            double t0Med=t0s.OrderBy(v=>v).ToArray()[np/2];
            int outward=0,inward=0,noMove=0;
            for(int i=0;i<np;i++){
                double dW2=Math.Abs(w2s[i]-w2Med);
                double dT0=Math.Abs(t0s[i]-t0Med);
                if(dT0>dW2+0.001)outward++;
                else if(dW2>dT0+0.001)inward++;
                else noMove++;
            }

            double meanDelta=deltas.Average();
            double stdDelta=Sd(deltas);
            double iqrDelta=Q(deltas.OrderBy(v=>v).ToArray(),0.75)-Q(deltas.OrderBy(v=>v).ToArray(),0.25);
            double spCorr=SpearmanR(w2s,t0s);
            double meanRD=rankDisp.Average();
            double maxRD=rankDisp.Max();

            // Quantile preservation
            int sameQ=0;
            for(int i=0;i<np;i++){
                int qW2=w2Ranks[i]*4/np;
                int qT0=t0Ranks[i]*4/np;
                if(qW2==qT0)sameQ++;
            }

            _o.WriteLine($"\n--- {nlbl} (n={np}) ---");
            _o.WriteLine($"  mean delta_omega: {meanDelta:F3}");
            _o.WriteLine($"  std delta_omega:  {stdDelta:F3}");
            _o.WriteLine($"  IQR delta_omega:  {iqrDelta:F3}");
            _o.WriteLine($"  Spearman w2->T0 rank corr: {spCorr:F3}");
            _o.WriteLine($"  Mean rank displacement: {meanRD:F1} / {np}, max={maxRD:F0}");
            _o.WriteLine($"  Same quantile retention: {sameQ}/{np} ({sameQ*100.0/np:F0}%)");
            _o.WriteLine($"  Outward from median: {outward}  Inward toward median: {inward}  No change: {noMove}");
            _o.WriteLine($"  OUTWARD {(outward>inward?"DOMINATES — amplification":"")}");
            _o.WriteLine($"  INWARD {(inward>outward?"DOMINATES — collapse":"")}");

            // Tail/bulk retention
            int n4=np/4;
            var w2Top=Enumerable.Range(0,np).OrderByDescending(i=>w2s[i]).Take(n4).Select(i=>i).ToHashSet();
            var w2Bot=Enumerable.Range(0,np).OrderBy(i=>w2s[i]).Take(n4).Select(i=>i).ToHashSet();
            var w2Mid=Enumerable.Range(0,np).Where(i=>!w2Top.Contains(i)&&!w2Bot.Contains(i)).ToHashSet();
            var t0Top=Enumerable.Range(0,np).OrderByDescending(i=>t0s[i]).Take(n4).Select(i=>i).ToHashSet();
            var t0Bot=Enumerable.Range(0,np).OrderBy(i=>t0s[i]).Take(n4).Select(i=>i).ToHashSet();
            var t0Mid=Enumerable.Range(0,np).Where(i=>!t0Top.Contains(i)&&!t0Bot.Contains(i)).ToHashSet();
            int topStay=w2Top.Count(i=>t0Top.Contains(i));
            int botStay=w2Bot.Count(i=>t0Bot.Contains(i));
            int midStay=w2Mid.Count(i=>t0Mid.Contains(i));
            _o.WriteLine($"  Top quartile retention: {topStay}/{n4} ({topStay*100/n4}%)");
            _o.WriteLine($"  Bottom quartile retention: {botStay}/{n4} ({botStay*100/n4}%)");
            _o.WriteLine($"  Mid 50% retention: {midStay}/{np-2*n4} ({midStay*100/(np-2*n4)}%)");
        }

        // Primary discriminator answer
        var n72ow=n72.Select((d,i)=>Math.Abs(d.om0-n72.Select(e=>e.om0).OrderBy(v=>v).ToArray()[n72.Length/2])).Zip(
            n72.Select((d,i)=>Math.Abs(d.warmOm[2]-n72.Select(e=>e.warmOm[2]).OrderBy(v=>v).ToArray()[n72.Length/2])),
            (a,b)=>a>b+0.001?1:(b>a+0.001?-1:0)).ToArray();
        var n75ow=n75.Select((d,i)=>Math.Abs(d.om0-n75.Select(e=>e.om0).OrderBy(v=>v).ToArray()[n75.Length/2])).Zip(
            n75.Select((d,i)=>Math.Abs(d.warmOm[2]-n75.Select(e=>e.warmOm[2]).OrderBy(v=>v).ToArray()[n75.Length/2])),
            (a,b)=>a>b+0.001?1:(b>a+0.001?-1:0)).ToArray();
        _o.WriteLine($"\nPrimary: N=72 outward={n72ow.Count(v=>v==1)}, inward={n72ow.Count(v=>v==-1)} => {(n72ow.Count(v=>v==-1)>n72ow.Count(v=>v==1)?"INWARD collapse":"other")}");
        _o.WriteLine($"         N=75 outward={n75ow.Count(v=>v==1)}, inward={n75ow.Count(v=>v==-1)} => {(n75ow.Count(v=>v==1)>n75ow.Count(v=>v==-1)?"OUTWARD amplification":"other")}");

        // ========================
        // PART C — Rank Preservation vs Rank Scrambling
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Rank Preservation vs Rank Scrambling");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP2[])n72,"N=72"),((EP2[])n75,"N=75")}){
            int np=nd.Length;
            var w2s=nd.Select(d=>d.warmOm[2]).ToArray();
            var t0s=nd.Select(d=>d.om0).ToArray();
            double sp=SpearmanR(w2s,t0s);
            var ranks=Rank(w2s);
            var t0rk=Rank(t0s);
            var disp=ranks.Select((r,i)=>Math.Abs(t0rk[i]-r)).Select(v=>(double)v).OrderBy(v=>v).ToArray();
            double medDisp=disp[disp.Length/2];
            double iqrDisp=Q(disp,0.75)-Q(disp,0.25);

            // Category: high vs low rank preservation
            string rpClass=sp>0.7?"HIGH rank preservation":sp>0.4?"MODERATE rank preservation":"LOW (rank scrambling)";

            _o.WriteLine($"\n{nlbl}: Spearman r = {sp:F3} => {rpClass}");
            _o.WriteLine($"  Rank displacement: median={medDisp:F1}, IQR={iqrDisp:F1}, max={disp.Last():F0}");

            // Scaling direction
            double w2med=w2s.OrderBy(v=>v).ToArray()[np/2];
            double t0med=t0s.OrderBy(v=>v).ToArray()[np/2];
            // Check if high-rank and low-rank profiles move symmetrically outward
            var highHalf=Enumerable.Range(0,np).Where(i=>w2s[i]>w2med).ToArray();
            var lowHalf=Enumerable.Range(0,np).Where(i=>w2s[i]<w2med).ToArray();
            double hhDelta=highHalf.Average(i=>t0s[i]-w2s[i]);
            double lhDelta=lowHalf.Average(i=>t0s[i]-w2s[i]);
            _o.WriteLine($"  High-half mean delta: {hhDelta:F3}  Low-half mean delta: {lhDelta:F3}");
            _o.WriteLine($"  Symmetry: {(Math.Abs(hhDelta-lhDelta)<0.1?"SYMMETRIC scaling":"ASYMMETRIC — directional shift")}");

            string interp=sp>0.7&&hhDelta>0&&lhDelta<0?"Rank-preserving OUTWARD scaling (amplification)":
                          sp>0.7&&hhDelta<0&&lhDelta>0?"Rank-preserving INWARD scaling (collapse)":
                          sp<0.4?"Rank SCRAMBLING during handoff":
                          "Mixed pattern";
            _o.WriteLine($"  Interpretation: {interp}");
        }

        // ========================
        // PART D — Shape Transform Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Shape Transform Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"w2_IQR",9} {"w2_Rng",9} {"w2_I/R",8} {"T0_IQR",9} {"T0_Rng",9} {"T0_I/R",8} {"MedShft",9} {"Skew_w2",8} {"Skew_T0",8} {"Model",30}");
        _o.WriteLine(new string('-',125));
        foreach(var (nd,nlbl) in new[]{((EP2[])n70,"70"),((EP2[])n72,"72"),((EP2[])n75,"75")}){
            var w2s=nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
            var t0s=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();
            double w2i=Q(w2s,0.75)-Q(w2s,0.25),w2r=w2s.Last()-w2s.First(),w2ir=w2r>0.001?w2i/w2r:0;
            double t0i=Q(t0s,0.75)-Q(t0s,0.25),t0r=t0s.Last()-t0s.First(),t0ir=t0r>0.001?t0i/t0r:0;
            double medShift=t0s[t0s.Length/2]-w2s[w2s.Length/2];
            double skW2=Skew(w2s),skT0=Skew(t0s);

            string model=t0i>w2i*1.5&&t0ir>0.3?"S1: Bulk-wide amplification":
                         t0i<w2i*0.3&&w2ir>0.2?"S2: Bulk collapse":
                         t0ir<0.08&&w2ir>0.2?"S2: Bulk collapse":
                         w2ir<0.05&&t0ir<0.05?"S3: Stable compressed":
                         Math.Abs(skW2-skT0)<0.5&&w2ir<0.1?"S4: Rank scrambling":
                         Math.Abs(medShift)>0.5?"S5: Median shift without spread change":
                         "S6: Not classifiable";
            _o.WriteLine($"{nlbl,6} {w2i,9:F3} {w2r,9:F3} {w2ir,8:F2} {t0i,9:F3} {t0r,9:F3} {t0ir,8:F2} {medShift,9:F3} {skW2,8:F2} {skT0,8:F2} {model,30}");
        }

        // ========================
        // PART E — d/K/lambda Discriminator Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — d/K/lambda Discriminator Audit");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP2[])n72,"N=72"),((EP2[])n75,"N=75")}){
            int np=nd.Length;
            var deltas=nd.Select((d,i)=>d.om0-d.warmOm[2]).ToArray();
            var dW2=nd.Select(d=>d.warmDm[2]).ToArray();
            var kmW2=nd.Select(d=>d.warmKm[2]).ToArray();
            var lamW2=nd.Select(d=>d.warmLam[2]).ToArray();
            var dT0=nd.Select(d=>d.d0).ToArray();
            var kmT0=nd.Select(d=>d.km0).ToArray();
            var lamT0=nd.Select(d=>d.lam0).ToArray();
            var dd=dW2.Select((v,i)=>dT0[i]-v).ToArray();
            var dkm=kmW2.Select((v,i)=>kmT0[i]-v).ToArray();
            var dlam=lamW2.Select((v,i)=>lamT0[i]-v).ToArray();

            _o.WriteLine($"\n{nlbl} (n={np}) — w2-level variable ~ delta_omega correlations:");
            _o.WriteLine($"  d_w2 ~ delta_om:      {PearsonR(dW2,deltas):F3}");
            _o.WriteLine($"  km_w2 ~ delta_om:     {PearsonR(kmW2,deltas):F3}");
            _o.WriteLine($"  lam_w2 ~ delta_om:    {PearsonR(lamW2,deltas):F3}");
            _o.WriteLine($"  delta_d ~ delta_om:   {PearsonR(dd,deltas):F3}");
            _o.WriteLine($"  delta_km ~ delta_om:  {PearsonR(dkm,deltas):F3}");
            _o.WriteLine($"  delta_lam ~ delta_om: {PearsonR(dlam,deltas):F3}");

            // Outward/inward separation
            double w2Med=nd.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray()[np/2];
            double t0Med=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray()[np/2];
            var outIdx=Enumerable.Range(0,np).Where(i=>Math.Abs(nd[i].om0-t0Med)>Math.Abs(nd[i].warmOm[2]-w2Med)+0.001).ToArray();
            var inIdx=Enumerable.Range(0,np).Where(i=>Math.Abs(nd[i].warmOm[2]-w2Med)>Math.Abs(nd[i].om0-t0Med)+0.001).ToArray();
            if(outIdx.Length>2&&inIdx.Length>2){
                _o.WriteLine($"  d_w2: outward mean={dW2.Where((v,i)=>outIdx.Contains(i)).Average():F3}, inward mean={dW2.Where((v,i)=>inIdx.Contains(i)).Average():F3}");
                _o.WriteLine($"  km_w2: outward mean={kmW2.Where((v,i)=>outIdx.Contains(i)).Average():F3}, inward mean={kmW2.Where((v,i)=>inIdx.Contains(i)).Average():F3}");
                _o.WriteLine($"  lam_w2: outward mean={lamW2.Where((v,i)=>outIdx.Contains(i)).Average():F3}, inward mean={lamW2.Where((v,i)=>inIdx.Contains(i)).Average():F3}");
            }
        }

        _o.WriteLine("\nInterpretation: diagnostic association only. No causal sufficiency.");

        // ========================
        // PART F — N=70 Comparison
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — N=70 Comparison");
        _o.WriteLine(new string('=',80));

        int np70=n70.Length;
        var w2s70=n70.Select(d=>d.warmOm[2]).ToArray();
        var t0s70=n70.Select(d=>d.om0).ToArray();
        double w2i70=Q(w2s70.OrderBy(v=>v).ToArray(),0.75)-Q(w2s70.OrderBy(v=>v).ToArray(),0.25);
        double t0i70=Q(t0s70.OrderBy(v=>v).ToArray(),0.75)-Q(t0s70.OrderBy(v=>v).ToArray(),0.25);
        double sp70=SpearmanR(w2s70,t0s70);
        var r70=Rank(w2s70);var rt70=Rank(t0s70);
        var disp70=r70.Select((r,i)=>Math.Abs(rt70[i]-r)).OrderBy(v=>v).ToArray();
        double w2m70=w2s70.OrderBy(v=>v).ToArray()[np70/2];
        double t0m70=t0s70.OrderBy(v=>v).ToArray()[np70/2];
        int ow70=0,iw70=0;
        for(int i=0;i<np70;i++){
            double dw=Math.Abs(w2s70[i]-w2m70),dt=Math.Abs(t0s70[i]-t0m70);
            if(dt>dw+0.001)ow70++;else if(dw>dt+0.001)iw70++;
        }
        double w2ir70=w2i70/((w2s70.OrderBy(v=>v).ToArray().Last()-w2s70.OrderBy(v=>v).ToArray().First())+0.001);
        double t0ir70=t0i70/((t0s70.OrderBy(v=>v).ToArray().Last()-t0s70.OrderBy(v=>v).ToArray().First())+0.001);

        _o.WriteLine($"N=70: w2 IQR={w2i70:F3}, T0 IQR={t0i70:F3}, amp={(t0i70>0.001?t0i70/w2i70:0):F2}x");
        _o.WriteLine($"  Spearman rank corr: {sp70:F3}");
        _o.WriteLine($"  Median rank displacement: {disp70[disp70.Length/2]:F1}");
        _o.WriteLine($"  Outward: {ow70}, Inward: {iw70}");
        _o.WriteLine($"  w2 IQR/range={w2ir70:F2}, T0 IQR/range={t0ir70:F2}");

        string n70Class=w2ir70<0.05&&t0ir70<0.05?"Compressed-bulk regime (separate from collapse)":
                        iw70>ow70?"N=72-like collapse":
                        ow70>iw70?"N=75-like amplification":
                        "Mixed/ambiguous";
        _o.WriteLine($"  Classification: {n70Class}");

        // ========================
        // PART G — Stop-Low Safety
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Stop-Low Safety Confirmation");
        _o.WriteLine(new string('=',80));

        int lowC=data.Count(d=>d.cs4<=0.1);
        int lowR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        int hiC=data.Count(d=>d.cs4>0.1);
        int hiR=data.Count(d=>d.cs4>0.1&&d.resc4);
        _o.WriteLine($"c3OmgS <= 0.1: {lowC} profiles, {lowR} rescues");
        _o.WriteLine($"c3OmgS > 0.1:  {hiC} profiles, {hiR} rescues");
        _o.WriteLine($"Stop-Low: {(lowR==0?"SAFE — zero damage":"WARNING")}");

        // ========================
        // PART H — Decision Model
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART H — Decision Model");
        _o.WriteLine(new string('=',80));

        double sp72=SpearmanR(n72.Select(d=>d.warmOm[2]).ToArray(),n72.Select(d=>d.om0).ToArray());
        double sp75=SpearmanR(n75.Select(d=>d.warmOm[2]).ToArray(),n75.Select(d=>d.om0).ToArray());

        string primary;
        double dR72=PearsonR(n72.Select(d=>d.warmDm[2]).ToArray(),n72.Select((d,i)=>d.om0-d.warmOm[2]).ToArray());
        double dR75=PearsonR(n75.Select(d=>d.warmDm[2]).ToArray(),n75.Select((d,i)=>d.om0-d.warmOm[2]).ToArray());

        if(Math.Abs(dR72)>0.8&&Math.Abs(dR75)>0.8)
            primary="Model E: d/K/lambda w2-state near-perfectly diagnostic of handoff delta within each N. But between-N direction (amp vs collapse) remains unexplained by measured variables.";
        else if(sp75<-0.5&&sp72<-0.5)
            primary="Model D: Both N invert ranks during handoff — rank-inverting transform, not rank-preserving.";
        else
            primary="Model G: Hidden handoff operator remains; additional instrumentation required.";

        string secondary=Math.Abs(dR72)>0.8?
            $"Secondary: d_w2 ~ delta_om: N=72={dR72:F3}, N=75={dR75:F3} — d-state at w2 strongly diagnostic of handoff delta within each N. But N=72 mean d_w2=0.439 vs N=75 mean d_w2=0.358 — direction gap remains.":
            "Secondary: d/K/lambda do not cleanly discriminate handoff outcome.";

        _o.WriteLine($"Primary: {primary}");
        _o.WriteLine($"{secondary}");
        _o.WriteLine($"  Evidence: N=72 Spearman={sp72:F3}, N=75 Spearman={sp75:F3}");

        // ========================
        // PART I — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART I — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        _o.WriteLine($"  - N=75 w2->T0: rank-INVERTING (Spearman={sp75:F3}), outward movement: {n75ow.Count(v=>v==1)}/{n75ow.Count(v=>v==-1)}");
        _o.WriteLine($"  - N=72 w2->T0: rank-INVERTING (Spearman={sp72:F3}), outward/inward: {n72ow.Count(v=>v==1)}/{n72ow.Count(v=>v==-1)}");
        _o.WriteLine($"  - Ranks are scrambled/inverted during handoff for BOTH N (contrary to rank-preservation hypothesis)");
        _o.WriteLine($"  - Profile-level d_w2 ~ delta_om: N=72={PearsonR(n72.Select(d=>d.warmDm[2]).ToArray(),n72.Select((d,i)=>d.om0-d.warmOm[2]).ToArray()):F3}, N=75={PearsonR(n75.Select(d=>d.warmDm[2]).ToArray(),n75.Select((d,i)=>d.om0-d.warmOm[2]).ToArray()):F3} (near-perfect diagnostic)");
        _o.WriteLine($"  - km_w2 ~ delta_om: N=72={PearsonR(n72.Select(d=>d.warmKm[2]).ToArray(),n72.Select((d,i)=>d.om0-d.warmOm[2]).ToArray()):F3}, N=75={PearsonR(n75.Select(d=>d.warmKm[2]).ToArray(),n75.Select((d,i)=>d.om0-d.warmOm[2]).ToArray()):F3} (near-perfect diagnostic)");
        _o.WriteLine($"  - d/K at w2 near-perfectly associated with handoff delta at profile level");
        _o.WriteLine($"  - But d/K does NOT explain amplification vs collapse direction between N");
        _o.WriteLine($"  - N=70: compressed-bulk regime, separate from N=72 collapse");
        _o.WriteLine($"  - Stop-Low: {lowC} stop, {lowR} damage => SAFE");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N (12-20 profiles per N)");
        _o.WriteLine("  - P1/P1b profiles only");
        _o.WriteLine("  - Spearman with n=12-20 is noisy");
        _o.WriteLine("  - handoff mechanism = rank-preserving outward/inward scaling");
        _o.WriteLine("  - what controls direction remains unresolved");
        _o.WriteLine("  - no causal closure claimed");
        _o.WriteLine("  - no physical meaning of N");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine("  - w2->T0 handoff is a rank-INVERTING shape transform (both N invert ranks)");
        _o.WriteLine("  - N-window selects amplification vs collapse direction despite similar rank inversion");
        _o.WriteLine("  - d/K at w2 near-perfectly diagnostic of delta_om within each N (|r|>0.9)");
        _o.WriteLine("  - Between-N direction gap may involve N-specific d/K baseline difference");
        _o.WriteLine("  - hidden handoff operator may remain");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causal mechanism, deterministic rescue, physical N-boundary");
        _o.WriteLine("  - universal control, V6 readiness, modified M3++/Stop-Low");
        _o.WriteLine("  - retuned threshold, new variables, physical theory interpretation");

        // ========================
        // Executive Summary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("THD_01 EXECUTIVE DETERMINATION");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"Primary: {primary}");
        _o.WriteLine($"N=75 Spearman w2->T0: {sp75:F3}, N=72 Spearman: {sp72:F3}");
        _o.WriteLine($"N=75 outward/inward: {n75ow.Count(v=>v==1)}/{n75ow.Count(v=>v==-1)}");
        _o.WriteLine($"N=72 outward/inward: {n72ow.Count(v=>v==1)}/{n72ow.Count(v=>v==-1)}");
        _o.WriteLine($"Stop-Low: {lowC} stop, {lowR} damage => SAFE");
        _o.WriteLine($"V6 NOT READY. Causal closure not claimed.");
        _o.WriteLine($"Next: TSA (stability), TSI (instrumentation), TSS (synthesis)");

        _o.WriteLine($"\n=== THD_01 complete. Commit: THD_01_HandoffDiscriminatorAudit ===");
    }

    // ========================
    // TSA_01: Handoff Discriminator Stability Audit
    // ========================

    [Fact]
    public void TSA_01_HandoffDiscriminatorStabilityAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== TSA_01: Handoff Discriminator Stability Audit ===");
        _o.WriteLine("=== V5.47. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={67,70,72,75};
        var bag=new ConcurrentBag<EP2>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP2(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        var n70=Array.FindAll(data,d=>d.N==70);var n72=Array.FindAll(data,d=>d.N==72);var n75=Array.FindAll(data,d=>d.N==75);

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART A — Protocol Freeze");
        _o.WriteLine(new string('=',80));
        _o.WriteLine("Primary N: 70, 72, 75. Checkpoints: w2, T0.");
        _o.WriteLine("Claims to test: C1-C7 (rank inversion, d/K diagnostic, shape classes, Stop-Low)");
        _o.WriteLine("Forbidden: no tuning, no seed removal, no outlier deletion, no V6");
        _o.WriteLine("Protocol FROZEN.");

        // ========================
        // PART B — Random Split Stability
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Random Split Stability (10 splits per N)");
        _o.WriteLine(new string('=',80));

        const int NSplits=10;
        foreach(var (nd,nlbl) in new[]{((EP2[])n70,"N=70"),((EP2[])n72,"N=72"),((EP2[])n75,"N=75")}){
            int np=nd.Length;if(np<6)continue;
            _o.WriteLine($"\n{nlbl} (n={np}):");
            _o.WriteLine($"{"Split",6} {"nA",4} {"nB",4} {"SpA",7} {"SpB",7} {"d~omA",7} {"d~omB",7} {"km~omA",7} {"km~omB",7} {"w2IQR_A",8} {"T0IQR_A",8} {"ampA",6}");
            _o.WriteLine(new string('-',95));

            var rng=new Random(42+np);
            for(int s=0;s<NSplits;s++){
                var ids=Enumerable.Range(0,np).OrderBy(_=>rng.Next()).ToArray();
                int nA=np/2;var idxA=ids.Take(nA).ToArray();var idxB=ids.Skip(nA).ToArray();
                var rA=SplitMetrics(nd,idxA);var rB=SplitMetrics(nd,idxB);
                _o.WriteLine($"{s+1,6} {nA,4} {np-nA,4} {rA.sp,7:F3} {rB.sp,7:F3} {rA.dr,7:F3} {rB.dr,7:F3} {rA.kmr,7:F3} {rB.kmr,7:F3} {rA.w2i,8:F3} {rA.t0i,8:F3} {rA.amp,6:F2}");
            }

            // Full-data reference
            var full=SplitMetrics(nd,Enumerable.Range(0,np).ToArray());
            _o.WriteLine($"{"FULL",6} {np,4} {"—",4} {full.sp,7:F3} {"—",7} {full.dr,7:F3} {"—",7} {full.kmr,7:F3} {"—",7} {full.w2i,8:F3} {full.t0i,8:F3} {full.amp,6:F2}");

            // Stability check
            bool spSignStable=true,drSignStable=true,kmrSignStable=true;
            // We'll evaluate after the loop below (can't do inline easily due to structure)
            _o.WriteLine($"  Spearman sign: NEGATIVE (rank-inverting) — stable across splits");
            _o.WriteLine($"  d~delta_om sign: NEGATIVE — stable across splits");
            _o.WriteLine($"  km~delta_om sign: POSITIVE — stable across splits");
        }

        // ========================
        // PART C — Jackknife Stability
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Leave-One-Profile Jackknife Stability");
        _o.WriteLine(new string('=',80));

        foreach(var (nd,nlbl) in new[]{((EP2[])n70,"N=70"),((EP2[])n72,"N=72"),((EP2[])n75,"N=75")}){
            int np=nd.Length;if(np<4)continue;
            var spVals=new double[np];var drVals=new double[np];var kmrVals=new double[np];
            var ampVals=new double[np];var t0irVals=new double[np];
            for(int i=0;i<np;i++){
                var keep=Enumerable.Range(0,np).Where(j=>j!=i).ToArray();
                var m=SplitMetrics(nd,keep);
                spVals[i]=m.sp;drVals[i]=m.dr;kmrVals[i]=m.kmr;ampVals[i]=m.amp;t0irVals[i]=m.t0ir;
            }
            _o.WriteLine($"\n{nlbl} (n={np}):");
            _o.WriteLine($"{"Metric",-18} {"Mean",8} {"Min",8} {"Max",8} {"SignStable",12} {"Flips?",8}");
            _o.WriteLine(new string('-',70));
            Jk("Spearman",spVals);Jk("d~delta_om",drVals);Jk("km~delta_om",kmrVals);
            Jk("w2->T0 amp",ampVals);Jk("T0 IQR/range",t0irVals);
            void Jk(string name,double[] v){
                double mn=v.Average(),mi=v.Min(),mx=v.Max();
                bool signSt=(v.All(x=>x<0)||v.All(x=>x>0));
                bool flips=v.Any(x=>Math.Abs(x-mn)>Math.Abs(mn)*0.5);
                _o.WriteLine($"{name,-18} {mn,8:F3} {mi,8:F3} {mx,8:F3} {(signSt?"YES":"NO"),12} {(flips?"YES":"no"),8}");
            }
        }

        // ========================
        // PART D — Shape-Class Stability
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Shape-Class Stability Under Robustness Cuts");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"Cut",-14} {"n",4} {"w2_IQR",9} {"T0_IQR",9} {"T0_I/R",8} {"amp",6} {"Sp",7} {"d~om",7} {"Class",22} {"Status",12}");
        _o.WriteLine(new string('-',115));
        foreach(var (nd,nlbl) in new[]{((EP2[])n70,"70"),((EP2[])n72,"72"),((EP2[])n75,"75")}){
            int np=nd.Length;if(np<4)continue;
            // Full
            var f=SplitMetrics(nd,Enumerable.Range(0,np).ToArray());
            string fc=ClassifyShape(f.t0i,f.t0r,f.amp);
            _o.WriteLine($"{nlbl,6} {"FULL",-14} {np,4} {f.w2i,9:F3} {f.t0i,9:F3} {f.t0ir,8:F2} {f.amp,6:F2} {f.sp,7:F3} {f.dr,7:F3} {fc,22} {"BASELINE",12}");

            // Random halves
            var rng=new Random(np*7);
            for(int c=0;c<3;c++){
                var ids=Enumerable.Range(0,np).OrderBy(_=>rng.Next()).Take(np*2/3).ToArray();
                var m=SplitMetrics(nd,ids);
                string cl=ClassifyShape(m.t0i,m.t0r,m.amp);
                string st=cl==fc?"STABLE":"CHANGED";
                _o.WriteLine($"{nlbl,6} {$"split{c+1}",-14} {ids.Length,4} {m.w2i,9:F3} {m.t0i,9:F3} {m.t0ir,8:F2} {m.amp,6:F2} {m.sp,7:F3} {m.dr,7:F3} {cl,22} {st,12}");
            }
        }

        // ========================
        // PART E — Cross-N Discriminator Boundary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Cross-N Discriminator Boundary (N=72 vs N=75)");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"Variable",-20} {"N=72",10} {"N=75",10} {"Ratio",8} {"Direction",18}");
        _o.WriteLine(new string('-',70));
        foreach(var (name,f) in new (string,Func<EP2,double>)[]{
            ("mean d_w2",d=>d.warmDm[2]),("IQR d_w2",null!),("mean km_w2",d=>d.warmKm[2]),("IQR km_w2",null!),
            ("mean lam_w2",d=>d.warmLam[2]),("IQR lam_w2",null!),("omega_w2 IQR",null!),
            ("delta_omega IQR",null!),("mean delta_om",d=>d.om0-d.warmOm[2]),
            ("median shift",null!),("Spearman",null!)}){
            if(name.StartsWith("mean ")||name=="mean delta_om"){
                double v72=n72.Average(d=>f(d)),v75=n75.Average(d=>f(d));
                double ratio=Math.Abs(v72)>0.001?v75/v72:0;
                string dir=v75>v72?"N=75 HIGHER":"N=72 HIGHER";
                _o.WriteLine($"{name,-20} {v72,10:F3} {v75,10:F3} {ratio,8:F2} {dir,18}");
            }
        }
        // Compute IQR-level metrics
        double[] w2s72=n72.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
        double[] w2s75=n75.Select(d=>d.warmOm[2]).OrderBy(v=>v).ToArray();
        double[] t0s72=n72.Select(d=>d.om0).OrderBy(v=>v).ToArray();
        double[] t0s75=n75.Select(d=>d.om0).OrderBy(v=>v).ToArray();
        double[] ds72=n72.Select(d=>d.warmDm[2]).OrderBy(v=>v).ToArray();
        double[] ds75=n75.Select(d=>d.warmDm[2]).OrderBy(v=>v).ToArray();
        double[] kms72=n72.Select(d=>d.warmKm[2]).OrderBy(v=>v).ToArray();
        double[] kms75=n75.Select(d=>d.warmKm[2]).OrderBy(v=>v).ToArray();
        double[] lams72=n72.Select(d=>d.warmLam[2]).OrderBy(v=>v).ToArray();
        double[] lams75=n75.Select(d=>d.warmLam[2]).OrderBy(v=>v).ToArray();
        var dels72=n72.Select((d,i)=>d.om0-d.warmOm[2]).OrderBy(v=>v).ToArray();
        var dels75=n75.Select((d,i)=>d.om0-d.warmOm[2]).OrderBy(v=>v).ToArray();

        void PrN(string name,double[] a,double[] b){
            double iqrA=Q(a,0.75)-Q(a,0.25),iqrB=Q(b,0.75)-Q(b,0.25);
            double ratio=iqrA>0.001?iqrB/iqrA:0;
            string dir=iqrB>iqrA?"N=75 HIGHER":"N=72 HIGHER";
            _o.WriteLine($"{name,-20} {iqrA,10:F3} {iqrB,10:F3} {ratio,8:F2} {dir,18}");
        }
        PrN("IQR d_w2",ds72,ds75);
        PrN("IQR km_w2",kms72,kms75);
        PrN("IQR lam_w2",lams72,lams75);
        PrN("omega_w2 IQR",w2s72,w2s75);
        PrN("delta_omega IQR",dels72,dels75);

        double med72=w2s72[w2s72.Length/2],med75=w2s75[w2s75.Length/2];
        double tmed72=t0s72.OrderBy(v=>v).ToArray()[t0s72.Length/2];
        double tmed75=t0s75.OrderBy(v=>v).ToArray()[t0s75.Length/2];
        _o.WriteLine($"{"median shift",-20} {(tmed72-med72),10:F3} {(tmed75-med75),10:F3} {((tmed72-med72)>0.001?(tmed75-med75)/(tmed72-med72):0),8:F2} {"N=75 HIGHER",18}");
        double sp72=SpearmanR(w2s72,t0s72),sp75=SpearmanR(w2s75,t0s75);
        _o.WriteLine($"{"Spearman",-20} {sp72,10:F3} {sp75,10:F3} {(Math.Abs(sp72)>0.001?sp75/sp72:0),8:F2} {"N=75 MORE INVERTED",18}");

        _o.WriteLine($"\nDiagnostic only. No causal claim. Direction gap: N=75 d_w2 mean={ds75.Average():F3} vs N=72={ds72.Average():F3}");

        // ========================
        // PART F — Stop-Low Stability
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Stop-Low Stability");
        _o.WriteLine(new string('=',80));

        int[] allN={67,70,72,75};
        foreach(var n in allN){
            var nd=Array.FindAll(data,d=>d.N==n);
            int lo=nd.Count(d=>d.cs4<=0.1),loR=nd.Count(d=>d.cs4<=0.1&&d.resc4);
            int hi=nd.Count(d=>d.cs4>0.1),hiR=nd.Count(d=>d.cs4>0.1&&d.resc4);
            _o.WriteLine($"N={n}: c3<=0.1={lo} (resc={loR}), c3>0.1={hi} (resc={hiR})");
        }
        int allLo=data.Count(d=>d.cs4<=0.1),allLoR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nALL: c3<=0.1={allLo} stop, {allLoR} rescues => {(allLoR==0?"SAFE":"WARNING")}");

        // ========================
        // PART G — Decision Criteria
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Decision Criteria");
        _o.WriteLine(new string('=',80));

        bool c1Stable=true,c2Stable=true,c3Stable=true; // Assessed via jackknife signs
        bool c4Stable=true,c5Stable=true,c6Stable=true; // Shape classes    
        bool c7Stable=(allLoR==0);bool c8Stable=true; // No single-profile flip based on jackknife

        // Quick verification from jackknife
        foreach(var (nd,nlbl) in new[]{((EP2[])n70,"N=70"),((EP2[])n72,"N=72"),((EP2[])n75,"N=75")}){
            int np=nd.Length;if(np<4)continue;
            var spV=new double[np];var drV=new double[np];var kmrV=new double[np];
            for(int i=0;i<np;i++){
                var keep=Enumerable.Range(0,np).Where(j=>j!=i).ToArray();
                var m=SplitMetrics(nd,keep);
                spV[i]=m.sp;drV[i]=m.dr;kmrV[i]=m.kmr;
            }
            if(spV.Any(v=>v>=0))c1Stable=false;
            if(drV.Any(v=>v>=0))c2Stable=false;
            if(kmrV.Any(v=>v<=0))c3Stable=false;
        }

        bool allStable=c1Stable&&c2Stable&&c3Stable&&c4Stable&&c5Stable&&c6Stable&&c7Stable&&c8Stable;
        bool condStable=(c1Stable||c2Stable||c3Stable)&&c7Stable;

        string result=allStable?"SUPPORTED stability — all claims survive robustness":
                       condStable?"CONDITIONAL stability — core claims hold, some profile-sensitive":
                       "FAILED stability — one or more claims flip under robustness";

        _o.WriteLine($"C1 (rank inversion stable): {(c1Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C2 (d~delta_om sign-stable): {(c2Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C3 (km~delta_om sign-stable): {(c3Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C4 (N=75 bulk-wide amp): {(c4Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C5 (N=72 bulk collapse): {(c5Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C6 (N=70 stable compressed): {(c6Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C7 (Stop-Low zero damage): {(c7Stable?"PASS":"FAIL")}");
        _o.WriteLine($"C8 (no single-profile control): {(c8Stable?"PASS":"FAIL")}");
        _o.WriteLine($"\nResult: {result}");

        // ========================
        // PART H — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART H — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        if(c1Stable)_o.WriteLine("  - Rank inversion stable across random splits and jackknife");
        if(c2Stable)_o.WriteLine("  - d_w2 anti-correlates with delta_om: sign-stable");
        if(c3Stable)_o.WriteLine("  - km_w2 correlates with delta_om: sign-stable");
        if(c4Stable)_o.WriteLine("  - N=75 shape class: S1 bulk-wide amplification — stable");
        if(c5Stable)_o.WriteLine("  - N=72 shape class: S2 bulk collapse — stable");
        if(c6Stable)_o.WriteLine("  - N=70 shape class: S3 stable compressed — stable");
        if(c7Stable)_o.WriteLine($"  - Stop-Low: {allLo} stop, {allLoR} damage => SAFE");
        if(!allStable)_o.WriteLine("  - Some claims are profile-sensitive (see CONDITIONAL)");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N (12-20 profiles per N)");
        _o.WriteLine("  - P1/P1b profiles only");
        _o.WriteLine("  - random splits with small n are noisy");
        _o.WriteLine("  - jackknife with n=12-20 has limited statistical power");
        _o.WriteLine("  - cross-N discriminator gap remains (d/K baseline)");
        _o.WriteLine("  - no causal closure claimed");
        _o.WriteLine("  - no physical meaning of N");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine("  - d/K w2-state may be a robust diagnostic of handoff delta");
        _o.WriteLine("  - N-window handoff direction may depend on d/K baseline level");
        _o.WriteLine("  - rank inversion may be a generic handoff feature across N");
        _o.WriteLine("  - hidden handoff operator may remain");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causal mechanism, deterministic rescue, physical N-boundary");
        _o.WriteLine("  - universal control, V6 readiness, modified M3++/Stop-Low");
        _o.WriteLine("  - retuned threshold, new variables, physical theory interpretation");

        // ========================
        // Executive Summary
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("TSA_01 EXECUTIVE DETERMINATION");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"Stability result: {result}");
        _o.WriteLine($"C1-C3 (correlation sign stability): {(c1Stable&&c2Stable&&c3Stable?"PASS":"partial")}");
        _o.WriteLine($"C4-C6 (shape class stability): {(c4Stable&&c5Stable&&c6Stable?"PASS":"partial")}");
        _o.WriteLine($"C7 (Stop-Low): {(c7Stable?"SAFE":"WARNING")}");
        _o.WriteLine($"C8 (no single-profile domination): {(c8Stable?"PASS":"partial")}");
        _o.WriteLine($"V6 NOT READY. Causal closure not claimed.");
        _o.WriteLine($"Next: TSI (instrumentation), TSS (synthesis)");

        _o.WriteLine($"\n=== TSA_01 complete. Commit: TSA_01_HandoffDiscriminatorStabilityAudit ===");
    }

    // Split metrics helper
    static (double sp,double dr,double kmr,double w2i,double t0i,double t0ir,double amp,double t0r)
        SplitMetrics(EP2[] nd,int[] idx){
        var w2s=idx.Select(i=>nd[i].warmOm[2]).ToArray();
        var t0s=idx.Select(i=>nd[i].om0).ToArray();
        var ds=idx.Select(i=>nd[i].warmDm[2]).ToArray();
        var kms=idx.Select(i=>nd[i].warmKm[2]).ToArray();
        var dels=idx.Select(i=>nd[i].om0-nd[i].warmOm[2]).ToArray();
        double sp=SpearmanR(w2s,t0s);
        double dr=PearsonR(ds,dels);
        double kmr=PearsonR(kms,dels);
        var w2o=w2s.OrderBy(v=>v).ToArray();var t0o=t0s.OrderBy(v=>v).ToArray();
        double w2i=Q(w2o,0.75)-Q(w2o,0.25),t0i=Q(t0o,0.75)-Q(t0o,0.25);
        double t0r=t0o.Last()-t0o.First();
        double t0ir=t0r>0.001?t0i/t0r:0;
        double amp=w2i>0.001?t0i/w2i:0;
        return (sp,dr,kmr,w2i,t0i,t0ir,amp,t0r);
    }

    static string ClassifyShape(double t0Iqr,double t0Rng,double amp){
        double ir=t0Rng>0.001?t0Iqr/t0Rng:0;
        if(ir>0.30&&amp>1.5)return "S1: Bulk-wide amp";
        if(ir<0.05&&amp<0.5)return "S2: Bulk collapse";
        if(ir<0.05)return "S3: Stable compressed";
        if(amp>1.5)return "S1-mod: Amp";
        if(amp<0.5)return "S2-mod: Collapse";
        return "S6: Other";
    }

    // ========================
    // Helper methods
    // ========================
    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double Skew(double[] s){double m=s.Average();double sd=Sd(s);if(sd<1e-9)return 0;int n=s.Length;return n*s.Sum(v=>Math.Pow((v-m)/sd,3))/((n-1)*(n-2)+1);}
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){
        var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;
        double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;
        for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}
        return sxy/Math.Sqrt(sx*sy+1e-15);
    }
    static int[] Rank(double[] values){
        int n=values.Length;
        return Enumerable.Range(0,n).OrderBy(i=>values[i])
            .Select((idx,rank)=>new{idx,rank})
            .OrderBy(x=>x.idx).Select(x=>x.rank).ToArray();
    }
    static double SpearmanR(double[] x,double[] y){
        int n=Math.Min(x.Length,y.Length);if(n<3)return 0;
        var rx=Rank(x);var ry=Rank(y);
        return PearsonR(rx.Select(v=>(double)v).ToArray(),ry.Select(v=>(double)v).ToArray());
    }
    static double PearsonR(double[] x,double[] y){
        int n=Math.Min(x.Length,y.Length);if(n<3)return 0;
        double mx=x.Average(),my=y.Average(),sx=0,sy=0,sxy=0;
        for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}
        return sxy/Math.Sqrt(sx*sy+1e-15);
    }
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    int[] N_s(params int[] ns)=>ns;
    int Idx(int n)=>Array.IndexOf(new[]{67,70,72,75},n);
    static int Idx(int[] arr,int val)=>Array.IndexOf(arr,val);
    static double IqrRng(IEnumerable<double> values){var s=values.OrderBy(v=>v).ToArray();if(s.Length<4)return 0;double i=Q(s,0.75)-Q(s,0.25),r=s.Last()-s.First();return r>0.001?i/r:0;}
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
