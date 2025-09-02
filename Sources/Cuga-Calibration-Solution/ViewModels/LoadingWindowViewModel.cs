using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Extensions;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.Microscope.Enums;
using CugaCalibration.ViewModels.Common;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Mapper;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(LoadingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LoadingWindowViewModel(
    StageViewModel stageViewModel,
    ReviewViewModel reviewViewModel,
    MicroscopeViewModel microscopeViewModel,
    AfViewModel afViewModel,
    AdsViewModel adsViewModel,
    LaserViewModel laserViewModel,
    EFEMViewModel efemViewModel,
    ConfigViewModel configViewModel,
    MonitorViewModel monitorViewModel,
    ILogger<LoadingWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider,
    ApplicationCookie applicationCookie,
    string applicationName) : ViewModelBase
{
    private const int ConnectCount = 8;

    [ObservableProperty]
    private string _title = applicationName;

    [ObservableProperty]
    private string _message = "Please Wait, Connecting ...";

    [ObservableProperty]
    private double _processValue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    private bool _isCanClose;

    [ObservableProperty]
    private bool _isFailed;

    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            if (await ConnectAsync(afViewModel.Connect, "Connecting Auto Focus Wcf Service", 1).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(microscopeViewModel.Connect, "Connecting Microscope Wcf Service", 2).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(reviewViewModel.Connect, "Connecting Review Wcf Service", 3).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(stageViewModel.Connect, "Connecting Stage Wcf Service", 4).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(adsViewModel.Connect, "Connecting Ads Wcf Service", 5).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(laserViewModel.Connect, "Connecting Laser Wcf Service", 6).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(efemViewModel.Connect, "Connecting EFEM Wcf Service", 7).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(configViewModel.Connect, "Connecting Configure Wcf Service", 8).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(monitorViewModel.Connect, "Connecting Monitor Wcf Service", 9).ConfigureAwait(false) == false) return;

            CustomerAdaptToMapper.RegisterType<MicroscopeLensInformation, CgMicroscopeLens>(
                microscopeLensInformation => microscopeLensInformation.AdaptTo().LensCode,
                microscopeViewModel.CgMicroscopeLensToMicroscopeLensInfo
            );

            Message = "Connected OK!!!";

            var microscopeLensInformationList = microscopeViewModel.GetMicroscopeLensInformationList();
            var laserLightInformationList = laserViewModel.GetLaserLightInformationList();

            applicationCookie.MicroscopeLensInformationList = [.. microscopeLensInformationList.Select(t => t.Clone())];
            applicationCookie.LaserLightInformationList = laserLightInformationList;

            CoreWcfModelsExtension.Initialize(() => applicationCookie.MicroscopeLensInformationList);

            contextProvider.Send(() => CloseView(true));

            await Task.Delay(300).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            IsFailed = true;
            Message = $"Connecting Failed: {ex}";
            logger.LogError(ex, "{@Name}: Connecting Failed", nameof(LoadingWindowViewModel));
            dialogWindowProvider.ShowDialog($"Connecting Failed: {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            contextProvider.Send(() => IsCanClose = true);
        }

        return;

        async Task<bool> ConnectAsync(Func<bool> func, string title, int index)
        {
            Message = $"{title} ...";
            var stageConnectResult = await Task.Run(func).ConfigureAwait(false);
            if (stageConnectResult == false)
            {
                IsFailed = true;
                Message = $"{title} Failed.";
            }

            ProcessValue = 100d / ConnectCount * index;
            await Task.Delay(300).ConfigureAwait(false);

            return stageConnectResult;
        }
    }

    [RelayCommand(CanExecute = nameof(IsCanClose))]
    private void Close()
    {
        CloseView(false);
    }
}