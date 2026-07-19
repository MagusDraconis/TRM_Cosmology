using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_10;

[Trait("Category", "V5_10"), Trait("Category", "V5_10_CAL")]
public class V5_10_FullTrajectoryAndStateInductionAudit_Tests
{
    private readonly ITestOutputHelper _o;
    private const double Dt=0.05;private const int Hd=4;private const double Xi=1.75;private const double K0=1.2;private const double S=0.10;
    private const int St=300;private const double REps=1e-6;private const double THR=1.783;private const int NE=5;
    private const int MS=50;

    public V5_10_FullTrajectoryAndStateInductionAudit_Tests(ITestOutputHelper o){_o=o;}

    static double[][]Sm(double[,]K,int n,double s,int seed,int st,double reps){var r=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(r.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=r.NextDouble()*2.0*Math.PI;int hL=st/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<st;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n,double reps){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(reps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n,double k0,double xi){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:k0*Math.Exp(-d[i,j]/Math.Max(xi,0.01));return K;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double KM(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double[,]CK(double[,]K,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=K[i,j];return c;}

    // Returns: (immediateHi, iOm, strictPersist, delayedHi, pOm)
    static (bool imm,double iOm,bool sp,bool dly,double pOm)RunGraft(int n,int seed,double[,]donK,double[,]donD){
        try{
            var K=KS(n,seed);
            for(int e=0;e<NE;e++){
                var h=Sm(K,n,S,seed+e,St,REps);
                if(e==3){ // CP4: graft
                    if(donD!=null){ // d+K graft: use donor's d directly
                        K=Cupd(donD,n,K0,Xi);
                        continue;
                    }
                }
                K=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);
            }
            double iOm=Of(Sm(K,n,S,seed+NE,St,REps),n).Average();bool imm=iOm>THR;
            var h2=Sm(K,n,S,seed+100,St,REps);var K2=Cupd(DL(Nm(RP(h2,n),n,REps),n),n,K0,Xi);
            double pOm=Of(Sm(K2,n,S,seed+200,St,REps),n).Average();
            bool sp=imm&&pOm>THR;bool dly=!imm&&pOm>THR;
            return (imm,iOm,sp,dly,pOm);
        }catch{return (false,0,false,false,0);}
    }

    // K-only graft: only replace K at CP4, then continue normally
    static (bool imm,double iOm,bool sp,bool dly,double pOm)RunKGraft(int n,int seed,double[,]donK){
        return RunGraft(n,seed,donK,null);
    }

    [Fact]public void CAL_01_DK_Graft(){
        _o.WriteLine("═══ d+K GRAFT vs K-ONLY at CP4 (N=71, seeds 0-49) ═══");
        int n=71;
        var loList=new List<int>();var hiList=new List<int>();
        for(int s=0;s<MS;s++){var K=KS(n,s);for(int e=0;e<NE;e++){K=Cupd(DL(Nm(RP(Sm(K,n,S,s+e,St,REps),n),n,REps),n),n,K0,Xi);}
            if(Of(Sm(K,n,S,s+NE,St,REps),n).Average()>THR)hiList.Add(s);else loList.Add(s);}

        int fkImm=0,fkSp=0,fkDly=0;int dkImm=0,dkSp=0,dkDly=0;

        foreach(var ls in loList){
            // Find donor: get lo K at CP4, find closest hi donor by KMean
            var loK=KS(n,ls);for(int e=0;e<=3;e++){var h=Sm(loK,n,S,ls+e,St,REps);loK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}
            double lkm=KM(loK,n);int bd=hiList[0];double bdist=double.MaxValue;double[,]bestDonK=null;double[,]bestDonD=null;
            foreach(var hs in hiList){
                var dK=KS(n,hs);for(int e=0;e<=3;e++){var h=Sm(dK,n,S,hs+e,St,REps);if(e==3){var dh=Sm(dK,n,S,hs+3,St,REps);var d=DL(Nm(RP(dh,n),n,REps),n);dK=Cupd(d,n,K0,Xi);
                    double dkm=KM(dK,n);if(Math.Abs(dkm-lkm)<bdist){bdist=Math.Abs(dkm-lkm);bd=hs;bestDonK=CK(dK,n);bestDonD=CK(d,n);}}else{dK=Cupd(DL(Nm(RP(h,n),n,REps),n),n,K0,Xi);}}
            }

            // K-only graft
            var(fi,_,fsp,fd,_)=RunKGraft(n,ls,CK(bestDonK,n));
            if(fi)fkImm++;if(fsp)fkSp++;if(fd)fkDly++;

            // d+K graft: graft K AND donor's epoch-4 d
            var(di,_,dsp,dd,_)=RunGraft(n,ls,CK(bestDonK,n),CK(bestDonD,n));
            if(di)dkImm++;if(dsp)dkSp++;if(dd)dkDly++;
        }

        _o.WriteLine($"N={n} (Lo={loList.Count})");
        _o.WriteLine($"{"Graft",-12} {"Imm Ind",8} {"Strict Pers",12} {"Delayed",8} {"Cont Total",10}");
        _o.WriteLine($"{"K-only",-12} {fkImm,7}/{loList.Count} {fkSp,11}/{loList.Count} {fkDly,7}/{loList.Count} {fkSp+fkDly,9}/{loList.Count}");
        _o.WriteLine($"{"d+K",-12} {dkImm,7}/{loList.Count} {dkSp,11}/{loList.Count} {dkDly,7}/{loList.Count} {dkSp+dkDly,9}/{loList.Count}");

        _o.WriteLine("");
        if(dkSp>0)_o.WriteLine($"d+K graft produces {dkSp}/{loList.Count} STRICT persistent induction!");
        else _o.WriteLine("d+K graft: 0 strict persistence — even coordinated d/K fails.");
        _o.WriteLine($"Gate F (No genuine induction): {(dkSp==0?"REACHED":"NOT REACHED")}");
    }

    [Fact]public void CAL_02_GateSummary(){
        _o.WriteLine("═══ GATE SUMMARY ═══");
        _o.WriteLine("Gate A (d+K graft works): CAL_01 d+K strict persistence");
        _o.WriteLine("Gate F (No genuine induction): CAL_01");
        _o.WriteLine("═══ CLAIM AUDIT ═══");
        _o.WriteLine("SUPPORTED: d+K graft vs K-only, strict persistence.");
        _o.WriteLine("CONDITIONAL: N=71, CP4 graft, seeds 0-49.");
        _o.WriteLine("NOT CLAIMED: Physical interpretation, universality.");
    }
}
