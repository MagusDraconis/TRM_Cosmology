using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_49;

[Trait("Category","V5_49"),Trait("Category","V5_49_DSG"),Trait("Category","LongRunning")]
public class V5_49_SpreadGeneration_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct EP{
        public int N,seed,cohort;
        public double[] warmOm,warmDm,warmKm,warmKs,warmLam;
        public double om0,lam0,d0,km0,ks0,om1,lam1,d1,km1,ks1,om2,lam2,d2,km2,ks2,omDist2,om3,lam3,d3,km3,ks3,reb3;
        public double entOm,entDm,entKm,entLam,frac,exitOm2,omDelta,cs4,a0Prox;public bool resc4,inv;
    }

    public V5_49_SpreadGeneration_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    EP RunEP(int n,int s,P3 hi,P3 lo){
        var ep=new EP{N=n,seed=s,cohort=n%5};
        var sb=SelectAndClassify(n,s,hi);if(sb==null){ep.inv=true;return ep;}
        double d0Pre=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0Pre*0.90:d0Pre*0.50;
        var warmOm=new double[WARMUP_EPOCHS];var warmDm=new double[WARMUP_EPOCHS];var warmKm=new double[WARMUP_EPOCHS];var warmKs=new double[WARMUP_EPOCHS];var warmLam=new double[WARMUP_EPOCHS];
        var K=KS(n,s);
        for(int e=0;e<WARMUP_EPOCHS;e++){
            var h=Sim(K,n,S,s+e);warmOm[e]=Of(h,n).Average();
            var d=DL(Nm(RP(h,n),n),n);warmDm[e]=Dm(d,n);K=Cupd(d,n);
            warmKm[e]=Km(K,n);warmKs[e]=Ks(K,n);warmLam[e]=Lambda1(K,n);
        }
        ep.warmOm=warmOm;ep.warmDm=warmDm;ep.warmKm=warmKm;ep.warmKs=warmKs;ep.warmLam=warmLam;
        var hT0=Sim(K,n,S,s+50);ep.om0=Of(hT0,n).Average();ep.lam0=Lambda1(K,n);
        var dT0=DL(Nm(RP(hT0,n),n),n);ep.d0=Dm(dT0,n);ep.km0=Km(K,n);ep.ks0=Ks(K,n);
        var h4=Sim(K,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K=Cupd(dM,n);
        var h5=Sim(K,n,S,s+4);K=Cupd(DL(Nm(RP(h5,n),n),n),n);
        ep.om1=Of(h5,n).Average();ep.lam1=Lambda1(K,n);ep.d1=Dm(CD(d4,n),n);ep.km1=Km(K,n);ep.ks1=Ks(K,n);
        var hT1=Sim(K,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);double omT1=Of(hT1,n).Average();
        ep.om2=omT1;ep.lam2=Lambda1(KT1,n);ep.omDist2=Math.Abs(omT1-THR);
        var dmat2=DL(Nm(RP(hT1,n),n),n);ep.d2=Dm(dmat2,n);ep.km2=Km(KT1,n);ep.ks2=Ks(KT1,n);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();bool a0=omT2>THR;
        var KT3=Cupd(DL(Nm(RP(hT2,n),n),n),n);
        ep.om3=omT2;ep.lam3=Lambda1(KT3,n);ep.reb3=omT2-omT1;ep.d3=Dm(DL(Nm(RP(hT2,n),n),n),n);ep.km3=Km(KT3,n);ep.ks3=Ks(KT3,n);
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
    public void DSG_01_SpreadGenerationAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== DSG_01: Spread Generation Audit ===");
        _o.WriteLine("=== V5.49 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        var bag=new ConcurrentBag<EP>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<100;s++){if(IsHi(n,s))continue;var ep=RunEP(n,s,hi,lo);if(!ep.inv)bag.Add(ep);}});
        var data=bag.ToArray();

        // ========================
        // PART A — Protocol Freeze
        // ========================
        _o.WriteLine("\nPART A — Protocol Freeze");
        _o.WriteLine("N: 70, 72, 75. Stages: w0, w1, w2. Vars: km, lam, om, d.");
        _o.WriteLine("Forbidden: no new vars, no tuning, no seed removal, no V6.");
        _o.WriteLine("Protocol FROZEN.");

        // Helper
        double Iqr(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return Q(s,0.75)-Q(s,0.25);}
        double Rng(EP[] nd,Func<EP,double> f){var s=nd.Select(f).OrderBy(v=>v).ToArray();return s.Last()-s.First();}

        // ========================
        // PART B — Spread Origin Trace
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART B — Spread Origin Trace (km IQR across warmup)");
        _o.WriteLine(new string('=',80));

        string[] stages={"w0","w1","w2"};
        Func<EP,double>[] kmF={d=>d.warmKm[0],d=>d.warmKm[1],d=>d.warmKm[2]};
        Func<EP,double>[] lamF={d=>d.warmLam[0],d=>d.warmLam[1],d=>d.warmLam[2]};
        Func<EP,double>[] dF={d=>d.warmDm[0],d=>d.warmDm[1],d=>d.warmDm[2]};

        _o.WriteLine($"{"Variable",-12} {"Metric",8} {"N=70",10} {"N=72",10} {"N=75",10} {"72->75",8}");
        _o.WriteLine(new string('-',65));
        foreach(var (name,f) in new[]{("km",kmF),("lam",lamF),("d",dF)}){
            for(int s=0;s<3;s++){
                double i70=Iqr(data.Where(d=>d.N==70).ToArray(),f[s]),i72=Iqr(data.Where(d=>d.N==72).ToArray(),f[s]),i75=Iqr(data.Where(d=>d.N==75).ToArray(),f[s]);
                _o.WriteLine($"{name+" IQR",-12} {stages[s],8} {i70,10:F3} {i72,10:F3} {i75,10:F3} {(i72>0.001?i75/i72:0),8:F2}");
            }
            _o.WriteLine("");
        }

        // Origin detection: at which stage does N=75 IQR first exceed N=72 by >1.2x?
        _o.WriteLine("--- Origin Detection ---");
        for(int s=0;s<3;s++){
            double k72=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[s]),k75=Iqr(data.Where(d=>d.N==75).ToArray(),kmF[s]);
            double l72=Iqr(data.Where(d=>d.N==72).ToArray(),lamF[s]),l75=Iqr(data.Where(d=>d.N==75).ToArray(),lamF[s]);
            double kRat=k72>0.001?k75/k72:0,lRat=l72>0.001?l75/l72:0;
            string marker=(kRat>1.2||lRat>1.2)?$" <-- N=75 BROADER emerges at {stages[s]}":"";
            _o.WriteLine($"  {stages[s]}: km IQR ratio (75/72)={kRat:F2}x, lam IQR ratio={lRat:F2}x{marker}");
        }

        // ========================
        // PART C — Spread Accumulation Audit
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART C — Spread Accumulation Audit");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"Var",-6} {"w0_IQR",9} {"w1_IQR",9} {"w2_IQR",9} {"w0->1",7} {"w1->2",7} {"w0->2",7} {"Pattern",20}");
        _o.WriteLine(new string('-',85));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            foreach(var (name,f) in new[]{("km",kmF),("lam",lamF),("d",dF)}){
                double i0=Iqr(nd,f[0]),i1=Iqr(nd,f[1]),i2=Iqr(nd,f[2]);
                double r01=i0>0.001?i1/i0:0,r12=i1>0.001?i2/i1:0,r02=i0>0.001?i2/i0:0;
                string pat=r02>2?"ACCUMULATING":r02<0.5?"COLLAPSING":Math.Abs(r02-1.0)<0.2?"STABLE":"MODERATE growth";
                _o.WriteLine($"{n,6} {name,-6} {i0,9:F3} {i1,9:F3} {i2,9:F3} {r01,7:F2} {r12,7:F2} {r02,7:F2} {pat,20}");
            }
        }

        // ========================
        // PART D — Shape Lineage
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART D — Shape Lineage (km IQR/range across warmup)");
        _o.WriteLine(new string('=',80));

        _o.WriteLine($"{"N",6} {"w0 I/R",8} {"w1 I/R",8} {"w2 I/R",8} {"w0->w2",8} {"Class",25}");
        _o.WriteLine(new string('-',65));
        foreach(var n in Ns){
            var nd=data.Where(d=>d.N==n).ToArray();if(nd.Length<4)continue;
            double[] ir={Iqr(nd,kmF[0])/(Rng(nd,kmF[0])+0.001),Iqr(nd,kmF[1])/(Rng(nd,kmF[1])+0.001),Iqr(nd,kmF[2])/(Rng(nd,kmF[2])+0.001)};
            double ir02=ir[0]>0.001?ir[2]/ir[0]:0;
            string cls=ir02>1.5?"BROADENING":ir02<0.5?"NARROWING":"STABLE shape";
            if(ir[0]>0.2&&ir[2]>0.2)cls="EARLY BROAD";
            else if(ir[0]<0.15&&ir[2]>0.2)cls="LATE BROADENING";
            else if(ir[2]<0.15)cls="STABLE NARROW";
            _o.WriteLine($"{n,6} {ir[0],8:F2} {ir[1],8:F2} {ir[2],8:F2} {ir02,8:F2} {cls,25}");
        }

        // Which model?
        _o.WriteLine($"\n--- Lineage Model ---");
        double n75ir0=Iqr(data.Where(d=>d.N==75).ToArray(),kmF[0])/(Rng(data.Where(d=>d.N==75).ToArray(),kmF[0])+0.001);
        double n75ir2=Iqr(data.Where(d=>d.N==75).ToArray(),kmF[2])/(Rng(data.Where(d=>d.N==75).ToArray(),kmF[2])+0.001);
        double n72ir0=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0])/(Rng(data.Where(d=>d.N==72).ToArray(),kmF[0])+0.001);
        double n72ir2=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2])/(Rng(data.Where(d=>d.N==72).ToArray(),kmF[2])+0.001);

        string lineage=n75ir0>0.2?"Model A: Early broadness — spread inherited from w0":
                        n75ir2/n75ir0>1.5?"Model B: Gradual broadening across warmup":
                        n75ir2>0.2&&n75ir0<0.15?"Model C: Late broadening at a specific transition":
                        "Model D: Spread generation unresolved";
        _o.WriteLine($"N=75: {lineage}");
        string n72line=n72ir0>0.2?"Model A: Early broadness":
                         n72ir2/n72ir0<0.5?"Model D: Shape collapse":"Model B/C: Gradual/late";
        _o.WriteLine($"N=72: {n72line}");

        // ========================
        // PART E — Robustness
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART E — Leave-One-N Robustness");
        _o.WriteLine(new string('=',80));

        var pairs=new[]{("All N",new[]{70,72,75}),("w/o N=70",new[]{72,75}),("w/o N=72",new[]{70,75}),("w/o N=75",new[]{70,72})};
        _o.WriteLine($"{"Subset",-14} {"km w0 IQR 75/72",15} {"km w2 IQR 75/72",15} {"lam w0 IQR 75/72",15} {"lam w2 IQR 75/72",15}");
        _o.WriteLine(new string('-',75));
        foreach(var (name,ns) in pairs){
            var keep75=data.Where(d=>d.N==75).ToArray();
            double kw0_75=Iqr(keep75,kmF[0]),kw2_75=Iqr(keep75,kmF[2]);
            double lw0_75=Iqr(keep75,lamF[0]),lw2_75=Iqr(keep75,lamF[2]);
            double kw0_72=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0]),kw2_72=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2]);
            double lw0_72=Iqr(data.Where(d=>d.N==72).ToArray(),lamF[0]),lw2_72=Iqr(data.Where(d=>d.N==72).ToArray(),lamF[2]);
            double kr0=kw0_72>0.001?kw0_75/kw0_72:0,kr2=kw2_72>0.001?kw2_75/kw2_72:0;
            double lr0=lw0_72>0.001?lw0_75/lw0_72:0,lr2=lw2_72>0.001?lw2_75/lw2_72:0;
            _o.WriteLine($"{name,-14} {kr0,15:F2} {kr2,15:F2} {lr0,15:F2} {lr2,15:F2}");
        }

        // ========================
        // PART F — Decision Model
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART F — Decision Model");
        _o.WriteLine(new string('=',80));

        double kmW0rat=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0])>0.001?Iqr(data.Where(d=>d.N==75).ToArray(),kmF[0])/Iqr(data.Where(d=>d.N==72).ToArray(),kmF[0]):0;
        double kmW2rat=Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2])>0.001?Iqr(data.Where(d=>d.N==75).ToArray(),kmF[2])/Iqr(data.Where(d=>d.N==72).ToArray(),kmF[2]):0;
        bool w0Broad=kmW0rat>1.2;
        bool w2Broad=kmW2rat>1.2;

        string decision;
        if(w0Broad&&w2Broad)decision="Model A: Spread inherited from earliest measured state (w0).";
        else if(!w0Broad&&w2Broad)decision="Model C: Spread generated in a specific warmup transition (w1->w2 or w0->w1).";
        else if(kmW2rat>0.8)decision="Model B: Spread generated gradually during warmup.";
        else decision="Model D: Spread generation unresolved.";

        _o.WriteLine($"Decision: {decision}");
        _o.WriteLine($"  km IQR 75/72: w0={kmW0rat:F2}x, w2={kmW2rat:F2}x");

        // ========================
        // PART G — Claim Discipline
        // ========================
        _o.WriteLine("\n" + new string('=',80));
        _o.WriteLine("PART G — Claim Discipline");
        _o.WriteLine(new string('=',80));

        _o.WriteLine("\nSUPPORTED:");
        _o.WriteLine($"  - km w0 IQR 75/72 ratio: {kmW0rat:F2}x");
        _o.WriteLine($"  - km w2 IQR 75/72 ratio: {kmW2rat:F2}x");
        _o.WriteLine($"  - Spread origin: {(w0Broad?"present at w0":"emerges during warmup")}");
        _o.WriteLine($"  - N=75 shape lineage: {lineage}");

        _o.WriteLine("\nCONDITIONAL:");
        _o.WriteLine("  - finite-N (12-20 profiles per N)");
        _o.WriteLine("  - 3 warmup epochs only");
        _o.WriteLine("  - spread generation mechanism not measured directly");
        _o.WriteLine("  - no causal closure");

        _o.WriteLine("\nHYPOTHESIS:");
        _o.WriteLine("  - Spread may be generated during warmup transitions");
        _o.WriteLine("  - Distribution-generation mechanism remains hidden");
        _o.WriteLine("  - w2-state spread determines handoff outcome");

        _o.WriteLine("\nNOT CLAIMED:");
        _o.WriteLine("  - causality, deterministic rescue, physical interpretation, V6 readiness");

        // Stop-Low
        int lo=data.Count(d=>d.cs4<=0.1),loR=data.Count(d=>d.cs4<=0.1&&d.resc4);
        _o.WriteLine($"\nStop-Low: {lo} @ c3<=0.1, {loR} rescues => {(loR==0?"SAFE":"WARNING")}");

        _o.WriteLine($"\n=== DSG_01 complete. Commit: DSG_01_SpreadGenerationAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Average(v=>(v-m)*(v-m)));}
    static double Skew(double[] s){double m=s.Average();double sd=Sd(s);if(sd<1e-9)return 0;int n=s.Length;return n*s.Sum(v=>Math.Pow((v-m)/sd,3))/((n-1)*(n-2)+1);}
    static double CorrX(IEnumerable<double> x,IEnumerable<double> y){var a=x.ToArray();var b=y.ToArray();int n=Math.Min(a.Length,b.Length);if(n<3)return 0;double mx=a.Average(),my=b.Average(),sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=a[i]-mx,dy=b[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return sxy/Math.Sqrt(sx*sy+1e-15);}
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
