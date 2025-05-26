using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Models.Common.StageMap;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.BrightFieldStageMap;

public sealed partial class ChuckBrightFieldStageMapDto : CalibrationDtoBase, ICloneable<ChuckBrightFieldStageMapDto>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;

    [ObservableProperty]
    private StageMapDto _calibrationStageMap = new();

    [ObservableProperty]
    private StageMapDto _verifyStageMap = new();

    #region Mapper

    public ChuckBrightFieldStageMapDto Clone() => new()
    {
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
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