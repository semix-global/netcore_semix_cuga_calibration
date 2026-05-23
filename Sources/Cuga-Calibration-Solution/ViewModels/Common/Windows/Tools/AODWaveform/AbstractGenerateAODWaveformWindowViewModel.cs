using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class GenerateAODWaveformCache<TParam, TProfile> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    [ObservableProperty]
    public partial TParam Param { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<TProfile> Profiles { get; set; } = [];

    [ObservableProperty]
    public partial string AODWaveformResultFilePath { get; set; } = string.Empty;
}

public abstract partial class AbstractGenerateAODWaveformWindowViewModel<TCache, TParam, TProfile> : ViewModelBase
    where TCache : GenerateAODWaveformCache<TParam, TProfile>, new()
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    protected readonly ILogger<AbstractGenerateAODWaveformWindowViewModel<TCache, TParam, TProfile>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;

    public abstract TCache Cache { get; set; }

    public abstract string Name { get; }

    [RelayCommand]
    protected abstract void ImportAODWaveformParams();

    protected abstract void GenerateAODWaveform(CancellationToken cancellationToken);

    protected abstract void SetAODWaveformProfiles(CancellationToken cancellationToken);

    protected AbstractGenerateAODWaveformWindowViewModel()
    {
        Logger = (ILogger<AbstractGenerateAODWaveformWindowViewModel<TCache, TParam, TProfile>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
    }

    [RelayCommand]
    private async Task LoadedAsync() => await Task.Run(() => Cache = CacheProvider.GetOrDefault<TCache>());

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAODWaveformAsync(CancellationToken cancellationToken) => await InvokeAsync(() => GenerateAODWaveform(cancellationToken), "Generate AOD Waveform Profiles");

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SetAODWaveformAsync(CancellationToken cancellationToken) => await InvokeAsync(() => SetAODWaveformProfiles(cancellationToken), "Set AOD Waveform Profiles");

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            CacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save cache");
        }

        CloseView(true);
    }

    private async Task InvokeAsync(Action action, string actionName)
    {
        await Task.Run(() =>
        {
            try
            {
                action();

                DialogWindowProvider.ShowDialog($"{Name}: {actionName} Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: {actionName} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: {actionName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogError(ex, "{@ActionName} Failed", actionName);
            }
        }).ConfigureAwait(false);
    }
}