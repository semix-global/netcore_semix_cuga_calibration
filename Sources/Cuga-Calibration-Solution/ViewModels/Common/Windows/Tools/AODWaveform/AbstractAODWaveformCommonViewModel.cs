using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract class AbstractAODWaveformCommonViewModel<TParam, TProfile> : ViewModelBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    protected abstract string AODWaveformName { get; }

    protected abstract (bool IsSuccess, IReadOnlyList<TProfile> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(TParam param, CancellationToken cancellationToken);

    protected abstract void SetAODWaveProfiles(TParam param, IReadOnlyList<TProfile> profiles);
}