using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core.Shared;

public class TrmDatasetTransformer
{
    private readonly TrmDistanceMapper _mapper;

    public TrmDatasetTransformer(TrmDistanceMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Transform an ACCEPT-like radial profile from GR to TRM geometry.
    /// </summary>
    public List<ClusterProfilePointTrm> TransformClusterProfile(
        List<ClusterProfilePoint> data,
        double redshift)
    {
        if(data == null || data.Count == 0)
            return new List<ClusterProfilePointTrm>();

        var mapping = _mapper.MapFromRedshift(redshift);
        double f = mapping.ConversionFactor ?? throw new InvalidOperationException("Conversion factor missing.");

        var result = new List<ClusterProfilePointTrm>();

        foreach(var point in data)
        {
            // ✅ Radius scaling
            double rTrm = point.RadiusMpc_GR * f;

            // ✅ Density scaling (Volume effect)
            double densityTrm = point.Density_GR / Math.Pow(f, 3);

            // Temperatur bleibt unverändert (lokale physikalische Größe)
            result.Add(new ClusterProfilePointTrm(
                rTrm,
                densityTrm,
                point.Temperature_keV
            ));
        }

        return result;
    }


    //TransformLuminosity(...)
    //TransformMassProfile(...)
    //TransformSurfaceBrightness(...)

}