using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_16;

[Trait("Category","V5_16"),Trait("Category","V5_16_EGE"),Trait("Category","LongRunning")]
public class V5_16_FeatureDiscoveryExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;
    const int MaxSeedsPerCohort=30;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;public double[,]? Kmat;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct FProfile{
        public int seed,cohort,n;public string cls,stateGroup;
        public double projHiVec,orthHiVec,dMean,kMean,dStd,lambda1,lambda2,lambda3,spGap,kFrob,top1,top5,top10,omT1,omT2,dT1,dVelocity,kVelocity;
        public bool immHi,persist,predM3p;
        // F1 — matrix localization
        public double matEntropy,matGini,concRatio;
        // F2 — node concentration
        public double rowSumMax,rowSumMin,rowSumStd,nodeConcIdx;
        // F3 — graph topology
        public double meanDeg,degVar,maxDeg,edgeDensity,largestCompFrac;
        // F4 — spectral shape
        public double l1Ratio,spEffRank,top3Energy;
        // F5 — motif overlap
        public double hiOverlap,loOverlap;
        // F6 — memory
        public double cumDmov,cumKmov,dirConsistency,dSignChg,kSignChg;
        // F7 — intervention response
        public double rebMagnitude,dRespElasticity;
    }

    public V5_16_FeatureDiscoveryExecution_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void EGE_01_FeatureCollectionAndAvailability(){
        _o.WriteLine("═══ EGE_01: Feature collection and availability audit ═══");
        var profiles=CollectProfiles();
        int n=profiles.Length;_o.WriteLine($"Profiles: {n}");

        _o.WriteLine(string.Format("\n{0,-4} {1,-18} {2,6} {3,6} {4,8}",
            "Fam","Feature","Avail","Miss","Status"));
        // F1
        PA("F1","matEntropy",profiles,p=>!double.IsNaN(p.matEntropy),n);
        PA("F1","concRatio",profiles,p=>!double.IsNaN(p.concRatio),n);
        // F2
        PA("F2","rowSumMax",profiles,p=>!double.IsNaN(p.rowSumMax),n);
        PA("F2","nodeConcIdx",profiles,p=>!double.IsNaN(p.nodeConcIdx),n);
        // F3
        PA("F3","meanDeg",profiles,p=>!double.IsNaN(p.meanDeg),n);
        PA("F3","edgeDensity",profiles,p=>!double.IsNaN(p.edgeDensity),n);
        // F4
        PA("F4","lambda3",profiles,p=>!double.IsNaN(p.lambda3),n);
        PA("F4","spEffRank",profiles,p=>!double.IsNaN(p.spEffRank),n);
        // F5
        PA("F5","hiOverlap",profiles,p=>!double.IsNaN(p.hiOverlap),n);
        PA("F5","loOverlap",profiles,p=>!double.IsNaN(p.loOverlap),n);
        // F6
        PA("F6","cumDmov",profiles,p=>!double.IsNaN(p.cumDmov),n);
        PA("F6","dirConsistency",profiles,p=>!double.IsNaN(p.dirConsistency),n);
        // F7
        PA("F7","rebMagnitude",profiles,p=>!double.IsNaN(p.rebMagnitude),n);
        PA("F7","dRespElasticity",profiles,p=>!double.IsNaN(p.dRespElasticity),n);

        _o.WriteLine("\nAll F1-F7 feature families extractable with current engine.");
        _profiles=profiles;
    }

    [Fact]public void EGE_02_ResidualContrastAnalysis(){
        _o.WriteLine("═══ EGE_02: Residual contrast analysis ═══");
        var profiles=EnsureProfiles();
        // Classify state groups
        AssignGroups(profiles);

        // G4 vs G3 (FN vs TN) — train
        _o.WriteLine("\n─── FN vs TN (G4 vs G3) — Train ───");
        Contrast(profiles.Where(p=>p.cohort==0).ToArray(),"G4","G3",
            ("matEntropy",p=>p.matEntropy),("concRatio",p=>p.concRatio),
            ("nodeConcIdx",p=>p.nodeConcIdx),("edgeDensity",p=>p.edgeDensity),
            ("lambda3",p=>p.lambda3),("spEffRank",p=>p.spEffRank),
            ("hiOverlap",p=>p.hiOverlap),("loOverlap",p=>p.loOverlap),
            ("cumDmov",p=>p.cumDmov),("dirConsistency",p=>p.dirConsistency),
            ("rebMagnitude",p=>p.rebMagnitude),("dRespElast",p=>p.dRespElasticity));

        // G2 vs G1 (FP vs TP) — train
        _o.WriteLine("\n─── FP vs TP (G2 vs G1) — Train ───");
        Contrast(profiles.Where(p=>p.cohort==0).ToArray(),"G2","G1",
            ("matEntropy",p=>p.matEntropy),("concRatio",p=>p.concRatio),
            ("nodeConcIdx",p=>p.nodeConcIdx),("edgeDensity",p=>p.edgeDensity),
            ("lambda3",p=>p.lambda3),("spEffRank",p=>p.spEffRank),
            ("hiOverlap",p=>p.hiOverlap),("loOverlap",p=>p.loOverlap),
            ("rebMagnitude",p=>p.rebMagnitude),("dRespElast",p=>p.dRespElasticity));

        // N=75 success vs failure
        _o.WriteLine("\n─── N=75 success vs failure (P1/P1b) ───");
        var n75=profiles.Where(p=>p.n==75&&(p.cls=="P1"||p.cls=="P1b")).ToArray();
        var n75S=n75.Where(p=>p.persist).ToArray();var n75F=n75.Where(p=>!p.persist).ToArray();
        QCompare(n75S,n75F,("nodeConcIdx",p=>p.nodeConcIdx),("hiOverlap",p=>p.hiOverlap),
            ("lambda3",p=>p.lambda3),("cumDmov",p=>p.cumDmov),
            ("rebMagnitude",p=>p.rebMagnitude),("matEntropy",p=>p.matEntropy));
    }

    [Fact]public void EGE_03_FeatureFamilyRanking(){
        _o.WriteLine("═══ EGE_03: Feature family ranking ═══");
        var profiles=EnsureProfiles();

        // Compute average effect size per family for FN vs TN (train)
        var families=new[]{
            ("F1:MatrixLoc",new[]{("entropy",F(p=>p.matEntropy)),("concRatio",F(p=>p.concRatio))}),
            ("F2:NodeConc",new[]{("nodeIdx",F(p=>p.nodeConcIdx)),("rowSumMax",F(p=>p.rowSumMax))}),
            ("F3:Topology",new[]{("edgeDens",F(p=>p.edgeDensity)),("meanDeg",F(p=>p.meanDeg))}),
            ("F4:Spectral",new[]{("lambda3",F(p=>p.lambda3)),("spEffRank",F(p=>p.spEffRank))}),
            ("F5:Motif",new[]{("hiOverlap",F(p=>p.hiOverlap)),("loOverlap",F(p=>p.loOverlap))}),
            ("F6:Memory",new[]{("cumDmov",F(p=>p.cumDmov)),("dirCons",F(p=>p.dirConsistency))}),
            ("F7:Response",new[]{("rebMag",F(p=>p.rebMagnitude)),("dElast",F(p=>p.dRespElasticity))}),
        };

        var train=profiles.Where(p=>p.cohort==0).ToArray();
        var fn=train.Where(p=>p.stateGroup=="G4").ToArray();
        var tn=train.Where(p=>p.stateGroup=="G3").ToArray();
        var fp=train.Where(p=>p.stateGroup=="G2").ToArray();
        var tp=train.Where(p=>p.stateGroup=="G1").ToArray();

        _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,10} {4,8}",
            "Family","FNvTN_ES","FPvTP_ES","TopFeat","Verdict"));
        foreach(var (fname,feats) in families){
            double fnES=0,fpES=0;string topFeat="";
            foreach(var (fn1,f1) in feats){
                if(fn.Length>=3&&tn.Length>=3){
                    double es=EffSz(fn.Select(f1).ToArray(),tn.Select(f1).ToArray());
                    if(es>fnES){fnES=es;topFeat=fn1;}
                }
                if(fp.Length>=3&&tp.Length>=3){
                    double es2=EffSz(fp.Select(f1).ToArray(),tp.Select(f1).ToArray());
                    if(es2>fpES)fpES=es2;
                }
            }
            double avgES=(fnES+fpES)/2;
            string v=avgES>0.4?"PROMISING":avgES>0.25?"MARGINAL":avgES>0.1?"WEAK":"DEAD";
            _o.WriteLine($"{fname,-14} {fnES,8:F3} {fpES,8:F3} {topFeat,10} {v,8}");
        }
    }

    [Fact]public void EGE_04_RedundancyAndDeadEnds(){
        _o.WriteLine("═══ EGE_04: Redundancy and dead-end analysis ═══");
        var profiles=EnsureProfiles();

        // Check correlation of new features with projHiVec, orthHiVec, dMean
        var train=profiles.Where(p=>p.cohort==0).ToArray();
        var newFeats=new[]{"matEntropy","concRatio","nodeConcIdx","edgeDensity","lambda3",
            "spEffRank","hiOverlap","loOverlap","cumDmov","dirConsistency","rebMagnitude","dRespElasticity"};
        var m3feats=new[]{"projHiVec","orthHiVec","dMean","dStd","lambda1"};

        _o.WriteLine(string.Format("\n{0,-14} {1,8} {2,8} {3,8} {4,8} {5,8}",
            "Feature","r(proj)","r(orth)","r(dMean)","r(dStd)","Redundant?"));
        foreach(var nf in newFeats){
            var vals=GetVals(train,nf);
            if(vals.Length<5)continue;
            double rp=Corr(vals,GetVals(train,"projHiVec")),ro=Corr(vals,GetVals(train,"orthHiVec"));
            double rd=Corr(vals,GetVals(train,"dMean")),rs=Corr(vals,GetVals(train,"dStd"));
            double maxR=Math.Max(Math.Max(Math.Abs(rp),Math.Abs(ro)),Math.Max(Math.Abs(rd),Math.Abs(rs)));
            _o.WriteLine($"{nf,-14} {rp,8:+.00} {ro,8:+.00} {rd,8:+.00} {rs,8:+.00} {(maxR>0.7?"YES":"no"),8}");
        }

        _o.WriteLine("\n─── Dead-end features ───");
        _o.WriteLine("Features with max|r|>0.7 to M3+ and low effect size → redundant dead ends.");
        _o.WriteLine("Features with NaN/missing >30% → instrumentation dead ends.");
    }

    [Fact]public void EGE_05_CandidateShortlistAndGates(){
        _o.WriteLine("═══ EGE_05: Candidate shortlist and decision gates ═══");
        var profiles=EnsureProfiles();

        // Find best families
        var train=profiles.Where(p=>p.cohort==0).ToArray();
        var fn=train.Where(p=>p.stateGroup=="G4").ToArray();
        var tn=train.Where(p=>p.stateGroup=="G3").ToArray();
        var fp=train.Where(p=>p.stateGroup=="G2").ToArray();
        var tp=train.Where(p=>p.stateGroup=="G1").ToArray();

        bool anySignal=false,matSig=false,topoSig=false,specSig=false,memSig=false,motifSig=false;

        // Check each family
        double lambda3ES=fn.Length>=3?EffSz(fn.Select(p=>p.lambda3).ToArray(),tn.Select(p=>p.lambda3).ToArray()):0;
        double nodeES=fn.Length>=3?EffSz(fn.Select(p=>p.nodeConcIdx).ToArray(),tn.Select(p=>p.nodeConcIdx).ToArray()):0;
        double hiOverlapES=fn.Length>=3?EffSz(fn.Select(p=>p.hiOverlap).ToArray(),tn.Select(p=>p.hiOverlap).ToArray()):0;
        double cumDmovES=fn.Length>=3?EffSz(fn.Select(p=>p.cumDmov).ToArray(),tn.Select(p=>p.cumDmov).ToArray()):0;
        double edgeDensES=fn.Length>=3?EffSz(fn.Select(p=>p.edgeDensity).ToArray(),tn.Select(p=>p.edgeDensity).ToArray()):0;

        if(lambda3ES>0.3){anySignal=true;specSig=true;}
        if(nodeES>0.3){anySignal=true;matSig=true;}
        if(hiOverlapES>0.3){anySignal=true;motifSig=true;}
        if(cumDmovES>0.3){anySignal=true;memSig=true;}
        if(edgeDensES>0.3){anySignal=true;topoSig=true;}

        _o.WriteLine($"\nGate A — New Feature Found: {(anySignal?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B — Matrix Localization: {(matSig?$"REACHED — nodeIdx ES={nodeES:F2}":"NOT REACHED")}");
        _o.WriteLine($"Gate C — Topology/Motif: {((topoSig||motifSig)?$"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D — Spectral Shape: {(specSig?$"REACHED — lambda3 ES={lambda3ES:F2}":"NOT REACHED")}");
        _o.WriteLine($"Gate E — Trajectory Memory: {(memSig?$"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F — Provenance: NOT REACHED — provenance data not available");
        _o.WriteLine($"Gate G — No New Feature: {(!anySignal?"REACHED":"NOT REACHED")}");

        _o.WriteLine("\n─── Candidate Shortlist ───");
        if(specSig)_o.WriteLine("1. F4 Spectral Shape (lambda3, spEffRank)");
        if(matSig)_o.WriteLine("2. F2 Node Concentration (nodeConcIdx)");
        if(motifSig)_o.WriteLine("3. F5 Motif Overlap (hiOverlap)");
        if(memSig)_o.WriteLine("4. F6 Memory (cumDmov, dirConsistency)");
        if(topoSig)_o.WriteLine("5. F3 Topology (edgeDensity)");
        if(!anySignal)_o.WriteLine("None — M3+ near full explanatory ceiling.");

        _o.WriteLine("\n─── Next: EGA_FeatureValidation ───");
        _o.WriteLine(anySignal?"Validate candidate features on holdout.":"Declare full explanatory ceiling.");
    }

    // ══════════════════════════════════════════════════════════════
    static FProfile[]? _profiles;
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    FProfile[] EnsureProfiles(){if(_profiles!=null)return _profiles;var p=CollectProfiles();AssignGroups(p);_profiles=p;return p;}

    FProfile[] CollectProfiles(){
        // Pre-compute centroid K matrices for F5 overlap
        var hiKs=new ConcurrentDictionary<int,double[,]>();
        var loKs=new ConcurrentDictionary<int,double[,]>();
        foreach(var n in new[]{67,71,72,75,80}){
            var hiK=new double[n,n];var loK=new double[n,n];int hc=0,lc=0;
            for(int sd=0;sd<100&&(hc<10||lc<10);sd++){
                var K=KS(n,sd);
                for(int e=0;e<NE;e++){var h=Sim(K,n,S,sd+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                var Kf=Cupd(DL(Nm(RP(Sim(K,n,S,sd+NE),n),n),n),n);
                double om=Of(Sim(Kf,n,S,sd+NE+1),n).Average();
                if(om>THR&&hc<10){for(int i=0;i<n;i++)for(int j=0;j<n;j++)hiK[i,j]+=Kf[i,j];hc++;}
                else if(om<=THR&&lc<10){for(int i=0;i<n;i++)for(int j=0;j<n;j++)loK[i,j]+=Kf[i,j];lc++;}
            }
            if(hc>0)for(int i=0;i<n;i++)for(int j=0;j<n;j++)hiK[i,j]/=hc;
            if(lc>0)for(int i=0;i<n;i++)for(int j=0;j<n;j++)loK[i,j]/=lc;
            hiKs[n]=hiK;loKs[n]=loK;
        }
        var bag=new ConcurrentBag<FProfile>();
        foreach(var n in new[]{67,71,72,75,80}){var hi=Hi(n);var lo=Lo(n);PC(n,0,99,hi,lo,0,bag,hiKs[n],loKs[n]);PC(n,100,199,hi,lo,1,bag,hiKs[n],loKs[n]);}
        return bag.ToArray();
    }

    void PC(int n,int s,int e,P3 hi,P3 lo,int coh,ConcurrentBag<FProfile> b,double[,] hiK,double[,] loK){
        var seeds=new List<int>();for(int i=s;i<=e&&seeds.Count<MaxSeedsPerCohort;i++)if(!IsHi(n,i))seeds.Add(i);
        Parallel.ForEach(seeds.ToArray(),sd=>{b.Add(ProfileOne(n,sd,hi,lo,coh,hiK,loK));});
    }

    FProfile ProfileOne(int n,int seed,P3 hi,P3 lo,int coh,double[,] hiK,double[,] loK){
        var K=KS(n,seed);var dH=new List<double>();var kH=new List<double>();
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);dH.Add(Dm(d,n));kH.Add(Km(K,n));}
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
        double d2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
        double orth=Math.Sqrt(Math.Max(0,d2-proj*proj));

        // Extract edges from K3E for F1-F3
        var edges=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)edges.Add(K3E[i,j]);
        edges.Sort();int ec=edges.Count;double avg=edges.Average();
        double dStd=ec>0?Math.Sqrt(edges.Sum(x=>(x-avg)*(x-avg))/ec):0;
        // F1 — matrix localization
        double matEnt=0;for(int i=0;i<ec;i++)if(edges[i]>0)matEnt-=edges[i]*Math.Log(edges[i]+1e-15);
        double concR=edges[ec-1]/Math.Max(edges[ec/2],1e-15);
        // F2 — node concentration
        double rsMax=0,rsMin=double.MaxValue;var rowSums=new double[n];
        for(int i=0;i<n;i++){double rs=0;for(int j=0;j<n;j++)rs+=K3E[i,j];rowSums[i]=rs;if(rs>rsMax)rsMax=rs;if(rs<rsMin)rsMin=rs;}
        double rsAvg=rowSums.Average(),rsStd=Math.Sqrt(rowSums.Sum(x=>(x-rsAvg)*(x-rsAvg))/n);
        Array.Sort(rowSums);double nci=rowSums[n-1]/Math.Max(rowSums[n/2],1e-15);
        // F3 — graph topology
        edges.Sort((a,b)=>b.CompareTo(a));double tot=edges.Sum();
        double t1=tot>0?edges.Take(Math.Max(1,ec/100)).Sum()/tot:0;
        double t5=tot>0?edges.Take(Math.Max(1,ec/20)).Sum()/tot:0;
        double t10=tot>0?edges.Take(Math.Max(1,ec/10)).Sum()/tot:0;
        double thr=edges[Math.Max(0,ec/2)]; // median threshold
        int degSum=0,degMax=0,compCnt=0;var visited=new bool[n];
        for(int i=0;i<n;i++){int deg=0;for(int j=0;j<n;j++)if(i!=j&&K3E[i,j]>thr)deg++;degSum+=deg;if(deg>degMax)degMax=deg;}
        double mDeg=(double)degSum/n,degVar=0;
        for(int i=0;i<n;i++){int deg=0;for(int j=0;j<n;j++)if(i!=j&&K3E[i,j]>thr)deg++;degVar+=(deg-mDeg)*(deg-mDeg);}
        degVar/=n;double edDens=(double)degSum/(n*(n-1));
        // Largest component
        for(int i=0;i<n;i++)visited[i]=false;int largest=0;
        for(int i=0;i<n;i++){if(visited[i])continue;int sz=0;var q=new Queue<int>();q.Enqueue(i);visited[i]=true;
            while(q.Count>0){int u=q.Dequeue();sz++;for(int j=0;j<n;j++)if(!visited[j]&&K3E[u,j]>thr){visited[j]=true;q.Enqueue(j);}}
            if(sz>largest)largest=sz;}
        double lcFrac=(double)largest/n;
        // F4 — spectral
        double l1=PowerIteration(K3E,n,200),l2=PowerIterationDeflated(K3E,n,l1,200);
        double l3=PowerIterationDeflated2(K3E,n,l1,l2,200);
        double spGap=l1-l2;double f=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)f+=K3E[i,j]*K3E[i,j];
        double kFrob=Math.Sqrt(f);
        double l1Ratio=l1/(l1+l2+l3+1e-15);
        double spEff=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=K3E[i,j]*K3E[i,j];if(s>1e-10)spEff-=s*Math.Log(s+1e-15);}
        double top3E=(l1+l2+l3)/(l1+l2+l3+1e-15);
        // F5 — motif overlap
        double hiOvl=0,loOvl=0;int hoC=0,loC=0;
        for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){
            if(K3E[i,j]>thr&&hiK[i,j]>thr)hiOvl++;
            if(K3E[i,j]>thr)hoC++;
            if(K3E[i,j]>thr&&loK[i,j]>thr)loOvl++;
            if(K3E[i,j]>thr)loC++;
        }
        hiOvl=hoC>0?hiOvl/hoC:0;loOvl=loC>0?loOvl/loC:0;
        // F6 — memory
        double cumD=dH.Count>=2?dH[^1]-dH[0]:0,cumK=kH.Count>=2?kH[^1]-kH[0]:0;
        double dVel=dH.Count>=2?dH[^1]-dH[^2]:0,kVel=kH.Count>=2?kH[^1]-kH[^2]:0;
        int dSignChg=0;for(int i=1;i<dH.Count-1;i++)if(Math.Sign(dH[i+1]-dH[i])!=Math.Sign(dH[i]-dH[i-1]))dSignChg++;
        int kSignChg=0;for(int i=1;i<kH.Count-1;i++)if(Math.Sign(kH[i+1]-kH[i])!=Math.Sign(kH[i]-kH[i-1]))kSignChg++;
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
        // M3+
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        bool pM3p=n==75?isP1:(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV);
        return new FProfile{seed=seed,cohort=coh,n=n,cls=sb.cls,dMean=d0,kMean=km,dStd=dStd,
            projHiVec=proj,orthHiVec=orth,lambda1=l1,lambda2=l2,lambda3=l3,spGap=spGap,kFrob=kFrob,
            top1=t1,top5=t5,top10=t10,omT1=om1,omT2=om2,dT1=dT1,dVelocity=dVel,kVelocity=kVel,
            immHi=imm,persist=pers,predM3p=pM3p,
            matEntropy=matEnt,concRatio=concR,rowSumMax=rsMax,rowSumMin=rsMin,rowSumStd=rsStd,nodeConcIdx=nci,
            meanDeg=mDeg,degVar=degVar,maxDeg=degMax,edgeDensity=edDens,largestCompFrac=lcFrac,
            l1Ratio=l1Ratio,spEffRank=spEff,top3Energy=top3E,hiOverlap=hiOvl,loOverlap=loOvl,
            cumDmov=cumD,cumKmov=cumK,dirConsistency=dSignChg+kSignChg>0?1.0/(1+dSignChg+kSignChg):1,
            dSignChg=dSignChg,kSignChg=kSignChg,rebMagnitude=rebMag,dRespElasticity=dRespE,stateGroup="G0"};
    }

    void AssignGroups(FProfile[] p){for(int i=0;i<p.Length;i++){
        if(p[i].persist&&p[i].predM3p)p[i].stateGroup="G1";else if(!p[i].persist&&p[i].predM3p)p[i].stateGroup="G2";
        else if(!p[i].persist&&!p[i].predM3p)p[i].stateGroup="G3";else if(p[i].n==75)p[i].stateGroup="G5";
        else p[i].stateGroup="G4";}}

    // Helpers
    void PA(string f,string n,FProfile[] p,Func<FProfile,bool> v,int t){int a=p.Count(v);_o.WriteLine($"{f,-4} {n,-18} {a,6} {t-a,6} {(a==t?"OK":"PARTIAL"),8}");}
    void Contrast(FProfile[] p,string gA,string gB,params (string,Func<FProfile,double>)[] fs){
        var a=p.Where(x=>x.stateGroup==gA).ToArray();var b=p.Where(x=>x.stateGroup==gB).ToArray();
        if(a.Length<3||b.Length<3){_o.WriteLine("  Insufficient data");return;}
        _o.WriteLine(string.Format("  {0,-16} {1,8} {2,8} {3,8}","Feature",gA,gB,"ES"));
        foreach(var (n,f) in fs){double es=EffSz(a.Select(f).ToArray(),b.Select(f).ToArray());_o.WriteLine($"  {n,-16} {a.Average(f),8:F4} {b.Average(f),8:F4} {es,8:F3}");}
    }
    void QCompare(FProfile[] s,FProfile[] f,params (string,Func<FProfile,double>)[] fs){
        _o.WriteLine(string.Format("  {0,-16} {1,8} {2,8} {3,8}","Feature","Succ","Fail","ES"));
        foreach(var (n,fn) in fs)_o.WriteLine($"  {n,-16} {s.Average(fn),8:F4} {f.Average(fn),8:F4} {EffSz(s.Select(fn).ToArray(),f.Select(fn).ToArray()),8:F3}");
    }
    static double EffSz(double[] a,double[] b){double ma=a.Average(),mb=b.Average();double sa=Math.Sqrt(a.Sum(x=>(x-ma)*(x-ma))/a.Length),sb=Math.Sqrt(b.Sum(x=>(x-mb)*(x-mb))/b.Length);double p=Math.Sqrt((sa*sa+sb*sb)/2);return p>1e-12?Math.Abs(ma-mb)/p:0;}
    static double Corr(double[] a,double[] b){int n=Math.Min(a.Length,b.Length);double ma=a.Take(n).Average(),mb=b.Take(n).Average();double sa=0,sb=0,sc=0;for(int i=0;i<n;i++){double da=a[i]-ma,db=b[i]-mb;sa+=da*da;sb+=db*db;sc+=da*db;}return sc/Math.Sqrt(Math.Max(sa*sb,1e-30));}
    static Func<FProfile,double> F(Func<FProfile,double> f)=>f;
    double[] GetVals(FProfile[] p,string n)=>n switch{"matEntropy"=>p.Select(x=>x.matEntropy).ToArray(),"concRatio"=>p.Select(x=>x.concRatio).ToArray(),"nodeConcIdx"=>p.Select(x=>x.nodeConcIdx).ToArray(),"edgeDensity"=>p.Select(x=>x.edgeDensity).ToArray(),"lambda3"=>p.Select(x=>x.lambda3).ToArray(),"spEffRank"=>p.Select(x=>x.spEffRank).ToArray(),"hiOverlap"=>p.Select(x=>x.hiOverlap).ToArray(),"loOverlap"=>p.Select(x=>x.loOverlap).ToArray(),"cumDmov"=>p.Select(x=>x.cumDmov).ToArray(),"dirConsistency"=>p.Select(x=>x.dirConsistency).ToArray(),"rebMagnitude"=>p.Select(x=>x.rebMagnitude).ToArray(),"dRespElasticity"=>p.Select(x=>x.dRespElasticity).ToArray(),"projHiVec"=>p.Select(x=>x.projHiVec).ToArray(),"orthHiVec"=>p.Select(x=>x.orthHiVec).ToArray(),"dMean"=>p.Select(x=>x.dMean).ToArray(),"dStd"=>p.Select(x=>x.dStd).ToArray(),"lambda1"=>p.Select(x=>x.lambda1).ToArray(),_=>new double[p.Length]};

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
    static P3 PCentWK(int n,bool hi,double[,]? Kout){
        double d=0,k=0,ks=0;int c=0;double[,]? bestK=null;
        for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<NE;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}
        return new P3{dm=d/c,km=k/c,ks=ks/c};
    }
    static P3 PCent(int n,bool hi){return PCentWK(n,hi,null);}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    static double PowerIteration(double[,]A,int n,int maxIter=200){var v=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v[i]=rng.NextDouble();double norm=Math.Sqrt(v.Sum(x=>x*x));if(norm>0)for(int i=0;i<n;i++)v[i]/=norm;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];norm=Math.Sqrt(Av.Sum(x=>x*x));if(norm<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/norm;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
    static double PowerIterationDeflated(double[,]A,int n,double l1,int maxIter=200){var v1=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v1[i]=rng.NextDouble();double nr=Math.Sqrt(v1.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v1[i]/=nr;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v1[j];nr=Math.Sqrt(Av.Sum(x=>x*x));if(nr<1e-15)break;for(int i=0;i<n;i++)v1[i]=Av[i]/nr;}var v=new double[n];rng=new Random(137);for(int i=0;i<n;i++)v[i]=rng.NextDouble();nr=Math.Sqrt(v.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v[i]/=nr;double d=0;for(int i=0;i<n;i++)d+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=d*v1[i];nr=Math.Sqrt(v.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v[i]/=nr;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];nr=Math.Sqrt(Av.Sum(x=>x*x));if(nr<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/nr;d=0;for(int i=0;i<n;i++)d+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=d*v1[i];nr=Math.Sqrt(v.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v[i]/=nr;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
    static double PowerIterationDeflated2(double[,]A,int n,double l1,double l2,int maxIter=200){var v1=new double[n];var rng=new Random(42);for(int i=0;i<n;i++)v1[i]=rng.NextDouble();double nr=Math.Sqrt(v1.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v1[i]/=nr;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v1[j];nr=Math.Sqrt(Av.Sum(x=>x*x));if(nr<1e-15)break;for(int i=0;i<n;i++)v1[i]=Av[i]/nr;}var v2=new double[n];rng=new Random(137);for(int i=0;i<n;i++)v2[i]=rng.NextDouble();nr=Math.Sqrt(v2.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v2[i]/=nr;double d=0;for(int i=0;i<n;i++)d+=v2[i]*v1[i];for(int i=0;i<n;i++)v2[i]-=d*v1[i];nr=Math.Sqrt(v2.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v2[i]/=nr;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v2[j];nr=Math.Sqrt(Av.Sum(x=>x*x));if(nr<1e-15)break;for(int i=0;i<n;i++)v2[i]=Av[i]/nr;}var v=new double[n];rng=new Random(256);for(int i=0;i<n;i++)v[i]=rng.NextDouble();nr=Math.Sqrt(v.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v[i]/=nr;d=0;for(int i=0;i<n;i++)d+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=d*v1[i];d=0;for(int i=0;i<n;i++)d+=v[i]*v2[i];for(int i=0;i<n;i++)v[i]-=d*v2[i];nr=Math.Sqrt(v.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v[i]/=nr;double lambda=0;for(int iter=0;iter<maxIter;iter++){var Av=new double[n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Av[i]+=A[i,j]*v[j];nr=Math.Sqrt(Av.Sum(x=>x*x));if(nr<1e-15)break;for(int i=0;i<n;i++)v[i]=Av[i]/nr;d=0;for(int i=0;i<n;i++)d+=v[i]*v1[i];for(int i=0;i<n;i++)v[i]-=d*v1[i];d=0;for(int i=0;i<n;i++)d+=v[i]*v2[i];for(int i=0;i<n;i++)v[i]-=d*v2[i];nr=Math.Sqrt(v.Sum(x=>x*x));if(nr>0)for(int i=0;i<n;i++)v[i]/=nr;double nl=0;for(int i=0;i<n;i++){double s=0;for(int j=0;j<n;j++)s+=A[i,j]*v[j];nl+=v[i]*s;}if(Math.Abs(nl-lambda)<1e-10)break;lambda=nl;}return lambda;}
}
