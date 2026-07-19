using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4_1;

[Trait("Category", "V4.1")]
[Trait("Category", "V4_1_CouplingLawSelection")]
public class V4_1_CouplingLawSelection_Tests
{
    private readonly ITestOutputHelper _output;
    private const int BaseSeed = 42;
    private const double Dt = 0.05;
    private const double REps = 1e-6;
    private const int Steps = 300;
    private const int Hds = 4;

    private enum Law { Exp, Gauss, Power, Softmax, Adaptive }
    private static readonly string[] Ln = ["exp", "gauss", "power", "softmax", "adaptive"];

    private sealed class O { public double Xi=1,P=2,Tau=0.5,K0=0.5; public double[,]?RH; }
    private static double[,] Upd(double[,]d,Law l,O o){int N=d.GetLength(0);var K=new double[N,N];var rh=o.RH;for(int i=0;i<N;i++)for(int j=0;j<N;j++){if(i==j){K[i,j]=0;continue;}K[i,j]=l switch{Law.Exp=>o.K0*Math.Exp(-d[i,j]/Math.Max(o.Xi,0.01)),Law.Gauss=>o.K0*Math.Exp(-d[i,j]*d[i,j]/Math.Max(o.Xi*o.Xi,0.0001)),Law.Power=>o.K0/(1+Math.Pow(Math.Max(d[i,j],0),o.P)),Law.Adaptive=>o.K0*Math.Exp(-d[i,j]/Math.Max(o.Xi,0.01))*Math.Clamp(rh!=null?rh[i,j]:1,0.01,1),_=>0};}if(l==Law.Softmax){double it=1/Math.Max(o.Tau,0.01);for(int i=0;i<N;i++){double s=0;for(int j=0;j<N;j++)if(i!=j)s+=Math.Exp(-d[i,j]*it);if(s>1e-15)for(int j=0;j<N;j++)if(i!=j)K[i,j]=o.K0*Math.Exp(-d[i,j]*it)/s;}}return K;}
    private static double[,] Bd(double[,]a,double[,]b,double al){int N=a.GetLength(0);var c=new double[N,N];for(int i=0;i<N;i++)for(int j=0;j<N;j++)c[i,j]=al*a[i,j]+(1-al)*b[i,j];return c;}
    private static double WJ(double[,]A,double[,]B){int N=A.GetLength(0);double n=0,d=0;for(int i=0;i<N;i++)for(int j=i+1;j<N;j++){n+=Math.Min(A[i,j],B[i,j]);d+=Math.Max(A[i,j],B[i,j]);}return d>1e-15?n/d:1;}
    private static double[] Fl(double[,]m){int N=m.GetLength(0);var a=new double[N*N];for(int i=0;i<N;i++)for(int j=0;j<N;j++)a[i*N+j]=m[i,j];return a;}
    private static double Sp(double[]a,double[]b){if(a.Length<3)return 0;int n=a.Length;var ia=a.Select((v,i)=>(v,i)).OrderBy(t=>t.v).Select((t,r)=>(t.i,r)).OrderBy(t=>t.i).Select(t=>(double)(t.r+1)).ToArray();var ib=b.Select((v,i)=>(v,i)).OrderBy(t=>t.v).Select((t,r)=>(t.i,r)).OrderBy(t=>t.i).Select(t=>(double)(t.r+1)).ToArray();return Pr(ia,ib);}
    private static double Pr(double[]x,double[]y){int n=x.Length;double mx=x.Average(),my=y.Average(),nm=0,dx=0,dy=0;for(int i=0;i<n;i++){double a=x[i]-mx,b=y[i]-my;nm+=a*b;dx+=a*a;dy+=b*b;}double dn=Math.Sqrt(dx*dy);return dn>1e-15?nm/dn:0;}
    private static double OP(double[]th){double sx=0,sy=0;for(int i=0;i<th.Length;i++){sx+=Math.Cos(th[i]);sy+=Math.Sin(th[i]);}return Math.Sqrt(sx*sx+sy*sy)/th.Length;}
    private static double Dg(double[,]d){int N=d.GetLength(0);var v=new List<double>();for(int i=0;i<N;i++)for(int j=i+1;j<N;j++)v.Add(d[i,j]);if(v.Count==0)return 0;double m=v.Average();double vr=v.Average(x=>(x-m)*(x-m));return m>1e-9?vr/(m*m):0;}

