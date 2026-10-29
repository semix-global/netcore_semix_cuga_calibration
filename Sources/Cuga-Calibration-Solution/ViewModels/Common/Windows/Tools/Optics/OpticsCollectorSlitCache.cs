using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public enum OpticsCollectorSlitCaptureModeEnum
{
    FixedHeight,
    VariableHeight
}

public sealed partial class OpticsCollectorSlitCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int PmtId { get; set; }

    [ObservableProperty]
    public partial int ImageWidthPixel { get; set; }

    [ObservableProperty]
    public partial OpticsCollectorSlitCaptureModeEnum CaptureMode { get; set; } = OpticsCollectorSlitCaptureModeEnum.FixedHeight;

    [ObservableProperty]
    public partial bool IsForward { get; set; } = true;

    [ObservableProperty]
    public partial OpticsConfiguration HazeOpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration HazeCIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial LaserLightInformation HazeLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial Point HazeBrightFieldPosition { get; set; }

    [ObservableProperty]
    public partial double RangeEcs { get; set; }

    [ObservableProperty]
    public partial double StepEcs { get; set; }

    [ObservableProperty]
    public partial double ScanLength { get; set; } = 2000d;

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial int BinWidthPixel { get; set; }

    [ObservableProperty]
    public partial int DiscardedPixelCount { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<OpticsCollectorSlitChannelResult> ChannelResults { get; set; } = [];

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        PmtId,
        CaptureMode,
        CenterECS,
        RangeEcs,
        IsForward,
        HazeLaserLightInformation,
        HazeBrightFieldPosition,
        Capture = CaptureMode switch
        {
            OpticsCollectorSlitCaptureModeEnum.FixedHeight => new HtmlQuote(new { ImageWidthPixel, StepEcs }),
            OpticsCollectorSlitCaptureModeEnum.VariableHeight => new HtmlQuote(new { ScanLength }),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<HtmlQuote>(nameof(CaptureMode), CaptureMode, null)
        },
        HazeOpticsConfiguration = new HtmlQuote(HazeOpticsConfiguration.ToHtmlAnonymous()),
        HazeCIBConfiguration = new HtmlQuote(HazeCIBConfiguration.ToHtmlAnonymous())
    };
}