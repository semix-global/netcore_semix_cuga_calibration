using ScottPlot;
using ScottPlot.WPF;
using SkiaSharp.Views.WPF;

namespace Core.Utilities.WPF.ScottPlot.Extensions;

public static class WPFPlotExtensions
{
    public static PixelRect GetCurrentDataRect(this WpfPlot @this, Plot plot)
    {
        var skElement = ObjectHelper.GetPropertyValue<SKElement>(@this, "PlotFrameworkElement");

        var subplotRectangles = @this.Multiplot.Layout.GetSubplotRectangles(
            @this.Multiplot.Subplots,
            new PixelRect(0, skElement.CanvasSize.Width, skElement.CanvasSize.Height, 0));

        var index = Array.IndexOf(@this.Multiplot.GetPlots(), plot);

        using var paint = Paint.NewDisposablePaint();
        
        return plot.Layout.LayoutEngine.GetLayout(new PixelRect(
            left: subplotRectangles[index].Left / (float)plot.ScaleFactor,
            right: subplotRectangles[index].Right / (float)plot.ScaleFactor,
            bottom: subplotRectangles[index].Bottom / (float)plot.ScaleFactor,
            top: subplotRectangles[index].Top / (float)plot.ScaleFactor), plot, paint).DataRect;
    }

    public static Coordinates GetCurrentCoordinates(this WpfPlot @this, Plot plot, Pixel pixel)
    {
        var scaledPx = pixel.Divide((float)plot.ScaleFactor);
        var dataRect = @this.GetCurrentDataRect(plot);
        var x = plot.Axes.Bottom.GetCoordinate(scaledPx.X, dataRect);
        var y = plot.Axes.Left.GetCoordinate(scaledPx.Y, dataRect);

        return new Coordinates(x, y);
    }
}