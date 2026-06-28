
using System.Globalization;

namespace TRM.Core.Shared;

public class AcceptClusterTransformer
{
    private readonly TrmDistanceMapper _mapper;

    public AcceptClusterTransformer(TrmDistanceMapper mapper)
    {
        _mapper = mapper;
    }

    public List<AcceptClusterProfile> Transform(
        List<AcceptClusterProfile> data,
        double redshift)
    {
        double dA_TRM = _mapper.CalculateTrmAngularDiameterDistance(redshift);

        double dA_GR = LambdaCdMHelper.CalculateAngularDiameterDistance(redshift);

        double f = dA_TRM / dA_GR;

        var result = new List<AcceptClusterProfile>();

        foreach(var p in data)
        {
            double r = p.RadiusMid_Mpc_GR * f;

            double ne = p.Ne_cm3 / Math.Pow(f, 3);

            double pressure = p.Pressure_cgs / Math.Pow(f, 3);

            double entropy = p.Entropy_keV_cm2 * Math.Pow(f, 2);

            double mass = p.Mass_Msun * f;

            result.Add(new AcceptClusterProfile(
                p.Name,
                r,
                ne,
                p.Temperature_keV,
                pressure,
                entropy,
                mass
            ));
        }

        return result;
    }
}

