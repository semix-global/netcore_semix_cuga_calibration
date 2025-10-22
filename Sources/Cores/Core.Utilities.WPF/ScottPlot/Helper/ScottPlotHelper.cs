namespace Core.Utilities.WPF.ScottPlot.Helper;

public static class ScottPlotHelper
{
    public static string FormatDouble(double value)
    {
        var result = value.ToString("f3");

        if (value - Math.Truncate(value) == 0) return value.ToString("f0");

        if (result.EndsWith(".000")) result = value.ToString("e3");

        return result;
    }
}