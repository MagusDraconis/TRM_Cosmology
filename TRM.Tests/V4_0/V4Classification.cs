namespace TRM.Tests.V4;

/// <summary>
/// V4 gate classification for B1 coupling-modulation mechanism tests.
///
/// Thresholds (from TRM_V4_MappingTests.md):
///   VALID:   |α + 1| &lt; 0.10  AND  |β + 2| &lt; 0.10
///   PARTIAL: |α + 1| &lt; 0.30  OR   |β + 2| &lt; 0.30  (but not VALID)
///   INVALID: otherwise
/// </summary>
public enum V4Classification
{
    /// <summary>Exact Newtonian asymptotics — mechanism confirmed.</summary>
    Valid,

    /// <summary>Approximate scaling — mechanism plausible but not exact.</summary>
    Partial,

    /// <summary>Wrong asymptotic scaling — mechanism falsified.</summary>
    Invalid
}
