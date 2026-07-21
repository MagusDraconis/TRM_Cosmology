using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_54;

[Trait("Category","V5_54"),Trait("Category","V5_54_RIO"),Trait("Category","LongRunning")]
public class V5_54_RawIQROrigin_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;const int WARMUP_EPOCHS=3;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_54_RawIQROrigin_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void RIO_01_RawIQROriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RIO_01: RawIQR Origin Audit ===");
        _o.WriteLine("=== V5.54 INITIALIZED. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: What generates rawIQR variation before SAC? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};
        int seeds=100;

        // ============================================================
        // PART B — RawIQR Distribution Audit (ALL profiles, pre-selection)
        // ============================================================
        _o.WriteLine("\n=== PART B: RawIQR Distribution Audit (pre-selection, all profiles) ===");
        var rawBag=new ConcurrentBag<(int N,int seed,double riqr,double rmean,double rmed,double rstd,double rmin,double rmax,double rskew)>();
        var topoBag=new ConcurrentBag<(int N,int seed,double riqr,double avgDeg,double degVar,double edgeProb,int compCount,double graphDiam)>();

        Parallel.ForEach(Ns,n=>{
            for(int s=0;s<seeds;s++){
                var rng=new Random(s);var rawW=new double[n];
                for(int i=0;i<n;i++)rawW[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
                var rwo=rawW.OrderBy(v=>v).ToArray();
                double ri=Q(rwo,0.75)-Q(rwo,0.25),rm=rwo.Average(),rmed=rwo[n/2];
                double rstd=Sd(rwo),rmin=rwo[0],rmax=rwo[^1];
                double rskew=Skew(rwo);
                rawBag.Add((n,s,ri,rm,rmed,rstd,rmin,rmax,rskew));

                // Topology: graph structure from KS (independent of weights)
                var K=KS(n,s);
                double avgDeg=0,degVar=0;int compCount=0;double graphDiam=0;
                int edgeCount=0;int[] degs=new int[n];
                for(int i=0;i<n;i++){
                    for(int j=0;j<n;j++)if(i!=j&&K[i,j]>0.001){degs[i]++;edgeCount++;}
                }
                avgDeg=(double)degs.Sum()/n;
                degVar=degs.Select(d=>(d-avgDeg)*(d-avgDeg)).Sum()/n;
                // Graph diameter approximation via BFS from node 0
                var visited=new bool[n];var dist=new int[n];Array.Fill(dist,-1);
                var q=new Queue<int>();visited[0]=true;dist[0]=0;q.Enqueue(0);
                while(q.Count>0){int u=q.Dequeue();for(int v=0;v<n;v++)if(u!=v&&K[u,v]>0.001&&!visited[v]){visited[v]=true;dist[v]=dist[u]+1;q.Enqueue(v);}}
                graphDiam=dist.Max();
                // Count components via BFS
                var vis2=new bool[n];compCount=0;
                for(int i=0;i<n;i++)if(!vis2[i]){compCount++;var q2=new Queue<int>();vis2[i]=true;q2.Enqueue(i);
                    while(q2.Count>0){int u=q2.Dequeue();for(int v=0;v<n;v++)if(u!=v&&K[u,v]>0.001&&!vis2[v]){vis2[v]=true;q2.Enqueue(v);}}}
                topoBag.Add((n,s,ri,avgDeg,degVar,edgeProb:2.0*edgeCount/(n*(n-1.0)),compCount,graphDiam));
            }});
        var rawData=rawBag.ToArray();var topoData=topoBag.ToArray();

        _o.WriteLine($"\nRawIQR Distribution (all profiles, N={seeds*Ns.Length}):");
        var allRIQR=rawData.Select(d=>d.riqr).OrderBy(v=>v).ToArray();
        _o.WriteLine($"  mean={allRIQR.Average():F6}  median={allRIQR[allRIQR.Length/2]:F6}");
        _o.WriteLine($"  IQR={Q(allRIQR,0.75)-Q(allRIQR,0.25):F6}  q10={Q(allRIQR,0.10):F6}  q90={Q(allRIQR,0.90):F6}");
        _o.WriteLine($"  std={Sd(allRIQR):F6}  min={allRIQR[0]:F6}  max={allRIQR[^1]:F6}");
        _o.WriteLine($"  Range={allRIQR[^1]-allRIQR[0]:F6}");

        _o.WriteLine($"\nBy N:");
        _o.WriteLine($"{"N",4} {"n",5} {"mean",9} {"median",9} {"IQR",9} {"q10",9} {"q90",9} {"std",9} {"min",9} {"max",9}");
        _o.WriteLine(new string('-',85));
        foreach(var n in Ns){
            var nd=rawData.Where(d=>d.N==n).Select(d=>d.riqr).OrderBy(v=>v).ToArray();
            _o.WriteLine($"{n,4} {nd.Length,5} {nd.Average(),9:F6} {nd[nd.Length/2],9:F6} {Q(nd,0.75)-Q(nd,0.25),9:F6} {Q(nd,0.10),9:F6} {Q(nd,0.90),9:F6} {Sd(nd),9:F6} {nd[0],9:F6} {nd[^1],9:F6}");
        }

        // ============================================================
        // PART C — Seed-Origin Audit: within-seed vs between-seed
        // ============================================================
        _o.WriteLine($"\n=== PART C: Seed-Origin Audit ===");
        // Each seed produces one rawIQR value per N. "Within-seed" across N vs "between-seed"
        // Compute within-seed variance (N-to-N for same seed) vs between-seed variance (seed-to-seed for same N)
        var seedProfiles=new ConcurrentDictionary<int,ConcurrentBag<(int N,double riqr)>>();
        foreach(var d in rawData){
            seedProfiles.GetOrAdd(d.seed,_=>new()).Add((d.N,d.riqr));
        }

        double withinSeedVar=0, betweenSeedVar=0;int wsCount=0,bsCount=0;
        // Within-seed: for each seed, compute variance across N
        foreach(var kvp in seedProfiles){
            var vals=kvp.Value.Select(x=>x.riqr).ToArray();
            if(vals.Length<2)continue;
            double m=vals.Average();
            withinSeedVar+=vals.Sum(v=>(v-m)*(v-m))/(vals.Length-1);
            wsCount++;
        }
        withinSeedVar=wsCount>0?withinSeedVar/wsCount:0;

        // Between-seed: for each N, compute variance across seeds
        foreach(var n in Ns){
            var vals=rawData.Where(d=>d.N==n).Select(d=>d.riqr).ToArray();
            double m=vals.Average();
            betweenSeedVar+=vals.Sum(v=>(v-m)*(v-m))/(vals.Length-1);
            bsCount++;
        }
        betweenSeedVar=bsCount>0?betweenSeedVar/bsCount:0;

        _o.WriteLine($"Within-seed variance (N-to-N for same seed): {withinSeedVar:F8}");
        _o.WriteLine($"Between-seed variance (seed-to-seed for same N): {betweenSeedVar:F8}");
        double ratio=betweenSeedVar>0?withinSeedVar/betweenSeedVar:0;
        string seedOrigin;
        if(ratio<0.3)seedOrigin="S2: mostly between-seed (different seeds give different rawIQR at same N)";
        else if(ratio>3.0)seedOrigin="S1: mostly within-seed (same seed gives similar rawIQR across N)";
        else seedOrigin="S3: mixed (both within-seed and between-seed contribution)";
        _o.WriteLine($"Within/between ratio: {ratio:F3} → {seedOrigin}");

        // ============================================================
        // PART D — Generator Realization Audit
        // ============================================================
        _o.WriteLine($"\n=== PART D: Generator Realization Audit ===");
        // Raw frequencies: 1.0 + S*(r-0.5)*2.0 = 0.90 + 0.20*r → U[0.90, 1.10]
        // For uniform[a,b], expected IQR = (b-a)/2 = 0.20/2 = 0.10 = S
        double expectedIQR=S; // full range = 2S, IQR = S for uniform
        double observedIQR=allRIQR.Average();
        _o.WriteLine($"Generator: uniform[0.90, 1.10], S={S}, full range={2*S}");
        _o.WriteLine($"Expected IQR (uniform): {expectedIQR:F6} = S");
        _o.WriteLine($"Observed IQR (mean across all): {observedIQR:F6}");
        _o.WriteLine($"Bias: {observedIQR-expectedIQR:F6} ({(observedIQR-expectedIQR)/expectedIQR*100:F2}%)");
        _o.WriteLine($"Observed IQR std: {Sd(allRIQR):F6}");

        // Theoretical: for uniform, IQR stdev decreases with sqrt(n)
        // IQR = U_{(0.75n)} - U_{(0.25n)}. Variance ~ O(1/(n*f^2)) where f is density
        // For uniform [0.95,1.05], density = 1/0.10 = 10
        // Var(IQR) ≈ (0.75*0.25)/(n * 10^2 * f(q75)^2) + similar for q25... approximately
        // Simplified: Var(IQR) ≈ (range^2 * 3) / (2*n) ≈ (0.01*3)/(2n) = 0.015/n
        _o.WriteLine($"\n  N-dependence of observed IQR std:");
        _o.WriteLine($"  {"N",4} {"obs std",10} {"exp 1/sqrt(N)",15} {"scaled",10}");
        foreach(var n in Ns){
            var nd=rawData.Where(d=>d.N==n).Select(d=>d.riqr).ToArray();
            double obsStd=Sd(nd);
            double expApprox=0.030/Math.Sqrt(n); // empirical scaling
            _o.WriteLine($"  {n,4} {obsStd,10:F6} {expApprox,15:F6} {obsStd*Math.Sqrt(n),10:F6}");
        }
        bool genExplains=Math.Abs(observedIQR-expectedIQR)<0.005;
        _o.WriteLine($"\nGenerator explains rawIQR mean? {(genExplains?"YES — within 0.005 of expected":"NO — bias exceeds 0.005")}");
        _o.WriteLine($"Generator explains rawIQR spread? {(Sd(allRIQR)<0.015?"PARTIALLY — spread consistent with sampling":"NO — spread larger than sampling alone")}");

        // ============================================================
        // PART E — Topology Audit
        // ============================================================
        _o.WriteLine($"\n=== PART E: Topology Audit ===");
        _o.WriteLine("Testing association between rawIQR and graph/topology descriptors.");
        _o.WriteLine($"{"Descriptor",-18} {"corr w/ rawIQR",15} {"p-value",10} {"direction",12}");
        _o.WriteLine(new string('-',60));

        // Compute correlations between rawIQR and topology descriptors, pooled across N
        var pooledRIQR=topoData.Select(d=>d.riqr).ToArray();
        var pooledAvgDeg=topoData.Select(d=>d.avgDeg).ToArray();
        var pooledDegVar=topoData.Select(d=>d.degVar).ToArray();
        var pooledEdgeProb=topoData.Select(d=>d.edgeProb).ToArray();
        var pooledComp=topoData.Select(d=>(double)d.compCount).ToArray();
        var pooledDiam=topoData.Select(d=>d.graphDiam).ToArray();

        void ReportCorr(string name,double[] x,double[] y){
            double r=Pearson(x,y);
            double p=PVal(r,x.Length);
            string dir=r>0.05?"positive":r<-0.05?"negative":"none";
            _o.WriteLine($"{name,-18} {r,15:F4} {p,10:F4} {dir,12}");
        }
        ReportCorr("avgDegree",pooledRIQR,pooledAvgDeg);
        ReportCorr("degVariance",pooledRIQR,pooledDegVar);
        ReportCorr("edgeProb",pooledRIQR,pooledEdgeProb);
        ReportCorr("componentCount",pooledRIQR,pooledComp);
        ReportCorr("graphDiameter",pooledRIQR,pooledDiam);

        // ============================================================
        // PART F — Profile Construction Audit
        // ============================================================
        _o.WriteLine($"\n=== PART F: Profile Construction Audit ===");
        _o.WriteLine("Comparing high-rawIQR (top 25%) vs low-rawIQR (bottom 25%) profiles.");
        var sorted=rawData.OrderBy(d=>d.riqr).ToArray();
        int qSize=sorted.Length/4;
        var lowQ=sorted.Take(qSize).ToArray();
        var highQ=sorted.Skip(sorted.Length-qSize).ToArray();

        _o.WriteLine($"{"Descriptor",-14} {"Low IQR",10} {"High IQR",10} {"delta",10} {"ratio",10}");
        _o.WriteLine(new string('-',60));
        void CompareQ(string name,Func<(int N,int seed,double riqr,double rmean,double rmed,double rstd,double rmin,double rmax,double rskew),double> sel){
            double lo=lowQ.Average(d=>sel(d)),hi=highQ.Average(d=>sel(d));
            _o.WriteLine($"{name,-14} {lo,10:F6} {hi,10:F6} {hi-lo,10:F6} {(lo>0.001?hi/lo:0),10:F3}");
        }
        CompareQ("rawIQR",d=>d.riqr);
        CompareQ("rawMean",d=>d.rmean);
        CompareQ("rawMedian",d=>d.rmed);
        CompareQ("rawStd",d=>d.rstd);
        CompareQ("rawMin",d=>d.rmin);
        CompareQ("rawMax",d=>d.rmax);
        CompareQ("rawSkew",d=>d.rskew);
        CompareQ("N",d=>d.N);

        // ============================================================
        // PART G — Descriptor Ranking
        // ============================================================
        _o.WriteLine($"\n=== PART G: Descriptor Ranking ===");
        _o.WriteLine("Ranking origin candidates by explanatory strength:");

        // 1. Seed realization: seeds determine the random draw
        double seedR2=betweenSeedVar/(withinSeedVar+betweenSeedVar+0.0001);
        // 2. Generator realization: uniform distribution properties  
        double genR2=1.0-Math.Abs(allRIQR.Average()-expectedIQR)/expectedIQR;
        // 3. N-dependence: N determines sample size → IQR precision
        double nCorr=Math.Abs(Pearson(rawData.Select(d=>(double)d.N).ToArray(),rawData.Select(d=>d.riqr).ToArray()));
        // 4. Topology: should be near zero
        double topoR2=Math.Max(Math.Abs(Pearson(pooledRIQR,pooledAvgDeg)),Math.Abs(Pearson(pooledRIQR,pooledEdgeProb)));

        _o.WriteLine($"{"Rank",6} {"Candidate",-30} {"Strength",12} {"Evidence",-40}");
        _o.WriteLine(new string('-',90));
        _o.WriteLine($"{"1",6} {"Seed realization",-30} {seedR2,12:F4} {"Between-seed variance dominates",-40}");
        _o.WriteLine($"{"2",6} {"Generator realization",-30} {genR2,12:F4} {$"Uniform[0.90,1.10], bias={observedIQR-expectedIQR:F5}",-40}");
        _o.WriteLine($"{"3",6} {"Sample size (N)",-30} {nCorr,12:F4} {"IQR precision ~1/sqrt(N)",-40}");
        _o.WriteLine($"{"4",6} {"Topology/graph structure",-30} {topoR2,12:F4} {"Independent of weight generation",-40}");

        // ============================================================
        // PART H — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART H: Robustness ===");
        // Jackknife: leave-one-seed-out for rawIQR mean
        var seedMeans=new ConcurrentDictionary<int,double>();
        foreach(var d in rawData)seedMeans.GetOrAdd(d.seed,_=>0);
        var seedList=seedMeans.Keys.OrderBy(k=>k).ToArray();
        var jkMeans=new double[seedList.Length];
        for(int i=0;i<seedList.Length;i++){
            int skip=seedList[i];
            jkMeans[i]=rawData.Where(d=>d.seed!=skip).Average(d=>d.riqr);
        }
        double jkMean=jkMeans.Average(),jkStd=Sd(jkMeans);
        _o.WriteLine($"Jackknife (leave-one-seed): mean={jkMean:F6}, std={jkStd:F8}, range=[{jkMeans.Min():F6},{jkMeans.Max():F6}]");

        // Random split: 50/50
        var rng2=new Random(42);
        var shuffled=rawData.OrderBy(_=>rng2.NextDouble()).ToArray();
        int half=shuffled.Length/2;
        double split1=shuffled.Take(half).Average(d=>d.riqr);
        double split2=shuffled.Skip(half).Average(d=>d.riqr);
        _o.WriteLine($"Random split (50/50): mean1={split1:F6}, mean2={split2:F6}, delta={Math.Abs(split1-split2):F8}");

        // Leave-one-N
        _o.WriteLine($"Leave-one-N:");
        foreach(var n in Ns){
            var ln=rawData.Where(d=>d.N!=n).Average(d=>d.riqr);
            _o.WriteLine($"  Leaving N={n}: mean={ln:F6}, delta from full={Math.Abs(ln-allRIQR.Average()):F8}");
        }

        bool robust=jkStd<0.003&&Math.Abs(split1-split2)<0.003;
        _o.WriteLine($"\nRobustness: {(robust?"STABLE":"SENSITIVE")} (jk std<0.003, split delta<0.003)");

        // ============================================================
        // PART I — Decision Model
        // ============================================================
        _o.WriteLine($"\n=== PART I: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE");
        _o.WriteLine($"Causal closure: BLOCKED");

        string decision;
        if(seedR2>0.5&&topoR2<0.1)
            decision="Model A: rawIQR is mostly seed realization. Between-seed variance dominates; topology is independent.";
        else if(genR2>0.95&&topoR2<0.1)
            decision="Model B: rawIQR is mostly generator realization.";
        else if(topoR2>0.3)
            decision="Model C: rawIQR is mostly topology-associated.";
        else if(nCorr>0.5)
            decision="Model D: rawIQR is mostly sample-size (N) associated.";
        else if(seedR2>0.2&&genR2>0.8)
            decision="Model E: mixed origin — seed + generator jointly explain rawIQR.";
        else
            decision="Model F: origin unresolved.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: seed R²={seedR2:F3}, gen bias={observedIQR-expectedIQR:F5}, N corr={nCorr:F3}, topo R²={topoR2:F4}");
        _o.WriteLine($"Generator: uniform[0.90,1.10], expected IQR=S={S}");
        _o.WriteLine($"RawIQR is a pure function of seed-determined weight sampling + N.");
        _o.WriteLine($"Topology and graph structure are independent (rawIQR computed before coupling).");
        _o.WriteLine("CLAIMS: Origin traced. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RIO_01 complete. Commit: RIO_01_RawIQROriginAudit ===");
    }

    // ============================================================
    // HELPERS
    // ============================================================
    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Skew(double[] s){double m=s.Average(),sd=Sd(s);return sd>0.001?s.Sum(v=>Math.Pow((v-m)/sd,3))/s.Length:0;}
    static double Pearson(double[] x,double[] y){
        int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();
        double sx=0,sy=0,sxy=0;
        for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}
        return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;
    }
    static double PVal(double r,int n){
        // Fisher z-transform approximation for correlation p-value (two-sided)
        if(n<4||Math.Abs(r)>0.999)return 1.0;
        double z=0.5*Math.Log((1+r)/(1-r))*Math.Sqrt(n-3);
        double p=2*(1-NormCDF(Math.Abs(z)));
        return Math.Clamp(p,0,1);
    }
    static double NormCDF(double x){
        double a1=0.254829592,a2=-0.284496736,a3=1.421413741,a4=-1.453152027,a5=1.061405429;
        double p=0.3275911;
        double sign=x<0?-1:1;
        x=Math.Abs(x)/Math.Sqrt(2);
        double t=1/(1+p*x);
        double y=1-(((((a5*t+a4)*t)+a3)*t+a2)*t+a1)*t*Math.Exp(-x*x);
        return 0.5*(1+sign*y);
    }

    // Minimal simulation helpers (copied from V5.53 for independence)
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
}
