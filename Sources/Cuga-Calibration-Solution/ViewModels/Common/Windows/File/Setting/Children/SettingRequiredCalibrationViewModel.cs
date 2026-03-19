using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Setting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using CommunityToolkit.Diagnostics;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingRequiredCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingRequiredCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ISynchronizationContextProvider synchronizationContextProvider,
    ILogger<SettingRequiredCalibrationViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SettingRequiredCalibrationParam> _settingRequiredCalibrationParamList = [];

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await ReflectWcfObjToObservableObjAsync().ConfigureAwait(false);
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

    private Task ReflectWcfObjToObservableObjAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                synchronizationContextProvider.Send(SettingRequiredCalibrationParamList.Clear);

                var calibrationCategoryList = CalibrationReflectionHelper.GetCalibrationDescriptionList();

                foreach (var calibrationCategory in calibrationCategoryList)
                {
                    var items = new List<SettingRequiredCalibrationCategoryItem>();
                    var calibrationCategoryObj = new SettingRequiredCalibrationParam
                    {
                        Description = calibrationCategory.Description,
                        CategoryItems = items
                    };

                    var parentCalibrationCache = calibrationSetting.SettingRequiredCalibrationParamList.SingleOrDefault(t => t.Description == calibrationCategory.Description);

                    foreach (var calibrationCategoryItem in calibrationCategory.Items)
                    {
                        var categoryItem = new SettingRequiredCalibrationCategoryItem
                        {
                            AssemblyQualifiedName = calibrationCategoryItem.CalibrationDtoType.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false),
                            Description = Guard.IsNotNullAndReturn(calibrationCategoryItem.CalibrationDtoType.Namespace).Split('.').Last()
                        };
                        var childCalibrationCache = parentCalibrationCache?.CategoryItems.SingleOrDefault(t => t.AssemblyQualifiedName == categoryItem.AssemblyQualifiedName);
                        categoryItem.IsRequired = childCalibrationCache?.IsRequired ?? false;

                        items.Add(categoryItem);
                    }

                    synchronizationContextProvider.Send(() => SettingRequiredCalibrationParamList.Add(calibrationCategoryObj));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Get CalibrationObj IsOk Status Failed!");
            }
        });
    }
}