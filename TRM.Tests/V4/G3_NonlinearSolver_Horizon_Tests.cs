using System;
using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V4;

/// <summary>
/// G3-NonlinearSolver: solves nonlinear scalar field equation
/// ∇²φ = β·(∇φ)² in vacuum, determines strong-field behavior.
/// </summary>
[Trait("Category", "V4")]
[Trait("Category", "G3")]
public class G3_NonlinearSolver_Horizon_Tests
{
    private readonly ITestOutputHelper _output;

    private const double G = 1.0;
    private const double M = 1.0;
    private const int N = 1000;
    private const double RMin = 0.1;
    private const double RMax = 50.0;

    public G3_NonlinearSolver_Horizon_Tests(ITestOutputHelper o) { _output = o; }

    // ════════════════════════════════════════════════════════════
    // G3NS_SimpleHorizon — ODE-based horizon prediction
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3NS_SimpleHorizon_ODE_Based()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3-SIMPLE-HORIZON — ODE PREDICTION");
        _output.WriteLine("  φ'' + (2/r)φ' = β·(φ')²");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");

        // β from kernel: f'(0)=−0.88, f''(0)=−0.48 for b=1.25
        // β ∝ f''(0)/f'(0) ≈ 0.55 → positive (self-energy deepens potential)
        double beta = 0.55;

        int n = 5000;
        double rStart = 100.0;
        double dr = (rStart - 0.01) / n;

        _output.WriteLine($"  β = {beta:F3} (from f''/f' for b=1.25 kernel)");
        _output.WriteLine("");
        _output.WriteLine($"  {"r (GM)",10} {"φ(r)",12} {"B_00(r)",12} {"Φ=GM/r",12} {"Δφ/φ",10}");
        _output.WriteLine($"  {new string('-',10)} {new string('-',12)} {new string('-',12)} {new string('-',12)} {new string('-',10)}");

        // Integrate inward from large r
        double[] rArr = new double[n + 1];
        double[] phiArr = new double[n + 1];
        double[] phiPrimeArr = new double[n + 1];

        // Initial conditions at large r (Newtonian)
        rArr[n] = rStart;
        phiArr[n] = G * M / rStart;
        phiPrimeArr[n] = -G * M / (rStart * rStart);

        double rH = 0;
        bool foundHorizon = false;

        for (int i = n - 1; i >= 0; i--)
        {
            rArr[i] = rArr[i + 1] - dr;
            double r = rArr[i];
            double p = phiArr[i + 1];
            double pp = phiPrimeArr[i + 1];

            // RK2 step inward for: φ'' = β·(φ')² − (2/r)·φ'
            double phiDD = beta * pp * pp - 2.0 * pp / r;

            // Midpoint
            double rMid = r + 0.5 * dr;
            double pMid = p - 0.5 * pp * dr;
            double ppMid = pp - 0.5 * phiDD * dr;
            double phiDDMid = beta * ppMid * ppMid - 2.0 * ppMid / rMid;

            phiArr[i] = p - ppMid * dr;
            phiPrimeArr[i] = pp - phiDDMid * dr;

            double b00 = -2.0 * phiArr[i];

            // Print at key radii
            bool keyRadius = Math.Abs(r - 10.0) < 0.5 || Math.Abs(r - 5.0) < 0.2 ||
                            Math.Abs(r - 3.0) < 0.1 || Math.Abs(r - 2.0) < 0.1 ||
                            Math.Abs(r - 1.5) < 0.05;

            if (keyRadius)
            {
                double phiNewt = G * M / r;
                double delta = (phiArr[i] - phiNewt) / phiNewt;
                _output.WriteLine($"  {r,10:F2} {phiArr[i],12:F6} {b00,12:F6} {phiNewt,12:F6} {delta,10:P1}");
            }

            // Detect horizon: B_00(r) → −1
            if (!foundHorizon && b00 <= -0.999)
            {
                rH = r;
                foundHorizon = true;
            }
        }

        _output.WriteLine("");
        if (foundHorizon)
        {
            _output.WriteLine($"  ╔══════════════════════════════════════╗");
            _output.WriteLine($"  ║  HORIZON FOUND                      ║");
            _output.WriteLine($"  ║  r_H = {rH:F3} GM                      ║");
            double rSch = 2.0 * G * M;
            string cls = Math.Abs(rH - rSch) < 0.01 ? "GR-LIKE" :
                         Math.Abs(rH - rSch) < 0.5 ? "SHIFTED" : "STRONGLY SHIFTED";
            _output.WriteLine($"  ║  r_schwarzschild = {rSch:F2} GM               ║");
            _output.WriteLine($"  ║  Classification: {cls,-20} ║");
            _output.WriteLine($"  ╚══════════════════════════════════════╝");
        }
        else
        {
            _output.WriteLine("  NO HORIZON — B_00(r) > −1 for all r ≥ 0.01 GM.");
            _output.WriteLine("  Classification: HORIZONLESS OBJECT");
        }

        _output.WriteLine("");
        _output.WriteLine("  β > 0 → self-energy deepens potential → horizon farther out.");
        _output.WriteLine("  β < 0 → self-energy shallows → horizon closer or absent.");
        _output.WriteLine($"  β = {beta:F2} from kernel → {((beta > 0) ? "deeper potential" : "shallower potential")}.");
    }

    // ════════════════════════════════════════════════════════════
    // G3NS_02 — Summary
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void G3NS_02_Summary()
    {
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("  G3 SIMPLE HORIZON — SUMMARY");
        _output.WriteLine("══════════════════════════════════════════════");
        _output.WriteLine("");
        _output.WriteLine($"  β = 0.55 (from f''/f' for b=1.25 kernel)");
        _output.WriteLine("  β > 0 → self-energy deepens potential");
        _output.WriteLine("  → horizon (if present) is farther out than Schwarzschild");
        _output.WriteLine("");
        _output.WriteLine("  SCALAR MODEL LIMITATIONS:");
        _output.WriteLine("    - Scalar φ, not full tensor B_μν");
        _output.WriteLine("    - β estimated from kernel derivatives at origin");
        _output.WriteLine("    - Full solution requires G_μν = 8πG·T_μν[K]");
        _output.WriteLine("");
        _output.WriteLine("  G3 STATUS:");
        _output.WriteLine("    Scalar ODE:  SOLVED (qualitative prediction)");
        _output.WriteLine("    Full GR-like: OPEN (multi-week project)");
    }
}
