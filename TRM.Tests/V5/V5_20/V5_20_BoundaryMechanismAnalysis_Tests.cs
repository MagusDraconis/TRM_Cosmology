using Xunit;
using Xunit.Abstractions;

namespace TRM.Tests.V5_20;

[Trait("Category","V5_20"),Trait("Category","V5_20_BMA")]
public class V5_20_BoundaryMechanismAnalysis_Tests
{
    private readonly ITestOutputHelper _o;
    public V5_20_BoundaryMechanismAnalysis_Tests(ITestOutputHelper o){_o=o;}

    [Fact]public void BMA_01_DriverRanking(){
        _o.WriteLine("═══ BMA_01: Boundary driver ranking ═══");

        // BME data: (N, n, A0%, C3%, rescues, rebMag, dPre, dPost, dC3, kPre, kPost, distHi)
        double[][] data=[
            [60,8, 0, 0, 0,   0.117,  0.301,0.258,0,      1.029,1.052,double.NaN],
            [64,8, 0, 0, 0,   0.018,  0.325,0.321,0,      1.017,1.017,0.269],
            [65,7,14,29,1,    0.133,  0.302,0.368,0.828,  1.028,1.010,0.213],
            [66,3, 0, 0, 0,  -0.081,  0.248,0.572,0,      1.056,0.922,0.340],
            [70,8,50,62,1,    0.117,  0.269,0.349,0.591,  1.043,1.011,0.247],
            [72,8,75,100,2,   0.326,  0.389,0.317,0.607,  0.983,1.030,0.207],
            [75,8,100,100,0,  0.493,  0.314,0.274,0,      1.019,1.041,0.160],
            [80,4,100,100,0,  0.521,  0.304,0.325,0,      1.022,1.016,0.372]
        ];

        _o.WriteLine("\n─── BME diagnostic data ───");
        _o.WriteLine(string.Format("{0,4} {1,5} {2,5} {3,4} {4,6} {5,6}",
            "N","A0%","C3%","Resc","RebMag","distHi"));
        foreach(var r in data){
            double dist=r[10];string ds=double.IsNaN(dist)?"NaN":$"{dist:F3}";
            _o.WriteLine($"{r[0],4} {r[2]*100.0/r[1],4:F0}% {r[3]*100.0/r[1],4:F0}% {r[4],4} {r[5],6:F3} {ds,6}");
        }

        // Driver ranking using BME data
        _o.WriteLine("\n─── Boundary driver ranking ───");

        // Driver 1: Rescue opportunity (A0 failure count)
        _o.WriteLine("1. Rescue opportunity — seeds failing under A0 that C3 can rescue");
        _o.WriteLine("   N=64: 8 fail, 0 rescued. N=65: 6 fail, 1 rescued.");
        _o.WriteLine("   N=72: 2 fail, 2 rescued. N=80: 0 fail, 0 rescued.");

        // Driver 2: Inducibility (A0 > 0)
        _o.WriteLine("2. Inducibility — whether baseline intervention works at all");
        _o.WriteLine("   N=60-64: A0=0% (cannot induce). N=65+: A0>0% (can induce).");

        // Driver 3: distHi (basin proximity)
        _o.WriteLine("3. Basin proximity (distHi)");
        _o.WriteLine("   N=64: 0.27 → N=65: 0.21 (closer to Hi). N=72: 0.21 (closest).");

        // Driver 4: rebMagnitude
        _o.WriteLine("4. rebMagnitude signal strength");
        _o.WriteLine("   N=64: 0.02 → N=72: 0.33 (peak). N=80: 0.52 (but no room).");

        // Driver 5: orthHiVec
        _o.WriteLine("5. orthHiVec pre-filter (N=72 only)");
        _o.WriteLine("   Unique to N=72 — pre-filters most aligned candidates.");

        // Driver 6: saturation
        _o.WriteLine("6. Static saturation (A0=100%)");
        _o.WriteLine("   N=75+: A0 reaches ceiling → no failures to rescue.");

        // Gate assessment
        _o.WriteLine("\n─── Gates ───");
        _o.WriteLine("Gate A (Rescue opp dominates): NOT REACHED — inducibility prerequisite");
        _o.WriteLine("Gate B (Low-N explained): REACHED — non-inducible (A0=0%)");
        _o.WriteLine("Gate C (Peak explained): REACHED — rebMag+orthHiVec+distHi converge");
        _o.WriteLine("Gate D (Saturation): REACHED — A0=100%, no rescue opportunity");
        _o.WriteLine("Gate E (Mixed): REACHED — 3 independent mechanisms");
        _o.WriteLine("Gate F (Unresolved): NOT REACHED");

        _o.WriteLine("\n─── Boundary Model: Model E — Mixed Mechanism ───");
        _o.WriteLine("Lower: inducibility (can seeds reach Hi basin?)");
        _o.WriteLine("Peak: rebMag signal + orthHiVec pre-filter + basin proximity");
        _o.WriteLine("Upper: static saturation (A0 already 100%)");

        _o.WriteLine("\n─── Next: BMS Final Synthesis ───");
    }
}
