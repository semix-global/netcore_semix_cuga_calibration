using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class GenerateAODWaveformCache<TParam, TProfile> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    [ObservableProperty]
    private TParam _param = new();

    [ObservableProperty]
    private IReadOnlyList<TProfile> _profiles = [];

    [ObservableProperty]
    private string _aODWaveformResultFilePath = string.Empty;
}

public abstract partial class AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile> : AbstractAODWaveformCommonViewModel<TParam, TProfile>
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    protected readonly ILogger<AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;

    [ObservableProperty]
    private GenerateAODWaveformCache<TParam, TProfile> _cache = new();

    protected AbstractGenerateAODWaveformWindowViewModel()
    {
        Logger = (ILogger<AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    }

    [RelayCommand]
    private void Loaded() => Cache = CacheProvider.GetOrDefault<GenerateAODWaveformCache<TParam, TProfile>>();

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAODWaveformAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                Cache = new GenerateAODWaveformCache<TParam, TProfile>
                {
                    Param = Cache.Param
                };

                var (isSuccess, result, resultFilePath, exception) = GenerateAODWaveform(Cache.Param, cancellationToken);

                Cache.Profiles = result;
                Cache.AODWaveformResultFilePath = resultFilePath;

                if (isSuccess) DialogWindowProvider.ShowDialog($"Generate {AODWaveformName} AOD Waveform Success!");
                else throw GuardUtils.IsNotNullAndReturn(exception);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Generate {@AODWaveformName} AOD Waveform", nameof(AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>), AODWaveformName);
                DialogWindowProvider.ShowDialog($"""
                                                 Generate {AODWaveformName} AOD Waveform Failed!
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SetAODWaveformAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (Cache.Profiles.Count <= 0)
                {
                    DialogWindowProvider.ShowDialog($"{AODWaveformName} AOD Waveform Is Empty");
                    return;
                }

                SetAODWaveProfiles(Cache.Param, Cache.Profiles);

                DialogWindowProvider.ShowDialog($"Send {AODWaveformName} AOD Waveform Success!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Set {@AODWaveformName} AOD Waveform", nameof(AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>), AODWaveformName);
                DialogWindowProvider.ShowDialog($"""
                                                 Set {AODWaveformName} AOD Waveform Failed!
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        if (CacheProvider.Set(Cache, cancellationTokenSource.Token) == false)
            Logger.LogWarning("{@Name}: Save Generate {@AODWaveformName} AOD Waveform Param Failed", nameof(AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>), AODWaveformName);

        CloseView(true);
    }
}