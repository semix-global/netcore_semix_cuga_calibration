using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsBestFocusCache : OpticsGrabbingImageCache
{
    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _dSWFindBFMachinePosition;

    public OpticsBestFocusCache()
    {
        StageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;
        CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        StageCoordinateSystemEnum,
        ScanLength,
        CenterECS,
        RangeECS,
        CIBInformations,
        CalChipSiteModelEnum,
        CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
        LaserLightInformation,
        IsForward,
        IsKeepRawImageCIBProfileModeEnum
    };
}