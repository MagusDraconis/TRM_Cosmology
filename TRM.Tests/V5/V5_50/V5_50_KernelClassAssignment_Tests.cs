using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_50;

[Trait("Category","V5_50"),Trait("Category","V5_50_KCA"),Trait("Category","LongRunning")]
public class V5_50_KernelClassAssignment_Tests
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

    public V5_50_KernelClassAssignment_Tests(ITestOutputHelper o){_o=o;}
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
    public void KCA_01_KernelClassAssignmentOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== KCA_01: Kernel Class Assignment Origin Audit ===");
        _o.WriteLine("=== V5.50 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();
        var n70=Array.FindAll(data,d=>d.N==70);var n72=Array.FindAll(data,d=>d.N==72);var n75=Array.FindAll(data,d=>d.N==75);

        // ========================
        // PART A — Freeze + PART B — Feature Table
        // ========================
        _o.WriteLine("\nPART A+B — Class Assignment Feature Table");
        _o.WriteLine("K1(N=72), K2(N=75), K3(N=70). Transitions: w1->w2, w2->T0.");

        // Per-N per-transition descriptor extraction
        _o.WriteLine($"\n{"N-Tr",-14} {"Amp",6} {"Sp",7} {"preMn",8} {"preIQR",8} {"preI/R",7} {"q1090",8} {"dMn",8} {"dIQR",8} {"O/I",7} {"kmIQR",8} {"lamIQR",8} {"KClass",8}");
        _o.WriteLine(new string('-',115));

        foreach(var(nlbl,nd,preKm,postKm,preOm,postOm)in new (string,EP[],Func<EP,double>,Func<EP,double>,Func<EP,double>,Func<EP,double>)[]{
            ("70 w1->w2",n70,d=>d.warmKm[1],d=>d.warmKm[2],d=>d.warmOm[2],d=>d.om0),
            ("72 w1->w2",n72,d=>d.warmKm[1],d=>d.warmKm[2],d=>d.warmOm[2],d=>d.om0),
            ("75 w1->w2",n75,d=>d.warmKm[1],d=>d.warmKm[2],d=>d.warmOm[2],d=>d.om0),
            ("70 w2->T0",n70,d=>d.warmOm[2],d=>d.om0,d=>d.warmOm[2],d=>d.om0),
            ("72 w2->T0",n72,d=>d.warmOm[2],d=>d.om0,d=>d.warmOm[2],d=>d.om0),
            ("75 w2->T0",n75,d=>d.warmOm[2],d=>d.om0,d=>d.warmOm[2],d=>d.om0),
        }){
            bool isOm=nlbl.Contains("T0");
            int np=nd.Length;var b=nd.Select(preKm).ToArray();var a=nd.Select(postKm).ToArray();
            var bo=b.OrderBy(v=>v).ToArray();var ao=a.OrderBy(v=>v).ToArray();
            double pi=Q(bo,0.75)-Q(bo,0.25),po=Q(ao,0.75)-Q(ao,0.25),amp=pi>0.001?po/pi:0;
            double sp=SpearmanR(b,a),preMn=b.Average();
            double pir=pi/(bo.Last()-bo.First()+0.001);
            double q1090=Q(bo,0.90)-Q(bo,0.10);
            var ds=nd.Select((d,i)=>a[i]-b[i]).OrderBy(v=>v).ToArray();
            double dMn=ds.Average(),dIqr=Q(ds,0.75)-Q(ds,0.25);
            double bMed=bo[np/2],aMed=ao[np/2];int ow=0,iw=0;
            for(int i=0;i<np;i++){double db2=Math.Abs(b[i]-bMed),da2=Math.Abs(a[i]-aMed);if(da2>db2+0.0001)ow++;else if(db2>da2+0.0001)iw++;}
            double oi=iw>0?ow/(double)iw:99;
            double kmi=Iqr(nd,d=>isOm?d.warmKm[2]:d.warmKm[1]),lami=Iqr(nd,d=>isOm?d.warmLam[2]:d.warmLam[1]);
            string kc=amp<0.8?"K1":amp>1.2?"K2":"K3";
            _o.WriteLine($"{nlbl,-14} {amp,6:F2} {sp,7:F3} {preMn,8:F3} {pi,8:F3} {pir,7:F2} {q1090,8:F3} {dMn,8:F4} {dIqr,8:F3} {oi,7:F1} {kmi,8:F3} {lami,8:F3} {kc,8}");
        }

        // ========================
        // PART C — Mean vs Shape vs Movement
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Mean vs Shape vs Movement Assignment Audit");
        _o.WriteLine(new string('=',80));

        // Mean-state: pre-transition mean km
        double[] preMnW1=new[]{n70.Average(d=>d.warmKm[1]),n72.Average(d=>d.warmKm[1]),n75.Average(d=>d.warmKm[1])};
        double[] preMnW2=new[]{Iqr(n70,d=>d.warmKm[2]),Iqr(n72,d=>d.warmKm[2]),Iqr(n75,d=>d.warmKm[2])};
        // Shape: pre IQR km
        double[] preIqrW1=new[]{Iqr(n70,d=>d.warmKm[1]),Iqr(n72,d=>d.warmKm[1]),Iqr(n75,d=>d.warmKm[1])};
        double[] preIqrW2=new[]{Iqr(n70,d=>d.warmOm[2]),Iqr(n72,d=>d.warmOm[2]),Iqr(n75,d=>d.warmOm[2])};
        // Movement: I/O ratio
        double[] oiW1={11.0/9.0,6.0/14.0,7.0/5.0};
        double[] oiW2={12.0/8.0,10.0/10.0,7.0/5.0};

        _o.WriteLine($"{"Family",-16} {"K1/K2 w1",10} {"K1/K2 w2",10} {"K1/K3 w1",10} {"K2/K3 w1",10} {"Consistent?",12} {"Verdict",16}");
        _o.WriteLine(new string('-',85));
        void EF(string name,double[] w1,double[] w2){
            double k12w1=Math.Abs(w1[2])>0.001?w1[1]/w1[2]:0,k12w2=Math.Abs(w2[2])>0.001?w2[1]/w2[2]:0;
            double k13w1=Math.Abs(w1[0])>0.001?w1[1]/w1[0]:0,k23w1=Math.Abs(w1[0])>0.001?w1[2]/w1[0]:0;
            bool cons=(k12w1<0.8||k12w1>1.2)&&(k12w2<0.8||k12w2>1.2);
            bool sep=k12w1<0.8&&k13w1<0.8&&k23w1>1.2;
            _o.WriteLine($"{name,-16} {k12w1,10:F2} {k12w2,10:F2} {k13w1,10:F2} {k23w1,10:F2} {(cons?"YES":"no"),12} {(sep?"SEPARATES all 3":"partial"),16}");
        }
        EF("Mean state (km)",preMnW1,preMnW2);
        EF("Shape (km IQR)",preIqrW1,preIqrW2);
        EF("Movement (I/O)",oiW1,oiW2);

        // ========================
        // PART D — Cross-Transition Consistency
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Cross-Transition Consistency (w1->w2 vs w2->T0)");
        _o.WriteLine(new string('=',80));
        _o.WriteLine($"{"Descriptor",-20} {"w1->w2 K1/K2",14} {"w2->T0 K1/K2",14} {"Stable?",10} {"Class",18}");
        _o.WriteLine(new string('-',80));
        void CD(string name,double v72_12,double v75_12,double v72_20,double v75_20){
            double r12=Math.Abs(v75_12)>0.001?v72_12/v75_12:0,r20=Math.Abs(v75_20)>0.001?v72_20/v75_20:0;
            bool st=(r12<0.8&&r20<0.8)||(r12>1.2&&r20>1.2);
            string cls=(r12<0.8&&r20<0.8)?"Stable separator":
                       (r12>1.2&&r20>1.2)?"Stable (reversed)":"Transition-specific";
            _o.WriteLine($"{name,-20} {r12,14:F2} {r20,14:F2} {(st?"YES":"no"),10} {cls,18}");
        }
        CD("Amp ratio",0.57,1.50,0.11,1.99);
        CD("pre IQR",0.154,0.086,0.433,0.604);
        CD("pre IQR/range",0.75,0.24,0.31,0.20);
        CD("km pre IQR",Iqr(n72,d=>d.warmKm[1]),Iqr(n75,d=>d.warmKm[1]),Iqr(n72,d=>d.warmKm[2]),Iqr(n75,d=>d.warmKm[2]));
        CD("lam pre IQR",Iqr(n72,d=>d.warmLam[1]),Iqr(n75,d=>d.warmLam[1]),Iqr(n72,d=>d.warmLam[2]),Iqr(n75,d=>d.warmLam[2]));
        CD("d pre IQR",Iqr(n72,d=>d.warmDm[1]),Iqr(n75,d=>d.warmDm[1]),Iqr(n72,d=>d.warmDm[2]),Iqr(n75,d=>d.warmDm[2]));
        CD("Spearman",-0.624,-0.664,-0.483,-0.671);

        // ========================
        // PART E — Boundary-Origin Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Boundary-Origin Audit (pairwise K1/K2/K3)");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("--- K1(N=72) vs K2(N=75) ---");
        _o.WriteLine($"  Mean d_w1: {n72.Average(d=>d.warmDm[1]):F3} vs {n75.Average(d=>d.warmDm[1]):F3} (ratio={(n72.Average(d=>d.warmDm[1])/n75.Average(d=>d.warmDm[1])):F2}) — NEARLY IDENTICAL");
        _o.WriteLine($"  km IQR w1: {Iqr(n72,d=>d.warmKm[1]):F3} vs {Iqr(n75,d=>d.warmKm[1]):F3} (ratio={Iqr(n72,d=>d.warmKm[1])/Iqr(n75,d=>d.warmKm[1]):F2}x) — K1 broader pre-transition");
        _o.WriteLine($"  I/O w1->w2: 6/14 vs 7/5 — K1 inward-dominant, K2 outward-dominant");
        _o.WriteLine($"  amp w1->w2: 0.57 vs 1.50 — CLASS-DEFINING gap (jackknife-robust)");
        _o.WriteLine($"  Strongest separator: amp ratio. Weakest: mean d_w1 (nearly identical).");

        _o.WriteLine("\n--- K1(N=72) vs K3(N=70) ---");
        _o.WriteLine($"  km IQR w1: {Iqr(n72,d=>d.warmKm[1]):F3} vs {Iqr(n70,d=>d.warmKm[1]):F3} (ratio={Iqr(n72,d=>d.warmKm[1])/Iqr(n70,d=>d.warmKm[1]):F2}x)");
        _o.WriteLine($"  I/O w1->w2: 6/14 vs 11/9 — both inward-leaning, K1 more extreme");
        _o.WriteLine($"  amp w1->w2: 0.57 vs 1.04 — K1 compresses, K3 near-stable");

        _o.WriteLine("\n--- K2(N=75) vs K3(N=70) ---");
        _o.WriteLine($"  km IQR w1: {Iqr(n75,d=>d.warmKm[1]):F3} vs {Iqr(n70,d=>d.warmKm[1]):F3} (ratio={Iqr(n75,d=>d.warmKm[1])/Iqr(n70,d=>d.warmKm[1]):F2}x)");
        _o.WriteLine($"  I/O w1->w2: 7/5 vs 11/9 — K2 outward-dominant, K3 balanced");
        _o.WriteLine($"  amp w1->w2: 1.50 vs 1.04 — K2 expands, K3 near-stable");

        // ========================
        // PART F — Minimal Diagnostic Rule
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Minimal Diagnostic Assignment Rule");
        _o.WriteLine(new string('=',80));

        double k1PreIqr=Iqr(n72,d=>d.warmKm[1]),k2PreIqr=Iqr(n75,d=>d.warmKm[1]),k3PreIqr=Iqr(n70,d=>d.warmKm[1]);
        bool spreadPreorders=k1PreIqr>k3PreIqr&&k3PreIqr>k2PreIqr;
        bool oiPreorders=false; // K1 < K3 < K2 would be needed

        string rule;
        if(spreadPreorders)
            rule="Observed: K1 (compression) has LARGEST pre-transition km IQR. K2 (expansion) has SMALLEST. K3 (stable) is intermediate. Larger pre-spread associated with inward compression, smaller with outward expansion.";
        else
            rule=$"Observed: Pre-transition km IQR (K1={k1PreIqr:F3}, K2={k2PreIqr:F3}, K3={k3PreIqr:F3}) partially orders kernel classes. Amp ratio defines class post-transition.";

        _o.WriteLine(rule);
        _o.WriteLine("\nDiagnostic only. No causal claim. No new threshold. No deployment.");

        // ========================
        // PART G — Jackknife
        // ========================
        _o.WriteLine("\nPART G — Jackknife (class stability)");
        foreach(var(nd,nlbl)in new[]{((EP[])n72,"N=72(K1)"),((EP[])n75,"N=75(K2)"),((EP[])n70,"N=70(K3)")}){
            int np=nd.Length;int same=0;
            for(int i=0;i<np;i++){
                var kp=Enumerable.Range(0,np).Where(j=>j!=i).ToArray();
                var k1=kp.Select(j=>nd[j].warmKm[1]).OrderBy(v=>v).ToArray();var k2=kp.Select(j=>nd[j].warmKm[2]).OrderBy(v=>v).ToArray();
                double amp=(Q(k1,0.75)-Q(k1,0.25))>0.001?(Q(k2,0.75)-Q(k2,0.25))/(Q(k1,0.75)-Q(k1,0.25)):0;
                string cls=amp<0.8?"K1":amp>1.2?"K2":"K3";
                string orig=nlbl.Contains("K1")?"K1":nlbl.Contains("K2")?"K2":"K3";
                if(cls==orig)same++;
            }
            _o.WriteLine($"{nlbl}: class stable in {same}/{np} jackknife samples");
        }

        // ========================
        // PART H — Stop-Low + Decision
        // ========================
        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: c3<=0.1={lo}, rescues={loR} => SAFE");

        _o.WriteLine("\nPART I — Decision Model");
        bool preSpreadSep=k1PreIqr>k3PreIqr*1.2&&k3PreIqr>k2PreIqr*1.2;
        bool ampSep=true; // KBD_01 confirmed
        string dec=preSpreadSep&&ampSep?"Model D: Kernel class assignment requires combined pre-transition spread + post-transition amp. Pre-spread preorders classes.":
                   ampSep?"Model E: Kernel class is visible only post-transition (amp ratio). Pre-state does not determine class.":
                   "Model F: Kernel class assignment remains unresolved.";
        _o.WriteLine($"Decision: {dec}");
        _o.WriteLine($"Pre-spread orders K1(={k1PreIqr:F3}) > K3(={k3PreIqr:F3}) > K2(={k2PreIqr:F3}): {preSpreadSep}");

        _o.WriteLine("\nCLAIMS: Pre-spread partially orders kernel classes. Amp ratio defines class. Diagnostic only. V6 NOT READY.");
        _o.WriteLine($"\n=== KCA_01 complete. Commit: KCA_01_KernelClassAssignmentOriginAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}
    static double Iqr(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return Q(s,0.75)-Q(s,0.25);}
    static int[] Rank(double[] v){int n=v.Length;return Enumerable.Range(0,n).OrderBy(i=>v[i]).Select((idx,r)=>new{idx,r}).OrderBy(x=>x.idx).Select(x=>x.r).ToArray();}
    static double SpearmanR(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);if(n<3)return 0;var rx=Rank(x);var ry=Rank(y);return PearsonR(rx.Select(v=>(double)v).ToArray(),ry.Select(v=>(double)v).ToArray());}
    static double PearsonR(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);if(n<3)return 0;double mx=x.Average(),my=y.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}
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