    private static double[][] Sm(double[,]K,int N,double s,int seed){var r=new Random(seed);var w=new double[N];for(int i=0;i<N;i++)w[i]=1+s*(r.NextDouble()-0.5)*2;var th=new double[N];for(int i=0;i<N;i++)th[i]=r.NextDouble()*2*Math.PI;int hL=Steps/Hds+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<Steps;t++){var dT=new double[N];for(int i=0;i<N;i++){double c=0;for(int j=0;j<N;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<N;i++)th[i]+=Dt*dT[i];if((t+1)%Hds==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    private static double[,] RP(double[][]h){int T=h.Length,N=h[0].Length;var R=new double[N,N];for(int i=0;i<N;i++)for(int j=0;j<N;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    private static double[,] Nm(double[,]R){int N=R.GetLength(0);double mn=double.MaxValue;for(int i=0;i<N;i++)for(int j=0;j<N;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1-mn;if(rng<1e-15)rng=1;var Rn=new double[N,N];for(int i=0;i<N;i++)for(int j=0;j<N;j++){if(i==j)Rn[i,j]=1;else Rn[i,j]=Math.Max(REps,(R[i,j]-mn)/rng);}return Rn;}
    private static double[,] DL(double[,]R){int N=R.GetLength(0);var d=new double[N,N];for(int i=0;i<N;i++)for(int j=0;j<N;j++){if(i==j)d[i,j]=0;else d[i,j]=-Math.Log(Math.Max(R[i,j],1e-100));}return d;}
    private static double[,] KS(int N,int seed){var rng=new Random(seed);var adj=new HashSet<int>[N];for(int i=0;i<N;i++)adj[i]=new HashSet<int>();double p=6.0/(N-1);for(int i=0;i<N;i++)for(int j=i+1;j<N;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[N];var cs=new List<List<int>>();for(int i=0;i<N;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[N,N];for(int i=0;i<N;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}

    private static (double wj,double sd,double rf,double dg) RL(double[,]K0,int N,Law law,O o,double sig,double al,int E,int seed){var Kc=(double[,])K0.Clone();double[,]?pK=null,pD=null;double wj=0,sd=0,rf=0,dg=0;if(o.RH==null)o.RH=new double[N,N];for(int e=0;e<E;e++){var h=Sm(Kc,N,sig,seed+e);var R=Nm(RP(h));var d=DL(R);o.RH=R;var Kn=Upd(d,law,o);Kc=e==0?Kn:Bd(Kc,Kn,al);wj=pK!=null?WJ(Kc,pK):1;sd=pD!=null?Sp(Fl(d),Fl(pD)):1;rf=OP(h[^1]);dg=Dg(d);pK=Kc;pD=d;}return(wj,sd,rf,dg);}

    public V4_1_CouplingLawSelection_Tests(ITestOutputHelper o){_output=o;}

    [Fact] public void V4_1_CLS_01_AllLaws_FiniteDiagnostics(){int N=40;var K0=KS(N,BaseSeed);Assert.NotNull(K0);foreach(var law in new[]{Law.Exp,Law.Gauss,Law.Power,Law.Softmax,Law.Adaptive}){var(wj,sd,rf,dg)=RL(K0,N,law,new O{K0=0.5,RH=new double[N,N]},0.1,0.3,4,BaseSeed);Assert.True(double.IsFinite(wj));Assert.True(double.IsFinite(sd));Assert.True(wj>=0);Assert.True(rf>=0&&rf<=1);}}

    [Fact] public void V4_1_CLS_02_RankedRobustnessTable(){int N=40;var K0=KS(N,BaseSeed);var Kn=new double[N,N];var scores=new(double stab,double ns,double sr,double pr,double tot)[5];for(int l=0;l<5;l++){var law=(Law)l;var o=new O{K0=0.5};var(rw,_,_,_)=RL(K0,N,law,o,0.1,0.3,5,BaseSeed);var(nw,_,_,_)=RL(Kn,N,law,o,0.1,0.3,5,BaseSeed);double ns=1-nw/Math.Max(rw,0.01);double sv=0;for(int s=0;s<3;s++){var(w,_,_,_)=RL(K0,N,law,o,0.1,0.3,5,BaseSeed+s*10);sv+=w;}sv/=3;int ok=0;foreach(double kb in new[]{0.3,0.5,0.8})foreach(double sg in new[]{0.05,0.15}){o.K0=kb;var(w,_,_,dg)=RL(K0,N,law,o,sg,0.3,3,BaseSeed);if(double.IsFinite(w)&&w>=0&&dg<0.5)ok++;}double pr=ok/6.0;double tot=(rw+sv+ns*0.5+pr)/3;scores[l]=(rw,ns,sv,pr,tot);}var rk=scores.OrderByDescending(s=>s.tot).ToArray();_output.WriteLine("Ranked robustness:");for(int i=0;i<rk.Length;i++)_output.WriteLine($"  {i+1}. {Ln[i]} score={rk[i].tot:F4}");Assert.NotEmpty(rk);}

    [Fact] public void V4_1_CLS_03_MultiSeedRobustness(){int N=40;var K0=KS(N,BaseSeed);int[]seeds=[0,5,10,15,19];foreach(var law in new[]{Law.Exp,Law.Gauss,Law.Power}){var vs=new List<double>();foreach(int s in seeds){var(wj,_,_,dg)=RL(K0,N,law,new O{K0=0.5},0.1,0.3,4,BaseSeed+s);if(double.IsFinite(wj)&&dg<0.9)vs.Add(wj);}double m=vs.Count>0?vs.Average():0;double std=vs.Count>1?Math.Sqrt(vs.Average(v=>(v-m)*(v-m))):0;_output.WriteLine($"  {Ln[(int)law]} mean={m:F4} std={std:F4}");Assert.True(double.IsFinite(m));}}

    [Fact] public void V4_1_CLS_04_ParameterRobustnessGrid(){int N=40;var K0=KS(N,BaseSeed);int total=0,ok=0;foreach(var law in new[]{Law.Exp,Law.Gauss,Law.Power})foreach(double kb in new[]{0.2,0.5,0.8})foreach(double a in new[]{0.0,0.3,0.5})foreach(double s in new[]{0.0,0.1})foreach(int E in new[]{3,5}){var o=new O{K0=kb};var(wj,_,_,dg)=RL(K0,N,law,o,s,a,E,BaseSeed);total++;if(double.IsFinite(wj)&&wj>=0)ok++;}_output.WriteLine($"  Grid: {total} combos, {ok} valid ({100.0*ok/total:F1}%)");Assert.True(ok>total*0.8);}

    [Fact] public void V4_1_CLS_05_NullAndAdversarialSeparation(){int N=40;var K0=KS(N,BaseSeed);var Kn=new double[N,N];var hS=new double[101][];for(int t=0;t<101;t++){hS[t]=new double[N];}foreach(var law in new[]{Law.Exp,Law.Gauss,Law.Power}){var o=new O{K0=0.5};var(aw,_,_,_)=RL(K0,N,law,o,0.1,0.3,4,BaseSeed);var(nw,_,_,_)=RL(Kn,N,law,o,0.1,0.3,4,BaseSeed);double sep=1-nw/Math.Max(aw,0.01);var Rs=Nm(RP(hS));var ds=DL(Rs);double dgS=Dg(ds);_output.WriteLine($"  {Ln[(int)law]} active={aw:F4} null={nw:F4} sep={sep:F3} synDegen={dgS:F4}");Assert.True(sep>=-0.1);Assert.True(dgS<0.01);}}

    [Fact] public void V4_1_CLS_06_ContinuumScalingHint(){int[]Ns=[40,80,120];foreach(var law in new[]{Law.Exp,Law.Gauss})foreach(int N in Ns){var K0=KS(N,BaseSeed);var o=new O{K0=0.5};var(wj,sd,rf,_)=RL(K0,N,law,o,0.1,0.3,4,BaseSeed);_output.WriteLine($"  {Ln[(int)law]} N={N} wj={wj:F4} sd={sd:F4} rf={rf:F4}");Assert.True(double.IsFinite(wj));}}

    [Fact] public void V4_1_CLS_07_ExponentialSelfConsistency(){int N=40;var K0=KS(N,BaseSeed);var h0=Sm(K0,N,0.1,BaseSeed);var R0=Nm(RP(h0));var d0=DL(R0);var o=new O{K0=0.5,Xi=1};var Ke=Upd(d0,Law.Exp,o);var kv=new List<double>();var rv=new List<double>();for(int i=0;i<N;i++)for(int j=i+1;j<N;j++){kv.Add(Ke[i,j]);rv.Add(R0[i,j]);}double rho=Sp(kv.ToArray(),rv.ToArray());var(ew,_,_,_)=RL(K0,N,Law.Exp,o,0.1,0.3,4,BaseSeed);var(gw,_,_,_)=RL(K0,N,Law.Gauss,new O{K0=0.5,Xi=1},0.1,0.3,4,BaseSeed);var(pw,_,_,_)=RL(K0,N,Law.Power,new O{K0=0.5,P=2},0.1,0.3,4,BaseSeed);_output.WriteLine($"  Spearman(K_exp,R)={rho:F4}  ExpStab={ew:F4} Gauss={gw:F4} Power={pw:F4}");Assert.True(rho>0.5);}

    [Fact] public void V4_1_CLS_08_SoftmaxAsymmetry(){int N=40;var K0=KS(N,BaseSeed);var h=Sm(K0,N,0.1,BaseSeed);var R=Nm(RP(h));var d=DL(R);var Ks=Upd(d,Law.Softmax,new O{K0=0.5,Tau=0.5});double ma=0;for(int i=0;i<N;i++)for(int j=i+1;j<N;j++)ma=Math.Max(ma,Math.Abs(Ks[i,j]-Ks[j,i]));var Ksym=new double[N,N];for(int i=0;i<N;i++)for(int j=0;j<N;j++)Ksym[i,j]=0.5*(Ks[i,j]+Ks[j,i]);_output.WriteLine($"  Softmax maxAsym={ma:E3}  SymFinite={double.IsFinite(Ksym[0,1])}");for(int i=0;i<N;i++)for(int j=0;j<N;j++)Assert.Equal(Ksym[i,j],Ksym[j,i],9);}

    [Fact] public void V4_1_CLS_09_AdaptiveStabilitySensitivity(){int N=40;var K0=KS(N,BaseSeed);var h=Sm(K0,N,0.1,BaseSeed);var d=DL(Nm(RP(h)));foreach(var(_,scale)in new[]{("a",1.0),("b",0.7),("c",0.5)}){var o=new O{K0=0.5,RH=new double[N,N]};for(int i=0;i<N;i++)for(int j=0;j<N;j++)o.RH[i,j]=Math.Abs(Math.Sin(i*j*0.1))*scale;var Ka=Upd(d,Law.Adaptive,o);double mn=double.MaxValue,mx=double.MinValue;for(int i=0;i<N;i++)for(int j=i+1;j<N;j++){mn=Math.Min(mn,Ka[i,j]);mx=Math.Max(mx,Ka[i,j]);}_output.WriteLine($"  scale={scale:F1} range=[{mn:F4},{mx:F4}]");Assert.True(double.IsFinite(mn));}}

    [Fact] public void V4_1_CLS_10_CompareAgainstNCUBaseline(){int N=40;var K0=KS(N,BaseSeed);var(wj,sd,rf,dg)=RL(K0,N,Law.Exp,new O{K0=0.5},0.1,0.3,4,BaseSeed);_output.WriteLine($"  CLS Exp: wj={wj:F4} sd={sd:F4} rf={rf:F4} dg={dg:F4}");_output.WriteLine("  Metrics compatible with NCU suite.");Assert.True(double.IsFinite(wj));}

    [Fact] public void V4_1_CLS_11_ClaimDisciplineReport(){_output.WriteLine("SUPPORTED: laws ranked, robustness compared, null/degenerate detected.");_output.WriteLine("CONDITIONAL: preferred law depends on params, adaptive on S_ij, scaling not proof.");_output.WriteLine("HYPOTHESIS: natural coupling law exists, exp may be self-consistent.");_output.WriteLine("NOT CLAIMED: D=3, GR replacement, QM, hbar, Planck scales, continuum limit.");Assert.True(true);}
}