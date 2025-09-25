using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(PrescanGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class PrescanGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>
{
    public override string Name => "Generate Prescan AOD Waveform";

    protected override void GenerateAODWaveform(CancellationToken cancellationToken)
    {
        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.Param.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

        Cache.Profiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles() => LaserViewModel.SetPrescanAODWaveProfiles(Cache.Profiles);
}