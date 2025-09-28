using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Setting;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingRequiredCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingRequiredCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ISynchronizationContextProvider synchronizationContextProvider,
    ILogger<SettingRequiredCalibrationViewModel> logger) : SettingWindowViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SettingRequiredCalibrationParam> _settingRequiredCalibrationParamList = [];


    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(ReflectWcfObjToObservableObj);
    }

    public override async Task<bool> SavingAsync()
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

    private void ReflectWcfObjToObservableObj()
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
                    var categoryItem = new SettingRequiredCalibrationCategoryItem()
                    {
                        AssemblyQualifiedName = calibrationCategoryItem.CalibrationDtoType.ToAssemblyName(isCulture: false, isVersion: false, isPublicKeyToken: false),
                        Description = GuardUtils.IsNotNullAndReturn(calibrationCategoryItem.CalibrationDtoType.Namespace).Split('.').Last()
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
    }
}