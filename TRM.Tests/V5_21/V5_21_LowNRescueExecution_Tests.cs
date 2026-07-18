using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_21;

[Trait("Category","V5_21"),Trait("Category","V5_21_LRE"),Trait("Category","LongRunning")]
public class V5_21_LowNRescueExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_21_LowNRescueExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]public void LRE_01_LowNInducibilityTest(){
        _o.WriteLine("═══ LRE_01: Low-N inducibility test (N=60,64,65) ═══");
        _o.WriteLine("Testing whether basin-access interventions can induce N<65 seeds.");
        _o.WriteLine("Interventions: I0=baseline, I1=basin-push, I2=entry-amp, I4=K-preserve+C3");

        var results=new ConcurrentBag<(int n,int s,string i,bool a0,bool iPersist)>();

        foreach(var n in new[]{60,64,65}){
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

                // I0: Baseline M3++
                bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
                var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
                var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
                double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
                var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
                var hT1=Sim(K2,n,S,s+100);var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
                double reb=Dm(DL(Nm(RP(hT2,n),n),n),n)-Dm(DL(Nm(RP(hT1,n),n),n),n);
                bool a0=Of(hT2,n).Average()>THR;
                bool c3=a0;
                if(reb<RebT&&!a0){ // C3 correction
                    var dmat=DL(Nm(RP(hT1,n),n),n);double dm3=Dm(dmat,n);
                    double nudged=dm3+(hi.dm-dm3)*0.2;double f3=Math.Clamp((nudged+1e-9)/(dm3+1e-9),0.5,1.5);
                    for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat[i,j]*=f3;
                    c3=Of(Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat,n),n,S,s+300),n),n),n),n),n,S,s+400),n).Average()>THR;
                }
                results.Add((n,s,"I0:baseline",a0,c3));

                // I1: Stronger basin push — 75% toward Hi centroid
                var dMPush=CD(d4,n);double fracP=Math.Clamp((hi.dm+1e-9)/(cur+1e-9),0.01,100.0);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMPush[i,j]=Math.Max(dMPush[i,j]*fracP*0.75+hi.dm*0.25,d4[i,j]*0.5);
                var Kpush=Cupd(dMPush,n);var hPush=Sim(Kpush,n,S,s+4);
                Kpush=Cupd(DL(Nm(RP(hPush,n),n),n),n);
                var hT1P=Sim(Kpush,n,S,s+100);
                bool i1=Of(Sim(Cupd(DL(Nm(RP(hT1P,n),n),n),n),n,S,s+300),n).Average()>THR;
                results.Add((n,s,"I1:basin-push",a0,i1));

                // I2: Stronger entry-vector — 50% push
                var dVE=CD(d4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dVE[i,j]*=0.3;
                var Kv=Cupd(dVE,n);var hV=Sim(Kv,n,S,s+4);Kv=Cupd(DL(Nm(RP(hV,n),n),n),n);
                var hT1V=Sim(Kv,n,S,s+100);
                bool i2=Of(Sim(Cupd(DL(Nm(RP(hT1V,n),n),n),n),n,S,s+300),n).Average()>THR;
                results.Add((n,s,"I2:strong-comp",a0,i2));

                // I4: K-preserve + C3
                var Kcp=Cupd(DL(Nm(RP(hT1,n),n),n),n);
                var dmat4=DL(Nm(RP(hT1,n),n),n);double dm4=Dm(dmat4,n);
                double nudged4=dm4+(hi.dm-dm4)*0.2;double f4=Math.Clamp((nudged4+1e-9)/(dm4+1e-9),0.5,1.5);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat4[i,j]*=f4;
                var Kc4=Cupd(dmat4,n);for(int i=0;i<n;i++)for(int j=0;j<n;j++)Kc4[i,j]=(Kc4[i,j]+Kcp[i,j])/2;
                var hc4=Sim(Kc4,n,S,s+300);
                bool i4=Of(Sim(Cupd(DL(Nm(RP(hc4,n),n),n),n),n,S,s+400),n).Average()>THR;
                results.Add((n,s,"I4:K-pres+C3",a0,i4));
            }
        }

        var all=results.ToArray();
        _o.WriteLine($"\n─── Low-N inducibility results ───");
        _o.WriteLine(string.Format("{0,4} {1,-14} {2,6} {3,6} {4,6} {5,8}",
            "N","Intervention","Seeds","Strict","Rate","vsA0"));
        foreach(var n in new[]{60,64,65}){
            var a0=all.Where(r=>r.n==n&&r.i=="I0:baseline").ToArray();
            int a0s=a0.Count(r=>r.iPersist);
            foreach(var i in new[]{"I0:baseline","I1:basin-push","I2:strong-comp","I4:K-pres+C3"}){
                var sub=all.Where(r=>r.n==n&&r.i==i).ToArray();
                if(sub.Length==0)continue;
                int st=sub.Count(r=>r.iPersist);
                _o.WriteLine($"{n,4} {i,-14} {sub.Length,6} {st,6} {st*100.0/sub.Length,5:F0}% {(st-a0s)*100.0/sub.Length,7:+.0}%");
            }
        }

        // Gates
        var n60I1=all.Where(r=>r.n==60&&r.i=="I1:basin-push").Count(r=>r.iPersist);
        var n60I2=all.Where(r=>r.n==60&&r.i=="I2:strong-comp").Count(r=>r.iPersist);
        var n64I1=all.Where(r=>r.n==64&&r.i=="I1:basin-push").Count(r=>r.iPersist);
        var n60A0=all.Where(r=>r.n==60&&r.i=="I0:baseline").Count(r=>r.iPersist);
        bool anyBreak=n60I1>n60A0||n60I2>n60A0||n64I1>n60A0;

        _o.WriteLine($"\nGate A (Boundary breaks): {(anyBreak?"REACHED":"NOT REACHED")} — low-N stays rescue-immune");
        _o.WriteLine($"Gate C (Remains immune): {(!anyBreak?"REACHED":"NOT REACHED")} — N<65 boundary holds");
        _o.WriteLine($"Gate F (Entry-vector helps): {(n60I2>n60A0?"REACHED":"NOT REACHED")}");

        string verdict=!anyBreak?"N<65 remains rescue-immune under tested interventions. Lower boundary is robust.":
            "Low-N boundary can be breached by basin-access intervention.";
        _o.WriteLine($"\n─── Verdict: {verdict} ───");
        _o.WriteLine("\n─── Next: LRA or LRS ───");
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
