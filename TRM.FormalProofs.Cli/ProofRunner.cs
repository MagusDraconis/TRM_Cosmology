using System;
using System.Collections.Generic;
using System.Numerics;

namespace TRM.FormalProofs.Cli;

/// <summary>
/// Orchestrates and executes the exact-rational formal proofs.
/// </summary>
public static class ProofRunner
{
    /// <summary>
    /// Runs FP01: Exact qCore Derivation Proof.
    /// </summary>
    public static bool RunQCoreProof(BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP01: Exact qCore Derivation Proof");
        
        var result = M3ExactQCoreProof.ProveQCore(qMax);
        
        ReportWriter.WriteDetails(result.Details);
        
        string summaryText = result.Passed
            ? $"qCore derived exactly as [16, 17, 18] within limit q <= {qMax}."
            : $"Failed to derive standard [16, 17, 18] qCore. Actual set has {result.DerivedQCore.Count} elements.";
            
        ReportWriter.WriteSummary($"qcore --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP01_qcore_proof_report.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP02: Phase Defect Minimization Proof.
    /// </summary>
    public static bool RunPhaseDefectProof(BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP02: Phase Defect Minimization Proof");

        // First we derive the qCore
        var qCoreResult = M3ExactQCoreProof.ProveQCore(qMax);
        if (qCoreResult.DerivedQCore.Count == 0)
        {
            Console.WriteLine("Error: Unable to run phase defect proof because derived qCore is empty.");
            return false;
        }

        var result = M3PhaseDefectProof.ProvePhaseDefectMinimization(qCoreResult.DerivedQCore, targetShift: 3);
        
        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"m=3 is verified to have exactly 0 closure defect and is the unique minimizer across qCore."
            : $"Phase defect proof failed. Counterexamples: {result.Counterexamples.Count}.";

        ReportWriter.WriteSummary($"phase --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP02_phase_defect_proof_report.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP03: Finite Domain Selection Uniqueness Proof.
    /// </summary>
    public static bool RunFiniteDomainProof(int mMax, BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP03: Finite Domain Selection Uniqueness Proof");

        var result = M3FiniteDomainEnergyMarginProof.RunSearch(mMax, qMax, targetShift: 3);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"exact finite-domain proof within q<={qMax} (m=3 uniquely has 0 phase defect, no competitors inside m=3 qCore)."
            : $"Finite domain proof failed. Counterexamples: {result.Counterexamples.Count}.";

        ReportWriter.WriteSummary($"finite-domain --mmax {mMax} --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP03_finite_domain_proof_report.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP04: Exact Shared Functional Computation Proof.
    /// </summary>
    public static bool RunEnergyProof(int mMax, BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP04: Exact Shared Functional Computation Proof");

        var result = M3ExactEnergyMarginProofs.RunFP04(mMax, qMax);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Exact rational shared functional components successfully computed and m=3 is strictly admissible."
            : $"Failed to compute functional components or m=3 is inadmissible.";

        ReportWriter.WriteSummary($"energy --mmax {mMax} --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP04_energy_proof_report.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP05: Exact Energy Margin Positivity Proof.
    /// </summary>
    public static bool RunMarginProof(int mMax, BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP05: Exact Energy Margin Positivity Proof");

        var result = M3ExactEnergyMarginProofs.RunFP05(mMax, qMax);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"m=3 strictly outcompetes all admissible competitors with exact ΔE > 0."
            : $"Failed. Competitors matched or outcompeted m=3.";

        ReportWriter.WriteSummary($"margin --mmax {mMax} --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP05_margin_proof_report.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP06: Domain Boundary Abstention Proof.
    /// </summary>
    public static bool RunBoundaryProof(int mMax, BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP06: Domain Boundary Abstention Proof");

        var result = M3ExactEnergyMarginProofs.RunFP06(mMax, qMax);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Rule correctly abstains on exact boundary failures (no false m=3 selection)."
            : $"Failed to abstain gracefully under boundary violations.";

        ReportWriter.WriteSummary($"boundary --mmax {mMax} --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP06_boundary_proof_report.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP07: Exact Functional Symbolic Inequalities Export.
    /// </summary>
    public static bool RunExportInequalities(int mMax, BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP07: Exact Functional Symbolic Inequalities Export");

        var result = M3SymbolicProofObligations.RunFP07(mMax, qMax);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Symbolic inequalities successfully exported and strictly verified for finite domain."
            : $"Failed to verify symbolic inequalities over domain.";

        ReportWriter.WriteSummary($"export-inequalities --mmax {mMax} --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP07_inequalities_export.txt", result.Details);
            ReportWriter.SaveReportToFile("FP07_inequalities_export.json", result.JsonOutput);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP08: Proof Obligations Decomposed By Lemma.
    /// </summary>
    public static bool RunProofObligations(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP08: Proof Obligations Decomposed By Lemma");

        var result = M3SymbolicProofObligations.RunFP08();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Lemma decomposition successfully generated with EXACT-FINITE-PASS markings."
            : $"Failed to generate lemma decomposition.";

        ReportWriter.WriteSummary($"proof-obligations", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP08_proof_obligations.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP09: Counterexample Search and Minimal Witnesses Export.
    /// </summary>
    public static bool RunWitnesses(int mMax, BigInteger qMax, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP09: Minimal Witnesses Export");

        var result = M3SymbolicProofObligations.RunFP09(mMax, qMax);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"No counterexamples found. Minimal competitor margins strictly positive."
            : $"Counterexamples found in domain. Exact witnesses exported.";

        ReportWriter.WriteSummary($"witnesses --mmax {mMax} --qmax {qMax}", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP09_witnesses.txt", result.Details);
            ReportWriter.SaveReportToFile("FP09_witnesses.json", result.JsonOutput);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP10-FP11: Proof Assistant Definitions and Lemmas Export.
    /// </summary>
    public static bool RunExportProofAssistant(string target, bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP10-11: Proof Assistant Definitions and Lemmas Export");

        var result = M3ProofAssistantExport.RunFP10_11(target);

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Proof assistant definitions and theorem stubs exported successfully for target '{target}'."
            : $"Failed to generate proof assistant export.";

        ReportWriter.WriteSummary($"export-proof-assistant --target {target}", result.Passed, summaryText);

        if (saveToFile && result.Passed && target.ToLowerInvariant() == "lean")
        {
            ReportWriter.SaveReportToFile("FP10_11_TRM_M3_Scaffold.lean", result.LeanCode);
            ReportWriter.SaveReportToFile("FP10_11_proof_assistant_export_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP12: Verify Proof Assistant Export Matches Exact Witnesses.
    /// </summary>
    public static bool RunVerifyProofExport(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP12: Verify Proof Assistant Export Matches Exact Witnesses");

        var result = M3ProofAssistantExport.RunFP12();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Exported constants strictly match exact CLI computational witnesses."
            : $"Mismatches detected between exported constants and exact results.";

        ReportWriter.WriteSummary($"verify-proof-export", result.Passed, summaryText);

        if (saveToFile)
        {
            ReportWriter.SaveReportToFile("FP12_verify_proof_export_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP13: Lean Export Typecheck.
    /// </summary>
    public static bool RunLeanCheck(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP13: Lean Export Typecheck");

        var result = M3LeanValidationProofs.RunFP13();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Lean syntax validated and generated."
            : $"Failed to validate Lean syntax.";

        ReportWriter.WriteSummary($"lean-check", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP13_lean_check.lean", result.LeanCode);
            ReportWriter.SaveReportToFile("FP13_lean_check_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP14: Lean Constants Proven with Simp/Norm_num.
    /// </summary>
    public static bool RunLeanConstants(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP14: Lean Constants Proven");

        var result = M3LeanValidationProofs.RunFP14();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Simple finite constants successfully proven using rfl and norm_num."
            : $"Failed to generate constant proofs.";

        ReportWriter.WriteSummary($"lean-constants", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP14_lean_constants.lean", result.LeanCode);
            ReportWriter.SaveReportToFile("FP14_lean_constants_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP15: Lean Sorry Inventory.
    /// </summary>
    public static bool RunSorryInventory(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP15: Lean Sorry Inventory");

        var result = M3LeanValidationProofs.RunFP15();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Inventory of {result.SorryMappings.Count} remaining 'sorry' placeholders mapped to Lemmas."
            : $"Failed to map sorry inventory.";

        ReportWriter.WriteSummary($"sorry-inventory", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP15_sorry_inventory_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP16-FP17: Lean Phase Proofs over finite qCore.
    /// </summary>
    public static bool RunLeanPhaseProofs(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP16-FP17: Lean Phase Proofs");

        var result = M3LeanPhaseProofs.RunFP16_17();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Phase defects formally proven over finite qCore using norm_num."
            : $"Failed to generate phase proofs.";

        ReportWriter.WriteSummary($"lean-phase-proofs", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP16_17_lean_phase_proofs.lean", result.LeanCode);
            ReportWriter.SaveReportToFile("FP16_17_lean_phase_proofs_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP18: Lean Sorry Inventory (Updated).
    /// </summary>
    public static bool RunUpdatedSorryInventory(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP18: Lean Sorry Inventory Updated");

        var result = M3LeanPhaseProofs.RunFP18();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Inventory of {result.SorryMappings.Count} remaining 'sorry' placeholders."
            : $"Failed to map sorry inventory.";

        ReportWriter.WriteSummary($"sorry-inventory-updated", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP18_sorry_inventory_updated_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP19-FP20: Lean Domain Abstention boundary cases.
    /// </summary>
    public static bool RunLeanDomainAbstention(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP19-FP20: Lean Domain Abstention");

        var result = M3LeanDomainAbstentionProofs.RunFP19_20();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Domain abstention finite boundary cases formally proven in Lean."
            : $"Failed to generate domain abstention proofs.";

        ReportWriter.WriteSummary($"lean-domain-abstention", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP19_20_lean_domain_abstention.lean", result.LeanCode);
            ReportWriter.SaveReportToFile("FP19_20_lean_domain_abstention_log.txt", result.Details);
        }

        return result.Passed;
    }

    /// <summary>
    /// Runs FP21: Lean Final Sorry Inventory.
    /// </summary>
    public static bool RunFinalSorryInventory(bool saveToFile = true)
    {
        ReportWriter.WriteHeader("FP21: Final Lean Sorry Inventory");

        var result = M3LeanDomainAbstentionProofs.RunFP21();

        ReportWriter.WriteDetails(result.Details);

        string summaryText = result.Passed
            ? $"Final inventory explicitly bounds remaining gap to {result.SorryMappings.Count} continuous limit derivation(s)."
            : $"Failed to map final sorry inventory.";

        ReportWriter.WriteSummary($"sorry-inventory-final", result.Passed, summaryText);

        if (saveToFile && result.Passed)
        {
            ReportWriter.SaveReportToFile("FP21_sorry_inventory_final_log.txt", result.Details);
        }

        return result.Passed;
    }
}
