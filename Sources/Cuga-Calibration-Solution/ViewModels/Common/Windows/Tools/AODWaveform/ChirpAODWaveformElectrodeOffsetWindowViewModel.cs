using Core.Models.Models.Common.AODWaveform;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ChirpAODWaveformElectrodeOffsetWindowViewModel :
    AbstractAODWaveformElectrodeOffsetWindowViewModel<ChirpAODWaveformElectrodeOffsetCache, ChirpAODWaveformElectrodeOffsetItem, ChirpAODWaveformElectrodeOffsetResult>
{
    public override string Name => "Chirp AOD Waveform Electrode Offset";

    [DefaultCache]
    public override ChirpAODWaveformElectrodeOffsetCache Cache
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    protected override void GenerateAndSetFlatnessAODWaveform(ChirpAODWaveformElectrodeOffsetItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        Cache.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        foreach (var electrodeConfiguration in Cache.FlatnessGeneratePrescanAODWaveformParam.ElectrodeConfigurations) electrodeConfiguration.WithAmplitude(Cache.DefaultAmplitude);
        Cache.FlatnessGeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
        Cache.FlatnessGeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var prescanAODWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.FlatnessGeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

        var prescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        var prescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

        Cache.FlatnessGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.FlatnessGenerateChirpAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.FlatnessGenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.FlatnessGenerateChirpAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.FlatnessGenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

        var chirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
        var chirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

        if (htmlLogUniqueId != Guid.Empty)
        {
            Logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                PrescanAODWaveformResultFilePath = prescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                FlatnessGenerateChirpAODWaveformParam = new HtmlQuote(Cache.FlatnessGenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
                ChirpAODWaveformResultFilePath = chirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. chirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
            }), htmlLogUniqueId.LoggingHtml());
        }

        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, chirpAODWaveformProfiles);
    }

    protected override void GenerateResultAODWaveform(ChirpAODWaveformElectrodeOffsetResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        result.ChirpAODWaveformProfiles = [];
        result.ChirpAODWaveformResultFilePath = string.Empty;

        result.GenerateChirpAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;
        result.GenerateChirpAODWaveformParam.ElectrodeConfigurations = Cache.ElectrodeConfigurationResults;

        var aodWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(result.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

        result.ChirpAODWaveformProfiles = [.. AODWaveformProfileFactory.CreateChirpList(aodWaveformResult)];
        result.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation(result.GenerateChirpAODWaveformParam.ProductivityInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(result.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
            result.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. result.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }

    protected override void SetResultAODWaveformConfiguration(ChirpAODWaveformElectrodeOffsetResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        ConfigViewModel.SetChirpAODWaveformConfiguration(result.GenerateChirpAODWaveformParam.ProductivityInformation, result.ChirpAODWaveformResultFilePath);

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation(result.GenerateChirpAODWaveformParam.ProductivityInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(result.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
            result.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. result.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }
}