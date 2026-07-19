using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBEQ"),Trait("Category","LongRunning")]
public class V5_12_HighBasinEquilibriumBandAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EV{public double DD,DK,DKs;}
    struct P3{public double dm,km,ks;}
    struct Snap{public double om,dm,km,ks,es,proj,dh,dl;public bool hi;}
    struct Traj{public int seed;public double frac;public Snap t0,t1,t2,t3;public bool hiProx,immHi,persist;}
    struct Band{public double dP10,dP25,dP50,dP75,dP90,kP10,kP25,kP50,kP75,kP90,ksP10,ksP25,ksP50,ksP75,ksP90;
        public double dDrift,kDrift,ksDrift;public int n;}

    public V5_12_HighBasinEquilibriumBandAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBEQ_01_NaturalHiBandAndTargetedIntervention(){
        _o.WriteLine("═══ HBEQ: Natural Hi equilibrium band + targeted d-compression at N=71 ═══");
        int n=71;
        var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
        _o.WriteLine($"Hi centroid: dm={hi.dm:F4} km={hi.km:F4} ks={hi.ks:F4}");
        _o.WriteLine($"Lo centroid: dm={lo.dm:F4} km={lo.km:F4} ks={lo.ks:F4}");

        // ── Part 1: Measure Natural Hi band at CP5 ──
        var hiSeeds=new List<int>();for(int s=0;s<100;s++)if(IsHi(n,s))hiSeeds.Add(s);
        var hiSnaps=new ConcurrentBag<(double dm,double km,double ks,double om,double dDrift,double kDrift,double ksDrift)>();
        Parallel.ForEach(hiSeeds.ToArray(),s=>{
            var(nh,st)=HiBandSample(n,s,hi,lo,ev);
            hiSnaps.Add(nh);
        });
        var hs=hiSnaps.ToArray();
        var band=BandFrom(hs);
        _o.WriteLine($"\n─── Natural Hi Band (N={band.n}) ───");
        _o.WriteLine($"d_mean: p10={band.dP10:F4} p25={band.dP25:F4} p50={band.dP50:F4} p75={band.dP75:F4} p90={band.dP90:F4}");
        _o.WriteLine($"K_mean: p10={band.kP10:F4} p25={band.kP25:F4} p50={band.kP50:F4} p75={band.kP75:F4} p90={band.kP90:F4}");
        _o.WriteLine($"K_std:  p10={band.ksP10:F4} p25={band.ksP25:F4} p50={band.ksP50:F4} p75={band.ksP75:F4} p90={band.ksP90:F4}");
        _o.WriteLine($"Natural drift: dD={band.dDrift:F4} dK={band.kDrift:F4} dKs={band.ksDrift:F4}");

        // Lo seeds
        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<30;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // ── Part 2: Test band-targeted d-compression ──
        // Targets: p25(compressed), p50(moderate), p75(mild), over-compress below p10, under-entry above p90
        double[] targets=new[]{band.dP10*0.9,band.dP25,band.dP50,band.dP75,band.dP90*1.1};
        string[] labels=new[]{"OVER"," p25"," p50"," p75","UNDER"};

        _o.WriteLine($"\n─── Band-Targeted Before-Cupd d-Compression ({seeds.Length} Lo seeds) ───");
        _o.WriteLine($"Targets: OVER={targets[0]:F4} p25={targets[1]:F4} p50={targets[2]:F4} p75={targets[3]:F4} UNDER={targets[4]:F4}");
        _o.WriteLine(string.Format("\n{0,-6} {1,5} {2,7} {3,6} {4,6} {5,6} {6,8} {7,8} {8,8} {9,8}","Target","ImmHi","Persist","Strict","HiProx","InBand","dT1","kT1","dReb","kColl"));

        for(int ti=0;ti<targets.Length;ti++){
            double tgt=targets[ti];
            var tbag=new ConcurrentBag<Traj>();
            Parallel.ForEach(seeds,s=>{tbag.Add(B4CupdToTarget(n,s,tgt,hi,lo,ev));});
            var all=tbag.ToArray();
            int imm=all.Count(t=>t.immHi),per=all.Count(t=>t.persist),stp=all.Count(t=>t.immHi&&t.persist);
            int hpx=all.Count(t=>t.hiProx),inb=all.Count(t=>InBand(t.t1,band));
            double dT1=all.Average(t=>t.t1.dm),kT1=all.Average(t=>t.t1.km);
            double dReb=all.Average(t=>t.t2.dm-t.t1.dm),kCol=all.Average(t=>t.t2.km-t.t1.km);
            _o.WriteLine($"{labels[ti],-6} {imm,5} {per,7} {stp,6} {hpx,6} {inb,6} {dT1,8:F4} {kT1,8:F4} {dReb,8:F4} {kCol,8:F4}");
        }

        // ── Part 3: Hi-proximal vs band success ──
        _o.WriteLine($"\n─── Band Membership vs Persistence ───");
        // Collect all trajectories from all targets
        var allTraj=new ConcurrentBag<Traj>();
        foreach(var tgt in targets)Parallel.ForEach(seeds,s=>{allTraj.Add(B4CupdToTarget(n,s,tgt,hi,lo,ev));});
        var at=allTraj.ToArray();
        var inBandHiProx=at.Where(t=>InBand(t.t1,band)&&t.hiProx).ToArray();
        var outBandHiProx=at.Where(t=>!InBand(t.t1,band)&&t.hiProx).ToArray();
        _o.WriteLine($"In-band Hi-prox: {inBandHiProx.Length}, persist: {inBandHiProx.Count(t=>t.persist)}, strict: {inBandHiProx.Count(t=>t.immHi&&t.persist)}");
        _o.WriteLine($"Out-band Hi-prox: {outBandHiProx.Length}, persist: {outBandHiProx.Count(t=>t.persist)}, strict: {outBandHiProx.Count(t=>t.immHi&&t.persist)}");

        // Rebound vs band distance
        _o.WriteLine($"\n─── Rebound vs Band Distance ───");
        var hpxAll=at.Where(t=>t.hiProx).ToArray();
        if(hpxAll.Length>0){
            var below=hpxAll.Where(t=>t.t1.dm<band.dP25).ToArray();
            var within=hpxAll.Where(t=>t.t1.dm>=band.dP25&&t.t1.dm<=band.dP75).ToArray();
            var above=hpxAll.Where(t=>t.t1.dm>band.dP75).ToArray();
            if(below.Length>0)_o.WriteLine($"Below p25 (N={below.Length}): dReb={below.Average(t=>t.t2.dm-t.t1.dm):F4} kCol={below.Average(t=>t.t2.km-t.t1.km):F4}");
            if(within.Length>0)_o.WriteLine($"Within IQR (N={within.Length}): dReb={within.Average(t=>t.t2.dm-t.t1.dm):F4} kCol={within.Average(t=>t.t2.km-t.t1.km):F4}");
            if(above.Length>0)_o.WriteLine($"Above p75 (N={above.Length}): dReb={above.Average(t=>t.t2.dm-t.t1.dm):F4} kCol={above.Average(t=>t.t2.km-t.t1.km):F4}");
        }
    }

    [Fact]public void HBEQ_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (d_mean band sufficient): HBEQ_01 band-target results");
        _o.WriteLine("Gate E (over-compression rebound confirmed): HBEQ_01 rebound vs band distance");
        _o.WriteLine("Gate G (no band intervention works): global assessment");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("N=71 only. Band = Natural Hi d_mean IQR.");
        _o.WriteLine("Strict persistence = immHi + survives +1 continuation.");
        _o.WriteLine("No physical interpretation. No cross-N claims.");
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
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}

    static Snap MkSnap(double om,double dm,double km,double ks,P3 hi,P3 lo,EV ev)=>new Snap{om=om,dm=dm,km=km,ks=ks,hi=om>THR,dh=Dist(dm,km,ks,hi),dl=Dist(dm,km,ks,lo),es=ES(dm,km,ks,hi,lo),proj=Prj(dm-lo.dm,km-lo.km,ks-lo.ks,ev)};

    static P3 PCent(int n,bool hi){
        double d=0,k=0,ks=0;int c=0;
        for(int s=0;s<100;s++){var K=KS(n,s);double dm=0,km=0,kss=0;for(int e=0;e<NE;e++){var h=Sim(K,n,S,s+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,s+NE),n).Average();if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}}
        return new P3{dm=d/c,km=k/c,ks=ks/c};
    }
    static EV GetEV(int n){var hi=PCent(n,true);var lo=PCent(n,false);return new EV{DD=hi.dm-lo.dm,DK=hi.km-lo.km,DKs=hi.ks-lo.ks};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+NE),n).Average()>THR;}

    // ── Hi band sampling ──
    static ((double dm,double km,double ks,double om,double dD,double kD,double ksD),Snap) HiBandSample(int n,int seed,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hT1=Sim(K,n,S,seed+NE);var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);
        double om1=Of(hT1,n).Average(),dm1=Dm(dT1,n),km1=Km(kT1,n),ks1=Ks(kT1,n);

        var hT2=Sim(kT1,n,S,seed+200);var dT2=DL(Nm(RP(hT2,n),n),n);var kT2=Cupd(dT2,n);
        double om2=Of(hT2,n).Average(),dm2=Dm(dT2,n),km2=Km(kT2,n),ks2=Ks(kT2,n);

        var s1=MkSnap(om1,dm1,km1,ks1,hi,lo,ev);
        return ((dm1,km1,ks1,om1,dm2-dm1,km2-km1,ks2-ks1),s1);
    }

    static Band BandFrom((double dm,double km,double ks,double om,double dD,double kD,double ksD)[] samples){
        int n=samples.Length;
        double[] d=samples.Select(x=>x.dm).OrderBy(x=>x).ToArray();
        double[] k=samples.Select(x=>x.km).OrderBy(x=>x).ToArray();
        double[] ks=samples.Select(x=>x.ks).OrderBy(x=>x).ToArray();
        return new Band{
            dP10=P(d,10),dP25=P(d,25),dP50=P(d,50),dP75=P(d,75),dP90=P(d,90),
            kP10=P(k,10),kP25=P(k,25),kP50=P(k,50),kP75=P(k,75),kP90=P(k,90),
            ksP10=P(ks,10),ksP25=P(ks,25),ksP50=P(ks,50),ksP75=P(ks,75),ksP90=P(ks,90),
            dDrift=samples.Average(x=>x.dD),kDrift=samples.Average(x=>x.kD),ksDrift=samples.Average(x=>x.ksD),n=n};
    }
    static double P(double[]a,int p){int i=(int)(a.Length*p/100.0);return a[Math.Clamp(i,0,a.Length-1)];}
    static bool InBand(Snap s,Band b)=>s.dm>=b.dP25&&s.dm<=b.dP75;

    // ── Before-Cupd to target d_mean ──
    static Traj B4CupdToTarget(int n,int seed,double target,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        // CP3 baseline snapshot
        var h3=Sim(K,n,S,seed+50);var d3=DL(Nm(RP(h3,n),n),n);var k3=Cupd(d3,n);
        var t0=MkSnap(Of(h3,n).Average(),Dm(d3,n),Km(k3,n),Ks(k3,n),hi,lo,ev);

        // Intervene at CP4: compress d toward target before Cupd
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=(target+1e-9)/(curDm+1e-9);frac=Math.Clamp(frac,0.01,100.0);
        var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        // Epoch 5
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);

        var hT1=Sim(K,n,S,seed+100);double omT1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);
        var t1=MkSnap(omT1,Dm(dT1,n),Km(kT1,n),Ks(kT1,n),hi,lo,ev);

        var hT2=Sim(kT1,n,S,seed+200);double omT2=Of(hT2,n).Average();
        var dT2=DL(Nm(RP(hT2,n),n),n);var kT2=Cupd(dT2,n);
        var t2=MkSnap(omT2,Dm(dT2,n),Km(kT2,n),Ks(kT2,n),hi,lo,ev);

        var hT3=Sim(kT2,n,S,seed+300);double omT3=Of(hT3,n).Average();
        var dT3=DL(Nm(RP(hT3,n),n),n);var kT3=Cupd(dT3,n);
        var t3=MkSnap(omT3,Dm(dT3,n),Km(kT3,n),Ks(kT3,n),hi,lo,ev);

        return new Traj{seed=seed,frac=frac,t0=t0,t1=t1,t2=t2,t3=t3,
            hiProx=t1.dh<t1.dl,immHi=t1.hi,persist=t2.hi};
    }

    static double Prj(double dx,double dk,double dks,EV ev){double dot=dx*ev.DD+dk*ev.DK+dks*ev.DKs;double v2=ev.DD*ev.DD+ev.DK*ev.DK+ev.DKs*ev.DKs;return v2>1e-15?dot/Math.Sqrt(v2):0;}
    static double ES(double dm,double km,double ks,P3 hi,P3 lo)=>Dist(dm,km,ks,lo)-Dist(dm,km,ks,hi);
    static double Dist(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
