using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(MicroscopeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeWindowViewModel(
    StatusViewModel statusViewModel,
    MicroscopeViewModel microscopeViewModel,
    IMessenger messenger,
    ILogger<MicroscopeWindowViewModel> logger,
    ApplicationCookie applicationCookie) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    public partial StatusViewModel StatusViewModel { get; set; } = statusViewModel;

    [ObservableProperty]
    public partial ApplicationCookie ApplicationCookie { get; set; } = applicationCookie;

    protected override void Loadeding(CancellationToken cancellationToken)
    {
    }

    [RelayCommand]
    private async Task SwitchMagnificationAsync(MicroscopeLensInformation microscopeLensInformation)
    {
        await Task.Run(() =>
        {
            if (Interlocked.CompareExchange(ref StatusViewModel.IsSwitchMicroscopeLensInformationRunning, 1, 0) == 1) throw new InvalidOperationException("Task is already running");

            try
            {
                StatusViewModel.MicroscopeLensInformation = microscopeLensInformation;

                microscopeViewModel.SwitchMicroscopeLensInformation(microscopeLensInformation, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Switch magnification Failed");
            }
            finally
            {
                Interlocked.Exchange(ref StatusViewModel.IsSwitchMicroscopeLensInformationRunning, 0);
            }
        }).ConfigureAwait(false);
    }
}