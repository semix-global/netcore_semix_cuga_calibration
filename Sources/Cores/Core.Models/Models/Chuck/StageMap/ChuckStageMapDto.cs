using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.StageMap;

public sealed partial class ChuckStageMapDto : CalibrationDtoBase, ICloneable<ChuckStageMapDto>, IAdaptTo<CalibrationChuckStageMap>
{
    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private StageMapDto _calibrationBrightFieldStageMap = new();

    [ObservableProperty]
    private StageMapDto _calibrationDarkFieldStageMap = new();

    [ObservableProperty]
    private StageMapDto _expandStageMapDto = new();

    [ObservableProperty]
    private StageMapDto _verifyDarkFieldStageMap = new();

    [ObservableProperty]
    private StageMapDto _verifyBrightFieldStageMap = new();

    [ObservableProperty]
    private bool _isCalibrationBrightField;

    [ObservableProperty]
    private bool _isVerifyDarkField;

    [ObservableProperty]
    private bool _isVerifyBrightField;

    #region Mapper

    public ChuckStageMapDto Clone() => new()
    {
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
        ProductivityInformation = ProductivityInformation.Clone(),
        CalibrationBrightFieldStageMap = CalibrationBrightFieldStageMap.Clone(),
        CalibrationDarkFieldStageMap = CalibrationDarkFieldStageMap.Clone(),
        ExpandStageMapDto = ExpandStageMapDto.Clone(),
        VerifyDarkFieldStageMap = VerifyDarkFieldStageMap.Clone(),
        VerifyBrightFieldStageMap = VerifyBrightFieldStageMap.Clone(),
        IsCalibrationBrightField = IsCalibrationBrightField,
        IsVerifyDarkField = IsVerifyDarkField,
        IsVerifyBrightField = IsVerifyBrightField,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckStageMap AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation != MicroscopeLensInformation.Default ? HighMicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        OpticsMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        ExpandStageMap = ExpandStageMapDto.AdaptTo(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}