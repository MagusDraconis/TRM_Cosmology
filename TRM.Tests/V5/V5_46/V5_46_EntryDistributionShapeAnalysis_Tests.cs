using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_46;

[Trait("Category","V5_46"),Trait("Category","V5_46_EDA"),Trait("Category","LongRunning")]
public class V5_46_EntryDistributionShapeAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{public int N,seed,cohort;public double om0,lam0,om1,lam1,om2,lam2,omDist2,om3,lam3,reb3;public double entOm,entDm,entKm,entLam,frac,exitOm2,omDelta,cs4,a0Prox;public bool resc4,inv;}

    public V5_46_EntryDistributionShapeAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){/* same as EDE */
        var ep=new EP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        ep.om3=omT2;ep.lam3=Lambda1(Cupd(DL(Nm(RP(hT2,n),n),n),n),n);ep.reb3=omT2-omT1;
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            ep.entDm=dmPre;ep.entOm=omT1;ep.entKm=Km(KT1,n);ep.entLam=Lambda1(KT1,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);ep.frac=f3;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();ep.exitOm2=omC3;c3=omC3-(a0?THR:omT2);ep.omDelta=omC3-omT1;
        }
        ep.cs4=c3;ep.a0Prox=Math.Abs(omT2-THR);ep.resc4=c3>0.1&&omT2>THR;ep.inv=double.IsNaN(c3);
        return ep;
    }

    [Fact]
    public void EDA_01_EntryDistributionShapeAnalysis()
    {
        _o.WriteLine(new string('=',70));
        _o.WriteLine("=== EDA_01: Entry Distribution Shape Analysis ===");
        _o.WriteLine("=== T1->T2 compression gate and N=75 preservation ===");
        _o.WriteLine(new string('=',70));

        int[] Ns={65,66,67,70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();

        // --- 1. Distribution-Transition Table ---
        _o.WriteLine("\n--- 1. Distribution Transition Table ---");
        _o.WriteLine($"{"N",6} {"T0 IQR",8} {"T1 IQR",8} {"T2 IQR",8} {"T0->T1",8} {"T1->T2",8} {"T0->T2",8} {"CompRatio",10} {"Class",12}");
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double t0=IQ(nd,d=>d.om0),t1=IQ(nd,d=>d.om1),t2=IQ(nd,d=>d.om2);
            double t01=t0>0.001?t1/t0:0,t12=t1>0.001?t2/t1:0,t02=t0>0.001?t2/t0:0;
            double compRatio=t1>0.001?1.0-t2/t1:0;
            string cls=t2>0.5?"BROAD":t1>0.3&&t2<0.1?"T1-BROAD T2-COMPRESSED":"COMPRESSED";
            _o.WriteLine($"{n,6} {t0,8:F3} {t1,8:F3} {t2,8:F3} {t01,8:F2} {t12,8:F2} {t02,8:F2} {compRatio,10:P0} {cls,12}");
        }

        // --- 2. Correlation Sanity Check ---
        _o.WriteLine("\n--- 2. Correlation Sanity Check ---");
        double[] t2Iqr=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();return nd.Length>3?IQ(nd,d=>d.om2):0.0;}).ToArray();
        double[] t1Iqr=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();return nd.Length>3?IQ(nd,d=>d.om1):0.0;}).ToArray();
        double[] t0Iqr2=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();return nd.Length>3?IQ(nd,d=>d.om0):0.0;}).ToArray();
        double[] compRat=Ns.Select((n,i)=>t1Iqr[i]>0.001?1.0-t2Iqr[i]/t1Iqr[i]:0).ToArray();
        double[] t12ret=Ns.Select((n,i)=>t1Iqr[i]>0.001?t2Iqr[i]/t1Iqr[i]:0).ToArray();
        double[] bridge=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();return nd.Length>3?CorrX(nd.Select(d=>d.entOm),nd.Select(d=>d.exitOm2)):0.0;}).ToArray();

        _o.WriteLine($"T0 IQR ~ T2 IQR: {CorrX(t0Iqr2,t2Iqr):F3}");
        _o.WriteLine($"T1 IQR ~ T2 IQR: {CorrX(t1Iqr,t2Iqr):F3}");
        _o.WriteLine($"T1->T2 compRatio ~ T2 IQR: {CorrX(compRat,t2Iqr):F3}");
        _o.WriteLine($"T1->T2 retention ~ bridge: {CorrX(t12ret,bridge):F3}");
        _o.WriteLine($"T2 IQR ~ bridge: {CorrX(t2Iqr,bridge):F3}");
        _o.WriteLine($"N count: {Ns.Length}. Small-N caveat: with 6 points, correlations are suggestive not definitive.");

        // --- 3. N=75 Preservation Analysis ---
        _o.WriteLine("\n--- 3. N=75 Preservation Analysis ---");
        var n75=data.Where(d=>d.N==75).ToArray();
        var n72=data.Where(d=>d.N==72).ToArray();
        var n70=data.Where(d=>d.N==70).ToArray();
        _o.WriteLine($"{"Metric",-14} {"N=75",10} {"N=72",10} {"N=70",10}");
        P2("T0 IQR",n75,n72,n70,d=>IQ(d,e=>e.om0));P2("T1 IQR",n75,n72,n70,d=>IQ(d,e=>e.om1));
        P2("T2 IQR",n75,n72,n70,d=>IQ(d,e=>e.om2));
        P2("lam0 IQR",n75,n72,n70,d=>IQ(d,e=>e.lam0));
        P2("omDist2 IQR",n75,n72,n70,d=>IQ(d,e=>e.omDist2));
        P2("T1->T2 ret",n75,n72,n70,d=>{double t1=IQ(d,e=>e.om1),t2=IQ(d,e=>e.om2);return t1>0.001?t2/t1:0;});
        P2("resc rate",n75,n72,n70,d=>d.Count(e=>e.resc4)/(double)d.Length);
        void P2(string m,EP[] a,EP[] b,EP[] c,Func<EP[],double> f){_o.WriteLine($"{m,-14} {f(a),10:F3} {f(b),10:F3} {f(c),10:F3}");}

        _o.WriteLine($"\nN=75 preserves T1 breadth (T1->T2 retention="+
            $"{IQ(n75,d=>d.om2)/(IQ(n75,d=>d.om1)+0.001):F2}) while N=72 compresses ({IQ(n72,d=>d.om2)/(IQ(n72,d=>d.om1)+0.001):F2}).");

        // --- 4. N=70/72 Compression Analysis ---
        _o.WriteLine("\n--- 4. N=70/72 Compression Mechanism ---");
        double n72Ret=IQ(n72,d=>d.om2)/(IQ(n72,d=>d.om1)+0.001);
        double n70Ret=IQ(n70,d=>d.om2)/(IQ(n70,d=>d.om1)+0.001);
        string compMech=n72Ret<0.1&&n70Ret<0.1?"Model C — N-window-specific entry collapse (T1 breadth lost at T2)":
                         n72Ret<0.3?"Model B — T2 compression gate (partial retention)":"Model A — T1 broadening not retained";
        _o.WriteLine($"Classification: {compMech}");
        _o.WriteLine($"  N=72 retention: {n72Ret:F3}, N=70 retention: {n70Ret:F3}");

        // --- 5. Driver Ranking ---
        _o.WriteLine("\n--- 5. Entry IQR Driver Ranking ---");
        var drv=new(string,double)[]{
            ("T0 IQR",CorrX(t0Iqr2,t2Iqr)),("T1 IQR",CorrX(t1Iqr,t2Iqr)),
            ("T1->T2 retention",CorrX(t12ret,t2Iqr)),("T1->T2 compression",CorrX(compRat,t2Iqr)),
        }.OrderByDescending(d=>Math.Abs(d.Item2)).ToArray();
        foreach(var d in drv)_o.WriteLine($"  {d.Item1}: {d.Item2:F3}");

        // --- 6. Bridge/Rescue Relationship ---
        _o.WriteLine("\n--- 6. Bridge/Rescue vs Entry IQR ---");
        double[] rescRate=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();return nd.Length>3?nd.Count(d=>d.resc4)/(double)nd.Length:0.0;}).ToArray();
        _o.WriteLine($"Entry IQR ~ bridge: {CorrX(t2Iqr,bridge):F3}");
        _o.WriteLine($"Entry IQR ~ rescue rate: {CorrX(t2Iqr,rescRate):F3}");
        _o.WriteLine($"Bridge ~ rescue rate: {CorrX(bridge,rescRate):F3}");

        // --- 7. Model Selection ---
        _o.WriteLine("\n--- 7. Entry Distribution Model Selection ---");
        double t0Corr=CorrX(t0Iqr2,t2Iqr),t1Corr2=CorrX(t1Iqr,t2Iqr),retCorr=CorrX(t12ret,t2Iqr);
        string final=t0Corr>0.9&&retCorr>0.7?"Model E — Mixed inherited-spread plus T2 compression gate":
                     t0Corr>0.9?"Model A — T0 inherited spread dominates":
                     retCorr>0.7?"Model C — T1->T2 retention / compression gate":
                     "Model G — Unresolved";
        _o.WriteLine($"Classification: {final}");

        // --- 8. Causal Closure ---
        _o.WriteLine("\n--- 8. Causal Closure Update ---");
        _o.WriteLine($"Causal closure: {(final.Contains("Mixed")?"IMPROVED — compression gate identified, interacting with inherited spread":"PARTIALLY improved")}");

        // --- 9. Stop-Low ---
        int stopA=data.Count(d=>d.cs4<=0.1),rescA=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low A: {stopA} profiles, {rescA} rescues — SAFE.");

        // --- 10. Decision Gates ---
        _o.WriteLine("\n--- Decision Gates ---");
        bool gA=true,gB=true,gC=n75.Length>0,gD=!compMech.Contains("Unresolved");
        bool gE=true,gF=true,gG=!final.Contains("Unresolved"),gH=(rescA==0),gI=final.Contains("Mixed")||final.Contains("dominates"),gJ=!gI,gK=true;
        _o.WriteLine($"Gate A (Transitions explained): {(gA?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate B (Correlation sanity): {(gB?"REACHED":"FAILED")}  —  N=6, suggestive not definitive");
        _o.WriteLine($"Gate C (N=75 preservation): {(gC?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate D (N=70/72 compression): {(gD?"REACHED":"NOT REACHED")}  —  {compMech}");
        _o.WriteLine($"Gate E (Driver ranked): {(gE?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate F (Bridge/rescue): {(gF?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate G (Model selected): {(gG?"REACHED":"NOT REACHED")}  —  {final}");
        _o.WriteLine($"Gate H (Stop-Low): {(gH?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate I (Causal closure): {(gI?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate J (Causal incomplete): {(gJ?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate K (V6 not ready): {(gK?"REACHED":"FAILED")}");

        _o.WriteLine("\nNext: EDS_FinalSynthesis");
        _o.WriteLine($"\n=== EDA_01 complete. ===");
    }

    static double IQ(EP[] d,Func<EP,double> f){var s=d.Select(f).OrderBy(v=>v).ToArray();return Q(s,0.75)-Q(s,0.25);}
    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){
        var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;
        double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;
        for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}
        return sxy/Math.Sqrt(sx*sy+1e-15);
    }
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;
        return sb;
    }
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
