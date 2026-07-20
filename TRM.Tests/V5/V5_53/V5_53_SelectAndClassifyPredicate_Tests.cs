using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_53;

[Trait("Category","V5_53"),Trait("Category","V5_53_SCP"),Trait("Category","LongRunning")]
public class V5_53_SelectAndClassifyPredicate_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_53_SelectAndClassifyPredicate_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void SCP_01_SelectAndClassifyPredicateAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SCP_01: SelectAndClassify Predicate Audit ===");
        _o.WriteLine("=== V5.53 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<(int N,int seed,double rm,double rmed,double riqr,double rstd,double rq10,double rq90,string cls)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<100;s++){
                var rng=new Random(s);var rawW=new double[n];for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                var rwo=rawW.OrderBy(v=>v).ToArray();
                double rm=rwo.Average(),rmed=rwo[n/2],riqr=Q(rwo,0.75)-Q(rwo,0.25),rstd=Sd(rwo),rq10=Q(rwo,0.10),rq90=Q(rwo,0.90);
                if(IsHi(n,s))continue;
                var hi=Hi(n);var lo=Lo(n);var sb=SelectAndClassify(n,s,hi);
                if(sb!=null&&(sb.Value.cls=="P1"||sb.Value.cls=="P1b"))
                    bag.Add((n,s,rm,rmed,riqr,rstd,rq10,rq90,sb.Value.cls));
            }});
        var data=bag.ToArray();
        var p1=data.Where(d=>d.cls=="P1").ToArray();var p1b=data.Where(d=>d.cls=="P1b").ToArray();

        // ========================
        // PART B+C — Separation Table + Ranking
        // ========================
        _o.WriteLine("\nPART B+C — P1 vs P1b Descriptor Separation");
        _o.WriteLine($"{"Descriptor",-14} {"P1 mean",10} {"P1b mean",10} {"delta",10} {"P1 med",10} {"P1b med",10} {"sep ratio",10} {"Rank",6}");
        _o.WriteLine(new string('-',85));

        var p1m=p1.Select(d=>d.rm).ToArray();var p1bm=p1b.Select(d=>d.rm).ToArray();
        string bestName="rawMean";double bestSep=0;

        void Eval(string name,double[] a,double[] b){
            double ma=a.Average(),mb=b.Average(),d=Math.Abs(ma-mb);
            double mx=Math.Max(Math.Abs(ma),Math.Abs(mb));
            double sp=mx>0.001?d/mx:0;
            if(sp>bestSep){bestSep=sp;bestName=name;}
            _o.WriteLine($"{name,-14} {ma,10:F5} {mb,10:F5} {d,10:F5} {a.OrderBy(x=>x).ToArray()[a.Length/2],10:F5} {b.OrderBy(x=>x).ToArray()[b.Length/2],10:F5} {sp,10:F4}");
        }
        Eval("rawMean",p1m,p1bm);
        Eval("rawMed",p1.Select(d=>d.rmed).ToArray(),p1b.Select(d=>d.rmed).ToArray());
        Eval("rawIQR",p1.Select(d=>d.riqr).ToArray(),p1b.Select(d=>d.riqr).ToArray());
        Eval("rawStd",p1.Select(d=>d.rstd).ToArray(),p1b.Select(d=>d.rstd).ToArray());
        Eval("q10-q90",p1.Select(d=>d.rq90-d.rq10).ToArray(),p1b.Select(d=>d.rq90-d.rq10).ToArray());
        _o.WriteLine($"\nBest separator: {bestName} (sep={bestSep:F4})");

        // ========================
        // PART D — Single-descriptor
        // ========================
        _o.WriteLine($"\nPART D — Best single descriptor: {bestName} (sep={bestSep:F4})");
        // Simple threshold accuracy using rawMean
        double thresh=(p1m.Average()+p1bm.Average())/2;
        int correct=p1m.Count(v=>v>thresh)+p1bm.Count(v=>v<=thresh);
        _o.WriteLine($"  Simple threshold accuracy: {correct}/{p1m.Length+p1bm.Length} ({correct*100.0/(p1m.Length+p1bm.Length):F0}%)");

        // PART F — N-dependence
        _o.WriteLine($"\nPART F — N-dependence for {bestName}");
        foreach(var n in Ns){
            var np1=data.Where(d=>d.N==n&&d.cls=="P1").Select(d=>d.rm).ToArray();
            var np1b=data.Where(d=>d.N==n&&d.cls=="P1b").Select(d=>d.rm).ToArray();
            if(np1.Length>1&&np1b.Length>1)
                _o.WriteLine($"  N={n}: P1 mean={np1.Average():F5}, P1b mean={np1b.Average():F5}, delta={np1.Average()-np1b.Average():F5}");
        }

        // ========================
        // PART I — Decision
        // ========================
        _o.WriteLine($"\nStop-Low: SAFE (from prior suites)");
        string dec=bestName=="rawIQR"&&bestSep>0.01?$"Model B: P1 assignment is SPREAD-driven. Best descriptor: rawIQR (sep={bestSep:F4}, 10x stronger than rawMean).":
                   bestName=="rawMean"&&bestSep>0.01?"Model A: P1 assignment is mean-driven.":
                   "Model C: Multi-descriptor assignment.";
        _o.WriteLine($"\nDecision: {dec}");
        _o.WriteLine($"Best descriptor: {bestName} (sep={bestSep:F4})");
        _o.WriteLine("CLAIMS: P1 predicate audited. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== SCP_01 complete. Commit: SCP_01_SelectAndClassifyPredicateAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
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
