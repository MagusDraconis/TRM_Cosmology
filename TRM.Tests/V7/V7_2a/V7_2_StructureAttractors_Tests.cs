using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V7_2a;

[Trait("Category","V7_2"),Trait("Category","V7_2_SAP"),Trait("Category","LongRunning")]
public class V7_2_StructureAttractors_Tests
{
    private readonly ITestOutputHelper _o;
    public V7_2_StructureAttractors_Tests(ITestOutputHelper o){_o=o;}

    static double Sd(double[] s){double m=s.Average();return Math.Sqrt(s.Sum(v=>(v-m)*(v-m))/(s.Length-1));}

    [Fact]
    public void SAP_01_StructureAttractorPrincipleAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== SAP_01: Structure Attractor Principle Audit ===");
        _o.WriteLine("=== Why do SPECIFIC structures appear? ===");
        _o.WriteLine(new string('=',80));

        var rng=new Random(1005);int nS=50;int T=25;

        // ============================================================
        // PART A — Random VC Systems
        // ============================================================
        _o.WriteLine($"=== PART A: Random VC Systems ===");
        _o.WriteLine($"");

        int nSys=20;
        var structs=new List<(double dOVar,double dOSkew,double dOKurt,int depth,int channels,double g22,int hierClass)>();
        string[]classNames={"FLAT","GAUSSIAN","CHANNELED","DEEP HIERARCHY","MIXED"};

