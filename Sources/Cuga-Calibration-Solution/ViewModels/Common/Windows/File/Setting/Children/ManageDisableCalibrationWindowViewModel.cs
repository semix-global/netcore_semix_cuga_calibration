using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Enums;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(ManageDisableCalibrationWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class ManageDisableCalibrationWindowViewModel(
    ICacheProvider cacheProvider,
    ISynchronizationContextProvider contextProvider,
    ILogger<ManageDisableCalibrationWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    SettingDisableCalibrationConfigViewModel settingDisableCalibrationConfigViewModel,
    ApplicationCookie applicationCookie)
    : ViewModelBase
{
    [DefaultCache]
    [ObservableProperty]
    public partial SettingDisableCalibrationConfig[] Caches { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<SettingDisableCalibrationConfig> SettingDisableCalibrationConfigList { get; set; } = [];

    [ObservableProperty]
    public partial SettingDisableCalibrationConfig ConfigItem { get; set; } = new();

    [ObservableProperty]
    public partial SettingDisableCalibrationConfig DefaultItem { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                Caches = cacheProvider.GetOrDefaultArray<SettingDisableCalibrationConfig>();

                if (DefaultItem.CalibrationItems.Count == 0)
                    BuildConfigItem();

                // 如果缓存为空或者与当前的 CalibrationMenu 不一致，则重新构建
                if (Caches.Length == 0 || !ValidateCachesConsistency())
                {
                    // 一次性在UI线程更新所有数据，避免频繁的线程切换
                    contextProvider.Post(() =>
                    {
                        ConfigItem = DefaultItem.Clone();
                        Caches = [DefaultItem];
                        SettingDisableCalibrationConfigList = [.. Caches];
                        cacheProvider.SetArray(Caches, CancellationToken.None);
                    });
                }
                else
                {
                    SettingDisableCalibrationConfigList = [.. Caches];
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Load Failed!");
            dialogWindowProvider.ShowDialog("Load Failed!");
        }
    }

    /// <summary>
    /// 验证缓存数据与 CalibrationMenu 的一致性
    /// </summary>
    /// <returns>true 表示一致，false 表示不一致</returns>
    private bool ValidateCachesConsistency()
    {
        try
        {
            // 如果缓存中没有配置项，返回 false
            if (Caches.Length == 0)
                return false;

            // 获取第一个配置项（正常情况下应该只有一个）
            var cachedConfig = Caches[0];

            // 获取 CalibrationMenu 根节点
            var calibrationMenu = applicationCookie.CalibrationMenu;

            // 递归验证树结构的一致性
            return ValidateMenuNode(calibrationMenu, cachedConfig.CalibrationItems);
        }
        catch (Exception ex)
        {
            // 如果验证过程中出现异常，返回 false，重新构建
            System.Diagnostics.Debug.WriteLine($"Validate Caches Consistency Failed: {ex.Message}");
            return false;
        }

        bool ValidateMenuNode(CalibrationMenu menuNode, IReadOnlyList<SettingDisableCalibrationCategory> cachedCategories)
        {
            // 验证子节点数量是否一致
            if (menuNode.Children.Count != cachedCategories.Count)
                return false;

            // 验证每个子节点
            for (var i = 0; i < menuNode.Children.Count; i++)
            {
                var childMenu = menuNode.Children[i];
                var cachedCategory = cachedCategories.FirstOrDefault(c => c.SysMenu.Id == childMenu.SysMenu.Id);

                if (cachedCategory == null)
                    return false;

                // 验证 SysMenu 信息
                if (cachedCategory.SysMenu.Name != childMenu.SysMenu.Name ||
                    cachedCategory.SysMenu.MenuTypeEnum != childMenu.SysMenu.MenuTypeEnum)
                    return false;

                // 如果是叶子节点（Menu 类型），验证 CategoryItem
                if (childMenu.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu)
                {
                    if (cachedCategory.Item.AssemblyQualifiedName != childMenu.Entry.DTOType.AssemblyQualifiedName)
                        return false;
                }

                // 递归验证子节点
                if (!ValidateMenuNode(childMenu, cachedCategory.Children))
                    return false;
            }

            return true;
        }
    }

    private void BuildConfigItem()
    {
        try
        {
            // 获取 CalibrationMenu 根节点
            var calibrationMenu = applicationCookie.CalibrationMenu;

            if (calibrationMenu.Children.Count == 0)
            {
                logger.LogWarning("CalibrationMenu has no children.");
                return;
            }

            // 递归构建树状结构
            var categories = BuildCategoryTree(calibrationMenu);

            // 创建 ConfigItem
            DefaultItem = new SettingDisableCalibrationConfig
            {
                ConfigDescription = "Disable Calibration Configuration",
                CalibrationItems = categories
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Build ConfigItem Failed!");
        }

        return;

        IReadOnlyList<SettingDisableCalibrationCategory> BuildCategoryTree(CalibrationMenu menuNode)
        {
            var categories = new List<SettingDisableCalibrationCategory>();

            // 遍历所有子节点
            foreach (var child in menuNode.Children)
            {
                var category = new SettingDisableCalibrationCategory
                {
                    SysMenu = child.SysMenu
                };

                // 如果是叶子节点（Menu 类型），创建 CategoryItem
                if (child.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu)
                {
                    category.Item = new()
                    {
                        Description = child.Entry.Name,
                        IsDisable = false,
                        AssemblyQualifiedName = child.Entry.DTOType.AssemblyQualifiedName ?? string.Empty
                    };
                }

                // 递归处理子节点
                category.Children = BuildCategoryTree(child);

                categories.Add(category);
            }

            return categories;
        }
    }

    [RelayCommand]
    private void Add()
    {
        var newItem = DefaultItem.Clone();

        contextProvider.Post(() =>
        {
            SettingDisableCalibrationConfigList = [.. SettingDisableCalibrationConfigList, newItem];
            // 确保新添加的项被选中
            ConfigItem = newItem;
        });
    }

    [RelayCommand]
    private void Remove()
    {
        contextProvider.Post(() => { SettingDisableCalibrationConfigList = [.. SettingDisableCalibrationConfigList.Where(t => t != ConfigItem)]; });
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            SaveCache();

            dialogWindowProvider.ShowDialog("Save Configurations Successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save Configurations Failed!");
            dialogWindowProvider.ShowDialog($"Save Configurations Failed!\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void Edit()
    {
        // 使用 Clone 避免引用问题，确保配置数据的独立性
        settingDisableCalibrationConfigViewModel.ConfigItem = ConfigItem.Clone();

        if (Caches.SingleOrDefault(t => t.ConfigDescription == ConfigItem.ConfigDescription) is null)
            SaveCache();

        CloseView(true);
    }

    [RelayCommand]
    private void CloseDialog()
    {
        CloseView(false);
    }

    private void SaveCache()
    {
        Caches = [.. SettingDisableCalibrationConfigList];

        cacheProvider.SetArray(Caches, CancellationToken.None);
    }
}