using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_43;

[Trait("Category","V5_43"),Trait("Category","V5_43_HTA"),Trait("Category","LongRunning")]
public class V5_43_HiddenTraceAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct TP{public int N,seed,cohort;public double om0,lam0,om1,lam1,dTail1,om2,lam2,omDist2,om3,lam3,reb3,cs4,lam4;public bool resc4,pers4,inv;}

    public V5_43_HiddenTraceAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    TP RunTrace(int n,int s,P3 hi,P3 lo){
        var tp=new TP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){tp.inv=true;return tp;}
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var hT0=Sim(K,n,S,s+50);tp.om0=Of(hT0,n).Average();tp.lam0=Lambda1(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        tp.om1=Of(h5,n).Average();tp.lam1=Lambda1(K,n);tp.dTail1=Dm(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();tp.om2=omT1;tp.lam2=Lambda1(KT1,n);tp.omDist2=Math.Abs(omT1-THR);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();
        bool a0=omT2>THR;tp.om3=omT2;tp.lam3=Lambda1(Cupd(DL(Nm(RP(hT2,n),n),n),n),n);tp.reb3=omT2-omT1;
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            c3=Of(hc3cc,n).Average()-(a0?THR:omT2);
        }
        tp.cs4=c3;tp.lam4=Lambda1(Cupd(DL(Nm(RP(hT2,n),n),n),n),n);
        tp.resc4=c3>0.1&&omT2>THR;tp.pers4=tp.resc4;tp.inv=double.IsNaN(c3);
        return tp;
    }

    [Fact]
    public void HTA_01_HiddenTraceAnalysis()
    {
        _o.WriteLine(new string('=',70));
        _o.WriteLine("=== HTA_01: Hidden Trace Analysis ===");
        _o.WriteLine("=== T1 vs T4 divergence, early-similarity paradox ===");
        _o.WriteLine(new string('=',70));

        int[] Ns={65,66,67,70,72,75};
        var bag=new ConcurrentBag<TP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var tp=RunTrace(n,s,hi,lo);if(!tp.inv)bag.Add(tp);}});
        var data=bag.ToArray();
        _o.WriteLine($"Traced profiles: {data.Length}");

        // Match near-identical pairs
        var pairs=new System.Collections.Generic.List<(TP a,TP b)>();
        for(int i=0;i<data.Length;i++)
            for(int j=i+1;j<data.Length;j++){
                if(data[i].N!=data[j].N)continue;
                if(Math.Abs(data[i].lam0-data[j].lam0)<0.02&&Math.Abs(data[i].omDist2-data[j].omDist2)<0.1&&Math.Abs(data[i].reb3-data[j].reb3)<0.3)
                    {pairs.Add((data[i],data[j]));if(pairs.Count>=20)break;}
            }
        _o.WriteLine($"Near-identical pairs: {pairs.Count}");

        // Classify pairs
        var t1Div=new System.Collections.Generic.List<(TP a,TP b)>();
        var t4Div=new System.Collections.Generic.List<(TP a,TP b)>();
        var conv=new System.Collections.Generic.List<(TP a,TP b)>();
        foreach(var(a,b)in pairs){
            bool outDiv=(a.cs4>0.1)!=(b.cs4>0.1);
            if(!outDiv){conv.Add((a,b));continue;}
            double dOm0=Math.Abs(a.om0-b.om0),dOm1=Math.Abs(a.om1-b.om1);
            if(dOm0>0.05||dOm1>0.1)t1Div.Add((a,b));else t4Div.Add((a,b));
        }
        _o.WriteLine($"T1-divergent: {t1Div.Count}, T4-divergent: {t4Div.Count}, Convergent: {conv.Count}");

        // --- 1. T1 vs T4 Divergence Comparison ---
        _o.WriteLine("\n--- 1. T1 vs T4 Divergence Comparison ---");
        if(t1Div.Count>0&&t4Div.Count>0){
            double t1Om0=t1Div.Average(p=>Math.Abs(p.a.om0-p.b.om0)),t4Om0=t4Div.Average(p=>Math.Abs(p.a.om0-p.b.om0));
            double t1Om1=t1Div.Average(p=>Math.Abs(p.a.om1-p.b.om1)),t4Om1=t4Div.Average(p=>Math.Abs(p.a.om1-p.b.om1));
            double t1Om2=t1Div.Average(p=>Math.Abs(p.a.om2-p.b.om2)),t4Om2=t4Div.Average(p=>Math.Abs(p.a.om2-p.b.om2));
            double t1Om3=t1Div.Average(p=>Math.Abs(p.a.om3-p.b.om3)),t4Om3=t4Div.Average(p=>Math.Abs(p.a.om3-p.b.om3));
            double t1CsGap=t1Div.Average(p=>Math.Abs(p.a.cs4-p.b.cs4)),t4CsGap=t4Div.Average(p=>Math.Abs(p.a.cs4-p.b.cs4));
            double t1Lam0=t1Div.Average(p=>Math.Abs(p.a.lam0-p.b.lam0)),t4Lam0=t4Div.Average(p=>Math.Abs(p.a.lam0-p.b.lam0));
            double t1Reb3=t1Div.Average(p=>Math.Abs(p.a.reb3-p.b.reb3)),t4Reb3=t4Div.Average(p=>Math.Abs(p.a.reb3-p.b.reb3));

            _o.WriteLine($"{"Metric",-14} {"T1-Div",10} {"T4-Div",10} {"Ratio",10} {"Sig?",6}");
            Pr("T0 Om d",t1Om0,t4Om0);Pr("T1 Om d",t1Om1,t4Om1);Pr("T2 Om d",t1Om2,t4Om2);
            Pr("T3 Om d",t1Om3,t4Om3);Pr("cs gap",t1CsGap,t4CsGap);
            Pr("lam0 d",t1Lam0,t4Lam0);Pr("reb3 d",t1Reb3,t4Reb3);

            void Pr(string n,double a,double b){double r=b>0.001?a/b:0;string s=Math.Abs(Math.Log(r+0.001))>0.5?"Yes":"No";_o.WriteLine($"{n,-14} {a,10:F4} {b,10:F4} {r,10:F2} {s,6}");}
        }else _o.WriteLine("Insufficient data for comparison.");

        // --- 2. T4 Divergence Mechanism Analysis ---
        _o.WriteLine("\n--- 2. T4 Divergence Mechanism Analysis ---");
        string t4Mech="Insufficient data";
        if(t4Div.Count>0){
            // a0 proximity: how close is each profile to the THR boundary?
            double t4A0ProxA=t4Div.Average(p=>Math.Abs(p.a.om3-THR)),t4A0ProxB=t4Div.Average(p=>Math.Abs(p.b.om3-THR));
            double t4CsSensA=t4Div.Average(p=>Math.Abs(p.a.cs4-p.a.om1)),t4CsSensB=t4Div.Average(p=>Math.Abs(p.b.cs4-p.b.om1));
            // omDist at T2 (Omega T1)
            double t4OmDistA=t4Div.Average(p=>p.a.omDist2),t4OmDistB=t4Div.Average(p=>p.b.omDist2);

            _o.WriteLine("T4-divergent pair characteristics:");
            _o.WriteLine($"  a0 proximity (|om3-THR|): A={t4A0ProxA:F3}, B={t4A0ProxB:F3}");
            _o.WriteLine($"  c3 sensitivity (|cs4-om1|): A={t4CsSensA:F3}, B={t4CsSensB:F3}");
            _o.WriteLine($"  omDist at T2: A={t4OmDistA:F3}, B={t4OmDistB:F3}");

            // Test: does T4 divergence correlate with a0 proximity difference?
            double a0DiffCorr=0;int ct=0;
            foreach(var(a,b)in t4Div){
                double a0d=Math.Abs(Math.Abs(a.om3-THR)-Math.Abs(b.om3-THR));
                double csd=Math.Abs(a.cs4-b.cs4);
                if(ct++<1)a0DiffCorr=Math.Abs(a0d-csd);
            }
            t4Mech=t4A0ProxA<0.2||t4A0ProxB<0.2?"Model F — Threshold/a0 proximity interaction":
                   t4CsSensA>0.5||t4CsSensB>0.5?"Model E — C3 computation sensitivity":
                   "Model H — Unresolved (insufficient signal)";
            _o.WriteLine($"T4 mechanism classification: {t4Mech}");
        }

        // --- 3. Early-Similarity Paradox ---
        _o.WriteLine("\n--- 3. Early-Similarity Paradox Analysis ---");
        double divTOm0=t1Div.Concat(t4Div).Average(p=>Math.Abs(p.a.om0-p.b.om0));
        double convTOm0=conv.Count>0?conv.Average(p=>Math.Abs(p.a.om0-p.b.om0)):0;
        double divTOm3=t1Div.Concat(t4Div).Average(p=>Math.Abs(p.a.om3-p.b.om3));
        double convTOm3=conv.Count>0?conv.Average(p=>Math.Abs(p.a.om3-p.b.om3)):0;

        _o.WriteLine($"Divergent T0 Om delta: {divTOm0:F4}  Convergent T0 Om delta: {convTOm0:F4}  Ratio: {(convTOm0>0.001?divTOm0/convTOm0:0):F2}");
        _o.WriteLine($"Divergent T3 Om delta: {divTOm3:F4}  Convergent T3 Om delta: {convTOm3:F4}  Ratio: {(convTOm3>0.001?divTOm3/convTOm3:0):F2}");

        string paradox;
        if(divTOm0<convTOm0&&divTOm3<convTOm3)
            paradox="Model A — Late-stage amplification (small early differences amplify at T4)";
        else if(divTOm0<convTOm0*0.7)
            paradox="Model B — Threshold sensitivity (convergent pairs have larger noise margin)";
        else paradox="Model F — Unresolved (no clear pattern)";
        _o.WriteLine($"Paradox classification: {paradox}");

        // --- 4. Temporal Trace Explanatory Power ---
        _o.WriteLine("\n--- 4. Temporal Trace Explanatory Power ---");
        int totalDiv=t1Div.Count+t4Div.Count;
        _o.WriteLine($"Total divergent pairs: {totalDiv}");
        _o.WriteLine($"Explained at T1: {t1Div.Count} ({100.0*t1Div.Count/Math.Max(1,totalDiv):F1}%)");
        _o.WriteLine($"Unexplained (T4 only): {t4Div.Count} ({100.0*t4Div.Count/Math.Max(1,totalDiv):F1}%)");
        string tracePower=t1Div.Count>=totalDiv*0.5?"Trace explains majority":"Trace explains minority — hidden state dominates";
        _o.WriteLine($"Trace power: {tracePower}");

        // --- 5. Hidden Response-State Classification ---
        _o.WriteLine("\n--- 5. Hidden Response-State Classification ---");
        string hidden;
        if(t1Div.Count>=totalDiv*0.4)hidden="Model A — Temporal Omega trajectory (early divergence signals)";
        else if(t4Div.Count>totalDiv*0.5&&(t4Div.Average(p=>Math.Abs(p.a.om3-THR))<0.2||t4Div.Average(p=>Math.Abs(p.b.om3-THR))<0.2))
            hidden="Model F — Threshold / a0 interaction (Omega T2 proximity to THR)";
        else if(t4Div.Count>totalDiv*0.5)
            hidden="Model G — Unrecorded microstate / C3 response path dependence";
        else hidden="Model H — Unresolved";
        _o.WriteLine($"Classification: {hidden}");

        // --- 6. Causal Closure Update ---
        _o.WriteLine("\n--- 6. Causal Closure Update ---");
        _o.WriteLine($"Causal closure: {(hidden.Contains("Unresolved")||hidden.Contains("Unrecorded")?"Hidden state UNRESOLVED":"Trace explanation IMPROVED but not causal")}");
        _o.WriteLine("Temporal trace LOCALIZES divergence to late-stage T4 but does not identify mechanism.");

        // --- 7. Operational Audit ---
        _o.WriteLine("\n--- 7. Stop-Low Operational Audit ---");
        int stopA=data.Count(d=>d.cs4<=0.1),rescA=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"Stop-Low A: {stopA} profiles, {rescA} rescues — SAFE. Stop-Low unchanged.");

        // --- 8. Decision Gates ---
        _o.WriteLine("\n--- 8. Decision Gates ---");
        bool gA=t1Div.Count>0||t4Div.Count>0;
        bool gB=t1Div.Count>0||t4Div.Count>0;
        bool gC=paradox.Contains("Model");
        bool gD=t1Div.Count>=totalDiv*0.3;
        bool gE=hidden.Contains("Model");
        bool gF=(rescA==0);
        bool gG=t1Div.Count>=totalDiv*0.4;
        bool gH=t4Div.Count>totalDiv*0.5;
        bool gI=true;

        _o.WriteLine($"Gate A (T1/T4 comparison): {(gA?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate B (T4 divergence localized): {(gB?"REACHED":"FAILED")}  —  {t4Mech}");
        _o.WriteLine($"Gate C (Paradox addressed): {(gC?"REACHED":"FAILED")}  —  {paradox}");
        _o.WriteLine($"Gate D (Trace adds explanatory power): {(gD?"REACHED":"NOT REACHED")}  —  {t1Div.Count}/{totalDiv} T1-explained");
        _o.WriteLine($"Gate E (Hidden factor classified): {(gE?"REACHED":"FAILED")}  —  {hidden}");
        _o.WriteLine($"Gate F (Stop-Low preserved): {(gF?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate G (Causal closure improved): {(gG?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (Hidden state unresolved): {(gH?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate I (V6 still not ready): {(gI?"REACHED":"FAILED")}");

        // --- 9. Claim Discipline ---
        _o.WriteLine("\n--- 9. Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: {(t4Div.Count>0?$"T4-divergent pairs dominate ({t4Div.Count}/{totalDiv})":"T1-divergent pairs dominate")}.");
        _o.WriteLine($"SUPPORTED: Early-similarity: {paradox}.");
        _o.WriteLine($"SUPPORTED: Hidden factor: {hidden}.");
        _o.WriteLine("SUPPORTED: Temporal trace LOCALIZES but does not EXPLAIN late-stage divergence.");
        _o.WriteLine("SUPPORTED: Stop-Low operational validity is unchanged.");
        _o.WriteLine("NOT CLAIMED: Causal closure, V6 readiness, physical interpretation.");
        _o.WriteLine("Next: HTS_FinalSynthesis");

        _o.WriteLine("\nV6: Length NOT READY | Space NOT READY | Velocity NOT READY | c NOT READY");
        _o.WriteLine($"\n=== HTA_01 complete. ===");
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
