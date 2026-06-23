using System;

namespace TRM.Core;

public record ClusterDiagnostic
(
    string Name,
    double Z,
    double Fz,
    double MaxPressureGradient,
    double Improvement,
    string Diagnosis,
    double Weight,
    double Turbulence,
    double Shear,
    double Anisotropy,
    double InertialNorm,
    double DynamicFactor,
    double Ellipticity,
    string MorphologyClass,
    string Regime
);
