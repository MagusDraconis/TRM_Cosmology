using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_17;

[Trait("Category","V5_17"),Trait("Category","V5_17_ARV"),Trait("Category","LongRunning")]
public class V5_17_AdaptiveReAlignmentValidation_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebThresh=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_17_AdaptiveReAlignmentValidation_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void ARV_01_HoldoutValidation(){
        _o.WriteLine("═══ ARV_01: Holdout validation of C3 entry-vector re-alignment ═══");
        _o.WriteLine("Reproducing ARI on train, validating on holdout (100-199).");
        _o.WriteLine("C3 correction: nudge d 20% toward Hi-centroid direction.");

        int[] Ns={71,72};var allResults=new ConcurrentBag<(int n,int s,int coh,string model,bool a0Persist,bool c3Persist,double rebMag)>();

        foreach(var cohort in new[]{0,1}){
            int seedStart=cohort==0?0:100,seedEnd=cohort==0?99:199;
            int maxPerN=cohort==0?20:15; // smaller holdout sample

            foreach(var n in Ns){
                var hi=Hi(n);var lo=Lo(n);int count=0;
                for(int seed=seedStart;seed<=seedEnd&&count<maxPerN;seed++){
                    if(IsHi(n,seed))continue;
                    var K=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
                    var h3=Sim(K,n,S,seed+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
                    var h3E=Sim(K3,n,S,seed+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                    double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
                    var sb=new SBase{seed=seed,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
                    double dv=hi.dm-lo.dm,kv=hi.km-lo.km,sv=hi.ks-lo.ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                    double proj=vn>0?((d0-lo.dm)*dv+(km-lo.km)*kv+(ks-lo.ks)*sv)/vn:0;
                    double d2=(d0-lo.dm)*(d0-lo.dm)+(km-lo.km)*(km-lo.km)+(ks-lo.ks)*(ks-lo.ks);
                    double orth=Math.Sqrt(Math.Max(0,d2-proj*proj));
                    bool isP1=sb.cls=="P1"||sb.cls=="P1b";
                    bool sel=n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV;
                    if(!sel)continue;count++;

                    // A0: standard M3+ intervention
                    bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
                    var K2=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K2,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
                    var h4=Sim(K2,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
                    double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
                    for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
                    var h5=Sim(K2,n,S,seed+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
                    var hT1=Sim(K2,n,S,seed+100);double om1=Of(hT1,n).Average(),dT1=Dm(DL(Nm(RP(hT1,n),n),n),n);
                    var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);
                    double om2=Of(hT2,n).Average(),dT2=Dm(DL(Nm(RP(hT2,n),n),n),n);
                    double rebMag=dT2-dT1;bool a0Persist=om2>THR;

                    allResults.Add((n,seed,cohort,"A0",a0Persist,a0Persist,rebMag));

                    // C3: entry-vector re-alignment on low-reb seeds
                    bool c3Persist=a0Persist;
                    if(rebMag<RebThresh&&!a0Persist){
                        var dmat3=DL(Nm(RP(hT1,n),n),n);double dm3=Dm(dmat3,n);
                        double nudged=dm3+(hi.dm-dm3)*0.2;
                        double frac3=Math.Clamp((nudged+1e-9)/(dm3+1e-9),0.5,1.5);
                        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=frac3;
                        var Kc3=Cupd(dmat3,n);var hc3=Sim(Kc3,n,S,seed+300);
                        c3Persist=Of(Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,seed+400),n).Average()>THR;
                    }
                    allResults.Add((n,seed,cohort,"C3",a0Persist,c3Persist,rebMag));

                    // C1: K-preserve
                    bool c1Persist=a0Persist;
                    if(rebMag<RebThresh&&!a0Persist){
                        var Kc1=Cupd(DL(Nm(RP(hT1,n),n),n),n);var hc1=Sim(Kc1,n,S,seed+300);
                        c1Persist=Of(Sim(Cupd(DL(Nm(RP(hc1,n),n),n),n),n,S,seed+400),n).Average()>THR;
                    }
                    allResults.Add((n,seed,cohort,"C1",a0Persist,c1Persist,rebMag));
                }
            }
        }

        var all=allResults.ToArray();
        var lowReb=all.Where(r=>r.model=="A0"&&r.rebMag<RebThresh).ToArray();
        _o.WriteLine($"\nTotal seeds: {all.Count(r=>r.model=="A0")} | Low-reb: {lowReb.Length}");

        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8} {7,8}",
            "N","Cohort","A0","C3","C1","RescC3","RescC1","DamC3"));
        foreach(var n in Ns){foreach(var coh in new[]{0,1}){
            var sub=all.Where(r=>r.n==n&&r.coh==coh).ToArray();
            if(sub.Length==0)continue;
            int a0s=sub.Count(r=>r.model=="A0"&&r.c3Persist),c3s=sub.Count(r=>r.model=="C3"&&r.c3Persist);
            int c1s=sub.Count(r=>r.model=="C1"&&r.c3Persist); // C1 persist
            int rescC3=sub.Count(r=>r.model=="C3"&&!r.a0Persist&&r.c3Persist);
            int rescC1=sub.Count(r=>r.model=="C1"&&!r.a0Persist&&r.c3Persist);
            int damC3=sub.Count(r=>r.model=="C3"&&r.a0Persist&&!r.c3Persist);
            string cohLab=coh==0?"Train":"Hold";
            int a0Total=sub.Count(r=>r.model=="A0"),c3Total=sub.Count(r=>r.model=="C3"),c1Total=sub.Count(r=>r.model=="C1");
            _o.WriteLine($"{n,4} {cohLab,6} {a0s*100.0/Math.Max(1,a0Total),7:F0}% {c3s*100.0/Math.Max(1,c3Total),7:F0}% {c1s*100.0/Math.Max(1,c1Total),7:F0}% {rescC3,8} {rescC1,8} {damC3,8}");
        }}

