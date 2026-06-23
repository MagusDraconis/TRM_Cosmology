using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TRM.Core.Shared;

namespace TRM.Core.Domains.Domain4.Supernovae;


public class PantheonTrmScaleSolver
{
    private readonly TrmDistanceMapper _mapper;

    public PantheonTrmScaleSolver(TrmDistanceMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public PantheonScaleFitResult Evaluate(
        List<SupernovaPoint> data)
    {
        if (data == null || data.Count == 0)
        {
            return new PantheonScaleFitResult(
                0,
                double.MaxValue,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN);
        }

        var residuals = new List<double>();

        double sumSquared = 0.0;
        double sumResidual = 0.0;
        double sumAbsResidual = 0.0;
        double maxAbsResidual = 0.0;

        int count = 0;

        foreach (var sn in data)
        {
            if (sn.Z <= 0.0)
                continue;

            double muTheo = _mapper.CalculateTrmDistanceModulus(sn.Z);

            double residual = sn.MuObs - muTheo;
            double absResidual = Math.Abs(residual);

            residuals.Add(residual);

            sumSquared += residual * residual;
            sumResidual += residual;
            sumAbsResidual += absResidual;

            if (absResidual > maxAbsResidual)
                maxAbsResidual = absResidual;

            count++;
        }

        if (count == 0)
        {
            return new PantheonScaleFitResult(
                0,
                double.MaxValue,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN);
        }

        double rms = Math.Sqrt(sumSquared / count);
        double meanResidual = sumResidual / count;
        double meanAbsResidual = sumAbsResidual / count;

        double centeredSumSquared = residuals
            .Select(r => r - meanResidual)
            .Sum(dr => dr * dr);

        double centeredRms = Math.Sqrt(centeredSumSquared / count);

        return new PantheonScaleFitResult(
            count,
            rms,
            meanResidual,
            meanAbsResidual,
            maxAbsResidual,
            centeredRms);
    }


}