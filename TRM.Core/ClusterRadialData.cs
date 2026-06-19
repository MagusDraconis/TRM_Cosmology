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

public class AcceptShell
{
    public double RadiusKpc { get; set; }
    public double ElectronDensity { get; set; } // nelec
    public double Pressure { get; set; }        // Pitpl
    public double ReportedMass { get; set; }    // Mgrav (value from file)
    public double CalculatedMass { get; set; }  // Computed hydrostatic result
}