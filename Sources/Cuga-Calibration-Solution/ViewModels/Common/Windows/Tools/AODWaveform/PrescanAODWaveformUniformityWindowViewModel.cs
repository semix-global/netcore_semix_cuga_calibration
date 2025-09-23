using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed class PrescanAODWaveformUniformityCache : AODWaveformUniformityCache<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile, PrescanAODWaveformUniformityItem>;

public sealed class PrescanAODWaveformUniformityItem : AODWaveformUniformityItem<PrescanAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(PrescanAODWaveformUniformityWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class PrescanAODWaveformUniformityWindowViewModel : AbstractAODWaveformUniformityWindowViewModel<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile, PrescanAODWaveformUniformityItem, PrescanAODWaveformUniformityCache>
{
    protected override string AODWaveformName => "Prescan";

    protected override (bool IsSuccess, IReadOnlyList<PrescanAODWaveformProfile> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(GeneratePrescanAODWaveformParam param, CancellationToken cancellationToken)
    {
        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(param.AdaptTo(), cancellationToken);

        return (aodWaveformResult.IsSuccess, AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult), aodWaveformResult.FilePath, exception);
    }

    protected override void SetAODWaveProfiles(GeneratePrescanAODWaveformParam param, IReadOnlyList<PrescanAODWaveformProfile> profiles)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(profiles);
        LaserViewModel.SetChirpAODWaveProfile(param.OpticsMagTypeEnum);
    }
}