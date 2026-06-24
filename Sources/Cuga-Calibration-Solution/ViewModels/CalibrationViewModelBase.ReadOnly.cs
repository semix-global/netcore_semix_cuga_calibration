using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using System.IO;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase
{
    #region IOC

    public ILogger<CalibrationViewModelBase> Logger { get; }

    public CalibrationViewModelEntry Entry { get; }

    public IMessenger Messenger { get; } = HostApplication.GetRequiredService<IMessenger>();

    public IHostEnvironment HostEnvironment { get; } = HostApplication.GetRequiredService<IHostEnvironment>();

    public IDialogWindowProvider DialogWindowProvider { get; } = HostApplication.GetRequiredService<IDialogWindowProvider>();

    public IWindowManagerService WindowManagerService { get; } = HostApplication.GetRequiredService<IWindowManagerService>();

    public ISynchronizationContextProvider SynchronizationContextProvider { get; } = HostApplication.GetRequiredService<ISynchronizationContextProvider>();

    public ICalibrationAlgorithmService CalibrationAlgorithmService { get; } = HostApplication.GetRequiredService<ICalibrationAlgorithmService>();

    public ICacheProvider CacheProvider { get; } = HostApplication.GetRequiredService<ICacheProvider>();

    public ICacheProvider RecipeCacheProvider { get; } = HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey);

    public IApplicationCookieService ApplicationCookieService { get; } = HostApplication.GetRequiredService<IApplicationCookieService>();

    public ICalibrationCacheProvider CalibrationCacheProvider { get; } = HostApplication.GetRequiredService<ICalibrationCacheProvider>();

    public ICalibrationRecipeService CalibrationRecipeService { get; } = HostApplication.GetRequiredService<ICalibrationRecipeService>();

    public ApplicationSetting ApplicationSetting { get; } = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;

    public ApplicationCookie ApplicationCookie { get; } = HostApplication.GetRequiredService<ApplicationCookie>();

    public RecipeCookie RecipeCookie { get; } = HostApplication.GetRequiredService<RecipeCookie>();

    public CalibrationSetting CalibrationSetting { get; } = HostApplication.GetRequiredService<CalibrationSetting>();

    #region Common Viewmodel

    public AdsViewModel AdsViewModel { get; } = HostApplication.GetRequiredService<AdsViewModel>();

    public AfViewModel AfViewModel { get; } = HostApplication.GetRequiredService<AfViewModel>();

    public EFEMViewModel EfemViewModel { get; } = HostApplication.GetRequiredService<EFEMViewModel>();

    public LaserViewModel LaserViewModel { get; } = HostApplication.GetRequiredService<LaserViewModel>();

    public MicroscopeViewModel MicroscopeViewModel { get; } = HostApplication.GetRequiredService<MicroscopeViewModel>();

    public ReviewViewModel ReviewViewModel { get; } = HostApplication.GetRequiredService<ReviewViewModel>();

    public StageViewModel StageViewModel { get; } = HostApplication.GetRequiredService<StageViewModel>();

    public FourierViewModel FourierViewModel { get; } = HostApplication.GetRequiredService<FourierViewModel>();

    public OpticsViewModel OpticsViewModel { get; } = HostApplication.GetRequiredService<OpticsViewModel>();

    public CIBViewModel CIBViewModel { get; } = HostApplication.GetRequiredService<CIBViewModel>();

    public ConfigViewModel ConfigureViewModel { get; } = HostApplication.GetRequiredService<ConfigViewModel>();

    public MonitorViewModel MonitorViewModel { get; } = HostApplication.GetRequiredService<MonitorViewModel>();

    #endregion

    #endregion

    #region 配置

    public string Name => Entry.Name;

    public string AppHomeDirectory => ApplicationSetting.AppHomeDirectory;

    public CalibrationRecipeDTO CalibrationRecipeDto => RecipeCookie.CalibrationRecipeDto;

    public string ImageFileDirectory => Path.Combine(AppHomeDirectory, "Images", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string TemplateFileDirectory => Path.Combine(AppHomeDirectory, "Template", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string CsvFileDirectory => Path.Combine(AppHomeDirectory, "Csv", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string AODWaveformDirectoryPath => Path.Combine(AppHomeDirectory, "AODWaveform", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ResultAODWaveformDirectoryPath => Path.Combine(AppHomeDirectory, "Result", "AODWaveform", GetType().Name, DirectoryHelper.RemoveInvalidDirectoryName(CalibrateDirectoryName), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string CalibrateHtmlLogFileName => string.IsNullOrWhiteSpace(CalibrateFileName) ? "Calibrate" : $"Calibrate-{FileHelper.RemoveInvalidFileName(CalibrateFileName)}";

    public string VerifyHtmlFileLogName => string.IsNullOrWhiteSpace(VerifyFileName) ? "Verify" : $"Verify-{FileHelper.RemoveInvalidFileName(VerifyFileName)}";

    public bool IsCalibrated => Entry.Status.IsCalibrated;

    #endregion

    protected CalibrationViewModelBase()
    {
        Logger = (ILogger<CalibrationViewModelBase>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        Entry = ApplicationCookie.CalibrationViewModelEntries[GetType()];
    }
}