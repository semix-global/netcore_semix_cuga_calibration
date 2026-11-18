using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Diagnostics.CodeAnalysis;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(StatusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StatusViewModel(
    ConfigViewModel configViewModel,
    LaserViewModel laserViewModel,
    OpticsViewModel opticsViewModel,
    StageViewModel stageViewModel,
    MicroscopeViewModel microscopeViewModel,
    ReviewViewModel reviewViewModel,
    CIBViewModel cibViewModel,
    ApplicationCookie applicationCookie,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<StatusViewModel> logger) : ViewModelBase, IDisposable
{
    public const int MonitorMilliseconds = 500;

    private readonly ManualResetEventSlim _manualResetEvent = new(false);

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _disposable;

    public int IsSwitchMicroscopeLensInformationRunning;

    [ObservableProperty]
    public partial Point BrightFieldPosition { get; set; }

    [ObservableProperty]
    public partial Point DarkFieldPosition { get; set; }

    [ObservableProperty]
    public partial Point MachinePosition { get; set; }

    [ObservableProperty]
    public partial double MachineTheta { get; set; }

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial byte[] BitmapMemoryByteArray { get; set; } = [];

    [ObservableProperty]
    public partial double Fps { get; set; }

    [ObservableProperty]
    public partial bool IsEnable { get; private set; }

    private void CancelToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _disposable?.Wait();
        _disposable?.Dispose();
        _disposable = null;
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void RefreshToken()
    {
        CancelToken();

#pragma warning disable IDE0079
#pragma warning disable IDISP003

        _cancellationTokenSource = new CancellationTokenSource();

#pragma warning restore IDISP003
#pragma warning restore IDE0079
    }

    public void Monitor(bool isEnable)
    {
        RefreshToken();

        if (isEnable == false) return;

        IsEnable = true;

        var token = _cancellationTokenSource.Token;

        _disposable = Task.Factory.StartNew(() =>
        {
            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    Thread.Sleep(MonitorMilliseconds);

                    if (IsEnable == false)
                    {
                        _manualResetEvent.Reset();

                        dialogWindowProvider.ShowDialog("The current cache of cuga has expired. Please refresh the cookie and restart the current calibration item!", DialogButtonsEnum.OK, DialogIconEnum.Error);

                        _manualResetEvent.Wait(token);
                    }

                    BrightFieldPosition = stageViewModel.GetBrightFieldStagePosition();
                    DarkFieldPosition = stageViewModel.GetDarkFieldStagePosition();
                    MachinePosition = stageViewModel.GetMachineStagePosition();
                    MachineTheta = stageViewModel.GetMachineStageTheta();
                    if (IsSwitchMicroscopeLensInformationRunning == 0) MicroscopeLensInformation = microscopeViewModel.GetCurrentMicroscopeLensInformation();

                    try
                    {
                        BitmapMemoryByteArray = reviewViewModel.GetBrightFieldImageMemoryByteArray();

                        Fps = 1d / (MonitorMilliseconds / 1000d);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Get Status Failed");
                    }

                    IsEnable = applicationCookie.CIBInformations.SequenceEqual(cibViewModel.GetCIBInformations());
                }
                catch (Exception ex)
                {
                    Fps = 0d;

                    if (ex is OperationCanceledException) return;

                    IsEnable = false;
                    logger.LogWarning(ex, "Get Status Failed");
                }
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    [RelayCommand]
    public Task<bool> RefreshCookieAsync(bool isSilent) => Task.Run(() =>
    {
        try
        {
            var deviceCode = configViewModel.GetDeviceCode();
            var deviceCUGAVersion = configViewModel.GetDeviceCUGAVersion();
            var microscopeLensInformations = microscopeViewModel.GetMicroscopeLensInformations();
            var laserLightInformations = laserViewModel.GetLaserLightInformations();
            var productivityInformations = opticsViewModel.GetProductivityInformations();

            var cibInformations = cibViewModel.GetCIBInformations();
            var pmtInterval = opticsViewModel.GetPMTInterval();

            applicationCookie.DeviceCode = deviceCode;
            applicationCookie.DeviceCUGAVersion = deviceCUGAVersion;
            applicationCookie.MicroscopeLensInformations = [.. microscopeLensInformations.Select(t => t.Clone())];
            applicationCookie.BFCenterMachinePosition = stageViewModel.BrightFieldToMachinePosition(Point.Origin);
            applicationCookie.DFCenterMachinePosition = stageViewModel.DarkFieldToMachinePosition(Point.Origin);
            applicationCookie.LaserLightInformations = [.. laserLightInformations.Select(t => t.Clone())];
            applicationCookie.ProductivityInformations = [.. productivityInformations.Select(t => t.Clone())];
            applicationCookie.CIBInformations = [.. cibInformations.Select(t => t.Clone())];
            applicationCookie.HardwareStateConfig = configViewModel.GetHardwareConfigs();
            applicationCookie.PMTInterval = pmtInterval;

            if (new Version(applicationCookie.DeviceCUGAVersion) < new Version(ApplicationCookie.ApplicationCUGAVersion))
            {
                dialogWindowProvider.ShowDialog($"Refresh Cookie Failed: The current CUGA version: {applicationCookie.DeviceCUGAVersion} < The CUGA version of the application: {ApplicationCookie.ApplicationCUGAVersion}. Please update the device CUGA version!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return false;
            }

            Guard.IsNotNullOrWhiteSpace(applicationCookie.DeviceCode);
            Guard.IsNotEmpty(applicationCookie.MicroscopeLensInformations);
            Guard.IsNotEmpty(applicationCookie.LaserLightInformations);
            Guard.IsNotEmpty(applicationCookie.ProductivityInformations);
            Guard.IsNotEmpty(applicationCookie.CIBInformations);

            _manualResetEvent.Set();

            if (isSilent == false) dialogWindowProvider.ShowDialog("Refresh Cookie Ok!");

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Refresh Cookie Failed");
            dialogWindowProvider.ShowDialog($"Refresh Cookie Failed: {ex}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }

        return false;
    });

    public void Dispose()
    {
        CancelToken();

        _manualResetEvent.Dispose();
    }
}