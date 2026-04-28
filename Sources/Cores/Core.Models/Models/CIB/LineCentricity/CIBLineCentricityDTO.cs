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

namespace Core.Models.Models.CIB.LineCentricity;

[CacheVersion("1.0.0")]
public sealed partial class CIBLineCentricityDTO : CalibrationDtoBase, ICloneable<CIBLineCentricityDTO>, IAdaptTo<CalibrationLaserLineCentricityItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private Point _findDFMachinePosition;

    [ObservableProperty]
    private Point _dFMachineCenterPosition;

    [ObservableProperty]
    private Point _dFMatchPositionOffset;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _rawFilePath = string.Empty;

    #region Mapper

    public CIBLineCentricityDTO Clone()
    {
        return new CIBLineCentricityDTO
        {
            MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
            ProductivityInformation = ProductivityInformation.Clone(),
            PmtId = PmtId,
            FindDFMachinePosition = FindDFMachinePosition,
            DFMachineCenterPosition = DFMachineCenterPosition,
            DFMatchPositionOffset = DFMatchPositionOffset,
            FilePath = FilePath,
            RawFilePath = RawFilePath,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationLaserLineCentricityItem AdaptTo()
    {
        return new CalibrationLaserLineCentricityItem
        {
            CgMicroscopeLens = MicroscopeLensInformation != MicroscopeLensInformation.Default ? MicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
            CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
            CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
            Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
            PmtId = PmtId,
            DarkMachineCenterPosition = DFMachineCenterPosition.ToCgPoint(),
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}