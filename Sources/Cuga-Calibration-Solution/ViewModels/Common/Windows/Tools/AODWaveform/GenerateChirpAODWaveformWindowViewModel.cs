using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(GenerateChirpAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class GenerateChirpAODWaveformWindowViewModel(LaserViewModel laserViewModel) : AbstractGenerateAODWaveformWindowViewModel<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>
{
    protected override string AODWaveformName => "Chirp";

    protected override (bool IsSuccess, IReadOnlyList<ChirpAODWaveformProfile> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(CancellationToken cancellationToken)
    {
        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.Param.AdaptTo(), cancellationToken);

        return (aodWaveformResult.IsSuccess, AODWaveformProfileFactory.CreateChirpList(aodWaveformResult), aodWaveformResult.FilePath, exception);
    }

    protected override void SetAODWaveProfiles(IReadOnlyList<ChirpAODWaveformProfile> profiles) => laserViewModel.SetChirpAODWaveProfiles(profiles);
}