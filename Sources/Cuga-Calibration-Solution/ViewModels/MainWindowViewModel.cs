using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.DOEAngle;
using Core.Models.Models.Laser.FocusShift;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.LineOrientationOffset;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PmtAgcDelay;
using Core.Models.Models.Laser.Rtfc;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Ads;
using CugaCalibration.ViewModels.AOD;
using CugaCalibration.ViewModels.Chuck;
using CugaCalibration.ViewModels.Common.Windows.Management.Recipe;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using CugaCalibration.ViewModels.Common.Windows.View;
using CugaCalibration.ViewModels.Laser;
using CugaCalibration.ViewModels.Microscope;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
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
using System.Collections.ObjectModel;

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
    private readonly ICalibrationRecipeService _calibrationRecipeService;

    #region 界面显示属性

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private ApplicationCookie _applicationCookie;

    [ObservableProperty]
    private bool _isLoadingOk;

    [ObservableProperty]
    private CalibrationViewModelBase? _activeItem;

    [ObservableProperty]
    private CalibrationSetting _calibrationSetting;

    [ObservableProperty]
    private ObservableCollection<(string, string)> _selectReviewList = [];

    [ObservableProperty]
    private ObservableCollection<CalibrationItemStep> _calibrationStepList = [];

    /// <summary>
    /// 校准步骤索引
    /// </summary>
    [ObservableProperty]
    private int _calibrationStepIndex = -1;

    #endregion 界面显示属性

    #region 控制按钮

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalibrateCommand))]
    private bool _isCalibrateEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReviewCommand))]
    private bool _isReviewEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isCancelEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    private bool _isPreviousEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _isNextEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AutoCalibrateCommand))]
    [NotifyCanExecuteChangedFor(nameof(AutoReviewCommand))]
    private bool _isAutoCalibrateEnable;

    [ObservableProperty]
    private bool _isEnable = true;

    [ObservableProperty]
    private bool _isAutoCalibrate;

    [ObservableProperty]
    private double _autoCalibrationProgress;

    [ObservableProperty]
    private bool _autoCalibrationIsRunning = true;

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
        IApplicationCookieService applicationCookieService,
        ICalibrationRecipeService calibrationRecipeService)
    {
        _messenger = messenger;
        _logger = logger;
        _contextProvider = contextProvider;
        _cacheProvider = cacheProvider;
        _recipeCacheProvider = recipeCacheProvider;
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _calibrationCacheProviderService = calibrationCacheProviderService;
        _calibrationSetting = calibrationSetting;
        _applicationCookie = applicationCookie;
        _applicationCookieService = applicationCookieService;
        _title = applicationCookie.Title;
        _calibrationRecipeService = calibrationRecipeService;
        _messenger.RegisterAll(this);
    }

    #region Command

    [RelayCommand]
    private void Loaded()
    {
        IsLoadingOk = false;

        var showDialog = _windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoginWindowViewModel>());
        if (showDialog == false)
        {
            return;
        }

        _windowManagerService.ShowDialog(HostApplication.GetRequiredService<RecipeManagementViewModel>());
        if (ApplicationCookie.CalibrationRecipeDto is null)
        {
            return;
        }

        showDialog = _windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoadingWindowViewModel>());
        if (showDialog == false)
        {
            return;
        }

        Title = ApplicationCookie.Title;

        _windowManagerService.ShowWindow(HostApplication.GetRequiredService<StageWindowViewModel>());
        _windowManagerService.ShowWindow(HostApplication.GetRequiredService<MicroscopeWindowViewModel>());

        IsLoadingOk = true;
        LoadCalibrationStatus();
    }

    [RelayCommand]
    private void Closed()
    {
        _messenger.UnregisterAll(this);
        _recipeCacheProvider.Dispose();
        _cacheProvider.Dispose();
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

    [RelayCommand(CanExecute = nameof(IsAutoCalibrateEnable))]
    private async Task AutoCalibrateAsync(ObservableCollection<(string, string)> selectReviewList)
    {
        IsAutoCalibrateEnable = false;
        await Task.Run(async () =>
        {
            if (UpdateWaferMap() == false)
            {
                _dialogWindowProvider.ShowDialog("Update WaferMap Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            CalibrationStepIndex = 0;
            foreach (var (spaceName, name) in selectReviewList)
            {
                AutoCalibrationIsRunning = false;
                var abstractCalibrationViewModel = HostApplication.GetRequiredService<CalibrationViewModelBase>(spaceName);
                ActiveItem = abstractCalibrationViewModel;
                if (ActiveItem is not null)
                {
                    ActiveItem.IsAutoCalibrate = IsAutoCalibrate;
                    ActiveItem.IsRecipeCalibrate = IsAutoCalibrate;
                    ActiveItem.CalibrationStepIndex = 0;
                    ActiveItem.AutoCalibrationStepIndex = 0;
                    ActiveItem.AutoCalibrationProgress = 0d;
                    if (await ActiveItem.AutoCalibrateAsync(ActiveItem) == false) return;
                    CalibrationStepIndex++;
                    AutoCalibrationProgress = CalibrationStepIndex - 1 / (double)selectReviewList.Count * 100d;
                }
            }
        });
        AutoCalibrationIsRunning = true;
        IsEnable = true;
        IsAutoCalibrateEnable = true;
    }

    [RelayCommand(CanExecute = nameof(IsAutoCalibrateEnable))]
    private async Task AutoReviewAsync(ObservableCollection<(string, string)> selectReviewList)
    {
        IsAutoCalibrateEnable = false;
        await Task.Run(async () =>
        {
            if (ApplicationCookie.CalibrationRecipeDto is null)
            {
                _dialogWindowProvider.ShowDialog("Applied recipe is empty! Please select a recipe!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (selectReviewList.Count == 0)
            {
                _dialogWindowProvider.ShowDialog("Please select a review ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (UpdateWaferMap() == false)
            {
                _dialogWindowProvider.ShowDialog("Update WaferMap Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            CalibrationStepIndex = 0;
            foreach (var (spaceName, name) in selectReviewList)
            {
                AutoCalibrationIsRunning = false;
                IsEnable = false;
                var abstractCalibrationViewModel = HostApplication.GetRequiredService<CalibrationViewModelBase>(spaceName);
                ActiveItem = abstractCalibrationViewModel;
                if (ActiveItem is not null)
                {
                    ActiveItem.IsAutoCalibrate = IsAutoCalibrate;
                    ActiveItem.IsRecipeCalibrate = IsAutoCalibrate;
                    ActiveItem.CalibrationStepIndex = 0;
                    ActiveItem.AutoCalibrationStepIndex = 0;
                    ActiveItem.AutoCalibrationProgress = 0d;
                    if (await ActiveItem.AutoReviewAsync(ActiveItem) == false) return;
                    CalibrationStepIndex++;
                    AutoCalibrationProgress = CalibrationStepIndex / (double)selectReviewList.Count * 100d;
                }
            }
        });
        IsAutoCalibrateEnable = true;
        AutoCalibrationIsRunning = true;
        IsEnable = true;
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
    private void HandleSelectionChanged(ObservableCollection<(string, string)> selectReviewList)
    {
        CalibrationStepList.Clear();
        CalibrationStepIndex = -1;
        foreach (var (spaceName, name) in selectReviewList)
        {
            var calibrationItem = new CalibrationItemStep { StepName = name };
            CalibrationStepList.Add(calibrationItem);
        }

        if (selectReviewList.Count == 0)
        {
            IsAutoCalibrateEnable = false;
            return;
        }
        else IsAutoCalibrateEnable = true;

        SelectReviewList = selectReviewList;
    }

    [RelayCommand]
    private void OpenToolMenu(string viewModel)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(viewModel)) return;

            var viewModelBase = HostApplication.GetRequiredService<ViewModelBase>(viewModel);
            if (viewModelBase is null) return;
            switch (viewModelBase)
            {
                case MainWindowViewModel:
                    var save = _calibrationCacheProviderService.TrySave();
                    if (save)
                        _dialogWindowProvider.ShowDialog("Save Success.");
                    else
                        _dialogWindowProvider.ShowDialog("Save Failed! Please save it again.", DialogButtonsEnum.OK, DialogIconEnum.Error);

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

                default:
                    _windowManagerService.ShowDialog(viewModelBase);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Load Menu View({@ViewModel}) Failed", nameof(MainWindowViewModel), viewModel);
        }
    }

    [RelayCommand]
    private async Task OpenIsCheckedMenuAsync(string menuName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(menuName)) return;
            if (menuName == nameof(CalibrationTypeEnum.AutoCalibration))
            {
                await CancelAsync();
                if (!IsAutoCalibrate)
                {
                    IsAutoCalibrateEnable = IsAutoCalibrate;
                    if (ApplicationCookie.CalibrationMenu.ChildList.Count > 0)
                    {
                        foreach (var itemChildList in ApplicationCookie.CalibrationMenu.ChildList)
                        {
                            foreach (var x in itemChildList.ChildList) x.IsSelected = false;
                        }
                    }
                }

                _contextProvider.Post(() => { _messenger.Send(ToggleAutoCalibrateEventFactory.RefreshAutoCalibrateStatus(IsAutoCalibrate)); });
            }

            CalibrationStepIndex = -1;
            SelectReviewList.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Load Menu View({@ViewModel}) Failed", nameof(menuName), menuName);
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
            if (message.Value.IsRefreshMenuStatus == true)
            {
                LoadCalibrationStatus();
                OnPropertyChanged(nameof(ApplicationCookie));
                return; //防止在校准或验证过程中保存setting，导致把公共按钮的状态都禁用
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

    private bool UpdateWaferMap()
    {
        try
        {
            if (_calibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
                return false;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update wafer map failed");
            return false;
        }
    }

    private void LoadCalibrationStatus()
    {
        _ = Task.Run(() =>
        {
            try
            {
                if (IsLoadingOk == false) return;

                var calibrationSetting = _cacheProvider.GetOrDefault<CalibrationSetting>();
                var isCalibrationSettingChanged = calibrationSetting.IsOk(out _);
                if (isCalibrationSettingChanged) CalibrationSetting.AdaptIn(calibrationSetting);

                var calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopeFocusCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<MicroscopeFocusItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopeCalChipCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<MicroscopeCalChipDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopePixelSizeCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<MicroscopeCentricityCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<MicroscopeCentricityItemDto>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckGantryCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckGantryDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckCenterCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckCenterObjDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckPrealignerCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckPrealignerObjDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckStageMapCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckStageMapDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckAutoFocusCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckAutoFocusDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckGlobalScaleErrorCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckGlobalScaleErrorDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<ChuckRotateScaleCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<ChuckRotateScaleErrorDto>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<AdsPressureGainsCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AdsPressureGainsDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<AdsXGainsCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AdsXGainsItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<AdsYGainsCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<AdsYGainsItemDto>().IsOk(out _);

                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserAutoFocusCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<LaserAutoFocusDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserAttenuatorViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserAttenuatorDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserBeamStabilizerCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<LaserBeamStabilizerObjDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<AODDelayViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<AODDelayDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserXTCCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserXTCCalibrationItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserPixelSizeCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserPixelSizeItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserLineCentricityCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserLineCentricityItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserLineOrientationOffsetCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LineOrientationOffsetItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserXPixelSizeCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserIlluminationProfileCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserIlluminationProfileItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserOpticalPowerMeterViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserOpticalPowerMeterDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserXYAstigmatismCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserXYAstigmatismCalibrationItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<AODAlignmentViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<AODAlignmentDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserFocusShiftCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<FocusShiftDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserRtfcCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<RtfcDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserPmtAgcDelayCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefaultArray<LaserPmtAgcDelayItemDto>().IsOk(out _);
                calibrationItem = _applicationCookieService.FindCalibrationItem<LaserDOEAngleCalibrationViewModel>();
                if (calibrationItem is not null) calibrationItem.IsCalibrated = _cacheProvider.GetOrDefault<LaserDOEAngleDto>().IsOk(out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{@Name}: Load Calibration Status Failed", nameof(MainWindowViewModel));
            }
        });
    }
}