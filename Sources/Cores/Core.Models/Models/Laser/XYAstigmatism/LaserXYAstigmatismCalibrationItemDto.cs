using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using ChirpAODWaveformResult = Core.Models.Models.Common.AODWaveform.ChirpAODWaveformResult;

namespace Core.Models.Models.Laser.XYAstigmatism;

// ReSharper disable once InconsistentNaming
public sealed partial class LaserXYAstigmatismCalibrationItemDto : CalibrationDtoBase, ICloneable<LaserXYAstigmatismCalibrationItemDto>, IAdaptTo<CalibrationLaserXYAstigmatismItem>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _frequencyChangeRate;

    [ObservableProperty]
    private double _ecsX;

    [ObservableProperty]
    private double _ecsY;

    [ObservableProperty]
    private double _ecsErrorValue;

    [ObservableProperty]
    private double _qualityX;

    [ObservableProperty]
    private double _qualityY;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _originFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformResult> _chirpAodWaveResultList = [];

    public CalibrationLaserXYAstigmatismItem AdaptTo() => new()
    {
        CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        ChirpAODWaveformResultList = ChirpAodWaveResultList.Select(t => new CalibrationChirpAODWaveformResult { CgAwgElectrodeEnum = (int)t.OpticsAODElectrodeEnum, FilePath = t.FilePath }).ToArray()
    };

    #region Mapper

    public LaserXYAstigmatismCalibrationItemDto Clone() => new()
    {
        Index = Index,
        MicroscopeLensInformation = MicroscopeLensInformation,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        FrequencyChangeRate = FrequencyChangeRate,
        EcsX = EcsX,
        EcsY = EcsY,
        EcsErrorValue = EcsErrorValue,
        QualityX = QualityX,
        QualityY = QualityY,
        FilePath = FilePath,
        OriginFilePath = OriginFilePath,
        ChirpAodWaveResultList = [.. ChirpAodWaveResultList.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}