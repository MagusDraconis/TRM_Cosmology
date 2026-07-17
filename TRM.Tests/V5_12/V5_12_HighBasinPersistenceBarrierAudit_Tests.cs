using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBD"),Trait("Category","LongRunning")]
public class V5_12_HighBasinPersistenceBarrierAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EV{public double DD,DK,DKs;}
    struct P3{public double dm,km,ks;}
    struct Snap{public double om,dm,km,ks,es,proj,dh,dl;public bool hi;}
    struct Traj{public int seed;public Snap t0,t1,t2,t3;public bool hiProx,persist;}

    public V5_12_HighBasinPersistenceBarrierAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBD_01_CollapsePathAnalysis(){
        _o.WriteLine("═══ HBD: Persistence barrier — before-Cupd 25% d-compression at N=71 ═══");
        int n=71;
        var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
        _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4} ks={hi.ks:F4}");
        _o.WriteLine($"Lo: dm={lo.dm:F4} km={lo.km:F4} ks={lo.ks:F4}");
        _o.WriteLine($"Entry: dD={ev.DD:F4} dK={ev.DK:F4} dKs={ev.DKs:F4}");

        // Pre-compute Lo seed list + Natural Hi seeds for comparison
        var loSeeds=new List<int>();var hiSeeds=new List<int>();
        for(int s=0;s<100;s++){if(IsHi(n,s))hiSeeds.Add(s);else loSeeds.Add(s);}
        int nLo=30;var seeds=loSeeds.Take(nLo).ToArray();
        _o.WriteLine($"Natural Hi: {hiSeeds.Count}, Lo test seeds: {seeds.Length}");

        // Natural Hi trajectories (no intervention)
        var natHiTraj=new ConcurrentBag<Traj>();
        Parallel.ForEach(hiSeeds.Take(20).ToArray(),s=>{
            var t=NatTraj(n,s,hi,lo,ev,200);
            natHiTraj.Add(new Traj{seed=s,t0=t.t0,t1=t.t1,t2=t.t2,t3=t.t3,hiProx=true,persist=t.t2.hi});
        });
        var nhi=natHiTraj.ToArray();

        // Before-Cupd 25% d-compression
        double frac=0.25;
        var trajBag=new ConcurrentBag<Traj>();
        Parallel.ForEach(seeds,s=>{
            var t=B4CupdTraj(n,s,frac,hi,lo,ev);
            trajBag.Add(new Traj{seed=s,t0=t.t0,t1=t.t1,t2=t.t2,t3=t.t3,
                hiProx=t.t1.dh<t.t1.dl,persist=t.t1.hi&&t.t2.hi});
        });
        var all=trajBag.ToArray();

        // Classify
        var hiPrx=all.Where(t=>t.hiProx).ToArray();       // C3: Hi-proximal
        var immHi=all.Where(t=>t.t1.hi).ToArray();         // C4: immediate Hi
        var strct=all.Where(t=>t.t1.hi&&t.t2.hi).ToArray();// C6: strict persistent
        var nHiPrx=all.Where(t=>!t.hiProx).ToArray();      // not Hi-proximal (C7-like)

        _o.WriteLine($"\n─── State Class Inventory (before-Cupd {frac*100:F0}%) ───");
        _o.WriteLine($"Hi-proximal (distHi<distLo): {hiPrx.Length}");
        _o.WriteLine($"Immediate Hi (Omega>THR):    {immHi.Length}");
        _o.WriteLine($"Strict persistent:           {strct.Length}");
        _o.WriteLine($"Not Hi-proximal:             {nHiPrx.Length}");

        // Collapse analysis: T1→T2 for Hi-proximal non-persistent (C3)
        var c3=hiPrx.Where(t=>!t.persist).ToArray();
        _o.WriteLine($"\n─── Collapse Path: T1→T2 for {c3.Length} Hi-proximal non-persistent seeds ───");
        if(c3.Length>0){
            double dDm=c3.Average(t=>t.t2.dm-t.t1.dm),dKm=c3.Average(t=>t.t2.km-t.t1.km),dKs=c3.Average(t=>t.t2.ks-t.t1.ks);
            double dOm=c3.Average(t=>t.t2.om-t.t1.om),dEs=c3.Average(t=>t.t2.es-t.t1.es);
            double dDh=c3.Average(t=>t.t2.dh-t.t1.dh),dDl=c3.Average(t=>t.t2.dl-t.t1.dl);
            double dProj=c3.Average(t=>t.t2.proj-t.t1.proj);
            _o.WriteLine($"delta d_mean: {dDm,8:F4}  {(dDm>0?"⬆ REBOUND":"⬇ continue down")}");
            _o.WriteLine($"delta K_mean: {dKm,8:F4}  {(dKm<0?"⬇ COLLAPSE":"⬆ amplify")}");
            _o.WriteLine($"delta K_std:  {dKs,8:F4}  {(dKs>0?"⬆ WIDEN":"⬇ narrow")}");
            _o.WriteLine($"delta Omega:  {dOm,8:F4}");
            _o.WriteLine($"delta EntryS: {dEs,8:F4}  {(dEs<0?"⬇ COLLAPSE":"⬆ improve")}");
            _o.WriteLine($"delta distHi: {dDh,8:F4}  {(dDh>0?"⬆ AWAY":"⬇ closer")}");
            _o.WriteLine($"delta distLo: {dDl,8:F4}  {(dDl<0?"⬇ TOWARD Lo":"⬆ away")}");
            _o.WriteLine($"delta Proj:   {dProj,8:F4}  {(dProj<0?"⬇ AGAINST entry vector":"⬆ ALONG entry vector")}");
        }

        // Compare: Natural Hi T1→T2
        _o.WriteLine($"\n─── Natural High T1→T2 (control, {nhi.Length} seeds) ───");
        double nDm=nhi.Average(t=>t.t2.dm-t.t1.dm),nKm=nhi.Average(t=>t.t2.km-t.t1.km),nKs=nhi.Average(t=>t.t2.ks-t.t1.ks);
        double nOm=nhi.Average(t=>t.t2.om-t.t1.om),nEs=nhi.Average(t=>t.t2.es-t.t1.es);
        _o.WriteLine($"delta d_mean: {nDm,8:F4}  delta K_mean: {nKm,8:F4}  delta K_std: {nKs,8:F4}");
        _o.WriteLine($"delta Omega: {nOm,8:F4}  delta EntryS: {nEs,8:F4}");

        // Persistence separator: T1 state comparison
        _o.WriteLine($"\n─── Persistence Separator: T1 metrics ───");
        if(strct.Length>0&&c3.Length>0){
            _o.WriteLine($"Strict persistent (N={strct.Length}) vs Hi-prox non-persist (N={c3.Length}):");
            _o.WriteLine($"  d_mean:  {strct.Average(t=>t.t1.dm):F4} vs {c3.Average(t=>t.t1.dm):F4}  diff={strct.Average(t=>t.t1.dm)-c3.Average(t=>t.t1.dm):F4}");
            _o.WriteLine($"  K_mean:  {strct.Average(t=>t.t1.km):F4} vs {c3.Average(t=>t.t1.km):F4}  diff={strct.Average(t=>t.t1.km)-c3.Average(t=>t.t1.km):F4}");
            _o.WriteLine($"  K_std:   {strct.Average(t=>t.t1.ks):F4} vs {c3.Average(t=>t.t1.ks):F4}  diff={strct.Average(t=>t.t1.ks)-c3.Average(t=>t.t1.ks):F4}");
            _o.WriteLine($"  Omega:   {strct.Average(t=>t.t1.om):F4} vs {c3.Average(t=>t.t1.om):F4}");
            _o.WriteLine($"  EntryS:  {strct.Average(t=>t.t1.es):F4} vs {c3.Average(t=>t.t1.es):F4}");
            _o.WriteLine($"  distHi:  {strct.Average(t=>t.t1.dh):F4} vs {c3.Average(t=>t.t1.dh):F4}");
        }else{_o.WriteLine("Insufficient strict persistent or Hi-prox non-persist seeds for comparison.");}

        // Hi-proximal vs Natural Hi T1 comparison
        _o.WriteLine($"\n─── Hi-proximal induced vs Natural Hi at T1 ───");
        _o.WriteLine($"Induced Hi-prox: dm={hiPrx.Average(t=>t.t1.dm):F4} km={hiPrx.Average(t=>t.t1.km):F4} ks={hiPrx.Average(t=>t.t1.ks):F4} EntryS={hiPrx.Average(t=>t.t1.es):F4}");
        _o.WriteLine($"Natural Hi:      dm={nhi.Average(t=>t.t1.dm):F4} km={nhi.Average(t=>t.t1.km):F4} ks={nhi.Average(t=>t.t1.ks):F4} EntryS={nhi.Average(t=>t.t1.es):F4}");
    }

    [Fact]public void HBD_02_CoordinatedCollapsePath(){
        _o.WriteLine("═══ HBD: Coordinated d+K 100% collapse path at N=71 ═══");
        int n=71;
        var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<30;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Coordinated d+K at 100%
        double frac=1.00;
        var trajBag=new ConcurrentBag<Traj>();
        Parallel.ForEach(seeds,s=>{
            var t=CoordTraj(n,s,frac,hi,lo,ev);
            trajBag.Add(new Traj{seed=s,t0=t.t0,t1=t.t1,t2=t.t2,t3=t.t3,
                hiProx=t.t1.dh<t.t1.dl,persist=t.t1.hi&&t.t2.hi});
        });
        var all=trajBag.ToArray();

        var hiPrx=all.Where(t=>t.hiProx).ToArray();
        var immHi=all.Where(t=>t.t1.hi).ToArray();
        var strct=all.Where(t=>t.t1.hi&&t.t2.hi).ToArray();

        _o.WriteLine($"Hi-proximal: {hiPrx.Length}  ImmHi: {immHi.Length}  Strict: {strct.Length}");

        // Collapse for immHi non-persistent
        var c4=immHi.Where(t=>!t.persist).ToArray();
        _o.WriteLine($"\n─── Collapse: immHi non-persistent (N={c4.Length}) T1→T2 ───");
        if(c4.Length>0){
            _o.WriteLine($"d_mean: {c4.Average(t=>t.t2.dm-t.t1.dm):+0.0000;-0.0000}");
            _o.WriteLine($"K_mean: {c4.Average(t=>t.t2.km-t.t1.km):+0.0000;-0.0000}");
            _o.WriteLine($"K_std:  {c4.Average(t=>t.t2.ks-t.t1.ks):+0.0000;-0.0000}");
            _o.WriteLine($"Omega:  {c4.Average(t=>t.t2.om-t.t1.om):+0.0000;-0.0000}");
            _o.WriteLine($"distHi: {c4.Average(t=>t.t2.dh-t.t1.dh):+0.0000;-0.0000}");
            _o.WriteLine($"distLo: {c4.Average(t=>t.t2.dl-t.t1.dl):+0.0000;-0.0000}");
        }

        // T1: immHi vs Natural Hi centroid
        var nhiSeeds=new List<int>();for(int s=0;s<100&&nhiSeeds.Count<20;s++)if(IsHi(n,s))nhiSeeds.Add(s);
        var nhiBag=new ConcurrentBag<Snap>();
        Parallel.ForEach(nhiSeeds.ToArray(),s=>{nhiBag.Add(NatStateAt(n,s,hi,lo,ev,200));});
        var nhi=nhiBag.ToArray();
        _o.WriteLine($"\n─── immHi induced vs Natural Hi T1 ───");
        _o.WriteLine($"Induced: dm={immHi.Average(t=>t.t1.dm):F4} km={immHi.Average(t=>t.t1.km):F4} ks={immHi.Average(t=>t.t1.ks):F4}");
        _o.WriteLine($"NatHi:   dm={nhi.Average(t=>t.dm):F4} km={nhi.Average(t=>t.km):F4} ks={nhi.Average(t=>t.ks):F4}");
    }

    [Fact]public void HBD_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (d-rebound): HBD_01 collapse delta_d_mean");
        _o.WriteLine("Gate B (K-collapse): HBD_01 collapse delta_K_mean");
        _o.WriteLine("Gate C (K-structure): HBD_01 collapse delta_K_std");
        _o.WriteLine("Gate D (entry score insufficient): HBD_01 Hi-prox vs persist comparison");
        _o.WriteLine("Gate E (trajectory history): Hi-prox vs Natural Hi T1 comparison");
        _o.WriteLine("Gate F (rare persistent signature): HBD_01 strict persistent analysis");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("All observations at N=71, seeds 0-99. No cross-N claims.");
        _o.WriteLine("Strict persistence = immediate Hi + persists after +1 epoch.");
        _o.WriteLine("No physical interpretation. No sufficiency claims.");
    }

    // ── Engine ──
    static double[][]Sim(double[,]K,int n,double s,int seed){
        var rng=new Random(seed);
        var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;
        var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;
        int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();
        int hi=1;for(int t=0;t<St;t++){
            var dT=new double[n];
            for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}
            for(int i=0;i<n;i++)th[i]+=Dt*dT[i];
            if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();
        }return h;
    }
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];
        for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;
    }
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}

    // ── Metrics ──
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}

    static Snap MkSnap(double om,double dm,double km,double ks,P3 hi,P3 lo,EV ev){
        return new Snap{om=om,dm=dm,km=km,ks=ks,hi=om>THR,
            dh=Dist(dm,km,ks,hi),dl=Dist(dm,km,ks,lo),
            es=EntryS(dm,km,ks,hi,lo),proj=Prj(dm-lo.dm,km-lo.km,ks-lo.ks,ev)};
    }

    // ── Centroids ──
    static P3 PCent(int n,bool hi){
        double d=0,k=0,ks=0;int c=0;
        for(int s=0;s<100;s++){var K=KS(n,s);double dm=0,km=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sim(K,n,S,s+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}
            double om=Of(Sim(K,n,S,s+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}
        return new P3{dm=d/c,km=k/c,ks=ks/c};
    }
    static EV GetEV(int n){var hi=PCent(n,true);var lo=PCent(n,false);return new EV{DD=hi.dm-lo.dm,DK=hi.km-lo.km,DKs=hi.ks-lo.ks};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}

    // ── Full trajectories ──
    static (Snap t0,Snap t1,Snap t2,Snap t3) B4CupdTraj(int n,int seed,double frac,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        // Build to CP3
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        // T0: state after CP3 (before intervention)
        var h3=Sim(K,n,S,seed+3);
        var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3End=Sim(K3,n,S,seed+50);double om3=Of(h3End,n).Average();
        var d3End=DL(Nm(RP(h3End,n),n),n);var K3End=Cupd(d3End,n);
        var t0=MkSnap(om3,Dm(d3End,n),Km(K3End,n),Ks(K3End,n),hi,lo,ev);

        // Intervene: compress d before Cupd at CP4
        var h4=Sim(K,n,S,seed+3);
        var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double target=curDm+(hi.dm-curDm)*frac;
        var dM=CD(d4,n);double sc=target/Math.Max(curDm,1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=sc;
        K=Cupd(dM,n);
        // Epoch 5
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);
        // T1: state after intervention
        var hT1=Sim(K,n,S,seed+100);double omT1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);
        var t1=MkSnap(omT1,Dm(dT1,n),Km(KT1,n),Ks(KT1,n),hi,lo,ev);

        // T2: +1 continuation
        var hT2=Sim(KT1,n,S,seed+200);double omT2=Of(hT2,n).Average();
        var dT2=DL(Nm(RP(hT2,n),n),n);var KT2=Cupd(dT2,n);
        var t2=MkSnap(omT2,Dm(dT2,n),Km(KT2,n),Ks(KT2,n),hi,lo,ev);

        // T3: +2 continuation
        var hT3=Sim(KT2,n,S,seed+300);double omT3=Of(hT3,n).Average();
        var dT3=DL(Nm(RP(hT3,n),n),n);var KT3=Cupd(dT3,n);
        var t3=MkSnap(omT3,Dm(dT3,n),Km(KT3,n),Ks(KT3,n),hi,lo,ev);

        return (t0,t1,t2,t3);
    }

    static (Snap t0,Snap t1,Snap t2,Snap t3) CoordTraj(int n,int seed,double frac,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hE=Sim(K,n,S,seed+NE);var dE=DL(Nm(RP(hE,n),n),n);var KE=Cupd(dE,n);
        // T0
        var t0=MkSnap(Of(hE,n).Average(),Dm(dE,n),Km(KE,n),Ks(KE,n),hi,lo,ev);

        // Coordinated intervention
        double curDm=Dm(dE,n),curKm=Km(KE,n),curKs=Ks(KE,n);
        double tDm=curDm+ev.DD*frac,tKm=curKm+ev.DK*frac;
        var dM=CD(dE,n);double dSc=tDm/Math.Max(curDm,1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=dSc;
        var KM=Cupd(dM,n);double kSc=tKm/Math.Max(Km(KM,n),1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)KM[i,j]*=kSc;

        var h1=Sim(KM,n,S,seed+100);double om1=Of(h1,n).Average();
        var d1=DL(Nm(RP(h1,n),n),n);var K1=Cupd(d1,n);
        var t1=MkSnap(om1,Dm(d1,n),Km(K1,n),Ks(K1,n),hi,lo,ev);

        var h2=Sim(K1,n,S,seed+200);double om2=Of(h2,n).Average();
        var d2=DL(Nm(RP(h2,n),n),n);var K2=Cupd(d2,n);
        var t2=MkSnap(om2,Dm(d2,n),Km(K2,n),Ks(K2,n),hi,lo,ev);

        var h3=Sim(K2,n,S,seed+300);double om3=Of(h3,n).Average();
        var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var t3=MkSnap(om3,Dm(d3,n),Km(K3,n),Ks(K3,n),hi,lo,ev);

        return (t0,t1,t2,t3);
    }

    static (Snap t0,Snap t1,Snap t2,Snap t3) NatTraj(int n,int seed,P3 hi,P3 lo,EV ev,int baseOff){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h0=Sim(K,n,S,seed+NE);var d0=DL(Nm(RP(h0,n),n),n);var K0=Cupd(d0,n);
        var t0=MkSnap(0,0,0,0,hi,lo,ev); // unused for Nat
        var t1=MkSnap(Of(h0,n).Average(),Dm(d0,n),Km(K0,n),Ks(K0,n),hi,lo,ev);
        var h2=Sim(K0,n,S,seed+baseOff);var d2=DL(Nm(RP(h2,n),n),n);var K2=Cupd(d2,n);
        var t2=MkSnap(Of(h2,n).Average(),Dm(d2,n),Km(K2,n),Ks(K2,n),hi,lo,ev);
        var h3=Sim(K2,n,S,seed+baseOff+100);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var t3=MkSnap(Of(h3,n).Average(),Dm(d3,n),Km(K3,n),Ks(K3,n),hi,lo,ev);
        return (t0,t1,t2,t3);
    }

    static Snap NatStateAt(int n,int seed,P3 hi,P3 lo,EV ev,int off){
        var Kx=KS(n,seed);
        for(int e=0;e<NE;e++){var hs=Sim(Kx,n,S,seed+e);var ds=DL(Nm(RP(hs,n),n),n);Kx=Cupd(ds,n);}
        var hx=Sim(Kx,n,S,seed+off);double omx=Of(hx,n).Average();
        var dx=DL(Nm(RP(hx,n),n),n);var kf=Cupd(dx,n);
        return MkSnap(omx,Dm(dx,n),Km(kf,n),Ks(kf,n),hi,lo,ev);
    }

    static double Prj(double dx,double dk,double dks,EV ev){double dot=dx*ev.DD+dk*ev.DK+dks*ev.DKs;double v2=ev.DD*ev.DD+ev.DK*ev.DK+ev.DKs*ev.DKs;return v2>1e-15?dot/Math.Sqrt(v2):0;}
    static double EntryS(double dm,double km,double ks,P3 hi,P3 lo){return Dist(dm,km,ks,lo)-Dist(dm,km,ks,hi);}
    static double Dist(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
