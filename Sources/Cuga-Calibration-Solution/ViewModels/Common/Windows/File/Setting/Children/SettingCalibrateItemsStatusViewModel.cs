using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Wcf.Models;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingCalibrateItemsStatusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingCalibrateItemsStatusViewModel : ViewModelBase
{
    private readonly IMessenger _messenger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IGetResultFileService _getResultFileService;
    private readonly ILogger<SettingCalibrateItemsStatusViewModel> _logger;
    private readonly ICacheProvider _cacheProvider;
    private readonly ISynchronizationContextProvider _synchronizationContextProvider;
    private readonly ICalibrationCacheProvider _calibrationCacheProviderService;
    private readonly ICalibrationVersionFactory _calibrationVersionFactory;
    private readonly ConfigViewModel _configViewModel;
    private readonly ApplicationCookie _applicationCookie;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IApplicationCookieService _applicationCookieService;
    private bool _isLoadSuccess;

    /// <summary>
    /// 反序列化wcf对象
    /// </summary>
    private CalibrationObj? _calibrationObj;

    [ObservableProperty]
    public partial ObservableCollection<CalibrationCategory> CalibrationCategories { get; set; } = [];

    public SettingCalibrateItemsStatusViewModel(
        IMessenger messenger,
        IDialogWindowProvider dialogWindowProvider,
        IGetResultFileService getResultFileService,
        ILogger<SettingCalibrateItemsStatusViewModel> logger,
        ICacheProvider cacheProvider,
        ISynchronizationContextProvider synchronizationContextProvider,
        ICalibrationCacheProvider calibrationCacheProviderService,
        ICalibrationVersionFactory calibrationVersionFactory,
        ConfigViewModel configureViewModel,
        ApplicationCookie applicationCookie,
        IWindowManagerService windowManagerService,
        IApplicationCookieService applicationCookieService)
    {
        _messenger = messenger;
        _dialogWindowProvider = dialogWindowProvider;
        _getResultFileService = getResultFileService;
        _logger = logger;
        _cacheProvider = cacheProvider;
        _synchronizationContextProvider = synchronizationContextProvider;
        _calibrationCacheProviderService = calibrationCacheProviderService;
        _calibrationVersionFactory = calibrationVersionFactory;
        _configViewModel = configureViewModel;
        _applicationCookie = applicationCookie;
        _windowManagerService = windowManagerService;
        _applicationCookieService = applicationCookieService;

        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var appliedFilePath = _configViewModel.GetAppliedCalibrateResultFilePath();
                _getResultFileService.SetResultFilePath(appliedFilePath);

                // 每次切换都重新加载
                if (_getResultFileService.TryGet<CalibrationObj>(out var tempObj) == false)
                {
                    _dialogWindowProvider.ShowDialog("Load Calibration Result File Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                _calibrationObj = tempObj;

                // 询问是否使用一键禁用配置
                SettingDisableCalibrationConfig? selectedConfig = null;
                _synchronizationContextProvider.Send(() =>
                {
                    if (_dialogWindowProvider.TryShowDialog("Do you want to use one-click disable configuration?",
                            out var result, DialogButtonsEnum.YesNo, DialogIconEnum.Question) == true
                        && result == DialogResultEnum.Yes)
                    {
                        selectedConfig = ShowConfigSelectionDialog();
                    }
                });

                if (GetCalibrationObjIsOkStatus(selectedConfig) == false) return;

                _isLoadSuccess = true;
            }
            catch (Exception ex)
            {
                _isLoadSuccess = false;
                _logger.LogError(ex, "Get applied calibrate result file path failed!");
                _dialogWindowProvider.ShowDialog("Get applied calibrate result file path failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
        return;

        bool GetCalibrationObjIsOkStatus(SettingDisableCalibrationConfig? config = null)
        {
            try
            {
                // 从 CalibrationMenu 构建树状结构
                var newCategories = BuildCategoryTree(_applicationCookie.CalibrationMenu, config);

                // 一次性在UI线程更新所有数据，避免频繁的线程切换
                _synchronizationContextProvider.Post(() =>
                {
                    CalibrationCategories.Clear();
                    foreach (var category in newCategories)
                    {
                        CalibrationCategories.Add(category);
                    }
                });

                return true;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration expired!");
                _dialogWindowProvider.ShowDialog($"Configuration expired：{ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get CalibrationObj IsOk Status Failed!");
                return false;
            }
        }
    }

    private SettingDisableCalibrationConfig? ShowConfigSelectionDialog()
    {
        var configs = _cacheProvider.GetOrDefaultArray<SettingDisableCalibrationConfig>();
        if (configs.Length == 0)
        {
            _dialogWindowProvider.ShowDialog("Find Calibration Disable Config Failed,Please Create One First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return null;
        }

        var viewModel = HostApplication.GetRequiredService<SelectDisableConfigViewModel>();

        // 先加载数据
        viewModel.LoadedCommand.Execute(null);

        var result = _windowManagerService.ShowDialog(viewModel);

        return result == true ? viewModel.SelectedConfig : null;
    }

    private static IReadOnlyList<CalibrationCategory> BuildCategoryTree(CalibrationMenu menuNode, SettingDisableCalibrationConfig? config)
    {
        var rootConfigCategories = config?.CalibrationItems ?? [];

        List<CalibrationCategory> BuildRecursive(CalibrationMenu node, SettingDisableCalibrationCategory? parentConfigCategory)
        {
            var categories = new List<CalibrationCategory>();

            foreach (var child in node.Children)
            {
                // 配置匹配
                SettingDisableCalibrationCategory? matchedConfigCategory = null;
                if (config != null)
                {
                    var configChildren = parentConfigCategory?.Children ?? rootConfigCategories;
                    matchedConfigCategory = configChildren.FirstOrDefault(c => c.SysMenu.Id == child.SysMenu.Id);

                    if (matchedConfigCategory == null)
                    {
                        throw new InvalidOperationException($"Node '{child.SysMenu.Name}' (ID: {child.SysMenu.Id}) is not matched with any configuration item.");
                    }
                }

                var category = new CalibrationCategory
                {
                    SysMenu = child.SysMenu
                };

                // 叶子节点状态填充
                if (child.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu && child.Entry != CalibrationViewModelEntry.Default)
                {
                    var entry = child.Entry;

                    // 判断是否应用配置强制禁用
                    bool isAnyOk;
                    bool forceDisabled = false; // 标记是否被配置强制禁用

                    if (matchedConfigCategory?.CategoryItem.IsDisable == true)
                    {
                        isAnyOk = false; // 强制禁用
                        forceDisabled = true;
                    }
                    else
                    {
                        isAnyOk = entry.IsArray
                            ? entry.Cookie.Calibrations.Length > 0
                            : entry.Cookie.Calibration.IsCalibrated;
                    }

                    category.CategoryItem = new CalibrationCategoryItem
                    {
                        Description = entry.Name,
                        IsAnyOk = isAnyOk,
                        IsArray = entry.IsArray,
                        Type = entry.DTOType,
                        IsForceDisabled = forceDisabled // 传递强制禁用标记
                    };
                }
                else
                {
                    // 目录节点使用默认的 CategoryItem
                    category.CategoryItem = new CalibrationCategoryItem
                    {
                        Description = child.SysMenu.Name,
                        IsAnyOk = false,
                        IsArray = false,
                        Type = null
                    };
                }

                // 递归子节点
                category.Children = BuildRecursive(child, matchedConfigCategory).AsReadOnly();
                categories.Add(category);
            }

            return categories;
        }

        return BuildRecursive(menuNode, null).AsReadOnly();
    }

    public Task<bool> SavingAsync()
    {
        return Task.Run(async () =>
        {
            if (_isLoadSuccess == false) return true; // 未加载缓存成功，不保存

            // 获取所有叶子节点并检查是否有修改
            var allLeafNodes = CalibrationCategories.SelectMany(c => c.GetAllChildren()).ToList();
            var isChanged = allLeafNodes.Any(leafNode => leafNode.CategoryItem.IsChanged);

            if (isChanged == false) return true; // 未修改，不保存

            var showDialog = _dialogWindowProvider.TryShowDialog("Do you want to save the calibration enabled status settings? " +
                                                                 "  PS:If you select Yes, the calibration cache will be overwritten by Cuga's currently applied calibration file!",
                out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            if (showDialog == false || dialogResultEnum == DialogResultEnum.No)
            {
                Closing();
                return true;
            }

            Guard.IsNotNull(_calibrationObj);

            // 备份result
            if (_getResultFileService.TrySaveBackUp(_calibrationObj) == false)
            {
                _logger.LogError("Save BackUp Result File Failed!");
                return false;
            }

            // 禁用项写入db
            foreach (var leafNode in allLeafNodes)
            {
                leafNode.CategoryItem.Save(_applicationCookieService, CancellationToken.None);
            }

            // 序列化覆盖原先的result
            var appliedFilePath = _configViewModel.GetAppliedCalibrateResultFilePath();

            var calibrationVersionDTO = _calibrationVersionFactory.CreateInstanceFromCurrentDatabase(Path.GetFileName(appliedFilePath));

            var save = await _calibrationCacheProviderService.TrySaveAsync(calibrationVersionDTO, CancellationToken.None);
            if (save)
                _dialogWindowProvider.ShowDialog("Save Success.");
            else
                _dialogWindowProvider.ShowDialog("Save Failed! Please save it again.", DialogButtonsEnum.OK, DialogIconEnum.Error);

            return true;
        });
    }

    public bool Closing()
    {
        _calibrationObj = null;

        return true;
    }
}

public sealed partial class CalibrationCategory : ObservableObject
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    public partial CalibrationCategoryItem CategoryItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CalibrationCategory> Children { get; set; } = [];

    public string Name => SysMenu.Name;

    public IReadOnlyList<CalibrationCategory> GetAllChildren()
    {
        var result = new List<CalibrationCategory>();

        RecursionFn(this);

        return result;

        void RecursionFn(CalibrationCategory item)
        {
            if (item.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu) result.Add(item);

            foreach (var child in item.Children) RecursionFn(child);
        }
    }
}

public partial class CalibrationCategoryItem : ObservableObject
{
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAnyOk { get; set; }

    public bool IsArray { get; init; }
    public Type? Type { get; init; }

    /// <summary>
    /// 标记是否被配置强制禁用
    /// </summary>
    private bool _isForceDisabled;

    public bool IsForceDisabled
    {
        get => _isForceDisabled;
        init
        {
            _isForceDisabled = value;
            // 如果是强制禁用，标记为已修改
            if (value)
            {
                IsChanged = true;
            }
        }
    }

    public bool IsChanged { get; private set; }

    partial void OnIsAnyOkChanged(bool value)
    {
        if (value == false) IsChanged = true;
        else IsAnyOk = true;
    }

    public void Save(IApplicationCookieService applicationCookieService, CancellationToken cancellationToken)
    {
        if (IsChanged == false) return;

        if (IsArray)
        {
            if (Type is not null)
                applicationCookieService.SetCalibrations(Type, Guard.IsNotNullAndReturn(Array.CreateInstance(Type, 0) as CalibrationDTOBase[]), cancellationToken);
        }
        else
        {
            if (Type is not null)
                applicationCookieService.SetCalibration(Type, Guard.IsNotNullAndReturn(Activator.CreateInstance(Type) as CalibrationDTOBase), cancellationToken);
        }
    }
}