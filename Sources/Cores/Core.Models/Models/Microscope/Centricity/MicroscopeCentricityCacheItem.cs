using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCacheItem : CalibrationDtoBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;
}