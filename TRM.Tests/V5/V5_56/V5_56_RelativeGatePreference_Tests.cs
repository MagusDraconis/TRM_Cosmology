using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_56;

[Trait("Category","V5_56"),Trait("Category","V5_56_RGP"),Trait("Category","LongRunning")]
public class V5_56_RelativeGatePreference_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_56_RelativeGatePreference_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void RGP_01_RelativeGatePreferenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RGP_01: Relative Gate Preference Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Why does SAC prefer lower-ranked profiles? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var seedIQRd=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
            seedIQRd.AddOrUpdate(s,Q(wo,0.75)-Q(wo,0.25),(_,v)=>v+Q(wo,0.75)-Q(wo,0.25));
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        var pipeBag=new ConcurrentBag<(int N,int seed,double riqr,double resid,double rank,double rmean,double rmed,double rstd,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                var p=allProf.FirstOrDefault(x=>x.N==n&&x.seed==s);
                pipeBag.Add((n,s,p.riqr,p.riqr-siQ.GetValueOrDefault(s,0),seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1,p.rmean,p.rmed,p.rstd,cls));
            });});
        var pd=pipeBag.ToArray();
        var ret=pd.Where(d=>d.cls=="P1"||d.cls=="P1b").ToArray();
        var rej=pd.Where(d=>d.cls=="reject").ToArray();
        _o.WriteLine($"SAC outcomes: P1={ret.Count(d=>d.cls=="P1")}, P1b={ret.Count(d=>d.cls=="P1b")}, rejected={rej.Length}");

        // ============================================================
        // PART B — Rank Preference Shape
        // ============================================================
        _o.WriteLine($"\n=== PART B: Rank Preference Shape ===");
        _o.WriteLine($"{"Rank%",-10} {"Total",6} {"P1",4} {"P1b",4} {"Rej",5} {"P1%",7} {"Ret%",7}");
        _o.WriteLine(new string('-',50));
        for(int q=0;q<4;q++){
            double lo=q/4.0,hi=(q+1)/4.0+(q==3?0.01:0);
            var qd=pd.Where(d=>d.Item3>=lo&&d.Item3<hi).ToArray();
            int qp1=qd.Count(d=>d.cls=="P1"),qp1b=qd.Count(d=>d.cls=="P1b"),qr=qd.Count(d=>d.cls=="reject");
            _o.WriteLine($"{$"{lo*100:F0}-{hi*100:F0}%",-10} {qd.Length,6} {qp1,4} {qp1b,4} {qr,5} {(qp1+qp1b>0?qp1*100.0/(qp1+qp1b):0),7:F0}% {(qp1+qp1b)*100.0/qd.Length,7:F0}%");
        }

        // Monotonicity check
        var rates=new double[4];
        for(int q=0;q<4;q++){double lo=q/4.0,hi=(q+1)/4.0+(q==3?0.01:0);var qd=pd.Where(d=>d.Item3>=lo&&d.Item3<hi).ToArray();rates[q]=qd.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/qd.Length;}
        bool monotonic=true;for(int q=1;q<4;q++)if(rates[q]>rates[q-1])monotonic=false;
        _o.WriteLine($"Retention monotonic (decreasing with rank): {(monotonic?"YES":"NO")}");

        // ============================================================
        // PART C — Boundary Audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Boundary Audit ===");
        var loQ=pd.Where(d=>d.Item3<0.25).ToArray();
        var others=pd.Where(d=>d.Item3>=0.25).ToArray();
        _o.WriteLine($"Lowest quartile (0-25%): retained={loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")}/{loQ.Length} ({loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/loQ.Length:F0}%), P1%={loQ.Count(d=>d.cls=="P1")*100.0/Math.Max(1,loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")):F0}%");
        _o.WriteLine($"Others (25-100%): retained={others.Count(d=>d.cls=="P1"||d.cls=="P1b")}/{others.Length} ({others.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/others.Length:F0}%), P1%={others.Count(d=>d.cls=="P1")*100.0/Math.Max(1,others.Count(d=>d.cls=="P1"||d.cls=="P1b")):F0}%");
        double rr=loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/Math.Max(1,loQ.Length)/(others.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/Math.Max(1,others.Length)+0.01);
        _o.WriteLine($"Retention ratio (low/other): {rr:F1}x");

        // ============================================================
        // PART D — Local Competition
        // ============================================================
        _o.WriteLine($"\n=== PART D: Local Competition Audit ===");
        int loRet=0,midRet=0,hiRet=0,loTot=0,midTot=0,hiTot=0;
        foreach(var g in pd.GroupBy(d=>d.seed)){
            var ordered=g.OrderBy(d=>d.Item3).ToArray();if(ordered.Length<3)continue;
            var lowest=ordered[0];var middle=ordered[1];var highest=ordered[2];
            loTot++;midTot++;hiTot++;
            if(lowest.cls!="reject")loRet++;if(middle.cls!="reject")midRet++;if(highest.cls!="reject")hiRet++;
        }
        _o.WriteLine($"Lowest-rank retained: {loRet}/{loTot} ({loRet*100.0/loTot:F0}%)");
        _o.WriteLine($"Middle-rank retained: {midRet}/{midTot} ({midRet*100.0/midTot:F0}%)");
        _o.WriteLine($"Highest-rank retained: {hiRet}/{hiTot} ({hiRet*100.0/hiTot:F0}%)");

        // ============================================================
        // PART E — Profile Contrast (lowest-rank retained vs rejected)
        // ============================================================
        _o.WriteLine($"\n=== PART E: Profile Contrast Audit ===");
        var loRankRet=pd.Where(d=>d.Item3<0.25&&(d.cls=="P1"||d.cls=="P1b")).ToArray();
        var loRankRej=pd.Where(d=>d.Item3<0.25&&d.cls=="reject").ToArray();
        _o.WriteLine($"low-rank retained={loRankRet.Length}, low-rank rejected={loRankRej.Length}");
        _o.WriteLine($"{"Descriptor",-14} {"Retained",10} {"Rejected",10} {"Delta",10}");
        _o.WriteLine(new string('-',45));
        void C(string n,Func<(int,int,double,double,double,double,double,double,string),double> f){
            double r=loRankRet.DefaultIfEmpty().Average(f),j=loRankRej.DefaultIfEmpty().Average(f);
            _o.WriteLine($"{n,-14} {r,10:F5} {j,10:F5} {r-j,10:F5}");
        }
        C("rawIQR",d=>d.Item3);C("residual",d=>d.Item4);C("rawMean",d=>d.Item5);
        C("rawMedian",d=>d.Item6);C("rawStd",d=>d.Item7);

        // ============================================================
        // PART F — Rank Sufficiency
        // ============================================================
        _o.WriteLine($"\n=== PART F: Rank Sufficiency Audit ===");
        foreach(var lo in new[]{0.0,0.25,0.5}){
            double hi=lo+0.25+(lo>0.4?0.01:0);
            var bin=pd.Where(d=>d.Item3>=lo&&d.Item3<hi).ToArray();
            var bp1=bin.Where(d=>d.cls=="P1").ToArray();var bp1b=bin.Where(d=>d.cls=="P1b").ToArray();
            double rd=Math.Abs(bp1.DefaultIfEmpty().Average(d=>d.Item4)-bp1b.DefaultIfEmpty().Average(d=>d.Item4));
            _o.WriteLine($"Rank [{lo:F2},{hi:F2}): n={bin.Length}, P1={bp1.Length}, P1b={bp1b.Length}, resid delta={rd:F5} {(rd>0.0005?"residual MATTERS":"residual negligible")}");
        }

        // ============================================================
        // PART G — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART G: Robustness ===");
        var rng2=new Random(42);int loStable=0;
        for(int sp=0;sp<50;sp++){
            var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            double s1r=shuf.Take(h).Where(d=>d.Item3<0.25).Count(d=>d.cls!="reject")*100.0/Math.Max(1,shuf.Take(h).Count(d=>d.Item3<0.25));
            double s2r=shuf.Skip(h).Where(d=>d.Item3<0.25).Count(d=>d.cls!="reject")*100.0/Math.Max(1,shuf.Skip(h).Count(d=>d.Item3<0.25));
            if(Math.Abs(s1r-s2r)<20)loStable++;
        }
        _o.WriteLine($"Low-rank retention stable (50 splits): {loStable}/50");

        // ============================================================
        // PART H — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART H: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(loRet>midRet*2&&monotonic)decision="Model A: SAC selects lowest-rank profiles. Preference is monotonic.";
        else if(loRet>midRet)decision="Model B: SAC selects low-rank profiles plus residual adjustment.";
        else if(rr<1.5)decision="Model D: Rank is proxy for another descriptor.";
        else decision="Model E: Preference unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: lo-ret={loRet*100.0/loTot:F0}%, mid={midRet*100.0/midTot:F0}%, hi={hiRet*100.0/hiTot:F0}%, monotonic={monotonic}, low/other ratio={rr:F1}x");
        _o.WriteLine("CLAIMS: Gate preference audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RGP_01 complete. Commit: RGP_01_RelativeGatePreferenceAudit ===");
    }

    [Fact]
    public void GEO_01_ProfileSpaceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== GEO_01: Profile Space Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is rank primitive or geometric projection? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // Build profile descriptors + pipeline outcomes
        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
        });});

        // Within-seed ranks
        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        // Run pipeline for outcomes
        var pipeBag=new ConcurrentBag<(int N,int seed,double rank,double resid,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        var siQ=new ConcurrentDictionary<int,double>();
        foreach(var g in allProf.GroupBy(p=>p.seed))siQ[g.Key]=g.Average(p=>p.riqr);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                var p=allProf.FirstOrDefault(x=>x.N==n&&x.seed==s);
                pipeBag.Add((n,s,seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1,p.riqr-siQ.GetValueOrDefault(s,0),cls));
            });});
        var pd=pipeBag.ToArray();

        // ============================================================
        // PART A+B — Build profile space + compute geometry per seed
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: Profile Space Geometry ===");
        // For each seed, compute geometric features in (rawIQR, rawMean, rawMed, rawStd) space
        var geoResults=new ConcurrentBag<(int seed,int N,double rank,double nnDist,double centDist,double isol,string cls)>();

        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var members=g.ToArray();if(members.Length<2)continue;
            // Normalize each dimension to [0,1] within seed
            double iqrMin=members.Min(p=>p.riqr),iqrMax=members.Max(p=>p.riqr);
            double mnMin=members.Min(p=>p.rmean),mnMax=members.Max(p=>p.rmean);
            double mdMin=members.Min(p=>p.rmed),mdMax=members.Max(p=>p.rmed);
            double sdMin=members.Min(p=>p.rstd),sdMax=members.Max(p=>p.rstd);
            double iqrRng=iqrMax-iqrMin,mnRng=mnMax-mnMin,mdRng=mdMax-mdMin,sdRng=sdMax-sdMin;

            // Centroid
            double ci=(iqrRng>0.001?members.Average(p=>p.riqr):0),cm=(mnRng>0.001?members.Average(p=>p.rmean):0);
            double cmd=(mdRng>0.001?members.Average(p=>p.rmed):0),cs=(sdRng>0.001?members.Average(p=>p.rstd):0);

            foreach(var m in members){
                double ni=(iqrRng>0.001?(m.riqr-iqrMin)/iqrRng:0),nmn=(mnRng>0.001?(m.rmean-mnMin)/mnRng:0);
                double nmd=(mdRng>0.001?(m.rmed-mdMin)/mdRng:0),nsd=(sdRng>0.001?(m.rstd-sdMin)/sdRng:0);

                // Centroid distance
                double cd=Math.Sqrt((ni-ci)*(ni-ci)+(nmn-cm)*(nmn-cm)+(nmd-cmd)*(nmd-cmd)+(nsd-cs)*(nsd-cs));

                // Nearest-neighbor distance
                double nn=double.MaxValue;
                foreach(var o in members)if(o.N!=m.N||o.seed!=m.seed){
                    double oi=(iqrRng>0.001?(o.riqr-iqrMin)/iqrRng:0),omn=(mnRng>0.001?(o.rmean-mnMin)/mnRng:0);
                    double omd=(mdRng>0.001?(o.rmed-mdMin)/mdRng:0),osd=(sdRng>0.001?(o.rstd-sdMin)/sdRng:0);
                    double d=Math.Sqrt((ni-oi)*(ni-oi)+(nmn-omn)*(nmn-omn)+(nmd-omd)*(nmd-omd)+(nsd-osd)*(nsd-osd));
                    if(d<nn)nn=d;
                }
                if(nn>1e10)nn=0;

                // Isolation = centroid distance / nearest neighbor (higher = more isolated)
                double isol=nn>0.001?cd/nn:0;

                double rank=seedRanks.GetValueOrDefault(g.Key)?.GetValueOrDefault(m.N,-1)??-1;
                string cls=pd.FirstOrDefault(x=>x.N==m.N&&x.seed==m.seed).cls??"unknown";
                geoResults.Add((m.seed,m.N,rank,nn,cd,isol,cls));
            }
        }
        var geo=geoResults.ToArray();

        // ============================================================
        // PART B — Geometric isolation of retained profiles
        // ============================================================
        _o.WriteLine($"\nGeometric comparison (retained vs rejected):");
        var gRet=geo.Where(d=>d.cls=="P1"||d.cls=="P1b").ToArray();
        var gRej=geo.Where(d=>d.cls=="reject").ToArray();
        _o.WriteLine($"{"Metric",-14} {"Retained",10} {"Rejected",10} {"Delta",10} {"Effect",10}");
        _o.WriteLine(new string('-',55));
        void GeoComp(string n,Func<(int,int,double,double,double,double,string),double> f){
            double r=gRet.DefaultIfEmpty().Average(f),j=gRej.DefaultIfEmpty().Average(f);
            double allStd=Sd(geo.Select(d=>f(d)).ToArray());
            double eff=allStd>0.001?Math.Abs(r-j)/allStd:0;
            _o.WriteLine($"{n,-14} {r,10:F4} {j,10:F4} {r-j,10:F4} {eff,10:F3}σ");
        }
        GeoComp("nnDist",d=>d.Item4);GeoComp("centroidDist",d=>d.Item5);
        GeoComp("isolation",d=>d.Item6);GeoComp("rank",d=>d.Item3);

        // ============================================================
        // PART C — Model comparison
        // ============================================================
        _o.WriteLine($"\n=== PART C: Model Comparison ===");
        // Simple logistic-style ranking: separation of retained vs rejected
        double rankSep=Math.Abs(gRet.Average(d=>d.Item3)-gRej.Average(d=>d.Item3));
        double isolSep=Math.Abs(gRet.Average(d=>d.Item6)-gRej.Average(d=>d.Item6));
        double nnSep=Math.Abs(gRet.Average(d=>d.Item4)-gRej.Average(d=>d.Item4));
        double centSep=Math.Abs(gRet.Average(d=>d.Item5)-gRej.Average(d=>d.Item5));

        _o.WriteLine($"Rank separation: {rankSep:F4}");
        _o.WriteLine($"Isolation separation: {isolSep:F4}");
        _o.WriteLine($"NN-distance separation: {nnSep:F4}");
        _o.WriteLine($"Centroid-distance separation: {centSep:F4}");

        string bestGeo=isolSep>nnSep&&isolSep>centSep?"isolation":nnSep>centSep?"nnDist":"centDist";
        _o.WriteLine($"Best geometric descriptor: {bestGeo}");

        // ============================================================
        // PART D — Rank independence from geometry
        // ============================================================
        _o.WriteLine($"\n=== PART D: Rank Independence Test ===");
        // Within each seed, rank should be near-perfectly correlated with rawIQR
        // But does rank add information beyond geometric position?
        double rankVsIso=Pearson(geo.Select(d=>d.Item3).ToArray(),geo.Select(d=>d.Item6).ToArray());
        double rankVsNn=Pearson(geo.Select(d=>d.Item3).ToArray(),geo.Select(d=>d.Item4).ToArray());
        double rankVsCent=Pearson(geo.Select(d=>d.Item3).ToArray(),geo.Select(d=>d.Item5).ToArray());
        _o.WriteLine($"Rank vs isolation: r={rankVsIso:F4}");
        _o.WriteLine($"Rank vs nnDist: r={rankVsNn:F4}");
        _o.WriteLine($"Rank vs centDist: r={rankVsCent:F4}");

        // Control for isolation: within isolation bins, does rank still separate?
        var isos=geo.Select(d=>d.Item6).OrderBy(v=>v).ToArray();
        double iMed=isos[isos.Length/2];
        var loIso=geo.Where(d=>d.Item6<=iMed).ToArray();var hiIso=geo.Where(d=>d.Item6>iMed).ToArray();
        double loRankSep=Math.Abs(loIso.Where(d=>d.cls!="reject").DefaultIfEmpty().Average(d=>d.Item3)-loIso.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item3));
        double hiRankSep=Math.Abs(hiIso.Where(d=>d.cls!="reject").DefaultIfEmpty().Average(d=>d.Item3)-hiIso.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item3));
        _o.WriteLine($"Rank separation after isolation control: lo={loRankSep:F4}, hi={hiRankSep:F4}");
        bool rankSurvivesGeo=loRankSep>0.01||hiRankSep>0.01;
        _o.WriteLine($"Rank {(rankSurvivesGeo?"SURVIVES":"does NOT survive")} geometry control");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");
        var rng2=new Random(42);int rankBetter=0;
        for(int sp=0;sp<50;sp++){
            var shuf=geo.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();var s2=shuf.Skip(h).ToArray();
            double r1r=Math.Abs(s1.Where(d=>d.cls!="reject").DefaultIfEmpty().Average(d=>d.Item3)-s1.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item3));
            double r1i=Math.Abs(s1.Where(d=>d.cls!="reject").DefaultIfEmpty().Average(d=>d.Item6)-s1.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item6));
            if(r1r>r1i)rankBetter++;
        }
        _o.WriteLine($"Rank beats isolation in {rankBetter}/50 splits");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(rankSurvivesGeo&&rankSep>isolSep*2)decision="Model A: Rank is primitive. Geometry adds no independent signal.";
        else if(rankSurvivesGeo)decision="Model B: Rank + geometry both contribute. Rank survives geometry control but geometry adds marginal information.";
        else if(isolSep>rankSep)decision="Model C: Local geometric position dominates. Rank is a projection.";
        else decision="Model D: Rank + geometry jointly required.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: rankSep={rankSep:F4}, isolSep={isolSep:F4}, rank survives geo={rankSurvivesGeo}, rank beats isol={rankBetter}/50");
        _o.WriteLine("CLAIMS: Profile space audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== GEO_01 complete. Commit: GEO_01_ProfileSpaceAudit ===");
    }

    [Fact]
    public void NBR_01_LocalReferenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== NBR_01: Local Reference Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Are retained profiles neighborhood centers? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
        });});

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        var pipeBag=new ConcurrentBag<(int N,int seed,double rank,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                pipeBag.Add((n,s,seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1,cls));
            });});
        var pd=pipeBag.ToArray();

        // ============================================================
        // PART A+B — Neighborhood structure per seed
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: Neighborhood Structure ===");
        // For each seed, compute: mean similarity of each profile to all others
        // Similarity = 1 / (1 + Euclidean distance in normalized space)
        var nbrResults=new ConcurrentBag<(int seed,int N,double rank,double centrality,bool isRetained,bool isMostCentral,string cls)>();

        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var members=g.ToArray();if(members.Length<2)continue;
            // Normalize within seed
            double iqrMin=members.Min(p=>p.riqr),iqrMax=members.Max(p=>p.riqr);
            double mnMin=members.Min(p=>p.rmean),mnMax=members.Max(p=>p.rmean);
            double mdMin=members.Min(p=>p.rmed),mdMax=members.Max(p=>p.rmed);
            double sdMin=members.Min(p=>p.rstd),sdMax=members.Max(p=>p.rstd);
            double iqrR=iqrMax-iqrMin,mnR=mnMax-mnMin,mdR=mdMax-mdMin,sdR=sdMax-sdMin;

            // Compute pairwise similarities
            int M=members.Length;
            var sims=new double[M];
            for(int i=0;i<M;i++){
                double totalSim=0;
                for(int j=0;j<M;j++)if(i!=j){
                    double di=(iqrR>0.001?(members[i].riqr-members[j].riqr)/iqrR:0);
                    double dm=(mnR>0.001?(members[i].rmean-members[j].rmean)/mnR:0);
                    double dd=(mdR>0.001?(members[i].rmed-members[j].rmed)/mdR:0);
                    double ds=(sdR>0.001?(members[i].rstd-members[j].rstd)/sdR:0);
                    double dist=Math.Sqrt(di*di+dm*dm+dd*dd+ds*ds);
                    totalSim+=1.0/(1.0+dist);
                }
                sims[i]=totalSim/(M-1); // mean similarity = centrality
            }

            // Most central profile
            int mostCentralIdx=0;double maxSim=sims[0];
            for(int i=1;i<M;i++)if(sims[i]>maxSim){maxSim=sims[i];mostCentralIdx=i;}

            for(int i=0;i<M;i++){
                double rank=seedRanks.GetValueOrDefault(g.Key)?.GetValueOrDefault(members[i].N,-1)??-1;
                string cls=pd.FirstOrDefault(x=>x.N==members[i].N&&x.seed==members[i].seed).cls??"unknown";
                bool retained=cls!="reject"&&cls!="unknown";
                nbrResults.Add((members[i].seed,members[i].N,rank,sims[i],retained,i==mostCentralIdx,cls));
            }
        }
        var nb=nbrResults.ToArray();

        // ============================================================
        // PART B — Centrality comparison
        // ============================================================
        _o.WriteLine($"\nCentrality comparison:");
        var nbRet=nb.Where(d=>d.isRetained).ToArray();
        var nbRej=nb.Where(d=>d.cls=="reject").ToArray();
        _o.WriteLine($"Retained centrality: {nbRet.Average(d=>d.centrality):F4} (n={nbRet.Length})");
        _o.WriteLine($"Rejected centrality: {nbRej.Average(d=>d.centrality):F4} (n={nbRej.Length})");
        _o.WriteLine($"Delta: {nbRet.Average(d=>d.centrality)-nbRej.Average(d=>d.centrality):F4}");

        // Is the most-central profile retained?
        int mostCentralRetained=nb.Count(d=>d.isMostCentral&&d.isRetained);
        int mostCentralTotal=nb.Count(d=>d.isMostCentral);
        _o.WriteLine($"Most-central profiles retained: {mostCentralRetained}/{mostCentralTotal} ({mostCentralRetained*100.0/Math.Max(1,mostCentralTotal):F0}%)");

        // Is the retained profile also the most central in its seed?
        var seedSummary=nb.GroupBy(d=>d.seed).Select(g=>{
            var ret=g.FirstOrDefault(d=>d.isRetained);
            var mc=g.FirstOrDefault(d=>d.isMostCentral);
            return (seed:g.Key,retainedIsMC:ret.isRetained&&ret.isMostCentral,mcIsRet:mc.isRetained);
        }).ToArray();
        int retIsMC=seedSummary.Count(s=>s.retainedIsMC);
        int mcIsRet=seedSummary.Count(s=>s.mcIsRet);
        _o.WriteLine($"Seeds where retained=most-central: {retIsMC}/{seedSummary.Length} ({retIsMC*100.0/seedSummary.Length:F0}%)");
        _o.WriteLine($"Seeds where most-central=retained: {mcIsRet}/{seedSummary.Length} ({mcIsRet*100.0/seedSummary.Length:F0}%)");

        // ============================================================
        // PART C — Model comparison
        // ============================================================
        _o.WriteLine($"\n=== PART C: Model Comparison ===");
        // Model A: lowest rank → retained?
        var rankSorted=nb.OrderBy(d=>d.Item3).ToArray();
        int lowestRank=nb.Where(d=>d.rank==0).Count(d=>d.isRetained);
        int totalLowest=nb.Count(d=>d.rank==0);
        _o.WriteLine($"Lowest-rank retained: {lowestRank}/{totalLowest} ({lowestRank*100.0/Math.Max(1,totalLowest):F0}%)");

        // Model B: most central → retained?
        _o.WriteLine($"Most-central retained: {mostCentralRetained}/{mostCentralTotal} ({mostCentralRetained*100.0/Math.Max(1,mostCentralTotal):F0}%)");

        // Correlation: rank vs centrality
        double rRC=Pearson(nb.Select(d=>d.Item3).ToArray(),nb.Select(d=>d.centrality).ToArray());
        _o.WriteLine($"Rank vs centrality: r={rRC:F4}");

        // ============================================================
        // PART D — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART D: Robustness ===");
        var rng2=new Random(42);int centWins=0,rankWins=0;
        for(int sp=0;sp<50;sp++){
            var shuf=nb.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            double cR=Math.Abs(s1.Where(d=>d.isRetained).DefaultIfEmpty().Average(d=>d.Item3)-s1.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item3));
            double cC=Math.Abs(s1.Where(d=>d.isRetained).DefaultIfEmpty().Average(d=>d.centrality)-s1.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.centrality));
            if(cC>cR*0.1)centWins++;else rankWins++;
        }
        _o.WriteLine($"50 splits: centrality wins={centWins}, rank wins={rankWins}");

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(retIsMC>seedSummary.Length*0.5)decision="Model A: Retained profiles ARE neighborhood centers. SAC selects central profiles.";
        else if(rRC<-0.5)decision="Model B: Rank and centrality are negatively coupled — low rank = high centrality. Rank is a centrality proxy.";
        else if(centWins>rankWins&&mostCentralRetained>totalLowest)decision="Model C: Centrality beats rank. SAC selects neighborhood representatives.";
        else decision="Model D: Rank remains dominant over centrality. Retained profiles are NOT specially central.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: retIsMC={retIsMC}/{seedSummary.Length}, mcIsRet={mcIsRet}/{seedSummary.Length}, rank-cent r={rRC:F4}, centWins={centWins}");
        _o.WriteLine("CLAIMS: Neighborhood audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== NBR_01 complete. Commit: NBR_01_LocalReferenceAudit ===");
    }

    [Fact]
    public void RGS_01_RelativeGateStructureAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RGS_01: Relative Gate Structure Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: What gate structure does SAC use? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        var seedIQRd=new ConcurrentDictionary<int,double>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
            seedIQRd.AddOrUpdate(s,Q(wo,0.75)-Q(wo,0.25),(_,v)=>v+Q(wo,0.75)-Q(wo,0.25));
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        var pipeBag=new ConcurrentBag<(int N,int seed,double riqr,double resid,double rank,double rmean,double rmed,double rstd,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                var p=allProf.FirstOrDefault(x=>x.N==n&&x.seed==s);
                pipeBag.Add((n,s,p.riqr,p.riqr-siQ.GetValueOrDefault(s,0),seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1,p.rmean,p.rmed,p.rstd,cls));
            });});
        var pd=pipeBag.ToArray();

        // ============================================================
        // PART A — Rank Occupancy
        // ============================================================
        _o.WriteLine($"\n=== PART A: Rank Occupancy ===");
        _o.WriteLine($"{"Rank class",-12} {"Total",6} {"Retained",9} {"Ret%",7} {"P1",4} {"P1b",4} {"P1%",7}");
        _o.WriteLine(new string('-',55));
        // Exact rank classes from {0, 0.5, 1.0}
        foreach(var rc in new[]{0.0,0.5,1.0}){
            var qd=pd.Where(d=>Math.Abs(d.rank-rc)<0.01).ToArray();
            int qr=qd.Count(d=>d.cls!="reject"),qp1=qd.Count(d=>d.cls=="P1"),qp1b=qd.Count(d=>d.cls=="P1b");
            _o.WriteLine($"{$"rank={rc:F1}",-12} {qd.Length,6} {qr,9} {qr*100.0/qd.Length,7:F1}% {qp1,4} {qp1b,4} {(qp1+qp1b>0?qp1*100.0/(qp1+qp1b):0),7:F0}%");
        }

        // ============================================================
        // PART B — Winner-Take-All
        // ============================================================
        _o.WriteLine($"\n=== PART B: Winner-Take-All Audit ===");
        int winnerRet=0,secondRet=0,thirdRet=0,seedsWith3=0,seedsWithAnyRet=0;
        foreach(var g in pd.GroupBy(d=>d.seed)){
            var ordered=g.OrderBy(d=>d.Item3).ToArray();if(ordered.Length<3)continue;seedsWith3++;
            if(ordered[0].cls!="reject")winnerRet++;
            if(ordered[1].cls!="reject")secondRet++;
            if(ordered[2].cls!="reject")thirdRet++;
            if(ordered.Any(d=>d.cls!="reject"))seedsWithAnyRet++;
        }
        _o.WriteLine($"Seeds with 3 IsHi-pass: {seedsWith3}");
        _o.WriteLine($"Winner (rank=0) retained: {winnerRet}/{seedsWith3} ({winnerRet*100.0/seedsWith3:F0}%)");
        _o.WriteLine($"Runner-up (rank=0.5) retained: {secondRet}/{seedsWith3} ({secondRet*100.0/seedsWith3:F0}%)");
        _o.WriteLine($"Third (rank=1.0) retained: {thirdRet}/{seedsWith3} ({thirdRet*100.0/seedsWith3:F0}%)");

        // Concentration: what share of retained profiles are winners?
        int totalRet=pd.Count(d=>d.cls!="reject");
        _o.WriteLine($"Retention concentration: winner share={winnerRet}/{totalRet} ({winnerRet*100.0/Math.Max(1,totalRet):F0}%)");
        _o.WriteLine($"Winner-take-all: {(winnerRet>secondRet*2?"YES — winner dominates":"NO — retention is distributed")}");

        // ============================================================
        // PART C — Rank Gap Audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Rank Gap Audit ===");
        // Gap = rank difference to nearest competitor in same seed
        var gapResults=new List<(double gap,string cls)>();
        foreach(var g in pd.GroupBy(d=>d.seed)){
            var ordered=g.OrderBy(d=>d.Item3).ToArray();if(ordered.Length<2)continue;
            for(int i=0;i<ordered.Length;i++){
                double nearestGap=i==0?ordered[1].rank-ordered[0].rank:i==ordered.Length-1?ordered[i].rank-ordered[i-1].rank:Math.Min(ordered[i].rank-ordered[i-1].rank,ordered[i+1].rank-ordered[i].rank);
                gapResults.Add((nearestGap,ordered[i].cls));
            }
        }
        var gaps=gapResults.ToArray();
        var retGaps=gaps.Where(d=>d.cls!="reject").Select(d=>d.gap).ToArray();
        var rejGaps=gaps.Where(d=>d.cls=="reject").Select(d=>d.gap).ToArray();
        _o.WriteLine($"Retained gap: mean={retGaps.DefaultIfEmpty(0).Average():F4}, median={retGaps.OrderBy(v=>v).DefaultIfEmpty(0).ToArray()[retGaps.Length/2]:F4}");
        _o.WriteLine($"Rejected gap: mean={rejGaps.Average():F4}, median={rejGaps.OrderBy(v=>v).ToArray()[rejGaps.Length/2]:F4}");
        _o.WriteLine($"Gap type: {(retGaps.Length>0&&rejGaps.Length>0&&retGaps.Average()<rejGaps.Average()?"TIGHT — retained have smaller competitive gaps":"WIDE — retained have larger competitive gaps")}");

        // ============================================================
        // PART D — Residual Within-Rank
        // ============================================================
        _o.WriteLine($"\n=== PART D: Residual Within-Rank Audit ===");
        foreach(var rc in new[]{0.0,0.5,1.0}){
            var bin=pd.Where(d=>Math.Abs(d.rank-rc)<0.01).ToArray();
            var bRet=bin.Where(d=>d.cls!="reject").ToArray();var bRej=bin.Where(d=>d.cls=="reject").ToArray();
            if(bRet.Length<1||bRej.Length<1)continue;
            double rd=Math.Abs(bRet.DefaultIfEmpty().Average(d=>d.Item5)-bRej.DefaultIfEmpty().Average(d=>d.Item5));
            double md=Math.Abs(bRet.DefaultIfEmpty().Average(d=>d.rmean)-bRej.DefaultIfEmpty().Average(d=>d.rmean));
            _o.WriteLine($"Rank={rc:F1}: n_ret={bRet.Length}, n_rej={bRej.Length}, resid delta={rd:F5}, rmean delta={md:F5}");
        }

        // ============================================================
        // PART E — Model Comparison
        // ============================================================
        _o.WriteLine($"\n=== PART E: Model Comparison ===");
        // Simple ranking by retention separation
        double rankSep=Math.Abs(pd.Where(d=>d.cls!="reject").Average(d=>d.Item3)-pd.Where(d=>d.cls=="reject").Average(d=>d.Item3));
        double residSep=Math.Abs(pd.Where(d=>d.cls!="reject").Average(d=>d.Item5)-pd.Where(d=>d.cls=="reject").Average(d=>d.Item5));
        double winnerRate=winnerRet*100.0/Math.Max(1,seedsWith3);
        double runnerRate=secondRet*100.0/Math.Max(1,seedsWith3);

        _o.WriteLine($"{"Model",-30} {"Metric",12} {"Value",10}");
        _o.WriteLine(new string('-',55));
        _o.WriteLine($"{"A: rank only",-30} {"rank sep",12} {rankSep,10:F4}");
        _o.WriteLine($"{"B: rank + residual",-30} {"resid sep",12} {residSep,10:F5}");
        _o.WriteLine($"{"C: winner-take-all",-30} {"winner%",12} {winnerRate,10:F0}%");
        _o.WriteLine($"{"D: rank anomaly",-30} {"runner%",12} {runnerRate,10:F0}%");

        // ============================================================
        // PART F — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART F: Robustness ===");
        var rng2=new Random(42);int rankTop=0,winnerTop=0;
        for(int sp=0;sp<50;sp++){
            var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            // Count local winners retained in this split
            int w=0,ttl=0;
            foreach(var g in s1.GroupBy(d=>d.seed)){
                var o=g.OrderBy(d=>d.Item3).ToArray();if(o.Length<3)continue;ttl++;
                if(o[0].cls!="reject")w++;
            }
            double wr=ttl>0?w*100.0/ttl:0;
            if(wr>5)winnerTop++;
            double rSep=Math.Abs(s1.Where(d=>d.cls!="reject").DefaultIfEmpty().Average(d=>d.Item3)-s1.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item3));
            if(rSep>0.01)rankTop++;
        }
        _o.WriteLine($"Rank stable (>0.01 sep): {rankTop}/50. Winner stable (>5%): {winnerTop}/50");

        // ============================================================
        // PART G — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART G: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(winnerRate>20&&winnerRate>runnerRate*2)decision="Model A: SAC operates as lowest-rank selection. Winner-take-all dominates.";
        else if(winnerRate>runnerRate&&rankSep>0.01)decision="Model B: SAC operates as lowest-rank selection with residual refinement.";
        else if(winnerRate<10)decision="Model C: SAC does NOT operate as winner-take-all. Retention is sparse across all ranks.";
        else decision="Model E: Gate structure unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: winner={winnerRate:F0}%, runner={runnerRate:F0}%, rankSep={rankSep:F4}, residSep={residSep:F5}");

        // ============================================================
        // PART H — Frontier Impact
        // ============================================================
        _o.WriteLine($"\n=== PART H: Frontier Impact ===");
        _o.WriteLine($"GEO_01: Retained are NOT outliers (strengthens B — marginal selection).");
        _o.WriteLine($"NBR_01: Retained are NOT centers (strengthens B — rank drives selection).");
        _o.WriteLine($"RGS_01: Winner retention={winnerRate:F0}%, rankSep={rankSep:F4}.");
        _o.WriteLine($"Overall: {(winnerRate>15?"Model B strengthened — rank-primary gate confirmed.":"Model B weakened — rank effect is marginal.")}");

        _o.WriteLine($"\nCLAIMS: Gate structure audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RGS_01 complete. Commit: RGS_01_RelativeGateStructureAudit ===");
    }

    [Fact]
    public void RG0_01_RankZeroGateAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RG0_01: Rank-Zero Gate Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Rank0 gate or continuous preference? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        var seedIQRd=new ConcurrentDictionary<int,double>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
            seedIQRd.AddOrUpdate(s,Q(wo,0.75)-Q(wo,0.25),(_,v)=>v+Q(wo,0.75)-Q(wo,0.25));
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        var pipeBag=new ConcurrentBag<(int N,int seed,double rank,bool isRank0,double resid,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                var p=allProf.FirstOrDefault(x=>x.N==n&&x.seed==s);
                double rank=seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1;
                pipeBag.Add((n,s,rank,rank<0.01,p.riqr-siQ.GetValueOrDefault(s,0),cls));
            });});
        var pd=pipeBag.ToArray();
        var ret=pd.Where(d=>d.cls!="reject").ToArray();var rej=pd.Where(d=>d.cls=="reject").ToArray();
        _o.WriteLine($"Retained={ret.Length}, Rejected={rej.Length}");

        // ============================================================
        // PART A+B — Binary vs continuous rank
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: Binary Rank0 vs Continuous Rank ===");
        _o.WriteLine($"{"Predictor",-20} {"Ret mean",10} {"Rej mean",10} {"Delta",10} {"Effect(σ)",12}");
        _o.WriteLine(new string('-',65));
        double rankContSep=Math.Abs(ret.Average(d=>d.Item3)-rej.Average(d=>d.Item3));
        double rank0RateRet=ret.Count(d=>d.Item4)*100.0/ret.Length;
        double rank0RateRej=rej.Count(d=>d.Item4)*100.0/rej.Length;
        double allStd=Sd(pd.Select(d=>d.Item3).ToArray());
        double eff=allStd>0.001?rankContSep/allStd:0;
        _o.WriteLine($"{"continuous rank",-20} {ret.Average(d=>d.Item3),10:F4} {rej.Average(d=>d.Item3),10:F4} {rankContSep,10:F4} {eff,12:F4}σ");
        _o.WriteLine($"{"Rank0 indicator",-20} {rank0RateRet,10:F1}% {rank0RateRej,10:F1}% {Math.Abs(rank0RateRet-rank0RateRej),10:F1}%");

        // Threshold search: test rank < t for t in {0.01, 0.26, 0.51, 0.76}
        _o.WriteLine($"\nThreshold search:");
        _o.WriteLine($"{"Thresh",8} {"Ret%",8} {"Rej%",8} {"Diff",8} {"OR",8}");
        _o.WriteLine(new string('-',42));
        foreach(var t in new[]{0.01,0.26,0.51,0.76}){
            double rr=pd.Where(d=>d.Item3<t).Count(d=>d.cls!="reject")*100.0/Math.Max(1,pd.Count(d=>d.Item3<t));
            double rj=pd.Where(d=>d.rank>=t).Count(d=>d.cls!="reject")*100.0/Math.Max(1,pd.Count(d=>d.rank>=t));
            double oddsR=pd.Where(d=>d.Item3<t).Count(d=>d.cls!="reject")*1.0/Math.Max(1,pd.Where(d=>d.Item3<t).Count(d=>d.cls=="reject"));
            double oddsJ=pd.Where(d=>d.rank>=t).Count(d=>d.cls!="reject")*1.0/Math.Max(1,pd.Where(d=>d.rank>=t).Count(d=>d.cls=="reject"));
            _o.WriteLine($"{t,8:F2} {rr,8:F1}% {rj,8:F1}% {rr-rj,8:F1}% {oddsR/(oddsJ+0.01),8:F2}x");
        }

        // ============================================================
        // PART C — Model comparison
        // ============================================================
        _o.WriteLine($"\n=== PART C: Model Comparison ===");
        // Simple rank-based accuracy
        int rank0Correct=pd.Count(d=>d.Item4&&d.cls!="reject")+pd.Count(d=>!d.Item4&&d.cls=="reject");
        double medRank=pd.Select(d=>d.Item3).OrderBy(v=>v).ToArray()[pd.Length/2];
        int contCorrect=pd.Count(d=>d.Item3<medRank&&d.cls!="reject")+pd.Count(d=>d.rank>=medRank&&d.cls=="reject");
        _o.WriteLine($"Rank0 rule accuracy: {rank0Correct}/{pd.Length} ({rank0Correct*100.0/pd.Length:F1}%)");
        _o.WriteLine($"Median-rank rule accuracy: {contCorrect}/{pd.Length} ({contCorrect*100.0/pd.Length:F1}%)");

        // Rank0 + residual refinement
        var r0=pd.Where(d=>d.Item4).ToArray();
        double r0ResidSep=Math.Abs(r0.Where(d=>d.cls!="reject").DefaultIfEmpty().Average(d=>d.Item5)-r0.Where(d=>d.cls=="reject").DefaultIfEmpty().Average(d=>d.Item5));
        _o.WriteLine($"Rank0 + residual: resid sep within Rank0={r0ResidSep:F5}");
        _o.WriteLine($"Model ranking: Rank0 {(rank0Correct>contCorrect?"BEATS":"loses to")} continuous. Residual {(r0ResidSep>0.0005?"ADDS value":"adds nothing")}.");

        // ============================================================
        // PART D — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART D: Robustness ===");
        var rng2=new Random(42);int rank0Wins=0;
        for(int sp=0;sp<50;sp++){
            var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1=shuf.Take(h).ToArray();
            double r0r=s1.Where(d=>d.Item4).Count(d=>d.cls!="reject")*100.0/Math.Max(1,s1.Count(d=>d.Item4));
            double cr=s1.Where(d=>d.Item3<0.26).Count(d=>d.cls!="reject")*100.0/Math.Max(1,s1.Count(d=>d.Item3<0.26));
            if(r0r>cr)rank0Wins++;
        }
        _o.WriteLine($"Rank0 beats continuous threshold in splits: {rank0Wins}/50");

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double rank0RetRate=ret.Count(d=>d.Item4)*100.0/ret.Length;
        string decision;
        if(rank0RetRate>60)decision="Model B: Rank0 gate. SAC specifically targets the lowest-rank profile.";
        else if(rank0RetRate>40&&r0ResidSep>0.001)decision="Model C: Rank0 gate + residual refinement. Rank0 is primary filter; residual refines within Rank0.";
        else if(rankContSep>0.05)decision="Model A: Continuous rank preference. SAC prefers lower ranks along a spectrum.";
        else decision="Model D: Unresolved. Rank effect too weak to distinguish gate from spectrum.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: Rank0 ret%={rank0RetRate:F0}%, continuous sep={rankContSep:F4}, resid within-Rank0={r0ResidSep:F5}");
        _o.WriteLine("CLAIMS: Rank0 gate audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RG0_01 complete. Commit: RG0_01_RankZeroGateAudit ===");
    }

    [Fact]
    public void RKO_01_RankOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RKO_01: Rank Origin Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: What structure generates SAC-relevant rank? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
        });});

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        // ============================================================
        // PART A — Rank decomposition: what explains rank?
        // ============================================================
        _o.WriteLine($"\n=== PART A: Rank Decomposition ===");
        // Rank = f(rawIQR ordering). Compute correlation of rank with each descriptor.
        var allData=new List<(double rank,double riqr,double rmean,double rmed,double rstd)>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            foreach(var m in g){
                double rank=seedRanks.GetValueOrDefault(g.Key)?.GetValueOrDefault(m.N,-1)??-1;
                if(rank>=0)allData.Add((rank,m.riqr,m.rmean,m.rmed,m.rstd));
            }
        }
        var ad=allData.ToArray();
        _o.WriteLine($"{"Descriptor",-14} {"r with rank",12} {"R²",10}");
        _o.WriteLine(new string('-',38));
        void Rpt3(string n,double[] y){double r=Pearson(ad.Select(d=>d.rank).ToArray(),y);_o.WriteLine($"{n,-14} {r,12:F4} {r*r,10:F4}");}
        Rpt3("rawIQR",ad.Select(d=>d.riqr).ToArray());
        Rpt3("rawMean",ad.Select(d=>d.rmean).ToArray());
        Rpt3("rawMedian",ad.Select(d=>d.rmed).ToArray());
        Rpt3("rawStd",ad.Select(d=>d.rstd).ToArray());

        // ============================================================
        // PART B — Residualized rank
        // ============================================================
        _o.WriteLine($"\n=== PART B: Residualized Rank ===");
        // Remove rawIQR contribution: residual rank = rank - predicted rank from rawIQR
        // Since rank = f(sort(rawIQR)), residual rank ≈ 0 by construct
        // But we can test: after removing linear rawIQR effect, does residual rank survive?
        var rIQR=ad.Select(d=>d.riqr).ToArray();var rRank=ad.Select(d=>d.rank).ToArray();
        double b=(Pearson(rIQR,rRank)*Sd(rRank))/(Sd(rIQR)+0.0001);
        double a=rRank.Average()-b*rIQR.Average();
        var residRank=ad.Select((d,i)=>d.rank-(a+b*d.riqr)).ToArray();
        _o.WriteLine($"Residual rank after rawIQR removal: mean={residRank.Average():F6}, std={Sd(residRank):F6}");
        _o.WriteLine($"Residual rank range: [{residRank.Min():F4}, {residRank.Max():F4}]");
        _o.WriteLine($"Rank is {(Sd(residRank)<0.01?"ENTIRELY rawIQR-driven":"partially independent of rawIQR")}");

        // ============================================================
        // PART C — Pairwise competition
        // ============================================================
        _o.WriteLine($"\n=== PART C: Pairwise Competition ===");
        // Within each seed, rank is determined by pairwise rawIQR comparisons
        // Test: if we only knew pairwise "A beats B" from rawIQR, can we predict retention?
        int seedsWithData=0, pairwiseCorrect=0;
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var members=g.OrderBy(p=>p.riqr).ToArray();if(members.Length<3)continue;seedsWithData++;
            // Pairwise: lowest rawIQR beats highest rawIQR → rank ordering
            // The rank structure is fully determined by rawIQR ordering
            // So pairwise competition = rawIQR ordering = rank
            pairwiseCorrect++; // always correct: rank IS rawIQR ordering
        }
        _o.WriteLine($"Seeds with 3 profiles: {seedsWithData}");
        _o.WriteLine($"Pairwise competition model: {(seedsWithData>0?"Rank = rawIQR ordering (by definition). Pairwise model = rank model.":"N/A")}");

        // ============================================================
        // PART D — Rank reconstruction
        // ============================================================
        _o.WriteLine($"\n=== PART D: Rank Reconstruction ===");
        // Model A: rank = f(rawIQR) → R² from correlation
        double r2IQR=Pearson(rIQR,rRank);r2IQR=r2IQR*r2IQR;
        // Model B: rank = f(rawIQR, rawMean) — multivariate not needed since rank IS sort(rawIQR)
        _o.WriteLine($"Model A (rawIQR only): R²={r2IQR:F4} (rank = sort(rawIQR) by definition)");
        _o.WriteLine($"Model B (rawIQR + mean): R²={r2IQR:F4} (mean adds nothing — rank is sort construct)");
        _o.WriteLine($"Reconstruction: rank IS rawIQR ordering. No model needed.");

        // But test: within rawIQR ties (none exist since IQR is continuous), would other descriptors break ties?
        var iqrVals=ad.Select(d=>d.riqr).ToArray();
        int ties=iqrVals.Length-iqrVals.Distinct().Count();
        _o.WriteLine($"RawIQR ties: {ties} (of {iqrVals.Length}). Tie-breaking by other descriptors: {(ties>0?"possible":"NOT possible — rawIQR fully orders all profiles")}");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");
        var rng2=new Random(42);int r2Stable=0;
        for(int sp=0;sp<50;sp++){
            var shuf=ad.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            var s1r=shuf.Take(h).Select(d=>d.rank).ToArray();var s1i=shuf.Take(h).Select(d=>d.riqr).ToArray();
            double r=Pearson(s1r,s1i);if(r>0.7)r2Stable++;
        }
        _o.WriteLine($"Rank-IQR correlation stable (>0.7): {r2Stable}/50 splits");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        // Rank IS sort(rawIQR) by construction. Low linear r² is expected:
        // ordinal rank (0, 0.5, 1.0) vs continuous rawIQR → non-linear monotonic.
        if(ties==0||r2IQR<0.1)decision="Model A: Rank IS rawIQR ordering by construction. Low linear r² is expected for ordinal-vs-continuous mapping. SAC's rank sensitivity = SAC's rawIQR ordering sensitivity.";
        else decision="Model B: Rank is primarily rawIQR ordering.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: r²(rank,rawIQR)={r2IQR:F4}, residual rank std={Sd(residRank):F6}, ties={ties}");
        _o.WriteLine($"Rank = sort(rawIQR) by construction. SAC's rank sensitivity = SAC's rawIQR sensitivity.");
        _o.WriteLine("CLAIMS: Rank origin audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RKO_01 complete. Commit: RKO_01_RankOriginAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
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
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
