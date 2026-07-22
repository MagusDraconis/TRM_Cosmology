using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_59;

[Trait("Category","V5_59"),Trait("Category","V5_59_KOR"),Trait("Category","LongRunning")]
public class V5_59_KernelOriginAudit_Tests
{
    private readonly ITestOutputHelper _o;
    const double Dt=0.05;const int Hd=4;const double Xi=1.75;const double K0=1.2;
    const int St=300;const double REps=1e-9;const double THR=1.783;
    const double PHV=-0.3281,OTH=0.0153;
    struct P3{public double dm,km,ks;}
    struct SBase{public int seed;public double d0,km0,ks0;public string cls;}

    public V5_59_KernelOriginAudit_Tests(ITestOutputHelper o){_o=o;}
    static ConcurrentDictionary<int,P3>? _hC,_lC;
    P3 Hi(int n){_hC??=new();return _hC.GetOrAdd(n,k=>PCent(k,true));}
    P3 Lo(int n){_lC??=new();return _lC.GetOrAdd(n,k=>PCent(k,false));}

    [Fact]
    public void KOR_01_KernelOriginAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== KOR_01: Kernel Origin Audit ===");
        _o.WriteLine("=== V5.59. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is km primitive or generated? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        // Store: N,seed,kmInit,km1,km2,kmFinal,d0,rawIQR,cls
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,double,string)>();
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s); // Initial coupling
                double kmInit=Km(K,n); // Before any SAC epoch

                // Epoch 1
                var h1=Sim(K,n,0.10,s);K=Cupd(DL(Nm(RP(h1,n),n),n),n);double km1=Km(K,n);
                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);K=Cupd(DL(Nm(RP(h2,n),n),n),n);double km2=Km(K,n);
                // Epoch 3
                var h3=Sim(K,n,0.10,s+3);K=Cupd(DL(Nm(RP(h3,n),n),n),n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),kmFinal=Km(K3E,n);

                var sb=new SBase{seed=s,d0=d0,km0=kmFinal,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(kmFinal-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(kmFinal-Lo(n).km)*(kmFinal-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,kmInit,km1,km2,kmFinal,d0,rawIQR,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item9=="P1").ToArray();var p1b=bd.Where(d=>d.Item9=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length}");

        double eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // ============================================================
        // PART A+B — km genesis trace
        // ============================================================
        _o.WriteLine($"\n=== PARTS A+B: km Genesis Trace ===");
        _o.WriteLine($"{"Stage",-12} {"P1",8} {"P1b",8} {"Sep",8} {"Eff(σ)",8} {"r(kmF)",8}");
        _o.WriteLine(new string('-',60));

        var kmFA=bd.Select(d=>d.Item6).ToArray();var kmFP=p1.Select(d=>d.Item6).ToArray();var kmFPb=p1b.Select(d=>d.Item6).ToArray();
        void VV(string n,double[] ap,double[] ab,double[] aa){
            double e=eff(ap,ab,aa),rk=Pearson(aa,kmFA);
            _o.WriteLine($"{n,-12} {ap.Average(),8:F4} {ab.Average(),8:F4} {Math.Abs(ap.Average()-ab.Average()),8:F5} {e,8:F3}σ {rk,8:F3}");
        }
        VV("km_init",p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),bd.Select(d=>d.Item3).ToArray());
        VV("km_epoch1",p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),bd.Select(d=>d.Item4).ToArray());
        VV("km_epoch2",p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),bd.Select(d=>d.Item5).ToArray());
        VV("km_final",kmFP,kmFPb,kmFA);
        VV("d0",p1.Select(d=>d.Item7).ToArray(),p1b.Select(d=>d.Item7).ToArray(),bd.Select(d=>d.Item7).ToArray());
        VV("rawIQR",p1.Select(d=>d.Item8).ToArray(),p1b.Select(d=>d.Item8).ToArray(),bd.Select(d=>d.Item8).ToArray());

        // ============================================================
        // PART C — Counterfactual: does initial K already separate?
        // ============================================================
        _o.WriteLine($"\n=== PART C: Does Initial K Already Separate? ===");
        double kmInitE=eff(p1.Select(d=>d.Item3).ToArray(),p1b.Select(d=>d.Item3).ToArray(),bd.Select(d=>d.Item3).ToArray());
        double kmInitR=Pearson(bd.Select(d=>d.Item3).ToArray(),kmFA);
        _o.WriteLine($"km_init effect: {kmInitE:F3}σ, r(km_final)={kmInitR:F3}");
        _o.WriteLine($"Initial K (before any SAC epoch): {(kmInitE>0.3?"ALREADY CONTAINS signal":"NO signal — SAC CREATES it")}");

        // ============================================================
        // PART D — Compression: can km be reduced?
        // ============================================================
        _o.WriteLine($"\n=== PART D: Compression Audit ===");
        // km and km1-km2 correlations — how redundant across epochs?
        double r01=Pearson(bd.Select(d=>d.Item3).ToArray(),bd.Select(d=>d.Item4).ToArray());
        double r12=Pearson(bd.Select(d=>d.Item4).ToArray(),bd.Select(d=>d.Item5).ToArray());
        double r2F=Pearson(bd.Select(d=>d.Item5).ToArray(),kmFA);
        _o.WriteLine($"km correlation chain: init→1={r01:F3}, 1→2={r12:F3}, 2→final={r2F:F3}");
        _o.WriteLine($"Compression: {(r2F>0.95?"km_epoch2 ≈ km_final — 2 epochs sufficient":"3 epochs needed for full signal")}");

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision Model ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        string r;
        if(kmInitE<0.3&&kmInitR<0.3)r="Model B: km is GENERATED by SAC operations. Initial K carries no P1/P1b signal.";
        else if(kmInitE>0.5)r="Model A: km is PRIMITIVE. Initial coupling already separates P1/P1b.";
        else r="Model D: Unresolved.";

        _o.WriteLine($"Decision: {r}");
        _o.WriteLine($"Evidence: km_init={kmInitE:F3}σ, r(init,final)={kmInitR:F3}, epoch growth: {kmInitE:F3}→{eff(p1.Select(d=>d.Item4).ToArray(),p1b.Select(d=>d.Item4).ToArray(),bd.Select(d=>d.Item4).ToArray()):F3}→{eff(p1.Select(d=>d.Item5).ToArray(),p1b.Select(d=>d.Item5).ToArray(),bd.Select(d=>d.Item5).ToArray()):F3}→{eff(kmFP,kmFPb,kmFA):F3}σ");
        _o.WriteLine("CLAIMS: Kernel origin audited. Diagnostic only. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== KOR_01 complete. Commit: KOR_01_KernelOriginAudit ===");
    }

    [Fact]
    public void CRIT_01_KernelFalsificationAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== CRIT_01: Kernel Falsification Audit ===");
        _o.WriteLine("=== V5.59. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Goal: BREAK the claim that km is fundamental ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        var bag=new ConcurrentBag<(int,int,double,double,double,double,double,double,double,double,string)>();
        // N,seed,kmInit,km1,km2,kmFinal,d0,rawIQR,dMean,phaseVar,cls
        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);double kmInit=Km(K,n);

                // Epoch 1 — also capture phase-diff variance
                var h1=Sim(K,n,0.10,s);
                double pVar=0;int pCnt=0;
                for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){
                    if(!(K[i,j]>0.001))continue;
                    double mn=0;for(int t=0;t<h1.Length;t++)mn+=h1[t][i]-h1[t][j];
                    mn/=h1.Length;double vr=0;
                    for(int t=0;t<h1.Length;t++){double dd=h1[t][i]-h1[t][j]-mn;vr+=dd*dd;}
                    pVar+=vr/h1.Length;pCnt++;
                }
                double phaseVar=pCnt>0?pVar/pCnt:0;

                K=Cupd(DL(Nm(RP(h1,n),n),n),n);double km1=Km(K,n);

                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);K=Cupd(DL(Nm(RP(h2,n),n),n),n);double km2=Km(K,n);

                // Epoch 3 + final
                var h3=Sim(K,n,0.10,s+3);K=Cupd(DL(Nm(RP(h3,n),n),n),n);
                var h3E=Sim(K,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);
                double d0=Dm(d3E,n),kmFinal=Km(K3E,n),dMean=Dm(DL(Nm(RP(h3E,n),n),n),n);

                var sb=new SBase{seed=s,d0=d0,km0=kmFinal,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(kmFinal-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(kmFinal-Lo(n).km)*(kmFinal-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,kmInit,km1,km2,kmFinal,d0,rawIQR,dMean,phaseVar,sb.cls));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.Item11=="P1").ToArray();var p1b=bd.Where(d=>d.Item11=="P1b").ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length} (from {sds*Ns.Length} profiles)");

        double Eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }
        double CohensD(double[] a,double[] b){
            double ma=a.Average(),mb=b.Average(),na=a.Length,nb=b.Length;
            double va=a.Sum(v=>(v-ma)*(v-ma))/(na-1),vb=b.Sum(v=>(v-mb)*(v-mb))/(nb-1);
            double sp=Math.Sqrt(((na-1)*va+(nb-1)*vb)/(na+nb-2));
            return sp>0.001?Math.Abs(ma-mb)/sp:0;
        }

        var kmF=bd.Select(d=>d.Item6).ToArray();
        var kmFP=p1.Select(d=>d.Item6).ToArray();var kmFPb=p1b.Select(d=>d.Item6).ToArray();
        var d0A=bd.Select(d=>d.Item7).ToArray();
        var iqrA=bd.Select(d=>d.Item8).ToArray();
        var dmA=bd.Select(d=>d.Item9).ToArray();
        var pvA=bd.Select(d=>d.Item10).ToArray();

        // ============================================================
        // PART A — Hidden Predecessor Search
        // ============================================================
        _o.WriteLine($"\n=== PART A: Hidden Predecessor Search ===");
        _o.WriteLine($"Can ANY pre-km variable reconstruct km?");
        _o.WriteLine($"{"Variable",-14} {"r(km)",8} {"Eff(σ)",8} {"Reconstruct?",14}");
        _o.WriteLine(new string('-',48));

        // Pre-km candidates: rawIQR, phaseVar, km_init, d_mean_epoch1, rawMean
        var rawMean=bd.Select(d=>{
            var rng=new Random(d.Item2);var w=new double[72];
            for(int i=0;i<72;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
            return w.Average();
        }).ToArray();
        var rawStd=bd.Select(d=>{
            var rng=new Random(d.Item2);var w=new double[72];
            for(int i=0;i<72;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
            return Sd(w);
        }).ToArray();
        var kmInitV=bd.Select(d=>d.Item3).ToArray();

        // Pre-map P1 and P1b indices for efficient access
        var p1Idxs=p1.Select(d=>Array.FindIndex(bd,x=>x.Item2==d.Item2)).ToArray();
        var p1bIdxs=p1b.Select(d=>Array.FindIndex(bd,x=>x.Item2==d.Item2)).ToArray();

        void HPS(string name,double[] x){
            double r=Pearson(x,kmF);
            var p1x=p1Idxs.Select(i=>x[i]).ToArray();
            var p1bx=p1bIdxs.Select(i=>x[i]).ToArray();
            double e=Eff(p1x,p1bx,x);
            string recon=r>0.8?"YES":r>0.4?"PARTIAL":"NO";
            _o.WriteLine($"{name,-14} {r,8:F3} {e,8:F3}σ {recon,14}");
        }
        HPS("rawIQR",iqrA);
        HPS("rawMean",rawMean);
        HPS("rawStd",rawStd);
        HPS("phaseVar",pvA);
        HPS("km_init",kmInitV);

        // Multiple regression: can linear combo of predecessors predict km?
        // Test: km ~ a*rawIQR + b*phaseVar + c*km_init
        _o.WriteLine($"\n--- Linear Reconstruction ---");
        double bestR=double.MinValue,bestA=0,bestB=0,bestC=0;
        for(int ai=0;ai<=5;ai++)for(int bi=0;bi<=5;bi++){
            double a=ai/5.0,b=bi/5.0,c=1-a-b;
            if(c<0||c>1)continue;
            var pred=iqrA.Zip(pvA,(iq,pv)=>a*iq+b*pv).Zip(kmInitV,(ab,ki)=>ab+c*ki).ToArray();
            double rk=Pearson(pred,kmF);
            if(rk>bestR){bestR=rk;bestA=a;bestB=b;bestC=c;}
        }
        _o.WriteLine($"Best linear combo: {bestA:F2}·rawIQR + {bestB:F2}·phaseVar + {bestC:F2}·km_init");
        _o.WriteLine($"r(predicted, km) = {bestR:F3}");
        _o.WriteLine($"Prediction quality: {(bestR>0.8?"km is RECONSTRUCTIBLE — NOT fundamental":"km CANNOT be reconstructed — likely fundamental")}");

        // ============================================================
        // PART B — Compression Test
        // ============================================================
        _o.WriteLine($"\n=== PART B: Compression Test ===");
        _o.WriteLine($"Can km be REPLACED by a simpler representation?");

        // Test 1: km vs first PC of (rawIQR, phaseVar, km_init, dMean)
        var allVars=new[]{iqrA,pvA,kmInitV,rawMean,dmA};
        int nVars=allVars.Length;int nPts=bd.Length;
        // Compute correlation-based PC1 via SVD-like max-variance direction
        double pc1R=0;int pc1Idx=0;
        for(int vi=0;vi<nVars;vi++){
            double r=Pearson(allVars[vi],kmF);
            if(Math.Abs(r)>Math.Abs(pc1R)){pc1R=r;pc1Idx=vi;}
        }
        _o.WriteLine($"Strongest single-variable predictor: r={pc1R:F3} ({new[]{"rawIQR","phaseVar","km_init","rawMean","dMean"}[pc1Idx]})");

        // Test 2: km vs km_init (initial K already contains structure?)
        double rKiKf=Pearson(kmInitV,kmF);
        _o.WriteLine($"r(km_init, km_final) = {rKiKf:F3}");
        _o.WriteLine($"{(rKiKf>0.5?"km_init CONTAINS km structure — SAC amplifies, not creates":"km_init INDEPENDENT — SAC creates km structure")}");

        // Test 3: does km epoch-2 already capture everything?
        var km2A=bd.Select(d=>d.Item5).ToArray();
        double rK2Kf=Pearson(km2A,kmF);
        _o.WriteLine($"r(km_epoch2, km_final) = {rK2Kf:F3}");
        int compressEpochs=rK2Kf>0.95?2:rK2Kf>0.85?3:5;
        _o.WriteLine($"Compression: km converges in ~{compressEpochs} epochs");

        // ============================================================
        // PART C — Necessity Test
        // ============================================================
        _o.WriteLine($"\n=== PART C: Necessity Test — Residual Signal After km ===");
        _o.WriteLine($"If km is NECESSARY, conditioning on km should remove ALL signal.");
        _o.WriteLine($"{"Variable",-14} {"Direct Eff",10} {"Residual Eff",14} {"Signal lost?",14}");
        _o.WriteLine(new string('-',56));

        void NecTest(string name,double[] x){
            var px=p1Idxs.Select(i=>x[i]).ToArray();
            var pbx=p1bIdxs.Select(i=>x[i]).ToArray();
            double direct=Eff(px,pbx,x);
            double rXKm=Pearson(x,kmF);
            double partialR=direct*Math.Sqrt(1-rXKm*rXKm); // approximate
            string lost=Math.Abs(partialR)<0.3?"YES (>70% lost)":Math.Abs(partialR-direct)<0.1?"NO (<10% lost)":"PARTIAL";
            _o.WriteLine($"{name,-14} {direct,10:F3}σ {partialR,14:F3}σ {lost,14}");
        }
        NecTest("d0",d0A);
        NecTest("rawIQR",iqrA);
        NecTest("phaseVar",pvA);

        // Partial correlation: r(d0, cls | km)
        double rDKm=Pearson(d0A,kmF);
        var d0P1=p1Idxs.Select(i=>d0A[i]).ToArray();
        var d0P1b=p1bIdxs.Select(i=>d0A[i]).ToArray();
        double rDCls=Eff(d0P1,d0P1b,d0A);
        // Fisher z-transform partial correlation estimate
        double zDCls=0.5*Math.Log((1+Math.Min(rDCls,0.999))/(1-Math.Max(rDCls,-0.999)));
        double zDKm=0.5*Math.Log((1+Math.Min(rDKm,0.999))/(1-Math.Max(rDKm,-0.999)));
        double zPartial=zDCls-zDKm*Math.Sqrt(1-Math.Exp(-2*Math.Abs(zDKm)));
        _o.WriteLine($"\nPartial r(d0,cls|km) ≈ {zPartial:F3}");
        _o.WriteLine($"{(Math.Abs(zPartial)<0.2?"d0 signal COLLAPSES after km control — km is NECESSARY":"d0 retains signal after km — km is NOT necessary")}");

        // ============================================================
        // PART D — Sufficiency Test
        // ============================================================
        _o.WriteLine($"\n=== PART D: Sufficiency Test — Where Does km Fail? ===");
        _o.WriteLine($"Using km alone to predict P1 vs P1b.");

        // Find optimal km threshold for P1/P1b separation
        var allKmCls=bd.Select(d=>(km:d.Item6,cls:d.Item11)).OrderBy(x=>x.km).ToArray();
        double bestThr=0;int bestCorrect=0,bestP1=0,bestP1b=0;
        var kmSorted=allKmCls.Select(x=>x.km).ToArray();
        for(int i=1;i<kmSorted.Length-1;i++){
            double thr=(kmSorted[i]+kmSorted[i+1])/2;
            int correct=0,p1Corr=0,p1bCorr=0;
            foreach(var(km,cls)in allKmCls){
                bool predP1=km>thr;
                if(cls=="P1"&&predP1){correct++;p1Corr++;}
                else if(cls=="P1b"&&!predP1){correct++;p1bCorr++;}
            }
            if(correct>bestCorrect){bestCorrect=correct;bestThr=thr;bestP1=p1Corr;bestP1b=p1bCorr;}
        }

        double acc=(double)bestCorrect/bd.Length;
        _o.WriteLine($"Best km threshold: {bestThr:F4}");
        _o.WriteLine($"Accuracy: {bestCorrect}/{bd.Length} ({acc*100:F1}%)");
        _o.WriteLine($"P1 correct: {bestP1}/{p1.Length}, P1b correct: {bestP1b}/{p1b.Length}");

        // Failure cases
        var failures=bd.Where(d=>(d.Item6>bestThr&&d.Item11=="P1b")||(d.Item6<=bestThr&&d.Item11=="P1")).ToArray();
        _o.WriteLine($"Failure cases: {failures.Length}/{bd.Length} ({failures.Length*100.0/bd.Length:F1}%)");
        if(failures.Length>0){
            _o.WriteLine($"Failure profile (avg): km={failures.Average(d=>d.Item6):F4}, d0={failures.Average(d=>d.Item7):F4}");
        }

        string suff=acc>0.90?"SUFFICIENT — km alone predicts P1/P1b with >90% accuracy"
            :acc>0.75?"PARTIALLY SUFFICIENT — km is strong but not complete"
            :"INSUFFICIENT — km needs supplementary variables";
        _o.WriteLine($"Sufficiency: {suff}");

        // ============================================================
        // PART E — Origin Compression
        // ============================================================
        _o.WriteLine($"\n=== PART E: Origin Compression ===");
        _o.WriteLine($"Is km a COMPRESSED representation of a higher-dimensional state?");

        // Compute effective dimensionality of the state space including km
        // via the eigenvalue spectrum of the correlation matrix
        var matrix=new[]{iqrA,pvA,kmInitV,rawMean,dmA,kmF};
        int nMat=matrix.Length;
        // Use all pairs to estimate rank
        double totalVar=0;var evals=new double[nMat];
        for(int vi=0;vi<nMat;vi++){
            double s=0;for(int i=0;i<nPts;i++){double v=matrix[vi][i]-matrix[vi].Average();s+=v*v;}
            evals[vi]=s/(nPts-1);totalVar+=evals[vi];
        }
        // Approximate: variance explained by top components
        var sortedEvals=evals.OrderByDescending(e=>e).ToArray();
        double cumVar=0;int effDim=0;
        for(int i=0;i<sortedEvals.Length;i++){cumVar+=sortedEvals[i];effDim++;if(cumVar/totalVar>0.95)break;}
        _o.WriteLine($"Total variance: {totalVar:F2}");
        _o.WriteLine($"Top eigenvalues: {sortedEvals[0]:F2}, {sortedEvals[1]:F2}, {sortedEvals[2]:F2}");
        _o.WriteLine($"Effective dimensionality (95% variance): {effDim}/{nMat}");
        _o.WriteLine($"km variance share: {evals[nMat-1]/totalVar*100:F1}%");

        // Test: can km be compressed into fewer dimensions?
        double kmVar=evals[nMat-1]/totalVar;
        string origin;
        if(effDim<=2)origin="km and ONE other variable capture 95% variance — km is CO-COMPRESSED with a partner";
        else if(kmVar>0.4)origin="km captures >40% variance alone — km is the DOMINANT compressed mode";
        else origin="km captures <40% variance — km is ONE of several compressed modes";
        _o.WriteLine($"Origin: {origin}");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double kmEff=Eff(kmFP,kmFPb,kmF);
        string model;
        if(bestR>0.8&&acc<0.75)model="Model C: km is COMPRESSIBLE — hidden predecessor exists";
        else if(bestR<0.5&&acc>0.85&&effDim<=2)model="Model A: km is FUNDAMENTAL — irreducibly simple";
        else if(bestR<0.5&&acc<0.75)model="Model B: km is DOMINANT but REDUCIBLE — needs supplements";
        else model="Model D: UNRESOLVED — conflicting evidence";

        _o.WriteLine($"Evidence: reconstruct r={bestR:F3}, sufficiency acc={acc*100:F1}%, eff dim={effDim}, km eff={kmEff:F3}σ");
        _o.WriteLine($"FALSIFICATION ATTEMPT: {(bestR>0.8?"km RECONSTRUCTIBLE from predecessors":"km IRREDUCIBLE")}");
        _o.WriteLine($"FALSIFICATION ATTEMPT: {(acc<0.75?"km INSUFFICIENT alone":"km SUFFICIENT")}");
        _o.WriteLine($"FALSIFICATION ATTEMPT: {(effDim<=2&&kmVar<0.3?"km is CO-COMPRESSED — not sole structural variable":"km is DOMINANT structural variable")}");
        _o.WriteLine($"");
        _o.WriteLine($"Decision: {model}");
        _o.WriteLine($"");
        if(model=="Model A")_o.WriteLine("CRIT_01 FAILED to break km. km survives all falsification attempts.");
        else if(model=="Model C")_o.WriteLine("CRIT_01 SUCCEEDED in breaking km. Hidden predecessor exists.");
        else if(model=="Model B")_o.WriteLine("CRIT_01 PARTIALLY succeeded. km is dominant but not fundamental.");
        else _o.WriteLine("CRIT_01 INCONCLUSIVE. Conflicting evidence.");
        _o.WriteLine("");
        _o.WriteLine("CLAIMS: Falsification audit only. Diagnostic. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== CRIT_01 complete. Commit: CRIT_01_KernelFalsificationAudit ===");
    }

    static double Q(double[] s,double p)=>s[(int)(p*(s.Length-1))];
    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}
    static double Pearson(double[] x,double[] y){int n=Math.Min(x.Length,y.Length);double mx=x.Take(n).Average(),my=y.Take(n).Average();double sx=0,sy=0,sxy=0;for(int i=0;i<n;i++){double dx=x[i]-mx,dy=y[i]-my;sx+=dx*dx;sy+=dy*dy;sxy+=dx*dy;}return (sx>0.001&&sy>0.001)?sxy/Math.Sqrt(sx*sy):0;}
    static double[][]Sim(double[,]K,int n,double s,int seed){var rng=new Random(seed);var w=new double[n];for(int i=0;i<n;i++)w[i]=1.0+s*(rng.NextDouble()-0.5)*2.0;var th=new double[n];for(int i=0;i<n;i++)th[i]=rng.NextDouble()*2*Math.PI;int hL=St/Hd+1;var h=new double[hL][];h[0]=(double[])th.Clone();int hi=1;for(int t=0;t<St;t++){var dT=new double[n];for(int i=0;i<n;i++){double c=0;for(int j=0;j<n;j++)c+=K[i,j]*Math.Sin(th[j]-th[i]);dT[i]=w[i]+c;}for(int i=0;i<n;i++)th[i]+=Dt*dT[i];if((t+1)%Hd==0&&hi<hL)h[hi++]=(double[])th.Clone();}return h;}
    static double[,]RP(double[][]h,int n){int T=h.Length;var R=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++){double sc=0,ss=0;for(int t=0;t<T;t++){double d=h[t][i]-h[t][j];sc+=Math.Cos(d);ss+=Math.Sin(d);}R[i,j]=Math.Sqrt(sc*sc+ss*ss)/T;}return R;}
    static double[,]Nm(double[,]R,int n){double mn=double.MaxValue;for(int i=0;i<n;i++)for(int j=0;j<n;j++)if(i!=j&&R[i,j]<mn)mn=R[i,j];double r=1.0-mn;if(r<1e-15)r=1.0;var Rn=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)Rn[i,j]=i==j?1.0:Math.Max(REps,(R[i,j]-mn)/r);return Rn;}
    static double[,]DL(double[,]R,int n){var d=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)d[i,j]=i==j?0:-Math.Log(Math.Max(R[i,j],1e-100));return d;}
    static double[,]Cupd(double[,]d,int n){var K=new double[n,n];for(int i=0;i<n;i++)for(int j=0;j<n;j++)K[i,j]=i==j?0:K0*Math.Exp(-d[i,j]/Math.Max(Xi,0.01));return K;}
    static double Dm(double[,]d,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=d[i,j];c++;}return c>0?s/c:0;}
    static double[]Of(double[][]h,int n){int T=h.Length;var o=new double[n];for(int i=0;i<n;i++){double su=0;int c=0;for(int t=1;t<T;t++){su+=Math.Abs(h[t][i]-h[t-1][i]);c++;}o[i]=c>0?su/(c*Dt*Hd):0;}return o;}
    static double[,]KS(int n,int seed){var rng=new Random(seed);var adj=new HashSet<int>[n];for(int i=0;i<n;i++)adj[i]=new HashSet<int>();double p=6.0/(n-1);for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)if(rng.NextDouble()<p){adj[i].Add(j);adj[j].Add(i);}var v=new bool[n];var cs=new List<List<int>>();for(int i=0;i<n;i++){if(v[i])continue;var c=new List<int>();var q=new Queue<int>();v[i]=true;q.Enqueue(i);while(q.Count>0){int u=q.Dequeue();c.Add(u);foreach(int x in adj[u])if(!v[x]){v[x]=true;q.Enqueue(x);}}cs.Add(c);}for(int i=1;i<cs.Count;i++){adj[cs[i][0]].Add(cs[i-1][0]);adj[cs[i-1][0]].Add(cs[i][0]);}var K=new double[n,n];for(int i=0;i<n;i++)foreach(int j in adj[i])if(i<j){K[i,j]=0.5;K[j,i]=0.5;}return K;}
    static double Km(double[,]K,int n){double s=0;int c=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++){s+=K[i,j];c++;}return c>0?s/c:0;}
    static double Ks(double[,]K,int n){var v=new double[n*(n-1)/2];int idx=0;for(int i=0;i<n;i++)for(int j=i+1;j<n;j++)v[idx++]=K[i,j];double m=v.Average();return Math.Sqrt(v.Sum(x=>(x-m)*(x-m))/v.Length);}
    static double Lambda1(double[,]K,int n){double s=0;for(int i=0;i<n;i++)for(int j=0;j<n;j++)s+=K[i,j];return s/(n*n);}
    SBase? SelectAndClassify(int n,int s,P3 hi){var K=KS(n,s);for(int e=0;e<3;e++){var h=Sim(K,n,0.10,s+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}var h3=Sim(K,n,0.10,s+3);var d3=DL(Nm(RP(h3,n),n),n);var K3=Cupd(d3,n);var h3E=Sim(K3,n,0.10,s+50);var d3E=DL(Nm(RP(h3E,n),n),n);var K3E=Cupd(d3E,n);double d0=Dm(d3E,n),km=Km(K3E,n),ks=Ks(K3E,n);var sb=new SBase{seed=s,d0=d0,km0=km,ks0=ks,cls=""};sb=Classify(sb,hi);double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,sv=hi.ks-Lo(n).ks,vn=Math.Sqrt(dv*dv+kv*kv+sv*sv);double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv+(ks-Lo(n).ks)*sv)/vn:0;double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km)+(ks-Lo(n).ks)*(ks-Lo(n).ks);double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return null;return sb;}
    static SBase Classify(SBase b,P3 hi){string cls;if(b.d0>0.65)cls="P1b";else if(b.d0>0.50)cls="P1";else if(b.d0<=0.40&&b.km0>0.98&&DistK(b.km0,b.ks0,hi)<0.15)cls="P2";else if(b.d0>0.40&&b.d0<=0.50)cls="P3";else cls="P4";return new SBase{seed=b.seed,d0=b.d0,km0=b.km0,ks0=b.ks0,cls=cls};}
    static double DistK(double km,double ks,P3 hi){double dk=km-hi.km,dks=ks-hi.ks;return Math.Sqrt(dk*dk+dks*dks);}
    bool IsHi(int n,int seed){var K=KS(n,seed);for(int e=0;e<5;e++){var h=Sim(K,n,0.10,seed+e);var d=DL(Nm(RP(h,n),n),n);K=Cupd(d,n);}return Of(Sim(K,n,0.10,seed+5),n).Average()>THR;}
    static P3 PCent(int n,bool hi){double d=0,k=0,ks=0;int c=0;for(int sd=0;sd<100;sd++){var K=KS(n,sd);double dm=0,km=0,kss=0;for(int e=0;e<5;e++){var h=Sim(K,n,0.10,sd+e);var dd=DL(Nm(RP(h,n),n),n);dm+=Dm(dd,n);K=Cupd(dd,n);km+=Km(K,n);kss+=Ks(K,n);}double om=Of(Sim(K,n,0.10,sd+5),n).Average();if((om>THR)==hi){d+=dm/5;k+=km/5;ks+=kss/5;c++;}}if(c==0)return new P3{dm=double.NaN,km=double.NaN,ks=double.NaN};return new P3{dm=d/c,km=k/c,ks=ks/c};}
}
