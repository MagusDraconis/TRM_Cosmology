using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_14;

[Trait("Category","V5_14"),Trait("Category","V5_14_RGI"),Trait("Category","LongRunning")]
public class V5_14_ResidualInterventionLimitAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281;
    const double OrthThresh=0.0153; // Frozen from RGA (trained on 0-99)

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct Trial{public int seed,cohort;public string cls;public double dT1,omT1,omT2;public bool immHi,persist;}

    struct RProfile{
        public int seed,cohort,n;public string cls;
        public double projHiVec,orthHiVec,dMean,dStd,dVelocity,top1ES,lambda2,dT1,omT1,omT2;
        public bool immHi,persist,selM3,selM4;
    }

    public V5_14_ResidualInterventionLimitAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void RGI_01_M3vsM4InterventionComparison(){
        _o.WriteLine("═══ RGI_01: M3 vs M4 intervention comparison ═══");
        var profiles=CollectProfiles();

        // M3 baseline
        _o.WriteLine("\n─── M3 Baseline (Frozen) ───");
        PrintModelRates(profiles,"M3",p=>p.selM3);

        // M4 = M3 + orthHiVec
        _o.WriteLine("\n─── M4 = M3 + orthHiVec > 0.0153 ───");
        PrintModelRates(profiles,"M4",p=>p.selM4);

        // Head-to-head
        _o.WriteLine("\n─── M3 vs M4 — Holdout Lift ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,8} {3,8} {4,8} {5,8} {6,8} {7,10}",
            "N","Cohort","M3Rate","M4Rate","M3Cnt","M4Cnt","Lift","Verdict"));
        foreach(var n in new[]{71,72,75}){
            foreach(var coh in new[]{0,1}){
                var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
                var m3=sub.Where(p=>p.selM3).ToArray();
                var m4=sub.Where(p=>p.selM4).ToArray();
                double m3r=m3.Length>0?(double)m3.Count(p=>p.persist)/m3.Length:0;
                double m4r=m4.Length>0?(double)m4.Count(p=>p.persist)/m4.Length:0;
                string v=m4r>m3r+0.03?"IMPROVED":(m4r>m3r?"MARGINAL":(m4r>=m3r-0.02?"NEUTRAL":"DEGRADED"));
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {m3r,7:P0} {m4r,8:P0} {m3.Length,8} {m4.Length,8} {m4r-m3r,7:+0.0%} {v,10}");
            }
        }

        // Intervention outcomes (already included in profile)
        _o.WriteLine("\n─── M4 Holdout: Intervention Quality ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8}",
            "N","Sel","Strict","ImmHi","ImmOnly","dT1","dReb"));
        foreach(var n in new[]{71,72,75}){
            var ho=profiles.Where(p=>p.n==n&&p.cohort==1&&p.selM4).ToArray();
            if(ho.Length==0)continue;
            int st=ho.Count(p=>p.persist),ih=ho.Count(p=>p.immHi);
            double dT1=ho.Average(p=>p.dT1);
            double dReb=ho.Length>0?ho.Average(p=>p.omT2-p.omT1):0;
            _o.WriteLine($"{n,4} {ho.Length,6} {st,8} {ih,8} {(ih-st),8} {dT1,8:F4} {dReb,8:F4}");
        }

        _profiles=profiles;
    }

    [Fact]public void RGI_02_DisplacementAdjustmentTest(){
        _o.WriteLine("═══ RGI_02: Displacement adjustment test ═══");
        var profiles=EnsureProfiles();

        // Test: for M4-selected seeds, does stronger compression help?
        // Run additional displacement levels on N=72 train M4-selected (small subset)
        _o.WriteLine("Testing stronger compression on N=72 train M4-selected seeds.");
        var n72Train=profiles.Where(p=>p.n==72&&p.cohort==0&&p.selM4).ToArray();
        if(n72Train.Length==0){_o.WriteLine("No M4 candidates at N=72 train.");return;}

        var hi=GetHi(72);var lo=GetLo(72);
        _o.WriteLine(string.Format("\n{0,-12} {1,6} {2,8} {3,8} {4,8}",
            "Compression","Seeds","Strict","Rate","vsM3"));
        double[] compressions={0.50,0.40,0.30}; // baseline, +10%, +20% stronger (lower = stronger)

        foreach(var comp in compressions){
            var trials=new ConcurrentBag<(int seed,bool persist,bool immHi)>();
            Parallel.ForEach(n72Train,p=>{
                var K=KS(72,p.seed); // need to re-run from scratch with different compression
                // Actually, we need full RecoverFP to apply compression
                var sb=new SBase{seed=p.seed,d0=p.dMean,cls=p.cls};
                var trial=RunTrial(72,p.seed,p.dMean*comp,sb,hi,lo,0);
                trials.Add((p.seed,trial.persist,trial.immHi));
            });
            var res=trials.ToArray();
            int st=res.Count(r=>r.persist);
            double rate=(double)st/res.Length;
            double m3rate=n72Train.Length>0?(double)n72Train.Count(p=>p.persist)/n72Train.Length:0;
            string label=comp==0.50?"baseline(50%)":comp==0.40?"+10%(40%)":"+20%(30%)";
            _o.WriteLine($"{label,-12} {res.Length,6} {st,8} {rate,7:P0} {rate-m3rate,8:+0.0%}");
        }
    }

    [Fact]public void RGI_03_MissedCandidateAnalysis(){
        _o.WriteLine("═══ RGI_03: Missed candidate recovery ─══");
        var profiles=EnsureProfiles();

        // M3-rejected but orthHiVec-favorable: do any succeed?
        _o.WriteLine("\n─── Candidates rejected by M3 but favorable by orthHiVec ───");
        _o.WriteLine(string.Format("{0,4} {1,10} {2,6} {3,6} {4,8} {5,10}",
            "N","Cohort","RejOK","Strict","Rate","Verdict"));
        foreach(var n in new[]{71,72,75}){
            foreach(var coh in new[]{0,1}){
                var rej=profiles.Where(p=>p.n==n&&p.cohort==coh&&!p.selM3&&p.orthHiVec>OrthThresh&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
                if(rej.Length==0)continue;
                int st=rej.Count(p=>p.persist);
                double rate=(double)st/rej.Length;
                string cohLab=coh==0?"Train":"Hold";
                _o.WriteLine($"{n,4} {cohLab,10} {rej.Length,6} {st,6} {rate,7:P0} {(rate>0.1?"RECOVERABLE":"SPARSE"),10}");
            }
        }

        // M3-rejected + orthHiVec-favorable + P2 candidates
        _o.WriteLine("\n─── N=75 M3-rejected with favorable orthHiVec ───");
        foreach(var n in new[]{75}){
            var rej75=profiles.Where(p=>p.n==n&&p.cohort==0&&!p.selM3&&p.orthHiVec<0.0705&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
            _o.WriteLine($"N=75 train: {rej75.Length} rejected candidates with orthHiVec < 0.0705");
            _o.WriteLine($"  Strict persistence: {rej75.Count(p=>p.persist)}/{rej75.Length}");
        }
    }

    [Fact]public void RGI_04_N75SparseResidualTest(){
        _o.WriteLine("═══ RGI_04: N=75 sparse residual signal validation ═══");
        var profiles=EnsureProfiles();

        _o.WriteLine("\n─── N=75 orthHiVec < 0.07 (trained on N=75 reference) ───");
        var n75H=profiles.Where(p=>p.n==75&&p.cohort==1&&p.selM3).ToArray(); // M3-selected holdout
        var n75S=n75H.Where(p=>p.orthHiVec<0.0705).ToArray(); // N=75-specific: RGA trained inverted
        double m3r=n75H.Length>0?(double)n75H.Count(p=>p.persist)/n75H.Length:0;
        double orthR=n75S.Length>0?(double)n75S.Count(p=>p.persist)/n75S.Length:0;
        _o.WriteLine($"M3 selected N=75 holdout: {n75H.Length} seeds, {m3r:P0} rate");
        _o.WriteLine($"orthHiVec filtered: {n75S.Length} seeds, {orthR:P0} rate ({orthR-m3r:+.0%} lift)");

        _o.WriteLine("\n─── N=75 top1% edge share (trained on N=75 reference) ───");
        var n75T=profiles.Where(p=>p.n==75&&p.cohort==1&&p.selM3).ToArray();
        var n75Top=n75T.Where(p=>p.top1ES>0.0143).ToArray();
        double topR=n75Top.Length>0?(double)n75Top.Count(p=>p.persist)/n75Top.Length:0;
        _o.WriteLine($"top1% filtered: {n75Top.Length} seeds, {topR:P0} rate ({topR-m3r:+.0%} lift)");

        _o.WriteLine("\n─── Assessment ───");
        bool usable=n75S.Length>=3&&orthR>m3r+0.03;
        _o.WriteLine($"orthHiVec usable: {(usable?"YES":"TOO SPARSE")} (n={n75S.Length})");
        bool topUsable=n75Top.Length>=5&&topR>m3r+0.03;
        _o.WriteLine($"top1% usable: {(topUsable?"YES":"TOO SPARSE")} (n={n75Top.Length})");
    }

    [Fact]public void RGI_05_ControlCeilingAndGates(){
        _o.WriteLine("═══ RGI_05: Control ceiling assessment and decision gates ═══");
        var profiles=EnsureProfiles();

        // Compute M4 holdout lift per N
        double lift71=0,lift72=0,lift75=0;int sel71=0,sel72=0,sel75=0;
        foreach(var n in new[]{71,72,75}){
            var ho=profiles.Where(p=>p.n==n&&p.cohort==1).ToArray();
            var m3=ho.Where(p=>p.selM3).ToArray();var m4=ho.Where(p=>p.selM4).ToArray();
            double m3r=m3.Length>0?(double)m3.Count(p=>p.persist)/m3.Length:0;
            double m4r=m4.Length>0?(double)m4.Count(p=>p.persist)/m4.Length:0;
            if(n==71){lift71=m4r-m3r;sel71=m4.Length;}
            else if(n==72){lift72=m4r-m3r;sel72=m4.Length;}
            else{lift75=m4r-m3r;sel75=m4.Length;}
        }

        bool m4Improves=(lift72>0.05&&sel72>=3)||(lift71>0.05&&sel71>=3);
        bool n72Only=lift72>0.05&&sel72>=3&&lift71<0.03&&lift75<0.03;
        bool anyHarm=lift71<-0.03||lift75<-0.03;

        _o.WriteLine($"\nGate A — orthHiVec Intervention Value: {(m4Improves?$"REACHED — N=72 +{lift72:.0%}":"NOT REACHED")}");
        _o.WriteLine($"Gate B — N=72-Specific: {(n72Only?$"REACHED — N=72 +{lift72:.0%}, other N neutral":"SEE BELOW")}");
        _o.WriteLine($"Gate C — Stronger Displacement: SEE RGI_02");
        _o.WriteLine($"Gate D — Missed Candidate Recovery: SEE RGI_03");
        _o.WriteLine($"Gate E — N=75 Too Sparse: SEE RGI_04");
        _o.WriteLine($"Gate F — Control Ceiling: {(!m4Improves?$"REACHED — M4 does not materially improve holdout":"NOT REACHED")}");
        _o.WriteLine($"Gate G — Overfit Warning: {(anyHarm?"REACHED — M4 harms some N":"NOT REACHED")}");

        _o.WriteLine($"\n─── Control Ceiling Assessment ───");
        _o.WriteLine($"M4 lift: N=71 {lift71:+.0%}, N=72 {lift72:+.0%}, N=75 {lift75:+.0%}");
        if(m4Improves){
            _o.WriteLine($"M4 provides material improvement at N=72. M3+: P1/P1b + projHiVec + orthHiVec > {OrthThresh}.");
            _o.WriteLine("Ceiling: NOT REACHED — one N-specific residual degree of freedom exists.");
        }else{
            _o.WriteLine("M4 does not provide material intervention improvement. M3 is at practical control ceiling.");
            _o.WriteLine("V5.14 concludes: M3 is the best currently validated model.");
        }

        _o.WriteLine("\n─── Claim Discipline ───");
        _o.WriteLine("No physical interpretation. No new hidden variables without holdout intervention evidence.");
        _o.WriteLine("All thresholds trained on 0-99 only.");
        _o.WriteLine(m4Improves?"orthHiVec has marginal intervention value at N=72.":"orthHiVec is analysis-only; no intervention benefit.");
        _o.WriteLine("\n─── Next: RGS Final Synthesis ───");
    }

    // ══════════════════════════════════════════════════════════════
    static RProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hiC,_loC;
    P3 GetHi(int n){_hiC??=new();return _hiC.GetOrAdd(n,k=>PCent(k,true));}
    P3 GetLo(int n){_loC??=new();return _loC.GetOrAdd(n,k=>PCent(k,false));}
    RProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();_profiles=p;return p;}
    RProfile[] CollectProfiles(){var b=new ConcurrentBag<RProfile>();foreach(var n in new[]{67,71,72,75,80}){var hi=GetHi(n);var lo=GetLo(n);ProfileC(n,0,99,hi,lo,0,b);ProfileC(n,100,199,hi,lo,1,b);}return b.ToArray();}
    void ProfileC(int n,int s,int e,P3 hi,P3 lo,int coh,ConcurrentBag<RProfile> b){var seeds=new List<int>();for(int i=s;i<=e&&seeds.Count<MaxSeedsPerCohort;i++)if(!IsHi(n,i))seeds.Add(i);Parallel.ForEach(seeds.ToArray(),sd=>{b.Add(ProfileOne(n,sd,hi,lo,coh));});}
    RProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh){
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
        var edges=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K3E[i,j]);
        double avg=edges.Average();double dStd=edges.Count>0?Math.Sqrt(edges.Sum(x=>(x-avg)*(x-avg))/edges.Count):0;
        edges.Sort((a,b)=>b.CompareTo(a));double tot=edges.Sum();
        double t1=tot>0?edges.Take(Math.Max(1,(int)(edges.Count*0.01))).Sum()/tot:0;
        double dVel=dH.Count>=2?dH[^1]-dH[^2]:0;
        double l2=PowerIterationDeflated(K3E,n,PowerIteration(K3E,n,200),200);
        bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var trial=RunTrial(n,seed,tgt,sb,hi,lo,coh);
        return new RProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0,projHiVec=proj,orthHiVec=orth,
            dStd=dStd,dVelocity=dVel,top1ES=t1,lambda2=l2,dT1=trial.dT1,omT1=trial.omT1,omT2=trial.omT2,
            immHi=trial.immHi,persist=trial.persist,
            selM3=(sb.cls=="P1"||sb.cls=="P1b")&&proj>PHV,
            selM4=(sb.cls=="P1"||sb.cls=="P1b")&&proj>PHV&&orth>OrthThresh};
    }
    void PrintModelRates(RProfile[] profiles,string label,Func<RProfile,bool> selector){
        _o.WriteLine(string.Format("{0,4} {1,10} {2,6} {3,6} {4,8}","N","Cohort","Sel","Str","Rate"));
        foreach(var n in new[]{71,72,75})foreach(var coh in new[]{0,1}){
            var sub=profiles.Where(p=>p.n==n&&p.cohort==coh).ToArray();
            var sel=sub.Where(selector).ToArray();
            if(sel.Length==0)continue;
            string cohLab=coh==0?"Train":"Hold";
            _o.WriteLine($"{n,4} {cohLab,10} {sel.Length,6} {sel.Count(p=>p.persist),6} {(double)sel.Count(p=>p.persist)/sel.Length,7:P0}");
        }
    }
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
    static Trial RunTrial(int n,int seed,double target,SBase b,P3 hi,P3 lo,int cohort){var K=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);var h5=Sim(K,n,S,seed+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);var hT1=Sim(K,n,S,seed+100);double om1=Of(hT1,n).Average();var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);double om2=Of(hT2,n).Average();return new Trial{seed=seed,cohort=cohort,cls=b.cls,dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),omT1=om1,omT2=om2,immHi=om1>THR,persist=om2>THR};}
    static double PowerIteration(double[,]A,int n,int maxIter=200){var v=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v[i]=rng.NextDouble();double norm=Math.Sqrt(v.Sum(x=>x*x));if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];norm=Math.Sqrt(Av.Sum(x=>x*x));if(norm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/norm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
    static double PowerIterationDeflated(double[,]A,int n,double lambda1,int maxIter=200){var v1=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v1[i]=rng.NextDouble();double nrm=Math.Sqrt(v1.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v1[i]/=nrm;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v1[j];nrm=Math.Sqrt(Av.Sum(x=>x*x));if(nrm<1e-15)break;for(int i=0;i<n;i++)v1[i]=Av[i]/nrm;}var v=new double[n];rng=new Random(137);for(int i=0;i<n;i++)v[i]=rng.NextDouble();nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;double dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=dot*v1[i];nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];nrm=Math.Sqrt(Av.Sum(x=>x*x));if(nrm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/nrm;dot=0;for(int i=0;i<n;i++)dot+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=dot*v1[i];nrm=Math.Sqrt(v.Sum(x=>x*x));if(nrm>0)for(int i=0;i<n;i++)v[i]/=nrm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
}
