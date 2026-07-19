using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_15;

[Trait("Category","V5_15"),Trait("Category","V5_15_CVE"),Trait("Category","LongRunning")]
public class V5_15_ControlVarianceExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct VarProfile{
        public int seed,cohort,n;public string cls;
        public double projHiVec,orthHiVec,dMean,omT1,omT2;
        public bool immHi,persist;
        public bool predM0,predM3,predM3p;
    }

    public V5_15_ControlVarianceExecution_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void CVE_01_VarianceDecomposition(){
        _o.WriteLine("═══ CVE_01: Variance decomposition ═══");
        var profiles=CollectProfiles();

        // Count outcomes
        int total=profiles.Length,persist=profiles.Count(p=>p.persist),fail=total-persist;
        _o.WriteLine($"Total seeds: {total} | Persist: {persist} ({persist*100.0/total:F1}%) | Fail: {fail} ({fail*100.0/total:F1}%)");

        // Model predictions
        int m0Pred=profiles.Count(p=>p.predM0),m3Pred=profiles.Count(p=>p.predM3),m3pPred=profiles.Count(p=>p.predM3p);
        int m0Correct=profiles.Count(p=>p.predM0&&p.persist),m3Correct=profiles.Count(p=>p.predM3&&p.persist),m3pCorrect=profiles.Count(p=>p.predM3p&&p.persist);

        _o.WriteLine("\n─── Model prediction counts ───");
        _o.WriteLine(string.Format("{0,-12} {1,6} {2,8} {3,8} {4,8}",
            "Model","Pred","Correct","Rate","Explain%"));
        double baseRate=(double)persist/total;
        double e0=m0Pred>0?((double)m0Correct/m0Pred-baseRate)/(1-baseRate)*100:0;
        double e3=m3Pred>0?((double)m3Correct/m3Pred-baseRate)/(1-baseRate)*100:0;
        double e3p=m3pPred>0?((double)m3pCorrect/m3pPred-baseRate)/(1-baseRate)*100:0;
        double r0=(double)m0Correct/Math.Max(1,m0Pred),r3=(double)m3Correct/Math.Max(1,m3Pred),r3p=(double)m3pCorrect/Math.Max(1,m3pPred);
        _o.WriteLine(string.Format("{0,-12} {1,6} {2,8} {3,7:P0} {4,8:F1}%","Model","Pred","Correct","Rate","Explain%"));
        _o.WriteLine(string.Format("{0,-12} {1,6} {2,8} {3,7:P0} {4,8:F1}%",M0,m0Pred,m0Correct,r0,e0));
        _o.WriteLine(string.Format("{0,-12} {1,6} {2,8} {3,7:P0} {4,8:F1}%",M3,m3Pred,m3Correct,r3,e3));
        _o.WriteLine(string.Format("{0,-12} {1,6} {2,8} {3,7:P0} {4,8:F1}%",M3p,m3pPred,m3pCorrect,r3p,e3p));

        // Per-N variance decomposition
        _o.WriteLine("\n─── Per-N M3+ performance ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,6} {4,8} {5,8} {6,8}",
            "N","Total","Pred","Corr","Rate","Resid%","Expl%"));
        foreach(var n in new[]{67,71,72,75,80}){
            var sub=profiles.Where(p=>p.n==n).ToArray();
            int t=sub.Length,p=sub.Count(pp=>pp.persist);
            int pred=sub.Count(pp=>pp.predM3p),corr=sub.Count(pp=>pp.predM3p&&pp.persist);
            double rate=pred>0?(double)corr/pred:0;
            double resid=(double)(t-corr)/t;
            double expl=(double)corr/Math.Max(1,p);
            _o.WriteLine($"{n,4} {t,6} {pred,6} {corr,6} {rate,7:P0} {resid,8:P0} {expl,8:P0}");
        }

        _profiles=profiles;
    }

    [Fact]public void CVE_02_ResidualVariance(){
        _o.WriteLine("═══ CVE_02: Residual variance analysis ═══");
        var profiles=EnsureProfiles();

        // Residual = seeds where M3+ predicts success but fails, or predicts failure but succeeds
        var m3pFP=profiles.Where(p=>p.predM3p&&!p.persist).ToArray(); // false positive
        var m3pFN=profiles.Where(p=>!p.predM3p&&p.persist).ToArray(); // false negative
        var m3pTP=profiles.Where(p=>p.predM3p&&p.persist).ToArray();  // true positive
        var m3pTN=profiles.Where(p=>!p.predM3p&&!p.persist).ToArray();// true negative

        _o.WriteLine($"\nM3+ confusion matrix:");
        _o.WriteLine($"  TP={m3pTP.Length}  FP={m3pFP.Length}");
        _o.WriteLine($"  FN={m3pFN.Length}  TN={m3pTN.Length}");
        double acc=(double)(m3pTP.Length+m3pTN.Length)/profiles.Length;
        _o.WriteLine($"  Accuracy: {acc:P1}");

        // Where are residuals?
        _o.WriteLine("\n─── Residual concentration by N ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,6} {3,6} {4,8} {5,8}",
            "N","FP","FN","Total","FP/N","FN/N"));
        foreach(var n in new[]{67,71,72,75,80}){
            int fp=m3pFP.Count(p=>p.n==n),fn=m3pFN.Count(p=>p.n==n);
            var sub=profiles.Where(p=>p.n==n).ToArray();
            _o.WriteLine($"{n,4} {fp,6} {fn,6} {fp+fn,6} {(double)fp/sub.Length,7:P0} {(double)fn/sub.Length,8:P0}");
        }

        // Residual by cohort
        _o.WriteLine("\n─── Residual by cohort ───");
        foreach(var coh in new[]{0,1}){
            int fp=m3pFP.Count(p=>p.cohort==coh),fn=m3pFN.Count(p=>p.cohort==coh);
            var sub=profiles.Where(p=>p.cohort==coh).ToArray();
            _o.WriteLine($"Cohort {coh}: FP={fp} FN={fn} of {sub.Length} (err={(double)(fp+fn)/sub.Length:P1})");
        }

        // Residual by pathway class
        _o.WriteLine("\n─── Residual by pathway class ───");
        foreach(var cls in new[]{"P1b","P1","P2","P3","P4"}){
            int fp=m3pFP.Count(p=>p.cls==cls),fn=m3pFN.Count(p=>p.cls==cls);
            var sub=profiles.Where(p=>p.cls==cls).ToArray();
            if(sub.Length==0)continue;
            _o.WriteLine($"{cls}: FP={fp} FN={fn} of {sub.Length} ({fp+fn} errors, {(double)(fp+fn)/sub.Length:P0})");
        }
    }

    [Fact]public void CVE_03_ErrorStructureTest(){
        _o.WriteLine("═══ CVE_03: Error structure test ═══");
        var profiles=EnsureProfiles();

        // Are M3+ failures structurally different from M3+ successes?
        var m3pFP=profiles.Where(p=>p.predM3p&&!p.persist).ToArray();
        var m3pTP=profiles.Where(p=>p.predM3p&&p.persist).ToArray();

        _o.WriteLine($"\nM3+ predicted positive: {m3pTP.Length+m3pFP.Length} (TP={m3pTP.Length}, FP={m3pFP.Length})");

        if(m3pFP.Length>=3&&m3pTP.Length>=3){
            _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,8} {4,8}",
                "Feature","TP","FP","Diff","ES"));
            var feats=new[]{("dMean",F(p=>p.dMean)),("projHiVec",F(p=>p.projHiVec)),
                ("orthHiVec",F(p=>p.orthHiVec)),("omT1",F(p=>p.omT1))};
            foreach(var (name,f) in feats){
                double tp=m3pTP.Average(f),fp=m3pFP.Average(f);
                double es=EffSz(m3pTP.Select(f).ToArray(),m3pFP.Select(f).ToArray());
                _o.WriteLine($"{name,-14} {tp,8:F4} {fp,8:F4} {tp-fp,8:+0.0000} {es,8:F3}");
            }
        }

        // Are M3+ false negatives structurally different?
        var m3pFN=profiles.Where(p=>!p.predM3p&&p.persist).ToArray();
        var m3pTN=profiles.Where(p=>!p.predM3p&&!p.persist).ToArray();

        _o.WriteLine($"\nM3+ predicted negative: {m3pFN.Length+m3pTN.Length} (FN={m3pFN.Length}, TN={m3pTN.Length})");

        if(m3pFN.Length>=3&&m3pTN.Length>=3){
            _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,8} {4,8}",
                "Feature","FN","TN","Diff","ES"));
            var feats=new[]{("dMean",F(p=>p.dMean)),("projHiVec",F(p=>p.projHiVec)),
                ("orthHiVec",F(p=>p.orthHiVec)),("omT1",F(p=>p.omT1))};
            foreach(var (name,f) in feats){
                double fn=m3pFN.Average(f),tn=m3pTN.Average(f);
                double es=EffSz(m3pFN.Select(f).ToArray(),m3pTN.Select(f).ToArray());
                _o.WriteLine($"{name,-14} {fn,8:F4} {tn,8:F4} {fn-tn,8:+0.0000} {es,8:F3}");
            }
        }

        _o.WriteLine("\n─── Structure assessment ───");
        _o.WriteLine("If FP/TP or FN/TN effect sizes are small (<0.3), residuals are weakly structured.");
    }

    [Fact]public void CVE_04_CeilingAssessmentAndGates(){
        _o.WriteLine("═══ CVE_04: Ceiling assessment and decision gates ═══");
        var profiles=EnsureProfiles();

        // Compute holdout residuals only
        var ho=profiles.Where(p=>p.cohort==1).ToArray();
        var hoP=ho.Count(p=>p.persist);
        var hoPred=ho.Where(p=>p.predM3p).ToArray();
        var hoCorr=hoPred.Count(p=>p.persist);
        double hoExpl=(double)hoCorr/hoP;
        double hoResid=(double)(ho.Length-hoCorr-ho.Count(p=>!p.predM3p&&!p.persist))/ho.Length;

        // Per-N M3+ holdout rates
        _o.WriteLine("\n─── Holdout residual variance ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8} {4,8} {5,10}",
            "N","Total","Persist","M3+Corr","Resid","Ceiling?"));
        bool anyBelow50=false;
        foreach(var n in new[]{71,72,75}){
            var s=ho.Where(p=>p.n==n).ToArray();
            int t=s.Length,p=s.Count(pp=>pp.persist);
            int corr=s.Count(pp=>pp.predM3p&&pp.persist);
            int residP=p-corr;
            double m3pRate=s.Where(pp=>pp.predM3p).Count(pp=>pp.persist)*1.0/Math.Max(1,s.Count(pp=>pp.predM3p));
            if(m3pRate<0.5)anyBelow50=true;
            string ceilingLabel=m3pRate<0.5?"BELOW 50%":"OK";
            _o.WriteLine($"{n,4} {t,8} {p,8} {corr,8} {residP,8} {ceilingLabel,10}");
        }

        // Compute explained variance fraction
        _o.WriteLine($"\nHoldout M3+ explained persistence: {hoCorr}/{hoP} = {hoExpl:P0}");

        // Gates
        bool majorExpl=hoExpl>0.5;
        bool largeResid=hoResid>0.3;
        bool concN=(ho.Where(p=>p.n==71&&!p.predM3p&&p.persist).Count()>3||ho.Where(p=>p.n==72&&!p.predM3p&&p.persist).Count()>3);
        bool diffuse=!concN;
        bool ceiling=!majorExpl||largeResid;

        _o.WriteLine($"\nGate A — M3+ explains majority: {(majorExpl?$"REACHED — {hoExpl:P0} explained":"NOT REACHED")}");
        _o.WriteLine($"Gate B — Large residual remains: {(largeResid?$"REACHED — {hoResid:P0} residual":"NOT REACHED")}");
        _o.WriteLine($"Gate C — Residual concentrated: {(concN?"REACHED":"NOT REACHED")} — specific N windows");
        _o.WriteLine($"Gate D — Residual diffuse: {(diffuse?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E — Near ceiling: {(ceiling?$"REACHED — M3+ near ceiling":"NOT REACHED — room for improvement")}");

        _o.WriteLine("\n─── Claim discipline ───");
        _o.WriteLine("No new mechanisms. No new hidden variables. No causality claims. No physical interpretation.");
        _o.WriteLine(ceiling?"Model near explanatory ceiling. Residual variance may be noise-limited.":"Further structured variance may exist.");

        _o.WriteLine("\n─── Next: CVA_ResidualVarianceAnalysis ───");
    }

    // ══════════════════════════════════════════════════════════════
    static VarProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    VarProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();_profiles=p;return p;}

    VarProfile[] CollectProfiles(){
        var b=new ConcurrentBag<VarProfile>();
        foreach(var n in new[]{67,71,72,75,80}){var hi=Hi(n);var lo=Lo(n);PC(n,0,99,hi,lo,0,b);PC(n,100,199,hi,lo,1,b);}
        return b.ToArray();
    }
    void PC(int n,int s,int e,P3 hi,P3 lo,int coh,ConcurrentBag<VarProfile> b){
        var seeds=new List<int>();for(int i=s;i<=e&&seeds.Count<MaxSeedsPerCohort;i++)if(!IsHi(n,i))seeds.Add(i);
        Parallel.ForEach(seeds.ToArray(),sd=>{b.Add(ProfileOne(n,sd,hi,lo,coh));});
    }
    VarProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double distLo2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
        double orth=Math.Sqrt(Math.Max(0,distLo2-proj*proj));
        bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K2,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,seed+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,seed+100);double om1=Of(hT1,n).Average();
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average();
        bool pers=om2>THR,imm=om1>THR;
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        bool pM0=isP1;
        bool pM3=isP1&&proj>PHV;
        bool pM3p=isP1&&proj>PHV&&(n==72?orth>OTH:true);
        return new VarProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0,projHiVec=proj,orthHiVec=orth,
            omT1=om1,omT2=om2,immHi=imm,persist=pers,predM0=pM0,predM3=pM3,predM3p=pM3p};
    }

    static string M0="M0(P1only)",M3="M3(+projHi)",M3p="M3+(+orth)";
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
}
