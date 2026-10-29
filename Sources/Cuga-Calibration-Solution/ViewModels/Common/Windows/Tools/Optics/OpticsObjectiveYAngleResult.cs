using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsObjectiveYAngleResult : ObservableObject
{
    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [ObservableProperty]
    public partial string HazeImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ShinyWaferImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Point CenterChannelLightCenterPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point ReflectedLightCenterPosition { get; set; } = Point.Origin;

    public double LightCenterYPixelOffset => CenterChannelLightCenterPosition.Y - ReflectedLightCenterPosition.Y;

    [ObservableProperty]
    public partial double CenterChannelLightDiameterPixel { get; set; }

    [ObservableProperty]
    public partial double HorizontalDegree { get; set; }

    [ObservableProperty]
    public partial double YAngleDegrees { get; set; }

    public object ToHtmlAnonymous() => new
    {
        IsOk,
        CenterChannelLightCenterPosition,
        ReflectedLightCenterPosition,
        LightCenterYPixelOffset = Math.Round(LightCenterYPixelOffset, 4),
        HorizontalDegree = Math.Round(HorizontalDegree, 4),
        CenterChannelLightDiameterPixel,
        YAngleDegrees,
        HazeImage = new HtmlImage(HazeImageFilePath),
        ShinyWaferImage = new HtmlImage(ShinyWaferImageFilePath),
        ResultImage = new HtmlImage(ResultImageFilePath)
    };
}