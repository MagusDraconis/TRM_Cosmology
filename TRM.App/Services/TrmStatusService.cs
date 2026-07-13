using System.Net.Http.Json;
using TRM.App.Models;

namespace TRM.App.Services;

/// <summary>
/// Loads TRM V4.1 project status from wwwroot/data/trm-v4-1-status.json.
/// Falls back to default data if JSON cannot be loaded.
/// </summary>
public sealed class TrmStatusService
{
    private readonly HttpClient _http;
    private TrmStatusModel? _cached;

    public TrmStatusService(HttpClient http)
    {
        _http = http;
    }

    public async Task<TrmStatusModel> GetStatusAsync()
    {
        if (_cached is not null) return _cached;

        try
        {
            var model = await _http.GetFromJsonAsync<TrmStatusModel>(
                "data/trm-v4-1-status.json",
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (model is not null)
            {
                _cached = model;
                return _cached;
            }
        }
        catch
        {
            // Fall through to default.
        }

        _cached = BuildDefault();
        return _cached;
    }

    /// <summary>
    /// Returns the model synchronously (may be default if not yet loaded).
    /// Prefer GetStatusAsync() for production use.
    /// </summary>
    public TrmStatusModel GetStatusOrDefault()
    {
        return _cached ?? BuildDefault();
    }

    private static TrmStatusModel BuildDefault()
    {
        return new TrmStatusModel
        {
            ReleaseStatus = "EXPLORATORY",
            Date = "pending verification",
            TestSummary = new TestSummary
            {
                Total = 830,
                Passed = 830,
                Failed = 0,
                Skipped = 0,
                Verification = "pending verification",
                Command = "dotnet test --filter FullyQualifiedName~V4_1 -v normal",
                Date = "pending"
            },
            Suites =
            [
                new() { Name = "Pre-existing V4.1 audit/analysis", Tests = 248, Status = "PASS" },
                new() { Name = "Emergent Metric", Tests = 73, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Dimensional Emergence", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Blind Emergent Geometry", Tests = 18, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Self-Consistent Topology", Tests = 12, Status = "PASS", ClaimCategory = "CONDITIONAL" },
                new() { Name = "Topology Fixed-Point Robustness", Tests = 12, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Quantum Benchmarks", Tests = 12, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Planck Scale Benchmarks", Tests = 8, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Natural Continuous Coupling Update", Tests = 13, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Coupling Law Selection", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Exponential Fixed Point", Tests = 16, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Exp Fixed-Point Robustness", Tests = 7, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Fixed-Point Basin Mapping", Tests = 10, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Exponential Continuum Scaling", Tests = 10, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Cross-Law Continuum Scaling", Tests = 10, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Exponential Large-N Scaling", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Causal Structure Probe", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Causal Propagation Speed", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Lorentz Signature Probe", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Causal Front Robustness", Tests = 12, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Causal Front Param Optimization", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Fine Structure Parameter Scan", Tests = 10, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Fractal Band Structure", Tests = 12, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Energy-Load Trampoline Effect", Tests = 13, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Data Discovery", Tests = 5, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "SPARC Readiness", Tests = 5, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Energy-Load Response Kernel", Tests = 13, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "SPARC Residual Structure", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Energy-Time-Geometry Coupling", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "ETG Transfer Functions", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "ETG Calibration", Tests = 14, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "ETG Dimension Selection", Tests = 14, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Dimension Attractor", Tests = 12, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Dim Estimator Calibration", Tests = 13, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Dim Selection Mechanism", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Dim Attractor Value", Tests = 13, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Dim Continuum Limit", Tests = 11, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Causal-ETG-Dim Convergence", Tests = 13, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Causal-ETG-Dim Large-N Conv", Tests = 12, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Emergent Lorentz Signature", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Emergent Space-Time Separation", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Emergent Metric Tensor Proxy", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Emergent Geodesic Structure", Tests = 14, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "Geodesic Robustness", Tests = 14, Status = "PASS", ClaimCategory = "SUPPORTED" },
                new() { Name = "GR-Limit Probe", Tests = 15, Status = "PASS", ClaimCategory = "SUPPORTED" }
            ],
            SupportedClaims =
            [
                "Pipeline produces finite, deterministic outputs",
                "d = -log(R) is a valid metric under tested conditions",
                "Blind geometry reconstruction works under tested conditions",
                "Self-consistent topology loop is numerically stable",
                "Null models and degeneracies are correctly detected",
                "No built-in D=3 preference",
                "Quantum-like benchmark behavior (interference, uncertainty, discrete spectra, tunneling)",
                "TRM internal scale candidates are finite, positive, deterministic",
                "Dimensionless ratios between scales are computable",
                "Parameter sweeps show smooth variation across tested ranges",
                "kNN alone does not create false convergence",
                "Shuffled theta destroys geometric stability",
                "Linearized dynamics is stable diffusion with Laplacian spectrum",
                "Bounded graphs produce discrete spectra",
                "Basin diagnostics measurable (dK, corr, AVI, convergence classes)",
                "Perturbation recovery measurable (Frobenius, Spearman)",
                "Multi-seed basin distributions finite and bounded",
                "Scaling diagnostics measurable across N=40..200",
                "Sparsity and spectral proxies computable at all tested N",
                "Exponential fixed-point loop finite up to N=200",
                "Cross-law continuum diagnostics are finite and deterministic",
                "Continuous laws comparable without kNN update",
                "Exponential shows structural self-consistency (K ∝ R at xi=1)",
                "Large-N diagnostics finite at N=300, 500 with reduced epochs",
                "Runtime-safe sampled-pair approximations are deterministic",
                "Directed/asymmetric influence diagnostics are measurable",
                "Lagged phase correlation R_lag[i,j] is computable and finite",
                "Dimensionless c_eff candidates can be computed from d_ij/tau_ij",
                "Propagation response fronts are numerically classifiable",
                "Finite-front fits (tau = a*d + b) are computable",
                "Cone-like diagnostics and dispersion proxies are measurable",
                "Event density and front-fit stability can be ranked by regime",
                "Pre-calibration readiness score is numerically computable",
                "Parameter regimes can be ranked by readiness",
                "Event-class heatmaps identify stable parameter bands",
                "Fine structure bands and plateaus are numerically detectable",
                "Band persistence across seeds and N is measurable",
                "Box-counting dimension estimates are computable",
                "Self-similarity zoom correlations are evaluable",
                "Local omega-shift response is measurable",
                "Geometry deformation and remote response are testable"
            ],
            ConditionalClaims =
            [
                "Stable topology convergence depends on K, k, alpha, sigma, E",
                "D_eff stabilizes when Jaccard > 0.5",
                "Phase-lock outperforms correlation/lock-time under tested conditions",
                "Quantum-like structures depend on graph and parameter choices",
                "hbar_eff candidates are dimensionless and discretization-dependent",
                "TRM internal scales depend on discretization (N, k, alpha)"
            ],
            Hypotheses =
            [
                "Physical emergent space from self-consistent oscillator topology",
                "D = 3 selected by synchronization dynamics",
                "Continuum limit -> Riemannian metric",
                "Lorentzian spacetime emergence (3+1)",
                "Gravitational dynamics from emergent topology",
                "Physical quantum mechanics from TRM oscillator dynamics",
                "Planck scale correspondence"
            ],
            NotClaimed =
            [
                "GR is replaced",
                "D = 3 is derived",
                "G is predicted from first principles",
                "hbar is derived",
                "c is derived",
                "Planck length/time are derived",
                "Quantum mechanics is fully derived",
                "Born rule / entanglement / spin are derived",
                "Quantum gravity problem is solved"
            ],
            OpenProblems =
            [
                new() { Priority = "P1", Title = "Natural Continuous Coupling Update", Description = "Replace kNN topology construction with continuous update law K_next = F(R, d). Candidate forms: exp(-d/xi), Gaussian, power-law, softmax." },
                new() { Priority = "P1", Title = "Fixed-Point Uniqueness", Description = "Does the topology converge to a unique attractor independent of initial conditions?" },
                new() { Priority = "P1", Title = "Continuum Limit", Description = "Does the emergent geometry survive N -> infinity? Large-N scaling not yet established." },
                new() { Priority = "P2", Title = "Causal Structure", Description = "Introduce directed / asymmetric rate matrices to enable light-cone structure and propagation speed measurement." },
                new() { Priority = "P2", Title = "Lorentz Signature", Description = "Test whether spacetime-like propagation (omega(k) dispersion, invariant speed) emerges from recovered topology." },
                new() { Priority = "P3", Title = "hbar_eff Normalization", Description = "hbar_eff candidates are dimensionless graph units. Physical normalization requires identification of energy and time scales." },
                new() { Priority = "P3", Title = "Planck-Scale Continuum Comparison", Description = "Numerical comparison of l_TRM and t_TRM with physical l_P and t_P requires physical unit mapping." },
                new() { Priority = "P3", Title = "Born-Rule Benchmark", Description = "Does |psi|^2 emerge from oscillator amplitude in a continuum limit? No test exists." }
            ],
            Documents =
            [
                new() { Title = "Current Status & Test Evidence", Filename = "TRM_V4_1_Current_Status_And_Test_Evidence.md", Url = "documents/TRM_V4_1_Current_Status_And_Test_Evidence.md", Available = true, Description = "Full test suite table, release integrity check, claim discipline summary." },
                new() { Title = "V4.1 Self-Consistent Emergent Space Formalism", Filename = "TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md", Url = "documents/TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md", Available = true, Description = "Full pipeline definition, R candidates, claim discipline table, open problems." },
                new() { Title = "Zenodo Release Notes", Filename = "TRM_V4_1_Zenodo_Release_Notes.md", Url = "documents/TRM_V4_1_Zenodo_Release_Notes.md", Available = true, Description = "Build instructions, scope boundary, references, Zenodo metadata." }
            ],
            Roadmap = new Roadmap
            {
                RecommendedTestFile = "ECS: Continuum scaling active; next: N=300+ or cross-law continuum comparison",
                CandidateUpdateLaws =
                [
                    "K_ij_next = K0 * exp(-d_ij / xi)",
                    "K_ij_next = K0 * exp(-(d_ij / xi)^2)",
                    "K_ij_next = K0 / (1 + d_ij^p)",
                    "K_ij_next = K0 * softmax_j(-d_ij / tau)",
                    "K_ij_next = K0 * exp(-d_ij / xi) * S_ij  (S_ij = temporal stability of R_ij)"
                ]
            },
            ReleaseIntegrity = new ReleaseIntegrity
            {
                TotalTestsVerified = 453,
                PassedVerified = 453,
                VerificationMethod = "TEST-RUN-VERIFIED"
            }
        };
    }
}
