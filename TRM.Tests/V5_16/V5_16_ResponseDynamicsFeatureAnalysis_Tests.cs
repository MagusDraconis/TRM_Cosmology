using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_16;

[Trait("Category","V5_16"),Trait("Category","V5_16_EGA"),Trait("Category","LongRunning")]
public class V5_16_ResponseDynamicsFeatureAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct RDProfile{
        public int seed,cohort,n;public string cls,stateGroup,errMode;
        public double projHiVec,orthHiVec,dMean,dStd,dVelocity,lambda1,kFrob,edgeDensity,loOverlap,omT1,omT2,dT1,dT2;
        public bool immHi,persist,predM3p;
        public double rebMagnitude,dRespElast,dispAlong,dispOrth,actualCompFrac,deltaOmDeltaD,immIndRate;
    }

    public V5_16_ResponseDynamicsFeatureAnalysis_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void EGA_01_RebMagnitudeSeparation(){
        _o.WriteLine("═══ EGA_01: rebMagnitude separation ═══");
        var profiles=CollectProfiles();
        AssignGroups(profiles);

        _o.WriteLine("\n─── RebMagnitude by error group (train) ───");
        _o.WriteLine(string.Format("{0,-8} {1,6} {2,8} {3,8} {4,8} {5,8}",
            "Group","Count","Mean","Median","Std","ES_vs_TP"));
        var tp=profiles.Where(p=>p.stateGroup=="G1"&&p.cohort==0).ToArray();
        if(tp.Length>=3){
            double tpMean=tp.Average(p=>p.rebMagnitude),tpMed=Median(tp.Select(p=>p.rebMagnitude).ToArray());
            _o.WriteLine($"{"TP",-8} {tp.Length,6} {tpMean,8:F4} {tpMed,8:F4} {Std(tp.Select(p=>p.rebMagnitude).ToArray()),8:F4} {"-",8}");

            foreach(var g in new[]{"G2","G3","G4"}){
                var grp=profiles.Where(p=>p.stateGroup==g&&p.cohort==0).ToArray();
                if(grp.Length<3)continue;
                double es=EffSz(grp.Select(p=>p.rebMagnitude).ToArray(),tp.Select(p=>p.rebMagnitude).ToArray());
                _o.WriteLine($"{g,-8} {grp.Length,6} {grp.Average(p=>p.rebMagnitude),8:F4} {Median(grp.Select(p=>p.rebMagnitude).ToArray()),8:F4} {Std(grp.Select(p=>p.rebMagnitude).ToArray()),8:F4} {es,8:F3}");
            }
        }

        // By N
        _o.WriteLine("\n─── RebMagnitude by N (train, all groups) ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8}",
            "N","Count","Mean","SuccMean","FailMean"));
        foreach(var n in new[]{67,71,72,75,80}){
            var sub=profiles.Where(p=>p.n==n&&p.cohort==0).ToArray();
            var s=sub.Where(p=>p.persist).ToArray();var f=sub.Where(p=>!p.persist).ToArray();
            _o.WriteLine($"{n,4} {sub.Length,6} {sub.Average(p=>p.rebMagnitude),8:F4} {s.Average(p=>p.rebMagnitude),8:F4} {f.Average(p=>p.rebMagnitude),8:F4}");
        }

        // Train/holdout stability
        _o.WriteLine("\n─── Train/holdout stability ───");
        foreach(var g in new[]{"G1","G2"}){
            var tr=profiles.Where(p=>p.stateGroup==g&&p.cohort==0).ToArray();
            var ho=profiles.Where(p=>p.stateGroup==g&&p.cohort==1).ToArray();
            if(tr.Length<2||ho.Length<2)continue;
            _o.WriteLine($"{g}: Train={tr.Average(p=>p.rebMagnitude):F4} Hold={ho.Average(p=>p.rebMagnitude):F4} (Δ={tr.Average(p=>p.rebMagnitude)-ho.Average(p=>p.rebMagnitude):+.000})");
        }

        _profiles=profiles;
    }

    [Fact]public void EGA_02_IndependenceFromM3plus(){
        _o.WriteLine("═══ EGA_02: Independence from M3+ ═══");
        var profiles=EnsureProfiles();
        var train=profiles.Where(p=>p.cohort==0).ToArray();

        // Correlate rebMagnitude with M3+ features
        _o.WriteLine(string.Format("\n{0,-16} {1,8} {2,8} {3,8} {4,8}",
            "Feature","r(reb)","r(dMean)","r(projHi)","r(orthHi)"));
        var m3fs=new[]{"projHiVec","orthHiVec","dMean","dStd","lambda1","kFrob"};
        foreach(var f in m3fs){
            var rv=GetVals(train,"rebMagnitude");
            var fv=GetVals(train,f);
            double rrb=Corr(rv,GetVals(train,f));
            _o.WriteLine($"{f,-16} {rrb,8:+.00} {"-",8} {"-",8} {"-",8}");
        }
        // Correlation with key features
        double rdMean=Corr(GetVals(train,"rebMagnitude"),GetVals(train,"dMean"));
        double rProj=Corr(GetVals(train,"rebMagnitude"),GetVals(train,"projHiVec"));
        double rOrth=Corr(GetVals(train,"rebMagnitude"),GetVals(train,"orthHiVec"));
        _o.WriteLine($"\nrebMagnitude max|r| with M3+: {Math.Max(Math.Max(Math.Abs(rdMean),Math.Abs(rProj)),Math.Abs(rOrth)):F2}");

        // Does rebMagnitude improve error classification beyond M3+?
        _o.WriteLine("\n─── Logistic regression: persistence ~ M3+ vs M3++rebMagnitude ───");
        // Simple: for M3+ predicted positive, does rebMagnitude separate TP from FP?
        var m3p=train.Where(p=>p.predM3p).ToArray();
        if(m3p.Length>=6){
            var tp=m3p.Where(p=>p.persist).ToArray();var fp=m3p.Where(p=>!p.persist).ToArray();
            double meanTP=tp.Average(p=>p.rebMagnitude),meanFP=fp.Average(p=>p.rebMagnitude);
            double es=EffSz(tp.Select(p=>p.rebMagnitude).ToArray(),fp.Select(p=>p.rebMagnitude).ToArray());
            _o.WriteLine($"M3+ TP rebMean={meanTP:F4} FP rebMean={meanFP:F4} ES={es:F3}");
            // Simple threshold
            double bestT=FindBestThresh(m3p,p=>p.rebMagnitude);
            int above=m3p.Count(p=>p.rebMagnitude>bestT);
            int corrAbove=m3p.Count(p=>p.rebMagnitude>bestT&&p.persist);
            _o.WriteLine($"  Threshold={bestT:F4}: above={above} correct={corrAbove} ({corrAbove*100.0/above:F0}%)");
        }

        // rebMagnitude vs omT2
        _o.WriteLine("\n─── rebMagnitude vs omT2 ───");
        double rOm2=Corr(GetVals(train,"rebMagnitude"),GetVals(train,"omT2"));
        _o.WriteLine($"r(reb,omT2)={rOm2:+.00}");
    }

    [Fact]public void EGA_03_PreInterventionProxy(){
        _o.WriteLine("═══ EGA_03: Pre-intervention proxy search ═══");
        var profiles=EnsureProfiles();
        var train=profiles.Where(p=>p.cohort==0).ToArray();

        _o.WriteLine("\n─── Pre-intervention features predicting rebMagnitude ───");
        _o.WriteLine(string.Format("{0,-14} {1,8} {2,8} {3,10}",
            "Feature","r(reb)","ES_hiLo","Proxy?"));
        var candidates=new[]{"dMean","dStd","projHiVec","orthHiVec","dVelocity",
            "edgeDensity","loOverlap","lambda1","kFrob"};
        var rv=GetVals(train,"rebMagnitude");
        foreach(var f in candidates){
            var fv=GetVals(train,f);
            double r=Corr(rv,fv);
            // Split at median to test
            double med=fv.OrderBy(x=>x).ElementAt(fv.Length/2);
            var hi=train.Where(p=>GetVal(p,f)>med).ToArray();
            var lo=train.Where(p=>GetVal(p,f)<=med).ToArray();
            double es=EffSz(hi.Select(p=>p.rebMagnitude).ToArray(),lo.Select(p=>p.rebMagnitude).ToArray());
            _o.WriteLine($"{f,-14} {r,8:+.00} {es,8:F3} {(Math.Abs(r)>0.3?"PROMISING":"no"),10}");
        }

        // Best proxy
        _o.WriteLine("\n─── Best pre-intervention proxy ───");
        double bestR=0;string bestF="";
        foreach(var f in candidates){
            double r=Math.Abs(Corr(rv,GetVals(train,f)));
            if(r>bestR){bestR=r;bestF=f;}
        }
        _o.WriteLine($"Best: {bestF} (|r|={bestR:F2})");
        if(bestR>0.3)_o.WriteLine("Proxy exists — rebMagnitude may be partially predictable pre-intervention.");
        else _o.WriteLine("No strong proxy — rebMagnitude is largely post-intervention emergent.");
    }

    [Fact]public void EGA_04_ErrorModeAndNSpecific(){
        _o.WriteLine("═══ EGA_04: Error-mode classification and N-specific analysis ═══");
        var profiles=EnsureProfiles();
        ClassifyErrModes(profiles);

        // Error modes
        var modes=new[]{"highRebound","lowRebound","insufDisp","overRebound","K_collapse","offBasin","unresolved"};
        _o.WriteLine("\n─── Error mode distribution ───");
        foreach(var m in modes){
            int c=profiles.Count(p=>p.errMode==m);
            if(c>0)_o.WriteLine($"  {m}: {c} ({c*100.0/profiles.Length:F0}%)");
        }

        // By error group
        _o.WriteLine("\n─── Error mode by group ───");
        foreach(var g in new[]{"G2","G4"}){
            var grp=profiles.Where(p=>p.stateGroup==g).ToArray();
            if(grp.Length==0)continue;
            var top=modes.Select(m=>(m,grp.Count(p=>p.errMode==m))).OrderByDescending(x=>x.Item2).First();
            _o.WriteLine($"{g} (n={grp.Length}): top={top.m} ({top.Item2})");
        }

        // N-specific rebMagnitude
        _o.WriteLine("\n─── N=72 M3+ selected: TP vs FP rebMagnitude ───");
        var n72sel=profiles.Where(p=>p.n==72&&p.predM3p).ToArray();
        var n72tp=n72sel.Where(p=>p.persist).ToArray();var n72fp=n72sel.Where(p=>!p.persist).ToArray();
        if(n72tp.Length>=2&&n72fp.Length>=2)
            _o.WriteLine($"TP={n72tp.Average(p=>p.rebMagnitude):F4} FP={n72fp.Average(p=>p.rebMagnitude):F4} ES={EffSz(n72tp.Select(p=>p.rebMagnitude).ToArray(),n72fp.Select(p=>p.rebMagnitude).ToArray()):F3}");

        _o.WriteLine("\n─── N=75 success vs failure rebMagnitude ───");
        var n75=profiles.Where(p=>p.n==75&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        var n75s=n75.Where(p=>p.persist).ToArray();var n75f=n75.Where(p=>!p.persist).ToArray();
        _o.WriteLine($"Succ={n75s.Average(p=>p.rebMagnitude):F4} Fail={n75f.Average(p=>p.rebMagnitude):F4} ES={EffSz(n75s.Select(p=>p.rebMagnitude).ToArray(),n75f.Select(p=>p.rebMagnitude).ToArray()):F3}");
    }

    [Fact]public void EGA_05_FeatureVerdictAndGates(){
        _o.WriteLine("═══ EGA_05: Response-dynamics verdict and decision gates ═══");
        var profiles=EnsureProfiles();

        var tp=profiles.Where(p=>p.stateGroup=="G1").ToArray();
        var fp=profiles.Where(p=>p.stateGroup=="G2").ToArray();
        var fn=profiles.Where(p=>p.stateGroup=="G3").ToArray();
        var tn=profiles.Where(p=>p.stateGroup=="G4").ToArray();

        double fpES=fp.Length>=2&&tp.Length>=2?EffSz(fp.Select(p=>p.rebMagnitude).ToArray(),tp.Select(p=>p.rebMagnitude).ToArray()):0;
        double fnES=fn.Length>=2&&tn.Length>=2?EffSz(fn.Select(p=>p.rebMagnitude).ToArray(),tn.Select(p=>p.rebMagnitude).ToArray()):0;

        // Holdout stability
        var hoTP=profiles.Where(p=>p.stateGroup=="G1"&&p.cohort==1).ToArray();
        var hoFP=profiles.Where(p=>p.stateGroup=="G2"&&p.cohort==1).ToArray();
        double hoES=hoFP.Length>=2&&hoTP.Length>=2?EffSz(hoFP.Select(p=>p.rebMagnitude).ToArray(),hoTP.Select(p=>p.rebMagnitude).ToArray()):0;

        // Correlation with M3+
        var train=profiles.Where(p=>p.cohort==0).ToArray();
        double maxR=Math.Max(Math.Abs(Corr(GetVals(train,"rebMagnitude"),GetVals(train,"dMean"))),
            Math.Abs(Corr(GetVals(train,"rebMagnitude"),GetVals(train,"projHiVec"))));

        // Proxy
        double proxyR=Math.Abs(Corr(GetVals(train,"rebMagnitude"),GetVals(train,"dVelocity")));
        bool proxy=proxyR>0.3;

        // N-specific
        var n72=profiles.Where(p=>p.n==72&&p.predM3p).ToArray();
        double n72ES=n72.Count(p=>p.persist)>=2&&n72.Count(p=>!p.persist)>=2?
            EffSz(n72.Where(p=>p.persist).Select(p=>p.rebMagnitude).ToArray(),n72.Where(p=>!p.persist).Select(p=>p.rebMagnitude).ToArray()):0;

        _o.WriteLine($"\nGate A — rebMagnitude Explains: {(fpES>0.5||fnES>0.5?$"REACHED — FP/TP ES={fpES:F2} FN/TN ES={fnES:F2}":"NOT REACHED")}");
        _o.WriteLine($"Gate B — Adds Beyond M3+: {(maxR<0.3?$"REACHED — max|r|={maxR:F2}":"NOT REACHED — redundant with M3+")}");
        _o.WriteLine($"Gate C — Not Pre-Actionable: {(!proxy?"REACHED — no pre-intervention proxy":"NOT REACHED")}");
        _o.WriteLine($"Gate D — Proxy Found: {(proxy?$"REACHED — |r|={proxyR:F2}":"NOT REACHED")}");
        _o.WriteLine($"Gate E — N-Specific: {(n72ES>0.5?$"REACHED — N=72 ES={n72ES:F2}":"WEAK")}");
        _o.WriteLine($"Gate F — Weak/Redundant: {(!(fpES>0.5||fnES>0.5)?$"REACHED — no strong signal":"NOT REACHED")}");

        _o.WriteLine($"\n─── Feature-Family Verdict ───");
        _o.WriteLine($"rebMagnitude FP/TP ES={fpES:F2} FN/TN ES={fnES:F2} Holdout ES={hoES:F2}");
        if(fpES>0.5&&maxR<0.3)
            _o.WriteLine("EXPLANATORY AND ACTIONABLE — rebMagnitude explains residuals independently from M3+.");
        else if(fpES>0.5)
            _o.WriteLine("EXPLANATORY BUT REDUNDANT — signal exists but overlaps with M3+ features.");
        else if(maxR<0.3)
            _o.WriteLine("INDEPENDENT BUT WEAK — no overlap with M3+ but insufficient residual separation.");
        else
            _o.WriteLine("WEAK/REDUNDANT — neither independent nor strongly separating.");

        _o.WriteLine(proxy?"Pre-intervention proxy exists → potentially actionable.":"No pre-intervention proxy → explanatory only, not controllable.");

        _o.WriteLine("\n─── Next: EGI or EGS ───");
        _o.WriteLine(fpES>0.5&&proxy?"→ EGI: Validate proxy-based control.":"→ EGS: Final synthesis — document response-dynamics findings.");
    }

    // ══════════════════════════════════════════════════════════════
    static RDProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    RDProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();AssignGroups(p);ClassifyErrModes(p);_profiles=p;return p;}

    RDProfile[] CollectProfiles(){var b=new ConcurrentBag<RDProfile>();foreach(var n in new[]{67,71,72,75,80}){var hi=Hi(n);var lo=Lo(n);PC(n,0,99,hi,lo,0,b);PC(n,100,199,hi,lo,1,b);}return b.ToArray();}
    void PC(int n,int s,int e,P3 hi,P3 lo,int coh,ConcurrentBag<RDProfile> b){var seeds=new List<int>();for(int i=s;i<=e&&seeds.Count<MaxSeedsPerCohort;i++)if(!IsHi(n,i))seeds.Add(i);Parallel.ForEach(seeds.ToArray(),sd=>{b.Add(ProfileOne(n,sd,hi,lo,coh));});}

    RDProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh){
        var K=KS(n,seed);var dH=new List<double>();
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);dH.Add(Dm(d,n));}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double d2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
        double orth=Math.Sqrt(Math.Max(0,d2-proj*proj));
        var edges=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K3E[i,j]);
        double avg=edges.Average(),dStd=edges.Count>0?Math.Sqrt(edges.Sum(x=>(x-avg)*(x-avg))/edges.Count):0;
        double dVel=dH.Count>=2?dH[^1]-dH[^2]:0;
        // Topology/motif
        double thr=edges.OrderBy(x=>x).ElementAt(edges.Count/2);int hoC=0;double ed=0;
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){if(K3E[i,j]>thr)hoC++;ed+=K3E[i,j];}
        // Intervention
        bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K2,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,seed+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,seed+100);double om1=Of(hT1,n).Average(),dT1=Dm(DL(Nm(RP(hT1,n),n),n),n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average(),dT2=Dm(DL(Nm(RP(hT2,n),n),n),n);
        bool pers=om2>THR,imm=om1>THR;
        double rebMag=dT2-dT1,dRespE=Math.Abs(dT1-d0)>1e-9?Math.Abs(om1-om2)/Math.Abs(dT1-d0):0;
        double dispAlong=vn>0?((dT1-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn-proj:0;
        double dispOrth=Math.Sqrt(Math.Max(0,(dT1-lo.dm)*(dT1-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks)-dispAlong*dispAlong));
        double actComp=d0>0?(d0-dT1)/d0:0,deltaOmDD=Math.Abs(dT1-d0)>1e-9?Math.Abs(om1-om2)/Math.Abs(dT1-d0):0;
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        bool pM3p=n==75?isP1:(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV);
        return new RDProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0,dStd=dStd,dVelocity=dVel,
            projHiVec=proj,orthHiVec=orth,lambda1=PowerIteration(K3E,n,200),
            kFrob=Math.Sqrt(edges.Sum(x=>x*x)),edgeDensity=ed/(n*(n-1)/2.0),loOverlap=hoC/(double)Math.Max(1,n*(n-1)/2),
            omT1=om1,omT2=om2,dT1=dT1,dT2=dT2,immHi=imm,persist=pers,predM3p=pM3p,
            rebMagnitude=rebMag,dRespElast=dRespE,dispAlong=dispAlong,dispOrth=dispOrth,
            actualCompFrac=actComp,deltaOmDeltaD=deltaOmDD,immIndRate=dT1/d0,stateGroup="G0",errMode=""};
    }

    void AssignGroups(RDProfile[] p){for(int i=0;i<p.Length;i++){
        if(p[i].persist&&p[i].predM3p)p[i].stateGroup="G1";else if(!p[i].persist&&p[i].predM3p)p[i].stateGroup="G2";
        else if(!p[i].persist&&!p[i].predM3p)p[i].stateGroup="G4";else if(p[i].n==75)p[i].stateGroup="G5";
        else p[i].stateGroup="G3";}}

    void ClassifyErrModes(RDProfile[] p){for(int i=0;i<p.Length;i++){
        if(p[i].persist){p[i].errMode="none";continue;}
        if(!p[i].immHi)p[i].errMode="insufDisp";
        else if(p[i].rebMagnitude>0.3)p[i].errMode="highRebound";
        else if(p[i].rebMagnitude<-0.1)p[i].errMode="lowRebound";
        else if(p[i].omT2<THR*0.5)p[i].errMode="K_collapse";
        else if(p[i].dMean>0.85)p[i].errMode="overRebound";
        else p[i].errMode="unresolved";}}

    // Helpers
    static double EffSz(double[] a,double[] b){double ma=a.Average(),mb=b.Average();double sa=Math.Sqrt(a.Sum(x=>(x-ma)*(x-ma))/a.Length),sb=Math.Sqrt(b.Sum(x=>(x-mb)*(x-mb))/b.Length);double pl=Math.Sqrt((sa*sa+sb*sb)/2);return pl>1e-12?Math.Abs(ma-mb)/pl:0;}
    static double Corr(double[] a,double[] b){int n=Math.Min(a.Length,b.Length);double ma=a.Take(n).Average(),mb=b.Take(n).Average();double sa=0,sb=0,sc=0;for(int i=0;i<n;i++){double da=a[i]-ma,db=b[i]-mb;sa+=da*da;sb+=db*db;sc+=da*db;}return sc/Math.Sqrt(Math.Max(sa*sb,1e-30));}
    static double Median(double[] v){var s=v.OrderBy(x=>x).ToArray();return s[s.Length/2];}
    static double Std(double[] v){double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double FindBestThresh(RDProfile[] p,Func<RDProfile,double> f){double best=0,acc=0;var s=p.OrderBy(x=>f(x)).ToArray();for(int i=1;i<s.Length;i++){double t=(f(s[i-1])+f(s[i]))/2;int a=s.Count(x=>f(x)>t&&x.persist),b=s.Count(x=>f(x)<=t&&!x.persist);double ac=(double)(a+b)/s.Length;if(ac>acc){acc=ac;best=t;}}return best;}
    double[] GetVals(RDProfile[] p,string n)=>n switch{"rebMagnitude"=>p.Select(x=>x.rebMagnitude).ToArray(),"dMean"=>p.Select(x=>x.dMean).ToArray(),"dStd"=>p.Select(x=>x.dStd).ToArray(),"projHiVec"=>p.Select(x=>x.projHiVec).ToArray(),"orthHiVec"=>p.Select(x=>x.orthHiVec).ToArray(),"dVelocity"=>p.Select(x=>x.dVelocity).ToArray(),"edgeDensity"=>p.Select(x=>x.edgeDensity).ToArray(),"loOverlap"=>p.Select(x=>x.loOverlap).ToArray(),"lambda1"=>p.Select(x=>x.lambda1).ToArray(),"kFrob"=>p.Select(x=>x.kFrob).ToArray(),"omT2"=>p.Select(x=>x.omT2).ToArray(),_=>new double[p.Length]};
    double GetVal(RDProfile p,string n)=>n switch{"rebMagnitude"=>p.rebMagnitude,"dMean"=>p.dMean,"dStd"=>p.dStd,"projHiVec"=>p.projHiVec,"orthHiVec"=>p.orthHiVec,"dVelocity"=>p.dVelocity,"edgeDensity"=>p.edgeDensity,"loOverlap"=>p.loOverlap,"lambda1"=>p.lambda1,"kFrob"=>p.kFrob,_=>0};

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
    static double PowerIteration(double[,]A,int n,int maxIter=200){var v=new double[n];new Random(42);for(int i=0;i<n;i++)v[i]=new Random(42+i).NextDouble();double norm=Math.Sqrt(v.Sum(x=>x*x));if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];norm=Math.Sqrt(Av.Sum(x=>x*x));if(norm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/norm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
}
