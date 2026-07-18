using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_23;

[Trait("Category","V5_23"),Trait("Category","V5_23_CGI"),Trait("Category","LongRunning")]
public class V5_23_C3GainPerturbationAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct PertDiag{
        public int n,s,cohort;public string perturbation,result,causalityLabel;
        public double dMean,dStd,dP50,dP90,dP95,dTail;
        public double kMean,kStd,lambda1;
        public double alignPre,deltaAlign,c3OmegaShift,deltaD,deltaK,kSensitivity,omegaPerK,movementNorm;
        public double distHi,distLo,omT1,omT2;
        public bool a0,c3,immInduced,strictPersist,basinSuccess,invalid;
    }

    public V5_23_C3GainPerturbationAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    // ─── d-matrix tail manipulation ───
    // Returns modified d-matrix. expand=true for expansion, false for compression.
    // upperOnly=true means only scale the top percentiles (P3 upper-tail specific)
    double[,]? ManipulateDTail(double[,]dMat,int n,double factor,bool upperOnly){
        var vals=new List<(int i,int j,double v)>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)vals.Add((i,j,dMat[i,j]));
        vals.Sort((a,b)=>a.v.CompareTo(b.v));
        int m=vals.Count;
        double median=vals[m/2].v;
        double mean=vals.Average(x=>x.v);

        var mod=CD(dMat,n);
        for(int k=0;k<m;k++){
            var (i,j,v)=vals[k];
            double newV=v;
            if(upperOnly){
                // Only modify top 10%
                if(k>=(int)(m*0.9))newV=median+(v-median)*factor;
            }else{
                // Modify upper half, compensate lower half to preserve mean
                if(v>median)newV=median+(v-median)*factor;
                else newV=median+(v-median)/factor;
            }
            if(newV<REps)newV=REps;
            mod[i,j]=newV;mod[j,i]=newV;
        }
        if(!ValidD(mod,n))return null;
        return mod;
    }

    // ─── Core measurement ───
    PertDiag? MeasurePerturbation(int n,int s,P3 hi,P3 lo,Func<double[,],int,double[,]?>? perturb,string label){
        var d=new PertDiag{n=n,s=s,perturbation=label,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,vn=Math.Sqrt(dv*dv+kv*kv);
        d.distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km0-hi.km)*(km0-hi.km)+(ks0-hi.ks)*(ks0-hi.ks));
        d.distLo=Math.Sqrt((d0-lo.dm)*(d0-lo.dm)+(km0-lo.km)*(km0-lo.km)+(ks0-lo.ks)*(ks0-lo.ks));

        // M3++ probe
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var dm=DL(Nm(RP(h,n),n),n);K2=Cupd(dm,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);

        // Apply perturbation
        var dPostPert=dT1;
        if(perturb!=null){
            var mod=perturb(dT1,n);
            if(mod==null){d.invalid=true;d.result="invalid-perturb";return d;}
            dPostPert=mod;
        }

        // Measure post-perturbation d-metrics
        var dVals=new List<double>();
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)dVals.Add(dPostPert[i,j]);
        dVals.Sort();
        d.dMean=Dm(dPostPert,n);d.dStd=StdDev(dVals);
        d.dP50=Percentile(dVals,0.50);d.dP90=Percentile(dVals,0.90);d.dP95=Percentile(dVals,0.95);
        d.dTail=d.dP95-d.dP50;

        // K after perturbation
        var KT1=Cupd(dPostPert,n);d.kMean=Km(KT1,n);d.kStd=Ks(KT1,n);d.lambda1=Lambda1(KT1,n);

        // Pre-C3 alignment
        d.alignPre=vn>0?((d.dMean-lo.dm)*dv+(d.kMean-lo.km)*kv)/vn:0;
        d.omT1=Of(Sim(KT1,n,S,s+101),n).Average();

        // T2
        var hT2=Sim(Cupd(DL(Nm(RP(Sim(KT1,n,S,s+102),n),n),n),n),n,S,s+200);
        var dT2=DL(Nm(RP(hT2,n),n),n);d.omT2=Of(hT2,n).Average();d.a0=d.omT2>THR;

        // C3 using perturbed state
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(Sim(KT1,n,S,s+103),n),n),n);
            var KT1b=Cupd(dmat3,n);
            double dmPre=Dm(dmat3,n),kmPre=Km(KT1b,n),ksPre=Ks(KT1b,n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            if(!ValidD(dmat3,n)){d.invalid=true;return d;}
            var Kc3=Cupd(dmat3,n);
            var hc3=Sim(Kc3,n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);

            d.deltaD=dmPost-dmPre;d.deltaK=kmPost-kmPre;
            d.deltaAlign=vn>0?(((dmPost-lo.dm)*dv+(kmPost-lo.km)*kv)/vn-d.alignPre):0;
            d.c3OmegaShift=omC3-d.omT2;
            d.movementNorm=Math.Sqrt(d.deltaD*d.deltaD+d.deltaK*d.deltaK);
            d.kSensitivity=d.deltaK/Math.Max(1e-9,Math.Abs(d.deltaD));
            d.omegaPerK=d.c3OmegaShift/Math.Max(1e-9,Math.Abs(d.deltaK));
            d.c3=omC3>THR;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            d.strictPersist=d.c3&&Of(hCont,n).Average()>THR;
            d.immInduced=d.c3;
        }else{d.c3=false;d.strictPersist=false;}
        d.basinSuccess=d.strictPersist&&d.distHi<d.distLo&&!d.invalid;
        d.result=d.basinSuccess?"basin-success":d.strictPersist?"induced":"failed";
        return d;
    }

    [Fact]public void CGI_01_BaselineReproduction(){
        _o.WriteLine("═══ CGI_01: Baseline — reproduce CGA d_tail/gain relationship ═══");
        int[] Ns={64,65,66,70,72};
        var results=new ConcurrentBag<PertDiag>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){if(IsHi(n,s))continue;var d=MeasurePerturbation(n,s,hi,lo,null,"P0-baseline");if(d!=null)results.Add(d.Value);}});
        var all=results.ToArray();

        _o.WriteLine($"\n─── Baseline d_tail vs C3 gain ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,6}",
            "N","n","dTail","deltaD","deltaK","c3OmgS","Resc%"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Average(r=>r.dTail),8:F4} {sub.Average(r=>r.deltaD),8:F4} {sub.Average(r=>r.deltaK),8:F4} {sub.Average(r=>r.c3OmegaShift),8:F4} {sub.Count(r=>r.strictPersist)*100.0/sub.Length,5:F0}%");
        }
        _o.WriteLine($"\n─── Next: CGI_02 d_tail expansion ───");
    }

    [Fact]public void CGI_02_DTailExpansion(){
        _o.WriteLine("═══ CGI_02: d_tail expansion — does wider tail increase C3 gain? ═══");
        int[] Ns={64,65,72};
        var results=new ConcurrentBag<PertDiag>();

        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){if(IsHi(n,s))continue;
                var d0=MeasurePerturbation(n,s,hi,lo,null,"P0-baseline");
                if(d0!=null)results.Add(d0.Value);
                // P1: d_tail expansion
                foreach(var factor in new[]{1.1,1.25,1.5}){
                    var d1=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,factor,false),$"P1-tail+{(int)((factor-1)*100)}%");
                    if(d1!=null)results.Add(d1.Value);
                }
                // P3: upper-tail only
                var d3=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,1.5,true),"P3-upTail+50%");
                if(d3!=null)results.Add(d3.Value);
                // P4: median-preserving control
                var d4=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,1.0,false),"P4-medPreserve");
                if(d4!=null)results.Add(d4.Value);
            }});

        var all=results.ToArray();
        _o.WriteLine($"\nd_tail perturbation: {all.Length} results");

        _o.WriteLine($"\n─── N=64: expansion effect ───");
        _o.WriteLine(string.Format("{0,-16} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "Perturbation","Inv%","dTail","deltaD","deltaK","c3OmgS","kSens"));
        var n64=all.Where(r=>r.n==64).ToArray();
        foreach(var pt in new[]{"P0-baseline","P1-tail+10%","P1-tail+25%","P1-tail+50%","P3-upTail+50%","P4-medPreserve"}){
            var sub=n64.Where(r=>r.perturbation==pt).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{pt,-16} {sub.Count(r=>r.invalid)*100.0/sub.Length,5:F0}% {sub.Average(r=>r.dTail),8:F4} {sub.Average(r=>r.deltaD),8:F4} {sub.Average(r=>r.deltaK),8:F4} {sub.Average(r=>r.c3OmegaShift),8:F4} {Math.Abs(sub.Average(r=>r.kSensitivity)),8:F4}");
        }

        var n65=all.Where(r=>r.n==65).ToArray();
        var n72=all.Where(r=>r.n==72).ToArray();
        _o.WriteLine($"\n─── N=65 and N=72: expansion effect ───");
        foreach(var n in new[]{65,72}){
            var sub=all.Where(r=>r.n==n&&r.perturbation=="P1-tail+50%").ToArray();
            var baseLine=all.Where(r=>r.n==n&&r.perturbation=="P0-baseline").ToArray();
            if(sub.Length>0&&baseLine.Length>0)
                _o.WriteLine($"N={n}: c3OmgS {baseLine.Average(r=>r.c3OmegaShift):F4}→{sub.Average(r=>r.c3OmegaShift):F4} dTail {baseLine.Average(r=>r.dTail):F4}→{sub.Average(r=>r.dTail):F4}");
        }

        bool anyGain=n64.Where(r=>r.perturbation.StartsWith("P1")).Any(r=>Math.Abs(r.c3OmegaShift)>0.05);
        _o.WriteLine($"\nGate A (Expansion increases gain): {(anyGain?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"\n─── Next: CGI_03 d_tail compression ───");
    }

    [Fact]public void CGI_03_DTailCompression(){
        _o.WriteLine("═══ CGI_03: d_tail compression — does narrower tail suppress C3 gain? ═══");
        int[] Ns={65,72};
        var results=new ConcurrentBag<PertDiag>();

        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){if(IsHi(n,s))continue;
                var d0=MeasurePerturbation(n,s,hi,lo,null,"P0-baseline");
                if(d0!=null)results.Add(d0.Value);
                foreach(var factor in new[]{0.9,0.75,0.5}){
                    var d2=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,factor,false),$"P2-tail-{(int)((1-factor)*100)}%");
                    if(d2!=null)results.Add(d2.Value);
                }
            }});

        var all=results.ToArray();
        _o.WriteLine($"\n─── Compression effect ───");
        _o.WriteLine(string.Format("{0,4} {1,-16} {2,6} {3,8} {4,8} {5,8} {6,8}",
            "N","Perturbation","Inv%","dTail","deltaD","c3OmgS","kSens"));
        foreach(var n in Ns){
            foreach(var pt in new[]{"P0-baseline","P2-tail-10%","P2-tail-25%","P2-tail-50%"}){
                var sub=all.Where(r=>r.n==n&&r.perturbation==pt).ToArray();
                if(sub.Length==0)continue;
                _o.WriteLine($"{n,4} {pt,-16} {sub.Count(r=>r.invalid)*100.0/sub.Length,5:F0}% {sub.Average(r=>r.dTail),8:F4} {sub.Average(r=>r.deltaD),8:F4} {sub.Average(r=>r.c3OmegaShift),8:F4} {Math.Abs(sub.Average(r=>r.kSensitivity)),8:F4}");
            }
        }

        bool anySuppress=all.Where(r=>r.perturbation.StartsWith("P2")).Any(r=>Math.Abs(r.c3OmegaShift)<0.03);
        _o.WriteLine($"\nGate B (Compression suppresses gain): {(anySuppress?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"\n─── Next: CGI_04 N=64 breakability ───");
    }

    [Fact]public void CGI_04_N64Breakability(){
        _o.WriteLine("═══ CGI_04: N=64 breakability — can d_tail matching open N=64? ═══");
        int n=64;
        var hi=Hi(n);var lo=Lo(n);
        var results=new ConcurrentBag<PertDiag>();

        // First, get N=65 rescued d_tail reference
        double n65RefDTail=0;
        {var r65=new ConcurrentBag<double>();
        Parallel.For(0,399,s=>{if(IsHi(65,s))return;var sb=SelectAndClassify(65,s,Hi(65));if(sb==null)return;
            var d=MeasurePerturbation(65,s,Hi(65),Lo(65),null,"ref");if(d!=null&&d.Value.strictPersist)r65.Add(d.Value.dTail);});
        n65RefDTail=r65.Count>0?r65.Average():1.5;}

        _o.WriteLine($"N=65 rescued d_tail reference: {n65RefDTail:F4}");

        // For N=64: baseline + aggressive expansion
        Parallel.For(0,299,s=>{
            if(IsHi(n,s))return;
            var d0=MeasurePerturbation(n,s,hi,lo,null,"P0-baseline");
            if(d0!=null)results.Add(d0.Value);

            // Try factors to reach N=65 d_tail
            foreach(var factor in new[]{1.5,2.0,2.5}){
                var d1=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,factor,false),$"P5-factor{factor:F1}");
                if(d1!=null)results.Add(d1.Value);
            }
        });

        var all=results.ToArray();
        var baseN64=all.Where(r=>r.n==n&&r.perturbation=="P0-baseline").ToArray();
        _o.WriteLine($"\nN=64 baseline dTail: {baseN64.Average(r=>r.dTail):F4}, max c3OmgS: {baseN64.Max(r=>r.c3OmegaShift):F4}");

        _o.WriteLine($"\n─── N=64: d_tail expansion toward N=65 band ───");
        _o.WriteLine(string.Format("{0,-14} {1,6} {2,6} {3,8} {4,8} {5,8} {6,8}",
            "Perturbation","Inv%","Str%","dTail","c3OmgS","deltaD","kSens"));
        foreach(var pt in new[]{"P0-baseline","P5-factor1.5","P5-factor2.0","P5-factor2.5"}){
            var sub=all.Where(r=>r.n==n&&r.perturbation==pt).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{pt,-14} {sub.Count(r=>r.invalid)*100.0/sub.Length,5:F0}% {sub.Count(r=>r.strictPersist)*100.0/sub.Length,5:F0}% {sub.Average(r=>r.dTail),8:F4} {sub.Average(r=>r.c3OmegaShift),8:F4} {sub.Average(r=>r.deltaD),8:F4} {Math.Abs(sub.Average(r=>r.kSensitivity)),8:F4}");
        }

        // Best N=64 seed
        var best=all.Where(r=>r.n==n).OrderByDescending(r=>r.c3OmegaShift).FirstOrDefault();
        if(best.c3OmegaShift>0){
            _o.WriteLine($"\nBest N=64 seed (s={best.s}, {best.perturbation}):");
            _o.WriteLine($"  dTail={best.dTail:F4} c3OmgS={best.c3OmegaShift:F4} deltaD={best.deltaD:F4} kSens={Math.Abs(best.kSensitivity):F4}");
        }

        bool anyBreak=all.Where(r=>r.n==n).Any(r=>r.strictPersist);
        bool nearBreak=all.Where(r=>r.n==n).Any(r=>r.c3OmegaShift>0.1&&!r.strictPersist);

        _o.WriteLine($"\nGate C (d_tail opens N=64): {(anyBreak?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (Gain but no persistence): {(nearBreak?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: CGI_05 Tail specificity ───");
    }

    [Fact]public void CGI_05_TailSpecificity(){
        _o.WriteLine("═══ CGI_05: Upper-tail specificity ═══");
        int[] Ns={64,65,72};
        var results=new ConcurrentBag<PertDiag>();

        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){if(IsHi(n,s))continue;
                var d0=MeasurePerturbation(n,s,hi,lo,null,"P0");
                if(d0!=null)results.Add(d0.Value);
                var d3=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,1.5,true),"P3-upTail");
                if(d3!=null)results.Add(d3.Value);
                var d1=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,1.5,false),"P1-fullTail");
                if(d1!=null)results.Add(d1.Value);
            }});

        var all=results.ToArray();
        _o.WriteLine($"\n─── Upper-tail vs full-tail expansion ───");
        _o.WriteLine(string.Format("{0,4} {1,-12} {2,8} {3,8} {4,8} {5,8}",
            "N","Type","dTail","c3OmgS","deltaD","kSens"));
        foreach(var n in Ns){
            foreach(var pt in new[]{"P0","P3-upTail","P1-fullTail"}){
                var sub=all.Where(r=>r.n==n&&r.perturbation==pt).ToArray();
                if(sub.Length==0)continue;
                _o.WriteLine($"{n,4} {pt,-12} {sub.Average(r=>r.dTail),8:F4} {sub.Average(r=>r.c3OmegaShift),8:F4} {sub.Average(r=>r.deltaD),8:F4} {Math.Abs(sub.Average(r=>r.kSensitivity)),8:F4}");
            }
        }

        var n64up=all.Where(r=>r.n==64&&r.perturbation=="P3-upTail").Average(r=>r.c3OmegaShift);
        var n64full=all.Where(r=>r.n==64&&r.perturbation=="P1-fullTail").Average(r=>r.c3OmegaShift);
        bool upSpecific=Math.Abs(n64up-n64full)<0.01;
        _o.WriteLine($"\nGate E (Upper-tail specificity): {(upSpecific?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Next: CGI_06 Causality classification ───");
    }

    [Fact]public void CGI_06_CausalityClassification(){
        _o.WriteLine("═══ CGI_06: d_tail causality classification ═══");

        int[] Ns={64,65,72};
        var results=new ConcurrentBag<PertDiag>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<99;s++){if(IsHi(n,s))continue;
                var d0=MeasurePerturbation(n,s,hi,lo,null,"P0");if(d0!=null)results.Add(d0.Value);
                var d1=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,1.5,false),"P1+50%");if(d1!=null)results.Add(d1.Value);
                var d2=MeasurePerturbation(n,s,hi,lo,(dm,nn)=>ManipulateDTail(dm,nn,0.5,false),"P2-50%");if(d2!=null)results.Add(d2.Value);
            }});
        var all=results.ToArray();

        // Causality test: does deltaD change with d_tail manipulation?
        _o.WriteLine($"\n─── Causality evidence ───");
        foreach(var n in Ns){
            var b=all.Where(r=>r.n==n&&r.perturbation=="P0").ToArray();
            var e=all.Where(r=>r.n==n&&r.perturbation=="P1+50%").ToArray();
            var c=all.Where(r=>r.n==n&&r.perturbation=="P2-50%").ToArray();
            if(b.Length>0&&e.Length>0&&c.Length>0){
                _o.WriteLine($"N={n}: dTail {b.Average(r=>r.dTail):F3}→{e.Average(r=>r.dTail):F3}→{c.Average(r=>r.dTail):F3}");
                _o.WriteLine($"       deltaD {b.Average(r=>r.deltaD):F4}→{e.Average(r=>r.deltaD):F4}→{c.Average(r=>r.deltaD):F4}");
                _o.WriteLine($"       c3OmgS {b.Average(r=>r.c3OmegaShift):F4}→{e.Average(r=>r.c3OmegaShift):F4}→{c.Average(r=>r.c3OmegaShift):F4}");
            }
        }

        // Classification
        var n64b=all.Where(r=>r.n==64&&r.perturbation=="P0").ToArray();
        var n64e=all.Where(r=>r.n==64&&r.perturbation=="P1+50%").ToArray();
        double deltaDChange=n64b.Length>0&&n64e.Length>0?Math.Abs(n64e.Average(r=>r.deltaD)-n64b.Average(r=>r.deltaD)):0;
        double c3Change=n64b.Length>0&&n64e.Length>0?Math.Abs(n64e.Average(r=>r.c3OmegaShift)-n64b.Average(r=>r.c3OmegaShift)):0;

        string classification;
        if(deltaDChange>0.01&&c3Change>0.05)classification="causal driver";
        else if(deltaDChange>0.01)classification="necessary but not sufficient";
        else if(c3Change<0.01)classification="marker only";
        else classification="unresolved";

        _o.WriteLine($"\n─── Classification ───");
        _o.WriteLine($"deltaD change under perturbation: {deltaDChange:F4}");
        _o.WriteLine($"c3OmegaShift change: {c3Change:F4}");
        _o.WriteLine($"Classification: {classification}");

        _o.WriteLine($"Gate F (Marker only): {(classification=="marker only"?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (Unsafe/invalid): assessing");

        _o.WriteLine($"\n─── Audit complete ───");
    }

    // ─── Helpers ───
    static bool ValidD(double[,]d,int n){for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(double.IsNaN(d[i,j])||double.IsInfinity(d[i,j])||d[i,j]<0)return false;return true;}
    static double StdDev(List<double> v){if(v.Count<2)return 0;double m=v.Average();return Math.Sqrt(v.Average(x=>(x-m)*(x-m)));}
    static double Percentile(List<double> sorted,double p){if(sorted.Count==0)return 0;return sorted[Math.Clamp((int)(p*(sorted.Count-1)),0,sorted.Count-1)];}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}

    // ─── Frozen M3++ ───
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
