using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums.Optics;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
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
using System.Reactive.Linq;
using Core.Utilities.SourceGenerators.Attributes;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentWindowDarkFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentWindowDarkFieldViewModel : ViewModelBase, IRecipient<PropertyChangedMessage<bool>>, IRecipient<ValueChangedMessage<ToggleToolsEvent>>
{
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ILogger<AlignmentWindowBrightFieldViewModel> _logger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly CalibrationSetting _calibrationSetting;
    private readonly ApplicationCookie _applicationCookie;
    private readonly ICalibrationCacheProvider _calibrationCacheProvider;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private ReviewViewModel _reviewViewModel;

    [ObservableProperty]
    private StageViewModel _stageViewModel;

    [ObservableProperty]
    private MicroscopeViewModel _microscopeViewModel;

    [ObservableProperty]
    private LaserViewModel _laserViewModel;

    [ObservableProperty]
    private CIBViewModel _cIBViewModel;

    [ObservableProperty]
    private AlignmentParamWindowDarkFieldViewModel _alignmentParamWindowDarkFieldViewModel;

    [ObservableProperty]
    private AlignmentCacheDarkField _cache = new();

    [RecipeCache]
    [ObservableProperty]
    private AlignmentCacheDarkField[] _caches = [];

    #region 界面

    [ObservableProperty]
    private bool _isShowAlign = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkSiteCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(AlignmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(AdvancedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isEnable = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkSiteCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(AlignmentCommand))]
    [NotifyCanExecuteChangedFor(nameof(AdvancedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isNextEnable;

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
    private int _stepIndex;

    [ObservableProperty]
    private List<AlignmentItemStep> _stepList =
    [
        new() { StepName = "Low magnification mark point 1" },
        new() { StepName = "Low magnification mark point 2" },
        new() { StepName = "High magnification mark point 1" },
        new() { StepName = "High magnification mark point 2" }
    ];

    #endregion 界面

    public AlignmentWindowDarkFieldViewModel(
        ReviewViewModel reviewViewModel,
        StageViewModel stageViewModel,
        MicroscopeViewModel microscopeViewModel,
        LaserViewModel laserViewModel,
        CIBViewModel cibViewModel,
        IDialogWindowProvider dialogWindowProvider,
        ILogger<AlignmentWindowBrightFieldViewModel> logger,
        IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        AlignmentParamWindowDarkFieldViewModel alignmentParamWindowDarkFieldViewModel,
        IWindowManagerService windowManagerService,
        CalibrationSetting calibrationSetting,
        ApplicationCookie applicationCookie,
        ICalibrationCacheProvider calibrationCacheProvider)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _recipeCacheProvider = HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey)!;
        _logger = logger;
        _contextProvider = contextProvider;
        _alignmentParamWindowDarkFieldViewModel = alignmentParamWindowDarkFieldViewModel;
        _windowManagerService = windowManagerService;
        _calibrationSetting = calibrationSetting;
        _applicationCookie = applicationCookie;
        _calibrationCacheProvider = calibrationCacheProvider;
        _reviewViewModel = reviewViewModel;
        _stageViewModel = stageViewModel;
        _microscopeViewModel = microscopeViewModel;
        _laserViewModel = laserViewModel;
        _cIBViewModel = cibViewModel;
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
                var cancellationToken = _cancellationTokenSource.Token;

                Caches = _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
                Cache = Caches.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation
                                                    && t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum
                                                    && t is { IsOk: true, IsVerified: true })
                        ?? Cache;

                var productivityInformations = _applicationCookie.GetProductivityInformations(Cache.OpticsIlluminationModeEnum);

                if (_applicationCookie.MicroscopeLensInformations.Contains(Cache.LowMag) == false ||
                    _applicationCookie.MicroscopeLensInformations.Contains(Cache.HighMag) == false)
                {
                    Cache.LowMag = _calibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
                    Cache.HighMag = _calibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();
                }

                if (productivityInformations.Contains(Cache.ProductivityInformation) == false)
                {
                    Cache.ProductivityInformation = _applicationCookie.OILowProductivityInformation.Clone();
                    Cache.OpticsIlluminationModeEnum = OpticsIlluminationModeEnum.OI;
                }

                Cache.IsVerified = false;
                Cache.IsOk = false;

                _contextProvider.Send(() =>
                {
                    AlignmentParamWindowDarkFieldViewModel.MicroscopeLensInformationList = [.. _applicationCookie.MicroscopeLensInformations];
                    StepIndex = 0;
                });

                foreach (var x in StepList) x.StepIsNextEnable = x.DefaultIsNextEnable;

                Cache.LowSite1.Location = Cache.LowSite2.Location = Cache.HighSite1.Location = Cache.HighSite2.Location = Point.Origin;
                Cache.LowSite1.Template = Cache.LowSite2.Template = Cache.HighSite1.Template = Cache.HighSite2.Template = null;
                Advanced();

                ReviewViewModel.Monitor(cancellationToken);

#pragma warning disable IDE0079
#pragma warning disable IDISP001
                var subscribeReview = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.MonitorStageMilliseconds)).Subscribe(_ => ReviewViewModel.GetBrightFieldImageMemoryByteArray());
                cancellationToken.Register(subscribeReview.Dispose);
