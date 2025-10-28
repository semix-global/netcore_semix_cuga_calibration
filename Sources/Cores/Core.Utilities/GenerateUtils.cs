using MathNet.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;
using Constants = Net.Utilities.Models.Constants;

namespace Core.Utilities;

public static class GenerateUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [TargetedPatchingOptOut(Constants.TargetedPatchingOptOutReason)]
    public static double[] LinearContainsEdgeRange(double start, double step, double stop)
    {
        var values = Generate.LinearRange(start, step, stop);
        if (values.Contains(start) == false) values = [start, .. values];
        if (values.Contains(stop) == false) values = [.. values, stop];

        return values;
    }
}