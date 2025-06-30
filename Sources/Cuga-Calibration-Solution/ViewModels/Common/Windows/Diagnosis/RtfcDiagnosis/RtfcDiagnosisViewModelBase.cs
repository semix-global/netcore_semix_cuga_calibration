using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis.RtfcDiagonosis;

[IOCAppService(ServiceType = typeof(RtfcDiagnosisViewModelBase), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class RtfcDiagnosisViewModelBase : ViewModelBase
{
    protected readonly ICalibrationStatusService CalibrationStatusService;
    protected readonly ICalibrationAlgorithmService CalibrationAlgorithmService;
    protected readonly IMessenger Messenger;
    protected readonly IHostEnvironment HostEnvironment;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly ILogger<RtfcDiagnosisViewModelBase> Logger;
    protected readonly IOptions<ApplicationSetting> Options;
    protected readonly ISynchronizationContextProvider SynchronizationContextProvider;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IWindowManagerService WindowManagerService;
    private readonly string _typeName;

    #region 属性

    /// <summary>
    /// 校准图片路径名称
    /// </summary>
    public virtual string CalibrateDirectoryName { get; set; } = string.Empty;

    /// <summary>
    /// 日志唯一标识
    /// </summary>
    public Guid HtmlLogUniqueId { get; internal set; }

    public string Name => LogHtmlFileName;

    /// <summary>
    /// Csv文件存储名称前缀
    /// </summary>
    public string CsvFileDirectory => Path.Combine(Options.Value.AppHomeDirectory, "Csv", "Diagnosis", _typeName, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName));

    /// <summary>
    /// 校准名称
    /// </summary>
    /// <summary>
    /// 日志图片存储位置
    /// </summary>
    public string ImageFileDirectory => Path.Combine(Options.Value.AppHomeDirectory, "Images", _typeName, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    /// <summary>
    /// 模板存储位置
    /// </summary>m
    public string TemplateFileDirectory => Path.Combine(Options.Value.AppHomeDirectory, "Template", _typeName, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    /// <summary>
    /// Csv文件存储名称前缀
    /// </summary>
    public virtual string LogHtmlFileName { get; set; } = string.Empty;

    /// <summary>
    /// 校准文件名称
    /// </summary>
    public string DiagnosisHtmlLogFileName => string.IsNullOrWhiteSpace(LogHtmlFileName) ? "Diagnosis" : $"Diagnosis-{FileHelper.RemoveInvalidFileName(LogHtmlFileName)}";

    #region ViewModel

    [ObservableProperty]
    private AfViewModel _afViewModel = HostApplication.GetRequiredService<AfViewModel>();

    [ObservableProperty]
    private LaserViewModel _laserViewModel = HostApplication.GetRequiredService<LaserViewModel>();

    [ObservableProperty]
    private MicroscopeViewModel _microscopeViewModel = HostApplication.GetRequiredService<MicroscopeViewModel>();

    [ObservableProperty]
    private ReviewViewModel _reviewViewModel = HostApplication.GetRequiredService<ReviewViewModel>();

    [ObservableProperty]
    private StageViewModel _stageViewModel = HostApplication.GetRequiredService<StageViewModel>();

    [ObservableProperty]
    private MonitorViewModel _monitorViewModel = HostApplication.GetRequiredService<MonitorViewModel>();

    #endregion

    #region 界面

    [ObservableProperty]
    private bool _isEnableWindow = true;

    #endregion 界面

    #endregion 属性

    public RtfcDiagnosisViewModelBase()
    {
        _typeName = GetType().Name;
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        Logger = (ILogger<RtfcDiagnosisViewModelBase>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        Options = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>();
        SynchronizationContextProvider = HostApplication.GetRequiredService<ISynchronizationContextProvider>();
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        CalibrationStatusService = HostApplication.GetRequiredService<ICalibrationStatusService>();
        CalibrationAlgorithmService = HostApplication.GetRequiredService<ICalibrationAlgorithmService>();
        Messenger = HostApplication.GetRequiredService<IMessenger>();
        HostEnvironment = HostApplication.GetRequiredService<IHostEnvironment>();
        WindowManagerService = HostApplication.GetRequiredService<IWindowManagerService>();

        Messenger.RegisterAll(this);
    }

    #region Command

    public async Task ActionAsync(CancellationToken cancellationToken)
    {
        var result = await DiagnosisActionAsync(cancellationToken).ConfigureAwait(false);
        if (result == false)
            DialogWindowProvider.ShowDialog("Diagnosis Action Failed! ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    public async Task SaveAsync()
    {
        await Task.Run(async () =>
        {
            var result = await SavingAsync().ConfigureAwait(false);
            DialogWindowProvider.ShowDialog($"Saving {(result ? "Ok" : "Failed")}! ", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return;
        }).ConfigureAwait(false);
    }

    #endregion Command

    #region 重载

    public virtual Task<bool> LoadingAsync() => Task.FromResult(true);

    public virtual Task<bool> SavingAsync() => Task.FromResult(true);

    public virtual Task<bool> DiagnosisActionAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    #endregion 重载
}