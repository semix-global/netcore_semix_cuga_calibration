using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Local.SQL.DB.Providers.Models.Enums;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingRequiredCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingRequiredCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie,
    ISynchronizationContextProvider synchronizationContextProvider,
    ILogger<SettingRequiredCalibrationViewModel> logger) : ViewModelBase
{
    private bool _isLoadSuccess;

    [ObservableProperty]
    public partial ObservableCollection<SettingRequiredCalibrationParam> SettingRequiredCalibrationParamList { get; set; } = [];

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
                    SettingRequiredCalibrationParamList.Clear();
                    foreach (var category in newCategories)
                    {
                        SettingRequiredCalibrationParamList.Add(category);
                    }
                });
                _isLoadSuccess = true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Build Required Calibration Tree Failed!");
            }
        });
    }

    private IReadOnlyList<SettingRequiredCalibrationParam> BuildCategoryTree(CalibrationMenu menuNode)
    {
        var categories = new List<SettingRequiredCalibrationParam>();

        foreach (var child in menuNode.Children)
        {
            var category = new SettingRequiredCalibrationParam
            {
                SysMenu = child.SysMenu
            };

            if (child.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu && child.Entry != CalibrationViewModelEntry.Default)
            {
                var entry = child.Entry;
                var cache = FindInCache(child);

                category.CategoryItem = new SettingRequiredCalibrationCategoryItem
                {
                    Description = entry.Name,
                    AssemblyQualifiedName = entry.DTOType.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false),
                    IsRequired = cache?.CategoryItem.IsRequired ?? false
                };
            }
            else
            {
                category.CategoryItem = new SettingRequiredCalibrationCategoryItem
                {
                    Description = child.SysMenu.Name
                };
            }

            category.Children = BuildCategoryTree(child);
            categories.Add(category);
        }

        return categories.AsReadOnly();
    }

    private SettingRequiredCalibrationParam? FindInCache(CalibrationMenu menuNode)
    {
        foreach (var cache in calibrationSetting.SettingRequiredCalibrationParamList)
        {
            var found = FindRecursive(cache, menuNode);
            if (found is not null) return found;
        }

        return null;

        SettingRequiredCalibrationParam? FindRecursive(SettingRequiredCalibrationParam node, CalibrationMenu target)
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

            calibrationSetting.SettingRequiredCalibrationParamList = [.. SettingRequiredCalibrationParamList.Select(t => t.Clone())];
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saving required status failed! Error:");
            return false;
        }
    }
}