using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using ScottPlot;
using ScottPlot.DataSources;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed class MouseDragTextCrosshair(MouseButton mouseButton, MouseButton confirmMouseButton) : IUserActionResponse
{
    public MouseButton MouseButton { get; } = mouseButton;

    public MouseButton ConfirmMouseButton { get; } = confirmMouseButton;

    public Coordinates? ConfirmCoordinates { get; private set; }

#pragma warning disable IDE0079
#pragma warning disable IDISP006

    private Plot? _rememberedMouseDownPlot;

#pragma warning restore IDISP006
#pragma warning restore IDE0079

    public ResponseInfo Execute(IPlotControl plotControl, IUserAction userAction, KeyboardState keys)
    {
        if (userAction is IMouseButtonAction mouseDownAction
            && mouseDownAction.Button == MouseButton
            && mouseDownAction.IsPressed)
        {
            ConfirmCoordinates = null;

#pragma warning disable IDE0079
#pragma warning disable IDISP001
#pragma warning disable IDISP003

            var plotAtPixel = plotControl.Multiplot.GetPlotAtPixel(mouseDownAction.Pixel);
            if (plotAtPixel is not null) _rememberedMouseDownPlot = plotAtPixel;

#pragma warning restore IDISP003
#pragma warning restore IDISP001
#pragma warning restore IDE0079

            return ResponseInfo.NoActionRequired;
        }

        if (userAction is IMouseButtonAction { IsPressed: false } mouseUpAction
            && _rememberedMouseDownPlot is not null)
        {
            var crossHair = _rememberedMouseDownPlot.PlottableList.OfType<Crosshair>().First();
            var annotation = _rememberedMouseDownPlot.PlottableList.OfType<Annotation>().First();

            if (mouseUpAction.Button == MouseButton)
            {
                crossHair.IsVisible = false;
                annotation.IsVisible = false;

                ResetState(plotControl);

                return ResponseInfo.Refresh;
            }

            if (mouseUpAction.Button == ConfirmMouseButton)
            {
                if (crossHair.IsVisible && annotation.IsVisible) ConfirmCoordinates = crossHair.Position;

                ResetState(plotControl);

                return new ResponseInfo { IsPrimary = true, RefreshNeeded = true };
            }
        }

        if (userAction is IMouseAction mouseAction && _rememberedMouseDownPlot is not null)
        {
            var mouseLocation = _rememberedMouseDownPlot.GetCoordinates(mouseAction.Pixel);

            var scatterSourceCoordinatesArray = new ScatterSourceCoordinatesArray([.. _rememberedMouseDownPlot.PlottableList.OfType<Text>().Select(t => t.Location)]);
            var dataPoint = scatterSourceCoordinatesArray.GetNearest(mouseLocation, _rememberedMouseDownPlot.LastRender);

            var crossHair = _rememberedMouseDownPlot.PlottableList.OfType<Crosshair>().First();
            var annotation = _rememberedMouseDownPlot.PlottableList.OfType<Annotation>().First();

            crossHair.IsVisible = true;
            annotation.IsVisible = true;

            if (dataPoint.IsReal)
            {
                var text = _rememberedMouseDownPlot.PlottableList.OfType<Text>().OrderBy(t => t.Location.Distance(dataPoint.Coordinates)).First();

                crossHair.Position = dataPoint.Coordinates;
                crossHair.MarkerShape = MarkerShape.OpenCircle;
                crossHair.MarkerSize = Math.Abs(text.LabelBorderWidth) * 1.5f;

                annotation.LabelText = $"{text.LabelText}: {ScottPlotHelper.FormatDouble(dataPoint.X)}, {ScottPlotHelper.FormatDouble(dataPoint.Y)}";
                annotation.LabelBackgroundColor = text.LabelBackgroundColor;
                annotation.LabelBold = true;
            }
            else
            {
                crossHair.Position = mouseLocation;
                crossHair.MarkerShape = MarkerShape.None;
                crossHair.MarkerSize = 0;

                annotation.LabelText = $"{ScottPlotHelper.FormatDouble(mouseLocation.X)}, {ScottPlotHelper.FormatDouble(mouseLocation.Y)}";
                annotation.LabelBackgroundColor = Colors.Yellow;
                annotation.LabelBold = false;
            }

            annotation.LabelFontColor = annotation.LabelBackgroundColor.ToReadableForegroundColor();

            return ResponseInfo.Refresh;
        }

        return ResponseInfo.NoActionRequired;
    }

    public void ResetState(IPlotControl plotControl)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP003

        _rememberedMouseDownPlot = null;

#pragma warning restore IDISP003
#pragma warning restore IDE0079
    }
}