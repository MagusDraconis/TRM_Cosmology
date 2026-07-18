using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_17;

[Trait("Category","V5_17"),Trait("Category","V5_17_ARE"),Trait("Category","LongRunning")]
public class V5_17_AdaptiveResponseExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebThresh=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    struct AResult{
        public int seed,n,cohort;public string cls,model;
        public double rebMag,omT1,omT2,omFinal;public bool immHi,persist,rescued,damaged;
    }

    public V5_17_AdaptiveResponseExecution_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void ARE_01_AdaptiveInterventionTest(){
        _o.WriteLine("═══ ARE_01: Two-stage adaptive intervention test ═══");
        _o.WriteLine("Testing on N=71/72 train (0-99), M3+ selected + intervention.");
        _o.WriteLine("A0: M3+ static baseline. A2: rebMagnitude rescue. A4: universal. A5: wrong-way.");

        var results=new ConcurrentBag<AResult>();

        foreach(var n in new[]{71,72}){
            var hi=Hi(n);var lo=Lo(n);
            // Find M3+ selected candidates
            for(int s=0;s<100;s++){
                if(IsHi(n,s))continue;
                var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
                var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
                double d2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
                double orth=Math.Sqrt(Math.Max(0,d2-proj*proj));
                bool isP1=sb.cls=="P1"||sb.cls=="P1b";
                bool selM3p=n==75?isP1:(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV);
                if(!selM3p)continue;

                // Run first intervention (standard)
                bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
                var K2=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
                var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
                double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
                var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
                var hT1=Sim(K2,n,S,s+100);double om1=Of(hT1,n).Average(),dT1=Dm(DL(Nm(RP(hT1,n),n),n),n);
                // Probe: continue 1 more epoch to measure rebMagnitude
                var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);
                double om2=Of(hT2,n).Average(),dT2=Dm(DL(Nm(RP(hT2,n),n),n),n);
                double rebMag=dT2-dT1;
                bool a0Persist=om2>THR;

                // A2: rescue if rebMagnitude low
                bool a2Persist=a0Persist;
                if(rebMag<RebThresh&&!a0Persist){
                    // Apply second correction: additional 20% d compression
                    var dMC=CD(DL(Nm(RP(hT1,n),n),n),n);
                    for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMC[i,j]*=0.8;
                    var KC=Cupd(dMC,n);var hC=Sim(KC,n,S,s+300);
                    a2Persist=Of(Sim(Cupd(DL(Nm(RP(hC,n),n),n),n),n,S,s+400),n).Average()>THR;
                }

                // A4: universal second correction
                var dMU=CD(DL(Nm(RP(hT1,n),n),n),n);
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMU[i,j]*=0.8;
                var KU=Cupd(dMU,n);var hU=Sim(KU,n,S,s+300);
                bool a4Persist=Of(Sim(Cupd(DL(Nm(RP(hU,n),n),n),n),n,S,s+400),n).Average()>THR;

                // A5: wrong-way (correct high reb seeds)
                bool a5Persist=a0Persist;
                if(rebMag>=RebThresh&&a0Persist){
                    var dMW=CD(DL(Nm(RP(hT1,n),n),n),n);
                    for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dMW[i,j]*=0.8;
                    var KW=Cupd(dMW,n);var hW=Sim(KW,n,S,s+300);
                    a5Persist=Of(Sim(Cupd(DL(Nm(RP(hW,n),n),n),n),n,S,s+400),n).Average()>THR;
                }

                results.Add(new AResult{seed=s,n=n,cohort=0,cls=sb.cls,model="A0",rebMag=rebMag,omT1=om1,omT2=om2,omFinal=om2,immHi=om1>THR,persist=a0Persist});
                results.Add(new AResult{seed=s,n=n,cohort=0,cls=sb.cls,model="A2",rebMag=rebMag,persist=a2Persist,rescued=!a0Persist&&a2Persist,damaged=a0Persist&&!a2Persist});
                results.Add(new AResult{seed=s,n=n,cohort=0,cls=sb.cls,model="A4",rebMag=rebMag,persist=a4Persist,rescued=!a0Persist&&a4Persist,damaged=a0Persist&&!a4Persist});
                results.Add(new AResult{seed=s,n=n,cohort=0,cls=sb.cls,model="A5",rebMag=rebMag,persist=a5Persist,rescued=!a0Persist&&a5Persist,damaged=a0Persist&&!a5Persist});
            }
        }

        var all=results.ToArray();
        _o.WriteLine($"\nTotal trials: {all.Length}");

        // Per-model summary
        _o.WriteLine(string.Format("\n{0,-6} {1,6} {2,6} {3,6} {4,6} {5,6} {6,8} {7,8}",
            "Model","Seeds","Str","Resc","Dam","Unchg","Rate","ΔA0"));
        foreach(var m in new[]{"A0","A2","A4","A5"}){
            var mr=all.Where(r=>r.model==m).ToArray();
            var a0r=all.Where(r=>r.model=="A0").ToArray();
            int str=mr.Count(r=>r.persist),resc=mr.Count(r=>r.rescued),dam=mr.Count(r=>r.damaged);
            int unchg=mr.Length-str-resc-dam;
            double rate=(double)str/mr.Length;
            double a0rate=(double)a0r.Count(r=>r.persist)/a0r.Length;
            _o.WriteLine($"{m,-6} {mr.Length/4,6} {str,6} {resc,6} {dam,6} {unchg,6} {rate,7:P0} {rate-a0rate,8:+.0%}");
        }

        // Per-N
        _o.WriteLine(string.Format("\n{0,4} {1,8} {2,8} {3,8} {4,8}",
            "N","A0","A2","A4","A5"));
        foreach(var n in new[]{71,72}){
            double a0=Rate(all,n,"A0"),a2=Rate(all,n,"A2"),a4=Rate(all,n,"A4"),a5=Rate(all,n,"A5");
            _o.WriteLine($"{n,4} {a0,7:P0} {a2,8:P0} {a4,8:P0} {a5,8:P0}");
        }

        // rebMagnitude probe verification
        var probes=all.Where(r=>r.model=="A0").ToArray();
        var tp=probes.Where(r=>r.persist).ToArray();var fp=probes.Where(r=>!r.persist).ToArray();
        _o.WriteLine($"\nProbe: TP rebMean={tp.Average(r=>r.rebMag):F4} FP rebMean={fp.Average(r=>r.rebMag):F4} ES={EffSz(tp.Select(r=>r.rebMag).ToArray(),fp.Select(r=>r.rebMag).ToArray()):F3}");
        _o.WriteLine($"rebMag<{RebThresh}: {probes.Count(r=>r.rebMag<RebThresh)} seeds, persist={probes.Count(r=>r.rebMag<RebThresh&&r.persist)}/{probes.Count(r=>r.rebMag<RebThresh)}");
        _o.WriteLine($"rebMag>={RebThresh}: {probes.Count(r=>r.rebMag>=RebThresh)} seeds, persist={probes.Count(r=>r.rebMag>=RebThresh&&r.persist)}/{probes.Count(r=>r.rebMag>=RebThresh)}");

        _o.WriteLine("\n─── Gate assessment ───");
        double baseRate=Rate(all,0,"A0"),adaptRate=Rate(all,0,"A2");
        bool improves=adaptRate>baseRate+0.03;int rescues=all.Count(r=>r.model=="A2"&&r.rescued),damages=all.Count(r=>r.model=="A2"&&r.damaged);
        _o.WriteLine($"Adaptive improves: {(improves?$"YES — +{adaptRate-baseRate:.0%} ({rescues} rescues, {damages} damages)":"NO")}");
        _o.WriteLine($"Gate A (Adaptive Improves): {(improves?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate B (Rescue Works): {(rescues>0?$"REACHED — {rescues} rescued":"NOT REACHED")}");
        _o.WriteLine($"Gate E (Adaptive Harms): {(damages>rescues?"REACHED — damage > rescue":"NOT REACHED")}");
        _o.WriteLine($"Gate F (No Improvement): {(!improves?"REACHED":"NOT REACHED")}");

        _o.WriteLine("\n─── Next: ARA or ARS ───");
    }

    // ══════════════════════════════════════════════════════════════
    static ConcurrentDictionary<int,P3>? _hC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
    static ConcurrentDictionary<int,P3>? _lC;
    double Rate(AResult[] a,int n,string m)=>a.Where(r=>r.n==n&&r.model==m).ToArray() is var s&&s.Length>0?(double)s.Count(r=>r.persist)/s.Length:0;
    static double EffSz(double[] a,double[] b){double ma=a.Average(),mb=b.Average();double sa=Math.Sqrt(a.Sum(x=>(x-ma)*(x-ma))/a.Length),sb=Math.Sqrt(b.Sum(x=>(x-mb)*(x-mb))/b.Length);double p=Math.Sqrt((sa*sa+sb*sb)/2);return p>1e-12?Math.Abs(ma-mb)/p:0;}

    // ══════════════════════════════════════════════════════════════
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
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