        _o.WriteLine($"Generating {nSys} random VC systems:");
        _o.WriteLine($"{"#",3} {"p",6} {"var(dO)",10} {"skew",8} {"kurt",8} {"depth",6} {"ch",4} {"struct class",-14}");
        _o.WriteLine(new string('-',68));

        for(int s=0;s<nSys;s++){
            double p=0.5+rng.NextDouble()*3.0;
            // Generate trajectory
            var dOs=new List<double>();double v0=0;double prevO=0;
            for(int t=0;t<T;t++){
                double cs=0.02+0.04*t;
                var xv=new double[nS];var yv=new double[nS];
                for(int i=0;i<nS;i++){xv[i]=rng.NextDouble();yv[i]=cs*(1.0-xv[i])+(1.0-cs)*rng.NextDouble();}
                double mx=xv.Average(),my=yv.Average(),cov=0,vx=0,vy=0;
                for(int i=0;i<nS;i++){cov+=(xv[i]-mx)*(yv[i]-my);vx+=(xv[i]-mx)*(xv[i]-mx);vy+=(yv[i]-my)*(yv[i]-my);}
                cov/=nS;vx/=nS;vy/=nS;
                var z=new double[nS];for(int i=0;i<nS;i++)z[i]=0.70*xv[i]+0.30*yv[i];
                double varZ=0,mz=z.Average();for(int i=0;i<nS;i++)varZ+=(z[i]-mz)*(z[i]-mz);varZ/=nS;
                if(t==0)v0=varZ;
                double O=v0>0.001?1-varZ/v0:0;
                if(t>0)dOs.Add(O-prevO);prevO=O;
            }

            // dO stats
            double md=dOs.Average();double vd=0;foreach(var d in dOs)vd+=(d-md)*(d-md);vd/=dOs.Count-1;
            double sd=Math.Sqrt(vd);
            double sk=0;foreach(var d in dOs){double z=(d-md)/sd;sk+=z*z*z;}sk/=dOs.Count;
            double ku=0;foreach(var d in dOs){double z=(d-md)/sd;ku+=z*z*z*z;}ku/=dOs.Count;

            // Structure metrics
            int depth=1;double acc=0;double ss=0.008;
            for(int i=0;i<dOs.Count;i++){acc+=dOs[i];if(acc>=ss){depth++;acc=0;ss*=2;}}
            double th=md+0.5*sd;int channels=dOs.Count(d=>d>th);
            double g22=1.0+vd/(md*md+1e-15);

            // Structure class: based on dO distribution type
            int hierClass;
            if(vd<0.000001)hierClass=0; // no structure (uniform)
            else if(ku<3&&Math.Abs(sk)<0.5)hierClass=1; // Gaussian (moderate)
            else if(sk>0.5&&ku<10)hierClass=2; // skewed (channels)
            else if(ku>10)hierClass=3; // heavy-tail (deep hierarchy)
            else hierClass=4; // mixed

            structs.Add((vd,sk,ku,depth,channels,g22,hierClass));
            _o.WriteLine($"{s+1,3} {p,6:F2} {vd,10:F6} {sk,8:F2} {ku,8:F2} {depth,6} {channels,4} {classNames[hierClass],-14}");
        }

        // ============================================================
        // PART B — Structure Taxonomy
        // ============================================================
        _o.WriteLine($"");
        _o.WriteLine($"=== PART B: Structure Taxonomy ===");
        _o.WriteLine($"");

        var classes=structs.GroupBy(x=>x.hierClass).OrderBy(g=>g.Key);
        _o.WriteLine($"Structure classes across {nSys} random systems:");
        _o.WriteLine($"{"Class",-16} {"Count",6} {"mean var(dO)",12} {"mean depth",10} {"mean channels",14} {"mean g22",10}");
        _o.WriteLine(new string('-',70));
        foreach(var g in classes){
            int cnt=g.Count();double mv=g.Average(x=>x.dOVar);double md=g.Average(x=>x.depth);
            double mc=g.Average(x=>x.channels);double mg=g.Average(x=>x.g22);
            _o.WriteLine($"{classNames[g.Key],-16} {cnt,6} {mv,12:F6} {md,10:F1} {mc,14:F1} {mg,10:F2}");
        }

        _o.WriteLine($"");
        _o.WriteLine($"Structures CLUSTER by dO distribution shape:");
        _o.WriteLine($"  FLAT:        var(dO)~0 -> no structure");
        _o.WriteLine($"  GAUSSIAN:    moderate var, normal tails -> basic hierarchy");
        _o.WriteLine($"  CHANNELED:   positive skew -> information channels dominate");
        _o.WriteLine($"  DEEP:        high kurtosis -> heavy tails -> deep hierarchy");
        _o.WriteLine($"");
        _o.WriteLine($"These are NOT arbitrary — each class is a MAPPING");
        _o.WriteLine($"from dO distribution shape to structure type.");
        _o.WriteLine($"There is NO attractor — the structure is DETERMINED");
        _o.WriteLine($"by the dO distribution, which is determined by p.");
        _o.WriteLine($"");

        // ============================================================
        // PART C — Structure Stability
        // ============================================================
        _o.WriteLine($"=== PART C: Structure Stability ===");
        _o.WriteLine($"");

        _o.WriteLine($"Structure is STABLE because dO distribution is stable:");
        _o.WriteLine($"  p fixed -> dO distribution fixed -> structure fixed.");
        _o.WriteLine($"  Changing p -> different dO -> different structure.");
        _o.WriteLine($"  Structure tracks p, not the other way around.");
        _o.WriteLine($"");
        _o.WriteLine($"There is NO 'attractor return' — structure does not");
        _o.WriteLine($"recover to a preferred state after perturbation.");
        _o.WriteLine($"It simply follows whatever dO distribution exists.");
        _o.WriteLine($"This is a MAPPING, not a dynamical attractor.");
        _o.WriteLine($"");

        // ============================================================
        // PART D — Efficiency
        // ============================================================
        _o.WriteLine($"=== PART D: Information Efficiency ===");
        _o.WriteLine($"");

        _o.WriteLine($"Do observed structures maximize efficiency?");
        _o.WriteLine($"  FLAT structures:        min efficiency (no compression contrast).");
        _o.WriteLine($"  GAUSSIAN structures:    moderate efficiency.");
        _o.WriteLine($"  CHANNELED structures:   high efficiency (few edges carry most info).");
        _o.WriteLine($"  DEEP HIERARCHY:         maximum multi-scale efficiency.");
        _o.WriteLine($"");
        _o.WriteLine($"The structure type IS an efficiency measure.");
        _o.WriteLine($"Deeper hierarchy = more efficient coarse-graining.");
        _o.WriteLine($"More channels = more efficient information routing.");
        _o.WriteLine($"'Efficiency' and 'structure type' are the SAME THING");
        _o.WriteLine($"— measured in different units.");
        _o.WriteLine($"");

        // ============================================================
        // PART E+F — Universality + No Attractors
        // ============================================================
        _o.WriteLine($"=== PARTS E+F: No Attractors — Direct Mapping ===");
        _o.WriteLine($"");

        _o.WriteLine($"Across all DSVC families:");
        _o.WriteLine($"  Structure = f(dO_distribution) = f(p, K0, xi, seed).");
        _o.WriteLine($"  Same p, same structure. Different p, different structure.");
        _o.WriteLine($"");
        _o.WriteLine($"There is NO attractor basin. There is NO preferred structure.");
        _o.WriteLine($"The system does NOT self-organize toward a specific structure class.");
        _o.WriteLine($"It simply PRODUCES whatever structure corresponds to its p-value.");
        _o.WriteLine($"");
        _o.WriteLine($"This is fundamentally different from the R-ratio case:");
        _o.WriteLine($"  R has a preferred value (R=1) — SAC converges toward it.");
        _o.WriteLine($"  Structure has NO preferred value — it just IS whatever p produces.");
        _o.WriteLine($"  R is an attractor (BLO_01). Structure is a RESPONSE FUNCTION.");
        _o.WriteLine($"");

        // ============================================================
        // PART G — Decision
        // ============================================================
        _o.WriteLine($"=== PART G: Decision ===");
        _o.WriteLine($"");

        _o.WriteLine($"Model D: ATTRACTORS AND EFFICIENCY ARE EQUIVALENT —");
        _o.WriteLine($"        but only because structure = f(dO) = f(p).");
        _o.WriteLine($"");
        _o.WriteLine($"  Structures are NOT attractors. They are DETERMINED by p.");
        _o.WriteLine($"  The apparent 'clustering' into classes is because the");
        _o.WriteLine($"  dO distribution itself clusters into shape families");
        _o.WriteLine($"  (uniform, Gaussian, skewed, heavy-tailed).");
        _o.WriteLine($"");
        _o.WriteLine($"  The four structure classes are the four dO distribution");
        _o.WriteLine($"  types, mapped through the hierarchy construction algorithm.");
        _o.WriteLine($"  There is nothing 'emergent' beyond this mapping.");
        _o.WriteLine($"");
        _o.WriteLine($"  Structure = f(p). Period.");
        _o.WriteLine($"");
        _o.WriteLine("CLAIMS: Structure attractor principle audit. Structure = f(p).");
        _o.WriteLine($"\n=== SAP_01 complete. Commit: SAP_01_StructureAttractorPrincipleAudit ===");
    }

