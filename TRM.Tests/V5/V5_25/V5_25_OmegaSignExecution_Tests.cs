using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_25;

[Trait("Category","V5_25"),Trait("Category","V5_25_OSE"),Trait("Category","LongRunning")]
public class V5_25_OmegaSignExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;
    // Frozen sign rule — NO retuning
    const double OMDIST_THR=0.5;
    const double LAMBDA1_THR=0.95;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct ValProfile{
        public int n,s,cohort;public string stateGroup,failureClass;
        public double omT1,omDist,lambda1,kStd,kMean;
        public double deltaD,deltaK,c3OmegaShift,omegaPerK,deltaAlign,movementNorm;
        public int opkSign;public bool rulePos,rescued,persistent,a0;
        public string label; // TP, FP, TN, FN
    }

    public V5_25_OmegaSignExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    ValProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new ValProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0=sb.Value.d0,km0=sb.Value.km0,ks0=sb.Value.ks0;

        // M3++ probe to T1
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);

        p.omT1=Of(hT1,n).Average();p.omDist=THR-p.omT1;
        p.lambda1=Lambda1(KT1,n);p.kMean=Km(KT1,n);p.kStd=Ks(KT1,n);

        // Frozen rule prediction
        p.rulePos=p.omDist<OMDIST_THR&&p.lambda1<LAMBDA1_THR;

        // T2
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        double omT2=Of(hT2,n).Average();p.a0=omT2>THR;

        // C3
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nudged=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nudged+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);
            p.deltaD=dmPost-dmPre;p.deltaK=kmPost-kmPre;
            p.deltaAlign=0;p.movementNorm=Math.Sqrt(p.deltaD*p.deltaD+p.deltaK*p.deltaK);
            p.c3OmegaShift=omC3-omT2;
            p.omegaPerK=p.c3OmegaShift/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.opkSign=p.omegaPerK>0.1?1:p.omegaPerK<-0.1?-1:0;
            bool c3=omC3>THR;
            p.rescued=c3&&!p.a0;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=c3&&Of(hCont,n).Average()>THR;
        }else{p.rescued=false;p.persistent=false;}

        // Label
        bool actualPos=p.opkSign>0;
        p.label=p.rulePos?(actualPos?"TP":"FP"):(actualPos?"FN":"TN");
        p.stateGroup=p.label;

        // Failure classification
        if(p.label=="FP")p.failureClass=p.persistent?"sign-ok-no-rescue":"no-persistence";
        else if(p.label=="FN")p.failureClass=p.omDist<OMDIST_THR?"lambda1-borderline":"both-fail";
        else if(p.label=="TP")p.failureClass=p.persistent?"rescued":"sign-ok-no-persist";
        else p.failureClass="correct-reject";

        return p;
    }

    [Fact]public void OSE_01_SignRuleValidation(){
        _o.WriteLine("═══ OSE_01: Sign rule validation — frozen omDist+lambda1 ═══");
        _o.WriteLine($"Frozen rule: omDist<{OMDIST_THR} AND lambda1<{LAMBDA1_THR}");

        int[] Ns={62,63,64,65,66,67,70,72,75,80};
        var refSet=new ConcurrentBag<ValProfile>(); // seeds 0-99
        var hoSet=new ConcurrentBag<ValProfile>();  // seeds 100-399

        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){
                if(IsHi(n,s))continue;
                var p=BuildProfile(n,s,hi,lo);
                if(p==null)continue;
                if(s<100)refSet.Add(p.Value);else hoSet.Add(p.Value);
            }});

        var refAll=refSet.ToArray();var hoAll=hoSet.ToArray();
        _o.WriteLine($"\nReference (seeds 0-99): {refAll.Length}, Holdout (100-399): {hoAll.Length}");

        // ─── Rule validation: predict positive omegaPerK sign ───
        _o.WriteLine($"\n─── Sign prediction: combined rule ───");
        PrintValidation(refAll,hoAll,"sign");

        // ─── Single-variable comparison ───
        _o.WriteLine($"\n─── Single-variable comparison ───");
        _o.WriteLine(string.Format("{0,-20} {1,8} {2,8} {3,8} {4,8}",
            "Rule","RefAcc","HoAcc","HoPrec","HoRec"));
        foreach(var (label,fn) in new (string,Func<ValProfile,bool>)[]{
            ("omDist<0.5 only",p=>p.omDist<OMDIST_THR),
            ("lambda1<0.95 only",p=>p.lambda1<LAMBDA1_THR),
            ("omDist OR lambda1",p=>p.omDist<OMDIST_THR||p.lambda1<LAMBDA1_THR),
            ("omDist AND lambda1",p=>p.rulePos)}){
            var (hoAcc,hoPrec,hoRec)=ComputeMetrics(hoAll,fn);
            var (refAcc,_,_)=ComputeMetrics(refAll,fn);
            _o.WriteLine($"{label,-20} {refAcc,7:F0}% {hoAcc,7:F0}% {hoPrec,7:F0}% {hoRec,7:F0}%");
        }

        // ─── Rescue prediction ───
        _o.WriteLine($"\n─── Rescue prediction ───");
        _o.WriteLine(string.Format("{0,-20} {1,8} {2,8} {3,8} {4,8}",
            "Rule","HoAcc","HoPrec","HoRec","F1"));
        var (ha,hp,hr)=ComputeMetrics(hoAll,p=>p.rulePos,p=>p.persistent);
        var (ha2,hp2,hr2)=ComputeMetrics(refAll,p=>p.rulePos,p=>p.persistent);
        _o.WriteLine($"omDist+lambda1 (ho) {ha,7:F0}% {hp,7:F0}% {hr,7:F0}% {2*hp*hr/Math.Max(1,hp+hr),7:F0}%");
        _o.WriteLine($"omDist+lambda1 (ref) {ha2,7:F0}% {hp2,7:F0}% {hr2,7:F0}%");

        // Rescue rate by rule
        var rulePos=hoAll.Where(p=>p.rulePos).ToArray();
        var ruleNeg=hoAll.Where(p=>!p.rulePos).ToArray();
        _o.WriteLine($"\nRule-positive rescue rate: {rulePos.Count(p=>p.persistent)*100.0/Math.Max(1,rulePos.Length):F0}% ({rulePos.Length} seeds)");
        _o.WriteLine($"Rule-negative rescue rate: {ruleNeg.Count(p=>p.persistent)*100.0/Math.Max(1,ruleNeg.Length):F0}% ({ruleNeg.Length} seeds)");

        // ─── Cross-N validation ───
        _o.WriteLine($"\n─── Cross-N sign prediction (holdout) ───");
        _o.WriteLine(string.Format("{0,4} {1,6} {2,8} {3,8} {4,8} {5,8}",
            "N","n","Acc","Prec","Rec","rulePos%"));
        foreach(var n in Ns){
            var sub=hoAll.Where(p=>p.n==n).ToArray();
            if(sub.Length<3)continue;
            var (acc,prec,rec)=ComputeSignMetrics(sub);
            _o.WriteLine($"{n,4} {sub.Length,6} {acc,7:F0}% {prec,7:F0}% {rec,7:F0}% {sub.Count(p=>p.rulePos)*100.0/sub.Length,7:F0}%");
        }

        // ─── Failure analysis ───
        _o.WriteLine($"\n─── Failure analysis (holdout) ───");
        var fps=hoAll.Where(p=>p.label=="FP").ToArray();
        var fns=hoAll.Where(p=>p.label=="FN").ToArray();
        _o.WriteLine($"False positives: {fps.Length} ({fps.Length*100.0/hoAll.Length:F0}%)");
        _o.WriteLine($"  sign-ok-no-rescue: {fps.Count(p=>p.failureClass=="sign-ok-no-rescue")}");
        _o.WriteLine($"  no-persistence: {fps.Count(p=>p.failureClass=="no-persistence")}");
        _o.WriteLine($"False negatives: {fns.Length} ({fns.Length*100.0/hoAll.Length:F0}%)");
        _o.WriteLine($"  lambda1-borderline: {fns.Count(p=>p.failureClass=="lambda1-borderline")}");
        _o.WriteLine($"  both-fail: {fns.Count(p=>p.failureClass=="both-fail")}");

        // True positives rescued
        var tps=hoAll.Where(p=>p.label=="TP").ToArray();
        _o.WriteLine($"True positives: {tps.Length} — rescued: {tps.Count(p=>p.persistent)} ({tps.Count(p=>p.persistent)*100.0/Math.Max(1,tps.Length):F0}%)");

        // ─── Boundary explanation ───
        _o.WriteLine($"\n─── Boundary explanation ───");
        var n64all=hoAll.Where(p=>p.n==64).ToArray();
        _o.WriteLine($"N=64: rulePos={n64all.Count(p=>p.rulePos)*100.0/Math.Max(1,n64all.Length):F0}% omDist mean={n64all.Average(p=>p.omDist):F3} lambda1 mean={n64all.Average(p=>p.lambda1):F3}");
        var n65all=hoAll.Where(p=>p.n==65).ToArray();
        _o.WriteLine($"N=65: rulePos={n65all.Count(p=>p.rulePos)*100.0/Math.Max(1,n65all.Length):F0}% omDist mean={n65all.Average(p=>p.omDist):F3} lambda1 mean={n65all.Average(p=>p.lambda1):F3}");

        // ─── Gates ───
        var (hAcc,hPrec,hRec)=ComputeSignMetrics(hoAll);
        var (rAcc,rPrec,rRec)=ComputeRescueMetrics(hoAll);
        _o.WriteLine($"\n─── Gates ───");
        _o.WriteLine($"Gate A (Sign rule validated): {(hAcc>60?"REACHED":"NOT REACHED")} ({hAcc:F0}%)");
        _o.WriteLine($"Gate B (Rescue partially): {(rAcc>70?"REACHED":"NOT REACHED")} ({rAcc:F0}%)");
        _o.WriteLine($"Gate C (Combined beats single): assessing above");
        _o.WriteLine($"Gate D (N=64 explained): {(n64all.Count(p=>p.rulePos)<n64all.Length*0.2?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (N=65 onset): {(n65all.Any(p=>p.rulePos&&p.persistent)?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (Diagnostic only): {(!rulePos.Any(p=>p.persistent)?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Validation complete ───");
    }

    // ─── Metrics ───
    static (double acc,double prec,double rec) ComputeSignMetrics(ValProfile[] ps){
        int tp=ps.Count(p=>p.label=="TP"),fp=ps.Count(p=>p.label=="FP");
        int tn=ps.Count(p=>p.label=="TN"),fn=ps.Count(p=>p.label=="FN");
        double acc=(tp+tn)*100.0/ps.Length;
        double prec=tp*100.0/Math.Max(1,tp+fp);
        double rec=tp*100.0/Math.Max(1,tp+fn);
        return (acc,prec,rec);
    }
    static (double acc,double prec,double rec) ComputeRescueMetrics(ValProfile[] ps){
        int tp=ps.Count(p=>p.rulePos&&p.persistent),fp=ps.Count(p=>p.rulePos&&!p.persistent);
        int tn=ps.Count(p=>!p.rulePos&&!p.persistent),fn=ps.Count(p=>!p.rulePos&&p.persistent);
        double acc=(tp+tn)*100.0/ps.Length;
        double prec=tp*100.0/Math.Max(1,tp+fp);
        double rec=tp*100.0/Math.Max(1,tp+fn);
        return (acc,prec,rec);
    }
    static (double acc,double prec,double rec) ComputeMetrics(ValProfile[] ps,Func<ValProfile,bool> rule,Func<ValProfile,bool>? outcome=null){
        outcome??=p=>p.opkSign>0;
        int tp=ps.Count(p=>rule(p)&&outcome(p)),fp=ps.Count(p=>rule(p)&&!outcome(p));
        int tn=ps.Count(p=>!rule(p)&&!outcome(p)),fn=ps.Count(p=>!rule(p)&&outcome(p));
        double acc=(tp+tn)*100.0/ps.Length;
        double prec=tp*100.0/Math.Max(1,tp+fp);
        double rec=tp*100.0/Math.Max(1,tp+fn);
        return (acc,prec,rec);
    }
    void PrintValidation(ValProfile[] refPs,ValProfile[] hoPs,string target){
        var (rAcc,rPrec,rRec)=ComputeMetrics(refPs,p=>p.rulePos);
        var (hAcc,hPrec,hRec)=ComputeMetrics(hoPs,p=>p.rulePos);
        _o.WriteLine($"Reference: acc={rAcc:F0}% prec={rPrec:F0}% rec={rRec:F0}%");
        _o.WriteLine($"Holdout:   acc={hAcc:F0}% prec={hPrec:F0}% rec={hRec:F0}%");
    }

    // ─── Frozen M3++ ───
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0&&!double.IsNaN(vn)?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        bool isP1=sb.cls=="P1"||sb.cls=="P1b";
        if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))return null;
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
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
