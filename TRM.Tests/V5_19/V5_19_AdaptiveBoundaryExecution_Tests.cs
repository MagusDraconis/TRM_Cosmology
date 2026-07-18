using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_19;

[Trait("Category","V5_19"),Trait("Category","V5_19_ABE"),Trait("Category","LongRunning")]
public class V5_19_AdaptiveBoundaryExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebThresh=0.01;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    public V5_19_AdaptiveBoundaryExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]public void ABE_01_BoundarySweep(){
        _o.WriteLine("═══ ABE_01: Adaptive boundary sweep N=50→100 ═══");
        int[] Ns={50,55,60,65,70,71,72,73,74,75,76,80,85,90,95,100};
        var results=new ConcurrentBag<(int n,int s,bool a0,bool c3)>();

        foreach(var n in Ns){
            var hi=Hi(n);var lo=Lo(n);int cnt=0;
            for(int s=0;s<99&&cnt<6;s++){
                if(IsHi(n,s))continue;
                var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
                var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
                double d2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
                double orth=Math.Sqrt(Math.Max(0,d2-proj*proj));
                bool isP1=sb.cls=="P1"||sb.cls=="P1b";
                if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))continue;cnt++;

                bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
                var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
                var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
                double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
                var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
                var hT1=Sim(K2,n,S,s+100);var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
                double om1=Of(hT1,n).Average(),om2=Of(hT2,n).Average(),dT1=Dm(DL(Nm(RP(hT1,n),n),n),n),dT2=Dm(DL(Nm(RP(hT2,n),n),n),n);
                double reb=dT2-dT1;bool a0=om2>THR,c3p=a0;
                if(reb<RebThresh&&!a0){
                    var dmat3=DL(Nm(RP(hT1,n),n),n);double dm3=Dm(dmat3,n);
                    double nudged=dm3+(hi.dm-dm3)*0.2;
                    double f3=Math.Clamp((nudged+1e-9)/(dm3+1e-9),0.5,1.5);
                    for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
                    var Kc3=Cupd(dmat3,n);var hc3=Sim(Kc3,n,S,s+300);
                    c3p=Of(Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400),n).Average()>THR;
                }
                results.Add((n,s,a0,c3p));
            }
        }

        var all=results.ToArray();
        _o.WriteLine($"\nBoundary sweep: {all.Length} seeds across {Ns.Length} N values");

        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,6} {5,6} {6,8} {7,-14}",
            "N","Seeds","A0","C3","Resc","Dam","Lift","Regime"));
        foreach(var n in Ns){
            var sub=all.Where(r=>r.n==n).ToArray();
            if(sub.Length==0)continue;
            int a0s=sub.Count(r=>r.a0),c3s=sub.Count(r=>r.c3);
            int resc=sub.Count(r=>!r.a0&&r.c3),dam=sub.Count(r=>r.a0&&!r.c3);
            int lift=c3s-a0s;
            string regime=n<=55?"inaccessible":n<=65?"transition":n<=76&&lift>=1?"adaptive-active":
                n<=76?"adaptive-neutral":"saturated";
            if(n==72)regime="adaptive-peak";
            _o.WriteLine($"{n,4} {sub.Length,6} {a0s*100.0/sub.Length,5:F0}% {c3s*100.0/sub.Length,5:F0}% {resc,6} {dam,6} {lift*100.0/sub.Length,7:+.0}% {regime,-14}");
        }

        // Boundaries
        var lowOnset=Ns.FirstOrDefault(n=>all.Where(r=>r.n==n).ToArray() is var s&&s.Length>0&&s.Count(r=>!r.a0&&r.c3)>=1);
        var highEnd=Ns.LastOrDefault(n=>all.Where(r=>r.n==n).ToArray() is var s&&s.Length>0&&s.Count(r=>!r.a0&&r.c3)>=1);
        _o.WriteLine($"\nLower onset: N={lowOnset} | Upper last rescue: N={highEnd}");
        _o.WriteLine($"Adaptive window: N={lowOnset}–{highEnd}");

        _o.WriteLine("\n─── Next: ABA — Boundary Analysis ───");
    }

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
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
