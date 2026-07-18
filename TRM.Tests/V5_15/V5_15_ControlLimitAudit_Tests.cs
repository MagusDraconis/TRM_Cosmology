using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_15;

[Trait("Category","V5_15"),Trait("Category","V5_15_CVI"),Trait("Category","LongRunning")]
public class V5_15_ControlLimitAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct CVProfile{
        public int seed,cohort,n;public string cls;
        public double projHiVec,orthHiVec,dMean,omT1,omT2,dT1;
        public bool immHi,persist;
        public bool predM3p,predM3ppA,predM3ppB,predM3ppC2;
        // M3++C2: predM3ppC2 = selected by M3ppB AND got stronger displacement (tested separately)
    }

    public V5_15_ControlLimitAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void CVI_01_BaselineReproductionAndRefinements(){
        _o.WriteLine("═══ CVI_01: M3+ baseline and M3++ candidate refinements ═══");
        var profiles=CollectProfiles();

        // M3+ baseline
        _o.WriteLine("\n─── M3+ Baseline ───");
        PrintModel("M3+",profiles,p=>p.predM3p);

        // M3++A: N=75 applies projHiVec selector instead of bypass
        _o.WriteLine("\n─── M3++A: N=75 selector (not bypass) ───");
        PrintModel("M3++A",profiles,p=>p.predM3ppA);

        // M3++B: FN recovery — expand to P3+dMean>0.40+projHiVec favorable
        _o.WriteLine("\n─── M3++B: FN recovery rule (+P3 w/ projHiVec>PHV & dMean>0.40) ───");
        PrintModel("M3++B",profiles,p=>p.predM3ppB);

        // Compare
        _o.WriteLine("\n─── Holdout comparison ───");
        _o.WriteLine(string.Format("{0,-10} {1,6} {2,6} {3,6} {4,6} {5,6} {6,6} {7,8} {8,8}",
            "Model","TP","FP","TN","FN","Prec","Rec","FPR","FNR"));
        var ho=profiles.Where(p=>p.cohort==1).ToArray();
        foreach(var (label,pred) in new[]{
            ("M3+",Fp(p=>p.predM3p)),("M3++A",Fp(p=>p.predM3ppA)),("M3++B",Fp(p=>p.predM3ppB))}){
            int tp=ho.Count(p=>pred(p)&&p.persist),fp=ho.Count(p=>pred(p)&&!p.persist);
            int tn=ho.Count(p=>!pred(p)&&!p.persist),fn=ho.Count(p=>!pred(p)&&p.persist);
            double prec=tp+fp>0?(double)tp/(tp+fp):0,rec=tp+fn>0?(double)tp/(tp+fn):0;
            double fpr=tn+fp>0?(double)fp/(tn+fp):0,fnr=tp+fn>0?(double)fn/(tp+fn):0;
            _o.WriteLine($"{label,-10} {tp,6} {fp,6} {tn,6} {fn,6} {prec,5:P0} {rec,6:P0} {fpr,7:P0} {fnr,8:P0}");
        }

        // Per-N holdout comparison
        _o.WriteLine("\n─── Holdout per-N persistence rate ───");
        _o.WriteLine(string.Format("{0,4} {1,8} {2,8} {3,8}","N","M3+","M3++A","M3++B"));
        foreach(var n in new[]{71,72,75}){
            var s=ho.Where(p=>p.n==n).ToArray();
            double m3=Rate(s,p=>p.predM3p),ma=Rate(s,p=>p.predM3ppA),mb=Rate(s,p=>p.predM3ppB);
            _o.WriteLine($"{n,4} {m3,7:P0} {ma,8:P0} {mb,8:P0}");
        }

        _profiles=profiles;
    }

    [Fact]public void CVI_02_N75SelectorCorrection(){
        _o.WriteLine("═══ CVI_02: N=75 selector correction audit ═══");
        var profiles=EnsureProfiles();

        var n75=profiles.Where(p=>p.n==75).ToArray();
        _o.WriteLine(string.Format("\n{0,10} {1,6} {2,6} {3,6} {4,6} {5,6} {6,6} {7,6} {8,6} {9,10}",
            "Model","TP","FP","TN","FN","Prec","Rec","FPR","FNR","Verdict"));

        foreach(var coh in new[]{0,1}){
            var s=n75.Where(p=>p.cohort==coh).ToArray();
            // M3+ with N=75 bypass
            var m3pTP=s.Count(p=>p.predM3p&&p.persist);
            var m3pFP=s.Count(p=>p.predM3p&&!p.persist);
            var m3pTN=s.Count(p=>!p.predM3p&&!p.persist);
            var m3pFN=s.Count(p=>!p.predM3p&&p.persist);
            double m3pP=m3pTP+m3pFP>0?(double)m3pTP/(m3pTP+m3pFP):0;
            double m3pR=m3pTP+m3pFN>0?(double)m3pTP/(m3pTP+m3pFN):0;

            // M3++A: N=75 selector
            var aTP=s.Count(p=>p.predM3ppA&&p.persist);
            var aFP=s.Count(p=>p.predM3ppA&&!p.persist);
            var aTN=s.Count(p=>!p.predM3ppA&&!p.persist);
            var aFN=s.Count(p=>!p.predM3ppA&&p.persist);
            double aP=aTP+aFP>0?(double)aTP/(aTP+aFP):0;
            double aR=aTP+aFN>0?(double)aTP/(aTP+aFN):0;

            string cohLab=coh==0?"Train":"Hold";
            _o.WriteLine($"{cohLab+" M3+",10} {m3pTP,6} {m3pFP,6} {m3pTN,6} {m3pFN,6} {m3pP,5:P0} {m3pR,6:P0} {"-",6} {"-",6} {"-",10}");
            _o.WriteLine($"{cohLab+" M3++A",10} {aTP,6} {aFP,6} {aTN,6} {aFN,6} {aP,5:P0} {aR,6:P0} {"-",6} {"-",6} {(aP>m3pP?"PREC_UP":"PREC_DN"),10}");
        }
    }

    [Fact]public void CVI_03_DisplacementRepair(){
        _o.WriteLine("═══ CVI_03: Displacement repair test ═══");
        var profiles=EnsureProfiles();

        // Test stronger compression on M3+ selected seeds that fail with insufDisp
        // Limit to N=72 train to keep runtime low
        var candidates=profiles.Where(p=>p.n==72&&p.cohort==0&&p.predM3p&&!p.immHi).ToArray();
        if(candidates.Length==0){
            _o.WriteLine("No M3+ selected insufDisp candidates at N=72 train.");
            return;
        }
        _o.WriteLine($"Testing {candidates.Length} N=72 train insufDisp candidates with stronger compression.");

        var hi=Hi(72);var lo=Lo(72);
        _o.WriteLine(string.Format("\n{0,-14} {1,6} {2,8} {3,8} {4,8}",
            "Compression","Seeds","Strict","Rate","ImmHi%"));
        double[] comps={0.50,0.40,0.35,0.30}; // baseline, +10%, +15%, +20%

        foreach(var comp in comps){
            var results=new ConcurrentBag<(bool persist,bool immHi)>();
            Parallel.ForEach(candidates,p=>{
                var K=KS(72,p.seed);for(int e=0;e<3;e++){var h=Sim(K,72,S,p.seed+e);var d=DL(Nm(RP(h,72),72),72);K=Cupd(d,72);}
                var h4=Sim(K,72,S,p.seed+3);var d4=DL(Nm(RP(h4,72),72),72);double curDm=Dm(d4,72);
                double frac=Math.Clamp((p.dMean*comp+1e-9)/(curDm+1e-9),0.01,100.0);
                var dM=CD(d4,72);for(int i=0;i<72;i++)for(int j=0;j<72;j++)if(i!=j)dM[i,j]*=frac;
                K=Cupd(dM,72);var h5=Sim(K,72,S,p.seed+4);K=Cupd(DL(Nm(RP(h5,72),72),72),72);
                var hT1=Sim(K,72,S,p.seed+100);double om1=Of(hT1,72).Average();
                var hT2=Sim(Cupd(DL(Nm(RP(hT1,72),72),72),72),72,S,p.seed+200);double om2=Of(hT2,72).Average();
                results.Add((om2>THR,om1>THR));
            });
            var res=results.ToArray();
            string label=comp==0.50?"baseline(50%)":comp==0.40?"+10%(40%)":comp==0.35?"+15%(35%)":"+20%(30%)";
            _o.WriteLine($"{label,-14} {res.Length,6} {res.Count(r=>r.persist),8} {(double)res.Count(r=>r.persist)/res.Length,7:P0} {res.Count(r=>r.immHi)*100.0/res.Length,7:F0}%");
        }
    }

    [Fact]public void CVI_04_M3ppComparisonAndGates(){
        _o.WriteLine("═══ CVI_04: M3++ comparison and decision gates ═══");
        var profiles=EnsureProfiles();
        var ho=profiles.Where(p=>p.cohort==1).ToArray();

        // Compute holdout metrics for each model
        double[] Metrics(Func<CVProfile,bool> pred){
            int tp=ho.Count(p=>pred(p)&&p.persist),fp=ho.Count(p=>pred(p)&&!p.persist);
            int tn=ho.Count(p=>!pred(p)&&!p.persist),fn=ho.Count(p=>!pred(p)&&p.persist);
            double prec=tp+fp>0?(double)tp/(tp+fp):0,rec=tp+fn>0?(double)tp/(tp+fn):0;
            double fpr=tn+fp>0?(double)fp/(tn+fp):0,fnr=tp+fn>0?(double)fn/(tp+fn):0;
            double strict=ho.Where(pred).Count(p=>p.persist)*1.0/Math.Max(1,ho.Count(pred));
            double m3pR=tp+fn>0?(double)ho.Count(p=>p.predM3p&&p.persist)/(ho.Count(p=>p.predM3p&&p.persist)+ho.Count(p=>!p.predM3p&&p.persist)):0;
            return new[]{prec,rec,fpr,fnr,strict,rec-m3pR};
        }

        var m3m=Metrics(p=>p.predM3p);
        var ma=Metrics(p=>p.predM3ppA);
        var mb=Metrics(p=>p.predM3ppB);

        _o.WriteLine("\n─── Holdout model comparison ───");
        _o.WriteLine(string.Format("{0,-10} {1,8} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "Model","Prec","Recall","FPR","FNR","Rate","ΔRec"));
        _o.WriteLine(string.Format("{0,-10} {1,7:P0} {2,8:P0} {3,7:P0} {4,8:P0} {5,7:P0} {6,8}",
            "M3+",m3m[0],m3m[1],m3m[2],m3m[3],m3m[4],"-"));
        _o.WriteLine(string.Format("{0,-10} {1,7:P0} {2,8:P0} {3,7:P0} {4,8:P0} {5,7:P0} {6,8:+0.0%}",
            "M3++A",ma[0],ma[1],ma[2],ma[3],ma[4],ma[1]-m3m[1]));
        _o.WriteLine(string.Format("{0,-10} {1,7:P0} {2,8:P0} {3,7:P0} {4,8:P0} {5,7:P0} {6,8:+0.0%}",
            "M3++B",mb[0],mb[1],mb[2],mb[3],mb[4],mb[1]-m3m[1]));

        // Gate decisions
        bool aOk=ma[1]>m3m[1]+0.03&&ma[2]<m3m[2]+0.05; // N=75 recall up, FPR OK
        bool bOk=mb[1]>m3m[1]+0.03&&mb[2]<m3m[2]+0.05; // FN recovery helps
        bool n75Ok=profiles.Where(p=>p.n==75&&p.cohort==1).ToArray() is var n75h&&n75h.Length>0&&
            (double)n75h.Count(p=>p.predM3ppA&&p.persist)/Math.Max(1,n75h.Count(p=>p.predM3ppA))>
            (double)n75h.Count(p=>p.predM3p&&p.persist)/Math.Max(1,n75h.Count(p=>p.predM3p));
        bool anyHarm=ma[4]<m3m[4]-0.03||mb[4]<m3m[4]-0.03;
        bool combinedOk=aOk||bOk;

        _o.WriteLine($"\nGate A — FN Reducible: {(bOk?$"REACHED — +{mb[1]-m3m[1]:.0%} recall":"NOT REACHED")}");
        _o.WriteLine($"Gate B — N=75 Validated: {(n75Ok?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C — Displacement Repair: SEE CVI_03");
        string dLabel=combinedOk&&!anyHarm?"REACHED":"NOT REACHED";
        string dNote=anyHarm?" - harmful":"";
        _o.WriteLine($"Gate D — M3++ Established: {dLabel}{dNote}");
        _o.WriteLine($"Gate E — Overfit Warning: {(anyHarm?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F — Control Ceiling: {(!combinedOk?"REACHED - no refinement helps":"NOT REACHED")}");

        _o.WriteLine("\n─── Claim Discipline ───");
        _o.WriteLine("No new selectors beyond pre-registered refinements. No threshold tuning on holdout.");
        _o.WriteLine(combinedOk?"M3++ refinement available.":"M3+ remains best model.");

        _o.WriteLine("\n─── Next: CVS Final Synthesis ───");
    }

    // ══════════════════════════════════════════════════════════════
    static CVProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    CVProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();_profiles=p;return p;}
    CVProfile[] CollectProfiles(){var b=new ConcurrentBag<CVProfile>();foreach(var n in new[]{67,71,72,75,80}){var hi=Hi(n);var lo=Lo(n);PC(n,0,99,hi,lo,0,b);PC(n,100,199,hi,lo,1,b);}return b.ToArray();}
    void PC(int n,int s,int e,P3 hi,P3 lo,int coh,ConcurrentBag<CVProfile> b){var seeds=new List<int>();for(int i=s;i<=e&&seeds.Count<MaxSeedsPerCohort;i++)if(!IsHi(n,i))seeds.Add(i);Parallel.ForEach(seeds.ToArray(),sd=>{b.Add(ProfileOne(n,sd,hi,lo,coh));});}
    CVProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh){
        var K=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
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
        double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,seed+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,seed+100);double om1=Of(hT1,n).Average();
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average();
        bool pers=om2>THR,imm=om1>THR,isP1=sb.cls=="P1"||sb.cls=="P1b";
        // M3+ prediction
        bool pM3p=n==75?isP1:(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV);
        // M3++A: N=75 uses selector (same as N=71/72)
        bool pA=n==75?(isP1&&proj>PHV):pM3p;
        // M3++B: M3+ + FN recovery — include P3 with projHiVec>PHV and dMean>0.40
        bool isP3=sb.cls=="P3";bool fnRecovery=isP3&&proj>PHV&&d0>0.40;
        bool pB=pM3p||fnRecovery;
        return new CVProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0,projHiVec=proj,orthHiVec=orth,
            omT1=om1,omT2=om2,dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),immHi=imm,persist=pers,
            predM3p=pM3p,predM3ppA=pA,predM3ppB=pB};
    }

    void PrintModel(string label,CVProfile[] p,Func<CVProfile,bool> pred){
        int tp=p.Count(x=>pred(x)&&x.persist),fp=p.Count(x=>pred(x)&&!x.persist);
        int tn=p.Count(x=>!pred(x)&&!x.persist),fn=p.Count(x=>!pred(x)&&x.persist);
        double prec=tp+fp>0?(double)tp/(tp+fp):0,rec=tp+fn>0?(double)tp/(tp+fn):0;
        _o.WriteLine($"  TP={tp} FP={fp} TN={tn} FN={fn} | Prec={prec:P0} Rec={rec:P0} Acc={(double)(tp+tn)/p.Length:P0}");
    }
    double Rate(CVProfile[] p,Func<CVProfile,bool> pred){var sel=p.Where(pred).ToArray();return sel.Length>0?(double)sel.Count(x=>x.persist)/sel.Length:0;}
    static Func<CVProfile,bool> Fp(Func<CVProfile,bool> f)=>f;

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
