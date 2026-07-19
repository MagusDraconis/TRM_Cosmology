using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_12;

[Trait("Category","V5_12"),Trait("Category","V5_12_HBC")]
public class V5_12_HighEntryVectorInterventionAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;const int NE=5;

    struct EntryVec{public double DD,DK,DKs;}

    public V5_12_HighEntryVectorInterventionAudit_Tests(ITestOutputHelper o){_o=o;}

    // ── Helper: pre-compute Lo seed list ──
    static int[] LoSeeds(int n,int maxSeeds,int maxScan){
        var list=new List<int>();
        for(int s=0;s<maxScan&&list.Count<maxSeeds;s++)if(!IsNatHi(n,s))list.Add(s);
        return list.ToArray();
    }

    [Fact][Trait("Category","LongRunning")]
    public void HBC_01_DSpaceOnlyIntervention(){
        _o.WriteLine("═══ I1: d-only Hi-entry projection at N=71 (Parallel) ═══");
        int n=71;int nLo=30;
        var ev=GetEntryVector(n);
        var hi=Cent(n,true);var lo=Cent(n,false);
        var seeds=LoSeeds(n,nLo,100);
        _o.WriteLine($"Entry: dD={ev.DD:F4} dK={ev.DK:F4} dKs={ev.DKs:F4}");
        _o.WriteLine($"Hi: dm={hi.dm:F4} km={hi.km:F4} ks={hi.ks:F4} Lo: dm={lo.dm:F4} km={lo.km:F4} ks={lo.ks:F4}");
        _o.WriteLine($"Lo seeds: {seeds.Length}");

        foreach(var f in new[]{0.25,0.50,0.75,1.00}){
            var rbag=new ConcurrentBag<Res>();
            Parallel.ForEach(seeds,s=>{rbag.Add(DoDOnly(n,s,f,hi,lo,ev));});
            var all=rbag.ToArray();int imm=all.Count(r=>r.imm),pers=all.Count(r=>r.per),stdp=all.Count(r=>r.sp);
            double sp=all.Average(r=>r.proj),ss=all.Average(r=>r.es);
            _o.WriteLine($"{f*100,3:F0}% ImmHi={imm,3} Persist={pers,3} Strict={stdp,3} Proj={sp,8:F4} {(sp>0?"TWD":"AWAY"),5} Score={ss,8:F4}");
        }
    }

    [Fact][Trait("Category","LongRunning")]
    public void HBC_02_KOnlyIntervention(){
        _o.WriteLine("═══ I2: K-only Hi-entry projection at N=71 (Parallel) ═══");
        int n=71;
        var ev=GetEntryVector(n);
        var hi=Cent(n,true);var lo=Cent(n,false);
        var seeds=LoSeeds(n,30,100);

        foreach(var f in new[]{0.25,0.50,0.75,1.00}){
            var rbag=new ConcurrentBag<Res>();
            Parallel.ForEach(seeds,s=>{rbag.Add(DoKOnly(n,s,f,hi,lo,ev));});
            var all=rbag.ToArray();int imm=all.Count(r=>r.imm),pers=all.Count(r=>r.per),stdp=all.Count(r=>r.sp);
            double sp=all.Average(r=>r.proj),ss=all.Average(r=>r.es);
            _o.WriteLine($"{f*100,3:F0}% ImmHi={imm,3} Persist={pers,3} Strict={stdp,3} Proj={sp,8:F4} {(sp>0?"TWD":"AWAY"),5} Score={ss,8:F4}");
        }
    }

    [Fact][Trait("Category","LongRunning")]
    public void HBC_03_CoordinatedDKIntervention(){
        _o.WriteLine("═══ I3: Coordinated d+K entry-vector at N=71 (Parallel) ═══");
        int n=71;
        var ev=GetEntryVector(n);
        var hi=Cent(n,true);var lo=Cent(n,false);
        var seeds=LoSeeds(n,30,100);

        foreach(var f in new[]{0.25,0.50,0.75,1.00,1.25}){
            var rbag=new ConcurrentBag<Res>();
            Parallel.ForEach(seeds,s=>{rbag.Add(DoCoord(n,s,f,hi,lo,ev));});
            var all=rbag.ToArray();int imm=all.Count(r=>r.imm),pers=all.Count(r=>r.per),stdp=all.Count(r=>r.sp);
            int ch=all.Count(r=>r.dh<r.dl),cl=all.Length-ch;
            double sp=all.Average(r=>r.proj),ss=all.Average(r=>r.es);
            _o.WriteLine($"{f*100,3:F0}% ImmHi={imm,3} Persist={pers,3} Strict={stdp,3} Proj={sp,8:F4} {(sp>0?"TWD":"AWAY"),5} Score={ss,8:F4} <Hi={ch,3} <Lo={cl,3}");
        }
    }

    [Fact][Trait("Category","LongRunning")]
    public void HBC_04_BeforeCupdChannelTest(){
        _o.WriteLine("═══ I6: Before-Cupd d-compression channel at N=71 (Parallel) ═══");
        int n=71;
        var ev=GetEntryVector(n);
        var hi=Cent(n,true);var lo=Cent(n,false);
        var seeds=LoSeeds(n,30,100);

        foreach(var f in new[]{0.25,0.50,0.75,1.00}){
            var rbag=new ConcurrentBag<Res>();
            Parallel.ForEach(seeds,s=>{rbag.Add(DoB4Cupd(n,s,f,hi,lo,ev));});
            var all=rbag.ToArray();int imm=all.Count(r=>r.imm),pers=all.Count(r=>r.per),stdp=all.Count(r=>r.sp);
            int ch=all.Count(r=>r.dh<r.dl),cl=all.Length-ch;
            double sp=all.Average(r=>r.proj),ss=all.Average(r=>r.es);
            _o.WriteLine($"{f*100,3:F0}% ImmHi={imm,3} Persist={pers,3} Strict={stdp,3} Proj={sp,8:F4} {(sp>0?"TWD":"AWAY"),5} Score={ss,8:F4} <Hi={ch,3} <Lo={cl,3}");
        }
    }

    [Fact]public void HBC_05_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (d-only sufficient): HBC_01");
        _o.WriteLine("Gate B (K-only sufficient): HBC_02");
        _o.WriteLine("Gate C (coordinated required): HBC_03");
        _o.WriteLine("Gate E (before-Cupd channel): HBC_04");
        _o.WriteLine("Gate G (no genuine entry): global assessment");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("All interventions applied at post-CP5 state.");
        _o.WriteLine("Strict persistence = immediate Hi + persists after +1 epoch.");
        _o.WriteLine("No physical claims. No cross-N claim (N=71 only).");
    }

    struct Res{public bool imm,per,sp;public double proj,es,dh,dl;}

    // ── Engine ──
    static double[][] Sim(double[,]K,int n,double s,int seed){
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
        for(int i=0;i<n;i++)for(int j=0;j<n;j++){
            double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}
            R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;
        }return R;
    }
    static double[,]Nm(double[,]R,int n){
        double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];
        double rng=1.0-mn;if(rng<1e-15)rng=1.0;
        var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;
    }
    static double[,]DL(double[,]R,int n){var d=new double[n,n];
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;
    }
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;
    }
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];
        for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;
    }
    static double[,]KS(int n,int seed){
        var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();
        double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}
        var v=new bool[n];var cs=new List<List<int>>();
        for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);
            while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}
        for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}
        var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;
    }

    // ── Metrics ──
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}

    // ── Precomputed centroids ──
    static (double dm,double km,double ks) Cent(int n,bool hi){
        double d=0,k=0,ks=0;int c=0;
        for(int s=0;s<100;s++){
            var K=KS(n,s);double dm=0,km=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sim(K,n,S,s+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}
            double om=Of(Sim(K,n,S,s+NE),n).Average();
            if((om>THR)==hi){d+=dm/NE;k+=km/NE;ks+=kss/NE;c++;}
        }
        return (d/c,k/c,ks/c);
    }

    static EntryVec GetEntryVector(int n){
        var hi=Cent(n,true);var lo=Cent(n,false);
        double dD=hi.dm-lo.dm,dK=hi.km-lo.km,dKs=hi.ks-lo.ks;
        return new EntryVec{DD=dD,DK=dK,DKs=dKs};
    }

    static bool IsNatHi(int n,int seed){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        return Of(Sim(K,n,S,seed+NE),n).Average()>THR;
    }

    // ── Interventions ──
    static Res DoDOnly(int n,int seed,double frac,(double dm,double km,double ks)hi,(double dm,double km,double ks)lo,EntryVec ev){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hE=Sim(K,n,S,seed+NE);var dE=DL(Nm(RP(hE,n),n),n);
        double curDm=Dm(dE,n);double target=curDm+(hi.dm-curDm)*frac;
        var dM=CD(dE,n);double sc=target/Math.Max(curDm,1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=sc;
        var K2=Cupd(dM,n);
        var h2=Sim(K2,n,S,seed+100);double imOm=Of(h2,n).Average();
        var d2=DL(Nm(RP(h2,n),n),n);var K3=Cupd(d2,n);
        var h3=Sim(K3,n,S,seed+200);double peOm=Of(h3,n).Average();
        double proj=Prj(Dm(d2,n)-lo.dm,Km(K3,n)-lo.km,Ks(K3,n)-lo.ks,ev);
        double es=EntryS(Dm(d2,n),Km(K3,n),Ks(K3,n),hi,lo);
        double dh=Dist(Dm(d2,n),Km(K3,n),Ks(K3,n),hi);
        double dl=Dist(Dm(d2,n),Km(K3,n),Ks(K3,n),lo);
        return new Res{imm=imOm>THR,per=peOm>THR,sp=imOm>THR&&peOm>THR,proj=proj,es=es,dh=dh,dl=dl};
    }

    static Res DoKOnly(int n,int seed,double frac,(double dm,double km,double ks)hi,(double dm,double km,double ks)lo,EntryVec ev){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hE=Sim(K,n,S,seed+NE);var dE=DL(Nm(RP(hE,n),n),n);var KE=Cupd(dE,n);
        double curKm=Km(KE,n);double target=curKm+(hi.km-curKm)*frac;
        var KM=CK(KE,n);double sc=target/Math.Max(curKm,1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)KM[i,j]*=sc;
        var h2=Sim(KM,n,S,seed+100);double imOm=Of(h2,n).Average();
        var d2=DL(Nm(RP(h2,n),n),n);var K3=Cupd(d2,n);
        var h3=Sim(K3,n,S,seed+200);double peOm=Of(h3,n).Average();
        double proj=Prj(Dm(d2,n)-lo.dm,Km(K3,n)-lo.km,Ks(K3,n)-lo.ks,ev);
        double es=EntryS(Dm(d2,n),Km(K3,n),Ks(K3,n),hi,lo);
        double dh=Dist(Dm(d2,n),Km(K3,n),Ks(K3,n),hi);
        double dl=Dist(Dm(d2,n),Km(K3,n),Ks(K3,n),lo);
        return new Res{imm=imOm>THR,per=peOm>THR,sp=imOm>THR&&peOm>THR,proj=proj,es=es,dh=dh,dl=dl};
    }

    static Res DoCoord(int n,int seed,double frac,(double dm,double km,double ks)hi,(double dm,double km,double ks)lo,EntryVec ev){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hE=Sim(K,n,S,seed+NE);var dE=DL(Nm(RP(hE,n),n),n);var KE=Cupd(dE,n);
        double curDm=Dm(dE,n),curKm=Km(KE,n),curKs=Ks(KE,n);
        double tDm=curDm+ev.DD*frac,tKm=curKm+ev.DK*frac,tKs=curKs+ev.DKs*frac;
        var dM=CD(dE,n);double dSc=tDm/Math.Max(curDm,1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=dSc;
        var KM=Cupd(dM,n);double kSc=tKm/Math.Max(Km(KM,n),1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)KM[i,j]*=kSc;
        var h2=Sim(KM,n,S,seed+100);double imOm=Of(h2,n).Average();
        var d2=DL(Nm(RP(h2,n),n),n);var K3=Cupd(d2,n);
        var h3=Sim(K3,n,S,seed+200);double peOm=Of(h3,n).Average();
        double proj=Prj(Dm(d2,n)-lo.dm,Km(K3,n)-lo.km,Ks(K3,n)-lo.ks,ev);
        double es=EntryS(Dm(d2,n),Km(K3,n),Ks(K3,n),hi,lo);
        double dh=Dist(Dm(d2,n),Km(K3,n),Ks(K3,n),hi);
        double dl=Dist(Dm(d2,n),Km(K3,n),Ks(K3,n),lo);
        return new Res{imm=imOm>THR,per=peOm>THR,sp=imOm>THR&&peOm>THR,proj=proj,es=es,dh=dh,dl=dl};
    }

    static Res DoB4Cupd(int n,int seed,double frac,(double dm,double km,double ks)hi,(double dm,double km,double ks)lo,EntryVec ev){
        var K=KS(n,seed);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);
        double curDm=Dm(d4,n);double target=curDm+(hi.dm-curDm)*frac;
        var dM=CD(d4,n);double sc=target/Math.Max(curDm,1e-10);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=sc;
        K=Cupd(dM,n);
        var h5=Sim(K,n,S,seed+4);var d5=DL(Nm(RP(h5,n),n),n);K=Cupd(d5,n);
        var hE=Sim(K,n,S,seed+100);double imOm=Of(hE,n).Average();
        var dE=DL(Nm(RP(hE,n),n),n);var KE=Cupd(dE,n);
        var hP=Sim(KE,n,S,seed+200);double peOm=Of(hP,n).Average();
        double proj=Prj(Dm(dE,n)-lo.dm,Km(KE,n)-lo.km,Ks(KE,n)-lo.ks,ev);
        double es=EntryS(Dm(dE,n),Km(KE,n),Ks(KE,n),hi,lo);
        double dh=Dist(Dm(dE,n),Km(KE,n),Ks(KE,n),hi);
        double dl=Dist(Dm(dE,n),Km(KE,n),Ks(KE,n),lo);
        return new Res{imm=imOm>THR,per=peOm>THR,sp=imOm>THR&&peOm>THR,proj=proj,es=es,dh=dh,dl=dl};
    }

    static double Prj(double dx,double dk,double dks,EntryVec ev){double dot=dx*ev.DD+dk*ev.DK+dks*ev.DKs;double v2=ev.DD*ev.DD+ev.DK*ev.DK+ev.DKs*ev.DKs;return v2>1e-15?dot/Math.Sqrt(v2):0;}
    static double EntryS(double dm,double km,double ks,(double dm,double km,double ks)hi,(double dm,double km,double ks)lo){return Dist(dm,km,ks,lo)-Dist(dm,km,ks,hi);}
    static double Dist(double dm,double km,double ks,(double dm,double km,double ks)c){double dd=dm-c.dm,dk=km-c.km,dks=ks-c.ks;return Math.Sqrt(dd*dd+dk*dk+dks*dks);}
}
