using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.Attenuator;

public sealed partial class LaserAttenuatorObjDto : CalibrationDtoBase, ICloneable<LaserAttenuatorObjDto>
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private Point _stagePosition;

    [ObservableProperty]
    private double _initialightIntensity;

    [ObservableProperty]
    private double _laserPowerMeterAverageIntensity;

    [ObservableProperty]
    private List<double> _prescanWaveFormLightIntensitites = [];

    [ObservableProperty]
    private Point[] _coefficientCurvePositions = [];

    [ObservableProperty]
    private Point[] _coefficientFitCurvePositions = [];

    [ObservableProperty]
    private int _interval;

    [ObservableProperty]
    private string _filePath = string.Empty;

    #region Mapper

    public LaserAttenuatorObjDto Clone() => new()
    {
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        StagePosition = StagePosition,
        InitialightIntensity = InitialightIntensity,
        LaserPowerMeterAverageIntensity = LaserPowerMeterAverageIntensity,
        PrescanWaveFormLightIntensitites = [.. PrescanWaveFormLightIntensitites],
        CoefficientCurvePositions = [.. CoefficientCurvePositions],
        CoefficientFitCurvePositions = [.. CoefficientFitCurvePositions],
        Interval = Interval,
        FilePath = FilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}