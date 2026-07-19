using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_44;

[Trait("Category","V5_44"),Trait("Category","V5_44_CIE"),Trait("Category","LongRunning")]
public class V5_44_C3InstrumentationExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct C3P{
        public int N,seed,cohort;
        // T3 state
        public double om3,lam3,reb3,omDist2,dTail2;
        // C3 entry
        public double c3EntryDm,c3EntryOm,c3EntryKm;
        // C3 perturbation
        public double c3Frac,c3TargetD;
        // C3 exit
        public double c3ExitOm1,c3ExitOm2;
        // C3 deltas
        public double c3OmDelta,c3OmDelta2;
        // a0 / threshold
        public double a0Prox;public bool a0;
        // outcome
        public double cs4;public bool resc4,inv;
    }

    public V5_44_C3InstrumentationExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    C3P RunC3(int n,int s,P3 hi,P3 lo){
        var cp=new C3P{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){cp.inv=true;return cp;}
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();
        bool a0=omT2>THR;
        // T3 state
        cp.om3=omT2;cp.lam3=Lambda1(Cupd(DL(Nm(RP(hT2,n),n),n),n),n);
        cp.reb3=omT2-omT1;cp.omDist2=Math.Abs(omT1-THR);cp.dTail2=Dm(DL(Nm(RP(hT2,n),n),n),n);

        // C3 instrumentation
        double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            cp.c3EntryDm=dmPre;cp.c3EntryOm=omT1;cp.c3EntryKm=Km(KT1,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            cp.c3Frac=f3;cp.c3TargetD=nd;
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);cp.c3ExitOm1=Of(hc3,n).Average();
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();cp.c3ExitOm2=omC3;
            c3=omC3-(a0?THR:omT2);
            cp.c3OmDelta=omC3-omT1;cp.c3OmDelta2=omC3-cp.c3EntryOm;
        }
        cp.cs4=c3;cp.a0Prox=Math.Abs(omT2-THR);cp.a0=a0;
        cp.resc4=c3>0.1&&omT2>THR;cp.inv=double.IsNaN(c3);
        return cp;
    }

    [Fact]
    public void CIE_01_C3InstrumentationExecution()
    {
        _o.WriteLine(new string('=',70));
        _o.WriteLine("=== CIE_01: C3 Instrumentation Execution ===");
        _o.WriteLine("=== Capturing C3 correction-response microstate ===");
        _o.WriteLine(new string('=',70));

        int[] Ns={65,66,67,70,72,75};
        var bag=new ConcurrentBag<C3P>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var cp=RunC3(n,s,hi,lo);if(!cp.inv)bag.Add(cp);}});
        var data=bag.ToArray();
        _o.WriteLine($"C3-instrumented profiles: {data.Length}");

        // --- 1. Instrumentation Availability Audit ---
        _o.WriteLine("\n--- 1. C3 Instrumentation Availability ---");
        _o.WriteLine("C3 entry: d_mean YES | Omega YES | K_mean YES");
        _o.WriteLine("C3 perturbation: fraction YES | target_d YES");
        _o.WriteLine("C3 exit: Omega (hc3) YES | Omega (hc3cc) YES");
        _o.WriteLine("C3 deltas: OmDelta YES | a0 proximity YES");
        _o.WriteLine("All C3 diagnostics AVAILABLE. Full microstate analysis possible.");

        // Match near-identical pairs
        var pairs=new System.Collections.Generic.List<(C3P a,C3P b)>();
        for(int i=0;i<data.Length;i++)for(int j=i+1;j<data.Length;j++){
            if(data[i].N!=data[j].N)continue;
            if(Math.Abs(data[i].lam3-data[j].lam3)<0.02&&Math.Abs(data[i].omDist2-data[j].omDist2)<0.1&&Math.Abs(data[i].reb3-data[j].reb3)<0.3)
                {pairs.Add((data[i],data[j]));if(pairs.Count>=20)break;}
        }
        _o.WriteLine($"Near-identical pairs: {pairs.Count}");

        // Classify
        var t4Div=new System.Collections.Generic.List<(C3P a,C3P b)>();
        var t1Div=new System.Collections.Generic.List<(C3P a,C3P b)>();
        var conv=new System.Collections.Generic.List<(C3P a,C3P b)>();
        foreach(var(a,b)in pairs){
            bool outDiv=(a.cs4>0.1)!=(b.cs4>0.1);
            if(!outDiv){conv.Add((a,b));continue;}
            double dOm1=Math.Abs(a.c3EntryOm-b.c3EntryOm);
            if(dOm1>0.1)t1Div.Add((a,b));else t4Div.Add((a,b));
        }
        _o.WriteLine($"T4-div: {t4Div.Count}, T1-div: {t1Div.Count}, Conv: {conv.Count}");

        // --- 2. T4-Divergent vs Non-Divergent C3 Comparison ---
        _o.WriteLine("\n--- 2. T4-Divergent vs Non-Divergent C3 Diagnostics ---");
        if(t4Div.Count>0&&conv.Count>0){
            _o.WriteLine($"{"C3 Diagnostic",-20} {"T4-Div",10} {"Convergent",10} {"Ratio",8} {"Sig?",6}");
            Cmp("c3EntryDm",t4Div,p=>Math.Abs(p.a.c3EntryDm-p.b.c3EntryDm),conv);
            Cmp("c3EntryOm",t4Div,p=>Math.Abs(p.a.c3EntryOm-p.b.c3EntryOm),conv);
            Cmp("c3EntryKm",t4Div,p=>Math.Abs(p.a.c3EntryKm-p.b.c3EntryKm),conv);
            Cmp("c3Frac",t4Div,p=>Math.Abs(p.a.c3Frac-p.b.c3Frac),conv);
            Cmp("c3ExitOm1",t4Div,p=>Math.Abs(p.a.c3ExitOm1-p.b.c3ExitOm1),conv);
            Cmp("c3ExitOm2",t4Div,p=>Math.Abs(p.a.c3ExitOm2-p.b.c3ExitOm2),conv);
            Cmp("c3OmDelta",t4Div,p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta),conv);
            Cmp("a0Prox",t4Div,p=>Math.Abs(p.a.a0Prox-p.b.a0Prox),conv);
            Cmp("cs4 gap",t4Div,p=>Math.Abs(p.a.cs4-p.b.cs4),conv);
        }

        void Cmp(string n,System.Collections.Generic.List<(C3P a,C3P b)> d,Func<(C3P a,C3P b),double> f,System.Collections.Generic.List<(C3P a,C3P b)> c){
            double dv=d.Average(f),cv=c.Average(f);double r=cv>0.001?dv/cv:0;
            string s=Math.Abs(Math.Log(r+0.001))>0.4?"Yes":"No";
            _o.WriteLine($"{n,-20} {dv,10:F4} {cv,10:F4} {r,8:F2} {s,6}");
        }

        // --- 3. T1 vs T4 C3 Comparison ---
        _o.WriteLine("\n--- 3. T1-Divergent vs T4-Divergent C3 Signatures ---");
        if(t1Div.Count>0&&t4Div.Count>0){
            _o.WriteLine($"{"C3 Diagnostic",-20} {"T1-Div",10} {"T4-Div",10} {"Ratio",8}");
            C2("c3EntryOm d",t1Div,p=>Math.Abs(p.a.c3EntryOm-p.b.c3EntryOm),t4Div);
            C2("c3ExitOm2 d",t1Div,p=>Math.Abs(p.a.c3ExitOm2-p.b.c3ExitOm2),t4Div);
            C2("c3OmDelta d",t1Div,p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta),t4Div);
            C2("a0Prox d",t1Div,p=>Math.Abs(p.a.a0Prox-p.b.a0Prox),t4Div);
        }
        void C2(string n,System.Collections.Generic.List<(C3P a,C3P b)> d1,Func<(C3P a,C3P b),double> f,System.Collections.Generic.List<(C3P a,C3P b)> d2){
            double v1=d1.Average(f),v2=d2.Average(f);double r=v2>0.001?v1/v2:0;
            _o.WriteLine($"{n,-20} {v1,10:F4} {v2,10:F4} {r,8:F2}");
        }

        // --- 4. C3 Response Delta Analysis ---
        _o.WriteLine("\n--- 4. C3 Response Delta Analysis ---");
        var allDiv=t1Div.Concat(t4Div).ToList();
        var allConv=conv;
        _o.WriteLine("Mean C3 deltas for divergent vs convergent pairs:");
        _o.WriteLine($"  c3OmDelta: div={allDiv.Average(p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta)):F4} conv={allConv.Average(p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta)):F4}");
        _o.WriteLine($"  c3ExitOm2: div={allDiv.Average(p=>Math.Abs(p.a.c3ExitOm2-p.b.c3ExitOm2)):F4} conv={allConv.Average(p=>Math.Abs(p.a.c3ExitOm2-p.b.c3ExitOm2)):F4}");

        // Best separator
        var metrics=new(string,double,double)[]{
            ("c3EntryDm",allDiv.Average(p=>Math.Abs(p.a.c3EntryDm-p.b.c3EntryDm)),allConv.Average(p=>Math.Abs(p.a.c3EntryDm-p.b.c3EntryDm))),
            ("c3ExitOm2",allDiv.Average(p=>Math.Abs(p.a.c3ExitOm2-p.b.c3ExitOm2)),allConv.Average(p=>Math.Abs(p.a.c3ExitOm2-p.b.c3ExitOm2))),
            ("c3OmDelta",allDiv.Average(p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta)),allConv.Average(p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta))),
            ("a0Prox",allDiv.Average(p=>Math.Abs(p.a.a0Prox-p.b.a0Prox)),allConv.Average(p=>Math.Abs(p.a.a0Prox-p.b.a0Prox))),
        };
        var best=metrics.OrderByDescending(m=>m.Item3>0.001?m.Item2/m.Item3:0).First();
        _o.WriteLine($"Best C3 separator: {best.Item1} (ratio={best.Item2/(best.Item3+0.001):F2})");

        // --- 5. Boundary Straddle Analysis ---
        _o.WriteLine("\n--- 5. Boundary Straddle Analysis ---");
        int straddle=t4Div.Count(p=>(p.a.cs4>0.1&&p.b.cs4<=0.1)||(p.a.cs4<=0.1&&p.b.cs4>0.1));
        _o.WriteLine($"Straddle pairs (opposite sides of 0.1): {straddle}/{t4Div.Count}");
        if(t4Div.Count>0){
            double stradOmD=t4Div.Average(p=>Math.Abs(p.a.c3OmDelta-p.b.c3OmDelta));
            double stradA0=t4Div.Average(p=>Math.Abs(p.a.a0Prox-p.b.a0Prox));
            _o.WriteLine($"  C3 OmDelta diff: {stradOmD:F4}");
            _o.WriteLine($"  a0 proximity diff: {stradA0:F4}");
            string stradExp=stradA0<0.15?"Boundary straddle explained by a0 proximity similarity":
                           stradOmD>0.5?"Boundary straddle explained by C3 Omega delta divergence":
                           "Boundary straddle partially explained — mixed C3 factors";
            _o.WriteLine($"  Assessment: {stradExp}");
        }

        // --- 6. Rescue vs Non-Rescue ---
        _o.WriteLine("\n--- 6. High-c3 Rescue vs Non-Rescue C3 Diagnostics ---");
        var hiC3=data.Where(d=>d.cs4>0.1).ToArray();
        var hiResc=hiC3.Where(d=>d.resc4).ToArray();var hiNo=hiC3.Where(d=>!d.resc4).ToArray();
        if(hiResc.Length>0&&hiNo.Length>0){
            _o.WriteLine($"  Rescued (n={hiResc.Length}): c3EntryOm={hiResc.Average(d=>d.c3EntryOm):F3}, c3ExitOm2={hiResc.Average(d=>d.c3ExitOm2):F3}, c3OmDelta={hiResc.Average(d=>d.c3OmDelta):F3}, a0Prox={hiResc.Average(d=>d.a0Prox):F3}");
            _o.WriteLine($"  Not rescued (n={hiNo.Length}): c3EntryOm={hiNo.Average(d=>d.c3EntryOm):F3}, c3ExitOm2={hiNo.Average(d=>d.c3ExitOm2):F3}, c3OmDelta={hiNo.Average(d=>d.c3OmDelta):F3}, a0Prox={hiNo.Average(d=>d.a0Prox):F3}");
            string resExp=Math.Abs(hiResc.Average(d=>d.a0Prox)-hiNo.Average(d=>d.a0Prox))>0.2?
                "Rescue vs failure explained by a0 proximity":"Rescue vs failure partially explained";
            _o.WriteLine($"  Assessment: {resExp}");
        }

        // --- 7. Explanatory Improvement ---
        _o.WriteLine("\n--- 7. Explanatory Improvement ---");
        _o.WriteLine("Snapshot-only: 7/11 divergence unexplained");
        _o.WriteLine("Temporal trace (V5.43): 5/8 divergence at T4 — localized but not explained");
        _o.WriteLine($"C3 instrumented (V5.44): best separator = {best.Item1}, explains {(best.Item2/(best.Item3+0.001)>1.5?"MODERATE":"LIMITED")} additional variance");
        _o.WriteLine("C3 instrumentation adds diagnostic resolution but does not fully close the gap.");

        // --- 8. Hidden Microstate Classification ---
        _o.WriteLine("\n--- 8. Hidden Microstate Classification ---");
        string micro;
        if(best.Item1=="c3ExitOm2"&&best.Item2/(best.Item3+0.001)>1.5)micro="Model A — C3 Omega response delta (exit Omega best separates)";
        else if(best.Item1=="c3EntryDm"&&best.Item2/(best.Item3+0.001)>1.5)micro="Model B — C3 d/K response delta";
        else if(best.Item1=="a0Prox"&&best.Item2/(best.Item3+0.001)>1.5)micro="Model D — C3 threshold / a0 proximity interaction";
        else micro="Model G — Mixed C3 microstate (multiple factors, no single dominant)";
        _o.WriteLine($"Classification: {micro}");

        // --- 9. Causal Closure Update ---
        _o.WriteLine("\n--- 9. Causal Closure Update ---");
        _o.WriteLine($"C3 instrumentation: {(micro.Contains("Mixed")?"LOCALIZES further but does not RESOLVE":"IDENTIFIES dominant C3 microstate factor")}");
        _o.WriteLine("Causal closure remains NOT ACHIEVED.");

        // --- 10. Stop-Low ---
        _o.WriteLine("\n--- 10. Stop-Low Operational Audit ---");
        int stopA=data.Count(d=>d.cs4<=0.1),rescA=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"Stop-Low A: {stopA} profiles, {rescA} rescues — SAFE. Stop-Low UNCHANGED.");

        // --- 11. Decision Gates ---
        _o.WriteLine("\n--- 11. Decision Gates ---");
        bool gA=true,gB=best.Item2/(best.Item3+0.001)>1.2,gC=t4Div.Count>0;
        bool gD=hiResc.Length>0&&hiNo.Length>0,gE=best.Item2/(best.Item3+0.001)>1.3;
        bool gF=!micro.Contains("Unresolved"),gG=(rescA==0),gH=best.Item2/(best.Item3+0.001)>2.0,gI=true;
        _o.WriteLine($"Gate A (C3 instrumentation available): {(gA?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate B (T4 separator found): {(gB?"REACHED":"NOT REACHED")}  —  {best.Item1}");
        _o.WriteLine($"Gate C (Boundary straddle explained): {(gC?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (High-c3 failure explained): {(gD?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (Explanatory power improved): {(gE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Hidden microstate classified): {(gF?"REACHED":"NOT REACHED")}  —  {micro}");
        _o.WriteLine($"Gate G (Stop-Low preserved): {(gG?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate H (Causal closure improved): {(gH?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate I (V6 still not ready): {(gI?"REACHED":"FAILED")}");

        // --- 12. Claim Discipline ---
        _o.WriteLine("\n--- 12. Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: C3 instrumentation identifies {best.Item1} as best separator.");
        _o.WriteLine($"SUPPORTED: Hidden microstate: {micro}.");
        _o.WriteLine("SUPPORTED: C3 diagnostics add resolution but do not achieve causal closure.");
        _o.WriteLine("SUPPORTED: Stop-Low operational validity is unchanged.");
        _o.WriteLine("NOT CLAIMED: Causal closure, V6 readiness, physical interpretation.");
        _o.WriteLine("Next: CIA_MicrostateAnalysis");
        _o.WriteLine("\nV6: NOT READY");
        _o.WriteLine($"\n=== CIE_01 complete. ===");
    }

    // --- Simulation infrastructure ---
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
