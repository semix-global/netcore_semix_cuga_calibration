using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed class PrescanGenerateAODWaveformCache : GenerateAODWaveformCache<GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(PrescanGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class PrescanGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<PrescanGenerateAODWaveformCache, GeneratePrescanAODWaveformParam, PrescanAODWaveformProfile>
{
    public override string Name => "Generate Prescan AOD Waveform";

    [DefaultCache]
    public override PrescanGenerateAODWaveformCache Cache
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    protected override void ImportAODWaveformParams()
    {
        try
        {
            var isSuccess = true;

            var stringBuilder = new StringBuilder();

            var prescanCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            var prescanResult = prescanCache.Results.FirstOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.Equals(Cache.Param.ProductivityInformation));

            if (prescanResult is null)
            {
                stringBuilder.AppendLine("Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.Param = prescanResult.GeneratePrescanAODWaveformParam.Clone();;
                stringBuilder.AppendLine("Ok: Prescan AOD Waveform Param Import Success!");
            }

            DialogWindowProvider.ShowDialog(stringBuilder.ToString(), DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }
        catch (Exception ex)
        {
            DialogWindowProvider.ShowDialog($"""
                                             Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            Logger.LogError(ex, "Import Parameters Failed");
        }
    }

    protected override void GenerateAODWaveform(CancellationToken cancellationToken)
    {
        Cache.Profiles = [];
        Cache.AODWaveformResultFilePath = string.Empty;

        var aodWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.Param.AdaptTo(), cancellationToken);

        Cache.Profiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles(CancellationToken cancellationToken) => LaserViewModel.SetPrescanAODWaveProfiles(Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum, Cache.Profiles);
}