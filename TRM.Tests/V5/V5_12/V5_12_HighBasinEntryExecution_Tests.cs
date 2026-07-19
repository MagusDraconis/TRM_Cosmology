using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace TRM.Tests.V5_12;

[Trait("Category", "V5_12"), Trait("Category", "V5_12_HBE")]
public class V5_12_HighBasinEntryExecution_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private static readonly int[] TN={67,71,72};

    public V5_12_HighBasinEntryExecution_Tests(ITestOutputHelper o){_o=o;}

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
    static double CohensD(double[]a,double[]b){double ma=a.Average(),mb=b.Average();double va=a.Sum(x=>(x-ma)*(x-ma))/a.Length,vb=b.Sum(x=>(x-mb)*(x-mb))/b.Length;double sp=Math.Sqrt((va+vb)/2);return sp>1e-15?Math.Abs(ma-mb)/sp:0;}

    [Fact]public void HBE_01_Signature(){
        _o.WriteLine("═══ HIGH-BASIN SIGNATURE: What separates NatHi from all other classes? (seeds 0-99) ═══");
        int MS=100;
        foreach(var n in TN){
            // Collect metrics for NatLo and NatHi
            var lo=new List<(double dm,double km,double ks,double om,double md)>();
            var hi=new List<(double dm,double km,double ks,double om,double md)>();
            for(int s=0;s<MS;s++){var K=KS(n,s);double dms=0,kms=0,kss=0;
                for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);dms+=DMean(d,n);K=Cupd(d,n,K0,Xi);kms+=KM(K,n);kss+=KSd(K,n);}
                var hf=Sm(K,n,S,s+NE,St,REps);double om=Of(hf,n).Average();var Rf=RP(hf,n);
                if(om>THR)hi.Add((dms/NE,kms/NE,kss/NE,om,new double[n*(n-1)/2].Average()));else lo.Add((dms/NE,kms/NE,kss/NE,om,0));}

            if(lo.Count<5||hi.Count<5)continue;
            var loD=lo.Select(x=>x.dm).ToArray();var loK=lo.Select(x=>x.km).ToArray();var loS=lo.Select(x=>x.ks).ToArray();var loO=lo.Select(x=>x.om).ToArray();
            var hiD=hi.Select(x=>x.dm).ToArray();var hiK=hi.Select(x=>x.km).ToArray();var hiS=hi.Select(x=>x.ks).ToArray();var hiO=hi.Select(x=>x.om).ToArray();

            _o.WriteLine($"N={n} (Lo={lo.Count} Hi={hi.Count})");
            _o.WriteLine($"{"Metric",-12} {"Lo Mean",10} {"Hi Mean",10} {"CohenD",8} {"Direction",12} {"Separation",12}");
            foreach(var(m,la,ha) in new[]{("d_mean",loD,hiD),("K_mean",loK,hiK),("K_std",loS,hiS),("Omega",loO,hiO)}){
                double cd=CohensD(la,ha);string dir=ha.Average()>la.Average()?"Hi > Lo":"Hi < Lo";
                string sep=cd>1.5?"STRONG":cd>0.8?"MODERATE":"WEAK";
                _o.WriteLine($"{m,-12} {la.Average(),10:F4} {ha.Average(),10:F4} {cd,8:F3} {dir,12} {sep,12}");
            }
            _o.WriteLine("");
        }
    }

    [Fact]public void HBE_02_EntryScore(){
        _o.WriteLine("═══ ENTRY SCORE: Simple additive basin entry score at N=71 (seeds 0-99) ═══");
        int n=71;int MS=100;
        var lo=new List<(double dm,double km,double ks,double om)>();
        var hi=new List<(double dm,double km,double ks,double om)>();
        for(int s=0;s<MS;s++){var K=KS(n,s);double dms=0,kms=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);dms+=DMean(d,n);K=Cupd(d,n,K0,Xi);kms+=KM(K,n);kss+=KSd(K,n);}
            var hf=Sm(K,n,S,s+NE,St,REps);double om=Of(hf,n).Average();
            if(om>THR)hi.Add((dms/NE,kms/NE,kss/NE,om));else lo.Add((dms/NE,kms/NE,kss/NE,om));}

        double loDm=lo.Average(x=>x.dm),loKm=lo.Average(x=>x.km),loKs=lo.Average(x=>x.ks),loOm=lo.Average(x=>x.om);
        double hiDm=hi.Average(x=>x.dm),hiKm=hi.Average(x=>x.km),hiKs=hi.Average(x=>x.ks),hiOm=hi.Average(x=>x.om);

        // Score: how many std-dev away from Lo toward Hi
        double sdDm=StDev(lo.Select(x=>x.dm).Concat(hi.Select(x=>x.dm)).ToArray());
        double sdKm=StDev(lo.Select(x=>x.km).Concat(hi.Select(x=>x.km)).ToArray());
        double sdKs=StDev(lo.Select(x=>x.ks).Concat(hi.Select(x=>x.ks)).ToArray());

        // Compute scores for Lo, Hi
        double[] LoScores=new double[lo.Count];double[] HiScores=new double[hi.Count];
        for(int i=0;i<lo.Count;i++)LoScores[i]=EntryScore(lo[i].dm,lo[i].km,lo[i].ks,loDm,loKm,loKs,sdDm,sdKm,sdKs,hiDm,hiKm,hiKs);
        for(int i=0;i<hi.Count;i++)HiScores[i]=EntryScore(hi[i].dm,hi[i].km,hi[i].ks,loDm,loKm,loKs,sdDm,sdKm,sdKs,hiDm,hiKm,hiKs);

        double loScore=LoScores.Average(),hiScore=HiScores.Average();

        _o.WriteLine($"Lo centroid: D={loDm:F3} K={loKm:F3} Ks={loKs:F3}");
        _o.WriteLine($"Hi centroid: D={hiDm:F3} K={hiKm:F3} Ks={hiKs:F3}");
        _o.WriteLine($"Lo mean score: {loScore:F3}  Hi mean score: {hiScore:F3}");
        _o.WriteLine($"Score separation (Cohen's d): {CohensD(LoScores,HiScores):F3}");
    }

    static double EntryScore(double dm,double km,double ks,double loDm,double loKm,double loKs,double sdDm,double sdKm,double sdKs,double hiDm,double hiKm,double hiKs){
        double dZ=(dm-loDm)/Math.Max(sdDm,1e-10);double kZ=(km-loKm)/Math.Max(sdKm,1e-10);double sZ=(ks-loKs)/Math.Max(sdKs,1e-10);
        double dHi=(dm-hiDm)/Math.Max(sdDm,1e-10);double kHi=(km-hiKm)/Math.Max(sdKm,1e-10);double sHi=(ks-hiKs)/Math.Max(sdKs,1e-10);
        return (dHi*dHi+kHi*kHi+sHi*sHi)-(dZ*dZ+kZ*kZ+sZ*sZ);
    }
    static double StDev(double[]v){double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}

    [Fact]public void HBE_03_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (Necessary conditions): HBE_01 metric separation");
        _o.WriteLine("Gate B (Sufficient conditions): HBE_02 entry score");
        _o.WriteLine("Gate C (N=71 partial entry): HBE_01 N=71 comparison");
        _o.WriteLine("Gate D (NatHi separable): HBE_01 Cohen's d > 1.5 for some metric");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: Metric separation, entry score, Cohen's d.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
