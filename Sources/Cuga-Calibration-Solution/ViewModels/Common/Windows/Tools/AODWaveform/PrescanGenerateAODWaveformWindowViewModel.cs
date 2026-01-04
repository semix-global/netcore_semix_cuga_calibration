using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(PrescanGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class PrescanGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>
{
    public override string Name => "Generate Prescan AOD Waveform";

    protected override void LoadedElectrodeOffsetResult(CancellationToken cancellationToken)
    {
        var prescanAODWaveformElectrodeInitializeCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeInitializeCache>();
        if (prescanAODWaveformElectrodeInitializeCache.ElectrodeConfigurationResults.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please initialize the prescan electrode configuration as it is currently empty.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return;
        }

        Cache.Param.ElectrodeConfigurations = prescanAODWaveformElectrodeInitializeCache.ElectrodeConfigurationResults;
    }

    protected override void GenerateAODWaveform(CancellationToken cancellationToken)
    {
        Cache.Profiles = [];
        Cache.AODWaveformResultFilePath = string.Empty;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.Param.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        Cache.Profiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles(CancellationToken cancellationToken) => LaserViewModel.SetPrescanAODWaveProfiles(Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum, Cache.Profiles);
}