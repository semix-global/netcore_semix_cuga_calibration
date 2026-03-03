using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Microscope;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeItemDto : CalibrationDtoBase, ICloneable<MicroscopePixelSizeItemDto>, IAdaptTo<CalibrationMicroscopePixelSizeItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Size _pixelSize;

    [ObservableProperty]
    private string _originFilePath = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    #region Mapper

    public MicroscopePixelSizeItemDto Clone() => new()
    {
        LensInformation = LensInformation,
        FindPosition = FindPosition,
        PixelSize = PixelSize,
        OriginFilePath = OriginFilePath,
        FilePath = FilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        Id = Id,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Expiration = Expiration
    };

    public CalibrationMicroscopePixelSizeItem AdaptTo() => new()
    {
        CgMicroscopeLens = LensInformation.AdaptTo().LensCode,
        PixelSize = PixelSize.ToCgSize(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}