        // Overall holdout
        var ho=all.Where(r=>r.coh==1).ToArray();
        int hoA0=ho.Count(r=>r.model=="A0"&&r.c3Persist);
        int hoC3=ho.Count(r=>r.model=="C3"&&r.c3Persist);
        int hoC1=ho.Count(r=>r.model=="C1"&&r.c3Persist);
        int hoResc=ho.Count(r=>r.model=="C3"&&!r.a0Persist&&r.c3Persist);
        int hoDam=ho.Count(r=>r.model=="C3"&&r.a0Persist&&!r.c3Persist);
        int hoTotal=ho.Count(r=>r.model=="A0");

        _o.WriteLine($"\n─── Holdout summary ───");
        _o.WriteLine($"A0: {hoA0}/{hoTotal} ({hoA0*100.0/hoTotal:F0}%)");
        _o.WriteLine($"C3: {hoC3}/{hoTotal} ({hoC3*100.0/hoTotal:F0}%), rescues={hoResc}, damage={hoDam}");
        _o.WriteLine($"C1: {hoC1}/{hoTotal} ({hoC1*100.0/hoTotal:F0}%)");

        // Probe reliability
        var probes=all.Where(r=>r.model=="A0").ToArray();
        var tp=probes.Where(r=>r.c3Persist).ToArray();var fp=probes.Where(r=>!r.c3Persist).ToArray();
        _o.WriteLine($"\nProbe ES: {EffSz(tp.Select(r=>r.rebMag).ToArray(),fp.Select(r=>r.rebMag).ToArray()):F2}");

        // Gates
        bool c3Imp=hoC3>hoA0+hoTotal*0.05,damOk=hoDam<=hoTotal*0.03;
        _o.WriteLine($"\nGate A (C3 validated holdout): {(c3Imp&&damOk?$"REACHED — +{(hoC3*100.0/hoTotal-hoA0*100.0/hoTotal):.0}pp, {hoDam} damage":"NOT REACHED")}");
        _o.WriteLine($"Gate D (C1 comparable): {(hoC1>=hoC3?"REACHED":"NOT REACHED")} — C1={hoC1} C3={hoC3}");
        _o.WriteLine($"Gate E (C3 damages): {(damOk?"NOT REACHED":"REACHED")} — {hoDam} damages");
        _o.WriteLine($"Gate H (ARI not reproduced): {(!c3Imp?"WARNING":"NOT REACHED")}");

        _o.WriteLine("\n─── Next: ARS Final Synthesis ───");
    }

    static double EffSz(double[] a,double[] b){double ma=a.Average(),mb=b.Average();double sa=Math.Sqrt(a.Sum(x=>(x-ma)*(x-ma))/a.Length),sb=Math.Sqrt(b.Sum(x=>(x-mb)*(x-mb))/b.Length);double p=Math.Sqrt((sa*sa+sb*sb)/2);return p>1e-12?Math.Abs(ma-mb)/p:0;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}
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
