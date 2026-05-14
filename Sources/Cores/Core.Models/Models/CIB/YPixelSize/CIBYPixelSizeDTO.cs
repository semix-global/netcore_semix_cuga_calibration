using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.CIB.YPixelSize;

[CacheVersion("1.0.0")]
public sealed partial class CIBYPixelSizeDTO : CalibrationDTOBase<CIBYPixelSizeDTO>, IAdaptTo<CalibrationLaserPixelSizeItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int PmtId { get; set; }

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double YPixelSize { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DrawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawFilePath { get; set; } = string.Empty;

    #region Mapper

    public override CIBYPixelSizeDTO Clone() => new()
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