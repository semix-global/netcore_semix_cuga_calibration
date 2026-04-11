using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.CIB.YPixelSize;

public sealed partial class CIBYPixelSizeDTO : CalibrationDtoBase, ICloneable<CIBYPixelSizeDTO>, IAdaptTo<CalibrationLaserPixelSizeItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private double _yPixelSize;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _drawImageFilePath = string.Empty;

    [ObservableProperty]
    private string _rawFilePath = string.Empty;

    #region Mapper

    public CIBYPixelSizeDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        PmtId = PmtId,
        FindBFMachinePosition = FindBFMachinePosition,
        YPixelSize = YPixelSize,
        FilePath = FilePath,
        DrawImageFilePath = DrawImageFilePath,
        RawFilePath = RawFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserPixelSizeItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        PmtId = PmtId,
        YPixelSize = YPixelSize,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}