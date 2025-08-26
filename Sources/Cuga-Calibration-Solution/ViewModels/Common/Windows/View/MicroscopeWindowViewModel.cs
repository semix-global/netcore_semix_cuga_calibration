using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Pattern;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Reactive.Linq;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(MicroscopeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeWindowViewModel(
    MicroscopeViewModel microscopeViewModel,
    IMessenger messenger,
    ILogger<MicroscopeWindowViewModel> logger,
    ApplicationCookie applicationCookie) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    private ApplicationCookie _applicationCookie = applicationCookie;

    /// <summary>
    /// 1: Running, 0: Not running
    /// </summary>
    private int _isRunning;

    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    protected override void Loadeding(CancellationToken cancellationToken)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001
        var subscribe = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.MonitorMicroscopeMilliseconds)).Subscribe(_ =>
        {
            try
            {
                if (_isRunning == 1) return;
                var result = microscopeViewModel.GetMagnification();
                MicroscopeMagnificationInfo = result;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Get Magnification Failed!");
            }
        });
        cancellationToken.Register(subscribe.Dispose);
#pragma warning restore IDISP001
#pragma warning restore IDE0079
    }

    [RelayCommand]
    private async Task SwitchMagnificationAsync()
    {
        await Task.Run(() =>
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1) throw new InvalidOperationException("Task is already running");

            try
            {
                microscopeViewModel.SwitchMagnification(MicroscopeMagnificationInfo, true);
                var result = microscopeViewModel.GetMagnification();
                MicroscopeMagnificationInfo = result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@Name}: Switch magnification failed", nameof(MicroscopeWindowViewModel));
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }).ConfigureAwait(false);
    }
}