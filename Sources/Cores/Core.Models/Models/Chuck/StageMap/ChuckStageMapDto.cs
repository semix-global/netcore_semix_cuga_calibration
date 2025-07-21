using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.StageMap;

public sealed partial class ChuckStageMapDto : CalibrationDtoBase, ICloneable<ChuckStageMapDto>, IAdaptTo<CalibrationChuckStageMap>
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _highMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

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
        HighMicroscopeMagnificationInfo = HighMicroscopeMagnificationInfo,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        StageSpeedEnum = StageSpeedEnum,
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
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationInfo, CgMicroscopeLens>(HighMicroscopeMagnificationInfo),
        OpticsMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        Speed = StageSpeedEnum.ToAdsSpeedEnum(),
        ExpandStageMap = ExpandStageMapDto.AdaptTo(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}