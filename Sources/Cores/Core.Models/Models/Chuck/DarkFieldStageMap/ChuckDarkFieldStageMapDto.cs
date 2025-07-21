using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.DarkFieldStageMap;

public sealed partial class ChuckDarkFieldStageMapDto : CalibrationDtoBase, ICloneable<ChuckDarkFieldStageMapDto>
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

    [ObservableProperty]
    private StageMapDto _calibrationStageMap = new();

    [ObservableProperty]
    private StageMapDto _expandStageMapDto = new();

    [ObservableProperty]
    private StageMapDto _verifyDarkFieldStageMap = new();

    [ObservableProperty]
    private StageMapDto _verifyBrightFieldStageMap = new();

    [ObservableProperty]
    private bool _isVerifyDarkField;

    [ObservableProperty]
    private bool _isVerifyBrightField;

    #region Mapper

    public ChuckDarkFieldStageMapDto Clone() => new()
    {
        MicroscopeMagnificationInfo = MicroscopeMagnificationInfo,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        StageSpeedEnum = StageSpeedEnum,
        CalibrationStageMap = CalibrationStageMap.Clone(),
        ExpandStageMapDto = ExpandStageMapDto.Clone(),
        VerifyDarkFieldStageMap = VerifyDarkFieldStageMap.Clone(),
        VerifyBrightFieldStageMap = VerifyBrightFieldStageMap.Clone(),
        IsVerifyDarkField = IsVerifyDarkField,
        IsVerifyBrightField = IsVerifyBrightField,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}