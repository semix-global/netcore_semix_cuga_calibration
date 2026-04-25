using Core.Models.Models.Common.AODWaveform;
using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(PrescanAODWaveformElectrodeInitializeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class PrescanAODWaveformElectrodeInitializeWindowViewModel :
    AbstractAODWaveformElectrodeInitializeWindowViewModel<PrescanAODWaveformElectrodeInitializeCache, PrescanAODWaveformElectrodeInitializeItem, PrescanAODWaveformElectrodeInitializeResult>
{
    public override string Name => "Prescan AOD Waveform Electrode Initialize";

    [DefaultCache]
    public override PrescanAODWaveformElectrodeInitializeCache Cache
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    protected override async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeInitializeCache>();
            var prescanAODWaveformElectrodeOffsetCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            Cache.ElectrodeConfigurationResults = prescanAODWaveformElectrodeOffsetCache.ElectrodeConfigurationResults;
        });
    }

    protected override void GenerateFlatnessAODWaveform(PrescanAODWaveformElectrodeInitializeItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        item.PrescanAODWaveformProfiles = [];
        item.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.FlatnessGeneratePrescanAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.FlatnessGeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.FlatnessGeneratePrescanAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.FlatnessGeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

        item.ChirpAODWaveformProfiles = [];
        item.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.FlatnessGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        foreach (var electrodeConfiguration in Cache.FlatnessGenerateChirpAODWaveformParam.ElectrodeConfigurations) electrodeConfiguration.WithAmplitude(Cache.DefaultAmplitude);
        Cache.FlatnessGenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);
        Cache.FlatnessGenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

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

    protected override void GenerateScanAODWaveform(PrescanAODWaveformElectrodeInitializeItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        item.PrescanAODWaveformProfiles = [];
        item.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.ScanGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.ScanGeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.ScanGeneratePrescanAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.ScanGeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

        item.ChirpAODWaveformProfiles = [];
        item.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.ScanGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.ScanGenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

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

    protected override void SetAODWaveformProfiles(PrescanAODWaveformElectrodeInitializeItem item, Guid htmlLogUniqueId)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.ChirpAODWaveformProfiles);
    }

    protected override void GenerateResultAODWaveform(PrescanAODWaveformElectrodeInitializeResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        result.PrescanAODWaveformProfiles = [];
        result.PrescanAODWaveformResultFilePath = string.Empty;

        result.GeneratePrescanAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;
        result.GeneratePrescanAODWaveformParam.ElectrodeConfigurations = Cache.ElectrodeConfigurationResults;

        var aodWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(result.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

        result.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        result.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation(result.GeneratePrescanAODWaveformParam.ProductivityInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(result.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            result.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. result.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }

    protected override void SetResultAODWaveformConfiguration(PrescanAODWaveformElectrodeInitializeResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        ConfigViewModel.SetPrescanAODWaveformConfiguration(result.GeneratePrescanAODWaveformParam.ProductivityInformation, result.PrescanAODWaveformResultFilePath);

        if (htmlLogUniqueId == Guid.Empty) return;

        Logger.LogHtmlInformation(result.GeneratePrescanAODWaveformParam.ProductivityInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(result.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            result.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. result.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), htmlLogUniqueId.LoggingHtml());
    }
}