using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_15;

[Trait("Category","V5_15"),Trait("Category","V5_15_CVA"),Trait("Category","LongRunning")]
public class V5_15_ResidualVarianceAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct VarProfile{
        public int seed,cohort,n;public string cls,errGroup;
        public double projHiVec,orthHiVec,dMean,dStd,dVelocity,omT1,omT2,lambda1,top1ES,distToHi;
        public bool immHi,persist,predM0,predM3,predM3p;
        public string failMode;
    }

    public V5_15_ResidualVarianceAnalysis_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void CVA_01_ConfusionDecomposition(){
        _o.WriteLine("═══ CVA_01: Confusion decomposition with N=75 bypass ═══");
        var profiles=CollectProfiles();

        // Classify error groups
        ClassifyErrors(profiles);

        // Overall confusion
        var tp=profiles.Where(p=>p.errGroup=="E1").ToArray();
        var fp=profiles.Where(p=>p.errGroup=="E2").ToArray();
        var tn=profiles.Where(p=>p.errGroup=="E3").ToArray();
        var fn=profiles.Where(p=>p.errGroup=="E4"||p.errGroup=="E5").ToArray();
        _o.WriteLine($"\nM3+ confusion (N=75 bypass): TP={tp.Length} FP={fp.Length} TN={tn.Length} FN={fn.Length}");
        double prec=(double)tp.Length/(tp.Length+fp.Length),rec=(double)tp.Length/(tp.Length+fn.Length);
        double spec=(double)tn.Length/(tn.Length+fp.Length),acc=(double)(tp.Length+tn.Length)/profiles.Length;
        _o.WriteLine($"Precision={prec:P1} Recall={rec:P1} Specificity={spec:P1} Accuracy={acc:P1}");

        // Per-N decomposition
        _o.WriteLine(string.Format("\n{0,4} {1,10} {2,4} {3,4} {4,4} {5,4} {6,6} {7,6} {8,6} {9,6}",
            "N","Cohort","TP","FP","TN","FN","Prec","Rec","FPR","FNR"));
        foreach(var n in new[]{67,71,72,75,80}){
            foreach(var coh in new[]{0,1}){
                var s=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                int t=s.Count(p=>p.errGroup=="E1"),fps=s.Count(p=>p.errGroup=="E2");
                int tns=s.Count(p=>p.errGroup=="E3"),fns=s.Count(p=>p.errGroup=="E4"||p.errGroup=="E5");
                double p=t+fps>0?(double)t/(t+fps):0,r=t+fns>0?(double)t/(t+fns):0;
                double fpr=tns+fps>0?(double)fps/(tns+fps):0,fnr=t+fns>0?(double)fns/(t+fns):0;
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {t,4} {fps,4} {tns,4} {fns,4} {p,5:P0} {r,6:P0} {fpr,5:P0} {fnr,6:P0}");
            }
        }

        _profiles=profiles;
    }

    [Fact]public void CVA_02_FalseNegativeAnalysis(){
        _o.WriteLine("═══ CVA_02: False negative analysis ═══");
        var profiles=EnsureProfiles();

        var fn=profiles.Where(p=>p.errGroup=="E4"||p.errGroup=="E5").ToArray();
        _o.WriteLine($"\nTotal FN: {fn.Length} (E4={fn.Count(p=>p.errGroup=="E4")} true FN, E5={fn.Count(p=>p.errGroup=="E5")} N=75 bypass FN)");

        // FN by N
        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,8} {5,10}",
            "N","E4","E5","Total","dMean","Cause"));
        foreach(var n in new[]{67,71,72,75,80}){
            int e4=fn.Count(p=>p.n==n&&p.errGroup=="E4"),e5=fn.Count(p=>p.n==n&&p.errGroup=="E5");
            var nfn=fn.Where(p=>p.n==n).ToArray();
            double dm=nfn.Length>0?nfn.Average(p=>p.dMean):0;
            string cause=n==75?"BYPASS_MISMATCH":n==67?"INACCESSIBLE":n==80?"SATURATED":"OVER_FILTER";
            _o.WriteLine($"{n,4} {e4,6} {e5,6} {e4+e5,6} {dm,8:F4} {cause,10}");
        }

        // FN features vs TP
        var tp=profiles.Where(p=>p.errGroup=="E1").ToArray();
        _o.WriteLine("\n─── FN vs TP features ───");
        _o.WriteLine(string.Format("{0,-14} {1,8} {2,8} {3,8}",
            "Feature","FN_Mean","TP_Mean","EffSize"));
        Compare(fn,tp,("projHiVec",p=>p.projHiVec),("orthHiVec",p=>p.orthHiVec),
            ("dMean",p=>p.dMean),("dStd",p=>p.dStd),("dVelocity",p=>p.dVelocity),
            ("omT1",p=>p.omT1),("lambda1",p=>p.lambda1));

        // FN failure modes
        _o.WriteLine("\n─── FN failure modes ───");
        ClassifyFailures(profiles);
        var modes=new[]{"insufDisp","overComp","K_collapse","overFilter","bypass","saturated","inaccessible","unresolved"};
        foreach(var m in modes){
            int c=fn.Count(p=>p.failMode==m);
            if(c>0)_o.WriteLine($"  {m}: {c} ({c*100.0/fn.Length:F0}%)");
        }
    }

    [Fact]public void CVA_03_FalsePositiveAnalysis(){
        _o.WriteLine("═══ CVA_03: False positive analysis ═══");
        var profiles=EnsureProfiles();

        var fp=profiles.Where(p=>p.errGroup=="E2").ToArray();
        _o.WriteLine($"\nTotal FP: {fp.Length}");

        // FP by N
        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,8} {3,8} {4,10}",
            "N","Count","dMean","omT1","Dominant"));
        foreach(var n in new[]{67,71,72,75,80}){
            var nfp=fp.Where(p=>p.n==n).ToArray();
            if(nfp.Length==0)continue;
            double dm=nfp.Average(p=>p.dMean),om=nfp.Average(p=>p.omT1);
            string dom=nfp.Count(p=>!p.immHi)>nfp.Length/2?"insufDisp":(nfp.Average(p=>p.omT2)<THR*0.5?"K_collapse":"mixed");
            _o.WriteLine($"{n,4} {nfp.Length,6} {dm,8:F4} {om,8:F4} {dom,10}");
        }

        // FP vs TN
        var tn=profiles.Where(p=>p.errGroup=="E3").ToArray();
        _o.WriteLine("\n─── FP vs TN features ───");
        _o.WriteLine(string.Format("{0,-14} {1,8} {2,8} {3,8}",
            "Feature","FP_Mean","TN_Mean","EffSize"));
        Compare(fp,tn,("projHiVec",p=>p.projHiVec),("orthHiVec",p=>p.orthHiVec),
            ("dMean",p=>p.dMean),("dStd",p=>p.dStd),("omT1",p=>p.omT1),
            ("lambda1",p=>p.lambda1));

        // FP failure modes
        ClassifyFailures(profiles);
        var modes=new[]{"insufDisp","overComp","K_collapse","d_rebound","unresolved"};
        foreach(var m in modes){
            int c=fp.Count(p=>p.failMode==m);
            if(c>0)_o.WriteLine($"  {m}: {c} ({c*100.0/fp.Length:F0}%)");
        }
    }

    [Fact]public void CVA_04_N75Analysis(){
        _o.WriteLine("═══ CVA_04: N=75 residual analysis ═══");
        var profiles=EnsureProfiles();

        var n75=profiles.Where(p=>p.n==75).ToArray();
        var n75P1=n75.Where(p=>p.cls=="P1"||p.cls=="P1b").ToArray();
        var n75Succ=n75P1.Where(p=>p.persist).ToArray();
        var n75Fail=n75P1.Where(p=>!p.persist).ToArray();

        _o.WriteLine($"\nN=75: {n75P1.Length} P1/P1b candidates, {n75Succ.Length} persist ({n75Succ.Length*100.0/n75P1.Length:F0}%)");

        // M3+ with bypass (all P1/P1b = predict success)
        int bypassPred=n75P1.Length,bypassCorr=n75Succ.Length;
        double bypassRate=(double)bypassCorr/bypassPred;
        _o.WriteLine($"M3+ N=75 bypass: predict ALL P1/P1b ({bypassPred}), correct={bypassCorr} ({bypassRate:P0})");

        // M3+ without bypass (projHiVec filter only)
        var m3Sel=n75P1.Where(p=>p.projHiVec>PHV).ToArray();
        int m3Corr=m3Sel.Count(p=>p.persist);
        double m3Rate=(double)m3Corr/Math.Max(1,m3Sel.Length);
        _o.WriteLine($"M3+ N=75 no-bypass: select {m3Sel.Length}, correct={m3Corr} ({m3Rate:P0})");

        // N=75 success vs failure separation
        _o.WriteLine("\n─── N=75 success vs failure features ───");
        _o.WriteLine(string.Format("{0,-14} {1,8} {2,8} {3,8}",
            "Feature","Succ","Fail","EffSize"));
        Compare(n75Succ,n75Fail,("dMean",p=>p.dMean),("dStd",p=>p.dStd),
            ("dVelocity",p=>p.dVelocity),("orthHiVec",p=>p.orthHiVec),
            ("projHiVec",p=>p.projHiVec),("lambda1",p=>p.lambda1),
            ("omT1",p=>p.omT1),("top1%",p=>p.top1ES));

        // N=75 model recommendation
        _o.WriteLine("\n─── N=75 recommendation ───");
        if(bypassRate>m3Rate+0.05)_o.WriteLine($"BYPASS superior: {bypassRate:P0} > {m3Rate:P0}. Use M3 bypass (all P1/P1b).");
        else if(m3Rate>bypassRate+0.05)_o.WriteLine($"SELECTOR superior: {m3Rate:P0} > {bypassRate:P0}. Use projHiVec at N=75.");
        else _o.WriteLine($"EQUIVALENT: bypass={bypassRate:P0} vs selector={m3Rate:P0}. Either works.");
    }

    [Fact]public void CVA_05_ResidualStructureAndGates(){
        _o.WriteLine("═══ CVA_05: Residual structure verdict and decision gates ═══");
        var profiles=EnsureProfiles();

        var tp=profiles.Where(p=>p.errGroup=="E1").ToArray();
        var fp=profiles.Where(p=>p.errGroup=="E2").ToArray();
        var fn=profiles.Where(p=>p.errGroup=="E4"||p.errGroup=="E5").ToArray();

        // Structural tests
        bool fnStructured=false;
        if(fn.Length>=5&&tp.Length>=5){
            double esOrth=EffSz(fn.Select(p=>p.orthHiVec).ToArray(),tp.Select(p=>p.orthHiVec).ToArray());
            double esDm=EffSz(fn.Select(p=>p.dMean).ToArray(),tp.Select(p=>p.dMean).ToArray());
            fnStructured=esOrth>0.3||esDm>0.3;
        }
        bool fpStructured=false;
        if(fp.Length>=5){
            double esOm=EffSz(fp.Where(p=>!p.immHi).Select(p=>p.omT1).DefaultIfEmpty(0).ToArray(),fp.Where(p=>p.immHi).Select(p=>p.omT1).DefaultIfEmpty(0).ToArray());
            fpStructured=fp.Count(p=>!p.immHi)>fp.Length*0.5; // majority are insufficient displacement
        }

        bool fnDominant=fn.Length>fp.Length;
        bool n75Sep=(profiles.Where(p=>p.n==75&&p.cls=="P1"||p.cls=="P1b").Count()>0);
        bool n7172Expl=fn.Where(p=>p.n==71||p.n==72).Count()<fn.Where(p=>p.n==75).Count();
        bool ceiling=!fnStructured&&!fpStructured;

        _o.WriteLine($"\nGate A — Structurally modelable: {(fnStructured||fpStructured?"REACHED":"NOT REACHED")} — {(fnStructured?"FN structured":"")} {(fpStructured?"FP structured":"")}");
        _o.WriteLine($"Gate B — FN dominate: {(fnDominant?$"REACHED — FN={fn.Length} > FP={fp.Length}":"NOT REACHED")}");
        _o.WriteLine($"Gate C — FP dominate: {(!fnDominant&&fp.Length>0?$"REACHED — FP={fp.Length}":"NOT REACHED")}");
        _o.WriteLine($"Gate D — N=75 separate model: {((profiles.Where(p=>p.n==75).Count(p=>p.errGroup=="E5")>5)?"REACHED":"MARGINAL")} — E5 bypass FNs exist");
        _o.WriteLine($"Gate E — N=71/72 residuals explained: {(n7172Expl?"PARTIAL":"NOT REACHED")}");
        _o.WriteLine($"Gate F — Ceiling supported: {(ceiling?$"REACHED — no strong residual structure":"NOT REACHED — residual structure exists")}");

        _o.WriteLine("\n─── Residual Structure Verdict ───");
        _o.WriteLine($"FN structured: {fnStructured} | FP structured: {fpStructured}");
        if(fnStructured)_o.WriteLine("False negatives show moderate structural signal — over-filtering is feature-correlated.");
        else _o.WriteLine("False negatives are weakly structured — near noise floor.");
        if(fpStructured)_o.WriteLine("False positives dominated by insufficient displacement — predictable failure mode.");
        else _o.WriteLine("False positives have no dominant structure.");

        _o.WriteLine("\n─── Claim Discipline ───");
        _o.WriteLine("No new selectors proposed. No thresholds tuned. No physical interpretation.");
        _o.WriteLine(ceiling?"Model near practical ceiling. Residual variance mostly unstructured.":"Residual structure exists but is weak. Marginal improvement possible.");
        _o.WriteLine("\n─── Next: CVI or CVS ───");
        if(ceiling)_o.WriteLine("→ CVS: Final synthesis — declare near-ceiling.");
        else _o.WriteLine("→ CVI: Control-limit audit — test if structured residuals are actionable.");
    }

    // ══════════════════════════════════════════════════════════════
    static VarProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    VarProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();ClassifyErrors(p);_profiles=p;return p;}
    VarProfile[] CollectProfiles(){var b=new ConcurrentBag<VarProfile>();foreach(var n in new[]{67,71,72,75,80}){var hi=Hi(n);var lo=Lo(n);PC(n,0,99,hi,lo,0,b);PC(n,100,199,hi,lo,1,b);}return b.ToArray();}

    void PC(int n,int s,int e,P3 hi,P3 lo,int coh,ConcurrentBag<VarProfile> b){
        var seeds=new List<int>();for(int i=s;i<=e&&seeds.Count<MaxSeedsPerCohort;i++)if(!IsHi(n,i))seeds.Add(i);
        Parallel.ForEach(seeds.ToArray(),sd=>{b.Add(ProfileOne(n,sd,hi,lo,coh));});
    }
    VarProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh){
        var K=KS(n,seed);var dH=new List<double>();
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);dH.Add(Dm(d,n));}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double distLo2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
        double orth=Math.Sqrt(Math.Max(0,distLo2-proj*proj));
        double distHi=Math.Sqrt((d0-hi.dm)*(d0-hi.dm)+(km-hi.km)*(km-hi.km)+(ks-hi.ks)*(ks-hi.ks));
        var edges=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K3E[i,j]);
        double avg=edges.Average(),dStd=edges.Count>0?Math.Sqrt(edges.Sum(x=>(x-avg)*(x-avg))/edges.Count):0;
        edges.Sort((a,b)=>b.CompareTo(a));double tot=edges.Sum(),t1=tot>0?edges.Take(Math.Max(1,(int)(edges.Count*0.01))).Sum()/tot:0;
        double l1=PowerIteration(K3E,n,200),dVel=dH.Count>=2?dH[^1]-dH[^2]:0;
        bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K2,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,seed+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,seed+100);double om1=Of(hT1,n).Average();
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average();
        bool pers=om2>THR,imm=om1>THR,isP1=sb.cls=="P1"||sb.cls=="P1b";
        // M3+ with N=75 bypass
        bool pM3p;
        if(n==75)pM3p=isP1; // bypass — all P1/P1b at N=75
        else if(n==72)pM3p=isP1&&proj>PHV&&orth>OTH;
        else pM3p=isP1&&proj>PHV; // N=71 or other: use M3 selector
        return new VarProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0,projHiVec=proj,orthHiVec=orth,
            dStd=dStd,dVelocity=dVel,omT1=om1,omT2=om2,lambda1=l1,top1ES=t1,distToHi=distHi,
            immHi=imm,persist=pers,predM0=isP1,predM3=isP1&&proj>PHV,predM3p=pM3p,errGroup="G0"};
    }

    void ClassifyErrors(VarProfile[] p){
        for(int i=0;i<p.Length;i++){
            if(p[i].persist&&p[i].predM3p)p[i].errGroup="E1";      // TP
            else if(!p[i].persist&&p[i].predM3p)p[i].errGroup="E2"; // FP
            else if(!p[i].persist&&!p[i].predM3p)p[i].errGroup="E3";// TN
            else if(p[i].n==75)p[i].errGroup="E5";                  // N=75 bypass FN
            else if(p[i].n==80)p[i].errGroup="E6";                  // saturated FN
            else if(p[i].n==67)p[i].errGroup="E7";                  // inaccessible FN
            else p[i].errGroup="E4";                                 // true FN
        }
    }
    void ClassifyFailures(VarProfile[] p){
        for(int i=0;i<p.Length;i++){
            if(p[i].persist){p[i].failMode="none";continue;}
            if(p[i].n==67)p[i].failMode="inaccessible";
            else if(p[i].n==80)p[i].failMode="saturated";
            else if(!p[i].immHi)p[i].failMode="insufDisp";
            else if(p[i].dMean>0.85)p[i].failMode="overComp";
            else if(p[i].omT2<THR*0.5)p[i].failMode="K_collapse";
            else if(p[i].omT2>p[i].omT1)p[i].failMode="d_rebound";
            else p[i].failMode="unresolved";
        }
    }
    void Compare(VarProfile[] a,VarProfile[] b,params (string,Func<VarProfile,double>)[] fs){
        foreach(var (n,f) in fs){
            if(a.Length<2||b.Length<2)continue;
            double ma=a.Average(f),mb=b.Average(f);
            double es=EffSz(a.Select(f).ToArray(),b.Select(f).ToArray());
            _o.WriteLine($"  {n,-14} {ma,8:F4} {mb,8:F4} {es,8:F3}");
        }
    }
    static Func<VarProfile,double> F(Func<VarProfile,double> f)=>f;
    static double EffSz(double[] a,double[] b){double ma=a.Average(),mb=b.Average();double sa=Math.Sqrt(a.Sum(x=>(x-ma)*(x-ma))/a.Length),sb=Math.Sqrt(b.Sum(x=>(x-mb)*(x-mb))/b.Length);double p=Math.Sqrt((sa*sa+sb*sb)/2);return p>1e-12?Math.Abs(ma-mb)/p:0;}

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
    static double PowerIteration(double[,]A,int n,int maxIter=200){var v=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v[i]=rng.NextDouble();double norm=Math.Sqrt(v.Sum(x=>x*x));if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];norm=Math.Sqrt(Av.Sum(x=>x*x));if(norm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/norm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
}
