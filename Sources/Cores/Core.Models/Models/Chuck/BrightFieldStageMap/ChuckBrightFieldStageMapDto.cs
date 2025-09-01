using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.BrightFieldStageMap;

public sealed partial class ChuckBrightFieldStageMapDto : CalibrationDtoBase, ICloneable<ChuckBrightFieldStageMapDto>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private StageMapDto _calibrationStageMap = new();

    [ObservableProperty]
    private StageMapDto _verifyStageMap = new();

    #region Mapper

    public ChuckBrightFieldStageMapDto Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
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