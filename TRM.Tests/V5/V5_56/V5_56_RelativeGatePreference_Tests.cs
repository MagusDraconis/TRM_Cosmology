using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_56;

[Trait("Category","V5_56"),Trait("Category","V5_56_RGP"),Trait("Category","LongRunning")]
public class V5_56_RelativeGatePreference_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_56_RelativeGatePreference_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void RGP_01_RelativeGatePreferenceAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RGP_01: Relative Gate Preference Audit ===");
        _o.WriteLine("=== V5.56. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Why does SAC prefer lower-ranked profiles? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        var seedIQRd=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();
            allProf.Add((n,s,Q(wo,0.75)-Q(wo,0.25),wo.Average(),wo[n/2],Sd(wo)));
            seedIQRd.AddOrUpdate(s,Q(wo,0.75)-Q(wo,0.25),(_,v)=>v+Q(wo,0.75)-Q(wo,0.25));
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        var pipeBag=new ConcurrentBag<(int N,int seed,double riqr,double resid,double rank,double rmean,double rmed,double rstd,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);
        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                if(!IsHi(n,s))return;var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                var p=allProf.FirstOrDefault(x=>x.N==n&&x.seed==s);
                pipeBag.Add((n,s,p.riqr,p.riqr-siQ.GetValueOrDefault(s,0),seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1,p.rmean,p.rmed,p.rstd,cls));
            });});
        var pd=pipeBag.ToArray();
        var ret=pd.Where(d=>d.cls=="P1"||d.cls=="P1b").ToArray();
        var rej=pd.Where(d=>d.cls=="reject").ToArray();
        _o.WriteLine($"SAC outcomes: P1={ret.Count(d=>d.cls=="P1")}, P1b={ret.Count(d=>d.cls=="P1b")}, rejected={rej.Length}");

        // ============================================================
        // PART B — Rank Preference Shape
        // ============================================================
        _o.WriteLine($"\n=== PART B: Rank Preference Shape ===");
        _o.WriteLine($"{"Rank%",-10} {"Total",6} {"P1",4} {"P1b",4} {"Rej",5} {"P1%",7} {"Ret%",7}");
        _o.WriteLine(new string('-',50));
        for(int q=0;q<4;q++){
            double lo=q/4.0,hi=(q+1)/4.0+(q==3?0.01:0);
            var qd=pd.Where(d=>d.rank>=lo&&d.rank<hi).ToArray();
            int qp1=qd.Count(d=>d.cls=="P1"),qp1b=qd.Count(d=>d.cls=="P1b"),qr=qd.Count(d=>d.cls=="reject");
            _o.WriteLine($"{$"{lo*100:F0}-{hi*100:F0}%",-10} {qd.Length,6} {qp1,4} {qp1b,4} {qr,5} {(qp1+qp1b>0?qp1*100.0/(qp1+qp1b):0),7:F0}% {(qp1+qp1b)*100.0/qd.Length,7:F0}%");
        }

        // Monotonicity check
        var rates=new double[4];
        for(int q=0;q<4;q++){double lo=q/4.0,hi=(q+1)/4.0+(q==3?0.01:0);var qd=pd.Where(d=>d.rank>=lo&&d.rank<hi).ToArray();rates[q]=qd.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/qd.Length;}
        bool monotonic=true;for(int q=1;q<4;q++)if(rates[q]>rates[q-1])monotonic=false;
        _o.WriteLine($"Retention monotonic (decreasing with rank): {(monotonic?"YES":"NO")}");

        // ============================================================
        // PART C — Boundary Audit
        // ============================================================
        _o.WriteLine($"\n=== PART C: Boundary Audit ===");
        var loQ=pd.Where(d=>d.rank<0.25).ToArray();
        var others=pd.Where(d=>d.rank>=0.25).ToArray();
        _o.WriteLine($"Lowest quartile (0-25%): retained={loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")}/{loQ.Length} ({loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/loQ.Length:F0}%), P1%={loQ.Count(d=>d.cls=="P1")*100.0/Math.Max(1,loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")):F0}%");
        _o.WriteLine($"Others (25-100%): retained={others.Count(d=>d.cls=="P1"||d.cls=="P1b")}/{others.Length} ({others.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/others.Length:F0}%), P1%={others.Count(d=>d.cls=="P1")*100.0/Math.Max(1,others.Count(d=>d.cls=="P1"||d.cls=="P1b")):F0}%");
        double rr=loQ.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/Math.Max(1,loQ.Length)/(others.Count(d=>d.cls=="P1"||d.cls=="P1b")*100.0/Math.Max(1,others.Length)+0.01);
        _o.WriteLine($"Retention ratio (low/other): {rr:F1}x");

        // ============================================================
        // PART D — Local Competition
        // ============================================================
        _o.WriteLine($"\n=== PART D: Local Competition Audit ===");
        int loRet=0,midRet=0,hiRet=0,loTot=0,midTot=0,hiTot=0;
        foreach(var g in pd.GroupBy(d=>d.seed)){
            var ordered=g.OrderBy(d=>d.rank).ToArray();if(ordered.Length<3)continue;
            var lowest=ordered[0];var middle=ordered[1];var highest=ordered[2];
            loTot++;midTot++;hiTot++;
            if(lowest.cls!="reject")loRet++;if(middle.cls!="reject")midRet++;if(highest.cls!="reject")hiRet++;
        }
        _o.WriteLine($"Lowest-rank retained: {loRet}/{loTot} ({loRet*100.0/loTot:F0}%)");
        _o.WriteLine($"Middle-rank retained: {midRet}/{midTot} ({midRet*100.0/midTot:F0}%)");
        _o.WriteLine($"Highest-rank retained: {hiRet}/{hiTot} ({hiRet*100.0/hiTot:F0}%)");

        // ============================================================
        // PART E — Profile Contrast (lowest-rank retained vs rejected)
        // ============================================================
        _o.WriteLine($"\n=== PART E: Profile Contrast Audit ===");
        var loRankRet=pd.Where(d=>d.rank<0.25&&(d.cls=="P1"||d.cls=="P1b")).ToArray();
        var loRankRej=pd.Where(d=>d.rank<0.25&&d.cls=="reject").ToArray();
        _o.WriteLine($"low-rank retained={loRankRet.Length}, low-rank rejected={loRankRej.Length}");
        _o.WriteLine($"{"Descriptor",-14} {"Retained",10} {"Rejected",10} {"Delta",10}");
        _o.WriteLine(new string('-',45));
        void C(string n,Func<(int,int,double,double,double,double,double,double,string),double> f){
            double r=loRankRet.DefaultIfEmpty().Average(f),j=loRankRej.DefaultIfEmpty().Average(f);
            _o.WriteLine($"{n,-14} {r,10:F5} {j,10:F5} {r-j,10:F5}");
        }
        C("rawIQR",d=>d.Item3);C("residual",d=>d.Item4);C("rawMean",d=>d.Item5);
        C("rawMedian",d=>d.Item6);C("rawStd",d=>d.Item7);

        // ============================================================
        // PART F — Rank Sufficiency
        // ============================================================
        _o.WriteLine($"\n=== PART F: Rank Sufficiency Audit ===");
        foreach(var lo in new[]{0.0,0.25,0.5}){
            double hi=lo+0.25+(lo>0.4?0.01:0);
            var bin=pd.Where(d=>d.rank>=lo&&d.rank<hi).ToArray();
            var bp1=bin.Where(d=>d.cls=="P1").ToArray();var bp1b=bin.Where(d=>d.cls=="P1b").ToArray();
            double rd=Math.Abs(bp1.DefaultIfEmpty().Average(d=>d.Item4)-bp1b.DefaultIfEmpty().Average(d=>d.Item4));
            _o.WriteLine($"Rank [{lo:F2},{hi:F2}): n={bin.Length}, P1={bp1.Length}, P1b={bp1b.Length}, resid delta={rd:F5} {(rd>0.0005?"residual MATTERS":"residual negligible")}");
        }

        // ============================================================
        // PART G — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART G: Robustness ===");
        var rng2=new Random(42);int loStable=0;
        for(int sp=0;sp<50;sp++){
            var shuf=pd.OrderBy(_=>rng2.NextDouble()).ToArray();int h=shuf.Length/2;
            double s1r=shuf.Take(h).Where(d=>d.rank<0.25).Count(d=>d.cls!="reject")*100.0/Math.Max(1,shuf.Take(h).Count(d=>d.rank<0.25));
            double s2r=shuf.Skip(h).Where(d=>d.rank<0.25).Count(d=>d.cls!="reject")*100.0/Math.Max(1,shuf.Skip(h).Count(d=>d.rank<0.25));
            if(Math.Abs(s1r-s2r)<20)loStable++;
        }
        _o.WriteLine($"Low-rank retention stable (50 splits): {loStable}/50");

        // ============================================================
        // PART H — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART H: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(loRet>midRet*2&&monotonic)decision="Model A: SAC selects lowest-rank profiles. Preference is monotonic.";
        else if(loRet>midRet)decision="Model B: SAC selects low-rank profiles plus residual adjustment.";
        else if(rr<1.5)decision="Model D: Rank is proxy for another descriptor.";
        else decision="Model E: Preference unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: lo-ret={loRet*100.0/loTot:F0}%, mid={midRet*100.0/midTot:F0}%, hi={hiRet*100.0/hiTot:F0}%, monotonic={monotonic}, low/other ratio={rr:F1}x");
        _o.WriteLine("CLAIMS: Gate preference audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RGP_01 complete. Commit: RGP_01_RelativeGatePreferenceAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
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
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
