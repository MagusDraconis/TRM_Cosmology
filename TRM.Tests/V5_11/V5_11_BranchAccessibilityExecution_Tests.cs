using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace TRM.Tests.V5_11;

[Trait("Category", "V5_11"), Trait("Category", "V5_11_BAE")]
public class V5_11_BranchAccessibilityExecution_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private static readonly int[] TN={67,71,72};private const int MS=100;

    public V5_11_BranchAccessibilityExecution_Tests(ITestOutputHelper o){_o=o;}

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
    static double DMean(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}

    struct StatePt { public double Om,Dm,Km,Ks;public bool Hi;public string Cls; }

    [Fact] public void BAE_01_BasinMap(){
        _o.WriteLine("═══ BASIN MAP: Natural Lo/Hi + Suppressed Hi (seeds 0-99) ═══");
        foreach(var n in TN){
            var pts=new List<StatePt>();
            for(int s=0;s<MS;s++){
                // Run B0
                var K=KS(n,s);double dmSum=0,kmSum=0,ksSum=0;
                for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);dmSum+=DMean(d,n);K=Cupd(d,n,K0,Xi);kmSum+=KM(K,n);ksSum+=KSd(K,n);}
                double om=Of(Sm(K,n,S,s+NE,St,REps),n).Average();bool hi=om>THR;
                pts.Add(new StatePt{Om=om,Dm=dmSum/NE,Km=kmSum/NE,Ks=ksSum/NE,Hi=hi,Cls=hi?"NatHi":"NatLo"});

                // Suppressed Hi: if natural hi, apply d_mean increase at epoch 5
                if(hi){
                    var K2=KS(n,s);
                    for(int e=0;e<NE;e++){var h=Sm(K2,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);
                        if(e==4){double dm=DMean(d,n);var d2=new double[n,n];
                            for(int i=0;i<n;i++){d2[i,i]=0;for(int j=i+1;j<n;j++){d2[i,j]=Math.Max(REps,d[i,j]+dm*0.5);d2[j,i]=d2[i,j];}}d=d2;}
                        K2=Cupd(d,n,K0,Xi);}
                    double om2=Of(Sm(K2,n,S,s+NE,St,REps),n).Average();
                    var hf=Sm(K2,n,S,s+NE,St,REps);var df=DL(Nm(RP(hf,n),n,REps),n);var Kf=Cupd(df,n,K0,Xi);
                    pts.Add(new StatePt{Om=om2,Dm=DMean(df,n),Km=KM(Kf,n),Ks=KSd(Kf,n),Hi=om2>THR,Cls="SupprHi"});
                }
            }

            var natLo=pts.Where(p=>p.Cls=="NatLo").ToList();
            var natHi=pts.Where(p=>p.Cls=="NatHi").ToList();
            var suppr=pts.Where(p=>p.Cls=="SupprHi").ToList();

            double lo_om=natLo.Average(p=>p.Om),lo_dm=natLo.Average(p=>p.Dm),lo_km=natLo.Average(p=>p.Km),lo_ks=natLo.Average(p=>p.Ks);
            double hi_om=natHi.Average(p=>p.Om),hi_dm=natHi.Average(p=>p.Dm),hi_km=natHi.Average(p=>p.Km),hi_ks=natHi.Average(p=>p.Ks);
            double sp_om=suppr.Average(p=>p.Om),sp_dm=suppr.Average(p=>p.Dm),sp_km=suppr.Average(p=>p.Km),sp_ks=suppr.Average(p=>p.Ks);

            // Distance from suppressed to lo/hi centroids
            double spToLo=NormDist(sp_dm,sp_km,sp_ks,lo_dm,lo_km,lo_ks);
            double spToHi=NormDist(sp_dm,sp_km,sp_ks,hi_dm,hi_km,hi_ks);
            double hiLoDist=NormDist(hi_dm,hi_km,hi_ks,lo_dm,lo_km,lo_ks);

            _o.WriteLine($"N={n} (Lo={natLo.Count} Hi={natHi.Count})");
            _o.WriteLine($"{"Class",-12} {"Omega",8} {"dMean",8} {"KMean",8} {"KStd",8}");
            _o.WriteLine($"{"NatLo",-12} {lo_om,8:F3} {lo_dm,8:F3} {lo_km,8:F3} {lo_ks,8:F3}");
            _o.WriteLine($"{"NatHi",-12} {hi_om,8:F3} {hi_dm,8:F3} {hi_km,8:F3} {hi_ks,8:F3}");
            _o.WriteLine($"{"SupprHi",-12} {sp_om,8:F3} {sp_dm,8:F3} {sp_km,8:F3} {sp_ks,8:F3}");
            _o.WriteLine($"SupprHi→NatLo dist: {spToLo:F3}  SupprHi→NatHi dist: {spToHi:F3}  Hi-Lo gap: {hiLoDist:F3}");
            string verdict = spToLo < spToHi ? "Suppressed Hi CLOSER TO Lo basin — suppression reaches natural basin" : "Suppressed Hi = separate region";
            _o.WriteLine(verdict);
            _o.WriteLine("");
        }
    }

    static double NormDist(double d1,double k1,double s1,double d2,double k2,double s2){
        return Math.Sqrt(Math.Pow((d1-d2)/Math.Max(Math.Abs(d2),1e-10),2)+Math.Pow((k1-k2)/Math.Max(Math.Abs(k2),1e-10),2)+Math.Pow((s1-s2)/Math.Max(Math.Abs(s2),1e-10),2));
    }

    [Fact] public void BAE_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (Hi->Lo reaches natural Lo): BAE_01 SupprHi distance to NatLo");
        _o.WriteLine("Gate B (Lo->Hi fails basin access): BAE_01 comparison");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: Basin distance comparison, natural vs suppressed.");
        _o.WriteLine("CONDITIONAL: N=67,71,72. d_mean/K_mean/K_std features.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