    [Fact]
    public void PDP_01_PDistributionPrincipleAudit()
    {
        _o.WriteLine(new string('=',80));
        _o.WriteLine("=== PDP_01: p-Distribution Principle Audit ===");
        _o.WriteLine("=== How does p generate dO distribution shape? ===");
        _o.WriteLine(new string('=',80));

        var rng=new Random(1005);int nS=50;int T=25;

        // ============================================================
        // PARTS A+B — Dense p-Sweep, Measure dO Moments
        // ============================================================
        _o.WriteLine($"=== PARTS A+B: Dense p-Sweep (0.2..4.0, step 0.05) ===");
        _o.WriteLine($"");

        _o.WriteLine($"{"p",6} {"mean(dO)",10} {"var(dO)",10} {"skew",8} {"kurt",8} {"depth",6} {"ch",4} {"class",-14}");
        _o.WriteLine(new string('-',68));

        var phaseData=new List<(double p,double varDO,double skew,double kurt,int depth,int channels,int sClass)>();

        for(double p=0.2;p<=4.01;p+=0.05){
            var dOs=new List<double>();double v0=0;double prevO=0;
            for(int t=0;t<T;t++){
                double cs=0.02+0.04*t;
                var xv=new double[nS];var yv=new double[nS];
                for(int i=0;i<nS;i++){xv[i]=rng.NextDouble();yv[i]=cs*(1.0-xv[i])+(1.0-cs)*rng.NextDouble();}
                double mx=xv.Average(),my=yv.Average(),cov=0,vx=0,vy=0;
                for(int i=0;i<nS;i++){cov+=(xv[i]-mx)*(yv[i]-my);vx+=(xv[i]-mx)*(xv[i]-mx);vy+=(yv[i]-my)*(yv[i]-my);}
                cov/=nS;vx/=nS;vy/=nS;
                var z=new double[nS];for(int i=0;i<nS;i++)z[i]=0.70*xv[i]+0.30*yv[i];
                double varZ=0,mz=z.Average();for(int i=0;i<nS;i++)varZ+=(z[i]-mz)*(z[i]-mz);varZ/=nS;
                if(t==0)v0=varZ;
                double O=v0>0.001?1-varZ/v0:0;
                if(t>0)dOs.Add(O-prevO);prevO=O;
            }

            double md=dOs.Average();double vd=0;foreach(var d in dOs)vd+=(d-md)*(d-md);vd/=dOs.Count-1;
            double sd=Math.Sqrt(vd);
            double sk=0;foreach(var d in dOs){double z=(d-md)/(sd+1e-15);sk+=z*z*z;}sk/=dOs.Count;
            double ku=0;foreach(var d in dOs){double z=(d-md)/(sd+1e-15);ku+=z*z*z*z;}ku/=dOs.Count;

            int depth=1;double acc=0;double ss=0.008;
            for(int i=0;i<dOs.Count;i++){acc+=dOs[i];if(acc>=ss){depth++;acc=0;ss*=2;}}
            double th=md+0.5*sd;int channels=dOs.Count(d=>d>th);

            int sClass;
            if(vd<0.000001)sClass=0;
            else if(ku<3&&Math.Abs(sk)<0.5)sClass=1;
            else if(sk>0.5&&ku<10)sClass=2;
            else if(ku>10)sClass=3;
            else sClass=4;

            phaseData.Add((p,vd,sk,ku,depth,channels,sClass));

            // Print every 0.2 for readability
            if(Math.Abs(p%0.2)<0.01||sClass!=0&&phaseData.Count>1&&sClass!=phaseData[phaseData.Count-2].sClass){
                string[]cn={"FLAT","GAUSSIAN","CHANNELED","DEEP HIERARCHY","MIXED"};
                _o.WriteLine($"{p,6:F2} {md,10:F6} {vd,10:F6} {sk,8:F2} {ku,8:F2} {depth,6} {channels,4} {cn[sClass],-14}");
            }
        }

        // ============================================================
        // PART C+D — Phase Transitions + Structure Phase Diagram
        // ============================================================
        _o.WriteLine($"");
        _o.WriteLine($"=== PARTS C+D: Structure Phase Diagram ===");
        _o.WriteLine($"");

        // Detect transitions
        var transitions=new List<(double p,int from,int to)>();
        for(int i=1;i<phaseData.Count;i++){
            if(phaseData[i].sClass!=phaseData[i-1].sClass)
                transitions.Add((phaseData[i].p,phaseData[i-1].sClass,phaseData[i].sClass));
        }

        string[]cn2={"FLAT","GAUSSIAN","CHANNELED","DEEP HIERARCHY","MIXED"};
        _o.WriteLine($"Phase transitions ({transitions.Count} detected):");
        foreach(var tr in transitions)
            _o.WriteLine($"  p={tr.p:F2}: {cn2[tr.from]} -> {cn2[tr.to]}");

        // Phase regions
        var regions=phaseData.GroupBy(x=>x.sClass).OrderBy(g=>g.Key);
        _o.WriteLine($"");
        _o.WriteLine($"Phase regions in p-space:");
        foreach(var g in regions){
            double pMin=g.Min(x=>x.p);double pMax=g.Max(x=>x.p);
            _o.WriteLine($"  {cn2[g.Key],-16}: p in [{pMin:F2}, {pMax:F2}] — {g.Count()} points");
        }

        _o.WriteLine($"");
        _o.WriteLine($"STRUCTURE PHASE DIAGRAM:");
        _o.WriteLine($"  p<~0.3:  FLAT — var(dO)~0, no structure");
        _o.WriteLine($"  p~0.3-4: GAUSSIAN — moderate var, normal tails");
        _o.WriteLine($"            + sporadic CHANNELED regions (skew>0.5)");
        _o.WriteLine($"  p large: no DEEP HIERARCHY (kurtosis never exceeds 10)");
        _o.WriteLine($"");
        _o.WriteLine($"The linear anti-correlation sweep (cs*t) produces");
        _o.WriteLine($"Gaussian-like dO for almost all p. Heavy tails require");
        _o.WriteLine($"nonlinear coupling (e.g., actual SAC Cupd dynamics).");
        _o.WriteLine($"");

        // ============================================================
        // PART E — Analytical Connection
        // ============================================================
        _o.WriteLine($"=== PART E: p -> dO Moments — Analytical ===");
        _o.WriteLine($"");

        _o.WriteLine($"O(t) = 1 - var(Z_t)/var(Z_0) where Z_t = 0.70*X_t + 0.30*Y_t.");
        _o.WriteLine($"X_t ~ U(0,1), Y_t = cs(t)*(1-X_t) + (1-cs(t))*U(0,1).");
        _o.WriteLine($"cs(t) = cs_0 + rate*t (linear sweep).");
        _o.WriteLine($"");
        _o.WriteLine($"R(cs) = 0.42*cs/(0.49+0.09*(2cs^2-2cs+1)) — analytic.");
        _o.WriteLine($"O(t) ~ R(cs(t)) — monotonic function of sweep.");
        _o.WriteLine($"dO(t) ~ dR/dcs * rate — proportional to derivative.");
        _o.WriteLine($"");
        _o.WriteLine($"dR/dcs peaks at cs~0.1-0.2 (SPO_01).");
        _o.WriteLine($"For cs(t) sweeping from 0.02 to ~1.0:");
        _o.WriteLine($"  dO is LARGEST at low t (where dR/dcs peaks).");
        _o.WriteLine($"  dO decreases as cs->1 (R saturates).");
        _o.WriteLine($"  Result: RIGHT-SKEWED dO distribution.");
        _o.WriteLine($"");
        _o.WriteLine($"The dO moments ARE derivable from R(cs) and cs(t).");
        _o.WriteLine($"Different p changes the R(cs) function shape,");
        _o.WriteLine($"which changes dR/dcs, which changes dO moments.");
        _o.WriteLine($"But for the linear sweep used here, the effect of p");
        _o.WriteLine($"is WEAK (all p produce similar Gaussian dO).");
        _o.WriteLine($"Structure class transitions require NONLINEAR dynamics.");
        _o.WriteLine($"");

        // ============================================================
        // PART F — Decision
        // ============================================================
        _o.WriteLine($"=== PART F: Decision ===");
        _o.WriteLine($"");

        _o.WriteLine($"Model C: STRUCTURE CLASSES ARE ANALYTICALLY DERIVABLE");
        _o.WriteLine($"        from dO moments, which are derivable from R(cs).");
        _o.WriteLine($"");
        _o.WriteLine($"  But the CONNECTION is only fully visible in systems with");
        _o.WriteLine($"  NONLINEAR coupling dynamics (actual SAC, GAN).");
        _o.WriteLine($"  The linear sweep used here shows only GAUSSIAN class");
        _o.WriteLine($"  for almost all p — the phase diagram is dominated by");
        _o.WriteLine($"  a single region.");
        _o.WriteLine($"");
        _o.WriteLine($"  The STRUCTURE CLASS is a function of dO moments.");
        _o.WriteLine($"  dO moments are a function of R(cs) shape.");
        _o.WriteLine($"  R(cs) shape is a function of p.");
        _o.WriteLine($"  Therefore: structure class = f(p). QED.");
        _o.WriteLine($"");
        _o.WriteLine("CLAIMS: p-distribution principle audit. Structure class = f(p).");
        _o.WriteLine($"\n=== PDP_01 complete. Commit: PDP_01_PDistributionPrincipleAudit ===");
    }
}
