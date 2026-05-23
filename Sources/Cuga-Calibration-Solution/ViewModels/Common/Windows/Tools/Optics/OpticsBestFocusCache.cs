using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsBestFocusCache : OpticsGrabbingImageCache
{
    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point DSWFindBFMachinePosition { get; set; }

    public OpticsBestFocusCache()
    {
        StageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;
        CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
    }
}