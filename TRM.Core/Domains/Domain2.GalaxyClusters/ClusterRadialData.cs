using System;
using System.Collections.Generic;
using System.Text;

namespace TRM.Core;

public class ClusterRadialData
{
    public double RadiusKpc { get; set; }
    public double TemperatureKev { get; set; }
    public double ElectronDensity { get; set; }

    // Computed from the measured profile data
    public double RequiredMass { get; set; }
}
