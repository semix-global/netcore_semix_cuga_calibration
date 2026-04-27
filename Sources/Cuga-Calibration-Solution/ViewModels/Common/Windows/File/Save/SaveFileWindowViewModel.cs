using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Common.Version;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.File.Setting;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
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
    ICalibrationVersionFactory calibrationVersionFactory) : ViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<CalibrationVersionCategoryGroup> CalibrationVersionCategories { get; set; } = [];

    [RelayCommand]
    private async Task LoadingAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var calibrationVersionHistories = cacheProvider.Gets<CalibrationVersionDTO>(10)
                    .Where(t => t.ResultFilePath != string.Empty);

                var calibrationCategoryList = CalibrationReflectionHelper.GetCalibrationDescriptionList();

                // Build Categories with hierarchy
                CalibrationVersionCategories =
                [
                    .. calibrationCategoryList.Select(t => new CalibrationVersionCategoryGroup(
                        t.Description,
                        [
                            .. t.Items.Select(tt =>
                            {
                                return new CalibrationVersionCategory(
                                    tt.Description.Split('.').Last(),
                                    tt.CalibrationDtoType,
                                    tt.IsArray,
                                    [
                                        .. calibrationVersionHistories
                                            .Select(o => (dto: o, info: o.GetVersionInfo(tt.CalibrationDtoType)))
                                            .Where(x => x.info != null)
                                            .Where(v => SQLiteHelper.GetTableInfo(tt.CalibrationDtoType).Version == GuardExtensions.IsNotNullAndReturn(v.info).Version)
                                            .Select(o => o.dto)
                                    ],
                                    null);
                            })
                        ]))
                ];
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
                            .SelectMany(g => g.Items)
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
        var selected = category.SelectItem?.GetVersionInfo(category.Type);
        if (selected is not null) return selected;

        // 界面如果未选择，则使用数据库最新一条数据
        var versionInfo = category.IsArray
            ? calibrationVersionFactory.CalibrationDTOItemsConvertToVersionInfo(category.Type)
            : calibrationVersionFactory.CalibrationDTOConvertToVersionInfo(category.Type);

        return versionInfo;
    }
}

public record CalibrationVersionCategoryGroup(string Description, IReadOnlyList<CalibrationVersionCategory> Items);

public partial class CalibrationVersionCategory(
    string description,
    Type type,
    bool isArray,
    IReadOnlyList<CalibrationVersionDTO> items,
    CalibrationVersionDTO? selectItem) : ObservableObject
{
    public string Description { get; } = description;
    public Type Type { get; } = type;
    public bool IsArray { get; } = isArray;
    public IReadOnlyList<CalibrationVersionDTO> Items { get; } = items;

    [ObservableProperty]
    public partial CalibrationVersionDTO? SelectItem { get; set; } = selectItem;
}