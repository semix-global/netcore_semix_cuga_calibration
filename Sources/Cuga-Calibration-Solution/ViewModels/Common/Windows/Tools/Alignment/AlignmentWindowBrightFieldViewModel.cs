using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentWindowBrightFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentWindowBrightFieldViewModel : ViewModelBase, IRecipient<PropertyChangedMessage<bool>>, IRecipient<ValueChangedMessage<ToggleToolsEvent>>
{
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ILogger<AlignmentWindowBrightFieldViewModel> _logger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly CalibrationSetting _calibrationSetting;
    private readonly ApplicationCookie _applicationCookie;
    private readonly ICalibrationCacheProvider _calibrationCacheProvider;
    private readonly ICalibrationStatusService _calibrationStatusService;
    private readonly IApplicationCookieService _applicationCookieService;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    public partial StatusViewModel StatusViewModel { get; set; }

    [ObservableProperty]
    public partial ReviewViewModel ReviewViewModel { get; set; }

    [ObservableProperty]
    public partial StageViewModel StageViewModel { get; set; }

    [ObservableProperty]
    public partial MicroscopeViewModel MicroscopeViewModel { get; set; }

    [ObservableProperty]
    public partial AlignmentParamWindowBrightFieldViewModel AlignmentParamWindowBrightFieldViewModel { get; set; }

    [ObservableProperty]
    public partial AlignmentCacheBrightField Cache { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentCacheBrightField[] Caches { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #region 界面

    [ObservableProperty]
    public partial bool IsShowAlign { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkSiteCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(AlignmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(AdvancedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsEnable { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkSiteCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(AlignmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(AdvancedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsNextEnable { get; set; }

    public bool IsAdvancedEnable => StepIndex == 0;

    public bool IsPreviousEnable => 0 < StepIndex && StepIndex <= StepList.Count - 1;

    public bool IsAlignment => StepList[^1].StepIsNextEnable;

    public bool IsSave => StepList[^1].StepIsNextEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkSiteCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(AlignmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(AdvancedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial int StepIndex { get; set; }

    [ObservableProperty]
    public partial List<AlignmentItemStep> StepList { get; set; } =
    [
        new() { StepName = "Low magnification mark point 1" },
        new() { StepName = "Low magnification mark point 2" },
        new() { StepName = "High magnification mark point 1" },
        new() { StepName = "High magnification mark point 2" }
    ];

    #endregion 界面

    public AlignmentWindowBrightFieldViewModel(
        StatusViewModel statusViewModel,
        ReviewViewModel reviewViewModel,
        StageViewModel stageViewModel,
        MicroscopeViewModel microscopeViewModel,
        IDialogWindowProvider dialogWindowProvider,
        ILogger<AlignmentWindowBrightFieldViewModel> logger,
        IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        AlignmentParamWindowBrightFieldViewModel alignmentParamWindowBrightFieldViewModel,
        IWindowManagerService windowManagerService,
        CalibrationSetting calibrationSetting,
        ApplicationCookie applicationCookie,
        ICalibrationCacheProvider calibrationCacheProvider,
        ICalibrationStatusService calibrationStatusService,
        IApplicationCookieService applicationCookieService)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _cacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        _recipeCacheProvider = HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey);
        _calibrationCacheProvider = calibrationCacheProvider;
        _calibrationStatusService = calibrationStatusService;
        _applicationCookieService = applicationCookieService;
        _logger = logger;
        _contextProvider = contextProvider;
        AlignmentParamWindowBrightFieldViewModel = alignmentParamWindowBrightFieldViewModel;
        _windowManagerService = windowManagerService;
        _calibrationSetting = calibrationSetting;
        _applicationCookie = applicationCookie;
        StatusViewModel = statusViewModel;
        ReviewViewModel = reviewViewModel;
        StageViewModel = stageViewModel;
        MicroscopeViewModel = microscopeViewModel;

        messenger.RegisterAll(this);
    }

    [RelayCommand]
    private Task LoadedAsync()
    {
        return InvokeAsync(() =>
        {
            try
            {
                CancelToken();
                _cancellationTokenSource = new CancellationTokenSource();

                MicroscopeCalChip = _applicationCookieService.GetCalibration<MicroscopeCalChipDTO>();

                Cache.IsVerified = false;
                Cache.IsOk = false;
                if (_applicationCookie.MicroscopeLensInformations.Contains(Cache.LowMag) == false ||
                    _applicationCookie.MicroscopeLensInformations.Contains(Cache.HighMag) == false)
                {
                    Cache.LowMag = _calibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
                    Cache.HighMag = _calibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();
                }

                _contextProvider.Send(() =>
                {
                    AlignmentParamWindowBrightFieldViewModel.MicroscopeLensInformationList = [.. _applicationCookie.MicroscopeLensInformations];
                    StepIndex = 0;
                });

                foreach (var x in StepList) x.StepIsNextEnable = x.DefaultIsNextEnable;

                Cache.LowSite1.Location = Cache.LowSite2.Location = Cache.HighSite1.Location = Cache.HighSite2.Location = Point.Origin;
                Cache.LowSite1.Template = Cache.LowSite2.Template = Cache.HighSite1.Template = Cache.HighSite2.Template = null;
                Advanced();

                // 设置到明场中心、低倍镜、角度为0(上料默认状态)
                MicroscopeCalChip.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;

                StageViewModel.SetAbsoluteStageTheta(0d);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMag);
                StageViewModel.SetBrightFieldAbsoluteStageXy(
                    Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel
                        ? new Point(0, 0)
                        : StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.CurrentItem.BrightFieldMachinePosition));
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog("Failed to set bright field absolute stage xy", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                _logger.LogError(ex, "{@Name}: Loaded Failed", nameof(AlignmentWindowBrightFieldViewModel));
            }
        });
    }

    [RelayCommand(CanExecute = nameof(IsPreviousEnable))]
    private Task PreviousAsync()
    {
        return InvokeAsync(() =>
        {
            StepList[StepIndex].StepIsNextEnable = StepList[StepIndex].DefaultIsNextEnable; // 恢复默认值
            Cache.IsVerified = false;
            _contextProvider.Send(() => StepIndex--);

            MovePositionAndSwitchMag();
        });
    }

    [RelayCommand(CanExecute = nameof(IsNextEnable))]
    private Task NextAsync()
    {
        return InvokeAsync(() =>
        {
            StepList[StepIndex].StepIsNextEnable = StepList[StepIndex].DefaultIsNextEnable; // 恢复默认值
            Cache.IsVerified = false;
            _contextProvider.Send(() => StepIndex++);

            MovePositionAndSwitchMag();
        });
    }

    [RelayCommand(CanExecute = nameof(IsEnable))]
    private Task MarkSiteAsync()
    {
        return InvokeAsync(() =>
        {
            var magnificationEnum = MicroscopeViewModel.GetCurrentMicroscopeLensInformation();

            switch (StepIndex)
            {
                case 0:
                    StepList[0].StepIsNextEnable = false;
                    if (magnificationEnum != Cache.LowMag)
                    {
                        _dialogWindowProvider.ShowDialog("Low magnification is not match", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return;
                    }

                    var resultLowSite1 = StageViewModel.MarkAlignSite1(Cache.LowSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);

                    Cache.LowSite1 = resultLowSite1;
                    Cache.LowSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.LowSite1.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    StepList[0].StepIsNextEnable = true;

                    Cache.LowSite2.Location = Cache.LowSite1.Location;
                    Cache.LowSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.LowSite2.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    NextAsync().Wait();

                    break;

                case 1:
                    StepList[1].StepIsNextEnable = false;
                    if (magnificationEnum != Cache.LowMag)
                    {
                        _dialogWindowProvider.ShowDialog("Low magnification is not match", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return;
                    }

                    var resultLowSite2 = StageViewModel.MarkAlignSite2(Cache.LowSite1, Cache.AlgorithmWaferTypeEnum);
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(resultLowSite2.Location, Cache.CalChipSiteModelEnum);

                    Cache.LowSite2 = resultLowSite2;
                    Cache.LowSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.LowSite2.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    StepList[1].StepIsNextEnable = true;

                    Cache.HighSite1.Location = Cache.LowSite1.Location;
                    Cache.HighSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.HighSite1.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    Cache.HighSite2.Location = Cache.LowSite2.Location;
                    Cache.HighSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.HighSite2.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    NextAsync().Wait();

                    break;

                case 2:
                    StepList[2].StepIsNextEnable = false;
                    if (magnificationEnum != Cache.HighMag)
                    {
                        _dialogWindowProvider.ShowDialog("Low magnification is not match", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return;
                    }

                    var resultHighSite1 = StageViewModel.MarkAlignSite1(Cache.HighSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);

                    Cache.HighSite1 = resultHighSite1;
                    Cache.HighSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.HighSite1.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    StepList[2].StepIsNextEnable = true;

                    NextAsync().Wait();

                    break;

                case 3:
                    StepList[3].StepIsNextEnable = false;
                    if (magnificationEnum != Cache.HighMag)
                    {
                        _dialogWindowProvider.ShowDialog("Low magnification is not match", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return;
                    }

                    var resultHighSite2 = StageViewModel.MarkAlignSite2(Cache.HighSite1, Cache.AlgorithmWaferTypeEnum);
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(resultHighSite2.Location, Cache.CalChipSiteModelEnum);

                    Cache.HighSite2 = resultHighSite2;
                    Cache.HighSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    Cache.HighSite2.UpdateTemplateMatchScoreThreshold(_calibrationSetting);

                    StepList[3].StepIsNextEnable = true;

                    break;
            }
        });
    }

    [RelayCommand(CanExecute = nameof(IsAlignment))]
    private Task AlignmentAsync()
    {
        return InvokeAsync(() =>
        {
            var result = StageViewModel.Alignment(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.LowMag,
                Cache.HighMag,
                Cache.AlgorithmWaferTypeEnum,
                Cache.CalChipSiteModelEnum);

            _dialogWindowProvider.ShowDialog("Alignment Ok");
            Cache.Result = result;
            Cache.IsVerified = true;
            _contextProvider.Send(() => SaveCommand.NotifyCanExecuteChanged());
        });
    }

    [RelayCommand(CanExecute = nameof(IsSave))]
    private Task SaveAsync()
    {
        return InvokeAsync(() =>
        {
            Cache.IsOk = true;
            try
            {
                if (Save(_cancellationTokenSource.Token) == false)
                    ThrowHelper.ThrowInvalidOperationException();

                _dialogWindowProvider.ShowDialog("Save Ok");
                Close();
            }
            catch (Exception ex)
            {
                Cache.IsOk = false;
                _logger.LogError(ex, "{@Name}: Failed to save alignment cache!", nameof(AlignmentWindowBrightFieldViewModel));
                _dialogWindowProvider.ShowDialog("Failed to save alignment cache!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
    }

    private bool Save(CancellationToken cancellationToken) => _calibrationCacheProvider.InvokeSave(update =>
    {
        update(Cache);
        Caches = [.. _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>(), .. _cacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>()];
        Caches =
        [
            Cache.Clone(),
            .. Caches
                .Where(t => t.CalChipSiteModelEnum != Cache.CalChipSiteModelEnum)
        ];

        _recipeCacheProvider.SetArray([.. Caches.Where(t => t.CalChipSiteModelEnum == CalChipSiteModelEnum.ChuckModel)], cancellationToken);
        _cacheProvider.SetArray([.. Caches.Where(t => t.CalChipSiteModelEnum != CalChipSiteModelEnum.ChuckModel)], cancellationToken);

        return true;
    }, nameof(AlignmentCacheBrightField), cancellationToken);

    [RelayCommand(CanExecute = nameof(IsAdvancedEnable))]
    private Task AdvancedAsync()
    {
        return InvokeAsync(() =>
        {
            Advanced();
            MovePositionAndSwitchMag();
        });
    }

    [RelayCommand(CanExecute = nameof(IsEnable))]
    private void Close()
    {
        try
        {
            CloseView(Cache.IsOk);
        }
        finally
        {
            CancelToken();
        }
    }

    private void MovePositionAndSwitchMag()
    {
        try
        {
            switch (StepIndex)
            {
                case 0:
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.LowSite1.Location, Cache.CalChipSiteModelEnum);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMag);
                    break;

                case 1:
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.LowSite2.Location, Cache.CalChipSiteModelEnum);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMag);
                    break;

                case 2:
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSite1.Location, Cache.CalChipSiteModelEnum);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMag);
                    break;

                case 3:
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSite2.Location, Cache.CalChipSiteModelEnum);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMag);
                    break;
            }
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Failed to set location!Error:{ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    private void Advanced()
    {
        AlignmentParamWindowBrightFieldViewModel.Cache = Cache;
        _ = _windowManagerService.ShowDialog(AlignmentParamWindowBrightFieldViewModel);

        Cache.IsVerified = false;
    }

    private void CancelToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

        _cancellationTokenSource = null;
    }

    private Task InvokeAsync(Action action)
    {
        return Task.Run(() =>
        {
            try
            {
                _contextProvider.Send(() => IsEnable = false);
                action();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{@Name}: Invoke Failed", nameof(AlignmentWindowBrightFieldViewModel));
            }
            finally
            {
                _contextProvider.Send(() => IsEnable = true);
            }
        });
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message is not { Sender: AlignmentItemStep, PropertyName: nameof(AlignmentItemStep.StepIsNextEnable) }) return;

        _contextProvider.Send(() =>
        {
            IsNextEnable = 0 <= StepIndex && StepIndex < StepList.Count - 1 && StepList[StepIndex].StepIsNextEnable;
            PreviousCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
            MarkSiteCommand.NotifyCanExecuteChanged();
            AlignmentCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
            AdvancedCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
        });
    }

    public void Receive(ValueChangedMessage<ToggleToolsEvent> message)
    {
        if (message.Value.IsToolsWindowEnable.HasValue)
            AlignmentParamWindowBrightFieldViewModel.IsToolsEnable = message.Value.IsToolsWindowEnable.Value;
    }
}