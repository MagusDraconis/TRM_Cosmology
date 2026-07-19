using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_25;

[Trait("Category","V5_25"),Trait("Category","V5_25_OSA"),Trait("Category","LongRunning")]
public class V5_25_OmegaSignRuleAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebT=0.01;
    const double OMDIST_THR=0.5,LAMBDA1_THR=0.95;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct RuleProfile{
        public int n,s,cohort;public string group,ruleClass;
        public double omDist,lambda1,kStd,dTail,dT1,deltaD,deltaK,kSens,c3OmgS,omegaPerK,alignPre;
        public int opkSign;public bool rulePos,rescued,persistent,a0;
        // Chain layer flags
        public bool hasDTail,hasDeltaD,hasDeltaK,hasPosSign,hasC3Gain;
    }

    public V5_25_OmegaSignRuleAnalysis_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    RuleProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new RuleProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return null;
        double d0=sb.Value.d0;

        // M3++ to T1
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);
        var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);

        p.dT1=Dm(dT1,n);p.lambda1=Lambda1(KT1,n);p.kStd=Ks(KT1,n);
        p.omDist=THR-Of(hT1,n).Average();
        p.rulePos=p.omDist<OMDIST_THR&&p.lambda1<LAMBDA1_THR;

        // d_tail
        var dVals=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)dVals.Add(dT1[i,j]);dVals.Sort();
        p.dTail=Percentile(dVals,0.95)-Percentile(dVals,0.50);
        p.hasDTail=p.dTail>0.5;

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
            double dmPost=Dm(dmat3,n),kmPost=Km(Cupd(dmat3,n),n);
            p.deltaD=dmPost-dmPre;p.deltaK=kmPost-kmPre;
            p.kSens=p.deltaK/Math.Max(1e-9,Math.Abs(p.deltaD));
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            double omC3=Of(hc3cc,n).Average();
            p.c3OmgS=omC3-omT2;p.omegaPerK=p.c3OmgS/Math.Max(1e-9,Math.Abs(p.deltaK));
            p.opkSign=p.omegaPerK>0.1?1:p.omegaPerK<-0.1?-1:0;
            bool c3=omC3>THR;p.rescued=c3&&!p.a0;
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=c3&&Of(hCont,n).Average()>THR;
        }else{p.rescued=false;p.persistent=false;}

        // Chain layer flags
        p.hasDeltaD=Math.Abs(p.deltaD)>0.01;
        p.hasDeltaK=Math.Abs(p.deltaK)>0.005;
        p.hasPosSign=p.opkSign>0;
        p.hasC3Gain=p.c3OmgS>0.05;

        // Group classification
        if(p.rulePos&&p.persistent)p.group="G1-rule+rescue";
        else if(p.rulePos&&!p.persistent)p.group="G2-rule+noRescue";
        else if(!p.rulePos&&p.persistent)p.group="G3-rule-rescue";
        else p.group="G4-rule-noRescue";

        p.ruleClass=p.rulePos?(p.opkSign>0?"TP-sign":"FP-sign"):(p.opkSign>0?"FN-sign":"TN-sign");
        return p;
    }

    [Fact]public void OSA_01_SignRuleDeepAnalysis(){
        _o.WriteLine("═══ OSA_01: Sign rule deep analysis ═══");

        int[] Ns={62,63,64,65,66,67,70,72,75,80};
        var profiles=new ConcurrentBag<RuleProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<399;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)profiles.Add(p.Value);}});
        var all=profiles.ToArray();
        _o.WriteLine($"\nTotal seeds: {all.Length}");

        // ─── 1. Precision/recall decomposition ───
        var tp=all.Where(p=>p.rulePos&&p.opkSign>0).ToArray();
        var fp=all.Where(p=>p.rulePos&&p.opkSign<=0).ToArray();
        var fnSeeds=all.Where(p=>!p.rulePos&&p.opkSign>0).ToArray();
        var tn=all.Where(p=>!p.rulePos&&p.opkSign<=0).ToArray();
        _o.WriteLine($"\n─── Confusion matrix ───");
        _o.WriteLine($"TP={tp.Length} FP={fp.Length} FN={fnSeeds.Length} TN={tn.Length}");
        _o.WriteLine($"Precision={tp.Length*100.0/(tp.Length+fp.Length):F0}% Recall={tp.Length*100.0/(tp.Length+fnSeeds.Length):F0}%");

        // Why low recall? Analyze FN
        _o.WriteLine($"\n─── False negatives (rule-, sign+): {fnSeeds.Length} ───");
        _o.WriteLine($"omDist: mean={fnSeeds.Average(p=>p.omDist):F3} <{OMDIST_THR}={fnSeeds.Count(p=>p.omDist<OMDIST_THR)*100.0/fnSeeds.Length:F0}%");
        _o.WriteLine($"lambda1: mean={fnSeeds.Average(p=>p.lambda1):F3} <{LAMBDA1_THR}={fnSeeds.Count(p=>p.lambda1<LAMBDA1_THR)*100.0/fnSeeds.Length:F0}%");
        _o.WriteLine($"omDist borderline [0.5,0.6): {fnSeeds.Count(p=>p.omDist>=0.5&&p.omDist<0.6)*100.0/fnSeeds.Length:F0}%");
        _o.WriteLine($"lambda1 borderline [0.90,0.95): {fnSeeds.Count(p=>p.lambda1>=0.90&&p.lambda1<0.95)*100.0/fnSeeds.Length:F0}%");

        // ─── 2. Rule-positive non-rescue ───
        var rpResc=all.Where(p=>p.rulePos&&p.persistent).ToArray();
        var rpNoResc=all.Where(p=>p.rulePos&&!p.persistent).ToArray();
        _o.WriteLine($"\n─── Rule-positive: rescued vs not ───");
        _o.WriteLine($"Rescued: {rpResc.Length}, Not rescued: {rpNoResc.Length}");
        _o.WriteLine(string.Format("{0,-12} {1,10} {2,10}","Metric","Rescued","NoResc"));
        string[] gm={"dTail","dT1","deltaD","deltaK","c3OmgS","omegaPerK","kSens"};
        foreach(var m in gm){
            double rv=Math.Abs(GetMean(rpResc,m)),nv=Math.Abs(GetMean(rpNoResc,m));
            _o.WriteLine($"{m,-12} {rv,10:F4} {nv,10:F4}");
        }

        // ─── 3. Rule-negative rescues ───
        var rnResc=all.Where(p=>!p.rulePos&&p.persistent).ToArray();
        _o.WriteLine($"\n─── Rule-negative rescues: {rnResc.Length} ───");
        if(rnResc.Length>0){
            _o.WriteLine($"omDist={rnResc.Average(p=>p.omDist):F3} lambda1={rnResc.Average(p=>p.lambda1):F3}");
            _o.WriteLine($"dTail={rnResc.Average(p=>p.dTail):F3} deltaD={rnResc.Average(p=>p.deltaD):F4} c3OmgS={rnResc.Average(p=>p.c3OmgS):F4}");
            _o.WriteLine($"These seeds rescue despite failing the sign rule.");
        }

        // ─── 4. N=65 onset analysis ───
        _o.WriteLine($"\n─── N=65 onset analysis ───");
        var n65=all.Where(p=>p.n==65).ToArray();
        var n65resc=n65.Where(p=>p.rescued).ToArray();
        var n65fail=n65.Where(p=>!p.rescued).ToArray();
        _o.WriteLine($"N=65 rescued: {n65resc.Length}/{n65.Length}");

        _o.WriteLine(string.Format("{0,-12} {1,10} {2,10}","Metric","Rescued","Failed"));
        string[] nm={"omDist","lambda1","dTail","deltaD","deltaK","c3OmgS","omegaPerK","opkSign"};
        foreach(var m in nm){
            double rv=GetMean(n65resc,m),fv=GetMean(n65fail,m);
            _o.WriteLine($"{m,-12} {rv,10:F4} {fv,10:F4}");
        }
        _o.WriteLine($"rulePos rate: rescued={n65resc.Count(p=>p.rulePos)*100.0/n65resc.Length:F0}% failed={n65fail.Count(p=>p.rulePos)*100.0/n65fail.Length:F0}%");
        _o.WriteLine($"sign+ rate: rescued={n65resc.Count(p=>p.opkSign>0)*100.0/n65resc.Length:F0}% failed={n65fail.Count(p=>p.opkSign>0)*100.0/n65fail.Length:F0}%");

        // ─── 5. Full-chain requirement ───
        _o.WriteLine($"\n─── Full-chain requirement ───");
        _o.WriteLine(string.Format("{0,-30} {1,6} {2,8}","Chain condition","n","Rescue%"));
        var chains=new (string,Func<RuleProfile,bool>)[]{
            ("rule-positive only",p=>p.rulePos),
            ("rule+ & dTail>0.5",p=>p.rulePos&&p.hasDTail),
            ("rule+ & deltaD>0.01",p=>p.rulePos&&p.hasDeltaD),
            ("rule+ & deltaK>0.005",p=>p.rulePos&&p.hasDeltaK),
            ("rule+ & sign+",p=>p.rulePos&&p.hasPosSign),
            ("rule+ & c3OmgS>0.05",p=>p.rulePos&&p.hasC3Gain),
            ("rule+ & sign+ & c3OmgS>0.05",p=>p.rulePos&&p.hasPosSign&&p.hasC3Gain),
            ("sign+ & c3OmgS>0.05 (no rule)",p=>p.hasPosSign&&p.hasC3Gain),
        };
        foreach(var (label,fn) in chains){
            var sub=all.Where(fn).ToArray();
            _o.WriteLine($"{label,-30} {sub.Length,6} {sub.Count(p=>p.persistent)*100.0/Math.Max(1,sub.Length),7:F0}%");
        }

        // ─── 6. Rule role classification ───
        _o.WriteLine($"\n─── Rule role classification ───");
        double ruleResc=all.Count(p=>p.rulePos&&p.persistent)*100.0/Math.Max(1,all.Count(p=>p.rulePos));
        double noRuleResc=all.Count(p=>!p.rulePos&&p.persistent)*100.0/Math.Max(1,all.Count(p=>!p.rulePos));
        double signResc=all.Count(p=>p.hasPosSign&&p.persistent)*100.0/Math.Max(1,all.Count(p=>p.hasPosSign));
        double chainResc=all.Count(p=>p.hasPosSign&&p.hasC3Gain&&p.persistent)*100.0/Math.Max(1,all.Count(p=>p.hasPosSign&&p.hasC3Gain));

        _o.WriteLine($"Rule+ rescue: {ruleResc:F0}%, Rule- rescue: {noRuleResc:F0}%");
        _o.WriteLine($"Sign+ rescue: {signResc:F0}%, Sign+ & C3Gain rescue: {chainResc:F0}%");

        string role;
        if(ruleResc>20)role="A: sufficient rescue rule";
        else if(ruleResc>5&&noRuleResc<2)role="B: necessary sign rule";
        else if(ruleResc>5)role="C: high-precision enrichment rule";
        else if(ruleResc<3)role="D: diagnostic-only rule";
        else role="E: incomplete chain rule";

        _o.WriteLine($"Classification: {role}");

        _o.WriteLine($"\n─── Gates ───");
        _o.WriteLine($"Gate A (Rule role clarified): REACHED — {role}");
        bool lowRecExplained=fnSeeds.Count(p=>p.omDist<0.6||p.lambda1<0.98)>fnSeeds.Length*0.5;
        _o.WriteLine($"Gate B (Low recall explained): {(lowRecExplained?"REACHED":"NOT REACHED")}");
        bool rpFailExplained=rpNoResc.Length>0&&rpNoResc.Average(p=>Math.Abs(p.c3OmgS))<0.1;
        _o.WriteLine($"Gate C (Rule+ failures explained): {(rpFailExplained?"REACHED":"NOT REACHED")}");
        bool n65Explained=n65resc.Length>0&&n65resc.All(p=>p.hasPosSign);
        _o.WriteLine($"Gate D (N=65 onset): {(n65Explained?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate E (Full chain required): {(chainResc>ruleResc*1.5?"REACHED":"NOT REACHED")}");

        _o.WriteLine($"\n─── Analysis complete ───");
    }

    static double GetMean(RuleProfile[] ps,string m)=>ps.Length==0?0:m switch{
        "dTail"=>ps.Average(p=>p.dTail),"dT1"=>ps.Average(p=>p.dT1),
        "deltaD"=>ps.Average(p=>p.deltaD),"deltaK"=>ps.Average(p=>p.deltaK),
        "c3OmgS"=>ps.Average(p=>p.c3OmgS),"omegaPerK"=>ps.Average(p=>p.omegaPerK),
        "kSens"=>ps.Average(p=>p.kSens),"omDist"=>ps.Average(p=>p.omDist),
        "lambda1"=>ps.Average(p=>p.lambda1),"opkSign"=>ps.Average(p=>p.opkSign),_=>0
    };
    static double Percentile(List<double> s,double p){if(s.Count==0)return 0;return s[Math.Clamp((int)(p*(s.Count-1)),0,s.Count-1)];}

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

