using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities.SourceGenerators.Attributes;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed class PrescanGenerateAODWaveformCache : GenerateAODWaveformCache<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(PrescanGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
[DefaultCache(typeof(PrescanGenerateAODWaveformCache))]
public sealed class PrescanGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<PrescanGenerateAODWaveformCache, GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>
{
    public override string Name => "Generate Prescan AOD Waveform";

    protected override void LoadedElectrodeOffsetResult(CancellationToken cancellationToken)
    {
        var prescanAODWaveformElectrodeInitializeCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();
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

        var aodWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.Param.AdaptTo(), cancellationToken);

        Cache.Profiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles(CancellationToken cancellationToken) => LaserViewModel.SetPrescanAODWaveProfiles(Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum, Cache.Profiles);
}