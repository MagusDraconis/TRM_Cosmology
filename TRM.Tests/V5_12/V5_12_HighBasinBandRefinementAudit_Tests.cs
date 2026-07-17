using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBF"),Trait("Category","LongRunning")]
public class V5_12_HighBasinBandRefinementAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EV{public double DD,DK,DKs;}
    struct P3{public double dm,km,ks;}
    struct Snap{public double om,dm,km,ks,es,proj,dh,dl;public bool hi;}
    struct Traj{public int seed;public double tgt;public Snap t1,t2;public bool hiProx,immHi,persist;}
    struct Band{public double dP05,dP10,dP25,dP40,dP50,dP60,dP75,dP90,dP95;public double dDrift;public int n;}
    struct SweepRow{public double tgt;public int n,imm,per,stp,hpx;public double dt1,kt1,dreb,kcol;}

    public V5_12_HighBasinBandRefinementAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void HBF_01_FineDT1Sweep(){
        _o.WriteLine("═══ HBF: Fine dT1 target sweep at N=71 ═══");
        int n=71,nLo=30;
        var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
        var band=MeasureBand(n,hi,lo,ev);
        _o.WriteLine($"NatHi Band (N={band.n}): d p05={band.dP05:F4} p10={band.dP10:F4} p25={band.dP25:F4} p50={band.dP50:F4} p75={band.dP75:F4} p90={band.dP90:F4} p95={band.dP95:F4}");
        _o.WriteLine($"NatHi d-drift: {band.dDrift:F4}");

        var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<nLo;s++)if(!IsHi(n,s))loSeeds.Add(s);
        var seeds=loSeeds.ToArray();

        // Fine sweep: 0.32 (over-compress) to 0.66 (above median)
        double[] targets={0.32,0.35,0.40,0.44,0.47,0.49,0.51,0.53,0.56,0.59,0.62,0.66};
        var rows=new ConcurrentBag<SweepRow>();

        foreach(var tgt in targets){
            var tbag=new ConcurrentBag<Traj>();
            Parallel.ForEach(seeds,s=>{tbag.Add(B4CupdT(n,s,tgt,hi,lo,ev));});
            var all=tbag.ToArray();
            int imm=all.Count(t=>t.immHi),per=all.Count(t=>t.persist),stp=all.Count(t=>t.immHi&&t.persist);
            int hpx=all.Count(t=>t.hiProx);
            double dt1=all.Average(t=>t.t1.dm),kt1=all.Average(t=>t.t1.km);
            double dreb=all.Average(t=>t.t2.dm-t.t1.dm),kcol=all.Average(t=>t.t2.km-t.t1.km);
            rows.Add(new SweepRow{tgt=tgt,n=all.Length,imm=imm,per=per,stp=stp,hpx=hpx,dt1=dt1,kt1=kt1,dreb=dreb,kcol=kcol});
        }

        var sr=rows.OrderBy(r=>r.tgt).ToArray();
        _o.WriteLine(string.Format("\n{0,7} {1,5} {2,7} {3,6} {4,6} {5,8} {6,8} {7,8} {8,8} {9,8}","Target","ImmHi","Persist","Strict","HiProx","dT1","kT1","dReb","kCol","RebDir"));
        foreach(var r in sr){
            string rd=r.dreb>0.02?"REBOUND":r.dreb<-0.02?"DRIFT DN":"NEUTRAL";
            _o.WriteLine($"{r.tgt,7:F2} {r.imm,5} {r.per,7} {r.stp,6} {r.hpx,6} {r.dt1,8:F4} {r.kt1,8:F4} {r.dreb,8:F4} {r.kcol,8:F4} {rd,8}");
        }

        // Optimal band estimate
        var best=sr.Where(r=>r.stp>0).ToArray();
        if(best.Length>0){
            _o.WriteLine($"\n─── Optimal Band Estimate ───");
            _o.WriteLine($"Strict persistence found at targets: {string.Join(", ",best.Select(r=>r.tgt.ToString("F2")))}");
            _o.WriteLine($"dT1 range: [{best.Min(r=>r.dt1):F4}, {best.Max(r=>r.dt1):F4}]");
            _o.WriteLine($"dReb range: [{best.Min(r=>r.dreb):F4}, {best.Max(r=>r.dreb):F4}]");
        }
        // Rebound curve summary
        _o.WriteLine($"\n─── Rebound Reversal ───");
        var reb=sr.Where(r=>r.dreb>0).ToArray();var drift=sr.Where(r=>r.dreb<=0).ToArray();
        _o.WriteLine($"Rebound (dReb>0): targets {string.Join(", ",reb.Select(r=>r.tgt.ToString("F2")))} — max stp={reb.Max(r=>r.stp)}");
        _o.WriteLine($"Drift (dReb<=0): targets {string.Join(", ",drift.Select(r=>r.tgt.ToString("F2")))} — max stp={drift.Max(r=>r.stp)}");

        // Strict persistent seed detail
        var allTraj=new ConcurrentBag<Traj>();
        foreach(var tgt in targets)Parallel.ForEach(seeds,s=>{allTraj.Add(B4CupdT(n,s,tgt,hi,lo,ev));});
        var at=allTraj.ToArray();
        var stpSeeds=at.Where(t=>t.immHi&&t.persist).GroupBy(t=>t.seed).Select(g=>g.First()).ToArray();
        if(stpSeeds.Length>0){
            _o.WriteLine($"\n─── Strict Persistent Seeds (unique) ───");
            foreach(var s in stpSeeds.OrderBy(s=>s.seed))
                _o.WriteLine($"Seed {s.seed,3}: tgt={s.tgt:F2} dT1={s.t1.dm:F4} kT1={s.t1.km:F4} dT2={s.t2.dm:F4} kT2={s.t2.km:F4} dReb={s.t2.dm-s.t1.dm:F4}");
        }
    }

    [Fact]public void HBF_02_CrossNValidation(){
        _o.WriteLine("═══ HBF: Cross-N validation at best dT1 targets ═══");
        int nLo=20;
        // Test p50 and UNDER-equivalent at N=67, N=71, N=72
        foreach(var n in new[]{67,71,72}){
            _o.WriteLine($"\n─── N={n} ───");
            var ev=GetEV(n);var hi=PCent(n,true);var lo=PCent(n,false);
            var band=MeasureBand(n,hi,lo,ev);
            _o.WriteLine($"Band: p25={band.dP25:F4} p50={band.dP50:F4} p75={band.dP75:F4} p90={band.dP90:F4} drift={band.dDrift:F4}");

            var loSeeds=new List<int>();for(int s=0;s<100&&loSeeds.Count<nLo;s++)if(!IsHi(n,s))loSeeds.Add(s);
            var seeds=loSeeds.ToArray();

            // Test p50 and p90 targets at each N
            double[] tgts={band.dP50,band.dP90};
            foreach(var tgt in tgts){
                var tbag=new ConcurrentBag<Traj>();
                Parallel.ForEach(seeds,s=>{tbag.Add(B4CupdT(n,s,tgt,hi,lo,ev));});
                var all=tbag.ToArray();
                int imm=all.Count(t=>t.immHi),per=all.Count(t=>t.persist),stp=all.Count(t=>t.immHi&&t.persist);
                int hpx=all.Count(t=>t.hiProx);
                double dt1=all.Average(t=>t.t1.dm),dreb=all.Average(t=>t.t2.dm-t.t1.dm);
                _o.WriteLine($"tgt={tgt:F2}: ImmHi={imm,3} Persist={per,3} Strict={stp,3} HiProx={hpx,3} dT1={dt1:F4} dReb={dreb,8:F4}");
            }
        }
    }

    [Fact]public void HBF_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (stable dT1 band): HBF_01 sweep");
        _o.WriteLine("Gate B (d only sufficient): HBF_01 strict persistence count");
        _o.WriteLine("Gate E (N=71 specific): HBF_02 cross-N");
        _o.WriteLine("Gate F (over-compression rebound): HBF_01 targets <0.40");
        _o.WriteLine("Gate G (no reproducible band): global assessment");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("N=71 primary, N=67/72 validation. Band = Natural Hi d_mean percentiles.");
        _o.WriteLine("Strict persistence = immHi + survives +1 continuation. No physical claims.");
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

    static Band MeasureBand(int n,P3 hi,P3 lo,EV ev){
        var hiSeeds=new List<int>();for(int sd=0;sd<100;sd++)if(IsHi(n,sd))hiSeeds.Add(sd);
        var snaps=new ConcurrentBag<(double dm,double dr)>();
        Parallel.ForEach(hiSeeds.ToArray(),s=>{
            var K=KS(n,s);for(int e=0;e<NE;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
            var h1=Sim(K,n,S,s+NE);var d1=DL(Nm(RP(h1,n),n),n);var K1=Cupd(d1,n);
            double dm1=Dm(d1,n);
            var h2=Sim(K1,n,S,s+200);var d2=DL(Nm(RP(h2,n),n),n);
            double dm2=Dm(d2,n);
            snaps.Add((dm1,dm2-dm1));
        });
        var arr=snaps.ToArray();var d=arr.Select(x=>x.dm).OrderBy(x=>x).ToArray();
        return new Band{dP05=P(d,5),dP10=P(d,10),dP25=P(d,25),dP40=P(d,40),dP50=P(d,50),dP60=P(d,60),dP75=P(d,75),dP90=P(d,90),dP95=P(d,95),dDrift=arr.Average(x=>x.dr),n=arr.Length};
    }
    static double P(double[]a,int p){int i=Math.Clamp(a.Length*p/100,0,a.Length-1);return a[i];}

    static Traj B4CupdT(int n,int seed,double target,P3 hi,P3 lo,EV ev){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double frac=Math.Clamp((target+1e-9)/(curDm+1e-9),0.01,100.0);
        var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);

        var hT1=Sim(K,n,S,seed+100);double omT1=Of(hT1,n).Average();
        var dT1=DL(Nm(RP(hT1,n),n),n);var kT1=Cupd(dT1,n);
        var t1=MkSnap(omT1,Dm(dT1,n),Km(kT1,n),Ks(kT1,n),hi,lo,ev);

        var hT2=Sim(kT1,n,S,seed+200);double omT2=Of(hT2,n).Average();
        var dT2=DL(Nm(RP(hT2,n),n),n);var kT2=Cupd(dT2,n);
        var t2=MkSnap(omT2,Dm(dT2,n),Km(kT2,n),Ks(kT2,n),hi,lo,ev);

        return new Traj{seed=seed,tgt=target,t1=t1,t2=t2,hiProx=t1.dh<t1.dl,immHi=t1.hi,persist=t2.hi};
    }

    static double Prj(double dx,double dk,double dks,EV ev){double dot=dx*ev.DD+dk*ev.DK+dks*ev.DKs;double v2=ev.DD*ev.DD+ev.DK*ev.DK+ev.DKs*ev.DKs;return v2>1e-15?dot/Math.Sqrt(v2):0;}
    static double ES(double dm,double km,double ks,P3 hi,P3 lo)=>Dist(dm,km,ks,lo)-Dist(dm,km,ks,hi);
    static double Dist(double dm,double km,double ks,P3 c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
