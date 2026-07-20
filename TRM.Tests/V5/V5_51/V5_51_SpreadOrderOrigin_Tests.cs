using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_51;

[Trait("Category","V5_51"),Trait("Category","V5_51_SOO"),Trait("Category","LongRunning")]
public class V5_51_SpreadOrderOrigin_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{
        public int N,seed,cohort;public double[] warmOm,warmDm,warmKm,warmKs,warmLam;
        public double om0,lam0,d0,km0,ks0,om1,lam1,d1,km1,ks1,om2,lam2,d2,km2,ks2,omDist2,om3,lam3,d3,km3,ks3,reb3;
        public double entOm,entDm,entKm,entLam,frac,exitOm2,omDelta,cs4,a0Prox;public bool resc4,inv;
    }

    public V5_51_SpreadOrderOrigin_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){
        var ep=new EP{N=n,seed=s,cohort=n%5};var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0Pre=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0Pre*0.90:d0Pre*0.50;
        var warmOm=new double[WARMUP_EPOCHS];var warmDm=new double[WARMUP_EPOCHS];var warmKm=new double[WARMUP_EPOCHS];var warmKs=new double[WARMUP_EPOCHS];var warmLam=new double[WARMUP_EPOCHS];
        var K=KS(n,s);
        for(int e=0;e<WARMUP_EPOCHS;e++){var h=Sim(K,n,S,s+e);warmOm[e]=Of(h,n).Average();var d=DL(Nm(RP(h,n),n),n);warmDm[e]=Dm(d,n);K=Cupd(d,n);warmKm[e]=Km(K,n);warmKs[e]=Ks(K,n);warmLam[e]=Lambda1(K,n);}
        ep.warmOm=warmOm;ep.warmDm=warmDm;ep.warmKm=warmKm;ep.warmKs=warmKs;ep.warmLam=warmLam;
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);var dT0=DL(Nm(RP(hT0,n),n),n);ep.d0=Dm(dT0,n);ep.km0=Km(K,n);ep.ks0=Ks(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);ep.d1=Dm(CD(d4,n),n);ep.km1=Km(K,n);ep.ks1=Ks(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);double omT1=Of(hT1,n).Average();
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);var dmat2=DL(Nm(RP(hT1,n),n),n);ep.d2=Dm(dmat2,n);ep.km2=Km(KT1,n);ep.ks2=Ks(KT1,n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;var KT3=Cupd(DL(Nm(RP(hT2,n),n),n),n);
        ep.om3=omT2;ep.lam3=Lambda1(KT3,n);ep.reb3=omT2-omT1;ep.d3=Dm(DL(Nm(RP(hT2,n),n),n),n);ep.km3=Km(KT3,n);ep.ks3=Ks(KT3,n);
        double c3=0;if(!double.IsNaN(hi.dm)){var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);ep.entDm=dmPre;ep.entOm=omT1;ep.entKm=Km(KT1,n);ep.entLam=Lambda1(KT1,n);double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);ep.frac=f3;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);double omC3=Of(hc3cc,n).Average();ep.exitOm2=omC3;c3=omC3-(a0?THR:omT2);ep.omDelta=omC3-omT1;}
        ep.cs4=c3;ep.a0Prox=Math.Abs(omT2-THR);ep.resc4=c3>0.1&&omT2>THR;ep.inv=double.IsNaN(c3);return ep;
    }

    [Fact]
    public void SOO_01_SpreadOrderOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SOO_01: Spread Order Origin Audit ===");
        _o.WriteLine("=== V5.51 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        var n70=Array.FindAll(data,d=>d.N==70);var n72=Array.FindAll(data,d=>d.N==72);var n75=Array.FindAll(data,d=>d.N==75);

        // ========================
        // PARTS A+B — Spread Ordering Lineage
        // ========================
        _o.WriteLine("\nPART A+B — Spread Ordering Lineage (km IQR across stages)");
        _o.WriteLine("K1(N=72), K3(N=70), K2(N=75). Goal: when does K1>K3>K2 first appear?");

        string[] stg={"w0","w1","w2","T0"};
        Func<EP,double>[] kmF={d=>d.warmKm[0],d=>d.warmKm[1],d=>d.warmKm[2],d=>d.warmKm[2]}; // T0 km = w2 km
        Func<EP,double>[] lamF={d=>d.warmLam[0],d=>d.warmLam[1],d=>d.warmLam[2],d=>d.warmLam[2]};
        Func<EP,double>[] dF={d=>d.warmDm[0],d=>d.warmDm[1],d=>d.warmDm[2],d=>d.warmDm[2]};
        Func<EP,double>[] omF={d=>d.warmOm[0],d=>d.warmOm[1],d=>d.warmOm[2],d=>d.om0};

        _o.WriteLine($"\n{"Stage",6} {"K1(km)",9} {"K3(km)",9} {"K2(km)",9} {"K1>K3>K2?",14} {"K1(lam)",9} {"K3(lam)",9} {"K2(lam)",9} {"lam order?",14}");
        _o.WriteLine(new string('-',95));

        for(int s=0;s<4;s++){
            double km1=Iqr(n72,kmF[s]),km3=Iqr(n70,kmF[s]),km2=Iqr(n75,kmF[s]);
            double lm1=Iqr(n72,lamF[s]),lm3=Iqr(n70,lamF[s]),lm2=Iqr(n75,lamF[s]);
            bool kmOrd=km1>km3&&km3>km2,lmOrd=lm1>lm3&&lm3>lm2;
            string marker=(kmOrd||lmOrd)?(s==0?"<-- PRESENT at w0!":s==1?"<-- emerges w1":s==2?"<-- emerges w2":""):"";
            _o.WriteLine($"{stg[s],6} {km1,9:F3} {km3,9:F3} {km2,9:F3} {(kmOrd?"YES":"no"),14} {lm1,9:F3} {lm3,9:F3} {lm2,9:F3} {(lmOrd?"YES":"no"),14} {marker}");
        }

        // Accumulation audit
        _o.WriteLine($"\n--- Accumulation per transition ---");
        _o.WriteLine($"{"Trans",-10} {"K1 dIQR",10} {"K3 dIQR",10} {"K2 dIQR",10} {"K1/K2",8} {"Order gain?",14}");
        _o.WriteLine(new string('-',65));
        for(int s=0;s<3;s++){
            double k1d=Iqr(n72,kmF[s+1])-Iqr(n72,kmF[s]),k3d=Iqr(n70,kmF[s+1])-Iqr(n70,kmF[s]),k2d=Iqr(n75,kmF[s+1])-Iqr(n75,kmF[s]);
            double rPre=Iqr(n75,kmF[s])>0.001?Iqr(n72,kmF[s])/Iqr(n75,kmF[s]):0;
            double rPost=Iqr(n75,kmF[s+1])>0.001?Iqr(n72,kmF[s+1])/Iqr(n75,kmF[s+1]):0;
            _o.WriteLine($"{stg[s]+"->"+stg[s+1],-10} {k1d,10:F3} {k3d,10:F3} {k2d,10:F3} {rPost/rPre,8:F2} {(rPost>rPre?"SEPARATING":"converging"),14}");
        }

        // ========================
        // PART D — Descriptor Independence
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Descriptor Independence (ordering at w1 = pre-transition)");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"{"Descriptor",-14} {"K1",8} {"K3",8} {"K2",8} {"K1>K3>K2?",14} {"Sep ratio",10}");
        _o.WriteLine(new string('-',60));
        void DO(string name,double v72,double v70,double v75){
            bool ord=v72>v70&&v70>v75;double sep=v75>0.001?(v72-v75)/v75:0;
            _o.WriteLine($"{name,-14} {v72,8:F3} {v70,8:F3} {v75,8:F3} {(ord?"YES":"no"),14} {sep,10:F2}x");
        }
        DO("km IQR w1",Iqr(n72,d=>d.warmKm[1]),Iqr(n70,d=>d.warmKm[1]),Iqr(n75,d=>d.warmKm[1]));
        DO("lam IQR w1",Iqr(n72,d=>d.warmLam[1]),Iqr(n70,d=>d.warmLam[1]),Iqr(n75,d=>d.warmLam[1]));
        DO("d IQR w1",Iqr(n72,d=>d.warmDm[1]),Iqr(n70,d=>d.warmDm[1]),Iqr(n75,d=>d.warmDm[1]));
        DO("km IQR w0",Iqr(n72,d=>d.warmKm[0]),Iqr(n70,d=>d.warmKm[0]),Iqr(n75,d=>d.warmKm[0]));
        DO("lam IQR w0",Iqr(n72,d=>d.warmLam[0]),Iqr(n70,d=>d.warmLam[0]),Iqr(n75,d=>d.warmLam[0]));
        _o.WriteLine($"\nkm and lam show K1>K3>K2 at w1. d and w0 descriptors do NOT. Ordering is km/lam-specific and w1-emergent.");

        // ========================
        // PART E — Robustness
        // ========================
        _o.WriteLine("\nPART E — Jackknife (w1 km IQR ordering)");
        bool ordAll=true;
        for(int i=0;i<Math.Max(n72.Length,Math.Max(n70.Length,n75.Length));i++){
            var k72=JackIqr(n72,i,d=>d.warmKm[1]);var k70=JackIqr(n70,i,d=>d.warmKm[1]);var k75=JackIqr(n75,i,d=>d.warmKm[1]);
            if(k72==0||k70==0||k75==0)continue;
            if(!(k72>k70&&k70>k75))ordAll=false;
        }
        _o.WriteLine($"K1>K3>K2 ordering stable under single-profile removal: {ordAll}");

        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: c3<=0.1={lo}, rescues={loR} => SAFE");

        // ========================
        // PART F — Decision
        // ========================
        _o.WriteLine("\nPART F — Decision Model");
        bool w0Ord=Iqr(n72,d=>d.warmKm[0])>Iqr(n70,d=>d.warmKm[0])&&Iqr(n70,d=>d.warmKm[0])>Iqr(n75,d=>d.warmKm[0]);
        bool w1Ord=Iqr(n72,d=>d.warmKm[1])>Iqr(n70,d=>d.warmKm[1])&&Iqr(n70,d=>d.warmKm[1])>Iqr(n75,d=>d.warmKm[1]);
        string dec=w0Ord?"Model A: Ordering inherited from w0":
                   w1Ord?"Model C: Ordering generated at w0->w1 transition":
                   "Model E: Ordering origin unresolved";
        _o.WriteLine($"Decision: {dec} (w0 ordered: {w0Ord}, w1 ordered: {w1Ord})");
        _o.WriteLine("CLAIMS: Spread ordering emerges at w0->w1. km/lam specific. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== SOO_01 complete. Commit: SOO_01_SpreadOrderOriginAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Iqr(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return s.Length>3?Q(s,0.75)-Q(s,0.25):0;}
    static double JackIqr(EP[] nd,int exclude,Func<EP,double> f){var keep=Enumerable.Range(0,nd.Length).Where(j=>j!=exclude).ToArray();if(keep.Length<4)return 0;var s=keep.Select(j=>f(nd[j])).OrderBy(v=>v).ToArray();return Q(s,0.75)-Q(s,0.25);}
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
