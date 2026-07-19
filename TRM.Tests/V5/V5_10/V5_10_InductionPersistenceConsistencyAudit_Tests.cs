using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace TRM.Tests.V5_10;

[Trait("Category", "V5_10"), Trait("Category", "V5_10_CAK")]
public class V5_10_InductionPersistenceConsistencyAudit_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private const int MS=50;

    public V5_10_InductionPersistenceConsistencyAudit_Tests(ITestOutputHelper o){_o=o;}

    static double[][]Sm(double[,]K,int n,double s,int seed,int st,double reps){var r=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(r.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=r.NextDouble()*2.0*Math.PI;int hL=st/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<st;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n,double reps){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(reps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n,double k0,double xi){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:k0*Math.Exp(-d[i,j]/Math.Max(xi,0.01));return K;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double KM(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double KSd(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];return Std(v);}
    static double Std(double[]v){double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}
    static double[,]KMeanMatch(double[,]K,int n,double tgt){double km=KM(K,n);if(km<1e-10)return CK(K,n);double s=tgt/km;var r=new double[n,n];for(int i=0;i<n;i++){r[i,i]=0;for(int j=i+1;j<n;j++){r[i,j]=K[i,j]*s;r[j,i]=r[i,j];}}return r;}
    static double[,]KStdMatch(double[,]K,int n,double tgtKs){double km=KM(K,n),ks=KSd(K,n);if(ks<1e-10)return CK(K,n);double s=tgtKs/ks;var r=new double[n,n];for(int i=0;i<n;i++){r[i,i]=0;for(int j=i+1;j<n;j++){r[i,j]=km+(K[i,j]-km)*s;if(r[i,j]<REps)r[i,j]=REps;r[j,i]=r[i,j];}}return r;}

    // Returns: (immediateHi, immediateOm, persistHi, persistOm, delayedHi)
    // immediateHi = post-graft Omega > THR
    // persistHi   = immediateHi AND +1 epoch Omega > THR
    // delayedHi   = NOT immediateHi AND +1 epoch Omega > THR
    static (bool imm,double iOm,bool pers,double pOm,bool delayed)RunGraftDiag(int n,int seed,double[,]modDonorK){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sm(K,n,S,seed+e,St,REps);if(e==3){K=CK(modDonorK,n);continue;}K=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
        double iOm=Of(Sm(K,n,S,seed+NE,St,REps),n).Average();bool imm=iOm>THR;
        var h2=Sm(K,n,S,seed+100,St,REps);var K2=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
        double pOm=Of(Sm(K2,n,S,seed+200,St,REps),n).Average();bool pers=imm&&pOm>THR;
        bool delayed=!imm&&pOm>THR;
        return (imm,iOm,pers,pOm,delayed);
    }

    [Fact]public void CAK_01_PersistenceAudit(){
        _o.WriteLine("═══ PERSISTENCE AUDIT: N=71 CP4 (seeds 0-49) ═══");
        int n=71;
        var loList=new List<int>();var hiList=new List<int>();
        for(int s=0;s<MS;s++){var K=KS(n,s);for(int e=0;e<NE;e++){K=Cupd(DL(Nm(RP(Sm(K,n,S,s+e,St,REps),n),n,REps),n),n,K0,Xi);}
            if(Of(Sm(K,n,S,s+NE,St,REps),n).Average()>THR)hiList.Add(s);else loList.Add(s);}

        int fkImm=0,fkPers=0,fkDelayed=0;int kmImm=0,kmPers=0,kmDelayed=0;int ksImm=0,ksPers=0,ksDelayed=0;int kkImm=0,kkPers=0,kkDelayed=0;

        foreach(var ls in loList){
            var loK=KS(n,ls);for(int e=0;e<=3;e++){var h=Sm(loK,n,S,ls+e,St,REps);loK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
            double lkm=KM(loK,n);int bd=hiList[0];double bdist=double.MaxValue;double[,]bestDonK=null;double bestDonKs=0;
            foreach(var hs in hiList){var dK=KS(n,hs);for(int e=0;e<=3;e++){dK=Cupd(DL(Nm(RP(Sm(dK,n,S,hs+e,St,REps),n),n,REps),n),n,K0,Xi);}
                double d=Math.Abs(KM(dK,n)-lkm);if(d<bdist){bdist=d;bd=hs;bestDonK=CK(dK,n);bestDonKs=KSd(dK,n);}}

            // Full K
            var(fi,_,fp,_,fd)=RunGraftDiag(n,ls,CK(bestDonK,n));
            if(fi)fkImm++;if(fp)fkPers++;if(fd)fkDelayed++;
            // K_mean only
            var(ki,_,kp,_,kd)=RunGraftDiag(n,ls,KMeanMatch(CK(loK,n),n,KM(bestDonK,n)));
            if(ki)kmImm++;if(kp)kmPers++;if(kd)kmDelayed++;
            // K_std only
            var(si,_,sp,_,sd)=RunGraftDiag(n,ls,KStdMatch(CK(loK,n),n,bestDonKs));
            if(si)ksImm++;if(sp)ksPers++;if(sd)ksDelayed++;
            // Both
            var(bi,_,bp,_,bdel)=RunGraftDiag(n,ls,KStdMatch(KMeanMatch(CK(loK,n),n,KM(bestDonK,n)),n,bestDonKs));
            if(bi)kkImm++;if(bp)kkPers++;if(bdel)kkDelayed++;
        }

        _o.WriteLine($"N=71 (Lo={loList.Count})");
        _o.WriteLine($"{"Graft Type",-16} {"Imm Induced",12} {"Strict Pers",12} {"Delayed Ind",12} {"Cont Total",12}");
        _o.WriteLine($"{"Full K",-16} {fkImm,11}/{loList.Count} {fkPers,11}/{loList.Count} {fkDelayed,11}/{loList.Count} {fkPers+fkDelayed,11}/{loList.Count}");
        _o.WriteLine($"{"K_mean only",-16} {kmImm,11}/{loList.Count} {kmPers,11}/{loList.Count} {kmDelayed,11}/{loList.Count} {kmPers+kmDelayed,11}/{loList.Count}");
        _o.WriteLine($"{"K_std only",-16} {ksImm,11}/{loList.Count} {ksPers,11}/{loList.Count} {ksDelayed,11}/{loList.Count} {ksPers+ksDelayed,11}/{loList.Count}");
        _o.WriteLine($"{"K_mean+K_std",-16} {kkImm,11}/{loList.Count} {kkPers,11}/{loList.Count} {kkDelayed,11}/{loList.Count} {kkPers+kkDelayed,11}/{loList.Count}");

        _o.WriteLine("");
        _o.WriteLine("── CAJ RECONCILIATION ──");
        _o.WriteLine($"CAJ Full K:     induced=14/34 persisted=10/34");
        _o.WriteLine($"CAK Full K:     imm={fkImm}/{loList.Count} strict={fkPers}/{loList.Count} delayed={fkDelayed}/{loList.Count}");
        _o.WriteLine($"CAJ K_mean:     induced=6/34  persisted=12/34");
        _o.WriteLine($"CAK K_mean:     imm={kmImm}/{loList.Count} strict={kmPers}/{loList.Count} delayed={kmDelayed}/{loList.Count}");
        _o.WriteLine($"CAJ K_std:      induced=5/34  persisted=13/34");
        _o.WriteLine($"CAK K_std:      imm={ksImm}/{loList.Count} strict={ksPers}/{loList.Count} delayed={ksDelayed}/{loList.Count}");
        _o.WriteLine("");
        string verdict = kmDelayed > kmImm ? "K_mean DELAYED INDUCTION dominates — CAJ 'persisted' counted all continuation-high, not strict persistence" : "CAJ persistence semantics are consistent";
        _o.WriteLine(verdict);
    }

    [Fact]public void CAK_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (CAJ semantics valid): CAK_01 strict persistence matches CAJ persisted?");
        _o.WriteLine("Gate B (Continuation-high total): CAK_01 delayed vs strict");
        _o.WriteLine("Gate D (Full K still dominates strict): CAK_01");
        _o.WriteLine("Gate E (Scalar K = delayed induction): CAK_01 K_mean/K_std delayed");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: Per-seed tracking, strict/delayed persistence.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
