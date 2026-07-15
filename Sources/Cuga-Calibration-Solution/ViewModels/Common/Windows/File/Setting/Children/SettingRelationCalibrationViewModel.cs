using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Models.Models.Setting.CalibrationRelationConfig;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Enums;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingRelationCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SettingRelationCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ICalibrationRelationService calibrationRelationService,
    ISynchronizationContextProvider synchronizationContextProvider,
    ApplicationCookie applicationCookie,
    ILogger<SettingRelationCalibrationViewModel> logger,
    IWindowManagerService windowManagerService,
    IDialogWindowProvider dialogWindowProvider,
    SettingRelationCalibrationDetailViewModel settingRelationCalibrationDetailViewModel)
    : ViewModelBase
{
    private bool _isLoadSuccess;

    [ObservableProperty]
    public partial ObservableCollection<SettingCalibrationRelationParam> CalibrationRelationConfigs { get; set; } = [];

    [RelayCommand]
    private void OpenDetail(SettingCalibrationRelationParam param)
    {
        if (param.SysMenu.MenuTypeEnum != MenuTypeEnum.Menu) return;

        settingRelationCalibrationDetailViewModel.SelectedParam = param;
        windowManagerService.ShowDialog(settingRelationCalibrationDetailViewModel);
    }

    [RelayCommand]
    private void ClearDependencyConfig()
    {
        if (dialogWindowProvider.TryShowDialog("Are you sure you want to clear all dependency configurations?", out var result, DialogButtonsEnum.YesNo, DialogIconEnum.Question) != true) return;
        if (result != DialogResultEnum.Yes) return;

        foreach (var config in CalibrationRelationConfigs)
        {
            foreach (var child in config.GetAllChildren())
            {
                UncheckAll(child.Item.DependencyRelationConfigs);
            }
        }
    }

    private static void UncheckAll(IEnumerable<SettingCalibrationRelationConfig> configs)
    {
        foreach (var config in configs)
        {
            config.Item.IsUsed = false;
            UncheckAll(config.Children);
        }
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (_isLoadSuccess) return;

                var newCategories = BuildCategoryTree(applicationCookie.CalibrationMenu);

                synchronizationContextProvider.Post(() =>
                {
                    CalibrationRelationConfigs.Clear();
                    foreach (var category in newCategories)
                    {
                        CalibrationRelationConfigs.Add(category);
                    }
                });
                _isLoadSuccess = true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Build Calibration Tree Failed!");
            }
        });
    }

    private IReadOnlyList<SettingCalibrationRelationParam> BuildCategoryTree(CalibrationMenu menuNode)
    {
        var categories = new List<SettingCalibrationRelationParam>();

        foreach (var child in menuNode.Children)
        {
            var item = new SettingCalibrationRelationCategoryItem();

            var cacheParam = FindInCache(child);
            item.DependencyRelationConfigs = new ObservableCollection<SettingCalibrationRelationConfig>(BuildRelationConfigTrees(applicationCookie.CalibrationMenu.Children, cacheParam));

            var category = new SettingCalibrationRelationParam
            {
                SysMenu = child.SysMenu,
                Item = item,
                Children = BuildCategoryTree(child)
            };

            categories.Add(category);
        }

        return categories.AsReadOnly();
    }

    private IReadOnlyList<SettingCalibrationRelationConfig> BuildRelationConfigTrees(IReadOnlyList<CalibrationMenu> menuNodes, SettingCalibrationRelationParam? cacheParam)
    {
        var configs = new List<SettingCalibrationRelationConfig>();
        foreach (var menuNode in menuNodes)
        {
            configs.Add(BuildRelationConfigTree(menuNode));
        }
        return configs.AsReadOnly();

        SettingCalibrationRelationConfig BuildRelationConfigTree(CalibrationMenu menuNode)
        {
            var cacheConfig = FindConfigInTrees(cacheParam?.Item.DependencyRelationConfigs ?? [], menuNode.SysMenu.Id);

            var entry = menuNode.Entry;
            var isDefaultEntry = entry == CalibrationViewModelEntry.Default;
            var assemblyQualifiedName = isDefaultEntry
                ? null
                : entry.DTOType.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false);

            var isArray = false;
            if (isDefaultEntry == false)
            {
                ApplicationCookie.CalibrationViewModelEntries.TryGetValue(entry.ViewModelType, out var viewModelEntry);
                isArray = viewModelEntry?.IsArray ?? entry.IsArray;
            }

            var config = new SettingCalibrationRelationConfig
            {
                SysMenu = menuNode.SysMenu,
                Item = new SettingCalibrationRelationConfigItem
                {
                    Description = isDefaultEntry ? menuNode.SysMenu.Name : entry.Name,
                    AssemblyQualifiedName = assemblyQualifiedName ?? string.Empty,
                    IsUsed = cacheConfig?.Item.IsUsed ?? false,
                    IsArray = isArray
                }
            };

            var children = new List<SettingCalibrationRelationConfig>();
            foreach (var child in menuNode.Children)
            {
                children.Add(BuildRelationConfigTree(child));
            }

            config.Children = children.AsReadOnly();
            return config;
        }
    }

    private static SettingCalibrationRelationConfig? FindConfigInTrees(IReadOnlyList<SettingCalibrationRelationConfig> roots, long sysMenuId)
    {
        foreach (var root in roots)
        {
            var found = FindRecursive(root, sysMenuId);
            if (found is not null) return found;
        }
        return null;

        static SettingCalibrationRelationConfig? FindRecursive(SettingCalibrationRelationConfig node, long targetId)
        {
            if (node.SysMenu.Id == targetId) return node;
            foreach (var child in node.Children)
            {
                var result = FindRecursive(child, targetId);
                if (result is not null) return result;
            }
            return null;
        }
    }

    private SettingCalibrationRelationParam? FindInCache(CalibrationMenu menuNode)
    {
        foreach (var cache in calibrationSetting.SettingCalibrationRelationParams)
        {
            var found = FindRecursive(cache, menuNode);
            if (found is not null) return found;
        }

        return null;

        SettingCalibrationRelationParam? FindRecursive(SettingCalibrationRelationParam node, CalibrationMenu target)
        {
            if (node.SysMenu.Id == target.SysMenu.Id) return node;
            foreach (var child in node.Children)
            {
                var result = FindRecursive(child, target);
                if (result is not null) return result;
            }

            return null;
        }
    }

    public async Task<bool> SavingAsync()
    {
        try
        {
            await Task.CompletedTask.ConfigureAwait(false);

            calibrationSetting.SettingCalibrationRelationParams = [.. CalibrationRelationConfigs.Select(t => t.Clone())];

            if (calibrationRelationService.RefreshRelationConfigCookies(out var message) == false)
            {
                dialogWindowProvider.ShowDialog($"Refresh Relation Config Cookies Failed: {message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saving required status failed! Error:");
            return false;
        }
    }

    public void Closing()
    {
        _isLoadSuccess = false;
    }
}