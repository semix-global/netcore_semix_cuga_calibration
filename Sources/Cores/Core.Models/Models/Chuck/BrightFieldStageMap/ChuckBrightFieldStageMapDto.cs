using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.BrightFieldStageMap;

public sealed partial class ChuckBrightFieldStageMapDto : CalibrationDtoBase, ICloneable<ChuckBrightFieldStageMapDto>
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private StageMapDto _calibrationStageMap = new();

    [ObservableProperty]
    private StageMapDto _verifyStageMap = new();

    #region Mapper

    public ChuckBrightFieldStageMapDto Clone() => new()
    {
        MicroscopeMagnificationInfo = MicroscopeMagnificationInfo,
        CalibrationStageMap = CalibrationStageMap.Clone(),
        VerifyStageMap = VerifyStageMap.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}