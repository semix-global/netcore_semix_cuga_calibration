using Net.Utilities.Models.Geometries;

namespace Net.Utilities.WPF.MVVM.Providers;

public static class DialogWindowProviderExtensions
{
    public static void ShowPlotAsReadonly(this IDialogWindowProvider dialogWindowProvider, List<(string Title, IReadOnlyList<Point> Points)> plots)
    {
        dialogWindowProvider.ShowPlot(plots.Select<(string Title, IReadOnlyList<Point> Points), (string Title, Point[] Points)>(t => (t.Title, [.. t.Points])).ToList());
    }
}