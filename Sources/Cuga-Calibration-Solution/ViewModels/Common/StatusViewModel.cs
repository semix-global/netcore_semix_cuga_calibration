using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using R3;

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
    private readonly ManualResetEventSlim _manualResetEvent = new(false);
    private long _frameCount;
    public int IsSwitchMicroscopeLensInformationRunning;
    private IDisposable? _disposable0;
    private IDisposable? _disposable1;

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

    public void Monitor(bool isEnable)
    {
        _disposable0?.Dispose();
        _disposable1?.Dispose();

        if (isEnable == false) return;

        IsEnable = true;

#pragma warning disable IDISP003

        _disposable0 = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.MonitorMilliseconds))
            .Subscribe(_ =>
            {
                try
                {
                    if (IsEnable == false)
                    {
                        _manualResetEvent.Reset();

                        if (dialogWindowProvider.TryShowDialog("The current cache of cuga has expired. Please refresh the cookie and restart the current calibration item!", out var _, DialogButtonsEnum.OK, DialogIconEnum.Error) != true) return;

                        _manualResetEvent.Wait();
                    }

                    BrightFieldPosition = stageViewModel.GetBrightFieldStagePosition();
                    DarkFieldPosition = stageViewModel.GetDarkFieldStagePosition();
                    MachinePosition = stageViewModel.GetMachineStagePosition();
                    MachineTheta = stageViewModel.GetMachineStageTheta();
                    if (IsSwitchMicroscopeLensInformationRunning == 0) MicroscopeLensInformation = microscopeViewModel.GetCurrentMicroscopeLensInformation();

                    try
                    {
                        BitmapMemoryByteArray = reviewViewModel.GetBrightFieldImageMemoryByteArray();

                        _frameCount++;
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Get Status Failed!");
                    }

                    IsEnable = applicationCookie.CIBInformations.SequenceEqual(cibViewModel.GetCIBInformations());
                }
                catch (Exception ex)
                {
                    IsEnable = false;
                    logger.LogWarning(ex, "Get Status Failed!");
                }
            });

        _disposable1 = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.MonitorMilliseconds)).Subscribe(_ =>
        {
            Fps = _frameCount / (CalibrationConstantsHelper.MonitorMilliseconds / 1000d);
            _frameCount = 0;
        });

#pragma warning restore IDISP003
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

            applicationCookie.DeviceCode = deviceCode;
            applicationCookie.DeviceCUGAVersion = deviceCUGAVersion;
            applicationCookie.MicroscopeLensInformations = [.. microscopeLensInformations.Select(t => t.Clone())];
            applicationCookie.LaserLightInformations = [.. laserLightInformations.Select(t => t.Clone())];
            applicationCookie.ProductivityInformations = [.. productivityInformations.Select(t => t.Clone())];
            applicationCookie.CIBInformations = [.. cibInformations.Select(t => t.Clone())];
            applicationCookie.HardwareStateConfig = configViewModel.GetHardwareConfigs();

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
        _manualResetEvent.Dispose();
        _disposable0?.Dispose();
        _disposable1?.Dispose();
    }
}