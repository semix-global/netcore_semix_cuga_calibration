using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;

namespace Core.Utilities.WPF.ScottPlot.Interactivity.UserActionResponses;

internal sealed class ScatterMouseMove : IUserActionResponse
{
    public void ResetState(IPlotControl plotControl)
    {
    }

    public ResponseInfo Execute(IPlotControl plotControl, IUserAction userInput, KeyboardState keys)
    {
        if (userInput is not IMouseAction mouseAction) return ResponseInfo.NoActionRequired;

        var mousePixel = mouseAction.Pixel;

#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plotPlot = plotControl.Multiplot.GetPlotAtPixel(mousePixel);
        if (plotPlot is null) return ResponseInfo.NoActionRequired;

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        var mouseLocation = plotPlot.GetCoordinates(mousePixel);

        var nearestList = plotPlot.PlottableList.OfType<Scatter>()
            .Select(t => (Scatter: t, Result: t.Data.GetNearest(mouseLocation, plotPlot.LastRender)))
            .Where(t => t.Result.IsReal)
            .ToList();

        var crossHair = plotPlot.PlottableList.OfType<Crosshair>().First();
        var annotation = plotPlot.PlottableList.OfType<Annotation>().First();

        if (nearestList.Count > 0 && nearestList[^1].Result.IsReal)
        {
            crossHair.IsVisible = true;
            crossHair.Position = nearestList[^1].Result.Coordinates;
            crossHair.MarkerShape = MarkerShape.OpenCircle;
            crossHair.MarkerSize = 10;

            annotation.IsVisible = true;
            annotation.LabelText = $"{nearestList[^1].Scatter.LegendText}: {FormatDouble(nearestList[^1].Result.X)}, {FormatDouble(nearestList[^1].Result.Y)}";
            annotation.LabelBackgroundColor = nearestList[^1].Scatter.LineColor;
            annotation.LabelBold = true;
        }
        else
        {
            crossHair.IsVisible = true;
            crossHair.Position = mouseLocation;
            crossHair.MarkerShape = MarkerShape.None;
            crossHair.MarkerSize = 0;

            annotation.IsVisible = true;
            annotation.LabelText = $"{FormatDouble(mouseLocation.X)}, {FormatDouble(mouseLocation.Y)}";
            annotation.LabelBackgroundColor = Colors.Yellow;
            annotation.LabelBold = false;
        }

        annotation.LabelFontColor = ToForegroundColor(annotation.LabelBackgroundColor);

        return ResponseInfo.Refresh;
    }

    private static string FormatDouble(double value)
    {
        var result = value.ToString("f3");

        if (value - Math.Truncate(value) == 0) return value.ToString("f0");

        if (result.EndsWith(".000")) result = value.ToString("e3");

        return result;
    }

    private static Color ToForegroundColor(Color backgroundColor)
    {
        // 使用加权平方根公式计算亮度（人眼感知模型）
        var luma = (int)Math.Sqrt(backgroundColor.Red * backgroundColor.Red * 0.299 + backgroundColor.Green * backgroundColor.Green * 0.587 + backgroundColor.Blue * backgroundColor.Blue * 0.114);

        return luma > 130 ? Colors.Black : Colors.White;
    }
}