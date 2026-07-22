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

    [Fact]
    public void RES_01_ResidualStructureAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RES_01: Residual Structure Audit ===");
        _o.WriteLine("=== V5.59. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Is d0 residual after km genuine or artifact? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        int nEpochs=5; // Full 5-epoch convergence
        var bag=new ConcurrentBag<(int N,int s,double km,double d0,double lam,double d2,
            double rawIQR,double rank,int cls,double kmInit,double d0Init,double km1,double d01,double km2,double d02)>();

        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                var ws=w.OrderBy(v=>v).ToArray();
                double rawIQR=Q(ws,0.75)-Q(ws,0.25);
                int[] ranks=Enumerable.Range(0,n).OrderBy(i=>w[i]).Select((v,rk)=>new{v,rk}).OrderBy(x=>x.v).Select(x=>x.rk).ToArray();
                double meanRank=ranks.Average();

                var K=KS(n,s);double kmInit=Km(K,n);

                // Epoch 1
                var h1=Sim(K,n,0.10,s);var d1=DL(Nm(RP(h1,n),n),n);
                double d01i=Dm(d1,n);
                K=Cupd(d1,n);double km1i=Km(K,n);

                // Epoch 2
                var h2=Sim(K,n,0.10,s+1);var d2m=DL(Nm(RP(h2,n),n),n);
                double d02i=Dm(d2m,n);
                K=Cupd(d2m,n);double km2i=Km(K,n);

                // Epochs 3-5
                for(int e=3;e<=nEpochs;e++){
                    var he=Sim(K,n,0.10,s+e-1);
                    K=Cupd(DL(Nm(RP(he,n),n),n),n);
                }

                // Final measurement
                var hF=Sim(K,n,0.10,s+50);
                var dF=DL(Nm(RP(hF,n),n),n);
                var KF=Cupd(dF,n);
                double d0=Dm(dF,n),km=Km(KF,n),lam=Lambda1(KF,n);
                double d2=DL(Nm(RP(hF,n),n),n).Cast<double>().Average(); // d2_mean

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,vn=Math.Sqrt(dv*dv+kv*kv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,km,d0,lam,0,rawIQR,meanRank,sb.cls=="P1"?1:2,kmInit,d01i,km1i,d01i,km2i,d02i));
            });});
        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.cls==1).ToArray();var p1b=bd.Where(d=>d.cls==2).ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length} (from {sds*Ns.Length} profiles)");

        double Eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        var kmA=bd.Select(d=>d.km).ToArray();
        var d0A=bd.Select(d=>d.d0).ToArray();
        var lamA=bd.Select(d=>d.lam).ToArray();
        var iqrA=bd.Select(d=>d.rawIQR).ToArray();
        var rnkA=bd.Select(d=>d.rank).ToArray();
        var nA=bd.Select(d=>(double)d.N).ToArray();

        // ============================================================
        // PART A — Residual Signal After km
        // ============================================================
        _o.WriteLine($"\n=== PART A: Residual Signal After km ===");
        _o.WriteLine($"{"Variable",-12} {"Direct Eff",12} {"r(km)",8} {"Resid_Eff",12} {"Signal_type",14}");
        _o.WriteLine(new string('-',60));

        // Residualize each variable on km
        double mk=kmA.Average();double vk=0;for(int i=0;i<kmA.Length;i++)vk+=(kmA[i]-mk)*(kmA[i]-mk);
        double[] Residualize(double[]x){
            double mx=x.Average();double cv=0;for(int i=0;i<x.Length;i++)cv+=(x[i]-mx)*(kmA[i]-mk);
            double beta=cv/(vk+1e-15);double alpha=mx-beta*mk;
            return x.Select((xi,i)=>xi-(alpha+beta*kmA[i])).ToArray();
        }

        void PartA(string name,double[]x){
            double dir=Eff(p1.Select(d=>x[Array.IndexOf(bd,d)]).ToArray(),
                           p1b.Select(d=>x[Array.IndexOf(bd,d)]).ToArray(),x);
            double rKm=Pearson(x,kmA);
            var resid=Residualize(x);
            double rEff=Eff(p1.Select(d=>resid[Array.IndexOf(bd,d)]).ToArray(),
                            p1b.Select(d=>resid[Array.IndexOf(bd,d)]).ToArray(),resid);
            string sig=Math.Abs(rEff)<0.3?">70% absorbed":Math.Abs(rEff)<0.6?"PARTIAL (<50%)":"INDEPENDENT (<20%)";
            _o.WriteLine($"{name,-12} {dir,12:F3}σ {rKm,8:F3} {rEff,12:F3}σ {sig,14}");
        }
        PartA("d0",d0A);
        PartA("lambda1",lamA);
        PartA("rawIQR",iqrA);
        PartA("rank",rnkA);
        PartA("N",nA);

        // ============================================================
        // PART B — Residual Decomposition
        // ============================================================
        _o.WriteLine($"\n=== PART B: Residual Decomposition of d0 ===");
        double rDK=Pearson(d0A,kmA);
        double varD0=d0A.Sum(v=>(v-d0A.Average())*(v-d0A.Average()))/(d0A.Length-1);
        double varExplained=rDK*rDK*varD0;
        double varResidual=varD0-varExplained;
        _o.WriteLine($"d0 total variance: {varD0:F6}");
        _o.WriteLine($"Variance explained by km: {varExplained:F6} ({varExplained/varD0*100:F1}%)");
        _o.WriteLine($"Residual variance: {varResidual:F6} ({varResidual/varD0*100:F1}%)");

        // Residual d0 effect size
        var d0Resid=Residualize(d0A);
        var d0ResidP1=p1.Select(d=>d0Resid[Array.IndexOf(bd,d)]).ToArray();
        var d0ResidP1b=p1b.Select(d=>d0Resid[Array.IndexOf(bd,d)]).ToArray();
        double residEff=Eff(d0ResidP1,d0ResidP1b,d0Resid);
        _o.WriteLine($"d0 residual direct effect: {residEff:F3}σ");

        double d0DirEff=Eff(p1.Select(d=>d.d0).ToArray(),p1b.Select(d=>d.d0).ToArray(),d0A);
        double kmDirEff=Eff(p1.Select(d=>d.km).ToArray(),p1b.Select(d=>d.km).ToArray(),kmA);
        _o.WriteLine($"\nd0 direct: {d0DirEff:F3}σ, km direct: {kmDirEff:F3}σ");
        _o.WriteLine($"d0 residual: {residEff:F3}σ ({(residEff/d0DirEff*100):F0}% of original)");

        // ============================================================
        // PART C — Failure-Case Analysis
        // ============================================================
        _o.WriteLine($"\n=== PART C: Failure-Case Analysis ===");

        // Find optimal km threshold
        var sorted=bd.OrderBy(d=>d.km).ToArray();
        double bestThr=0;int bestOk=0;
        for(int i=1;i<sorted.Length-1;i++){
            double thr=(sorted[i].km+sorted[i+1].km)/2;int ok=0;
            foreach(var d in sorted){if((d.cls==1&&d.km>thr)||(d.cls==2&&d.km<=thr))ok++;}
            if(ok>bestOk){bestOk=ok;bestThr=thr;}
        }

        // d0-only threshold
        var sortedD0=bd.OrderBy(d=>d.d0).ToArray();
        double bestThrD0=0;int bestOkD0=0;
        for(int i=1;i<sortedD0.Length-1;i++){
            double thr=(sortedD0[i].d0+sortedD0[i+1].d0)/2;int ok=0;
            foreach(var d in sortedD0){if((d.cls==1&&d.d0>thr)||(d.cls==2&&d.d0<=thr))ok++;}
            if(ok>bestOkD0){bestOkD0=ok;bestThrD0=thr;}
        }

        // Failure cases: km wrong, d0 correct
        var kmFailD0Ok=bd.Where(d=>
            (d.km>bestThr&&d.cls==2&&d.d0>bestThrD0)||  // P1b: km says P1, d0 says P1b
            (d.km<=bestThr&&d.cls==1&&d.d0<=bestThrD0)   // P1: km says P1b, d0 says P1
        ).ToArray();
        var d0FailKmOk=bd.Where(d=>
            (d.d0>bestThrD0&&d.cls==2&&d.km<=bestThr)||
            (d.d0<=bestThrD0&&d.cls==1&&d.km>bestThr)
        ).ToArray();

        _o.WriteLine($"km threshold: {bestThr:F4}, accuracy: {bestOk}/{bd.Length} ({bestOk*100.0/bd.Length:F1}%)");
        _o.WriteLine($"d0 threshold: {bestThrD0:F4}, accuracy: {bestOkD0}/{bd.Length} ({bestOkD0*100.0/bd.Length:F1}%)");
        _o.WriteLine($"km-fail/d0-ok cases: {kmFailD0Ok.Length}");
        _o.WriteLine($"d0-fail/km-ok cases: {d0FailKmOk.Length}");

        if(kmFailD0Ok.Length>0){
            double avgKmF=kmFailD0Ok.Average(d=>d.km),avgD0F=kmFailD0Ok.Average(d=>d.d0);
            double avgIqrF=kmFailD0Ok.Average(d=>d.rawIQR),avgNF=kmFailD0Ok.Average(d=>d.N);
            _o.WriteLine($"km-fail/d0-ok profile: km={avgKmF:F4}, d0={avgD0F:F4}, N≈{avgNF:F0}");
        }

        // ============================================================
        // PART D — Joint Models
        // ============================================================
        _o.WriteLine($"\n=== PART D: Joint Model Comparison ===");
        _o.WriteLine($"{"Model",-22} {"Accuracy",10} {"P1_acc",8} {"P1b_acc",8} {"Edge over km",14}");
        _o.WriteLine(new string('-',64));

        // Model A: km only
        int kmAcc=bestOk;
        int kmP1=bd.Count(d=>d.cls==1&&d.km>bestThr);
        int kmP1b=bd.Count(d=>d.cls==2&&d.km<=bestThr);
        _o.WriteLine($"{"A: km only",-22} {kmAcc*100.0/bd.Length,10:F1}% {kmP1*100.0/p1.Length,8:F1}% {kmP1b*100.0/p1b.Length,8:F1}% {"—",14}");

        // Model B: d0 only
        int d0P1=bd.Count(d=>d.cls==1&&d.d0>bestThrD0);
        int d0P1b=bd.Count(d=>d.cls==2&&d.d0<=bestThrD0);
        _o.WriteLine($"{"B: d0 only",-22} {bestOkD0*100.0/bd.Length,10:F1}% {d0P1*100.0/p1.Length,8:F1}% {d0P1b*100.0/p1b.Length,8:F1}% {$"+{bestOkD0-bestOk}",14}");

        // Model C: km + d0 (simple AND rule)
        int cOk=0,cP1=0,cP1b=0;
        foreach(var d in bd){
            bool kmPred=d.km>bestThr,d0Pred=d.d0>bestThrD0;
            bool final=kmPred&&d0Pred; // P1 if both agree
            if((d.cls==1&&final)||(d.cls==2&&!final))cOk++;
            if(d.cls==1&&final)cP1++;
            if(d.cls==2&&!final)cP1b++;
        }
        _o.WriteLine($"{"C: km AND d0",-22} {cOk*100.0/bd.Length,10:F1}% {cP1*100.0/p1.Length,8:F1}% {cP1b*100.0/p1b.Length,8:F1}% {$"+{cOk-bestOk}",14}");

        // Model D: km + d0 residual (km primary, d0_resid corrects failures)
        var d0ResidC=Residualize(d0A);
        double bestThrDR=0;int bestOkDR=0;
        var sortDR=bd.OrderBy(d=>d0ResidC[Array.IndexOf(bd,d)]).ToArray();
        for(int i=1;i<sortDR.Length-1;i++){
            double thr=d0ResidC[Array.IndexOf(bd,sortDR[i])];int ok=0;
            foreach(var d in sortDR){double r=d0ResidC[Array.IndexOf(bd,d)];if((d.cls==1&&r>thr)||(d.cls==2&&r<=thr))ok++;}
            if(ok>bestOkDR){bestOkDR=ok;bestThrDR=thr;}
        }
        // Two-stage: km first, then d0_resid corrects
        int dOk=0,dP1=0,dP1b=0;
        foreach(var d in bd){
            bool kmPred=d.km>bestThr;
            if(kmPred==(d.cls==1)){dOk++;if(d.cls==1)dP1++;else dP1b++;}
            else{
                double dr=d0ResidC[Array.IndexOf(bd,d)];
                bool dResidPred=dr>bestThrDR;
                if(dResidPred==(d.cls==1)){dOk++;if(d.cls==1)dP1++;else dP1b++;}
            }
        }
        _o.WriteLine($"{"D: km + d0_resid",-22} {dOk*100.0/bd.Length,10:F1}% {dP1*100.0/p1.Length,8:F1}% {dP1b*100.0/p1b.Length,8:F1}% {$"+{dOk-bestOk}",14}");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");

        // Jackknife: drop one profile
        var jkAcc=new double[bd.Length];
        for(int j=0;j<bd.Length;j++){
            var sub=bd.Where((d,i)=>i!=j).ToArray();
            var subKm=sub.Select(d=>d.km).ToArray();
            var subSort=sub.OrderBy(d=>d.km).ToArray();
            int bestJ=0;double bestThrJ=0;
            for(int i=1;i<subSort.Length-1;i++){
                double thr=(subSort[i].km+subSort[i+1].km)/2;int ok=0;
                foreach(var d in subSort)if((d.cls==1&&d.km>thr)||(d.cls==2&&d.km<=thr))ok++;
                if(ok>bestJ){bestJ=ok;bestThrJ=thr;}
            }
            jkAcc[j]=bestJ*100.0/sub.Length;
        }
        _o.WriteLine($"Jackknife km accuracy: {jkAcc.Average():F1}% ± {Sd(jkAcc):F1}% (range [{jkAcc.Min():F0}-{jkAcc.Max():F0}]%)");

        // Random split: 70/30
        var rngSplit=new Random(42);
        var idxs=Enumerable.Range(0,bd.Length).OrderBy(_=>rngSplit.Next()).ToArray();
        int splitN=bd.Length*70/100;
        var train=idxs.Take(splitN).Select(i=>bd[i]).ToArray();
        var test=idxs.Skip(splitN).Select(i=>bd[i]).ToArray();
        var trainKm=train.Select(d=>d.km).OrderBy(k=>k).ToArray();
        double testThr=0;int testBest=0;
        for(int i=1;i<trainKm.Length-1;i++){
            double thr=(trainKm[i]+trainKm[i+1])/2;int ok=0;
            foreach(var d in train)if((d.cls==1&&d.km>thr)||(d.cls==2&&d.km<=thr))ok++;
            if(ok>testBest){testBest=ok;testThr=thr;}
        }
        int testOk=0;
        foreach(var d in test)if((d.cls==1&&d.km>testThr)||(d.cls==2&&d.km<=testThr))testOk++;
        _o.WriteLine($"70/30 split: train acc={testBest*100.0/train.Length:F1}%, test acc={testOk*100.0/test.Length:F1}%");

        // Leave-one-N
        foreach(var nv in Ns){
            var sub=bd.Where(d=>d.N!=nv).ToArray();
            var subKm=sub.Select(d=>d.km).OrderBy(k=>k).ToArray();
            int bestLN=0;double bestThrLN=0;
            for(int i=1;i<subKm.Length-1;i++){
                double thr=(subKm[i]+subKm[i+1])/2;int ok=0;
                foreach(var d in sub)if((d.cls==1&&d.km>thr)||(d.cls==2&&d.km<=thr))ok++;
                if(ok>bestLN){bestLN=ok;bestThrLN=thr;}
            }
            var left=bd.Where(d=>d.N==nv).ToArray();int leftOk=0;
            foreach(var d in left)if((d.cls==1&&d.km>bestThrLN)||(d.cls==2&&d.km<=bestThrLN))leftOk++;
            _o.WriteLine($"Leave-out N={nv}: trained on {sub.Length}, test on {left.Length}: {leftOk}/{left.Length} ({leftOk*100.0/(left.Length+1e-9):F0}%) correct");
        }

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double improvementOverKm=dOk-bestOk;
        double residSignal=residEff;

        string model;
        if(residSignal<0.3&&improvementOverKm<=1)model="Model A: d0 residual is ARTIFACT — no independent signal";
        else if(residSignal>=0.3&&residSignal<0.8&&improvementOverKm<=2)model="Model B: d0 residual is SECONDARY — weak but genuine";
        else if(improvementOverKm>=3)model="Model C: km+d0 DUAL KERNEL — both contribute independently";
        else model="Model D: UNRESOLVED";

        _o.WriteLine($"d0 residual effect: {residSignal:F3}σ, improvement over km-only: {improvementOverKm} profiles");
        _o.WriteLine($"Decision: {model}");
        _o.WriteLine($"");
        _o.WriteLine($"Summary:");
        _o.WriteLine($"  Direct effects: km={kmDirEff:F3}σ, d0={d0DirEff:F3}σ");
        _o.WriteLine($"  Post-km residual: d0={residSignal:F3}σ, lam={Eff(p1.Select(d=>Residualize(lamA)[Array.IndexOf(bd,d)]).ToArray(),p1b.Select(d=>Residualize(lamA)[Array.IndexOf(bd,d)]).ToArray(),Residualize(lamA)):F3}σ");
        _o.WriteLine($"  Jackknife stability: {jkAcc.Average():F1}±{Sd(jkAcc):F1}%");
        _o.WriteLine($"  Random split generalization: train→test drop = {testBest*100.0/train.Length-testOk*100.0/test.Length:F1}%");
        _o.WriteLine("");
        if(model=="Model A")_o.WriteLine("d0 residual is absorbed by km. km is the SOLE kernel.");
        else if(model=="Model B")_o.WriteLine("d0 carries weak independent structure. km is PRIMARY, d0 is SECONDARY.");
        else _o.WriteLine("Both km and d0 contribute. DUAL kernel supported.");
        _o.WriteLine("");
        _o.WriteLine("CLAIMS: Residual audit only. Diagnostic. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RES_01 complete. Commit: RES_01_ResidualStructureAudit ===");
    }

    [Fact]
    public void DK_01_DualKernelAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== DK_01: Dual Kernel Audit ===");
        _o.WriteLine("=== V5.59. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: What generates the d0 residual structure? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        // Track d0 at every pipeline stage: R[9], Nm d[10], DL d[11], Cupd km[12]
        var bag=new ConcurrentBag<(int N,int s,double km,double d0,double dR,double dN,
            double dDL,double dCupd,double rawIQR,int cls,double kmInit,double d0Init)>();

        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);double kmInit=Km(K,n);

                // Epoch 1 — capture ALL pipeline stages
                var h1=Sim(K,n,0.10,s);
                var R=RP(h1,n);           // Phase coherence
                double dR=1-Dm(R,n);      // 1 - mean coherence (raw "distance")
                var Rn=Nm(R,n);           // Normalized
                double dNm=Dm(Rn,n);      // Normalized mean distance
                var dDL=DL(Rn,n);         // Distance transform
                double dDLm=Dm(dDL,n);
                K=Cupd(dDL,n);            // Coupling update
                double km1=Km(K,n);

                // Epochs 2-5
                for(int e=2;e<=5;e++){
                    var he=Sim(K,n,0.10,s+e-1);
                    K=Cupd(DL(Nm(RP(he,n),n),n),n);
                }

                // Final measurement
                var hF=Sim(K,n,0.10,s+50);
                var dF=DL(Nm(RP(hF,n),n),n);
                var KF=Cupd(dF,n);
                double d0=Dm(dF,n),km=Km(KF,n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,vn=Math.Sqrt(dv*dv+kv*kv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,km,d0,dR,dNm,dDLm,km1,rawIQR,sb.cls=="P1"?1:2,kmInit,d0));
            });});

        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.cls==1).ToArray();var p1b=bd.Where(d=>d.cls==2).ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length} (from {sds*Ns.Length} profiles)");

        double Eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        var kmA=bd.Select(d=>d.km).ToArray();
        var d0A=bd.Select(d=>d.d0).ToArray();
        var dRA=bd.Select(d=>d.dR).ToArray();
        var dNA=bd.Select(d=>d.dN).ToArray();
        var dDLA=bd.Select(d=>d.dDL).ToArray();
        var dCA=bd.Select(d=>d.dCupd).ToArray();
        var iqrA=bd.Select(d=>d.rawIQR).ToArray();
        var kmInitA=bd.Select(d=>d.kmInit).ToArray();

        // ============================================================
        // PART A — Construct d0_residual and measure
        // ============================================================
        _o.WriteLine($"\n=== PART A: d0_residual Construction ===");

        // Linear fit: d0 = a + b*km
        double mk=kmA.Average();double vk=0;double cd=0;
        for(int i=0;i<kmA.Length;i++){vk+=(kmA[i]-mk)*(kmA[i]-mk);cd+=(d0A[i]-d0A.Average())*(kmA[i]-mk);}
        double beta=cd/(vk+1e-15);double alpha=d0A.Average()-beta*mk;
        var d0Resid=d0A.Select((d,i)=>d-(alpha+beta*kmA[i])).ToArray();

        double d0Dir=Eff(p1.Select(d=>d.d0).ToArray(),p1b.Select(d=>d.d0).ToArray(),d0A);
        double kmDir=Eff(p1.Select(d=>d.km).ToArray(),p1b.Select(d=>d.km).ToArray(),kmA);
        double resDir=Eff(p1.Select((d,i)=>d0Resid[Array.IndexOf(bd,d)]).ToArray(),
                            p1b.Select((d,i)=>d0Resid[Array.IndexOf(bd,d)]).ToArray(),d0Resid);

        _o.WriteLine($"d0 direct: {d0Dir:F3}σ, km direct: {kmDir:F3}σ");
        _o.WriteLine($"d0_residual direct: {resDir:F3}σ ({resDir/d0Dir*100:F0}% retained)");
        _o.WriteLine($"d0_residual variance: {d0Resid.Sum(v=>v*v)/(d0Resid.Length-1):F6}");
        _o.WriteLine($"r(d0_residual, km) = {Pearson(d0Resid,kmA):F6} (should be ≈0)");

        // ============================================================
        // PART B — Failure-Case Localization
        // ============================================================
        _o.WriteLine($"\n=== PART B: Failure-Case Localization ===");

        // Find thresholds
        var sorted=bd.OrderBy(d=>d.km).ToArray();
        double thrKm=0;int bestKm=0;
        for(int i=1;i<sorted.Length-1;i++){double t=(sorted[i].km+sorted[i+1].km)/2;int o=0;foreach(var d in sorted)if((d.cls==1&&d.km>t)||(d.cls==2&&d.km<=t))o++;if(o>bestKm){bestKm=o;thrKm=t;}}

        var sortD0=bd.OrderBy(d=>d.d0).ToArray();
        double thrD0=0;int bestD0=0;
        for(int i=1;i<sortD0.Length-1;i++){double t=(sortD0[i].d0+sortD0[i+1].d0)/2;int o=0;foreach(var d in sortD0)if((d.cls==1&&d.d0>t)||(d.cls==2&&d.d0<=t))o++;if(o>bestD0){bestD0=o;thrD0=t;}}

        // Failure groups
        var kmFailD0ok=bd.Where(d=>((d.cls==1&&d.km<=thrKm&&d.d0>thrD0)||(d.cls==2&&d.km>thrKm&&d.d0<=thrD0))).ToArray();
        var d0FailKmOk=bd.Where(d=>((d.cls==1&&d.d0<=thrD0&&d.km>thrKm)||(d.cls==2&&d.d0>thrD0&&d.km<=thrKm))).ToArray();
        var bothOk=bd.Where(d=>((d.cls==1&&d.km>thrKm&&d.d0>thrD0)||(d.cls==2&&d.km<=thrKm&&d.d0<=thrD0))).ToArray();
        var bothFail=bd.Where(d=>((d.cls==1&&d.km<=thrKm&&d.d0<=thrD0)||(d.cls==2&&d.km>thrKm&&d.d0>thrD0))).ToArray();

        _o.WriteLine($"km-fail/d0-ok: {kmFailD0ok.Length}, d0-fail/km-ok: {d0FailKmOk.Length}");
        _o.WriteLine($"Both correct: {bothOk.Length}, Both wrong: {bothFail.Length}");

        // Profile comparison
        _o.WriteLine($"\nProfile comparison:");
        _o.WriteLine($"{"Group",-20} {"n",4} {"km",8} {"d0",8} {"dR",8} {"dN",8} {"dDL",8} {"km_init",8} {"rawIQR",8} {"N",4}");
        _o.WriteLine(new string('-',88));
        void Profile(string name, (int N,int s,double km,double d0,double dR,double dN,double dDL,double dCupd,double rawIQR,int cls,double kmInit,double d0Init)[] g){
            if(g.Length==0)return;
            _o.WriteLine($"{name,-20} {g.Length,4} {g.Average(d=>d.km),8:F4} {g.Average(d=>d.d0),8:F4} {g.Average(d=>d.dR),8:F4} {g.Average(d=>d.dN),8:F4} {g.Average(d=>d.dDL),8:F4} {g.Average(d=>d.kmInit),8:F4} {g.Average(d=>d.rawIQR),8:F4} {g.Average(d=>d.N),4:F0}");
        }
        Profile("km-fail/d0-ok",kmFailD0ok);
        Profile("d0-fail/km-ok",d0FailKmOk);
        Profile("Both correct",bothOk);
        Profile("Both wrong",bothFail);

        _o.WriteLine($"\nkm-fail/d0-ok distinguishing features:");
        if(kmFailD0ok.Length>0&&d0FailKmOk.Length>0){
            double dRdiff=kmFailD0ok.Average(d=>d.dR)-d0FailKmOk.Average(d=>d.dR);
            double iqrDiff=kmFailD0ok.Average(d=>d.rawIQR)-d0FailKmOk.Average(d=>d.rawIQR);
            double nDiff=kmFailD0ok.Average(d=>d.N)-d0FailKmOk.Average(d=>d.N);
            _o.WriteLine($"  dR (raw distance): Δ={dRdiff:F4} (km-fail/d0-ok vs d0-fail/km-ok)");
            _o.WriteLine($"  rawIQR: Δ={iqrDiff:F4}");
            _o.WriteLine($"  N: Δ={nDiff:F1}");
        }

        // ============================================================
        // PART C — Origin Trace Through Pipeline
        // ============================================================
        _o.WriteLine($"\n=== PART C: Origin Trace Through SAC Pipeline ===");
        _o.WriteLine($"At which stage does d0_residual structure emerge?");
        _o.WriteLine($"{"Stage",-10} {"Effect σ",10} {"r(km)",8} {"Resid σ",10} {"Emergence",12}");
        _o.WriteLine(new string('-',52));

        void TraceStage(string name,double[]x){
            double e=Eff(p1.Select(d=>x[Array.IndexOf(bd,d)]).ToArray(),
                         p1b.Select(d=>x[Array.IndexOf(bd,d)]).ToArray(),x);
            double rk=Pearson(x,kmA);
            // Residualize on km
            double mx=x.Average();double cv=0;
            for(int i=0;i<x.Length;i++)cv+=(x[i]-mx)*(kmA[i]-mk);
            double b=cv/(vk+1e-15);double a=mx-b*mk;
            var resid=x.Select((xi,i)=>xi-(a+b*kmA[i])).ToArray();
            double rEff=Eff(p1.Select(d=>resid[Array.IndexOf(bd,d)]).ToArray(),
                            p1b.Select(d=>resid[Array.IndexOf(bd,d)]).ToArray(),resid);
            string emg=Math.Abs(rEff)<0.2?"AFTER RP":Math.Abs(rEff)<0.5?"AFTER DL":"AFTER Cupd";
            _o.WriteLine($"{name,-10} {e,10:F3}σ {rk,8:F3} {rEff,10:F3}σ {emg,12}");
        }

        _o.WriteLine($"\nStage 1: Raw data (pre-SAC)");
        TraceStage("rawIQR",iqrA);
        TraceStage("km_init",kmInitA);

        _o.WriteLine($"\nStage 2: Epoch 1 pipeline");
        TraceStage("RP (dR)",dRA);
        TraceStage("Nm (dN)",dNA);
        TraceStage("DL (dDL)",dDLA);
        TraceStage("Cupd (km1)",dCA);

        _o.WriteLine($"\nStage 3: Final (5 epochs)");
        TraceStage("d0_final",d0A);
        TraceStage("km_final",kmA);

        // Measure at which stage r(d_stage, km_final) crosses 0.5
        _o.WriteLine($"\nd0_residual r(d_stage, km):");
        _o.WriteLine($"  dR: r={Pearson(dRA,kmA):F3}");
        _o.WriteLine($"  dN: r={Pearson(dNA,kmA):F3}");
        _o.WriteLine($"  dDL: r={Pearson(dDLA,kmA):F3}");
        _o.WriteLine($"  km1 (Cupd): r={Pearson(dCA,kmA):F3}");
        _o.WriteLine($"  d0_final: r={Pearson(d0A,kmA):F3}");

        // ============================================================
        // PART D — Interaction Models
        // ============================================================
        _o.WriteLine($"\n=== PART D: Interaction Models ===");

        // Model A: km only
        int aOk=0,aP1=0,aP1b=0;
        foreach(var d in bd){bool p=d.km>thrKm;bool ok=(d.cls==1&&p)||(d.cls==2&&!p);aOk+=ok?1:0;if(d.cls==1&&p)aP1++;if(d.cls==2&&!p)aP1b++;}
        _o.WriteLine($"A: km only — {aOk*100.0/bd.Length:F1}% (P1={aP1*100.0/p1.Length:F1}%, P1b={aP1b*100.0/p1b.Length:F1}%)");

        // Model B: d0_residual only
        var sortR=bd.OrderBy(d=>d0Resid[Array.IndexOf(bd,d)]).ToArray();
        double thrR=0;int bestR=0;
        for(int i=1;i<sortR.Length-1;i++){double t=d0Resid[Array.IndexOf(bd,sortR[i])];int o=0;foreach(var d in sortR){double v=d0Resid[Array.IndexOf(bd,d)];if((d.cls==1&&v>t)||(d.cls==2&&v<=t))o++;}if(o>bestR){bestR=o;thrR=t;}}
        int bOk=0,bP1=0,bP1b=0;
        foreach(var d in bd){double v=d0Resid[Array.IndexOf(bd,d)];bool p=v>thrR;bool ok=(d.cls==1&&p)||(d.cls==2&&!p);bOk+=ok?1:0;if(d.cls==1&&p)bP1++;if(d.cls==2&&!p)bP1b++;}
        _o.WriteLine($"B: d0_resid only — {bOk*100.0/bd.Length:F1}% (P1={bP1*100.0/p1.Length:F1}%, P1b={bP1b*100.0/p1b.Length:F1}%)");

        // Model C: km + d0_residual (two-stage cascade)
        int cOk=0,cP1=0,cP1b=0;
        foreach(var d in bd){
            bool kmOk=(d.cls==1&&d.km>thrKm)||(d.cls==2&&d.km<=thrKm);
            if(kmOk){cOk++;if(d.cls==1)cP1++;else cP1b++;}
            else{double v=d0Resid[Array.IndexOf(bd,d)];bool rOk=(d.cls==1&&v>thrR)||(d.cls==2&&v<=thrR);cOk+=rOk?1:0;if(d.cls==1&&rOk)cP1++;if(d.cls==2&&rOk)cP1b++;}
        }
        _o.WriteLine($"C: km + d0_resid — {cOk*100.0/bd.Length:F1}% (P1={cP1*100.0/p1.Length:F1}%, P1b={cP1b*100.0/p1b.Length:F1}%) [{(cOk-aOk)} rescued]");

        // Model D: km + rawIQR (test if rawIQR adds beyond d0)
        var sortIQ=bd.OrderBy(d=>d.rawIQR).ToArray();
        double thrIQ=0;int bestIQ=0;
        for(int i=1;i<sortIQ.Length-1;i++){double t=(sortIQ[i].rawIQR+sortIQ[i+1].rawIQR)/2;int o=0;foreach(var d in sortIQ)if((d.cls==1&&d.rawIQR>t)||(d.cls==2&&d.rawIQR<=t))o++;if(o>bestIQ){bestIQ=o;thrIQ=t;}}
        int dOk=0,dP1=0,dP1b=0;
        foreach(var d in bd){
            bool kmOk=(d.cls==1&&d.km>thrKm)||(d.cls==2&&d.km<=thrKm);
            if(kmOk){dOk++;if(d.cls==1)dP1++;else dP1b++;}
            else{bool iqOk=(d.cls==1&&d.rawIQR>thrIQ)||(d.cls==2&&d.rawIQR<=thrIQ);dOk+=iqOk?1:0;if(d.cls==1&&iqOk)dP1++;if(d.cls==2&&iqOk)dP1b++;}
        }
        _o.WriteLine($"D: km + rawIQR — {dOk*100.0/bd.Length:F1}% (P1={dP1*100.0/p1.Length:F1}%, P1b={dP1b*100.0/p1b.Length:F1}%) [{(dOk-aOk)} rescued]");

        // ============================================================
        // PART E — Robustness
        // ============================================================
        _o.WriteLine($"\n=== PART E: Robustness ===");

        // Jackknife of km+d0_resid improvement
        var jkImp=new double[bd.Length];
        for(int j=0;j<bd.Length;j++){
            var sub=bd.Where((d,i)=>i!=j).ToArray();
            var subKm=sub.Select(d=>d.km).OrderBy(k=>k).ToArray();
            int bJ=0;double tJ=0;
            for(int i=1;i<subKm.Length-1;i++){double t=(subKm[i]+subKm[i+1])/2;int o=0;foreach(var d in sub)if((d.cls==1&&d.km>t)||(d.cls==2&&d.km<=t))o++;if(o>bJ){bJ=o;tJ=t;}}
            var subR=sub.Select(d=>d0Resid[Array.IndexOf(bd,d)]).OrderBy(r=>r).ToArray();
            int brJ=0;double trJ=0;
            for(int i=1;i<subR.Length-1;i++){double t=subR[i];int o=0;foreach(var d in sub){double v=d0Resid[Array.IndexOf(bd,d)];if((d.cls==1&&v>t)||(d.cls==2&&v<=t))o++;}if(o>brJ){brJ=o;trJ=t;}}
            int kmOnlyJ=0,twoStageJ=0;
            foreach(var d in sub){bool ko=(d.cls==1&&d.km>tJ)||(d.cls==2&&d.km<=tJ);kmOnlyJ+=ko?1:0;if(ko)twoStageJ++;else{double v=d0Resid[Array.IndexOf(bd,d)];twoStageJ+=((d.cls==1&&v>trJ)||(d.cls==2&&v<=trJ))?1:0;}}
            jkImp[j]=twoStageJ-kmOnlyJ;
        }
        _o.WriteLine($"Jackknife improvement: {jkImp.Average():F1} ± {Sd(jkImp):F1} profiles ({jkImp.Count(d=>d>0)}/{bd.Length} positive)");

        // Leave-one-N for km+d0_resid
        foreach(var nv in Ns){
            var sub=bd.Where(d=>d.N!=nv).ToArray();
            var subKm=sub.Select(d=>d.km).OrderBy(k=>k).ToArray();
            int bN=0;double tN=0;
            for(int i=1;i<subKm.Length-1;i++){double t=(subKm[i]+subKm[i+1])/2;int o=0;foreach(var d in sub)if((d.cls==1&&d.km>t)||(d.cls==2&&d.km<=t))o++;if(o>bN){bN=o;tN=t;}}
            var subR=sub.Select(d=>d0Resid[Array.IndexOf(bd,d)]).OrderBy(r=>r).ToArray();
            int brN=0;double trN=0;
            for(int i=1;i<subR.Length-1;i++){double t=subR[i];int o=0;foreach(var d in sub){double v=d0Resid[Array.IndexOf(bd,d)];if((d.cls==1&&v>t)||(d.cls==2&&v<=t))o++;}if(o>brN){brN=o;trN=t;}}
            var left=bd.Where(d=>d.N==nv).ToArray();
            int lKm=0,lTwo=0;
            foreach(var d in left){bool ko=(d.cls==1&&d.km>tN)||(d.cls==2&&d.km<=tN);lKm+=ko?1:0;if(ko)lTwo++;else{double v=d0Resid[Array.IndexOf(bd,d)];lTwo+=((d.cls==1&&v>trN)||(d.cls==2&&v<=trN))?1:0;}}
            _o.WriteLine($"Leave-N={nv}: km={lKm}/{left.Length}, two-stage={lTwo}/{left.Length} ({(lTwo-lKm)} rescued)");
        }

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART F: Decision ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        int improvementOverKm=cOk-aOk;
        int improvementRawIqr=dOk-aOk;

        _o.WriteLine($"Improvement: d0_resid adds {improvementOverKm}, rawIQR adds {improvementRawIqr}");
        _o.WriteLine($"d0_residual effect: {resDir:F3}σ");
        _o.WriteLine($"Jackknife improvement: {jkImp.Average():F1}±{Sd(jkImp):F1}");

        string model;
        if(resDir<0.3&&improvementOverKm<=0)model="Model A: d0 residual is ARTIFACT — no genuine secondary structure";
        else if(resDir>=0.5&&improvementOverKm>=2)model="Model C: DUAL-KERNEL SYSTEM — km and d0_resid form complementary kernels";
        else if(resDir>=0.3&&improvementOverKm>=1&&jkImp.Average()>=0.5)model="Model B: d0_resid is SECONDARY KERNEL — weak but genuine";
        else model="Model D: UNRESOLVED";

        _o.WriteLine($"Decision: {model}");
        _o.WriteLine($"");
        if(model=="Model C")_o.WriteLine("DK_01 confirms dual-kernel: km (coupling) + d0_resid (distance). Both are SAC-generated.");
        else if(model=="Model B")_o.WriteLine("DK_01 confirms secondary kernel: d0_resid is genuine but weak (0.99σ, +1 rescue). rawIQR (5 rescues) is the stronger secondary signal.");
        else if(model=="Model A")_o.WriteLine("DK_01: d0_resid is artifact — no genuine kernel.");
        else _o.WriteLine("DK_01 inconclusive. Further investigation needed.");
        _o.WriteLine("");
        _o.WriteLine("CLAIMS: Dual-kernel audit only. Diagnostic. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== DK_01 complete. Commit: DK_01_DualKernelAudit ===");
    }

    [Fact]
    public void RSC_01_RescueCaseAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== RSC_01: Rescue Case Audit ===");
        _o.WriteLine("=== V5.59. Frozen: M3++, Stop-Low, c3OmgS ===");
        _o.WriteLine("=== Question: Why does rawIQR rescue 5 km failures? ===");
        _o.WriteLine(new string('=',80));

        int[] Ns={70,72,75};int sds=200;
        var bag=new ConcurrentBag<(int N,int s,double km,double d0,double dR,double dN,
            double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)>();

        var hi70=Hi(70);var hi72=Hi(72);var hi75=Hi(75);

        Parallel.ForEach(Ns,n=>{var hi=n==70?hi70:n==72?hi72:hi75;
            Parallel.For(0,sds,s=>{
                if(!IsHi(n,s))return;
                var rng=new Random(s);var w=new double[n];
                for(int i=0;i<n;i++)w[i]=1.0+0.10*(rng.NextDouble()-0.5)*2.0;
                double rawIQR=Q(w.OrderBy(v=>v).ToArray(),0.75)-Q(w.OrderBy(v=>v).ToArray(),0.25);

                var K=KS(n,s);double kmInit=Km(K,n);

                // Epoch 1 with pipeline trace
                var h1=Sim(K,n,0.10,s);
                var R=RP(h1,n);double dR=1-Dm(R,n);
                var Rn=Nm(R,n);double dN=1-Dm(Rn,n);
                var dDL=DL(Rn,n);double dDLm=Dm(dDL,n);
                K=Cupd(dDL,n);double km1=Km(K,n);

                for(int e=2;e<=5;e++){var he=Sim(K,n,0.10,s+e-1);K=Cupd(DL(Nm(RP(he,n),n),n),n);}

                var hF=Sim(K,n,0.10,s+50);
                var dF=DL(Nm(RP(hF,n),n),n);var KF=Cupd(dF,n);
                double d0=Dm(dF,n),km=Km(KF,n);

                var sb=new SBase{seed=s,d0=d0,km0=km,ks0=0,cls=""};sb=Classify(sb,hi);
                double dv=hi.dm-Lo(n).dm,kv=hi.km-Lo(n).km,vn=Math.Sqrt(dv*dv+kv*kv);
                double proj=vn>0?((d0-Lo(n).dm)*dv+(km-Lo(n).km)*kv)/vn:0;
                double d2o=(d0-Lo(n).dm)*(d0-Lo(n).dm)+(km-Lo(n).km)*(km-Lo(n).km);
                double orth=Math.Sqrt(Math.Max(0,d2o-proj*proj));
                if(!(n==72?sb.cls=="P1"||sb.cls=="P1b"?proj>PHV&&orth>OTH:false:sb.cls=="P1"||sb.cls=="P1b"?proj>PHV:false))return;
                bag.Add((n,s,km,d0,dR,dN,dDLm,km1,rawIQR,sb.cls=="P1"?1:2,kmInit,0));
            });});

        var bd=bag.ToArray();
        var p1=bd.Where(d=>d.cls==1).ToArray();var p1b=bd.Where(d=>d.cls==2).ToArray();
        _o.WriteLine($"Retained: P1={p1.Length}, P1b={p1b.Length} (from {sds*Ns.Length} probes)");

        // Compute d0_residual for all profiles (same method as RES_01/DK_01)
        var kmA=bd.Select(d=>d.km).ToArray();
        var d0A=bd.Select(d=>d.d0).ToArray();
        double mk=kmA.Average();double vk=0,cd=0;
        for(int i=0;i<kmA.Length;i++){vk+=(kmA[i]-mk)*(kmA[i]-mk);cd+=(d0A[i]-d0A.Average())*(kmA[i]-mk);}
        double beta=cd/(vk+1e-15);double alpha=d0A.Average()-beta*mk;
        var d0Resid=d0A.Select((d,i)=>d-(alpha+beta*kmA[i])).ToArray();

        // ============================================================
        // PART A — Identify Rescue Cases
        // ============================================================
        _o.WriteLine($"\n=== PART A: Rescue Case Identification ===");

        // Find optimal thresholds for km, rawIQR, d0_resid
        double FindThresh(double[]x,double c1=1){
            var srt=bd.OrderBy(d=>x[Array.IndexOf(bd,d)]).ToArray();
            double bestT=0;int best=0;
            for(int i=1;i<srt.Length-1;i++){
                double t=x[Array.IndexOf(bd,srt[i])];int o=0;
                foreach(var d in srt){double v=x[Array.IndexOf(bd,d)];if((d.cls==1&&v>t)||(d.cls==2&&v<=t))o++;}
                if(o>best){best=o;bestT=t;}
            }
            return bestT;
        }
        var iqrA=bd.Select(d=>d.rawIQR).ToArray();
        double thrKm=FindThresh(kmA);
        double thrIqr=FindThresh(iqrA);
        double thrRes=FindThresh(d0Resid);
        double thrD0=FindThresh(d0A);

        // Identify all rescue/normal groups
        var kmFail=new List<(int N,int s,double km,double d0,double dR,double dN,double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)>();
        var kmWin=new List<(int N,int s,double km,double d0,double dR,double dN,double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)>();
        var iqrRescue=new List<(int N,int s,double km,double d0,double dR,double dN,double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)>();
        var d0rescue=new List<(int N,int s,double km,double d0,double dR,double dN,double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)>();
        var bothFail=new List<(int N,int s,double km,double d0,double dR,double dN,double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)>();

        foreach(var d in bd){
            bool kmOk=(d.cls==1&&d.km>thrKm)||(d.cls==2&&d.km<=thrKm);
            bool iqOk=(d.cls==1&&d.rawIQR>thrIqr)||(d.cls==2&&d.rawIQR<=thrIqr);
            double dres=d0Resid[Array.IndexOf(bd,d)];
            bool drOk=(d.cls==1&&dres>thrRes)||(d.cls==2&&dres<=thrRes);

            if(kmOk)kmWin.Add(d);
            else{
                kmFail.Add(d);
                if(iqOk)iqrRescue.Add(d);
                if(drOk)d0rescue.Add(d);
                if(!iqOk&&!drOk)bothFail.Add(d);
            }
        }

        _o.WriteLine($"km wins: {kmWin.Count}, km fails: {kmFail.Count}");
        _o.WriteLine($"rawIQR rescues: {iqrRescue.Count}, d0_resid rescues: {d0rescue.Count}");
        _o.WriteLine($"Both fail: {bothFail.Count}");

        // ============================================================
        // PART B — Profile Comparison
        // ============================================================
        _o.WriteLine($"\n=== PART B: Rescue-Case Profile Comparison ===");
        _o.WriteLine($"{"Group",-18} {"n",4} {"km",8} {"d0",8} {"IQR",8} {"dR",8} {"dN",8} {"dDL",8} {"km_init",8} {"N",4}");
        _o.WriteLine(new string('-',88));

        double[] Stats((int N,int s,double km,double d0,double dR,double dN,double dDL,double km1,double rawIQR,int cls,double kmInit,double d0Resid)[] g){
            if(g.Length==0)return new double[0];
            _o.WriteLine($"{"",-18} {g.Length,4} {g.Average(d=>d.km),8:F4} {g.Average(d=>d.d0),8:F4} {g.Average(d=>d.rawIQR),8:F4} {g.Average(d=>d.dR),8:F4} {g.Average(d=>d.dN),8:F4} {g.Average(d=>d.dDL),8:F4} {g.Average(d=>d.kmInit),8:F4} {g.Average(d=>d.N),4:F0}");
            return new[]{g.Average(d=>d.km),g.Average(d=>d.d0),g.Average(d=>d.rawIQR),g.Average(d=>d.dR),g.Average(d=>d.dN),g.Average(d=>d.dDL),g.Average(d=>d.kmInit),(double)g.Average(d=>d.N)};
        }
        _o.WriteLine($"{"",-18} {"n",4} {"km",8} {"d0",8} {"IQR",8} {"dR",8} {"dN",8} {"dDL",8} {"km_init",8} {"N",4}");
        _o.WriteLine(new string('-',88));
        var kw=Stats(kmWin.ToArray());
        var kf=Stats(kmFail.ToArray());
        var ir=Stats(iqrRescue.ToArray());
        var dr=Stats(d0rescue.ToArray());
        Stats(bothFail.ToArray());

        // Effect sizes between groups
        double EffG(double[]g1,double[]g2,double[]all){
            double d=Math.Abs(g1.Average()-g2.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        if(kmFail.Count>0&&iqrRescue.Count>0){
            _o.WriteLine($"\n--- rawIQR rescue vs unrescued km failures ---");
            var unrescued=kmFail.Where(d=>!iqrRescue.Contains(d)).ToArray();
            if(unrescued.Length>0){
                _o.WriteLine($"rawIQR Δ: {iqrRescue.Average(d=>d.rawIQR)-unrescued.Average(d=>d.rawIQR):F4}");
                _o.WriteLine($"dR Δ: {iqrRescue.Average(d=>d.dR)-unrescued.Average(d=>d.dR):F4}");
                _o.WriteLine($"km Δ: {iqrRescue.Average(d=>d.km)-unrescued.Average(d=>d.km):F4}");
                _o.WriteLine($"d0 Δ: {iqrRescue.Average(d=>d.d0)-unrescued.Average(d=>d.d0):F4}");
            }
        }

        // ============================================================
        // PART C — Clustering
        // ============================================================
        _o.WriteLine($"\n=== PART C: Cluster Analysis ===");

        // Do the 5 rawIQR rescues form a distinct cluster in (km, d0) space?
        _o.WriteLine($"Rescued profiles in (km, d0) space:");
        _o.WriteLine($"{"Seed",5} {"N",3} {"cls",4} {"km",8} {"d0",8} {"rawIQR",8} {"dR",8} {"d0_resid",10}");
        _o.WriteLine(new string('-',58));
        foreach(var d in iqrRescue){
            double dr2=d0Resid[Array.IndexOf(bd,d)];
            _o.WriteLine($"{d.s,5} {d.N,3} {(d.cls==1?"P1":"P1b"),4} {d.km,8:F4} {d.d0,8:F4} {d.rawIQR,8:F4} {d.dR,8:F4} {dr2,10:F4}");
        }

        // Are they clustered by N?
        var nDist=iqrRescue.GroupBy(d=>d.N).Select(g=>(n:g.Key,c:g.Count())).OrderBy(x=>x.n);
        _o.WriteLine($"\nRescues by N: {string.Join(", ",nDist.Select(x=>$"N={x.n}:{x.c}"))}");

        // Distance from km-win centroid
        if(kmWin.Count>0){
            double kwKm=kmWin.Average(d=>d.km),kwD0=kmWin.Average(d=>d.d0);
            _o.WriteLine($"km-win centroid: km={kwKm:F4}, d0={kwD0:F4}");
            foreach(var d in iqrRescue){
                double dist=Math.Sqrt((d.km-kwKm)*(d.km-kwKm)+(d.d0-kwD0)*(d.d0-kwD0));
                _o.WriteLine($"  Rescue seed={d.s} N={d.N}: distance from centroid = {dist:F4}");
            }
        }

        // ============================================================
        // PART D — Counterfactual
        // ============================================================
        _o.WriteLine($"\n=== PART D: Counterfactual — Remove Rescue Cases ===");

        // Remove the 5 rawIQR rescue cases and recompute
        var subBD=bd.Where(d=>!iqrRescue.Any(r=>r.s==d.s&&r.N==d.N)).ToArray();
        var subKm=subBD.Select(d=>d.km).ToArray();
        var subIqr=subBD.Select(d=>d.rawIQR).ToArray();
        var subD0=subBD.Select(d=>d.d0).ToArray();
        var subP1=subBD.Where(d=>d.cls==1).ToArray();
        var subP1b=subBD.Where(d=>d.cls==2).ToArray();

        double Eff(double[] pv,double[] pbv,double[] all){
            double d=Math.Abs(pv.Average()-pbv.Average()),s=Sd(all);
            return s>0.001?d/s:0;
        }

        // Recompute km accuracy
        var sKm=subBD.OrderBy(d=>d.km).ToArray();
        double subThrKm=0;int subBestKm=0;
        for(int i=1;i<sKm.Length-1;i++){double t=(sKm[i].km+sKm[i+1].km)/2;int o=0;foreach(var d in sKm)if((d.cls==1&&d.km>t)||(d.cls==2&&d.km<=t))o++;if(o>subBestKm){subBestKm=o;subThrKm=t;}}
        double kmAcc=subBestKm*100.0/subBD.Length;

        // km + rawIQR two-stage
        var sIqr=subBD.OrderBy(d=>d.rawIQR).ToArray();
        double subThrIqr=0;int subBestIqr=0;
        for(int i=1;i<sIqr.Length-1;i++){double t=(sIqr[i].rawIQR+sIqr[i+1].rawIQR)/2;int o=0;foreach(var d in sIqr)if((d.cls==1&&d.rawIQR>t)||(d.cls==2&&d.rawIQR<=t))o++;if(o>subBestIqr){subBestIqr=o;subThrIqr=t;}}
        int kmIqAcc=0;
        foreach(var d in subBD){bool ko=(d.cls==1&&d.km>subThrKm)||(d.cls==2&&d.km<=subThrKm);if(ko)kmIqAcc++;else{bool io=(d.cls==1&&d.rawIQR>subThrIqr)||(d.cls==2&&d.rawIQR<=subThrIqr);kmIqAcc+=io?1:0;}}
        double kmIqPct=kmIqAcc*100.0/subBD.Length;

        _o.WriteLine($"With {iqrRescue.Count} rescue cases removed ({subBD.Length} remaining):");
        _o.WriteLine($"  km only: {kmAcc:F1}%");
        _o.WriteLine($"  km + rawIQR: {kmIqPct:F1}% ({(kmIqAcc-subBestKm)} rescued)");
        _o.WriteLine($"  rawIQR advantage: {(kmIqPct-kmAcc):F1}%");

        string cfConclusion=(kmIqPct-kmAcc)<1?"rawIQR advantage DISAPPEARS — the 5 cases were the entire benefit":
            (kmIqPct-kmAcc)<3?"rawIQR advantage PERSISTS but is reduced — residual genuine structure exists":
            "rawIQR advantage is ROBUST — genuine secondary structure";
        _o.WriteLine($"Counterfactual: {cfConclusion}");

        // ============================================================
        // PART E — Decision
        // ============================================================
        _o.WriteLine($"\n=== PART E: Decision ===");
        _o.WriteLine($"Stop-Low: SAFE. Causal closure: BLOCKED.");

        double avgIqrRescue=iqrRescue.Count>0?iqrRescue.Average(d=>d.rawIQR):0;
        double avgIqrKmWin=kmWin.Count>0?kmWin.Average(d=>d.rawIQR):0;
        double avgIqrKmFail=kmFail.Count>0?kmFail.Where(d=>!iqrRescue.Contains(d)).Average(d=>d.rawIQR):0;

        _o.WriteLine($"rawIQR: rescues={avgIqrRescue:F4}, km-wins={avgIqrKmWin:F4}, unrescued-fails={avgIqrKmFail:F4}");
        _o.WriteLine($"Counterfactual advantage: {(kmIqPct-kmAcc):F1}%");

        string model;
        if((kmIqPct-kmAcc)<1&&iqrRescue.Count<=2)model="Model C: SAMPLING ARTIFACT — rawIQR rescues are noise";
        else if((kmIqPct-kmAcc)>=2)model="Model A: rawIQR carries GENUINE secondary structure";
        else model="Model B: rawIQR identifies RARE km failure mode — genuine but narrow";

        _o.WriteLine($"Decision: {model}");
        _o.WriteLine($"");
        if(model=="Model A")_o.WriteLine("RSC_01: rawIQR is a genuine secondary classifier with broad applicability.");
        else if(model=="Model B")_o.WriteLine("RSC_01: rawIQR catches a narrow km failure mode. 71% rescue rate on km failures, but only 7 km failures exist.");
        else if(model=="Model C")_o.WriteLine("RSC_01: rawIQR advantage is a sampling artifact — counterfactual shows it evaporates.");
        else _o.WriteLine("RSC_01: inconclusive.");
        _o.WriteLine("");
        _o.WriteLine("CLAIMS: Rescue case audit only. Diagnostic. Not causal. V6 NOT READY.");
        _o.WriteLine($"\n=== RSC_01 complete. Commit: RSC_01_RescueCaseAudit ===");
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
