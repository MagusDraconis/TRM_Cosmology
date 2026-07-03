using System;
using System.Numerics;

namespace TRM.FormalProofs.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            RunInteractiveMenu();
            return 0;
        }

        string command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "qcore":
                return HandleQCore(args);
            case "phase":
                return HandlePhase(args);
            case "finite-domain":
                return HandleFiniteDomain(args);
            case "energy":
                return HandleEnergy(args);
            case "margin":
                return HandleMargin(args);
            case "boundary":
                return HandleBoundary(args);
            case "export-inequalities":
                return HandleExportInequalities(args);
            case "proof-obligations":
                return HandleProofObligations(args);
            case "witnesses":
                return HandleWitnesses(args);
            case "export-proof-assistant":
                return HandleExportProofAssistant(args);
            case "verify-proof-export":
                return HandleVerifyProofExport(args);
            case "lean-check":
                return HandleLeanCheck(args);
            case "lean-constants":
                return HandleLeanConstants(args);
            case "sorry-inventory":
                return HandleSorryInventory(args);
            case "lean-phase-proofs":
                return HandleLeanPhaseProofs(args);
            case "sorry-inventory-updated":
                return HandleSorryInventoryUpdated(args);
            case "lean-domain-abstention":
                return HandleLeanDomainAbstention(args);
            case "sorry-inventory-final":
                return HandleSorryInventoryFinal(args);
            case "decompose-continuous-domain":
                return HandleDecomposeContinuousDomain(args);
            case "exact-continuous-bounds":
                return HandleExactContinuousBounds(args);
            case "proof-obligation-map":
                return HandleProofObligationMap(args);
            case "fp25-phase-limit":
                return HandleFP25PhaseLimit(args);
            case "fp26-action-limit":
                return HandleFP26ActionLimit(args);
            case "fp27-phase-induction":
                return HandleFP27PhaseInduction(args);
            case "fp28-zero-iff":
                return HandleFP28ZeroIff(args);
            case "fp29-qcore-support":
                return HandleFP29QCoreSupport(args);
            case "fp30-phase-convergence":
                return HandleFP30PhaseConvergence(args);
            case "fp31-ceil-inequality":
                return HandleFP31CeilInequality(args);
            case "--help":
            case "-h":
            case "help":
                PrintHelp();
                return 0;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Unknown command '{args[0]}'.");
                Console.ResetColor();
                PrintHelp();
                return 1;
        }
    }

    private static void RunInteractiveMenu()
    {
        var isFirstIteration = true;
        while (true)
        {
            if (!isFirstIteration)
            {
                Console.WriteLine("\nPress any key to return to the menu...");
                Console.ReadKey(true);
                Console.Clear();
            }
            isFirstIteration = false;

            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("        TRM EXACT-RATIONAL FORMAL PROOF SCAFFOLDING MENU");
            Console.WriteLine("================================================================================");
            Console.WriteLine(" [1] Run FP01: Exact qCore Derivation Proof      (q <= 10,000)");
            Console.WriteLine(" [2] Run FP02: Phase Defect Minimization Proof  (q <= 10,000)");
            Console.WriteLine(" [3] Run FP03: Finite Domain Uniqueness Search  (m <= 5, q <= 10,000)");
            Console.WriteLine(" [4] Run FP04: Exact Shared Functional Proof    (m <= 5, q <= 10,000)");
            Console.WriteLine(" [5] Run FP05: Exact Energy Margin Proof        (m <= 5, q <= 10,000)");
            Console.WriteLine(" [6] Run FP06: Domain Boundary Abstention Proof (m <= 5, q <= 10,000)");
            Console.WriteLine(" [7] Run FP07: Export Symbolic Inequalities     (m <= 5, q <= 10,000)");
            Console.WriteLine(" [8] Run FP08: Proof Obligations by Lemma");
            Console.WriteLine(" [9] Run FP09: Export Minimal Witnesses         (m <= 5, q <= 10,000)");
            Console.WriteLine(" [10] Run FP10-11: Export Proof Assistant Stubs");
            Console.WriteLine(" [11] Run FP12: Verify Proof Assistant Export Match");
            Console.WriteLine(" [12] Run FP13: Lean Export Typecheck");
            Console.WriteLine(" [13] Run FP14: Prove Lean Constants (rfl, norm_num)");
            Console.WriteLine(" [14] Run FP15: Lean Sorry Inventory");
            Console.WriteLine(" [15] Run FP16-17: Lean Phase Proofs over qCore (norm_num)");
            Console.WriteLine(" [16] Run FP18: Lean Sorry Inventory Updated");
            Console.WriteLine(" [17] Run FP19-20: Lean Domain Abstention Proofs");
            Console.WriteLine(" [18] Run FP21: Lean Final Sorry Inventory");
            Console.WriteLine(" [19] View CLI Help and Manual");
            Console.WriteLine(" [0] Exit Proof Framework");
            Console.WriteLine("================================================================================");
            Console.Write(" Select an option: ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    Console.Clear();
                    ProofRunner.RunQCoreProof(10000);
                    break;
                case "2":
                    Console.Clear();
                    ProofRunner.RunPhaseDefectProof(10000);
                    break;
                case "3":
                    Console.Clear();
                    ProofRunner.RunFiniteDomainProof(5, 10000);
                    break;
                case "4":
                    Console.Clear();
                    ProofRunner.RunEnergyProof(5, 10000);
                    break;
                case "5":
                    Console.Clear();
                    ProofRunner.RunMarginProof(5, 10000);
                    break;
                case "6":
                    Console.Clear();
                    ProofRunner.RunBoundaryProof(5, 10000);
                    break;
                case "7":
                    Console.Clear();
                    ProofRunner.RunExportInequalities(5, 10000);
                    break;
                case "8":
                    Console.Clear();
                    ProofRunner.RunProofObligations();
                    break;
                case "9":
                    Console.Clear();
                    ProofRunner.RunWitnesses(5, 10000);
                    break;
                case "10":
                    Console.Clear();
                    ProofRunner.RunExportProofAssistant("lean");
                    break;
                case "11":
                    Console.Clear();
                    ProofRunner.RunVerifyProofExport();
                    break;
                case "12":
                    Console.Clear();
                    ProofRunner.RunLeanCheck();
                    break;
                case "13":
                    Console.Clear();
                    ProofRunner.RunLeanConstants();
                    break;
                case "14":
                    Console.Clear();
                    ProofRunner.RunSorryInventory();
                    break;
                case "15":
                    Console.Clear();
                    ProofRunner.RunLeanPhaseProofs();
                    break;
                case "16":
                    Console.Clear();
                    ProofRunner.RunUpdatedSorryInventory();
                    break;
                case "17":
                    Console.Clear();
                    ProofRunner.RunLeanDomainAbstention();
                    break;
                case "18":
                    Console.Clear();
                    ProofRunner.RunFinalSorryInventory();
                    break;
                case "19":
                    Console.Clear();
                    PrintHelp();
                    break;
                case "0":
                    Console.WriteLine("\nExiting Proof Framework. Goodbye!");
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid selection. Please enter a valid number.");
                    Console.ResetColor();
                    break;
            }
        }
    }

    private static int HandleQCore(string[] args)
    {
        BigInteger qMax = 10000; // default

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsed))
                {
                    qMax = parsed;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid qmax value '{args[i+1]}', using default: {qMax}");
                }
                i++;
            }
        }

        bool success = ProofRunner.RunQCoreProof(qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandlePhase(string[] args)
    {
        BigInteger qMax = 10000; // default

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsed))
                {
                    qMax = parsed;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid qmax value '{args[i+1]}', using default: {qMax}");
                }
                i++;
            }
        }

        bool success = ProofRunner.RunPhaseDefectProof(qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFiniteDomain(string[] args)
    {
        int mMax = 5;
        BigInteger qMax = 10000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--mmax" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedM))
                {
                    mMax = parsedM;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid mmax value '{args[i+1]}', using default: {mMax}");
                }
                i++;
            }
            else if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsedQ))
                {
                    qMax = parsedQ;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid qmax value '{args[i+1]}', using default: {qMax}");
                }
                i++;
            }
        }

        bool success = ProofRunner.RunFiniteDomainProof(mMax, qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleEnergy(string[] args)
    {
        int mMax = 5;
        BigInteger qMax = 10000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--mmax" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedM))
                {
                    mMax = parsedM;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid mmax value '{args[i+1]}', using default: {mMax}");
                }
                i++;
            }
            else if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsedQ))
                {
                    qMax = parsedQ;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid qmax value '{args[i+1]}', using default: {qMax}");
                }
                i++;
            }
        }

        bool success = ProofRunner.RunEnergyProof(mMax, qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleMargin(string[] args)
    {
        int mMax = 5;
        BigInteger qMax = 10000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--mmax" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedM))
                {
                    mMax = parsedM;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid mmax value '{args[i+1]}', using default: {mMax}");
                }
                i++;
            }
            else if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsedQ))
                {
                    qMax = parsedQ;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid qmax value '{args[i+1]}', using default: {qMax}");
                }
                i++;
            }
        }

        bool success = ProofRunner.RunMarginProof(mMax, qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleBoundary(string[] args)
    {
        int mMax = 5;
        BigInteger qMax = 10000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--mmax" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedM))
                {
                    mMax = parsedM;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid mmax value '{args[i+1]}', using default: {mMax}");
                }
                i++;
            }
            else if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsedQ))
                {
                    qMax = parsedQ;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid qmax value '{args[i+1]}', using default: {qMax}");
                }
                i++;
            }
        }

        bool success = ProofRunner.RunBoundaryProof(mMax, qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleExportInequalities(string[] args)
    {
        int mMax = 5;
        BigInteger qMax = 10000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--mmax" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedM)) mMax = parsedM;
                i++;
            }
            else if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsedQ)) qMax = parsedQ;
                i++;
            }
        }

        bool success = ProofRunner.RunExportInequalities(mMax, qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleProofObligations(string[] args)
    {
        bool success = ProofRunner.RunProofObligations(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleWitnesses(string[] args)
    {
        int mMax = 5;
        BigInteger qMax = 10000;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--mmax" && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsedM)) mMax = parsedM;
                i++;
            }
            else if (args[i] == "--qmax" && i + 1 < args.Length)
            {
                if (BigInteger.TryParse(args[i + 1], out var parsedQ)) qMax = parsedQ;
                i++;
            }
        }

        bool success = ProofRunner.RunWitnesses(mMax, qMax, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleExportProofAssistant(string[] args)
    {
        string target = "lean";

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--target" && i + 1 < args.Length)
            {
                target = args[i + 1].ToLowerInvariant();
                i++;
            }
        }

        bool success = ProofRunner.RunExportProofAssistant(target, saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleVerifyProofExport(string[] args)
    {
        bool success = ProofRunner.RunVerifyProofExport(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleLeanCheck(string[] args)
    {
        bool success = ProofRunner.RunLeanCheck(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleLeanConstants(string[] args)
    {
        bool success = ProofRunner.RunLeanConstants(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleSorryInventory(string[] args)
    {
        bool success = ProofRunner.RunSorryInventory(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleLeanPhaseProofs(string[] args)
    {
        bool success = ProofRunner.RunLeanPhaseProofs(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleSorryInventoryUpdated(string[] args)
    {
        bool success = ProofRunner.RunUpdatedSorryInventory(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleLeanDomainAbstention(string[] args)
    {
        bool success = ProofRunner.RunLeanDomainAbstention(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleSorryInventoryFinal(string[] args)
    {
        bool success = ProofRunner.RunFinalSorryInventory(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleDecomposeContinuousDomain(string[] args)
    {
        bool success = ProofRunner.RunDecomposeContinuousDomain(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleExactContinuousBounds(string[] args)
    {
        bool success = ProofRunner.RunExactContinuousBounds(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleProofObligationMap(string[] args)
    {
        bool success = ProofRunner.RunProofObligationMap(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP25PhaseLimit(string[] args)
    {
        bool success = ProofRunner.RunFP25_PhaseLimit(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP26ActionLimit(string[] args)
    {
        bool success = ProofRunner.RunFP26_ActionLimit(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP27PhaseInduction(string[] args)
    {
        bool success = ProofRunner.RunFP27_PhaseInduction(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP28ZeroIff(string[] args)
    {
        bool success = ProofRunner.RunFP28_ZeroIff(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP29QCoreSupport(string[] args)
    {
        bool success = ProofRunner.RunFP29_QCoreSupport(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP30PhaseConvergence(string[] args)
    {
        bool success = ProofRunner.RunFP30_PhaseConvergence(saveToFile: true);
        return success ? 0 : 1;
    }

    private static int HandleFP31CeilInequality(string[] args)
    {
        bool success = ProofRunner.RunFP31_CeilInequality(saveToFile: true);
        return success ? 0 : 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine();
        ReportWriter.WriteLineSeparator();
        Console.WriteLine("  TRM EXACT-RATIONAL FORMAL PROOF SCAFFOLDING CLI  ");
        ReportWriter.WriteLineSeparator();
        Console.WriteLine("This tool runs exact rational proofs to mathematically verify the uniqueness");
        Console.WriteLine("of m=3 selection rules within the TQM/TRM framework.");
        Console.WriteLine();
        Console.WriteLine("AVAILABLE COMMANDS (non-interactive mode):");
        Console.WriteLine();
        Console.WriteLine("  qcore [--qmax <value>]");
        Console.WriteLine("    Executes FP01: Derives the qCore interval exactly using rational arithmetic.");
        Console.WriteLine("    Default --qmax: 10000");
        Console.WriteLine();
        Console.WriteLine("  phase [--qmax <value>]");
        Console.WriteLine("    Executes FP02: Verifies closure defect for m=3 is exactly zero and minimized.");
        Console.WriteLine("    Default --qmax: 10000");
        Console.WriteLine();
        Console.WriteLine("  finite-domain [--mmax <value>] [--qmax <value>]");
        Console.WriteLine("    Executes FP03: Scans finite domain and reports exact selection uniqueness.");
        Console.WriteLine("    Default --mmax: 5, --qmax: 10000");
        Console.WriteLine();
        Console.WriteLine("  energy [--mmax <value>] [--qmax <value>]");
        Console.WriteLine("    Executes FP04: Computes exact shared functional (phase + bridge + action).");
        Console.WriteLine("    Default --mmax: 5, --qmax: 10000");
        Console.WriteLine();
        Console.WriteLine("  margin [--mmax <value>] [--qmax <value>]");
        Console.WriteLine("    Executes FP05: Proves strict positive energy margin (ΔE > 0) against competitors.");
        Console.WriteLine("    Default --mmax: 5, --qmax: 10000");
        Console.WriteLine();
        Console.WriteLine("  boundary [--mmax <value>] [--qmax <value>]");
        Console.WriteLine("    Executes FP06: Verifies graceful abstention under exact boundary failures.");
        Console.WriteLine("    Default --mmax: 5, --qmax: 10000");
        Console.WriteLine();
        Console.WriteLine("  sorry-inventory-final");
        Console.WriteLine("    Executes FP21: Final Lean sorry inventory after domain abstention proofs.");
        Console.WriteLine();
        Console.WriteLine("  decompose-continuous-domain");
        Console.WriteLine("    Executes FP22: Decomposes continuous-domain lemma into epsilon-bound stubs.");
        Console.WriteLine();
        Console.WriteLine("  exact-continuous-bounds");
        Console.WriteLine("    Executes FP23: Exports exact rational epsilon-bound definitions.");
        Console.WriteLine();
        Console.WriteLine("  proof-obligation-map");
        Console.WriteLine("    Executes FP24: Maps remaining proof obligations to first-principles assumptions.");
        Console.WriteLine();
        Console.WriteLine("  fp25-phase-limit");
        Console.WriteLine("    Executes FP25: Lean proof attempt for epsilon_phase_asymptotic_bound.");
        Console.WriteLine();
        Console.WriteLine("  fp26-action-limit");
        Console.WriteLine("    Executes FP26: Model-requirement report for epsilon_action_asymptotic_bound.");
        Console.WriteLine();
        Console.WriteLine("  fp27-phase-induction");
        Console.WriteLine("    Executes FP27: Phase defect positivity proof over unbounded ℤ (trichotomy).");
        Console.WriteLine();
        Console.WriteLine("  fp28-zero-iff");
        Console.WriteLine("    Executes FP28: Both directions of epsilon_phase_zero_iff_m3 (FP27 contrapositive).");
        Console.WriteLine();
        Console.WriteLine("  fp29-qcore-support");
        Console.WriteLine("    Executes FP29: Defines qCoreSupport(q) = 1 - 3/q as exact Rational function.");
        Console.WriteLine();
        Console.WriteLine("  fp30-phase-convergence");
        Console.WriteLine("    Executes FP30: Convergence proof for epsilon_phase_asymptotic_bound.");
        Console.WriteLine();
        Console.WriteLine("  fp31-ceil-inequality");
        Console.WriteLine("    Executes FP31: Closes the final ceil inequality gap (all PENDING-PROOF → DEFINED).");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  dotnet run --project TRM.FormalProofs.Cli -- energy --mmax 5 --qmax 10000");
        Console.WriteLine("  dotnet run --project TRM.FormalProofs.Cli -- margin --mmax 5 --qmax 10000");
        Console.WriteLine("  dotnet run --project TRM.FormalProofs.Cli -- boundary --mmax 5 --qmax 10000");
        Console.WriteLine();
        Console.WriteLine("RUN WITHOUT ARGUMENTS TO START INTERACTIVE MENU MODE:");
        Console.WriteLine("  dotnet run --project TRM.FormalProofs.Cli");
        ReportWriter.WriteLineSeparator();
        Console.WriteLine();
    }
}
