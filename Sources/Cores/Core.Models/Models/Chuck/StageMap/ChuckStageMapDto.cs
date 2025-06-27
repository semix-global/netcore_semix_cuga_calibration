using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.StageMap;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.StageMap;

public sealed partial class ChuckStageMapDto : CalibrationDtoBase, ICloneable<ChuckStageMapDto>, IAdaptTo<CalibrationChuckStageMap>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;
    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;

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
    private bool _isCalibrationBrightField = false;

    [ObservableProperty]
    private bool _isVerifyDarkField = false;

    [ObservableProperty]
    private bool _isVerifyBrightField = false;

    #region Mapper

    public ChuckStageMapDto Clone() => new()
    {
        HighMagnificationEnum = HighMagnificationEnum,
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
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationEnum, CgMicroscopeLens>(HighMagnificationEnum),
        OpticsMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        Speed = StageSpeedEnum.ToAdsSpeedEnum(),
        ExpandStageMap = ExpandStageMapDto.AdaptTo(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}