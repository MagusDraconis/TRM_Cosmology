using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace TRM.Tests.V5_12;

[Trait("Category", "V5_12"), Trait("Category", "V5_12_HBA")]
public class V5_12_HighBasinEntryAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;

    public V5_12_HighBasinEntryAnalysis_Tests(ITestOutputHelper o){_o=o;}

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

    struct StatePt{public double Dm,Km,Ks,Om;public string Cls;public bool Persists;}

    [Fact]public void HBA_01_EntryScoreClasses(){
        _o.WriteLine("═══ ENTRY-SCORE CLASSIFICATION: All state classes at N=71 (seeds 0-49) ═══");
        int n=71;int MS=50;
        var pts=new List<StatePt>();

        // Natural Lo/Hi
        for(int s=0;s<MS;s++){var K=KS(n,s);double dms=0,kms=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);dms+=DMean(d,n);K=Cupd(d,n,K0,Xi);kms+=KM(K,n);kss+=KSd(K,n);}
            double om=Of(Sm(K,n,S,s+NE,St,REps),n).Average();
            pts.Add(new StatePt{Dm=dms/NE,Km=kms/NE,Ks=kss/NE,Om=om,Cls=om>THR?"NatHi":"NatLo",Persists=true});}

        // Suppressed Hi: apply d_mean increase to NatHi seeds
        for(int s=0;s<MS;s++){
            var K=KS(n,s);double dms=0,kms=0,kss=0;bool wasHi=false;
            for(int e=0;e<NE;e++){var h=Sm(K,n,S,s+e,St,REps);var d=DL(Nm(RP(h,n),n,REps),n);
                if(e==4&&Of(Sm(K,n,S,s+NE,St,REps),n).Average()>THR){double dm=DMean(d,n);var d2=new double[n,n];
                    for(int i=0;i<n;i++){d2[i,i]=0;for(int j=i+1;j<n;j++){d2[i,j]=Math.Max(REps,d[i,j]+dm*0.5);d2[j,i]=d2[i,j];}}d=d2;wasHi=true;}
                dms+=DMean(d,n);K=Cupd(d,n,K0,Xi);kms+=KM(K,n);kss+=KSd(K,n);}
            if(wasHi){double om=Of(Sm(K,n,S,s+NE,St,REps),n).Average();
                pts.Add(new StatePt{Dm=dms/NE,Km=kms/NE,Ks=kss/NE,Om=om,Cls="SupprHi",Persists=om<=THR});}}

        // K-graft: Transient / Delayed / Failed
        for(int s=0;s<MS;s++){
            var blK=KS(n,s);for(int e=0;e<NE;e++){blK=Cupd(DL(Nm(RP(Sm(blK,n,S,s+e,St,REps),n),n,REps),n),n,K0,Xi);}
            if(Of(Sm(blK,n,S,s+NE,St,REps),n).Average()>THR)continue; // skip Hi

            // Find donor
            int donor=-1;for(int d=0;d<MS;d++){var dK=KS(n,d);for(int e=0;e<NE;e++){dK=Cupd(DL(Nm(RP(Sm(dK,n,S,d+e,St,REps),n),n,REps),n),n,K0,Xi);}if(Of(Sm(dK,n,S,d+NE,St,REps),n).Average()>THR){donor=d;break;}}
            if(donor<0)continue;

            var donK=KS(n,donor);for(int e=0;e<=3;e++){donK=Cupd(DL(Nm(RP(Sm(donK,n,S,donor+e,St,REps),n),n,REps),n),n,K0,Xi);}
            var K2=KS(n,s);double dms=0,kms=0,kss=0;
            for(int e=0;e<NE;e++){var h=Sm(K2,n,S,s+e,St,REps);if(e==3){K2=CK(donK,n);continue;}
                var d=DL(Nm(RP(h,n),n,REps),n);dms+=DMean(d,n);K2=Cupd(d,n,K0,Xi);kms+=KM(K2,n);kss+=KSd(K2,n);}
            double immOm=Of(Sm(K2,n,S,s+NE,St,REps),n).Average();bool immHi=immOm>THR;
            var h2=Sm(K2,n,S,s+100,St,REps);var K3=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
            double pOm=Of(Sm(K3,n,S,s+200,St,REps),n).Average();bool persHi=pOm>THR;

            var hf=Sm(K2,n,S,s+NE,St,REps);var df=DL(Nm(RP(hf,n),n,REps),n);var Kf=Cupd(df,n,K0,Xi);
            dms=DMean(df,n);kms=KM(Kf,n);kss=KSd(Kf,n);
            string cls=immHi&&persHi?"StrictPers":immHi?"Transient":persHi?"Delayed":"Failed";
            pts.Add(new StatePt{Dm=dms,Km=kms,Ks=kss,Om=immOm,Cls=cls,Persists=persHi});
        }

        // Centroids from NatLo/NatHi
        var lo=pts.Where(p=>p.Cls=="NatLo").ToList();var hi=pts.Where(p=>p.Cls=="NatHi").ToList();
        double loD=lo.Average(p=>p.Dm),loK=lo.Average(p=>p.Km),loS=lo.Average(p=>p.Ks);
        double hiD=hi.Average(p=>p.Dm),hiK=hi.Average(p=>p.Km),hiS=hi.Average(p=>p.Ks);
        var allVals=pts.Select(p=>new[]{p.Dm,p.Km,p.Ks}).ToArray();
        double sdD=Std(pts.Select(p=>p.Dm).ToArray()),sdK=Std(pts.Select(p=>p.Km).ToArray()),sdS=Std(pts.Select(p=>p.Ks).ToArray());

        // Compute entry score for all (use separate array)
        var scores = new double[pts.Count];
        for (int i = 0; i < pts.Count; i++) {
            var p = pts[i];
            double dZ=(p.Dm-loD)/Math.Max(sdD,1e-10),kZ=(p.Km-loK)/Math.Max(sdK,1e-10),sZ=(p.Ks-loS)/Math.Max(sdS,1e-10);
            double dHi=(p.Dm-hiD)/Math.Max(sdD,1e-10),kHi=(p.Km-hiK)/Math.Max(sdK,1e-10),sHi=(p.Ks-hiS)/Math.Max(sdS,1e-10);
            scores[i] = (dZ*dZ+kZ*kZ+sZ*sZ)-(dHi*dHi+kHi*kHi+sHi*sHi);
        }

        // Report score distributions by class
        _o.WriteLine($"N=71 Centroids: Lo D={loD:F3} K={loK:F3} Ks={loS:F3}  Hi D={hiD:F3} K={hiK:F3} Ks={hiS:F3}");
        _o.WriteLine($"{"Class",-12} {"Count",6} {"MeanSc",8} {"MedSc",8} {"MinSc",8} {"MaxSc",8} {"HiOverlap%",10}");
        foreach(var grp in pts.Select((p,i)=>(p.Cls,scores[i])).GroupBy(x=>x.Cls).OrderBy(g=>g.Key)){
            var scs=grp.Select(x=>x.Item2).OrderBy(x=>x).ToArray();
            if(scs.Length==0)continue;
            var hiOnly=pts.Select((p,i)=>(p.Cls,scores[i])).Where(x=>x.Cls=="NatHi").Select(x=>x.Item2).ToArray();
            double hiLo=hiOnly.Min(),hiHi=hiOnly.Max();
            int overlap=scs.Count(s=>s>=hiLo&&s<=hiHi);
            _o.WriteLine($"{grp.Key,-12} {scs.Length,6} {scs.Average(),8:F3} {scs[scs.Length/2],8:F3} {scs[0],8:F3} {scs[^1],8:F3} {overlap*100.0/scs.Length,9:F0}%");
        }

        // Key finding
        var trans=pts.Select((p,i)=>(p.Cls,scores[i])).Where(x=>x.Cls=="Transient").Select(x=>x.Item2).ToArray();
        var delay=pts.Select((p,i)=>(p.Cls,scores[i])).Where(x=>x.Cls=="Delayed").Select(x=>x.Item2).ToArray();
        var hiSc=pts.Select((p,i)=>(p.Cls,scores[i])).Where(x=>x.Cls=="NatHi").Select(x=>x.Item2).ToArray();
        bool anyTransInHi=trans.Length>0&&trans.Any(s=>s>=hiSc.Min()&&s<=hiSc.Max());
        bool anyDelayInHi=delay.Length>0&&delay.Any(s=>s>=hiSc.Min()&&s<=hiSc.Max());
        _o.WriteLine("");
        _o.WriteLine($"Transient in Hi range: {(anyTransInHi?"YES":"NO")}");
        _o.WriteLine($"Delayed in Hi range: {(anyDelayInHi?"YES":"NO")}");
        _o.WriteLine($"Gate C (Induced fail entry): {(!anyTransInHi&&!anyDelayInHi?"REACHED":"NOT REACHED")}");
    }

    [Fact]public void HBA_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (Necessary conditions): HBA_01");
        _o.WriteLine("Gate C (Induced fail entry): HBA_01");
        _o.WriteLine("Gate D (Score high, fail persistence): HBA_01");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: Entry-score distributions, class overlap audit.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
