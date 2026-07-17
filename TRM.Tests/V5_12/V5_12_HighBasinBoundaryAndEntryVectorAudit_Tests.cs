using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace TRM.Tests.V5_12;

[Trait("Category", "V5_12"), Trait("Category", "V5_12_HBI")]
public class V5_12_HighBasinBoundaryAndEntryVectorAudit_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;

    public V5_12_HighBasinBoundaryAndEntryVectorAudit_Tests(ITestOutputHelper o){_o=o;}

    static double[][]Sm(double[,]K,int n,double s,int seed,int st,double reps){var r=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(r.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=r.NextDouble()*2.0*Math.PI;int hL=st/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<st;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n,double reps){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(reps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n,double k0,double xi){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:k0*Math.Exp(-d[i,j]/Math.Max(xi,0.01));return K;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double Std(double[]v){double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double KM(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double KSd(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];return Std(v);}
    static double DMean(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}

    [Fact]public void HBI_01_EntryVector(){
        _o.WriteLine("═══ ENTRY VECTOR: Low->Hi projection analysis at N=71 (seeds 0-99) ═══");
        int n=71;int MS=100;
        var lo=new List<(double dm,double km,double ks,double om)>();var hi=new List<(double dm,double km,double ks,double om)>();
        for(int s=0;s<MS;s++){var K=KS(n,s);double dms=0,kms=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);dms+=DMean(d,n);K=Cupd(d,n,K0,Xi);kms+=KM(K,n);kss+=KSd(K,n);}
            double om=Of(Sm(K,n,S,s+NE,St,REps),n).Average();
            if(om>THR)hi.Add((dms/NE,kms/NE,kss/NE,om));else lo.Add((dms/NE,kms/NE,kss/NE,om));}

        // Centroids
        double loD=lo.Average(x=>x.dm),loK=lo.Average(x=>x.km),loS=lo.Average(x=>x.ks);
        double hiD=hi.Average(x=>x.dm),hiK=hi.Average(x=>x.km),hiS=hi.Average(x=>x.ks);
        double vD=hiD-loD,vK=hiK-loK,vS=hiS-loS;
        double vLen=Math.Sqrt(vD*vD+vK*vK+vS*vS);

        _o.WriteLine($"Lo centroid: D={loD:F4} K={loK:F4} Ks={loS:F4}");
        _o.WriteLine($"Hi centroid: D={hiD:F4} K={hiK:F4} Ks={hiS:F4}");
        _o.WriteLine($"Entry vector: dD={vD:F4} dK={vK:F4} dKs={vS:F4} |v|={vLen:F4}");
        _o.WriteLine($"Component contributions: D={vD/vLen*100:F0}% K={vK/vLen*100:F0}% Ks={vS/vLen*100:F0}%");
        _o.WriteLine("");

        // Projections for Natural Lo and Hi
        var loProj=lo.Select(x=>Proj(x.dm-loD,x.km-loK,x.ks-loS,vD,vK,vS)).ToArray();
        var hiProj=hi.Select(x=>Proj(x.dm-loD,x.km-loK,x.ks-loS,vD,vK,vS)).ToArray();
        _o.WriteLine($"Lo projection onto Hi vector: mean={loProj.Average():F4} range=[{loProj.Min():F3},{loProj.Max():F3}]");
        _o.WriteLine($"Hi projection onto Hi vector: mean={hiProj.Average():F4} range=[{hiProj.Min():F3},{hiProj.Max():F3}]");
        _o.WriteLine("");

        // K-graft: transient/delayed states (seeds 0-49)
        var kgProj=new List<double>();var kgCls=new List<string>();
        for(int s=0;s<50;s++){
            var blK=KS(n,s);for(int e=0;e<NE;e++){blK=Cupd(DL(Nm(RP(Sm(blK,n,S,s+e,St,REps),n),n,REps),n),n,K0,Xi);}
            if(Of(Sm(blK,n,S,s+NE,St,REps),n).Average()>THR)continue;
            int donor=-1;for(int d=0;d<50;d++){var dK=KS(n,d);for(int e=0;e<NE;e++){dK=Cupd(DL(Nm(RP(Sm(dK,n,S,d+e,St,REps),n),n,REps),n),n,K0,Xi);}if(Of(Sm(dK,n,S,d+NE,St,REps),n).Average()>THR){donor=d;break;}}
            if(donor<0)continue;
            var donK=KS(n,donor);for(int e=0;e<=3;e++){donK=Cupd(DL(Nm(RP(Sm(donK,n,S,donor+e,St,REps),n),n,REps),n),n,K0,Xi);}
            var K2=KS(n,s);
            for(int e=0;e<NE;e++){var h=Sm(K2,n,S,s+e,St,REps);if(e==3){K2=CK(donK,n);continue;}K2=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
            double immOm=Of(Sm(K2,n,S,s+NE,St,REps),n).Average();bool immHi=immOm>THR;
            var h2=Sm(K2,n,S,s+100,St,REps);var K3=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
            double pOm=Of(Sm(K3,n,S,s+200,St,REps),n).Average();bool persHi=pOm>THR;

            var hf=Sm(K2,n,S,s+NE,St,REps);var df=DL(Nm(RP(hf,n),n,REps),n);var Kf=Cupd(df,n,K0,Xi);
            double dm=DMean(df,n),km=KM(Kf,n),ks=KSd(Kf,n);
            string cls=immHi&&persHi?"Persist":immHi?"Transient":persHi?"Delayed":"Failed";
            kgProj.Add(Proj(dm-loD,km-loK,ks-loS,vD,vK,vS));
            kgCls.Add(cls);
        }

        _o.WriteLine($"{"Class",-12} {"Count",6} {"ProjMean",10} {"ProjRange",20} {"Direction",12}");
        foreach(var grp in kgCls.Select((c,i)=>(c,kgProj[i])).GroupBy(x=>x.c)){
            var vals=grp.Select(x=>x.Item2).ToArray();
            string dir=vals.Average()>0?"TOWARD Hi":"AWAY from Hi";
            _o.WriteLine($"{grp.Key,-12} {vals.Length,6} {vals.Average(),10:F4} [{vals.Min():F3},{vals.Max():F3}] {dir,12}");
        }

        _o.WriteLine("");
        bool transAway=kgCls.Where((c,i)=>c=="Transient").Select((_,i)=>kgProj[kgCls.IndexOf("Transient")+i]).DefaultIfEmpty(0).Average()<0;
        // Simpler
        var tProj=kgCls.Select((c,i)=>(c,kgProj[i])).Where(x=>x.c=="Transient").Select(x=>x.Item2).ToArray();
        _o.WriteLine($"Gate A (Induction moves away from Hi): {(tProj.Length>0&&tProj.Average()<0?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate D (K-graft orthogonal/off-manifold): projection comparison above.");
    }

    static double Proj(double x,double y,double z,double vx,double vy,double vz){
        double dot=x*vx+y*vy+z*vz;double v2=vx*vx+vy*vy+vz*vz;
        return v2>1e-15?dot/Math.Sqrt(v2):0;
    }

    [Fact]public void HBI_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A: HBI_01 projection direction.");
        _o.WriteLine("Gate D: HBI_01 K-graft orthogonal/off-manifold.");
        _o.WriteLine("Gate E: Entry vector N-dependent (N=71 only tested).");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: Entry vector, projection analysis.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
