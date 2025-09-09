using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractGenerateAODWaveformWindowViewModel<TResult, TParam> : ViewModelBase
    where TResult : AbstractAODWaveformProfile
    where TParam : AbstractGenerateAODWaveformParam, new()
{
    protected readonly ILogger<AbstractGenerateAODWaveformWindowViewModel<TResult, TParam>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;

    [ObservableProperty]
    private IReadOnlyList<TResult> _profiles = [];

    [ObservableProperty]
    private string _resultFilePath = string.Empty;

    [ObservableProperty]
    private TParam _param = new();

    protected abstract string AODWaveformName { get; }

    protected AbstractGenerateAODWaveformWindowViewModel()
    {
        Logger = (ILogger<AbstractGenerateAODWaveformWindowViewModel<TResult, TParam>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    }

    [RelayCommand]
    public void Loaded() => Param = CacheProvider.GetOrDefault<TParam>();

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAODWaveformAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                ResultFilePath = string.Empty;
                Profiles = [];

                var (isSuccess, result, resultFilePath, exception) = GenerateAODWaveform(cancellationToken);

                Profiles = result;
                ResultFilePath = resultFilePath;

                if (isSuccess) DialogWindowProvider.ShowDialog($"Generate {AODWaveformName} AOD Waveform Success!");
                else throw GuardUtils.IsNotNullAndReturn(exception);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Generate {@AODWaveformName} AOD Waveform", nameof(AbstractGenerateAODWaveformWindowViewModel<TResult, TParam>), AODWaveformName);
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
                if (Profiles.Count <= 0)
                {
                    DialogWindowProvider.ShowDialog($"{AODWaveformName} AOD Waveform Is Empty");
                    return;
                }

                SetAODWaveProfiles(Profiles);

                DialogWindowProvider.ShowDialog($"Send {AODWaveformName} AOD Waveform Success!");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Set {@AODWaveformName} AOD Waveform", nameof(AbstractGenerateAODWaveformWindowViewModel<TResult, TParam>), AODWaveformName);
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

        if (CacheProvider.Set(Param, cancellationTokenSource.Token) == false)
            Logger.LogWarning("{@Name}: Save Generate {@AODWaveformName} AOD Waveform Param Failed", nameof(AbstractGenerateAODWaveformWindowViewModel<TResult, TParam>), AODWaveformName);

        CloseView(true);
    }

    protected abstract (bool IsSuccess, IReadOnlyList<TResult> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(CancellationToken cancellationToken);

    protected abstract void SetAODWaveProfiles(IReadOnlyList<TResult> profiles);
}