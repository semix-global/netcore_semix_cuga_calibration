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
    private OpticsIlluminationModeEnum _opticsIlluminationMode = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh1 = Point.Origin;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh2 = Point.Origin;

    [ObservableProperty]
    public List<int> _cgFFBoxBeginAndEndNumberCh1 = [];

    [ObservableProperty]
    public List<int> _cgFFBoxBeginAndEndNumberCh2 = [];

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh1 = [0.3];

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh2 = [0.3];

    #region Mapper

    public override PupilSideChannelSpecularBlockerDTO Clone()
    {
        return new PupilSideChannelSpecularBlockerDTO
        {
            OpticsIlluminationMode = OpticsIlluminationMode,
            ProductivityInformation = ProductivityInformation.Clone(),
            CgFFBoxBeginPositionCh1 = CgFFBoxBeginPositionCh1,
            CgFFBoxBeginPositionCh2 = CgFFBoxBeginPositionCh2,
            CgFFBoxBeginAndEndNumberCh1 = CgFFBoxBeginAndEndNumberCh1,
            CgFFBoxBeginAndEndNumberCh2 = CgFFBoxBeginAndEndNumberCh2,
            CgFFBoxMoveDownPercentListCh1 = CgFFBoxMoveDownPercentListCh1,
            CgFFBoxMoveDownPercentListCh2 = CgFFBoxMoveDownPercentListCh2,

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