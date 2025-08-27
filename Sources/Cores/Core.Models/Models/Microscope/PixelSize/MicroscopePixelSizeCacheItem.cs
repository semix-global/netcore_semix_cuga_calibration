using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCacheItem : ObservableCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Grid_100um;

    [ObservableProperty]
    private AlgorithmStandardMaskSquareSizeEnum _algorithmStandardMaskSquareSizeEnum = AlgorithmStandardMaskSquareSizeEnum.Size10;

    [ObservableProperty]
    private Point _findPosition;
}