#pragma warning restore IDISP001
#pragma warning restore IDE0079

                // 设置到明场中心、低倍镜、角度为0(上料默认状态)
                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMag);
                StageViewModel.SetAbsoluteStageTheta(0);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog("Failed to set dark field absolute stage xy", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                _logger.LogError(ex, "{@Name}: Loaded Failed", nameof(AlignmentWindowDarkFieldViewModel));
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
            if (StepIndex is 2 or 3)
            {
                CIBViewModel.SetCIBConfiguration(_applicationCookie.CIBInformations, AlignmentParamWindowDarkFieldViewModel.CIBConfiguration);
            }

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
                    StageViewModel.SetBrightFieldAbsoluteStageXy(resultLowSite2.Location);

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

                    var resultHighSite1 = StageViewModel.MarkAlignSite1DarkField(Cache.ProductivityInformation, Cache.HighSizeEnum, Cache.AlgorithmWaferTypeEnum, Cache.OpticsIlluminationModeEnum);

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

                    var resultHighSite2 = StageViewModel.MarkAlignSite2DarkField(Cache.ProductivityInformation, Cache.HighSite1, Cache.AlgorithmWaferTypeEnum, Cache.OpticsIlluminationModeEnum);
                    StageViewModel.SetBrightFieldAbsoluteStageXy(resultHighSite2.Location);

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
            var result = StageViewModel.AlignmentDarkField(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.ProductivityInformation,
                Cache.LowMag,
                Cache.AlgorithmWaferTypeEnum,
                _calibrationSetting.SettingCommonParam.MainLaserLightInformation,
                Cache.OpticsIlluminationModeEnum);

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
                if (Save(CancellationToken.None) == false)
                    ThrowHelper.ThrowInvalidOperationException();

                _dialogWindowProvider.ShowDialog("Save Ok");
                Close();
            }
            catch (Exception ex)
            {
                Cache.IsOk = false;
                _logger.LogError(ex, "{@Name}: Failed to save alignment cache!", nameof(AlignmentWindowDarkFieldViewModel));
                _dialogWindowProvider.ShowDialog("Failed to save alignment cache!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
    }

    private bool Save(CancellationToken cancellationToken) => _calibrationCacheProvider.InvokeSave(update =>
    {
        update(Cache);
        Caches =
        [
            .. Caches
                .Where(t => (t.ProductivityInformation == Cache.ProductivityInformation
                             && t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum) == false),
            Cache.Clone()
        ];

        _recipeCacheProvider.SetArray(Caches, cancellationToken);
        return true;
    }, nameof(AlignmentCacheDarkField), cancellationToken);

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
                    StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite1.Location);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMag);
                    break;

                case 1:
                    StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite2.Location);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMag);
                    break;

                case 2:
                    StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite1.Location);
                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMag);
                    break;

                case 3:
                    StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite2.Location);
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
        AlignmentParamWindowDarkFieldViewModel.Cache = Cache;
        _ = _windowManagerService.ShowDialog(AlignmentParamWindowDarkFieldViewModel);

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
                _logger.LogError(e, "{@Name}: Invoke Failed", nameof(EFEMWindowViewModel));
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
            AlignmentParamWindowDarkFieldViewModel.IsToolsEnable = message.Value.IsToolsWindowEnable.Value;
    }
}