using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.CIB.LineOrientationOffset;

[CacheVersion("1.0.0")]
public sealed partial class CIBLineOrientationOffsetDTO : CalibrationDTOBase<CIBLineOrientationOffsetDTO>, IAdaptTo<CalibrationCIBLineOrientationOffsetItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int PmtId { get; set; }

    [ObservableProperty]
    public partial Point StartPosition { get; set; }

    [ObservableProperty]
    public partial Point EndPosition { get; set; }

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial Point ForwardFindDarkMachinePosition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XOffset))]
    public partial Point ReverseFindDarkMachinePosition { get; set; }

    public double XOffset => ReverseFindDarkMachinePosition.X - ForwardFindDarkMachinePosition.X;

    [ObservableProperty]
    public partial string ForwardFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReverseFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    #region Mapper

    public override CIBLineOrientationOffsetDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        PmtId = PmtId,
        StartPosition = StartPosition,
        EndPosition = EndPosition,
        FindPosition = FindPosition,
        ForwardFindDarkMachinePosition = ForwardFindDarkMachinePosition,
        ReverseFindDarkMachinePosition = ReverseFindDarkMachinePosition,
        ForwardFilePath = ForwardFilePath,
        ReverseFilePath = ReverseFilePath,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationCIBLineOrientationOffsetItem AdaptTo() => new()
    {
        CgMicroscopeLens = MicroscopeLensInformation != MicroscopeLensInformation.Default ? MicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        PmtId = PmtId,
        XOffset = XOffset,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}