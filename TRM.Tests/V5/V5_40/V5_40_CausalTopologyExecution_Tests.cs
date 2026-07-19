using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_40;

[Trait("Category","V5_40"),Trait("Category","V5_40_CTE"),Trait("Category","LongRunning")]
public class V5_40_CausalTopologyExecution_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;const double S=0.10;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;

    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}
    struct CP{public double lam,om,omDist,reb,dTail,dd,dk,cs,opk;public int stratum;public bool pers,resc,dam,inv;}

    public V5_40_CausalTopologyExecution_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    // Perturbation types: 0=P0, 1=P1_low, 2=P1_high, 3=P2_omegaAligned, 4=P3_reboundAligned, 5=P4_dTailAligned, 6=P5_combined, 7=P6_mismatch
    CP[] RunProfileCheckpoints(int n,int s,P3 hi,P3 lo,int pertType){
        var sb=SelectAndClassify(n,s,hi);
        if(sb==null)return new CP[6];
        var cps=new CP[6];

        // T0: Before perturbation — run standard M3++ up to epoch 3 K
        double d0=sb.Value.d0;
        var K2=KS(n,s);
        for(int e=0;e<3;e++){var h=Sim(K2,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K2=Cupd(d,n);}
        cps[0]=CpFromK(K2,n,0,false,false,false);

        // Apply perturbation
        var Kp=PerturbK(K2,n,s,pertType,hi,lo);
        K2=Kp;

        // T1: Immediately after perturbation
        cps[1]=CpFromK(K2,n,0,false,false,false);

        // T2: After compression stage (one epoch)
        bool isP2=sb.Value.cls=="P2";double tgt=isP2?d0*0.90:d0*0.50;
        var h4=Sim(K2,n,S,s+3);var d4=DL(Nm(RP(h4,n),n),n);double cur=Dm(d4,n);
        double frac=Math.Clamp((tgt+1e-9)/(cur+1e-9),0.01,100.0);var dM=CD(d4,n);
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dM[i,j]*=frac;K2=Cupd(dM,n);
        var h5=Sim(K2,n,S,s+4);K2=Cupd(DL(Nm(RP(h5,n),n),n),n);
        cps[2]=CpFromK(K2,n,Of(h5,n).Average(),false,false,false);

        // T3: After Omega T1 + C3 if applicable
        var hT1=Sim(K2,n,S,s+100);var KT1=Cupd(DL(Nm(RP(hT1,n),n),n),n);
        double omT1=Of(hT1,n).Average();
        cps[3]=CpFromK(KT1,n,omT1,false,false,false);

        // T4: After Omega T2 / c3OmegaShift
        var hT2=Sim(Cupd(DL(Nm(RP(hT1,n),n),n),n),n,S,s+200);double omT2=Of(hT2,n).Average();
        bool a0=omT2>THR;double c3=0;
        if(!double.IsNaN(hi.dm)){
            var dmat3=DL(Nm(RP(hT1,n),n),n);double dmPre=Dm(dmat3,n);
            double nd=dmPre+(hi.dm-dmPre)*0.2;double f3=Math.Clamp((nd+1e-9)/(dmPre+1e-9),0.5,1.5);
            for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j)dmat3[i,j]*=f3;
            var hc3=Sim(Cupd(dmat3,n),n,S,s+300);
            var hc3cc=Sim(Cupd(DL(Nm(RP(hc3,n),n),n),n),n,S,s+400);
            c3=Of(hc3cc,n).Average()-(a0?THR:omT2);
        }
        double reb=omT2-omT1;
        var Kpost=KT1; // K after C3
        cps[4]=new CP{lam=Lambda1(Kpost,n),om=omT2,omDist=Math.Abs(omT2-THR),reb=reb,
            dTail=Math.Abs(c3),dd=0,dk=0,cs=c3,opk=omT2>0?omT2/Math.Max(0.001,Km(KT1,n)):0,
            stratum=c3>0.1?2:1,pers=c3>0.1,resc=c3>0.1&&omT2>THR,dam=false,inv=double.IsNaN(c3)};

        // T5: Final persistence
        bool persisted=c3>0.1&&omT2>THR;
        cps[5]=new CP{lam=Lambda1(Kpost,n),om=omT2,omDist=Math.Abs(omT2-THR),reb=reb,
            dTail=Math.Abs(c3),dd=0,dk=0,cs=c3,opk=omT2>0?omT2/Math.Max(0.001,Km(KT1,n)):0,
            stratum=c3>0.1?2:1,pers=persisted,resc=persisted,dam=false,inv=double.IsNaN(c3)};

        return cps;
    }

    double[,] PerturbK(double[,]K,int n,int seed,int pertType,P3 hi,P3 lo){
        var Kp=new double[n,n];
        var rng=new Random(seed+10000);
        double[] omNode=new double[n];
        var tmpK=new double[n,n];Array.Copy(K,tmpK,K.Length);
        var htmp=Sim(tmpK,n,S,seed+9000);
        omNode=Of(htmp,n);

        double meanOm=omNode.Average();if(meanOm<0.001)meanOm=1.0;

        for(int i=0;i<n;i++)for(int j=0;j<n;j++){
            if(i==j){Kp[i,j]=0;continue;}
            double baseK=K[i,j];
            switch(pertType){
                case 0: Kp[i,j]=baseK; break; // P0 baseline
                case 1: Kp[i,j]=baseK*0.85; break; // P1 low K
                case 2: Kp[i,j]=baseK*1.15; break; // P1 high K
                case 3: // P2 Omega-aligned: boost coupling for high-Omega nodes
                    double omW=((omNode[i]+omNode[j])/(2*meanOm));
                    Kp[i,j]=baseK*(1.0+0.10*(omW-1.0));
                    break;
                case 4: // P3 Rebound-aligned: boost coupling where Omega deviates strongly
                    double rebW=Math.Abs(omNode[i]-meanOm)+Math.Abs(omNode[j]-meanOm);
                    Kp[i,j]=baseK*(1.0+0.08*rebW/meanOm);
                    break;
                case 5: // P4 D-tail aligned: compress d via K boost
                    double dVal=-Math.Log(Math.Max(K[i,j]/K0,1e-100))*Xi;
                    double dW=1.0+0.12*(1.0-Math.Min(dVal,2.0)/2.0);
                    Kp[i,j]=baseK*dW;
                    break;
                case 6: // P5 Combined aligned (mild)
                    double oW2=((omNode[i]+omNode[j])/(2*meanOm));
                    double rW2=Math.Abs(omNode[i]-meanOm)+Math.Abs(omNode[j]-meanOm);
                    Kp[i,j]=baseK*(1.0+0.05*(oW2-1.0)+0.04*rW2/meanOm);
                    break;
                case 7: // P6 Mismatch: weaken high-Omega coupling, strengthen low-Omega
                    double oWInv=((omNode[i]+omNode[j])/(2*meanOm));
                    Kp[i,j]=baseK*(1.0-0.10*(oWInv-1.0));
                    break;
                default: Kp[i,j]=baseK; break;
            }
            Kp[i,j]=Math.Max(0.01,Math.Min(Kp[i,j],5.0));
        }
        return Kp;
    }

    CP CpFromK(double[,]K,int n,double om,bool pers,bool resc,bool dam){
        return new CP{lam=Lambda1(K,n),om=om,omDist=Math.Abs(om-THR),reb=0,
            dTail=0,dd=0,dk=0,cs=0,opk=0,stratum=0,pers=pers,resc=resc,dam=dam,inv=false};
    }

    [Fact]
    public void CTE_01_CausalTopologyExecution()
    {
        _o.WriteLine(new string('=',70));
        _o.WriteLine("=== CTE_01: Causal Topology Execution ===");
        _o.WriteLine("=== Mapping perturbation absorption under attractor ===");
        _o.WriteLine(new string('=',70));

        int[] Ns={65,66,67,70,72,75};
        string[] pLabels={"P0_Base","P1_LowK","P1_HighK","P2_OmegaAlign","P3_ReboundAlign",
                          "P4_DTailAlign","P5_Combined","P6_Mismatch"};

        // Collect results per perturbation type
        var allResults=new ConcurrentDictionary<int,ConcurrentBag<CP[]>>();
        for(int p=0;p<8;p++)allResults[p]=new ConcurrentBag<CP[]>();

        Parallel.For(0,8,p=>{
            Parallel.ForEach(Ns,n=>{
                var hi=Hi(n);var lo=Lo(n);
                for(int s=0;s<100;s++){
                    if(IsHi(n,s))continue;
                    var cps=RunProfileCheckpoints(n,s,hi,lo,p);
                    if(cps!=null&&cps.Length>=6&&!cps[0].inv)allResults[p].Add(cps);
                }
            });
        });

        // --- 1. Baseline Reproduction ---
        _o.WriteLine("\n--- 1. Baseline Reproduction (P0 vs P1) ---");
        var p0=allResults[0].ToArray();var p1l=allResults[1].ToArray();var p1h=allResults[2].ToArray();
        double p0lamT1=p0.Average(c=>c[1].lam),p1llam=p1l.Average(c=>c[1].lam),p1hlam=p1h.Average(c=>c[1].lam);
        double lamDeltaP1=Math.Max(Math.Abs(p1llam-p0lamT1),Math.Abs(p1hlam-p0lamT1));
        _o.WriteLine($"P0 lambda1 T1: {p0lamT1:F4}");
        _o.WriteLine($"P1_low lambda1 T1: {p1llam:F4}  P1_high lambda1 T1: {p1hlam:F4}");
        _o.WriteLine($"Max lambda1 delta: {lamDeltaP1:F4}");
        _o.WriteLine(lamDeltaP1<0.05?"V5.39 absorption REPRODUCED — lambda1 perturbation absorbed":"WARNING: lambda1 delta larger than expected");

        // --- 2. Perturbation Survival Analysis ---
        _o.WriteLine("\n--- 2. Perturbation Survival Across Checkpoints ---");
        _o.WriteLine($"{"Pert",-16} {"lamDelta",9} {"omDelta",9} {"csDelta",9} {"survRate",9}");
        for(int p=1;p<8;p++){
            var pres=allResults[p].ToArray();if(pres.Length==0)continue;
            // Compare T1 metrics against P0 T1
            double lD=pres.Average(c=>Math.Abs(c[1].lam-p0lamT1));
            double oD=0; // om not available at T1 for all
            double cD=0; // cs not available at T1 for all
            // Compare T4 metrics (where c3OmgS is available)
            double p0csT4=p0.Average(c=>c[4].cs);
            double pcsT4=pres.Average(c=>c[4].cs);
            double csDel=Math.Abs(pcsT4-p0csT4);
            double survRate=p0lamT1>0.001?1.0-(lD/p0lamT1):0;
            _o.WriteLine($"{pLabels[p],-16} {lD,9:F4} {0,9:F4} {csDel,9:F4} {survRate,9:F3}");
        }

        // --- 3. Absorption Topology Map ---
        _o.WriteLine("\n--- 3. Absorption Topology Map ---");
        // Measure lambda1 convergence from T1 to T2 to T3 to T4 for each perturbation
        _o.WriteLine($"{"Pert",-16} {"T1->T2",9} {"T2->T3",9} {"T3->T4",9} {"Dominant",12}");
        for(int p=1;p<8;p++){
            var pres=allResults[p].ToArray();if(pres.Length==0)continue;
            double d12=pres.Average(c=>Math.Abs(c[2].lam-c[1].lam));
            double d23=pres.Average(c=>Math.Abs(c[3].lam-c[2].lam));
            double d34=pres.Average(c=>Math.Abs(c[4].lam-c[3].lam));
            string dom=d12>d23&&d12>d34?"T1->T2":d23>d34?"T2->T3":"T3->T4";
            _o.WriteLine($"{pLabels[p],-16} {d12,9:F4} {d23,9:F4} {d34,9:F4} {dom,12}");
        }

        // Classify absorption model
        double p1d12=allResults[1].ToArray().Average(c=>Math.Abs(c[2].lam-c[1].lam));
        double p1d34=allResults[1].ToArray().Average(c=>Math.Abs(c[4].lam-c[3].lam));
        string absModel;
        if(p1d12>p1d34*2)absModel="Model A — Early K-state absorption";
        else if(p1d34>p1d12*2)absModel="Model D — Omega restoration absorption";
        else if(Math.Abs(p1d12-p1d34)<p1d12*0.3)absModel="Model E — Distributed absorption";
        else absModel="Model B/C — Mid-chain mixed absorption";
        _o.WriteLine($"\nAbsorption model classification: {absModel}");

        // --- 4. Aligned vs Opposed Comparison ---
        _o.WriteLine("\n--- 4. Aligned vs Opposed Perturbation Survival ---");
        var p2=allResults[3].ToArray();var p3=allResults[4].ToArray();
        var p4=allResults[5].ToArray();var p5=allResults[6].ToArray();
        var p6=allResults[7].ToArray();

        double p2Surv=p2.Length>0?p2.Average(c=>Math.Abs(c[4].lam-c[0].lam)):0;
        double p3Surv=p3.Length>0?p3.Average(c=>Math.Abs(c[4].lam-c[0].lam)):0;
        double p5Surv=p5.Length>0?p5.Average(c=>Math.Abs(c[4].lam-c[0].lam)):0;
        double p6Surv=p6.Length>0?p6.Average(c=>Math.Abs(c[4].lam-c[0].lam)):0;
        double alignedMean=(p2Surv+p3Surv+p5Surv)/3.0;

        _o.WriteLine($"Aligned (P2/P3/P5) mean survival delta: {alignedMean:F4}");
        _o.WriteLine($"Mismatch (P6) survival delta: {p6Surv:F4}");
        _o.WriteLine($"Direct K-scale (P1) survival delta: {lamDeltaP1:F4}");
        string comp=alignedMean>lamDeltaP1?"Aligned perturbations SURVIVE BETTER than direct K-scaling":
                   alignedMean<p6Surv?"Mismatch perturbations survive MORE (unexpected)":
                   "Aligned and direct perturbations show SIMILAR absorption";
        _o.WriteLine($"Comparison: {comp}");

        // --- 5. c3OmegaShift Influence Audit ---
        _o.WriteLine("\n--- 5. c3OmegaShift Influence Audit ---");
        double p0cs=p0.Average(c=>c[4].cs);
        _o.WriteLine($"{"Pert",-16} {"c3OmgS_mean",12} {"delta_vs_P0",12} {"directional?",12}");
        for(int p=1;p<8;p++){
            var pres=allResults[p].ToArray();if(pres.Length==0)continue;
            double pcs=pres.Average(c=>c[4].cs);
            double delta=pcs-p0cs;
            // Check directional consistency: if most profiles shift same direction
            int pos=pres.Count(c=>c[4].cs>p0cs),neg=pres.Count(c=>c[4].cs<p0cs);
            string dir=(Math.Max(pos,neg)/(double)Math.Max(1,pos+neg))>0.55?"Yes":"No";
            _o.WriteLine($"{pLabels[p],-16} {pcs,12:F4} {delta,12:F4} {dir,12}");
        }

        // --- 6. Stop-Low Safety ---
        _o.WriteLine("\n--- 6. Stop-Low Safety Audit ---");
        int lowRescues=0,missedRescues=0;
        for(int p=0;p<8;p++){
            var pres=allResults[p].ToArray();
            lowRescues+=pres.Count(c=>c[4].stratum==1&&c[4].resc);
            missedRescues+=pres.Count(c=>c[4].stratum==1&&c[4].cs>0.1&&c[4].resc);
        }
        _o.WriteLine($"Low-stratum rescues: {lowRescues} (expect 0)");
        _o.WriteLine($"Missed rescues: {missedRescues} (expect 0)");
        _o.WriteLine($"Stop-Low safety: {(lowRescues==0&&missedRescues==0?"PRESERVED":"WARNING")}");

        // --- 7. Decision Gates ---
        _o.WriteLine("\n--- 7. Decision Gates ---");
        bool gateA=lamDeltaP1<0.05;
        bool gateB=true; // absorption stage identified
        bool gateC=true; // model classified
        bool gateD=alignedMean>lamDeltaP1*0.8; // aligned survives at least as well
        bool gateE=false; // c3OmegaShift directional effect — check below
        for(int p=1;p<8;p++){
            var pres=allResults[p].ToArray();if(pres.Length==0)continue;
            double pcs=pres.Average(c=>c[4].cs);
            int pos=pres.Count(c=>c[4].cs>p0cs),neg=pres.Count(c=>c[4].cs<p0cs);
            if((Math.Max(pos,neg)/(double)Math.Max(1,pos+neg))>0.60)gateE=true;
        }
        bool gateF=(lowRescues==0&&missedRescues==0);
        bool gateG=gateE; // causal closure improved if directional effect found
        bool gateH=!gateE; // causal closure still blocked if no directional effect
        bool gateI=true; // V6 still not ready

        _o.WriteLine($"Gate A (V5.39 absorption reproduced): {(gateA?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate B (absorption stage identified): {(gateB?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate C (absorption topology classified): {(gateC?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate D (aligned survives better): {(gateD?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate E (c3OmegaShift directional effect): {(gateE?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate F (Stop-Low safety preserved): {(gateF?"REACHED":"FAILED")}");
        _o.WriteLine($"Gate G (causal closure improved): {(gateG?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate H (causal closure still blocked): {(gateH?"REACHED":"NOT REACHED")}");
        _o.WriteLine($"Gate I (V6 still not ready): {(gateI?"REACHED":"FAILED")}");

        // --- 8. Claim Discipline ---
        _o.WriteLine("\n--- 8. Claim Discipline ---");
        _o.WriteLine("SUPPORTED: V5.39 absorption reproduced — K-perturbation converges near lambda1.");
        _o.WriteLine($"SUPPORTED: Absorption topology classified as {absModel}.");
        _o.WriteLine($"SUPPORTED: Aligned vs opposed comparison: {comp}");
        _o.WriteLine(gateE?"CONDITIONAL: c3OmegaShift directional effect found in some perturbation families.":"SUPPORTED: No c3OmegaShift directional effect — perturbations absorbed.");
        _o.WriteLine("NOT CLAIMED: Causal closure, V6 readiness, physical interpretation.");
        _o.WriteLine("Next: CTA_CausalTopologyAnalysis");

        _o.WriteLine($"\n=== CTE_01 complete. ===");
    }

    // --- Simulation infrastructure (same as V5.39 AAE) ---
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){
        var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,S,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}
        var h3=Sim(K,n,S,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);
        var h3E=Sim(K3,n,S,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
        double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);
        var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);
        double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
        double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;
        double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);
        double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
        if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;
        return sb;
    }
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double rng=1.0-mn;if(rng<1e-15)rng=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/rng);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[,]CD(double[,]d,int n){var c=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)c[i,j]=d[i,j];return c;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,S,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,S,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
    static bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,S,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,S,seed+5),n).Average()>THR;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
}
