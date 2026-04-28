using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.AutoFocus.CalChipFocusOffset;
using Core.Models.Models.AutoFocus.DarkAutoFocus;
using Core.Models.Models.AutoFocus.GlobalFocusOffset;
using Core.Models.Models.Chuck.AlignmentDegreeOffset;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.AGCDelay;
using Core.Models.Models.CIB.IlluminationProfile;
using Core.Models.Models.CIB.LightMatching;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.CIB.LineOrientationOffset;
using Core.Models.Models.CIB.MMD;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.XTC;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Fourier.CameraAlignment;
using Core.Models.Models.Fourier.CenterChannelFlexibleAperture;
using Core.Models.Models.Fourier.CenterChannelSpecularBlocker;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Core.Models.Models.Fourier.SideChannelSpecularBlocker;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Optics.GlobalFieldTilt;
using Core.Models.Models.Optics.INC;
using Core.Models.Models.Optics.Relay;
using Core.Models.Models.Optics.SC;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Ads;
using CugaCalibration.ViewModels.AOD;
using CugaCalibration.ViewModels.AutoFocus;
using CugaCalibration.ViewModels.Chuck;
using CugaCalibration.ViewModels.CIB;
using CugaCalibration.ViewModels.Common;
using CugaCalibration.ViewModels.Common.Windows.File.Save;
using CugaCalibration.ViewModels.Common.Windows.Management.Recipe.Management;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using CugaCalibration.ViewModels.Common.Windows.View;
using CugaCalibration.ViewModels.Flourier;
using CugaCalibration.ViewModels.Laser;
using CugaCalibration.ViewModels.Microscope;
using CugaCalibration.ViewModels.Optics;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(MainWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MainWindowViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>, IRecipient<PopupWindowEvent>
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IMessenger _messenger;
    private readonly ICalibrationCacheProvider _calibrationCacheProviderService;
    private readonly IApplicationCookieService _applicationCookieService;

    #region 界面显示属性

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial ApplicationCookie ApplicationCookie { get; set; }

    [ObservableProperty]
    public partial RecipeCookie RecipeCookie { get; set; }

    [ObservableProperty]
    public partial StatusViewModel StatusViewModel { get; set; }

    [ObservableProperty]
    public partial CalibrationViewModelBase? ActiveItem { get; set; }

    [ObservableProperty]
    public partial CalibrationSetting CalibrationSetting { get; set; }

    #endregion 界面显示属性

    #region 控制按钮

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalibrateCommand))]
    public partial bool IsCalibrateEnable { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReviewCommand))]
    public partial bool IsReviewEnable { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsCancelEnable { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    public partial bool IsPreviousEnable { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    public partial bool IsNextEnable { get; set; }

    [ObservableProperty]
    public partial bool IsEnable { get; set; } = true;

    #endregion 控制按钮

    public MainWindowViewModel(
        IMessenger messenger,
        ILogger<MainWindowViewModel> logger,
        ISynchronizationContextProvider contextProvider,
        ICacheProvider cacheProvider,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider recipeCacheProvider,
        IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        ICalibrationCacheProvider calibrationCacheProviderService,
        CalibrationSetting calibrationSetting,
        ApplicationCookie applicationCookie,
        RecipeCookie recipeCookie,
        StatusViewModel statusViewModel,
        IApplicationCookieService applicationCookieService)
    {
        _messenger = messenger;
        _logger = logger;
        _contextProvider = contextProvider;
        _cacheProvider = cacheProvider;
        _recipeCacheProvider = recipeCacheProvider;
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _calibrationCacheProviderService = calibrationCacheProviderService;
        CalibrationSetting = calibrationSetting;
        ApplicationCookie = applicationCookie;
        RecipeCookie = recipeCookie;
        StatusViewModel = statusViewModel;
        _applicationCookieService = applicationCookieService;
        Title = applicationCookie.Title;
        _messenger.RegisterAll(this);
    }

    #region Command

    [RelayCommand]
    private void Loaded()
    {
        var isLoadingOk = false;
        try
        {
            var showDialog = _windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoginWindowViewModel>());
            if (showDialog == false)
            {
                return;
            }

            showDialog = _windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoadingWindowViewModel>());
            if (showDialog == false)
            {
                return;
            }

            var recipeManagementViewModel = HostApplication.GetRequiredService<RecipeManagementViewModel>();
            recipeManagementViewModel.IsLoading = true;
            showDialog = _windowManagerService.ShowDialog(recipeManagementViewModel);
            if (showDialog == false)
            {
                return;
            }

            Title = ApplicationCookie.Title;

            _windowManagerService.ShowWindow(HostApplication.GetRequiredService<StageWindowViewModel>());
            _windowManagerService.ShowWindow(HostApplication.GetRequiredService<MicroscopeWindowViewModel>());

            isLoadingOk = true;
        }
        finally
        {
            if (isLoadingOk) LoadCalibrationStatus();
            StatusViewModel.Monitor(isLoadingOk);
        }
    }

    [RelayCommand]
    private void Closed()
    {
#pragma warning disable IDISP007

        StatusViewModel.Dispose();
        _messenger.UnregisterAll(this);
        _recipeCacheProvider.Dispose();
        _cacheProvider.Dispose();

#pragma warning restore IDISP007
    }

    [RelayCommand(CanExecute = nameof(IsCalibrateEnable))]
    private Task CalibrateAsync()
    {
        return ActiveItem?.CalibrateAsync() ?? Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(IsReviewEnable))]
    private Task ReviewAsync()
    {
        return ActiveItem?.ReviewAsync() ?? Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(IsCancelEnable))]
    private async Task CancelAsync()
    {
        var completedTask = ActiveItem?.CancelAsync() ?? Task.CompletedTask;
        await completedTask.ConfigureAwait(false);

        LoadCalibrationStatus();
        if (ActiveItem is not null) ActiveItem = null;
        Title = ApplicationCookie.Title;
    }

    [RelayCommand(CanExecute = nameof(IsPreviousEnable))]
    private Task PreviousAsync()
    {
        return ActiveItem?.PreviousAsync() ?? Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(IsNextEnable))]
    private Task NextAsync()
    {
        return ActiveItem?.NextAsync() ?? Task.CompletedTask;
    }

    [RelayCommand]
    private void OpenCalibration(string viewModel)
    {
        try
        {
            if (ActiveItem is not null && ActiveItem?.GetType().FullName != viewModel)
            {
                _dialogWindowProvider.ShowDialog("Calibration, cannot be switched", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (ActiveItem?.GetType().FullName == viewModel) return;
            // 获取menu中选择的校准大类的服务
            var abstractCalibrationViewModel = HostApplication.GetRequiredService<CalibrationViewModelBase>(viewModel);
            if (abstractCalibrationViewModel is not null)
            {
                ActiveItem = abstractCalibrationViewModel;
                Title = $"{ApplicationCookie.Title} {ActiveItem.Name}";
            }
            else
            {
                ActiveItem = null;
                Title = ApplicationCookie.Title;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Load Calibration View({@ViewModel}) Failed", nameof(MainWindowViewModel), viewModel);
        }
        finally
        {
            LoadCalibrationStatus();
        }
    }

    [RelayCommand]
    private async Task OpenToolMenuAsync(SysMenuDTO sysMenu)
    {
        try
        {
            var viewModel = sysMenu.Component;
            if (string.IsNullOrWhiteSpace(viewModel)) return;

            var viewModelBase = HostApplication.GetRequiredService<ViewModelBase>(viewModel);
            if (viewModelBase is null) return;
            switch (viewModelBase)
            {
                case MainWindowViewModel:
                    switch (sysMenu.Name)
                    {
                        case CalibrationConstantsHelper.Export:
                            if (_dialogWindowProvider.TryShowSaveFilePathDialog(".json", out var exportPath) == true)
                            {
                                var (isSuccess, message) = await _calibrationCacheProviderService.TryExportAsync(exportPath, CancellationToken.None);
                                if (isSuccess)
                                    _dialogWindowProvider.ShowDialog(message);
                                else
                                    _dialogWindowProvider.ShowDialog(message, DialogButtonsEnum.OK, DialogIconEnum.Error);
                            }

                            break;

                        case CalibrationConstantsHelper.Import:
                            if (_dialogWindowProvider.TryShowSelectFilePathDialog(".json", out var importPath) == true)
                            {
                                var (isSuccess, message) = await _calibrationCacheProviderService.TryImportAsync(importPath, CancellationToken.None);

                                if (isSuccess)
                                    _dialogWindowProvider.ShowDialog(message);
                                else
                                    _dialogWindowProvider.ShowDialog(message, DialogButtonsEnum.OK, DialogIconEnum.Error);
                            }

                            LoadCalibrationStatus();

                            break;
                    }

                    break;
                case SaveFileWindowViewModel saveFileWindowViewModel:
                    _windowManagerService.ShowDialog(saveFileWindowViewModel);

                    break;

                case PopupWindowViewModelBase popupWindowViewModel:
                    if (popupWindowViewModel.Show() == false) _windowManagerService.ShowWindow(popupWindowViewModel);

                    break;

                case AlignmentWindowBrightFieldViewModel alignmentWindowViewModel:
                    alignmentWindowViewModel.IsShowAlign = true;
                    _windowManagerService.ShowDialog(alignmentWindowViewModel);
                    break;

                case CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel:
                    var dialog = _dialogWindowProvider.TryShowSelectFilePathDialog(".jpg", out var filePath);
                    if (dialog == false) return;

                    createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                    createDarkImageTemplateWindowViewModel.TemplateFilePath = $"{FileHelper.GetFileFullName(filePath)}_Template";
                    _windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                    break;

                case ApplicationAboutWindowViewModel applicationAboutWindowViewModel:
                    _windowManagerService.ShowDialog(applicationAboutWindowViewModel);
                    break;

                case RecipeManagementViewModel recipeManagementViewModel:
                    recipeManagementViewModel.IsLoading = false;
                    _windowManagerService.ShowDialog(recipeManagementViewModel);
                    break;

                default:
                    _windowManagerService.ShowDialog(viewModelBase);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Load Menu View({@ViewModel}) Failed", nameof(MainWindowViewModel), sysMenu.Name);
        }
    }

    [RelayCommand]
    private void ShowLog()
    {
        var logWindowViewModel = HostApplication.GetRequiredService<LogWindowViewModel>();
        if (logWindowViewModel.Show() == false) _windowManagerService.ShowWindow(logWindowViewModel);
    }

    #endregion Command

    public void Receive(ValueChangedMessage<ToggleCalibrateEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsRefreshWindow.HasValue)
            {
                if (message.Value.IsRefreshWindow.Value == false) return;
                OnPropertyChanged(nameof(ApplicationCookie));
                LoadCalibrationStatus();
            }

            if (message.Value.IsCalibrateEnable.HasValue)
                IsCalibrateEnable = message.Value.IsCalibrateEnable.Value;
            if (message.Value.IsReviewEnable.HasValue)
                IsReviewEnable = message.Value.IsReviewEnable.Value;
            if (message.Value.IsCancelEnable.HasValue)
                IsCancelEnable = message.Value.IsCancelEnable.Value;
            if (message.Value.IsPreviousEnable.HasValue)
                IsPreviousEnable = message.Value.IsPreviousEnable.Value;
            if (message.Value.IsNextEnable.HasValue)
                IsNextEnable = message.Value.IsNextEnable.Value;
        });
    }

    public void Receive(PopupWindowEvent message)
    {
        IsEnable = message.IsPopupWindowEnable;
    }

    private void LoadCalibrationStatus()
    {
        _ = Task.Run(() =>
        {
            try
            {
                var calibrationSetting = _cacheProvider.GetOrDefault<CalibrationSetting>();
                CalibrationSetting.AdaptIn(calibrationSetting);

                var calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopeFocusCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<MicroscopeFocusItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopeCalChipViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<MicroscopeCalChipDTO>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopePixelSizeCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopeCentricityCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<MicroscopeCentricityItemDto>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckGantryCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckGantryDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckCenterAndThetaCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckCenterAndThetaItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckPrealignerCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckPrealignerDTO>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckStageMapCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckStageMapDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckGlobalScaleErrorCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckGlobalScaleErrorDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckAlignmentDegreeOffsetCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<ChuckAlignmentDegreeOffsetItemDto>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AdsPressureGainsCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AdsPressureGainsDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<AdsXGainsCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AdsXGainsItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<AdsYGainsCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AdsYGainsItemDto>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AutoFocusDarkAutoFocusViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<DarkAutoFocusDTO>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserBeamStabilizerCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<LaserBeamStabilizerObjDto>().IsOk(out _);

                #region NEW

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBMMDViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBMMDDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBLightMatchingViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBLightMatchingDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<OpticsRelayViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<OpticsRelayDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<OpticsGlobalFieldTiltViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<GlobalFieldTiltDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBYPixelSizeViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBYPixelSizeDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBXPixelSizeViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBXPixelSizeDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBLineCentricityViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBLineCentricityDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBLineOrientationOffsetViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBLineOrientationOffsetDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AODAlignmentViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<AODAlignmentDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AODDelayViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<AODDelayDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserOpticalPowerMeterViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserOpticalPowerMeterDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserAttenuatorViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserAttenuatorDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBXTCViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBXTCDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBAGCDelayViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBAGCDelayDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<CIBIlluminationProfileViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<CIBIlluminationProfileDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AODUniformityViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<AODUniformityDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<OpticsINCViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<OpticsINCDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<OpticsSCViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<OpticsSCDTO>().IsOk(out _);

                #endregion

                calibrationItem = _applicationCookieService.FindCalibrationItem<AutoFocusGlobalFocusOffsetViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<AutoFocusGlobalFocusOffsetDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AutoFocusCalChipFocusOffsetViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AutoFocusCalChipFocusOffsetDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<PupilCameraAlignmentViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<PupilCameraAlignmentDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<PupilSideChannelFlexibleApertureViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<PupilSideChannelFlexibleApertureDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<PupilSideChannelSpecularBlockerViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<PupilSideChannelSpecularBlockerDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<PupilCenterChannelFlexibleApertureViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<PupilCenterChannelFlexibleApertureDTO>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<PupilCenterChannelSpecularBlockerViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<PupilCenterChannelSpecularBlockerDTO>().IsOk(out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{@Name}: Load Calibration Status Failed", nameof(MainWindowViewModel));
            }
        });
    }
}