using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

[CacheVersion("1.0.0")]
public sealed partial class PupilSideChannelSpecularBlockerDTO : CalibrationDTOBase<PupilSideChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilSideChannelSpecularBlocker>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationMode { get; set; } = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh1 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh2 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial List<int> CgFFBoxBeginAndEndNumberCh1 { get; set; } = [];

    [ObservableProperty]
    public partial List<int> CgFFBoxBeginAndEndNumberCh2 { get; set; } = [];

    [ObservableProperty]
    public partial List<double> CgFFBoxMoveDownPercentListCh1 { get; set; } = [0.3];

    [ObservableProperty]
    public partial List<double> CgFFBoxMoveDownPercentListCh2 { get; set; } = [0.3];

    #region Mapper

    public override PupilSideChannelSpecularBlockerDTO Clone()
    {
        return new PupilSideChannelSpecularBlockerDTO
        {
            OpticsIlluminationMode = OpticsIlluminationMode,
            ProductivityInformation = ProductivityInformation.Clone(),
            CgFFBoxBeginPositionCh1 = CgFFBoxBeginPositionCh1,
            CgFFBoxBeginPositionCh2 = CgFFBoxBeginPositionCh2,
            CgFFBoxBeginAndEndNumberCh1 = [.. CgFFBoxBeginAndEndNumberCh1],
            CgFFBoxBeginAndEndNumberCh2 = [.. CgFFBoxBeginAndEndNumberCh2],
            CgFFBoxMoveDownPercentListCh1 = [.. CgFFBoxMoveDownPercentListCh1],
            CgFFBoxMoveDownPercentListCh2 = [.. CgFFBoxMoveDownPercentListCh2],

            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationPupilSideChannelSpecularBlocker AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        CgFFBoxMoveDownPercentListCh1 = CgFFBoxMoveDownPercentListCh1,
        CgFFBoxMoveDownPercentListCh2 = CgFFBoxMoveDownPercentListCh2,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}