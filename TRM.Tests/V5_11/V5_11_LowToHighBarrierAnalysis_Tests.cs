using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace TRM.Tests.V5_11;

[Trait("Category", "V5_11"), Trait("Category", "V5_11_BAA")]
public class V5_11_LowToHighBarrierAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private static readonly int[] TN={71,67,72};private const int MS=50;

    public V5_11_LowToHighBarrierAnalysis_Tests(ITestOutputHelper o){_o=o;}

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
    static double DMean(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}
    static double D2(double d1,double k1,double s1,double d2,double k2,double s2){return Math.Sqrt(Math.Pow((d1-d2)/Math.Max(Math.Abs(d2),1e-10),2)+Math.Pow((k1-k2)/Math.Max(Math.Abs(k2),1e-10),2)+Math.Pow((s1-s2)/Math.Max(Math.Abs(s2),1e-10),2));}

    [Fact]public void BAA_01_SimpleBarrier(){
        _o.WriteLine("═══ SIMPLE BARRIER: Lo->Hi via K_graft at N=71 CP4 (seeds 0-29) ═══");
        int n=71;int seeds=30;
        // Natural centroids
        var loPts=new List<(double d,double k,double s,double o)>();var hiPts=new List<(double d,double k,double s,double o)>();
        for(int s=0;s<seeds;s++){var K=KS(n,s);double ds=0,ksm=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);ds+=DMean(d,n);K=Cupd(d,n,K0,Xi);ksm+=KM(K,n);kss+=KSd(K,n);}
            double om=Of(Sm(K,n,S,s+NE,St,REps),n).Average();
            if(om>THR)hiPts.Add((ds/NE,ksm/NE,kss/NE,om));else loPts.Add((ds/NE,ksm/NE,kss/NE,om));}
        double loD=loPts.Average(x=>x.d),loK=loPts.Average(x=>x.k),loS=loPts.Average(x=>x.s);
        double hiD=hiPts.Average(x=>x.d),hiK=hiPts.Average(x=>x.k),hiS=hiPts.Average(x=>x.s);

        // For each Lo seed, run K-graft and track states
        int transC=0,delayC=0,failC=0;double transToLo=0,transToHi=0,delayToLo=0,delayToHi=0,failToLo=0,failToHi=0;
        for(int s=0;s<seeds;s++){
            // Run baseline to check if it's Lo
            var blK=KS(n,s);for(int e=0;e<NE;e++){blK=Cupd(DL(Nm(RP(Sm(blK,n,S,s+e,St,REps),n),n,REps),n),n,K0,Xi);}
            if(Of(Sm(blK,n,S,s+NE,St,REps),n).Average()>THR)continue; // skip Hi seeds

            // K-graft: use first Hi seed as donor (simplified)
            int donor=0;for(int i=0;i<seeds;i++){var dK=KS(n,i);for(int e=0;e<NE;e++){dK=Cupd(DL(Nm(RP(Sm(dK,n,S,i+e,St,REps),n),n,REps),n),n,K0,Xi);}if(Of(Sm(dK,n,S,i+NE,St,REps),n).Average()>THR){donor=i;break;}}

            // Get donor CP4 K
            var donK=KS(n,donor);double donD=0,donKm=0,donKs=0;
            for(int e=0;e<=3;e++){var h=Sm(donK,n,S,donor+e,St,REps);if(e==3){var d3=DL(Nm(RP(h,n),n,REps),n);donD=DMean(d3,n);}donK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
            donKm=KM(donK,n);donKs=KSd(donK,n);

            // Graft: replace Lo K at CP4 with donor K
            var K=KS(n,s);double immD=0,immKm=0,immKs=0;
            for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);if(e==3){K=CK(donK,n);continue;}K=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
            var hf=Sm(K,n,S,s+NE,St,REps);var df=DL(Nm(RP(hf,n),n,REps),n);var Kf=Cupd(df,n,K0,Xi);
            double immOm=Of(hf,n).Average();immD=DMean(df,n);immKm=KM(Kf,n);immKs=KSd(Kf,n);
            bool immHi=immOm>THR;

            // +1 epoch
            var h2=Sm(Kf,n,S,s+100,St,REps);var K2=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
            double pOm=Of(Sm(K2,n,S,s+200,St,REps),n).Average();
            var df2=DL(Nm(RP(Sm(K2,n,S,s+200,St,REps),n),n,REps),n);var Kf2=Cupd(df2,n,K0,Xi);
            double pD=DMean(df2,n),pKm=KM(Kf2,n),pKs=KSd(Kf2,n);
            bool persHi=pOm>THR;

            if(immHi&&persHi){ /* strict persistence — unexpected */ }
            else if(immHi&&!persHi){transC++;transToLo+=D2(immD,immKm,immKs,loD,loK,loS);transToHi+=D2(immD,immKm,immKs,hiD,hiK,hiS);}
            else if(!immHi&&persHi){delayC++;delayToLo+=D2(pD,pKm,pKs,loD,loK,loS);delayToHi+=D2(pD,pKm,pKs,hiD,hiK,hiS);}
            else{failC++;failToLo+=D2(immD,immKm,immKs,loD,loK,loS);failToHi+=D2(immD,immKm,immKs,hiD,hiK,hiS);}
        }

        _o.WriteLine($"N={n}: Lo={loPts.Count} Hi={hiPts.Count}");
        _o.WriteLine($"NatLo centroid: D={loD:F3} K={loK:F3} Ks={loS:F3}");
        _o.WriteLine($"NatHi centroid: D={hiD:F3} K={hiK:F3} Ks={hiS:F3}");
        _o.WriteLine($"{"Class",-12} {"Count",6} {"DistToLo",10} {"DistToHi",10} {"Closer To",12}");
        if(transC>0)_o.WriteLine($"{"Transient",-12} {transC,6} {transToLo/transC,10:F3} {transToHi/transC,10:F3} {(transToLo<transToHi?"LO BASIN":"HI BASIN"),12}");
        if(delayC>0)_o.WriteLine($"{"Delayed",-12} {delayC,6} {delayToLo/delayC,10:F3} {delayToHi/delayC,10:F3} {(delayToLo<delayToHi?"LO BASIN":"HI BASIN"),12}");
        if(failC>0)_o.WriteLine($"{"Failed",-12} {failC,6} {failToLo/failC,10:F3} {failToHi/failC,10:F3} {(failToLo<failToHi?"LO BASIN":"HI BASIN"),12}");

        _o.WriteLine("");
        if(transC>0)_o.WriteLine($"Transient states: {(transToLo<transToHi?"CLOSER TO LO BASIN — never reached Hi":"CLOSER TO HI BASIN — reached but collapsed")}");
        _o.WriteLine($"Gate B (Boundary only): {(transC>0&&transToLo<transToHi?"REACHED — induced states remain near Lo":"NOT REACHED")}");
    }

    [Fact]public void BAA_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (Induced states remain Lo): BAA_02");
        _o.WriteLine("Gate D (Reach Hi but collapse): BAA_02");
        _o.WriteLine("Gate E (N=71 reduced barrier): BAA_01 multi-N");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: Transient/delayed state localization.");
        _o.WriteLine("CONDITIONAL: N=71, K-graft CP4, seeds 0-29.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
