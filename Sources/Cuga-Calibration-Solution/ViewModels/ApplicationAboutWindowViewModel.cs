using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Utilities;
using Core.Utilities.WPF.ApplicationAbout.Helper;
using Core.Utilities.WPF.ApplicationAbout.Model;
using Core.Utilities.WPF.Assembly.Helper;
using Core.Utilities.WPF.Assembly.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using SourceGenerator.AssemblyMetadata;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(ApplicationAboutWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ApplicationAboutWindowViewModel : ViewModelBase
{
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IOptions<ApplicationSetting> _options;
    private readonly ILogger<ApplicationAboutWindowViewModel> _logger;

    [ObservableProperty]
    private ApplicationInfo _applicationInfo = new();

    [ObservableProperty]
    private OperatingSystemInfo _operatingSystemInfo = new();

    [ObservableProperty]
    private ObservableCollection<SystemInfo> _systemInfoList = [];

    [ObservableProperty]
    private ObservableCollection<AssemblyInfo> _assemblyList = [];

    public ApplicationAboutWindowViewModel(IDialogWindowProvider dialogWindowProvider, IOptions<ApplicationSetting> options, ILogger<ApplicationAboutWindowViewModel> logger)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _options = options;
        _logger = logger;
        try
        {
            #region assembly info

            var version = Platform.GetInstalledRuntimeVersion();
            if (version is not null)
                ApplicationInfo.RuntimeVersion = version.ToString();

            ApplicationInfo.OutPutPath = AppDomain.CurrentDomain.BaseDirectory;
            ApplicationInfo.VersionInfo.Metadata = new Metadata
            {
                ProjectName = CugaCalibrationSolutionAssemblyMetadata.Product,
                Configuration = CugaCalibrationSolutionAssemblyMetadata.Configuration,
                Version = CugaCalibrationSolutionAssemblyMetadata.Version,
                NeutralResourcesLanguage = CugaCalibrationSolutionAssemblyMetadata.NeutralResourcesLanguage,
                InformationalVersion = CugaCalibrationSolutionAssemblyMetadata.InformationalVersion
            };
            ApplicationInfo.VersionInfo = AssemblyVersionGenerator.GenerateAssemblyVersionsJson(ApplicationInfo);
            AssemblyList = new ObservableCollection<AssemblyInfo>(ApplicationInfo.VersionInfo.Assemblies);

            #endregion assembly info

            #region system info

            var osArchName = RuntimeInformation.OSArchitecture.ToString().ToUpperInvariant();
            OperatingSystemInfo.OperatingSystem = RuntimeInformation.OSDescription;
            OperatingSystemInfo.OperatingSystemVersion = $" ({Environment.OSVersion.Version}, {osArchName})";

            SystemInfoList =
            [
                new SystemInfo { SystemName = nameof(OperatingSystemInfo.DeviceName), SystemInformation = OperatingSystemInfo.DeviceName },
                new SystemInfo { SystemName = nameof(OperatingSystemInfo.OperatingSystem), SystemInformation = OperatingSystemInfo.OperatingSystem },
                new SystemInfo { SystemName = nameof(OperatingSystemInfo.OperatingSystemVersion), SystemInformation = OperatingSystemInfo.OperatingSystemVersion },
                new SystemInfo { SystemName = nameof(OperatingSystemInfo.ProcessorCount), SystemInformation = OperatingSystemInfo.ProcessorCount.ToString() },
                new SystemInfo { SystemName = nameof(ApplicationInfo.RuntimeVersion), SystemInformation = ApplicationInfo.RuntimeVersion }
            ];

            #endregion system info
        }
        catch (Exception e)
        {
            dialogWindowProvider.ShowDialog("Application About Window ViewModel Init Error", e.Message, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            _logger.LogError(e, "Application About Window ViewModel Init Error");
        }
    }

    [RelayCommand]
    private void ShowUpdateMarkDown()
    {
        try
        {
            // todo: hanqi markdown 编译到程序集
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var index = baseDirectory.IndexOf(ApplicationInfo.VersionInfo.Metadata.ProjectName, StringComparison.Ordinal);
            var projDirectory = string.Concat(baseDirectory.Take(index));
            var solutionDirectory = Path.GetFullPath(Path.Combine(projDirectory, @"..\"));
            var documentPath = Path.Combine(solutionDirectory, _options.Value.UpdateDocumentPath);

            using var _ = Process.Start(documentPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Show Update MarkDown Error");
        }
    }

    [RelayCommand]
    private void VersionUpdate()
    {
        _dialogWindowProvider.ShowDialog("Please download the installation exe in the shared folder and install it to update the version!");
    }

    [RelayCommand]
    private void OutputAssemblyInfoFile()
    {
        try
        {
            var tryShowSaveFilePathDialog = _dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var saveDirectory);
            if (tryShowSaveFilePathDialog == false) return;

            FileHelper.SerializeOperate(ApplicationInfo.VersionInfo, Path.Combine(saveDirectory, $"CurrentAssemblyVersions{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.json"));
            _dialogWindowProvider.ShowDialog($"Output Assembly Info File Success!\nPath:{saveDirectory}");
        }
        catch (Exception e)
        {
            _dialogWindowProvider.ShowDialog("Output Assembly Info File Error", e.Message, DialogButtonsEnum.OK, DialogIconEnum.Error);
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(false);
    }

    public partial class SystemInfo : ObservableObject
    {
        [ObservableProperty]
        private string _systemName = string.Empty;

        [ObservableProperty]
        private string _systemInformation = string.Empty;
    }
}