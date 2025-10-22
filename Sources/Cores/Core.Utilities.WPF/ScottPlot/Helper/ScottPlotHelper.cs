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

    public static string ToSafeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Sheet1";

        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

        name = invalidChars.Aggregate(name, (current, ch) => current.Replace(ch, '_'));
        name = name.Trim();

        if (name.Length > 31) name = name[..31];

        if (string.IsNullOrEmpty(name)) name = "Sheet1";

        return name;
    }
}