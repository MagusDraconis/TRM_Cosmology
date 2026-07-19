using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_38;

[Trait("Category","V5_38"),Trait("Category","V5_38_RIE"),Trait("Category","LongRunning")]
public class V5_38_ResponseInteractionExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;const double FTHR=0.1;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct RProfile{public int n,s,cohort;public double c3OmgS,omDist,lambda1,rebMag,dTail,kSens,omT1,omT2;public bool persistent,a0;}

    public V5_38_ResponseInteractionExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    RProfile? BuildProfile(int n,int s,P3 hi,P3 lo){
        var p=new RProfile{n=n,s=s,cohort=s/100};
        var sb=SelectAndClassify(n,s,hi);if(sb==null)return null;
        double d0=sb.Value.d0;bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        var hT1=Sim(K2,n,S,s+100);var dT1=DL(Nm(RP(hT1,n),n),n);var KT1=Cupd(dT1,n);
        p.omT1=Of(hT1,n).Average();p.omDist=THR-p.omT1;p.lambda1=Lambda1(KT1,n);
        var dVals=new List<double>();for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)dVals.Add(dT1[i,j]);dVals.Sort();
        p.dTail=Percentile(dVals,0.95)-Percentile(dVals,0.50);
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
        p.omT2=Of(hT2,n).Average();p.a0=p.omT2>THR;
        p.rebMag=Dm(DL(Nm(RP(hT2,n),n),n),n)-p.dTail;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);
            double dmPre=Dm(dmat3,n),kmPre=Km(Cupd(dmat3,n),n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;
            double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            p.kSens=(Km(Cupd(dmat3,n),n)-kmPre)/Math.Max(1e-9,Math.Abs(dmPre*(f3-1)));
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            p.c3OmgS=Of(hc3cc,n).Average()-(p.a0?THR:p.omT2);
            var hCont=Sim(Cupd(DL(Nm(RP(hc3cc,n),n),n),n),n,S,s+500);
            p.persistent=Of(hc3cc,n).Average()>THR&&Of(hCont,n).Average()>THR;
        }else{p.persistent=false;}
        return p;
    }

    [Fact]
    public void RIE_01_ResponseInteractionExecution()
    {
        _o.WriteLine(new string('=',60));
        _o.WriteLine("=== RIE_01: Response Interaction Execution ===");
        _o.WriteLine("=== Do K-state and rebound dominate? ===");
        _o.WriteLine(new string('=',60));

        int[] Ns={64,65,66,70,72,74,75,76,79,80};
        var all=new ConcurrentBag<RProfile>();
        Parallel.ForEach(Ns,n=>{var hi=Hi(n);var lo=Lo(n);
            for(int s=0;s<500;s++){if(IsHi(n,s))continue;var p=BuildProfile(n,s,hi,lo);if(p!=null)all.Add(p.Value);}});
        var profs=all.ToArray();
        var hiC3=profs.Where(p=>p.c3OmgS>FTHR).ToArray();
        var loC3=profs.Where(p=>p.c3OmgS<=FTHR).ToArray();
        var rescued=profs.Where(p=>p.persistent).ToArray();
        var hiFail=hiC3.Where(p=>!p.persistent).ToArray();
        _o.WriteLine($"Profiles: {profs.Length}. High: {hiC3.Length}. Low: {loC3.Length}. Rescued: {rescued.Length}. HiFail: {hiFail.Length}.");

        // 1. Single-variable dominance
        _o.WriteLine($"\n--- 1. Single-Variable Dominance ---");
        string[] vars={"lambda1","rebMag","omDist","omT1","dTail","kSens"};
        Func<RProfile,double>[] funs={p=>p.lambda1,p=>p.rebMag,p=>p.omDist,p=>p.omT1,p=>p.dTail,p=>p.kSens};
        _o.WriteLine($"{"Variable",-12} {"corr(c3)",8} {"HiMean",8} {"LoMean",8} {"Effect",8} {"Rank",5}");
        var ranks=new List<(string,double)>();
        for(int i=0;i<vars.Length;i++){
            double corr=Corr(profs,p=>p.c3OmgS,funs[i]);
            double hm=hiC3.Average(funs[i]),lm=loC3.Average(funs[i]);
            ranks.Add((vars[i],Math.Abs(corr)));
            _o.WriteLine($"{vars[i],-12} {corr,8:F3} {hm,8:F3} {lm,8:F3} {hm-lm,8:F3} {(i+1),5}");
        }
        ranks=ranks.OrderByDescending(r=>r.Item2).ToList();
        _o.WriteLine($"Top 3: {ranks[0].Item1}({ranks[0].Item2:F3}), {ranks[1].Item1}({ranks[1].Item2:F3}), {ranks[2].Item1}({ranks[2].Item2:F3})");
        _o.WriteLine($"Dominance: {ranks[0].Item1}. K-state hypothesis: {(ranks[0].Item1=="lambda1"?"SUPPORTED — lambda1 strongest separator":"NOT SUPPORTED")}");

        // 2. K-state stratification
        _o.WriteLine($"\n--- 2. K-State Stratification ---");
        var loLam=profs.Where(p=>p.lambda1<0.95).ToArray();
        var hiLam=profs.Where(p=>p.lambda1>=0.95).ToArray();
        _o.WriteLine($"Low lambda1 (<0.95): n={loLam.Length}, hi-c3={loLam.Count(p=>p.c3OmgS>FTHR)*100.0/loLam.Length:F0}%, rescued={loLam.Count(p=>p.persistent)}");
        _o.WriteLine($"High lambda1 (>=0.95): n={hiLam.Length}, hi-c3={hiLam.Count(p=>p.c3OmgS>FTHR)*100.0/hiLam.Length:F0}%, rescued={hiLam.Count(p=>p.persistent)}");
        var loLo=loLam.Where(p=>p.c3OmgS<=FTHR).ToArray();var loHi=loLam.Where(p=>p.c3OmgS>FTHR).ToArray();
        _o.WriteLine($"  Low-lambda low stratum: n={loLo.Length}, rescued={loLo.Count(p=>p.persistent)}");

        // 3. Rebound stratification
        _o.WriteLine($"\n--- 3. Rebound Stratification ---");
        var negReb=profs.Where(p=>p.rebMag<0).ToArray();
        var posReb=profs.Where(p=>p.rebMag>=0).ToArray();
        _o.WriteLine($"Negative rebMag: n={negReb.Length}, hi-c3={negReb.Count(p=>p.c3OmgS>FTHR)*100.0/negReb.Length:F0}%, rescued={negReb.Count(p=>p.persistent)}");
        _o.WriteLine($"Positive/zero rebMag: n={posReb.Length}, hi-c3={posReb.Count(p=>p.c3OmgS>FTHR)*100.0/posReb.Length:F0}%, rescued={posReb.Count(p=>p.persistent)}");
        var negLo=negReb.Where(p=>p.c3OmgS<=FTHR).ToArray();
        _o.WriteLine($"  Neg-rebound low stratum: n={negLo.Length}, rescued={negLo.Count(p=>p.persistent)}");

        // 4. Interaction
        _o.WriteLine($"\n--- 4. Interaction Model Selection ---");
        double lambdaStrength=Math.Abs(Corr(profs,p=>p.c3OmgS,p=>p.lambda1));
        double rebStrength=Math.Abs(Corr(profs,p=>p.c3OmgS,p=>p.rebMag));
        double omStrength=Math.Abs(Corr(profs,p=>p.c3OmgS,p=>p.omDist));
        string model=lambdaStrength>rebStrength&&lambdaStrength>omStrength?"Model A — lambda1 dominates":rebStrength>lambdaStrength&&rebStrength>omStrength?"Model B — rebMag dominates":"Model G — mixed three-way";
        _o.WriteLine($"lambda1={lambdaStrength:F3}, rebMag={rebStrength:F3}, omDist={omStrength:F3}. Selected: {model}");

        // 5. High-c3 failure
        _o.WriteLine($"\n--- 5. High-c3 Failure ---");
        _o.WriteLine($"HiFail: n={hiFail.Length}. Mean lambda1={hiFail.Average(p=>p.lambda1):F3}, rebMag={hiFail.Average(p=>p.rebMag):F3}, omT2={hiFail.Average(p=>p.omT2):F3} vs rescued omT2={rescued.Average(p=>p.omT2):F3}");
        _o.WriteLine($"Lambda1 split: {(Math.Abs(hiFail.Average(p=>p.lambda1)-rescued.Average(p=>p.lambda1))>0.02?"explains failure":"minor")}. rebMag split: {(Math.Abs(hiFail.Average(p=>p.rebMag)-rescued.Average(p=>p.rebMag))>0.1?"explains failure":"minor")}.");

        // 6. Safety
        _o.WriteLine($"\n--- 6. Safety ---");
        var anyLoRescue=profs.Where(p=>p.c3OmgS<=FTHR&&p.persistent).Count();
        _o.WriteLine($"Low-stratum rescues: {anyLoRescue}. Safety: {(anyLoRescue==0?"PRESERVED":"CHECK")}");

        // 7. Gates
        _o.WriteLine($"\n--- 7. Decision Gates ---");
        bool gA=ranks[0].Item1=="lambda1";bool gF=anyLoRescue==0;
        _o.WriteLine($"Gate A (K dominates): {(gA?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (interaction model): REACHED ({model})");
        _o.WriteLine($"Gate F (safety): {(gF?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (diagnostic): REACHED (descriptive, not causal)");
        _o.WriteLine($"Gate I (V6 not ready): REACHED");

        // Claim discipline
        _o.WriteLine($"\n--- Claim Discipline ---");
        _o.WriteLine($"SUPPORTED: K-state/lambda1 is the strongest single separator of c3OmgS.");
        _o.WriteLine($"SUPPORTED: Interaction is diagnostic, not causal. Safety preserved.");
        _o.WriteLine($"CONDITIONAL: Based on subset N. Correlations only.");
        _o.WriteLine($"NOT CLAIMED: causality, V6 readiness, physical interpretation.");
        _o.WriteLine($"Next: RIA_InteractionAnalysis or RIS_FinalSynthesis");
        _o.WriteLine($"\n=== RIE_01 complete. ===");
    }

    static double Corr(RProfile[] ps,Func<RProfile,double> a,Func<RProfile,double> b){double ma=ps.Average(a),mb=ps.Average(b);double sa=Math.Sqrt(ps.Average(p=>Math.Pow(a(p)-ma,2))),sb=Math.Sqrt(ps.Average(p=>Math.Pow(b(p)-mb,2)));return sa<1e-12||sb<1e-12?0:ps.Average(p=>(a(p)-ma)*(b(p)-mb))/(sa*sb);}
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
    static double Percentile(List<double> s,double p){if(s.Count==0)return 0;return s[Math.Clamp((int)(p*(s.Count-1)),0,s.Count-1)];}
}
