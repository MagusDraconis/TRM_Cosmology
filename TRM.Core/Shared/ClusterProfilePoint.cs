
namespace TRM.Core.Shared;


public record ClusterProfilePoint
(
    double RadiusMpc_GR,
    double Density_GR,
    double Temperature_keV
);


public record ClusterProfilePointTrm
(
    double RadiusMpc_TRM,
    double Density_TRM,
    double Temperature_keV
);


public record AcceptClusterPoint
(
    double RadiusKpc_GR,
    double ElectronDensity_cm3,
    double Temperature_keV,
    double Pressure_keV_cm3,
    double Entropy_keV_cm2
);


public record AcceptClusterPointTrm
(
    double RadiusKpc_TRM,
    double ElectronDensity_TRM,
    double Temperature_keV,
    double Pressure_TRM,
    double Entropy_TRM
);


public record AcceptClusterProfile
(
    string Name,
    double RadiusMid_Mpc_GR,
    double Ne_cm3,
    double Temperature_keV,
    double Pressure_cgs,
    double Entropy_keV_cm2,
    double Mass_Msun
);