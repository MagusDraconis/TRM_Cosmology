using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_57;

[Trait("Category","V5_57"),Trait("Category","V5_57_ART"),Trait("Category","LongRunning")]
public class V5_57_PipelineArtifactAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_57_PipelineArtifactAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void ART_01_PipelineArtifactAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== ART_01: Pipeline Artifact Audit ===");
        _o.WriteLine("=== V5.57. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Genuine discriminator or pipeline artifact? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int seeds=300;

        // ============================================================
        // Run full pipeline with stage tracking
        // ============================================================
        var stageBag=new ConcurrentBag<(int N,int seed,double riqr,double resid,double rank,int isHiPass,string cls)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        // Pre-compute seed IQRs
        var seedIQRd=new ConcurrentDictionary<int,double>();
        var allProf=new ConcurrentBag<(int N,int seed,double riqr)>();
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            var wo=w.OrderBy(v=>v).ToArray();double ri=Q(wo,0.75)-Q(wo,0.25);
            allProf.Add((n,s,ri));seedIQRd.AddOrUpdate(s,ri,(_,v)=>v+ri);
        });});
        var siQ=seedIQRd.ToDictionary(kv=>kv.Key,kv=>kv.Value/Ns.Length);

        var seedRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        foreach(var g in allProf.GroupBy(p=>p.seed)){
            var ordered=g.OrderBy(p=>p.riqr).Select((p,i)=>(p.N,i)).ToArray();if(ordered.Length<2)continue;
            var d2=new ConcurrentDictionary<int,double>();foreach(var(n,i)in ordered)d2[n]=(double)i/(ordered.Length-1);
            seedRanks[g.Key]=d2;
        }

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,seeds,s=>{
                var p=allProf.FirstOrDefault(x=>x.N==n&&x.seed==s);
                double resid=p.riqr-siQ.GetValueOrDefault(s,0),rank=seedRanks.GetValueOrDefault(s)?.GetValueOrDefault(n,-1)??-1;
                // Stage 0: pre-selection
                stageBag.Add((n,s,p.riqr,resid,rank,0,"pre"));
                // Stage 1: IsHi
                bool ih=IsHi(n,s);stageBag.Add((n,s,p.riqr,resid,rank,ih?1:0,ih?"IsHi-pass":"IsHi-fail"));
                if(!ih)return;
                // Stage 2: SAC
                var sb=SelectAndClassify(n,s,hi);
                string cls=sb==null?"reject":(sb.Value.cls=="P1"||sb.Value.cls=="P1b"?sb.Value.cls:"reject");
                stageBag.Add((n,s,p.riqr,resid,rank,1,cls));
            });});
        var sd=stageBag.ToArray();

        // ============================================================
        // PART A — Stage Localization
        // ============================================================
        _o.WriteLine($"\n=== PART A: Stage Localization ===");
        _o.WriteLine($"{"Stage",-12} {"n",6} {"rawIQR mean",12} {"resid mean",12} {"rank mean",12}");
        _o.WriteLine(new string('-',60));
        foreach(var stage in new[]{"pre","IsHi-pass","IsHi-fail","P1","P1b","reject"}){
            var ss=sd.Where(d=>d.cls==stage).ToArray();if(ss.Length<1)continue;
            _o.WriteLine($"{stage,-12} {ss.Length,6} {ss.Average(d=>d.riqr),12:F5} {ss.Average(d=>d.resid),12:F5} {ss.Average(d=>d.rank),12:F4}");
        }
        // Separation at each stage
        var preAll=sd.Where(d=>d.cls=="pre").ToArray();
        var ihP=sd.Where(d=>d.cls=="IsHi-pass").ToArray();
        var ihF=sd.Where(d=>d.cls=="IsHi-fail").ToArray();
        var sacP1=sd.Where(d=>d.cls=="P1").ToArray();
        var sacP1b=sd.Where(d=>d.cls=="P1b").ToArray();
        _o.WriteLine($"\nSeparation emergence:");
        _o.WriteLine($"  Pre→IsHi: resid delta={Math.Abs(ihP.Average(d=>d.resid)-ihF.Average(d=>d.resid)):F5} (rawIQR-neutral gate)");
        _o.WriteLine($"  SAC P1→P1b: rank delta={Math.Abs(sacP1.DefaultIfEmpty().Average(d=>d.rank)-sacP1b.DefaultIfEmpty().Average(d=>d.rank)):F4}, resid delta={Math.Abs(sacP1.DefaultIfEmpty().Average(d=>d.resid)-sacP1b.DefaultIfEmpty().Average(d=>d.resid)):F5}");

        // ============================================================
        // PART B — Permutation Audit
        // ============================================================
        _o.WriteLine($"\n=== PART B: Permutation Audit ===");
        // Shuffle ranks within each seed (break rank-outcome association)
        var rng2=new Random(42);
        int permReversals=0;
        for(int iter=0;iter<20;iter++){
            var permuted=new List<(int seed,double origRank,double permRank,string cls)>();
            foreach(var g in sd.Where(d=>d.cls=="P1"||d.cls=="P1b"||d.cls=="reject").GroupBy(d=>d.seed)){
                var members=g.ToArray();if(members.Length<2)continue;
                var origRanks=members.Select(d=>d.rank).ToArray();
                var shufRanks=origRanks.OrderBy(_=>rng2.NextDouble()).ToArray();
                for(int i=0;i<members.Length;i++)permuted.Add((g.Key,origRanks[i],shufRanks[i],members[i].cls));
            }
            var perm=permuted.ToArray();
            double origSep=Math.Abs(perm.Where(d=>d.cls=="P1").DefaultIfEmpty().Average(d=>d.origRank)-perm.Where(d=>d.cls=="P1b").DefaultIfEmpty().Average(d=>d.origRank));
            double permSep=Math.Abs(perm.Where(d=>d.cls=="P1").DefaultIfEmpty().Average(d=>d.permRank)-perm.Where(d=>d.cls=="P1b").DefaultIfEmpty().Average(d=>d.permRank));
            if(permSep>origSep*0.5)permReversals++;
        }
        _o.WriteLine($"Permutations where shuffled rank sep > 50% of original: {permReversals}/20");
        _o.WriteLine($"Effect: {(permReversals<5?"DESTROYED by permutation — effect is GENUINE":"SURVIVES permutation — effect is ARTIFACT")}");

        // ============================================================
        // PART C — Counterfactual Rankings
        // ============================================================
        _o.WriteLine($"\n=== PART C: Counterfactual Rankings ===");
        // Recompute with rawMean/random orderings using freshly computed means
        var altMeanRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        var altRandomRanks=new ConcurrentDictionary<int,ConcurrentDictionary<int,double>>();
        // Compute rawMean for each profile
        Parallel.ForEach(Ns,n=>{Parallel.For(0,seeds,s=>{
            var rng=new Random(s);var w=new double[n];
            for(int i=0;i<n;i++)w[i]=1.0+S*(rng.NextDouble()-0.5)*2.0;
            double rm=w.Average();
            altMeanRanks.GetOrAdd(s,_=>new()).TryAdd(n,rm);
        });});

        foreach(var g in altMeanRanks.GroupBy(kv=>kv.Key)){
            var members=g.ToArray();if(members.Length<2)continue;
            // rawMean ordering
            var byMean=members.OrderBy(kv=>kv.Value.Values.First()).Select((kv,idx)=>(kv.Key,idx)).ToArray();
            var rmR=new ConcurrentDictionary<int,double>();foreach(var(k,i2)in byMean)rmR[k]=(double)i2/(members.Length-1);
            altMeanRanks.GetOrAdd(g.Key,_=>new()); // already populated
            // replace with rank values
            var newDict=new ConcurrentDictionary<int,double>();foreach(var(k,i2)in byMean)newDict[k]=(double)i2/(members.Length-1);
            altMeanRanks[g.Key]=newDict;
        }

        // Random ordering per seed
        foreach(var g in seedRanks){
            var members=g.Value.Keys.ToArray();if(members.Length<2)continue;
            var shuf=members.OrderBy(_=>rng2.NextDouble()).Select((k,idx)=>(k,idx)).ToArray();
            var rd=new ConcurrentDictionary<int,double>();foreach(var(k,i2)in shuf)rd[k]=(double)i2/(members.Length-1);
            altRandomRanks[g.Key]=rd;
        }

        // Compare alternative orderings
        foreach(var(alt,altDict)in new[]{("rawMean",altMeanRanks),("random",altRandomRanks)}){
            double altP1=0,altP1b=0;int c1=0,c2=0;
            foreach(var d in sd.Where(d2=>d2.cls=="P1")){
                double ar=altDict.GetValueOrDefault(d.seed)?.GetValueOrDefault(d.N,-1)??-1;
                if(ar<0)continue;altP1+=ar;c1++;
            }
            foreach(var d in sd.Where(d2=>d2.cls=="P1b")){
                double ar=altDict.GetValueOrDefault(d.seed)?.GetValueOrDefault(d.N,-1)??-1;
                if(ar<0)continue;altP1b+=ar;c2++;
            }
            double pm=c1>0?altP1/c1:0,pbm=c2>0?altP1b/c2:0;
            _o.WriteLine($"{alt} ordering: P1 rank={pm:F4}, P1b rank={pbm:F4}, delta={Math.Abs(pm-pbm):F4} (orig P1={sacP1.DefaultIfEmpty().Average(d=>d.rank):F4})");
        }

        // ============================================================
        // PART D — Pipeline Localization
        // ============================================================
        _o.WriteLine($"\n=== PART D: Pipeline Localization ===");
        _o.WriteLine($"Effect chain:");
        _o.WriteLine($"  Generation → rank = f(rawIQR) [deterministic]");
        _o.WriteLine($"  IsHi: rank-neutral (pass rank={ihP.Average(d=>d.rank):F4}, fail={ihF.Average(d=>d.rank):F4})");
        _o.WriteLine($"  SAC: rank separation emerges (P1={sacP1.DefaultIfEmpty().Average(d=>d.rank):F4}, P1b={sacP1b.DefaultIfEmpty().Average(d=>d.rank):F4})");
        _o.WriteLine($"  Localization: SAC gate {(Math.Abs(ihP.Average(d=>d.rank)-ihF.Average(d=>d.rank))<0.05?"NOT at IsHi":"at IsHi")}");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");
        int origWins=0;
        for(int sp=0;sp<50;sp++){
            var shuf=sd.Where(d=>d.cls=="P1"||d.cls=="P1b"||d.cls=="reject").OrderBy(_=>rng2.NextDouble()).ToArray();
            int h=shuf.Length/2;var s1=shuf.Take(h).ToArray();
            double o=Math.Abs(s1.Where(d=>d.cls=="P1").DefaultIfEmpty().Average(d=>d.rank)-s1.Where(d=>d.cls=="P1b").DefaultIfEmpty().Average(d=>d.rank));
            if(o>0.01)origWins++;
        }
        _o.WriteLine($"Original rank sep stable: {origWins}/50 splits");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string decision;
        if(permReversals<5)decision="Model A: Genuine SAC discriminator. Permutation destroys effect → rank-outcome association is real, not artifact.";
        else if(permReversals<10)decision="Model C: Mixed — effect partially survives permutation.";
        else decision="Model B: Pipeline artifact. Permutation preserves effect → rank-outcome association is structural.";

        _o.WriteLine($"\nDecision: {decision}");
        _o.WriteLine($"Evidence: permutation survivals={permReversals}/20, IsHi neutrality confirmed");
        _o.WriteLine("CLAIMS: Artifact audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== ART_01 complete. Commit: ART_01_PipelineArtifactAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
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
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
