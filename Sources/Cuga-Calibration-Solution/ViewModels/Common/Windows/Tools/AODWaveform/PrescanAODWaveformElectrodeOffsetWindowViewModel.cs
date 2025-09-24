using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed class PrescanAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile, PrescanAODWaveformElectrodeOffsetItem>;

public sealed class PrescanAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem<PrescanAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(PrescanAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class PrescanAODWaveformElectrodeOffsetWindowViewModel : AbstractAODWaveformElectrodeOffsetWindowViewModel<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile, PrescanAODWaveformElectrodeOffsetItem, PrescanAODWaveformElectrodeOffsetCache>
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