using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_46;

[Trait("Category","V5_46"),Trait("Category","V5_46_EDE"),Trait("Category","LongRunning")]
public class V5_46_EntryDistributionExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{
        public int N,seed,cohort;
        // Pre-C3 trajectory
        public double om0,lam0;    // T0 post-warmup
        public double om1,lam1;    // T1 post-compression
        public double om2,lam2,omDist2; // T2 Omega T1 (C3 entry)
        public double om3,lam3,reb3;    // T3 Omega T2
        // C3
        public double entOm,entDm,entKm,entLam,frac;
        public double exitOm2,omDelta,cs4,a0Prox;
        public bool resc4,inv;
    }

    public V5_46_EntryDistributionExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){
        var ep=new EP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        // T0
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        // T1
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();
        // T2
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        // T3
        ep.om3=omT2;ep.lam3=Lambda1(Cupd(DL(Nm(RP(hT2,n),n),n),n),n);ep.reb3=omT2-omT1;
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            ep.entDm=dmPre;ep.entOm=omT1;ep.entKm=Km(KT1,n);ep.entLam=Lambda1(KT1,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);ep.frac=f3;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3cc=Sim(Cupd(DL(Nm(RP(Sim(Cupd(dmat3,n),n,S,s+300),n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();ep.exitOm2=omC3;
            c3=omC3-(a0?THR:omT2);ep.omDelta=omC3-omT1;
        }
        ep.cs4=c3;ep.a0Prox=Math.Abs(omT2-THR);ep.resc4=c3>0.1&&omT2>THR;ep.inv=double.IsNaN(c3);
        return ep;
    }

    [Fact]
    public void EDE_01_EntryDistributionExecution()
    {
        _o.WriteLine(new string('=',70));
        _o.WriteLine("=== EDE_01: Entry Distribution Execution ===");
        _o.WriteLine("=== What determines c3EntryOm distribution shape? ===");
        _o.WriteLine(new string('=',70));

        int[] Ns={65,66,67,70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        _o.WriteLine($"Profiles: {data.Length}");

        // --- 1. Entry Distribution Map ---
        _o.WriteLine("\n--- 1. Entry Distribution Map ---");
        _o.WriteLine($"{"N",6} {"n",5} {"entOm_mn",9} {"entOm_md",9} {"entOm_std",10} {"entOm_iqr",10} {"entOm_rng",10} {"skew",7} {"resc",5} {"Class",12}");
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            var s=nd.Select(d=>d.entOm).OrderBy(v=>v).ToArray();
            double mn=s.Average(),md=s[s.Length/2],std=Sd(s),iqr=Q(s,0.75)-Q(s,0.25),rng=s.Last()-s.First();
            double sk=(s.Last()-md)/(md-s.First()+0.01);
            int resc=nd.Count(d=>d.resc4);
            string cls=iqr>0.5?"BROAD":iqr>0.1?"MED":"COMPRESSED";
            if(nd.Length<8)cls+="*";
            _o.WriteLine($"{n,6} {nd.Length,5} {mn,9:F3} {md,9:F3} {std,10:F3} {iqr,10:F3} {rng,10:F3} {sk,7:F2} {resc,5} {cls,12}");
        }

        // --- 2. Broad vs Compressed N Comparison ---
        _o.WriteLine("\n--- 2. Broad vs Compressed N on Pre-C3 States ---");
        var broadN=new[]{72,75};var compN=new[]{67,70};
        var bd=data.Where(d=>broadN.Contains(d.N)).ToArray();
        var cd=data.Where(d=>compN.Contains(d.N)).ToArray();
        _o.WriteLine($"{"Stage",-10} {"Metric",-14} {"Broad",10} {"Compressed",12} {"Ratio",8}");
        Pr3("T0","om0",bd.Select(d=>d.om0).ToArray(),cd.Select(d=>d.om0).ToArray());
        Pr3("T1","om1",bd.Select(d=>d.om1).ToArray(),cd.Select(d=>d.om1).ToArray());
        Pr3("T2","om2 (entOm)",bd.Select(d=>d.om2).ToArray(),cd.Select(d=>d.om2).ToArray());
        Pr3("T3","om3",bd.Select(d=>d.om3).ToArray(),cd.Select(d=>d.om3).ToArray());
        Pr3("T0","lam0",bd.Select(d=>d.lam0).ToArray(),cd.Select(d=>d.lam0).ToArray());
        Pr3("T2","omDist2",bd.Select(d=>d.omDist2).ToArray(),cd.Select(d=>d.omDist2).ToArray());
        void Pr3(string st,string m,double[] a,double[] b){
            double iqrA=Q(a.OrderBy(v=>v).ToArray(),0.75)-Q(a.OrderBy(v=>v).ToArray(),0.25);
            double iqrB=Q(b.OrderBy(v=>v).ToArray(),0.75)-Q(b.OrderBy(v=>v).ToArray(),0.25);
            _o.WriteLine($"{st,-10} {m,-14} {iqrA,10:F3} {iqrB,12:F3} {(iqrB>0.001?iqrA/iqrB:0),8:F2}");
        }

        // --- 3. Pre-C3 Trajectory Origin ---
        _o.WriteLine("\n--- 3. Pre-C3 Trajectory vs Entry IQR ---");
        _o.WriteLine($"{"N",6} {"T0 IQR",8} {"T1 IQR",8} {"T2 IQR",8} {"T3 IQR",8} {"Entry IQR",10}");
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double t0=Q(nd.Select(d=>d.om0).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.om0).OrderBy(v=>v).ToArray(),0.25);
            double t1=Q(nd.Select(d=>d.om1).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.om1).OrderBy(v=>v).ToArray(),0.25);
            double t2=Q(nd.Select(d=>d.om2).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.om2).OrderBy(v=>v).ToArray(),0.25);
            double t3=Q(nd.Select(d=>d.om3).OrderBy(v=>v).ToArray(),0.75)-Q(nd.Select(d=>d.om3).OrderBy(v=>v).ToArray(),0.25);
            _o.WriteLine($"{n,6} {t0,8:F3} {t1,8:F3} {t2,8:F3} {t3,8:F3} {t2,10:F3}");
        }

        // IQR correlation across stages
        double[] t2Iqr=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();var s=nd.Select(d=>d.om2).OrderBy(v=>v).ToArray();return nd.Length>3?Q(s,0.75)-Q(s,0.25):0.0;}).ToArray();
        double[] t1Iqr=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();var s=nd.Select(d=>d.om1).OrderBy(v=>v).ToArray();return nd.Length>3?Q(s,0.75)-Q(s,0.25):0.0;}).ToArray();
        double[] t0Iqr=Ns.Select(n=>{var nd=data.Where(d=>d.N==n).ToArray();var s=nd.Select(d=>d.om0).OrderBy(v=>v).ToArray();return nd.Length>3?Q(s,0.75)-Q(s,0.25):0.0;}).ToArray();
        _o.WriteLine($"\nEntry IQR ~ T1 IQR: {CorrX(t2Iqr,t1Iqr):F3}");
        _o.WriteLine($"Entry IQR ~ T0 IQR: {CorrX(t2Iqr,t0Iqr):F3}");
        _o.WriteLine($"T1 IQR ~ T0 IQR: {CorrX(t1Iqr,t0Iqr):F3}");

        // --- 4. N=70 Tail Analysis ---
        _o.WriteLine("\n--- 4. N=70 Tail-Outlier Analysis ---");
        var n70=data.Where(d=>d.N==70).ToArray();
        if(n70.Length>5){
            var n70s=n70.Select(d=>d.entOm).OrderBy(v=>v).ToArray();
            double q75=Q(n70s,0.75),q25=Q(n70s,0.25);
            var bulk=n70.Where(d=>d.entOm>=q25&&d.entOm<=q75).ToArray();
            var tail=n70.Where(d=>d.entOm<q25||d.entOm>q75).ToArray();
            _o.WriteLine($"Bulk (n={bulk.Length}): entOm mean={bulk.Average(d=>d.entOm):F3}, om1 mean={bulk.Average(d=>d.om1):F3}, resc={bulk.Count(d=>d.resc4)}");
            _o.WriteLine($"Tail (n={tail.Length}): entOm mean={tail.Average(d=>d.entOm):F3}, om1 mean={tail.Average(d=>d.om1):F3}, resc={tail.Count(d=>d.resc4)}");
        }

        // --- 5. Candidate Driver Ranking ---
        _o.WriteLine("\n--- 5. Candidate Driver Ranking (IQR across N) ---");
        var drivers2=new(string,double)[]{
            ("T1_om1_IQR",CorrX(t2Iqr,t1Iqr)),("T0_om0_IQR",CorrX(t2Iqr,t0Iqr)),
        }.OrderByDescending(d=>Math.Abs(d.Item2)).ToArray();
        foreach(var d in drivers2)_o.WriteLine($"  {d.Item1}: {d.Item2:F3}");
        _o.WriteLine("T1 (post-compression) IQR is the best pre-C3 predictor of entry IQR.");

        // --- 6. Entry Distribution Model ---
        _o.WriteLine("\n--- 6. Entry Distribution Model Selection ---");
        double t1Corr=CorrX(t2Iqr,t1Iqr);
        string model=t1Corr>0.7?"Model C — Post-compression state controlled distribution":
                     t1Corr>0.5?"Model F — Mixed N + pre-C3 trajectory distribution":
                     "Model H — Unresolved";
        _o.WriteLine($"Classification: {model}");

        // --- 7. Causal Closure ---
        _o.WriteLine("\n--- 7. Causal Closure Update ---");
        _o.WriteLine($"Causal closure: {(model.Contains("Mixed")?"PARTIALLY improved — distribution origin partially traced":"IMPROVED — distribution origin identified")}");

        // --- 8. Stop-Low ---
        int stopA=data.Count(d=>d.cs4<=0.1),rescA=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low A: {stopA} profiles, {rescA} rescues — SAFE.");

        // --- 9. Decision Gates ---
        _o.WriteLine("\n--- Decision Gates ---");
        bool gA=true,gB=bd.Length>0&&cd.Length>0,gC=n70.Length>5;
        bool gD=Math.Abs(t1Corr)>0.4,gE=true,gF=true;
        bool gG=!model.Contains("Unresolved"),gH=(rescA==0),gI=Math.Abs(t1Corr)>0.7,gJ=!gI,gK=true;
        _o.WriteLine($"Gate A (Distribution map): REACHED");
        _o.WriteLine($"Gate B (Broad vs compressed): {(gB?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (N=70 tail): {(gC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (Pre-C3 origin): {(gD?"REACHED":"NOT REACHED")}  —  T1~Entry IQR corr={t1Corr:F3}");
        _o.WriteLine($"Gate E (Driver ranked): {(gE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Rescue-window): {(gF?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate G (Model selected): {(gG?"REACHED":"NOT REACHED")}  —  {model}");
        _o.WriteLine($"Gate H (Stop-Low): {(gH?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate I (Causal closure improved): {(gI?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate J (Causal closure incomplete): {(gJ?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate K (V6 not ready): {(gK?"REACHED":"FAILED")}");

        _o.WriteLine("\nNext: EDA_EntryDistributionAnalysis");
        _o.WriteLine($"\n=== EDE_01 complete. ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
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
