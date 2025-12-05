using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

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

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = OpticsIlluminationModeEnum.OI;
}

public abstract partial class AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile> : ViewModelBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    protected readonly ILogger<AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;

    [ObservableProperty]
    private GenerateAODWaveformCache<TParam, TProfile> _cache = new();

    public abstract string Name { get; }

    protected abstract void GenerateAODWaveform(CancellationToken cancellationToken);

    protected abstract void SetAODWaveformProfiles();

    protected AbstractGenerateAODWaveformWindowViewModel()
    {
        Logger = (ILogger<AbstractGenerateAODWaveformWindowViewModel<TParam, TProfile>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
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
                GenerateAODWaveform(cancellationToken);

                DialogWindowProvider.ShowDialog($"{Name}: Generate Success");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Generate Failed", Name);
                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: Generate Failed
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
                SetAODWaveformProfiles();

                DialogWindowProvider.ShowDialog($"{Name}: Set Success");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Set Failed", Name);
                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: Set Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

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
}