using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCacheItem : CalibrationDtoBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _magnificationInfo = new();

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

}