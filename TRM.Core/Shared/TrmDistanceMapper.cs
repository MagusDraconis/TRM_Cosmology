using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core.Shared;


public class TrmDistanceMapper
{
    private readonly TrmCosmologyParameters _scaling;

    public TrmDistanceMapper(TrmCosmologyParameters scaling)
    {
        _scaling = scaling ?? throw new ArgumentNullException(nameof(scaling));
    }

    /// <summary>
    /// TRM core distance from redshift.
    /// This is the same structural distance used in the current CMB scale consistency path:
    /// D = (c / H_T) * ln(1 + z)
    /// </summary>
    public double CalculateTrmBaseDistance(double z)
    {
        if (z < 0.0)
            throw new ArgumentOutOfRangeException(nameof(z), "Redshift z must be non-negative.");

        return (PhysicalConstants.C_Kms / _scaling.HT) * Math.Log(1.0 + z);
    }

    /// <summary>
    /// Current TRM angular-diameter-like distance.
    /// For now this equals the TRM base distance.
    /// If later TRM requires a projection correction, it belongs here.
    /// </summary>

    public double CalculateTrmAngularDiameterDistance(double z)
    {
        return CalculateTrmBaseDistance(z) / (1.0 + z);
    }


    /// <summary>
    /// TRM luminosity-distance-like quantity.
    /// Conservative first version: base distance times one photon/redshift factor.
    /// This should be treated as model version 1, not final law.
    /// </summary>
    public double CalculateTrmLuminosityDistance(double z)
    {
        double dBase = CalculateTrmBaseDistance(z);

        return dBase * (1.0 + z);
    }

    /// <summary>
    /// Maps redshift to TRM distances.
    /// If a GR distance is provided, also returns the z-dependent conversion factor.
    /// </summary>
    public DistanceMappingResult MapFromRedshift(
        double z,
        double? grDistance = null,
        DistanceMeasureKind grDistanceKind = DistanceMeasureKind.Luminosity)
    {
        double dBase = CalculateTrmBaseDistance(z);
        double dAngular = CalculateTrmAngularDiameterDistance(z);
        double dLuminosity = CalculateTrmLuminosityDistance(z);

        double trmComparable = grDistanceKind switch
        {
            DistanceMeasureKind.ComovingLike => dBase,
            DistanceMeasureKind.AngularDiameter => dAngular,
            DistanceMeasureKind.Luminosity => dLuminosity,
            _ => dLuminosity
        };

        double? factor = null;

        if (grDistance.HasValue)
        {
            if (grDistance.Value <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(grDistance), "GR distance must be positive.");

            factor = trmComparable / grDistance.Value;
        }

        return new DistanceMappingResult(
            z,
            dBase,
            dAngular,
            dLuminosity,
            grDistance,
            factor);
    }

    /// <summary>
    /// Converts an externally supplied GR distance into the corresponding TRM distance
    /// by using a redshift-dependent conversion factor.
    /// </summary>
    public double ConvertGrDistanceToTrm(
        double z,
        double grDistance,
        DistanceMeasureKind distanceKind)
    {
        var mapped = MapFromRedshift(z, grDistance, distanceKind);

        if (!mapped.ConversionFactor.HasValue)
            throw new InvalidOperationException("Conversion factor was not computed.");

        return grDistance * mapped.ConversionFactor.Value;
    }

    /// <summary>
    /// Converts TRM luminosity distance to distance modulus.
    /// dL must be in Mpc.
    /// </summary>
    public double CalculateDistanceModulusFromLuminosityDistance(double dL_Mpc)
    {
        if (dL_Mpc <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(dL_Mpc), "Luminosity distance must be positive.");

        return 5.0 * Math.Log10(dL_Mpc) + 25.0;
    }

    public double CalculateTrmDistanceModulus(double z)
    {
        double dL = CalculateTrmLuminosityDistance(z);
        return CalculateDistanceModulusFromLuminosityDistance(dL);
    }
}
