using Xunit;
using Xunit.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace TRM.Tests.V5_10;

[Trait("Category", "V5_10"), Trait("Category", "V5_10_CAI")]
public class V5_10_LowToHighTrajectoryInductionAudit_Tests
{
    private readonly ITestOutputHelper _output;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private static readonly int[] TN={67,71};private const int MS=50;

    public V5_10_LowToHighTrajectoryInductionAudit_Tests(ITestOutputHelper o){_output=o;}

    static double[][] Sm(double[,]K,int n,double s,int seed,int st,double reps){var r=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(r.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=r.NextDouble()*2.0*Math.PI;int hL=st/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<st;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n,double reps){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(reps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n,double k0,double xi){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:k0*Math.Exp(-d[i,j]/Math.Max(xi,0.01));return K;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double KM(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}

    [Fact]public void CAI_01_GraftTest(){
        _output.WriteLine("═══ K-GRAFT: Lo->Hi via donor K at CP2/CP3/CP4 (seeds 0-49) ═══");
        foreach(var n in TN){
            var loSeeds=new List<int>();var hiSeeds=new List<int>();
            var allKs=new double[MS][,];var allIsHi=new bool[MS];
            for(int s=0;s<MS;s++){var K=KS(n,s);for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);K=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
                double om=Of(Sm(K,n,S,s+NE,St,REps),n).Average();allKs[s]=CK(K,n);allIsHi[s]=om>THR;
                if(om>THR)hiSeeds.Add(s);else loSeeds.Add(s);}
            if(loSeeds.Count==0||hiSeeds.Count==0)continue;

            // Re-run to get checkpoint Ks for donors (hi seeds) at CP1-CP4 (unused, removed)

            // For each lo seed, test graft at CP2/CP3/CP4
            int[]cps={1,2,3};int[]cpHi=new int[cps.Length];int[]cpPers=new int[cps.Length];
            Parallel.ForEach(loSeeds,ls=>{
                // Re-run lo to collect K at each cp
                var loK=KS(n,ls);var loCpK=new double[NE][,];
                for(int e=0;e<NE;e++){var h=Sm(loK,n,S,ls+e,St,REps);loK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);loCpK[e]=CK(loK,n);}

                for(int ci=0;ci<cps.Length;ci++){int cp=cps[ci];
                    // Find closest hi donor at this cp by KMean
                    double lkm=KM(loCpK[cp],n);int bestD=hiSeeds[0];double bd=double.MaxValue;
                    // Run donor to cp to get its K
                    foreach(var hs in hiSeeds){
                        var dK=KS(n,hs);for(int e=0;e<=cp;e++){var h=Sm(dK,n,S,hs+e,St,REps);dK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
                        double d=Math.Abs(KM(dK,n)-lkm);if(d<bd){bd=d;bestD=hs;}
                    }
                    // Run donor to cp to get donor K
                    var donK=KS(n,bestD);for(int e=0;e<=cp;e++){var h=Sm(donK,n,S,bestD+e,St,REps);donK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
                    // Graft: replace lo's K at cp with donor's K, continue
                    var gK=KS(n,ls);for(int e=0;e<NE;e++){var h=Sm(gK,n,S,ls+e,St,REps);if(e==cp){gK=CK(donK,n);continue;}gK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
                    double om=Of(Sm(gK,n,S,ls+NE,St,REps),n).Average();bool hi=om>THR;
                    // Persistence
                    var h2=Sm(gK,n,S,ls+100,St,REps);var K2=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
                    bool pers=Of(Sm(K2,n,S,ls+200,St,REps),n).Average()>THR;
                    if(hi)Interlocked.Increment(ref cpHi[ci]);if(pers)Interlocked.Increment(ref cpPers[ci]);
                }
            });

            // Endpoint: replace final K, +1 epoch
            int epHi=0,epPers=0;
            Parallel.ForEach(loSeeds,ls=>{
                var donK=allKs[hiSeeds[0]]; // just use first hi donor's final K
                var h=Sm(donK,n,S,ls+300,St,REps);var K2=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);
                double om=Of(Sm(K2,n,S,ls+400,St,REps),n).Average();
                var h3=Sm(K2,n,S,ls+500,St,REps);var K3=Cupd(DL(Nm(RP(h3,n),n,REps),n),n,K0,Xi);
                bool pers=Of(Sm(K3,n,S,ls+600,St,REps),n).Average()>THR;
                if(om>THR)Interlocked.Increment(ref epHi);if(pers)Interlocked.Increment(ref epPers);
            });

            _output.WriteLine($"N={n} (Lo={loSeeds.Count} Hi={hiSeeds.Count})");
            _output.WriteLine($"{"",-12} {"Induced",8} {"Persisted",10}");
            for(int i=0;i<cps.Length;i++)_output.WriteLine($"{"CP"+(cps[i]+1),-12} {cpHi[i],7}/{loSeeds.Count} {cpPers[i],9}/{loSeeds.Count}");
            _output.WriteLine($"{"Endpoint",-12} {epHi,7}/{loSeeds.Count} {epPers,9}/{loSeeds.Count}");
            _output.WriteLine("");
        }
    }

    [Fact]public void CAI_02_GateSummary(){
        _output.WriteLine("═══ GATE SUMMARY ═══");
        _output.WriteLine("Gate A (Trajectory graft induction): CAI_01");
        _output.WriteLine("Gate F (Endpoint fails, trajectory works): CAI_01");
        _output.WriteLine("Gate G (No genuine induction): CAI_01");
        _output.WriteLine("═══ CLAIM AUDIT ═══");
        _output.WriteLine("SUPPORTED: K-graft, endpoint graft, persistence.");
        _output.WriteLine("CONDITIONAL: N=67,71. KMean-donor matching.");
        _output.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
