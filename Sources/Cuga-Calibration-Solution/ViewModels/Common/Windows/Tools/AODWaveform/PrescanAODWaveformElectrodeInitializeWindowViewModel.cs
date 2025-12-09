using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeInitializeCache : AODWaveformElectrodeInitializeCache<PrescanAODWaveformElectrodeInitializeItem, PrescanAODWaveformElectrodeInitializeResult>
{
    [ObservableProperty]
    private double _chirpFrequency;

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}

public sealed partial class PrescanAODWaveformElectrodeInitializeItem : AODWaveformElectrodeInitializeItem
{
    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

public sealed partial class PrescanAODWaveformElectrodeInitializeResult : AODWaveformElectrodeInitializeResult
{
    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

[IOCAppService(ServiceType = typeof(PrescanAODWaveformElectrodeInitializeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PrescanAODWaveformElectrodeInitializeWindowViewModel : AbstractAODWaveformElectrodeInitializeWindowViewModel<PrescanAODWaveformElectrodeInitializeCache, PrescanAODWaveformElectrodeInitializeItem, PrescanAODWaveformElectrodeInitializeResult>
{
    public override string Name => "Prescan AOD Waveform Initialize";

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeInitializeCache>();
            var prescanAODWaveformElectrodeOffsetCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            Cache.ElectrodeConfigurationResults = prescanAODWaveformElectrodeOffsetCache.ElectrodeConfigurationResults;
        });
    }


    protected override void GenerateFlatnessFixedAODWaveform(CancellationToken cancellationToken)
    {
        Cache.ChirpAODWaveformProfiles = [];
        Cache.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.FlatnessGenerateChirpAODWaveformParam.OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum;
        Cache.FlatnessGenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.FlatnessGenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);
        Cache.FlatnessGenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.FlatnessGenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        Cache.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(Cache.FlatnessGenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
            Cache.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void GenerateFlatnessChangedAODWaveform(PrescanAODWaveformElectrodeInitializeItem item, CancellationToken cancellationToken)
    {
        Cache.FlatnessGeneratePrescanAODWaveformParam.OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum;
        Cache.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.FlatnessGeneratePrescanAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.FlatnessGeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.FlatnessGenerateChirpAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.FlatnessGeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        item.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
            item.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void SetAODWaveformProfiles(PrescanAODWaveformElectrodeInitializeItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(Cache.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);
    }

    protected override void GenerateAODWaveform(PrescanAODWaveformElectrodeInitializeItem item, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override void GenerateResultAODWaveform(PrescanAODWaveformElectrodeInitializeResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        result.GeneratePrescanAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;
        result.GeneratePrescanAODWaveformParam.ElectrodeConfigurations = Cache.ElectrodeConfigurationResults;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(result.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        result.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        result.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation($"{result.GeneratePrescanAODWaveformParam.OpticsIlluminationModeEnum}-{result.GeneratePrescanAODWaveformParam.ProductivityInformation}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(result.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            result.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. result.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void SetResultAODWaveformProfiles(PrescanAODWaveformElectrodeInitializeResult result, CancellationToken cancellationToken)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, result.PrescanAODWaveformProfiles);
    }

    protected override void SetResultAODWaveformConfig(PrescanAODWaveformElectrodeInitializeResult result, CancellationToken cancellationToken)
    {
        ConfigViewModel.SetPrescanAODWaveProfiles(Cache.OpticsIlluminationModeEnum, result.GeneratePrescanAODWaveformParam.ProductivityInformation, result.PrescanAODWaveformResultFilePath);

        Logger.LogHtmlInformation($"{result.GeneratePrescanAODWaveformParam.OpticsIlluminationModeEnum}-{result.GeneratePrescanAODWaveformParam.ProductivityInformation}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(result.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            result.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. result.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }
}