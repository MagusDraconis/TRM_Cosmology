using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBG"),Trait("Category","LongRunning")]
public class V5_12_SeedIntrinsicPersistenceAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EV{public double DD,DK,DKs;}
    struct P3{public double dm,km,ks;}
    struct Snap{public double om,dm,km,ks,dstd,kstd,es,proj,dh,dl;public bool hi;}
    struct SeedProfile{public int seed;public double baseDm,baseKm,baseKs,baseDstd,baseKstd,baseOm,baseEs,baseDh,baseDl;
        public double natDDm,natDKm;public bool isNatLo;}
    struct PostInt{public int seed;public double t1Dm,t1Km,t1Ks,t1Om,t1Es,t1Dh,t1Dl,t1Dstd,t1Kstd;
        public double t2Dm,t2Km,t2Ks,t2Om,t2Es,t2Dh,t2Dl;public bool immHi,persist,hiProx;}

    public V5_12_SeedIntrinsicPersistenceAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBG_01_SeedIntrinsicProfiling(){
        _o.WriteLine("═══ HBG: Seed-intrinsic persistence profiling at N=71 ═══");
        int n=71,nSeeds=50;
        var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
        _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4} ks={hi.ks:F4}  Lo: dm={lo.dm:F4} km={lo.km:F4} ks={lo.ks:F4}");

        // Get Lo seeds and Hi seeds
        var loSeeds=new List<int>();var hiSeeds=new List<int>();
        for(int s=0;s<100;s++){if(IsHi(n,s))hiSeeds.Add(s);else loSeeds.Add(s);}
        var testSeeds=loSeeds.Take(nSeeds).ToArray();
        _o.WriteLine($"Natural Hi: {hiSeeds.Count}, Lo test: {testSeeds.Length} (seeds {testSeeds[0]}-{testSeeds[^1]})");

        // ── Part 1: Pre-intervention profiling ──
        var profiles=new ConcurrentBag<SeedProfile>();
        Parallel.ForEach(testSeeds,s=>{profiles.Add(ProfileSeed(n,s,hi,lo,ev));});
        var pf=profiles.OrderBy(p=>p.seed).ToArray();

        // ── Part 2: Apply before-Cupd d-compression at target=0.50 ──
        double target=0.50;
        var results=new ConcurrentBag<PostInt>();
        Parallel.ForEach(testSeeds,s=>{results.Add(ApplyIntervention(n,s,target,hi,lo,ev));});
        var res=results.OrderBy(r=>r.seed).ToArray();

        // ── Part 3: Classify ──
        var s1=new List<int>(); // strict persistent
        var s2=new List<int>(); // Hi-prox non-persist
        var s3=new List<int>(); // immHi non-persist
        var s4=new List<int>(); // delayed
        var s5=new List<int>(); // failed
        for(int i=0;i<res.Length;i++){
            var r=res[i];
            if(r.immHi&&r.persist)s1.Add(r.seed);
            else if(r.hiProx&&!r.persist)s2.Add(r.seed);
            else if(r.immHi&&!r.persist)s3.Add(r.seed);
            else if(!r.immHi&&r.persist)s4.Add(r.seed);
            else s5.Add(r.seed);
        }
        _o.WriteLine($"\n─── Classification ({testSeeds.Length} seeds, tgt={target:F2}) ───");
        _o.WriteLine($"S1 Strict Persistent:   {s1.Count}  seeds: [{string.Join(", ",s1)}]");
        _o.WriteLine($"S2 HiProx Non-Persist:  {s2.Count}  seeds: [{string.Join(", ",s2.Take(8))}{((s2.Count>8)?", ...":"")}]");
        _o.WriteLine($"S3 ImmHi Non-Persist:   {s3.Count}  seeds: [{string.Join(", ",s3.Take(8))}{((s3.Count>8)?", ...":"")}]");
        _o.WriteLine($"S4 Delayed:             {s4.Count}  seeds: [{string.Join(", ",s4)}]");
        _o.WriteLine($"S5 Failed:              {s5.Count}  seeds: [{string.Join(", ",s5.Take(8))}{((s5.Count>8)?", ...":"")}]");

        // ── Part 4: Pre-intervention comparison ──
        var pfDict=pf.ToDictionary(p=>p.seed);
        _o.WriteLine($"\n─── Pre-Intervention Profile Comparison ───");
        CompareGroup("S1 Strict",s1,pfDict,_o);
        CompareGroup("S2 HiProxNP",s2,pfDict,_o);
        CompareGroup("S5 Failed",s5,pfDict,_o);
        // Profile Hi seeds separately
        var hiPfs=new ConcurrentBag<SeedProfile>();
        Parallel.ForEach(hiSeeds.Take(20).ToArray(),s=>{hiPfs.Add(ProfileSeed(n,s,hi,lo,ev));});
        var hiPfDict=hiPfs.ToDictionary(p=>p.seed);
        CompareGroup("NatHi",hiSeeds.Take(20).ToList(),hiPfDict,_o);
        CompareGroup("NatLo",loSeeds.Take(20).ToList(),pfDict,_o);

        // ── Part 5: Seed 39 forensic ──
        if(pfDict.ContainsKey(39)){
            _o.WriteLine($"\n─── Seed 39 Forensic Profile ───");
            var p39=pfDict[39];var r39=res.First(r=>r.seed==39);
            _o.WriteLine($"Baseline: dm={p39.baseDm:F4} km={p39.baseKm:F4} ks={p39.baseKs:F4} Om={p39.baseOm:F4} Es={p39.baseEs:F4}");
            _o.WriteLine($"Nat drift: dD={p39.natDDm:F4} dK={p39.natDKm:F4}");
            _o.WriteLine($"Post-int T1: dm={r39.t1Dm:F4} km={r39.t1Km:F4} Om={r39.t1Om:F4} dh={r39.t1Dh:F4} dl={r39.t1Dl:F4}");
            _o.WriteLine($"Post-int T2: dm={r39.t2Dm:F4} km={r39.t2Km:F4} Om={r39.t2Om:F4}");
            _o.WriteLine($"T1->T2: dD={r39.t2Dm-r39.t1Dm:F4} dK={r39.t2Km-r39.t1Km:F4}");
            _o.WriteLine($"immHi={r39.immHi} persist={r39.persist} hiProx={r39.hiProx}");
            double natDm=hiPfs.Average(p=>p.baseDm);
            double natKm=hiPfs.Average(p=>p.baseKm);
            _o.WriteLine($"vs NatHi mean: dm={p39.baseDm-natDm:+0.0000;-0.0000} km={p39.baseKm-natKm:+0.0000;-0.0000}");
        }

        // ── Part 6: Relaxation direction ──
        _o.WriteLine($"\n─── Relaxation Direction (T1->T2) ───");
        foreach(var g in new[]{("S1 Strict",s1),("S2 HiProxNP",s2),("S5 Failed",s5)}){
            var rr=g.Item2.Select(s=>res.First(r=>r.seed==s)).ToArray();
            if(rr.Length>0){
                double dD=rr.Average(r=>r.t2Dm-r.t1Dm),dK=rr.Average(r=>r.t2Km-r.t1Km);
                _o.WriteLine($"{g.Item1,-12}: dD={dD,8:F4} dK={dK,8:F4}  natHi-like={(dD<0&&dK>0?"YES":"no")}");
            }
        }

        // ── Part 7: Persistence predictability ──
        _o.WriteLine($"\n─── Pre-Intervention Predictability ──");
        var allPairs=pf.Select(p=>(p,res.First(r=>r.seed==p.seed))).ToArray();
        var strict=allPairs.Where(x=>x.Item2.immHi&&x.Item2.persist).ToArray();
        var nonPersist=allPairs.Where(x=>x.Item2.hiProx&&!(x.Item2.immHi&&x.Item2.persist)).ToArray();
        if(strict.Length>0&&nonPersist.Length>0){
            _o.WriteLine($"Strict (N={strict.Length}) vs HiProxNonPersist (N={nonPersist.Length}):");
            double sDm=strict.Average(x=>x.p.baseDm),nDm=nonPersist.Average(x=>x.p.baseDm);
            double sKm=strict.Average(x=>x.p.baseKm),nKm=nonPersist.Average(x=>x.p.baseKm);
            double sKs=strict.Average(x=>x.p.baseKs),nKs=nonPersist.Average(x=>x.p.baseKs);
            double sEs=strict.Average(x=>x.p.baseEs),nEs=nonPersist.Average(x=>x.p.baseEs);
            double sDD=strict.Average(x=>x.p.natDDm),nDD=nonPersist.Average(x=>x.p.natDDm);
            _o.WriteLine($"  baseDm:  {sDm:F4} vs {nDm:F4}  diff={sDm-nDm:+0.0000;-0.0000}");
            _o.WriteLine($"  baseKm:  {sKm:F4} vs {nKm:F4}  diff={sKm-nKm:+0.0000;-0.0000}");
            _o.WriteLine($"  baseKs:  {sKs:F4} vs {nKs:F4}  diff={sKs-nKs:+0.0000;-0.0000}");
            _o.WriteLine($"  baseEs:  {sEs:F4} vs {nEs:F4}  diff={sEs-nEs:+0.0000;-0.0000}");
            _o.WriteLine($"  natDDm:  {sDD:F4} vs {nDD:F4}  diff={sDD-nDD:+0.0000;-0.0000}");
        }
    }

    [Fact]public void HBG_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (seed-intrinsic signature): HBG_01 pre-intervention comparison");
        _o.WriteLine("Gate B (relaxation direction): HBG_01 T1->T2 deltas");
        _o.WriteLine("Gate C (NatHi proximity before): HBG_01 baseDh/baseDl");
        _o.WriteLine("Gate D (Seed 39 unique): HBG_01 forensic vs other strict");
        _o.WriteLine("Gate E (no predictive signature): HBG_01 predictability");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("N=71 primary. Pre-intervention = CP3 state.");
        _o.WriteLine("No physical claims. No cross-N (HBG_01 N=71 only).");
    }

    // ── Engine ──
    static double[][]Sim(double[,]K,int n,double s,int seed){
        var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;
        var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;
        int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();
        int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;
    }
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}

    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Dstd(double[,]d,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=d[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}

    static Snap MkSnap(double om,double dm,double km,double ks,double dstd,double kstd,P3 hi,P3 lo,EV ev)=>new Snap{om=om,dm=dm,km=km,ks=ks,dstd=dstd,kstd=kstd,hi=om>THR,dh=Dist(dm,km,ks,hi),dl=Dist(dm,km,ks,lo),es=ES(dm,km,ks,hi,lo),proj=Prj(dm-lo.dm,km-lo.km,ks-lo.ks,ev)};

    static P3 PCent(int n,bool hi){
        double d=0,k=0,ks=0;int c=0;
        for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<NE;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}
        return new P3{dm=d/c,km=k/c,ks=ks/c};
    }
    static EV GetEV(int n){var hi=PCent(n,true);var lo=PCent(n,false);return new EV{DD=hi.dm-lo.dm,DK=hi.km-lo.km,DKs=hi.ks-lo.ks};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}

    static SeedProfile ProfileSeed(int n,int seed,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        // Build to CP3 (3 epochs)
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        // CP3 state: simulate epoch 3 then compute metrics
        var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3End=Sim(K3,n,S,seed+50);double om3=Of(h3End,n).Average();
        var d3End=DL(Nm(RP(h3End,n),n),n);var K3End=Cupd(d3End,n);
        double dm3=Dm(d3End,n),km3=Km(K3End,n),ks3=Ks(K3End,n),dstd3=Dstd(d3End,n),kstd3=Ks(K3End,n);

        // Natural continuation: CP3->CP5 (no intervention)
        var hNat=Sim(K3,n,S,seed+200);var dNat=DL(Nm(RP(hNat,n),n),n);var KNat=Cupd(dNat,n);
        double dmNat=Dm(dNat,n),kmNat=Km(KNat,n);

        return new SeedProfile{seed=seed,baseDm=dm3,baseKm=km3,baseKs=ks3,baseDstd=dstd3,baseKstd=kstd3,
            baseOm=om3,baseEs=ES(dm3,km3,ks3,hi,lo),baseDh=Dist(dm3,km3,ks3,hi),baseDl=Dist(dm3,km3,ks3,lo),
            natDDm=dmNat-dm3,natDKm=kmNat-km3,isNatLo=true};
    }

    static PostInt ApplyIntervention(int n,int seed,double target,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);

        var hT1=Sim(K,n,S,seed+100);double omT1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);
        double dm1=Dm(dT1,n),km1=Km(kT1,n),ks1=Ks(kT1,n),dst1=Dstd(dT1,n),kst1=Ks(kT1,n);

        var hT2=Sim(kT1,n,S,seed+200);double omT2=Of(hT2,n).Average();
        var dT2=DL(Nm(RP(hT2,n),n),n);var kT2=Cupd(dT2,n);
        double dm2=Dm(dT2,n),km2=Km(kT2,n),ks2=Ks(kT2,n);

        return new PostInt{seed=seed,t1Dm=dm1,t1Km=km1,t1Ks=ks1,t1Om=omT1,t1Es=ES(dm1,km1,ks1,hi,lo),
            t1Dh=Dist(dm1,km1,ks1,hi),t1Dl=Dist(dm1,km1,ks1,lo),t1Dstd=dst1,t1Kstd=kst1,
            t2Dm=dm2,t2Km=km2,t2Ks=ks2,t2Om=omT2,t2Es=ES(dm2,km2,ks2,hi,lo),
            t2Dh=Dist(dm2,km2,ks2,hi),t2Dl=Dist(dm2,km2,ks2,lo),
            immHi=omT1>THR,persist=omT2>THR,hiProx=Dist(dm1,km1,ks1,hi)<Dist(dm1,km1,ks1,lo)};
    }

    static void CompareGroup(string label,List<int> seeds,Dictionary<int,SeedProfile> pfDict,ITestOutputHelper o){
        var vals=seeds.Where(pfDict.ContainsKey).Select(s=>pfDict[s]).ToArray();
        if(vals.Length==0){o.WriteLine($"{label}: N=0");return;}
        o.WriteLine($"{label,-12}: N={vals.Length,2} dm={vals.Average(p=>p.baseDm):F4} km={vals.Average(p=>p.baseKm):F4} ks={vals.Average(p=>p.baseKs):F4} Dh={vals.Average(p=>p.baseDh):F4} Dl={vals.Average(p=>p.baseDl):F4} Es={vals.Average(p=>p.baseEs):F4} nDD={vals.Average(p=>p.natDDm):F4}");
    }

    static double Prj(double dx,double dk,double dks,EV ev){double dot=dx*ev.DD+dk*ev.DK+dks*ev.DKs;double v2=ev.DD*ev.DD+ev.DK*ev.DK+ev.DKs*ev.DKs;return v2>1e-15?dot/Math.Sqrt(v2):0;}
    static double ES(double dm,double km,double ks,P3 hi,P3 lo)=>Dist(dm,km,ks,lo)-Dist(dm,km,ks,hi);
    static double Dist(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
