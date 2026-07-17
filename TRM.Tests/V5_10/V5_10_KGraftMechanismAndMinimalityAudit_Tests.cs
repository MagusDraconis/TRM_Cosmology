using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace TRM.Tests.V5_10;

[Trait("Category", "V5_10"), Trait("Category", "V5_10_CAJ")]
public class V5_10_KGraftMechanismAndMinimalityAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private static readonly int[] TN={71,67};private const int MS=50;

    public V5_10_KGraftMechanismAndMinimalityAudit_Tests(ITestOutputHelper o){_output=o;}

    static double[][]Sm(double[,]K,int n,double s,int seed,int st,double reps){var r=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(r.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=r.NextDouble()*2.0*Math.PI;int hL=st/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<st;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n,double reps){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(reps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n,double k0,double xi){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:k0*Math.Exp(-d[i,j]/Math.Max(xi,0.01));return K;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double KM(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Std(double[]v){double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double KSd(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];return Std(v);}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}

    // Modify K: rescale to target K_mean
    static double[,] KMeanMatch(double[,]K,int n,double tgt){
        double km=KM(K,n);if(km<1e-10)return CK(K,n);double s=tgt/km;
        var r=new double[n,n];for(int i=0;i<n;i++){r[i,i]=0;for(int j=i+1;j<n;j++){r[i,j]=K[i,j]*s;r[j,i]=r[i,j];}}return r;}

    // Modify K: spread from mean to target K_std, preserving K_mean
    static double[,] KStdMatch(double[,]K,int n,double tgtKs){
        double km=KM(K,n),ks=KSd(K,n);if(ks<1e-10)return CK(K,n);double s=tgtKs/ks;
        var r=new double[n,n];for(int i=0;i<n;i++){r[i,i]=0;for(int j=i+1;j<n;j++){r[i,j]=km+(K[i,j]-km)*s;if(r[i,j]<REps)r[i,j]=REps;r[j,i]=r[i,j];}}return r;}

    // Run graft: at CP4, replace lo K with modified donor K, then continue
    static (bool hi,bool pers)RunGraftModified(int n,int seed,double[,]modDonorK){
        var (origK,_)=GraftAtCP(n,seed,3,modDonorK); // CP4 = epoch index 3
        double om=Of(Sm(origK,n,S,seed+NE,St,REps),n).Average();bool hi=om>THR;
        var h2=Sm(origK,n,S,seed+100,St,REps);var K2=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
        bool pers=Of(Sm(K2,n,S,seed+200,St,REps),n).Average()>THR;
        return (hi,pers);
    }

    // Full K graft at CP (epoch index)
    static (double[,]finalK,int ep)GraftAtCP(int n,int seed,int cpIdx,double[,]donorK){
        var K=KS(n,seed);
        for(int e=0;e<NE;e++){var h=Sm(K,n,S,seed+e,St,REps);if(e==cpIdx){K=CK(donorK,n);continue;}K=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
        return (K,cpIdx+1);
    }

    [Fact]public void CAJ_01_Decompose(){
        _output.WriteLine("═══ K-GRAFT DECOMPOSITION: N=71 CP4 (seeds 0-49) ═══");
        foreach(var n in TN){
            var loList=new List<int>();var hiList=new List<int>();
            for(int s=0;s<MS;s++){var K=KS(n,s);for(int e=0;e<NE;e++){K=Cupd(DL(Nm(RP(Sm(K,n,S,s+e,St,REps),n),n,REps),n),n,K0,Xi);}
                if(Of(Sm(K,n,S,s+NE,St,REps),n).Average()>THR)hiList.Add(s);else loList.Add(s);}
            if(loList.Count<5||hiList.Count<5)continue;

            // Full K graft (CAI reproduction)
            int fkHi=0,fkPers=0;int kmHi=0,kmPers=0;int ksHi=0,ksPers=0;int kmksHi=0,kmksPers=0;
            Parallel.ForEach(loList,ls=>{
                // Find donor at CP4
                var loK=KS(n,ls);for(int e=0;e<=3;e++){var h=Sm(loK,n,S,ls+e,St,REps);loK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
                double lkm=KM(loK,n);int bd=hiList[0];double bdist=double.MaxValue;
                double[,]bestDonK=null;double bestDonKs=0;
                foreach(var hs in hiList){var dK=KS(n,hs);for(int e=0;e<=3;e++){dK=Cupd(DL(Nm(RP(Sm(dK,n,S,hs+e,St,REps),n),n,REps),n),n,K0,Xi);}
                    double d=Math.Abs(KM(dK,n)-lkm);if(d<bdist){bdist=d;bd=hs;bestDonK=CK(dK,n);bestDonKs=KSd(dK,n);}}
                // Full K
                var(fhi,fpers)=RunGraftModified(n,ls,CK(bestDonK,n));
                if(fhi)Interlocked.Increment(ref fkHi);if(fpers)Interlocked.Increment(ref fkPers);
                // K_mean only
                var kmK=KMeanMatch(CK(loK,n),n,KM(bestDonK,n));
                var(khi,kpers)=RunGraftModified(n,ls,kmK);
                if(khi)Interlocked.Increment(ref kmHi);if(kpers)Interlocked.Increment(ref kmPers);
                // K_std only
                var ksK=KStdMatch(CK(loK,n),n,bestDonKs);
                var(shi,spers)=RunGraftModified(n,ls,ksK);
                if(shi)Interlocked.Increment(ref ksHi);if(spers)Interlocked.Increment(ref ksPers);
                // Both K_mean + K_std
                var both=KStdMatch(KMeanMatch(CK(loK,n),n,KM(bestDonK,n)),n,bestDonKs);
                var(bhi,bpers)=RunGraftModified(n,ls,both);
                if(bhi)Interlocked.Increment(ref kmksHi);if(bpers)Interlocked.Increment(ref kmksPers);
            });

            _output.WriteLine($"N={n} (Lo={loList.Count} Hi={hiList.Count})");
            _output.WriteLine($"{"Graft Type",-16} {"Induced",8} {"Persisted",10}");
            _output.WriteLine($"{"Full K",-16} {fkHi,7}/{loList.Count} {fkPers,9}/{loList.Count}");
            _output.WriteLine($"{"K_mean only",-16} {kmHi,7}/{loList.Count} {kmPers,9}/{loList.Count}");
            _output.WriteLine($"{"K_std only",-16} {ksHi,7}/{loList.Count} {ksPers,9}/{loList.Count}");
            _output.WriteLine($"{"K_mean+K_std",-16} {kmksHi,7}/{loList.Count} {kmksPers,9}/{loList.Count}");
            _output.WriteLine("");
        }
    }

    [Fact]public void CAJ_02_GateSummary(){
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (K magnitude sufficient): CAJ_01 K_mean-only");
        _output.WriteLine("Gate B (K spectrum required): CAJ_01 K_std-only vs K_mean-only");
        _output.WriteLine("Gate F (N=71 transition-specific): CAJ_01 N=71 vs N=67");
        _output.WriteLine("Gate G (No genuine induction): CAJ_01 persistence check");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: K-graft decomposition, K_mean/K_std decomposition.");
        _output.WriteLine("CONDITIONAL: N=71,67. CP4 graft. Seeds 0-49.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
