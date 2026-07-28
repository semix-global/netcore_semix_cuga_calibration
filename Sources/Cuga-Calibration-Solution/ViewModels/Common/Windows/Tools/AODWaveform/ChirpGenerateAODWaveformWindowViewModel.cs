using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed class ChirpGenerateAODWaveformCache : GenerateAODWaveformCache<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(ChirpGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ChirpGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<ChirpGenerateAODWaveformCache, GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>
{
    public override string Name => "Generate Chirp AOD Waveform";

    [DefaultCache]
    public override ChirpGenerateAODWaveformCache Cache
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

            var chirpCache = CacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

            var chirpResult = chirpCache.Results.SingleOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum
                                                                      && t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.Param.ProductivityInformation.OpticsMagType);

            if (chirpResult is null)
            {
                stringBuilder.AppendLine("Warning: Chirp AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.Param = chirpResult.GenerateChirpAODWaveformParam.Clone();
                Cache.Param.ProductivityInformation = Cache.Param.ProductivityInformation.Clone();
                stringBuilder.AppendLine("Ok: Chirp AOD Waveform Param Import Success!");
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

        var aodWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.Param.AdaptTo(), cancellationToken);

        Cache.Profiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles(CancellationToken cancellationToken) => LaserViewModel.SetChirpAODWaveProfiles(Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum, Cache.Profiles);
}