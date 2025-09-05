using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(GeneratePrescanAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class GeneratePrescanAODWaveformWindowViewModel(
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    LaserViewModel laserViewModel,
    ILogger<GeneratePrescanAODWaveformWindowViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _param = new();

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _profiles = [];

    [ObservableProperty]
    private string _resultFilePath = string.Empty;

    [RelayCommand]
    public void Loaded() => Param = cacheProvider.GetOrDefault<GeneratePrescanAODWaveformParam>();

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GeneratePrescanAODWaveformAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                ResultFilePath = string.Empty;
                Profiles = [];

                var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Param.AdaptTo(), cancellationToken);

                ResultFilePath = aodWaveformResult.FilePath;
                Profiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);

                if (aodWaveformResult.IsSuccess) dialogWindowProvider.ShowDialog("Generate Prescan AOD Waveform Success!");
                else throw GuardUtils.IsNotNullAndReturn(exception);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@Name}: Generate Prescan AOD Waveform", nameof(GeneratePrescanAODWaveformWindowViewModel));
                dialogWindowProvider.ShowDialog($"""
                                                 Generate Prescan AOD Waveform Failed!
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SetPrescanAODWaveformAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (Profiles.Count <= 0)
                {
                    dialogWindowProvider.ShowDialog("Prescan AOD Waveform Is Empty");
                    return;
                }

                laserViewModel.SetPrescanAODWaveProfiles(Profiles);

                dialogWindowProvider.ShowDialog("Send Prescan AOD Waveform Success!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@Name}: Set Prescan AOD Waveform", nameof(GeneratePrescanAODWaveformWindowViewModel));
                dialogWindowProvider.ShowDialog($"""
                                                 Set Prescan AOD Waveform Failed!
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        if (cacheProvider.Set(Param, cancellationTokenSource.Token) == false)
            logger.LogWarning("{@Name}: Save Generate Prescan AOD Waveform Param Failed", nameof(GeneratePrescanAODWaveformWindowViewModel));

        CloseView(true);
    }
}