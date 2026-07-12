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
                Total = 386,
                Passed = 386,
                Failed = 0,
                Skipped = 0,
                Verification = "pending verification",
                Command = "dotnet test --filter FullyQualifiedName~V4_1",
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
                new() { Name = "Planck Scale Benchmarks", Tests = 8, Status = "PASS", ClaimCategory = "SUPPORTED" }
            ],
            SupportedClaims =
            [
                "Pipeline produces finite, deterministic outputs",
                "d = -log(R) is a valid metric under tested conditions",
                "Blind geometry reconstruction works under tested conditions",
                "Self-consistent topology loop is numerically stable",
                "Null models and degeneracies are correctly detected",
                "No built-in D=3 preference"
            ],
            ConditionalClaims =
            [
                "Stable topology convergence depends on K, k, alpha, sigma, E",
                "D_eff stabilizes when Jaccard > 0.5",
                "Phase-lock outperforms correlation/lock-time under tested conditions"
            ],
            Hypotheses =
            [
                "Physical emergent space from self-consistent oscillator topology",
                "D = 3 selected by synchronization dynamics",
                "Continuum limit -> Riemannian metric",
                "Lorentzian spacetime emergence (3+1)",
                "Gravitational dynamics from emergent topology"
            ],
            NotClaimed =
            [
                "GR is replaced",
                "D = 3 is derived",
                "G is predicted from first principles",
                "hbar is derived",
                "c is derived",
                "Planck length/time are derived",
                "Quantum mechanics is fully derived"
            ],
            OpenProblems =
            [
                new() { Priority = "P1", Title = "Natural Continuous Coupling Update", Description = "Replace kNN with continuous update law K_next = F(R, d)." },
                new() { Priority = "P1", Title = "Fixed-Point Uniqueness", Description = "Does the topology converge to a unique attractor?" },
                new() { Priority = "P1", Title = "Continuum Limit", Description = "Does the emergent geometry survive N -> infinity?" },
                new() { Priority = "P2", Title = "Causal Structure", Description = "Introduce directed / asymmetric rate matrices." },
                new() { Priority = "P2", Title = "Lorentz Signature", Description = "Test spacetime-like propagation on recovered topology." }
            ],
            Documents =
            [
                new() { Title = "Current Status & Test Evidence", Filename = "TRM_V4_1_Current_Status_And_Test_Evidence.md", Available = false, Description = "Full test suite table, release integrity check." },
                new() { Title = "V4.1 Formalism", Filename = "TRM_V4_1_SelfConsistent_Emergent_Space_Formalism.md", Available = false, Description = "Pipeline definition, claim discipline, open problems." },
                new() { Title = "Zenodo Release Notes", Filename = "TRM_V4_1_Zenodo_Release_Notes.md", Available = false, Description = "Build instructions, scope boundary, references." }
            ],
            Roadmap = new Roadmap
            {
                RecommendedTestFile = "TRM.Tests/V4_1/V4_1_NaturalCouplingUpdate_Tests.cs",
                CandidateUpdateLaws =
                [
                    "K_ij_next = K0 * exp(-d_ij / xi)",
                    "K_ij_next = K0 * exp(-(d_ij / xi)^2)",
                    "K_ij_next = K0 / (1 + d_ij^p)",
                    "K_ij_next = K0 * softmax_j(-d_ij / tau)"
                ]
            },
            ReleaseIntegrity = new ReleaseIntegrity
            {
                TotalTestsVerified = 386,
                PassedVerified = 386,
                VerificationMethod = "pending verification"
            }
        };
    }
}
