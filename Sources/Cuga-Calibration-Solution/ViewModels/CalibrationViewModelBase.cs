using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums;
using Core.Models.Events;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Recipe;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Humanizer;
using Local.NoSQL.DB.Providers.Helper;
using Local.NoSQL.DB.Providers.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase : ViewModelBase, IRecipient<PropertyChangedMessage<bool>>, IRecipient<ValueChangedMessage<ToggleAutoCalibrateEvent>>, IDisposable
{
    protected readonly ILogger<CalibrationViewModelBase> Logger;
    protected readonly IMessenger Messenger;
    protected readonly IHostEnvironment HostEnvironment;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly IWindowManagerService WindowManagerService;
    protected readonly ISynchronizationContextProvider SynchronizationContextProvider;
    protected readonly ICalibrationAlgorithmService CalibrationAlgorithmService;
    protected readonly ICacheProvider CacheProvider;
    protected readonly ICacheProvider RecipeCacheProvider;
    protected readonly ICalibrationStatusService CalibrationStatusService;
    protected readonly ICalibrationRecipeService CalibrationRecipeService;
    protected readonly CalibrationSetting CalibrationSetting;
    protected readonly string AppHomeDirectory;

    private readonly string _typeName;

    private string? _name;
    private CancellationTokenSource? _cancellationTokenSource;

    #region 属性

    #region ViewModels

    [ObservableProperty]
    private AdsViewModel _adsViewModel = HostApplication.GetRequiredService<AdsViewModel>();

    [ObservableProperty]
    private AfViewModel _afViewModel = HostApplication.GetRequiredService<AfViewModel>();

    [ObservableProperty]
    private EFEMViewModel _efemViewModel = HostApplication.GetRequiredService<EFEMViewModel>();

    [ObservableProperty]
    private LaserViewModel _laserViewModel = HostApplication.GetRequiredService<LaserViewModel>();

    [ObservableProperty]
    private MicroscopeViewModel _microscopeViewModel = HostApplication.GetRequiredService<MicroscopeViewModel>();

    [ObservableProperty]
    private ReviewViewModel _reviewViewModel = HostApplication.GetRequiredService<ReviewViewModel>();

    [ObservableProperty]
    private StageViewModel _stageViewModel = HostApplication.GetRequiredService<StageViewModel>();

    [ObservableProperty]
    private ConfigViewModel _configureViewModel = HostApplication.GetRequiredService<ConfigViewModel>();

    [ObservableProperty]
    private MonitorViewModel _monitorViewModel = HostApplication.GetRequiredService<MonitorViewModel>();

    #endregion ViewModels

    #region 重载只读属性

    /// <summary>
    /// 校准图片路径名称
    /// </summary>
    public virtual string CalibrateDirectoryName { get; set; } = string.Empty;

    /// <summary>
    /// 校准Html日志文件路径名称
    /// </summary>
    public virtual string CalibrateFileName { get; set; } = string.Empty;

    /// <summary>
    /// 验证Html日志文件路径名称
    /// </summary>
    public virtual string VerifyFileName { get; set; } = string.Empty;

    /// <summary>
    /// 校准步骤名称列表
    /// </summary>
    public virtual List<CalibrationItemStep> CalibrationStepList => [];

    /// <summary>
    /// 校准步骤名称列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CalibrationItemStep> _autoCalibrationStepList = [];

    #endregion 重载只读属性

    public ApplicationCookie ApplicationCookie { get; }


    public CalibrationRecipeDto? CalibrationRecipeDto => ApplicationCookie.CalibrationReviseRecipeDto;

    /// <summary>
    /// 校准名称
    /// </summary>
    public string Name => _name ??= _typeName.Humanize(LetterCasing.Title).Replace("CalibrationViewModel".Humanize(LetterCasing.Title), string.Empty);

    /// <summary>
    /// 校准名称
    /// </summary>
    /// <summary>
    /// 日志图片存储位置
    /// </summary>
    public string ImageFileDirectory => Path.Combine(AppHomeDirectory, "Images", _typeName, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    /// <summary>
    /// 模板存储位置
    /// </summary>m
    public string TemplateFileDirectory => Path.Combine(AppHomeDirectory, "Template", _typeName, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    /// <summary>
    /// Csv文件存储位置
    /// </summary>
    public string CsvFileDirectory => Path.Combine(AppHomeDirectory, "Csv", _typeName, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    /// <summary>
    /// 校准文件名称
    /// </summary>
    public string CalibrateHtmlLogFileName => string.IsNullOrWhiteSpace(CalibrateFileName) ? "Calibrate" : $"Calibrate-{FileHelper.RemoveInvalidFileName(CalibrateFileName)}";

    /// <summary>
    /// 校准日志名称
    /// </summary>
    public string VerifyHtmlFileLogName => string.IsNullOrWhiteSpace(VerifyFileName) ? "Verify" : $"Verify-{FileHelper.RemoveInvalidFileName(VerifyFileName)}";

    /// <summary>
    /// 校验日志名称
    /// </summary>
    public double CalibrationProgress =>
        ViewEnum switch
        {
            CalibrationItemViewEnum.Welcome => 0d,
            CalibrationItemViewEnum.Review => 100d,
            CalibrationItemViewEnum.Loading or CalibrationItemViewEnum.Calibration =>
                CalibrationStepIndex < 0 || CalibrationStepList.Count <= CalibrationStepIndex
                    ? 0
                    : CalibrationStepList[CalibrationStepIndex].StepIsNextEnable
                        ? (CalibrationStepIndex + 1d) / CalibrationStepList.Count * 100d
                        : (CalibrationStepIndex + 0d) / CalibrationStepList.Count * 100d,
            _ => 0d
        };

    [ObservableProperty]
    private double _autoCalibrationProgress;

    /// <summary>
    /// 日志唯一标识
    /// </summary>
    public Guid HtmlLogUniqueId { get; set; }

    /// <summary>
    /// 是否应用配方
    /// </summary>
    public bool IsRecipeCalibrate { get; set; }

    /// <summary>
    /// 是否正在编辑配方
    /// </summary>
    public bool IsRecipeEditing { get; set; }

    /// <summary>
    /// 是否自动化校准
    /// </summary>
    [ObservableProperty]
    private bool _isAutoCalibrate;

    #region 校准相关

    /// <summary>
    /// 界面状态
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    private CalibrationItemViewEnum _viewEnum;

    /// <summary>
    /// 校准步骤索引
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    private int _calibrationStepIndex = -1;

    /// <summary>
    /// 校准步骤索引
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    private int _autoCalibrationStepIndex = -1;

    /// <summary>
    /// 验证校准步骤索引
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    private int _autoReviewCalibrationStepIndex = -1;

    /// <summary>
    /// 校准步名称
    /// </summary>
    [ObservableProperty]
    private string _calibrationStepName = string.Empty;

    [ObservableProperty]
    private List<CalibrationItemStep> _stepList = [];

    /// <summary>
    /// 是否已校准完成
    /// </summary>
    [ObservableProperty]
    private bool _isCalibrated;

    #endregion 校准相关

    #endregion 属性

    protected CalibrationViewModelBase()
    {
        _typeName = GetType().Name;
        AppHomeDirectory = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value.AppHomeDirectory;
        Logger = (ILogger<CalibrationViewModelBase>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        Messenger = HostApplication.GetRequiredService<IMessenger>();
        HostEnvironment = HostApplication.GetRequiredService<IHostEnvironment>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        WindowManagerService = HostApplication.GetRequiredService<IWindowManagerService>();
        SynchronizationContextProvider = HostApplication.GetRequiredService<ISynchronizationContextProvider>();
        CalibrationAlgorithmService = HostApplication.GetRequiredService<ICalibrationAlgorithmService>();
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        RecipeCacheProvider = HostApplication.GetKeyedService<ICacheProvider>(LiteDbConstantHelper.RecipeDbKey);
        CalibrationStatusService = HostApplication.GetRequiredService<ICalibrationStatusService>();
        CalibrationRecipeService = HostApplication.GetRequiredService<ICalibrationRecipeService>();
        ApplicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        CalibrationSetting = HostApplication.GetRequiredService<CalibrationSetting>();

        Messenger.RegisterAll(this);
    }

    #region 公开

    /// <summary>
    /// 加载校准
    /// </summary>
    [RelayCommand]
    public async Task LoadedAsync()
    {
        try
        {
            if (!IsAutoCalibrate)
            {
                RefreshToken();
                await Task.Run(async () =>
                {
                    //IsAutoCalibrate = false;
                    ViewEnum = CalibrationItemViewEnum.Loading;
                    if (await LoadedingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                    {
                        UpdateFailedStatus();
                        Logger.LogWarning("{@Name}: Loading Failed", Name);
                        return;
                    }

                    UpdateWelcomeStatus();
                    Logger.LogInformation("{@Name}: Loading Ok!", Name);
                }, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            else
            {
                UpdateAutoCalibrateStatus();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Loading Exception", Name);
            UpdateFailedStatus();
        }
    }

    /// <summary>
    /// 打开校准窗口
    /// </summary>
    [RelayCommand]
    public async Task CalibrateAsync()
    {
        try
        {
            CheckStatus();
            await Task.Run(async () =>
            {
                Monitor();

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (IsRecipeEditing == false)
                {
                    DialogWindowProvider.TryShowDialog("Do you want to enable recipe information!", out var dialogButtonsEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Warning);
                    IsRecipeCalibrate = dialogButtonsEnum == DialogResultEnum.Yes;
                }

                if (await CalibratingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    ViewEnum = CalibrationItemViewEnum.Welcome;
                    Logger.LogWarning("{@Name}: Calibrate Failed", Name);
                    return;
                }

                HtmlLogUniqueId = Guid.NewGuid();
                Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());

                UpdateCalibrateStatus();
                Logger.LogInformation("{@Name}: Begin Calibrate!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Calibrate Exception", Name);
            UpdateFailedStatus();
        }
    }

    /// <summary>
    /// 打开校准复查窗口
    /// </summary>
    [RelayCommand]
    public async Task ReviewAsync()
    {
        try
        {
            CheckStatus();
            await Task.Run(async () =>
            {
                Monitor();

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await ReviewingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    ViewEnum = CalibrationItemViewEnum.Welcome;

                    DialogWindowProvider.ShowDialog("Please calibrate!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                UpdateReviewStatus();
                Logger.LogInformation("{@Name}: Begin Review!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Review Exception", Name);
            UpdateFailedStatus();
        }
    }

    /// <summary>
    /// 关闭校准界面
    /// </summary>
    [RelayCommand]
    public async Task CancelAsync()
    {
        try
        {
            UpdateDisableAll();

            await Task.Run(async () =>
            {
                CancelToken();

                if (ViewEnum == CalibrationItemViewEnum.Calibration) Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{ApplicationCookie.DeviceCode}_{CalibrateHtmlLogFileName}_Step1-Step{CalibrationStepIndex + 1}_Failed"));

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await CancelingAsync().ConfigureAwait(false) == false)
                {
                    Logger.LogError("{@Name}: Cancel Failed", Name);
                    UpdateFailedStatus();
                    return;
                }

                UpdateCancelStatus();
                Logger.LogInformation("{@Name}: Cancel!", Name);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Cancel Exception", Name);
            UpdateFailedStatus();
        }
    }

    /// <summary>
    /// 校准前一步
    /// </summary>
    [RelayCommand]
    public async Task PreviousAsync()
    {
        try
        {
            CheckStatus();

            await Task.Run(async () =>
            {
                CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = CalibrationStepList[CalibrationStepIndex].DefaultIsNextEnable; // 恢复默认值

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await PreviousingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdatePreviousNextStatus();
                    Logger.LogWarning("{@Name}: Previous Failed", Name);
                    return;
                }

                CalibrationStepIndex--;
                UpdatePreviousNextStatus();

                Logger.LogInformation("{@Name}: Previous!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Previous Exception", Name);
            UpdateFailedStatus();
        }
    }

    /// <summary>
    /// 校准下一步
    /// </summary>
    [RelayCommand]
    public async Task NextAsync()
    {
        try
        {
            CheckStatus();
            await Task.Run(async () =>
            {
                CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = CalibrationStepList[CalibrationStepIndex].DefaultIsNextEnable; // 恢复默认值

                ViewEnum = CalibrationItemViewEnum.Loading;
                if (await NextingAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    UpdatePreviousNextStatus();
                    Logger.LogWarning("{@Name}: Next Failed", Name);
                    return;
                }

                CalibrationStepIndex++;
                UpdatePreviousNextStatus();

                if (CalibrationStepIndex == 0)
                {
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
                    HtmlLogUniqueId = Guid.NewGuid();
                    Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogInformation("{@Name}: Next!", Name);
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Next Exception", Name);
            UpdateFailedStatus();
        }
    }

    /// <summary>
    /// 自动化校准
    /// </summary>
    [RelayCommand]
    public async Task<bool> AutoCalibrateAsync(CalibrationViewModelBase calibrationViewModelBase)
    {
        CheckStatus();
        var result = false;
        await Task.Run(async () =>
        {
            ViewEnum = CalibrationItemViewEnum.Auto;
            if (calibrationViewModelBase is not null)
            {
                if (await calibrationViewModelBase.AutomationActionAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                {
                    ViewEnum = CalibrationItemViewEnum.Welcome;
                    Logger.LogWarning("{@Name}: Auto Failed", Name);
                    UpdateAutoCalibrateStatus();
                    return;
                }
            }

            result = true;
            UpdateAutoCalibrateStatus();
            ViewEnum = CalibrationItemViewEnum.Welcome;
            Logger.LogInformation("{@Name}: Auto Ok!", Name);
        }, _cancellationTokenSource.Token);
        return result;
    }

    /// <summary>
    /// 自动化验证
    /// </summary>
    [RelayCommand]
    public async Task<bool> AutoReviewAsync(CalibrationViewModelBase calibrationViewModelBase)
    {
        CheckStatus();
        var result = true;
        await Task.Run(async () =>
        {
            ViewEnum = CalibrationItemViewEnum.Auto;
            if (calibrationViewModelBase is not null)
            {
                if (await calibrationViewModelBase.AutomationReviewActionAsync(_cancellationTokenSource.Token).ConfigureAwait(false) == false)
                    result = false;
            }

            UpdateAutoCalibrateStatus();
            ViewEnum = CalibrationItemViewEnum.Welcome;
            Logger.LogInformation($"{Name}: Auto {(result ? "OK" : "Failed")}!");
        }, _cancellationTokenSource.Token).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// 校准下一步
    /// </summary>
    public async Task<bool> AutoStepAsync()
    {
        try
        {
            await Task.Run(() => { UpdateAutoStepStatus(); });
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Next Exception", Name);
            return false;
        }
    }

    #region Event

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message is not { Sender: CalibrationItemStep, PropertyName: nameof(CalibrationItemStep.StepIsNextEnable) }) return;

        UpdateNextStatus();
        OnPropertyChanged(nameof(CalibrationProgress));
    }

    #endregion Event

    #endregion 公开

    #region 重载

    protected virtual Task<bool> LoadedingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> CalibratingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> ReviewingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> CancelingAsync()
    {
        return Task.Run(() =>
        {
            if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out _))
                MicroscopeViewModel.SwitchMicroscopeLensInformation(ApplicationCookie.MicroscopeLensInformationList[0]);
            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
            return true;
        });
    }

    protected virtual Task<bool> PreviousingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual Task<bool> NextingAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    protected virtual bool EnableDependedCalibrationItems(CancellationToken cancellationToken) => true;

    protected virtual void Monitor() => CheckStatus();

    public virtual void GetAutoCalibrationStep() => AutoCalibrationStepList.ToList();

    public virtual Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
        HtmlLogUniqueId = Guid.NewGuid();
        AutoReviewCalibrationStepIndex = -1;
        Logger.LogHtmlInformation($"1. {Name}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        return Task.FromResult(true);
    }

    public virtual Task<bool> AutomationReviewActionAsync(CancellationToken cancellationToken)
    {
        Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
        HtmlLogUniqueId = Guid.NewGuid();
        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
        AutoCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
        CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName;
        AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;

        return Task.FromResult(true);
    }

    public virtual Task<bool> AutomationRecipeInformationAsync(string recipeName) => Task.FromResult(true);

    #endregion 重载

    #region 校准

    protected Task<bool> InvokeCalibrateAsync(Func<Task<bool>> func, string comment = "")
    {
        return InvokeCalibrateAsync(() => func.Invoke().GetAwaiter().GetResult(), comment);
    }

    protected Task<bool> InvokeVerifyAsync(Func<Task<bool>> func)
    {
        return InvokeVerifyAsync(() => func.Invoke().GetAwaiter().GetResult());
    }

    protected async Task<bool> InvokeCalibrateAsync(Func<bool> func, string comment = "")
    {
        if (IsAutoCalibrate)
        {
            Logger.LogHtmlInformation($"{AutoCalibrationStepIndex + 1}. {AutoCalibrationStepList[AutoCalibrationStepIndex].StepName}{(string.IsNullOrWhiteSpace(comment) ? string.Empty : $"[{comment}]")}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
        }
        else
        {
            Logger.LogHtmlInformation($"{CalibrationStepIndex + 1}. {CalibrationStepList[CalibrationStepIndex].StepName}{(string.IsNullOrWhiteSpace(comment) ? string.Empty : $"[{comment}]")}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
        }

        var calibrateName = Name.Trim().Replace(" ", "");
        var result = false;
        try
        {
            CheckStatus();
            await Task.Run(() =>
            {
                if (IsAutoCalibrate)
                {
                    result = func.Invoke();
                }
                else
                {
                    UpdateDisableAll();
                    result = CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = func.Invoke();
                }
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            result = false;

            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"Calibrate {calibrateName} Canceled!");
                Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                return result;
            }

            Logger.LogHtmlCritical(ex, "Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            return result;
        }
        finally
        {
            if (IsAutoCalibrate)
            {
                if (AutoCalibrationStepIndex == AutoCalibrationStepList.Count - 1 || !result) Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{nameof(CalibrationTypeEnum.AutoCalibration)}_{ApplicationCookie.DeviceCode}_{calibrateName}_{(result ? "OK" : "Failed")}"));
            }
            else
            {
                UpdatePreviousNextStatus();
                if (CalibrationStepIndex == CalibrationStepList.Count - 1 || !result) Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml($"{nameof(CalibrationTypeEnum.HandleCalibration)}_{ApplicationCookie.DeviceCode}_{calibrateName}_{CalibrateHtmlLogFileName}_{(result ? "OK" : "Failed")}"));
            }
        }
    }

    protected async Task<bool> InvokeVerifyAsync(Func<bool> func)
    {
        if (IsAutoCalibrate == false)
            HtmlLogUniqueId = Guid.NewGuid();
        var calibrateName = Name.Trim().Replace(" ", "");
        Logger.LogHtmlInformation($"1. {calibrateName}", HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation("Verify", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
        var result = false;
        try
        {
            CheckStatus();

            await Task.Run(() =>
            {
                if (IsAutoCalibrate == false) UpdateDisableAll();

                result = func.Invoke();
            }, _cancellationTokenSource.Token).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            result = false;
            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"Calibrate {calibrateName} Canceled!");
                Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                return result;
            }

            Logger.LogHtmlCritical(ex, "Critical", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            return result;
        }
        finally
        {
            UpdateReviewStatus();
            if (IsAutoCalibrate)
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{nameof(CalibrationTypeEnum.AutoVerify)}_{ApplicationCookie.DeviceCode}_{calibrateName}_{VerifyHtmlFileLogName}_{(result ? "OK" : "Failed")}"));
            else
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{nameof(CalibrationTypeEnum.HandleVerify)}_{ApplicationCookie.DeviceCode}_{calibrateName}_{VerifyHtmlFileLogName}_{(result ? "OK" : "Failed")}"));
        }
    }

    protected bool InvokeSave(Func<Action<ICacheItem>, bool> func)
    {
        while (true)
        {
            if (func(UpdateIsInsert)) return true;

            DialogWindowProvider.TryShowDialog("Save Failed!", out var dialogResultEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);

            if (dialogResultEnum == DialogResultEnum.Retry) continue;

            return false;
        }

        void UpdateIsInsert(ICacheItem cacheItem)
        {
            cacheItem.Id = 0;
            cacheItem.CreatedTime = DateTime.Now;
            cacheItem.IsDeleted = false;

            if (cacheItem is not IEntityAdd entity) return;

            entity.CreatedUserId = ApplicationCookie.SysUser.Id;
            entity.CreatedUserName = ApplicationCookie.SysUser.UserName;
        }
    }

    #endregion 校准

    #region 状态更新

    private void CancelToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void RefreshToken()
    {
        CancelToken();

        _cancellationTokenSource = new CancellationTokenSource();
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void CheckStatus()
    {
        if (_cancellationTokenSource is null) RefreshToken();

        if (_cancellationTokenSource.IsCancellationRequested) ThrowHelper.ThrowOperationCanceledException();
    }

    private void UpdateFailedStatus()
    {
        CalibrationStepIndex = int.MinValue;
        IsCalibrated = false;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Error;
    }

    private void UpdateCancelStatus()
    {
        CalibrationStepIndex = int.MinValue;
        IsCalibrated = false;

        UpdateDisableAll();
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Welcome;
    }

    private void UpdateWelcomeStatus()
    {
        CalibrationStepIndex = int.MinValue;
        IsCalibrated = false;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(true));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(true));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Welcome;
    }

    private void UpdateAutoCalibrateStatus()
    {
        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());
        //UpdatePreviousNextStatus();
    }

    private void UpdateCalibrateStatus()
    {
        CalibrationStepIndex = int.MinValue;
        IsCalibrated = false;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Calibration;

        if (CalibrationStepIndex < 0) CalibrationStepIndex = 0;
        UpdatePreviousNextStatus();
    }

    private void UpdateReviewStatus()
    {
        CalibrationStepIndex = CalibrationStepList.Count - 1;
        IsCalibrated = false;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Review;
    }

    private void UpdatePreviousNextStatus()
    {
        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());
        if (IsCalibrated)
        {
            if (!IsAutoCalibrate)
            {
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
                HtmlLogUniqueId = Guid.NewGuid();
                DialogWindowProvider.ShowDialog($"Calibration {Name} Ok!");
            }

            UpdateWelcomeStatus();
        }
        else
        {
            UpdatePreviousStatus();
            UpdateNextStatus();

            ViewEnum = CalibrationItemViewEnum.Calibration;
        }
    }

    private void UpdateDisableAll()
    {
        Messenger.Send(ToggleCalibrateEventFactory.Disable());
        Messenger.Send(PopupWindowEventFactory.DisableIsPopupWindowEnable());
    }

    private void UpdatePreviousStatus()
    {
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsPreviousEnable(0 < CalibrationStepIndex && CalibrationStepIndex <= CalibrationStepList.Count - 1));
    }

    private void UpdateNextStatus()
    {
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsNextEnable(0 <= CalibrationStepIndex && CalibrationStepIndex < CalibrationStepList.Count && CalibrationStepList[CalibrationStepIndex].StepIsNextEnable));
    }

    private void UpdateAutoStepStatus()
    {
        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());
    }

    #endregion 状态更新

    public virtual void Dispose()
    {
        CancelToken();

        GC.SuppressFinalize(this);
    }

    public void Receive(ValueChangedMessage<ToggleAutoCalibrateEvent> message)
    {
        if (message.Value.IsAutoCalibrateEnable.HasValue)
            IsAutoCalibrate = message.Value.IsAutoCalibrateEnable.Value;
        if (!IsAutoCalibrate)
        {
            IsAutoCalibrate = IsAutoCalibrate;
            IsRecipeCalibrate = IsAutoCalibrate;
            CalibrationStepIndex = -1;
            AutoCalibrationStepIndex = -1;
            AutoCalibrationProgress = 0d;
        }
    }
}