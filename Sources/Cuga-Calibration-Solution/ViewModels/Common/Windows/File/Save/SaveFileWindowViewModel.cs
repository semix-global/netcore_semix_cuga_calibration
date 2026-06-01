using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Version;
using Core.Utilities.WPF.Assembly.Model;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.File.Setting;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.File.Save;

[IOCAppService(ServiceType = typeof(SaveFileWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class SaveFileWindowViewModel(
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<SettingWindowViewModel> logger,
    ICalibrationCacheProvider calibrationCacheProvider,
    ICalibrationVersionFactory calibrationVersionFactory,
    ApplicationCookie applicationCookie) : ViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<CalibrationVersionCategory> CalibrationVersionCategories { get; set; } = [];

    [RelayCommand]
    private async Task LoadingAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var calibrationVersionHistories = cacheProvider.Gets<CalibrationVersionDTO>(10)
                    .Where(t => t.ResultFilePath != string.Empty)
                    .DistinctBy(t => t.ResultFilePath);

                // Build Categories with hierarchy
                CalibrationVersionCategories = BuildCategoryTree(applicationCookie.CalibrationMenu, [.. calibrationVersionHistories]);
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Restore Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Restore Setting");
            }
        }).ConfigureAwait(false);
    }

    private IReadOnlyList<CalibrationVersionCategory> BuildCategoryTree(CalibrationMenu menuNode, IReadOnlyList<CalibrationVersionDTO> calibrationVersionHistories)
    {
        var categories = new List<CalibrationVersionCategory>();

        foreach (var child in menuNode.Children)
        {
            var category = new CalibrationVersionCategory
            {
                SysMenu = child.SysMenu
            };

            if (child.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu && child.Entry != CalibrationViewModelEntry.Default)
            {
                var entry = child.Entry;

                category.CategoryItem = new CalibrationVersionCategoryItem
                {
                    Description = child.SysMenu.Name,
                    IsArray = entry.IsArray,
                    AssemblyQualifiedName = entry.DTOType.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false),
                    Items =
                    [
                        .. calibrationVersionHistories
                            .Select(o => (dto: o, info: o.GetVersionInfo(entry.DTOType)))
                            .Where(x => x.info != null)
                            .Where(v => SQLiteHelper.GetTableInfo(entry.DTOType).Version == GuardExtensions.IsNotNullAndReturn(v.info).Version)
                            .Select(o => o.dto)
                    ],
                };
            }
            else
            {
                category.CategoryItem = new CalibrationVersionCategoryItem
                {
                    Description = child.SysMenu.Name
                };
            }

            category.Children = BuildCategoryTree(child, calibrationVersionHistories);
            categories.Add(category);
        }

        return categories.AsReadOnly();
    }

    [RelayCommand]
    private async Task SavingAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                // 组装
                var calibrationVersionDTO = new CalibrationVersionDTO
                {
                    ResultFilePath = $"Result_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.dat",
                    CalibrationVersionInfos =
                    [
                        ..
                        CalibrationVersionCategories
                            .SelectMany(g => g.GetAllChildren())
                            .Select(ResolveVersionInfo)
                            .Where(t => t != null)
                            .Select(t => GuardExtensions.IsNotNullAndReturn(t))
                    ]
                };

                cacheProvider.Set(calibrationVersionDTO, CancellationToken.None);

                if (await calibrationCacheProvider.TrySaveAsync(calibrationVersionDTO, CancellationToken.None) == false)
                {
                    dialogWindowProvider.ShowDialog("Save Calibration Result File Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                CloseView(true);
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Save Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Save Calibration Result File.");
            }
        });
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }

    /// <summary>
    /// 返回指定校准type的versionInfo，用于组装
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    private CalibrationVersionDTO.VersionInfo? ResolveVersionInfo(CalibrationVersionCategory category)
    {
        var selected = category.CategoryItem.SelectItem?.GetVersionInfo(Guard.IsNotNullAndReturn(category.CategoryItem.TypeInstance));
        if (selected is not null) return selected;

        // 界面如果未选择，则使用数据库最新一条数据
        var versionInfo = category.CategoryItem.IsArray
            ? calibrationVersionFactory.CalibrationDTOItemsConvertToVersionInfo(Guard.IsNotNullAndReturn(category.CategoryItem.TypeInstance))
            : calibrationVersionFactory.CalibrationDTOConvertToVersionInfo(Guard.IsNotNullAndReturn(category.CategoryItem.TypeInstance));

        return versionInfo;
    }
}

public sealed partial class CalibrationVersionCategory : ObservableObject
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    private CalibrationVersionCategoryItem _categoryItem = new();

    [ObservableProperty]
    public partial IReadOnlyList<CalibrationVersionCategory> Children { get; set; } = [];

    public string Name => SysMenu.Name;

    public IReadOnlyList<CalibrationVersionCategory> GetAllChildren()
    {
        var result = new List<CalibrationVersionCategory>();

        RecursionFn(this);

        return result;

        void RecursionFn(CalibrationVersionCategory item)
        {
            if (item.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu) result.Add(item);

            foreach (var child in item.Children) RecursionFn(child);
        }
    }
}

public partial class CalibrationVersionCategoryItem : TypeInfo
{
    [ObservableProperty]
    public partial bool IsArray { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CalibrationVersionDTO> Items { get; set; } = [];

    [ObservableProperty]
    public partial CalibrationVersionDTO? SelectItem { get; set; }
}