using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(GeneratePrescanAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class GeneratePrescanAODWaveformWindowViewModel(LaserViewModel laserViewModel) : AbstractGenerateAODWaveformWindowViewModel<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>
{
    protected override string AODWaveformName => "Prescan";

    protected override (bool IsSuccess, IReadOnlyList<PrescanAODWaveformProfile> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(CancellationToken cancellationToken)
    {
        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.Param.AdaptTo(), cancellationToken);

        return (aodWaveformResult.IsSuccess, AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult), aodWaveformResult.FilePath, exception);
    }

    protected override void SetAODWaveProfiles(IReadOnlyList<PrescanAODWaveformProfile> profiles) => laserViewModel.SetPrescanAODWaveProfiles(profiles);
}