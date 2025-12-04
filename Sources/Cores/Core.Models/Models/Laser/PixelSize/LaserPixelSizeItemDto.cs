using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.PixelSize;

public sealed partial class LaserPixelSizeItemDto : CalibrationDtoBase, ICloneable<LaserPixelSizeItemDto>, IAdaptTo<CalibrationLaserPixelSizeItem>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationMode = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _yPixelSize;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _originFilePath = string.Empty;

    #region Mapper

    public LaserPixelSizeItemDto Clone() => new()
    {
        OpticsIlluminationMode = OpticsIlluminationMode,
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        PmtId = PmtId,
        FindPosition = FindPosition,
        YPixelSize = YPixelSize,
        FilePath = FilePath,
        OriginFilePath = OriginFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserPixelSizeItem AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationMode.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        PmtId = PmtId,
        YPixelSize = YPixelSize,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}