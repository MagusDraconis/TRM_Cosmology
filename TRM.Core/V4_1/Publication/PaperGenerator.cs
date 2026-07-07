using System.Text;
using TRM.Core.V4_1.Analysis;
using TRM.Core.V4_1.Reporting;
using TRM.Core.V4_1.Sync;

namespace TRM.Core.V4_1.Publication;

/// <summary>
/// Generates publication-ready paper sections from validated analysis outputs.
/// Classification: FRAMEWORK — no claims beyond analysis results.
/// </summary>
public static class PaperGenerator
{
    public static PaperDocument Generate(
        List<SyncParameterScanResult> scanResults,
        List<DimensionAnalysisResult> analysis,
        List<AnalysisStatement> conclusions)
    {
        var doc = new PaperDocument
        {
            Title = "TRM V4.1 — Emergent Space and Dimensional Diagnostics",
            Sections = new List<PaperSection>
            {
                BuildAbstract(conclusions),
                BuildIntroduction(),
                BuildMethods(),
                BuildResults(analysis, conclusions),
                BuildLimitations(),
                BuildConclusion()
            },
            ClaimEvidence = BuildClaimEvidence(analysis, conclusions)
        };
        return doc;
    }

    private static PaperSection BuildAbstract(List<AnalysisStatement> conclusions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("TRM V4.1 extends the V4 interpretation layer toward emergent spacetime ");
        sb.AppendLine("by defining spatial structure as graph distance on a coupled-oscillator network. ");
        sb.AppendLine("Deterministic parameter scans across graph families D = 1–4 measure ");
        sb.AppendLine("synchronization quality, bridge-band spread, and perturbation recovery. ");
        sb.AppendLine("Structured analysis of scan outputs yields conditional conclusions — ");
        sb.AppendLine("no claim of dimensional selection is made. ");
        sb.AppendLine("All results are reproducible, falsifiable, and reviewer-verifiable.");
        return new PaperSection { Title = "Abstract", Content = sb.ToString().Replace("\n", " ") };
    }

    private static PaperSection BuildIntroduction()
    {
        return new PaperSection
        {
            Title = "1. Introduction",
            Content = "V4 provides time from oscillator dynamics but treats space as an external embedding. " +
                      "V4.1 asks whether space and causal propagation can be expressed in TRM-native terms. " +
                      "The central hypothesis: space emerges as graph distance; the causal speed bound is one " +
                      "lattice step per oscillator cycle (c_TRM = 1). This document reports deterministic " +
                      "diagnostics across graph families D = 1..4 using validated parameter scans."
        };
    }

    private static PaperSection BuildMethods()
    {
        return new PaperSection
        {
            Title = "2. Methods",
            Content = "Graph families are generated deterministically via GraphFactory (chain, square grid, " +
                      "cubic lattice, hypercubic 4D). Kuramoto synchronization is simulated with Euler " +
                      "integration across a parameter grid: coupling K = [0.5, 1.0], frequency spread = [0.0, 0.1]. " +
                      "Diagnostics include final order parameter R, perturbation recovery, bridge-band proxy, " +
                      "and sync quality score. All computations are deterministic and reproducible."
        };
    }

    private static PaperSection BuildResults(
        List<DimensionAnalysisResult> analysis, List<AnalysisStatement> conclusions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Table 1: Per-dimension diagnostics.\n");
        sb.AppendLine("| D | Mean Q | σ_Q | Bridge | Recov | Synced | Marg | Unsynced |");
        sb.AppendLine("|---|--------|-----|--------|-------|--------|------|----------|");
        foreach (var a in analysis)
            sb.AppendLine($"| {a.Dimension} | {a.MeanQuality:F3} | {a.QualityStdDev:F3} | " +
                $"{a.MeanBridgeBand:E2} | {a.MeanRecovery:F3} | {a.SyncedCount} | {a.MarginalCount} | {a.UnsyncedCount} |");
        sb.AppendLine("\nStructured conclusions:\n");
        foreach (var c in conclusions)
            sb.AppendLine($"- [{c.Confidence}] {c.Text}");
        return new PaperSection { Title = "3. Results", Content = sb.ToString() };
    }

    private static PaperSection BuildLimitations()
    {
        return new PaperSection
        {
            Title = "4. Limitations",
            Content = "D = 3 is not derived from oscillator dynamics — it remains a HYPOTHESIS. " +
                      "Diagnostics are sensitive to coupling strength K and frequency spread. " +
                      "CML parameter scans cover a limited grid; exhaustive exploration is future work. " +
                      "The bridge-band proxy uses seeded natural frequency samples and is not a full spectral analysis. " +
                      "All conclusions are CONDITIONAL or SUPPORTED — none rise to DERIVED."
        };
    }

    private static PaperSection BuildConclusion()
    {
        return new PaperSection
        {
            Title = "5. Conclusion",
            Content = "TRM V4.1 provides a deterministic framework for evaluating dimensional diagnostics " +
                      "across graph families. The parameter scan infrastructure, analysis layer, and " +
                      "figure generation pipeline are fully reproducible. Future directions include " +
                      "exhaustive CML scans, dense eigensolver optimization, and synchronization-based " +
                      "dimensional selection criteria. No claim of dimensional selection is made at this stage."
        };
    }

    private static List<ClaimEvidenceMap> BuildClaimEvidence(
        List<DimensionAnalysisResult> analysis, List<AnalysisStatement> conclusions)
    {
        var maps = new List<ClaimEvidenceMap>();
        foreach (var c in conclusions)
        {
            string evidence = c.Metrics.Count > 0
                ? string.Join(", ", c.Metrics.Select(kv => $"{kv.Key}={kv.Value:F3}"))
                : "from structured analysis";
            maps.Add(new ClaimEvidenceMap
            {
                Claim = c.Text,
                Evidence = evidence,
                Classification = c.Confidence.ToString()
            });
        }
        return maps;
    }

    /// <summary>Render paper to Markdown string.</summary>
    public static string ToMarkdown(PaperDocument doc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {doc.Title}\n");
        foreach (var s in doc.Sections)
        {
            sb.AppendLine($"## {s.Title}");
            sb.AppendLine(s.Content);
            sb.AppendLine();
        }
        if (doc.ClaimEvidence.Count > 0)
        {
            sb.AppendLine("## Claim–Evidence Map");
            sb.AppendLine("| Claim | Evidence | Classification |");
            sb.AppendLine("|---|---|---|");
            foreach (var ce in doc.ClaimEvidence)
                sb.AppendLine($"| {ce.Claim} | {ce.Evidence} | {ce.Classification} |");
        }
        return sb.ToString();
    }
}
