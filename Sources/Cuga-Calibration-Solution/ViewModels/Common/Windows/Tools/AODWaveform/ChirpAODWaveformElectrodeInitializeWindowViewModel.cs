using Core.Models.Models.Common.AODWaveform;
using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeInitializeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ChirpAODWaveformElectrodeInitializeWindowViewModel :
    AbstractAODWaveformElectrodeInitializeWindowViewModel<ChirpAODWaveformElectrodeInitializeCache, ChirpAODWaveformElectrodeInitializeItem, ChirpAODWaveformElectrodeInitializeResult>
{
    public override string Name => "Chirp AOD Waveform Electrode Initialize";

    [DefaultCache]
    public override ChirpAODWaveformElectrodeInitializeCache Cache
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    protected override async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = CacheProvider.GetOrDefault<ChirpAODWaveformElectrodeInitializeCache>();
            var chirpAODWaveformElectrodeOffsetCache = CacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

            Cache.ElectrodeConfigurationResults = chirpAODWaveformElectrodeOffsetCache.ElectrodeConfigurationResults;
        });
    }

    protected override void GenerateFlatnessAODWaveform(ChirpAODWaveformElectrodeInitializeItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        item.PrescanAODWaveformProfiles = [];
        item.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        foreach (var electrodeConfiguration in Cache.FlatnessGeneratePrescanAODWaveformParam.ElectrodeConfigurations) electrodeConfiguration.WithAmplitude(Cache.DefaultAmplitude);
        Cache.FlatnessGeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
        Cache.FlatnessGeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.FlatnessGeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

        item.ChirpAODWaveformProfiles = [];
        item.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.FlatnessGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.FlatnessGenerateChirpAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.FlatnessGenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.FlatnessGenerateChirpAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var chirpAODWaveformResult = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.FlatnessGenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
        item.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
            item.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
            FlatnessGenerateChirpAODWaveformParam = new HtmlQuote(Cache.FlatnessGenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
            item.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. item.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }

    protected override void GenerateScanAODWaveform(ChirpAODWaveformElectrodeInitializeItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        item.PrescanAODWaveformProfiles = [];
        item.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.ScanGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.ScanGeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.ScanGeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

        item.ChirpAODWaveformProfiles = [];
        item.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.ScanGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.ScanGenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.ScanGenerateChirpAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var chirpAODWaveformResult = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.ScanGenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
        item.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            ScanGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.ScanGeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            item.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
            ScanGenerateChirpAODWaveformParam = new HtmlQuote(Cache.ScanGenerateChirpAODWaveformParam.ToHtmlAnonymous()),
            item.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. item.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }

    protected override void SetAODWaveformProfiles(ChirpAODWaveformElectrodeInitializeItem item, Guid htmlLogUniqueId)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.ChirpAODWaveformProfiles);
    }

    protected override void GenerateResultAODWaveform(ChirpAODWaveformElectrodeInitializeResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        result.ChirpAODWaveformProfiles = [];
        result.ChirpAODWaveformResultFilePath = string.Empty;

        result.GenerateChirpAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;
        result.GenerateChirpAODWaveformParam.ElectrodeConfigurations = Cache.ElectrodeConfigurationResults;

        var aodWaveformResult = AODWaveformGenerator.GenerateChirpAODWaveform(result.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

        result.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        result.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation(result.GenerateChirpAODWaveformParam.ProductivityInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(result.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
            result.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. result.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }

    protected override void SetResultAODWaveformConfiguration(ChirpAODWaveformElectrodeInitializeResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken)
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