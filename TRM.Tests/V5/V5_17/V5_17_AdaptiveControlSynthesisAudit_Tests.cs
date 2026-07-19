using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_17;

[Trait("Category","V5_17"),Trait("Category","V5_17_ARS"),Trait("Category","LongRunning")]
public class V5_17_AdaptiveControlSynthesisAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153,RebThresh=0.01;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_17_AdaptiveControlSynthesisAudit_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void ARS_01_C1vsC3HeadToHead(){
        _o.WriteLine("═══ ARS_01: C1 vs C3 head-to-head with rescue overlap ═══");

        var sr=new ConcurrentBag<(int n,int s,int coh,bool a0,bool c1,bool c3,double reb)>();

        foreach(var coh in new[]{0,1}){
            int ss=coh==0?0:100,se=coh==0?99:199,mp=coh==0?15:12;
            foreach(var n in new[]{71,72}){
                var hi=Hi(n);var lo=Lo(n);int cnt=0;
                for(int seed=ss;seed<=se&&cnt<mp;seed++){
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
                    if(!(n==72?isP1&&proj>PHV&&orth>OTH:isP1&&proj>PHV))continue;
                    cnt++;

                    bool isP2=sb.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
                    var K2=KS(n,seed);for(int e=0;e<3;e++){var h=Sim(K2,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
                    var h4=Sim(K2,n,S,seed+3);var d4=DL(Nm(RP(h4,n),n),n);double curDm=Dm(d4,n);
                    double frac=Math.Clamp((tgt+1e-9)/(curDm+1e-9),0.01,100.0);var dM=CD(d4,n);
                    for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
                    var h5=Sim(K2,n,S,seed+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
                    var hT1=Sim(K2,n,S,seed+100);double om1=Of(hT1,n).Average(),dT1=Dm(DL(Nm(RP(hT1,n),n),n),n);
                    var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,seed+200);
                    double om2=Of(hT2,n).Average(),dT2=Dm(DL(Nm(RP(hT2,n),n),n),n);
                    double reb=dT2-dT1;bool a0=om2>THR;

                    bool c1p=a0,c3p=a0;
                    if(reb<RebThresh&&!a0){
                        var Kc1=Cupd(DL(Nm(RP(hT1,n),n),n),n);var hc1=Sim(Kc1,n,S,seed+300);
                        c1p=Of(Sim(Cupd(DL(Nm(RP(hc1,n),n),n),n),n,S,seed+400),n).Average()>THR;
                        var dmat3=DL(Nm(RP(hT1,n),n),n);double dm3=Dm(dmat3,n);
                        double nudged=dm3+(hi.dm-dm3)*0.2;
                        double frac3=Math.Clamp((nudged+1e-9)/(dm3+1e-9),0.5,1.5);
                        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=frac3;
                        var Kc3=Cupd(dmat3,n);var hc3=Sim(Kc3,n,S,seed+300);
                        c3p=Of(Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,seed+400),n).Average()>THR;
                    }
                    sr.Add((n,seed,coh,a0,c1p,c3p,reb));
                }
            }
        }

        var all=sr.ToArray();
        int total=all.Length;int a0s=all.Count(r=>r.a0),c1s=all.Count(r=>r.c1),c3s=all.Count(r=>r.c3);
        int rescC1=all.Count(r=>!r.a0&&r.c1),rescC3=all.Count(r=>!r.a0&&r.c3);
        int both=all.Count(r=>!r.a0&&r.c1&&r.c3),c1Only=all.Count(r=>!r.a0&&r.c1&&!r.c3),c3Only=all.Count(r=>!r.a0&&!r.c1&&r.c3);
        int damC1=all.Count(r=>r.a0&&!r.c1),damC3=all.Count(r=>r.a0&&!r.c3);

        _o.WriteLine($"Seeds: {total} | A0: {a0s} ({a0s*100.0/total:F0}%) | C1: {c1s} ({c1s*100.0/total:F0}%) | C3: {c3s} ({c3s*100.0/total:F0}%)");
        _o.WriteLine($"\n─── Rescue overlap ───");
        _o.WriteLine($"C1 rescues: {rescC1} | C3 rescues: {rescC3}");
        _o.WriteLine($"Both rescue: {both} | C1 only: {c1Only} | C3 only: {c3Only}");
        _o.WriteLine($"Overlap: {(both>0?$"{both}/{(c1Only+both+c3Only)} ({both*100.0/(c1Only+both+c3Only):F0}%)":"none")}");
        _o.WriteLine($"C1 damage: {damC1} | C3 damage: {damC3}");

        _o.WriteLine(string.Format("\n{0,4} {1,6} {2,6} {3,6} {4,6} {5,6} {6,6}",
            "N","Coh","A0","C1","C3","CR1","CR3"));
        foreach(var n in new[]{71,72})foreach(var coh in new[]{0,1}){
            var sub=all.Where(r=>r.n==n&&r.coh==coh).ToArray();
            if(sub.Length==0)continue;
            int sa0=sub.Count(r=>r.a0),sc1=sub.Count(r=>r.c1),sc3=sub.Count(r=>r.c3);
            int r1=sub.Count(r=>!r.a0&&r.c1),r3=sub.Count(r=>!r.a0&&r.c3);
            string cl=coh==0?"Train":"Hold";
            _o.WriteLine($"{n,4} {cl,6} {sa0*100.0/sub.Length,5:F0}% {sc1*100.0/sub.Length,5:F0}% {sc3*100.0/sub.Length,5:F0}% {r1,6} {r3,6}");
        }

        _o.WriteLine("\n─── C1 vs C3 verdict ───");
        if(c3s==c1s)_o.WriteLine("C1 and C3 are EQUIVALENT in overall persistence.");
        else if(c3s>c1s)_o.WriteLine($"C3 preferred (+{c3s-c1s} strict)");
        else _o.WriteLine($"C1 preferred (+{c1s-c3s} strict)");

        if(both>=rescC1*0.5)_o.WriteLine("HIGH OVERLAP — C1 and C3 rescue mostly same seeds.");
        else _o.WriteLine("DIVERGENT rescues — C1 and C3 rescue different seeds.");

        _o.WriteLine("\n─── Adaptive principle ───");
        _o.WriteLine(both>=c1Only+c3Only?"Shared mechanism: both corrections operate through same pathway.":"Complementary mechanisms: C1 and C3 provide independent rescue channels.");

        _o.WriteLine("\n─── Gates ───");
        _o.WriteLine($"Gate A (C3 preferred): {(c3s>c1s+1?"REACHED":"NOT REACHED")} — diff={c3s-c1s}");
        _o.WriteLine($"Gate B (C1 preferred): {(c1s>c3s+1?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate C (Equivalent): {(Math.Abs(c1s-c3s)<=1?"REACHED — C1=C3":"NOT REACHED")}");
        _o.WriteLine($"Gate D (Shared principle): {(both>=rescC1*0.5?"REACHED":"NOT REACHED")} — overlap={both}/{rescC1+c3Only}");
        _o.WriteLine($"Gate E (Adaptive validated): {(c3s>a0s?"REACHED":"NOT REACHED")} — C3={c3s} > A0={a0s}");
        _o.WriteLine($"Gate F (Sample limit): CHECK — small N per stratum");

        _o.WriteLine("\n─── Recommendation ───");
        if(Math.Abs(c1s-c3s)<=1&&both>=rescC1*0.5)
            _o.WriteLine("Finalize V5.17 with ADAPTIVE FAMILY: C1/C3 as equivalent adaptive corrections.");
        else if(c3s>c1s)
            _o.WriteLine("Finalize V5.17 with C3 (entry-vector re-alignment) as preferred.");
        else
            _o.WriteLine("Finalize V5.17 with C1 (K-preserve) as preferred.");
    }

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
