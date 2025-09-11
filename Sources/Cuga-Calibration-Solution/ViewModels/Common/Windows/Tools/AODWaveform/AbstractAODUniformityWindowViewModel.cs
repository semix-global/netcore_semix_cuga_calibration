using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODUniformityCache<TParam> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
{
    [ObservableProperty]
    private TParam _param = new();

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private double _startCenterFrequency;

    [ObservableProperty]
    private double _endCenterFrequency;

    [ObservableProperty]
    private double _stepCenterFrequency;

    [ObservableProperty]
    private double _targetMeasurePower;

    [ObservableProperty]
    private double _coefficientStep = 0.1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMin))]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMax))]
    private double _targetThreshold = 0.05;

    public double TargetThresholdRateMin => 1 - TargetThreshold;

    public double TargetThresholdRateMax => 1 + TargetThreshold;

    [ObservableProperty]
    private double _retryCount = 20;
}

public sealed partial class AODUniformityItem<TProfile> : ObservableCacheBase
    where TProfile : AbstractAODWaveformProfile
{
    [ObservableProperty]
    private IReadOnlyList<TProfile> _profiles = [];
}

public abstract partial class AbstractAODUniformityWindowViewModel<TParam, TProfile> : ViewModelBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    [ObservableProperty]
    private AODUniformityCache<TParam> _cache = new();

    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private double _frequency = 0.495;

    [ObservableProperty]
    private double _measurePower;

    [ObservableProperty]
    private double _rateOfTargetPower;

    [ObservableProperty]
    private bool _isOk;
}