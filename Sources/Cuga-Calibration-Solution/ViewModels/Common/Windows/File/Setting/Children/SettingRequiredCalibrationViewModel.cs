using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.OpticalPower;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Wcf.Models;
using Core.Wcf.Models.Ads;
using Core.Wcf.Models.Chuck;
using Core.Wcf.Models.Laser;
using Core.Wcf.Models.Microscope;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Helper.Object;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingRequiredCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingRequiredCalibrationViewModel(
    CalibrationSetting calibrationSetting,
    ICalibrationCacheProvider calibrationCacheProvider,
    ISynchronizationContextProvider synchronizationContextProvider,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<SettingRequiredCalibrationViewModel> logger) : SettingWindowViewModelBase
{
    /// <summary>
    /// wcf对象
    /// </summary>
    private CalibrationObj _calibrationObj = new();

    [ObservableProperty]
    private SettingRequiredCalibrationParam _requiredCalibrationParam = new();

    [ObservableProperty]
    private SettingRequiredCalibrationParam _cacheRequiredCalibrationParam = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            if (ReflectWcfObjToObservableObj() == false) return;
        });
        OnPropertyChanged(nameof(RequiredCalibrationParam));
    }

    public override Task<bool> SavingAsync()
    {
        return Task.Run(() =>
        {
            //if (ReflectObservableObjToWcfObj() == false) return false; //状态写回wcf obj
            if (SaveFileCache() == false) return false; //写回缓存
            CacheRequiredCalibrationParam = RequiredCalibrationParam.Clone();
            calibrationSetting.SettingRequiredCalibrationParam = CacheRequiredCalibrationParam;
            return true;
        });

        bool SaveFileCache()
        {
            try
            {
                var cancellationToken = CancellationToken.None;

                Guard.IsNotNull(_calibrationObj);

                var adsParamList = RequiredCalibrationParam.AdsRequiredCalibrationList;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<AdsPressureGainsDto>(adsParamList.Single(t => t.CalibrationClassName == nameof(CalibrationAdsPressureGains)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<AdsXGainsItemDto>(adsParamList.Single(t => t.CalibrationClassName == nameof(CalibrationAdsXGainsItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<AdsYGainsItemDto>(adsParamList.Single(t => t.CalibrationClassName == nameof(CalibrationAdsYGainsItem)).IsRequired, cancellationToken) == false) return false;

                var microscopeParamList = RequiredCalibrationParam.MicroscopeRequiredCalibrationList;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<MicroscopeFocusItemDto>(microscopeParamList.Single(t => t.CalibrationClassName == nameof(CalibrationMicroscopeFocusItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<MicroscopeCalChipDto>(microscopeParamList.Single(t => t.CalibrationClassName == nameof(CalibrationMicroscopeCalChip)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<MicroscopePixelSizeItemDto>(microscopeParamList.Single(t => t.CalibrationClassName == nameof(CalibrationMicroscopePixelSizeItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<MicroscopeCentricityItemDto>(microscopeParamList.Single(t => t.CalibrationClassName == nameof(CalibrationMicroscopeCentricityItem)).IsRequired, cancellationToken) == false) return false;

                var chuckParamList = RequiredCalibrationParam.ChuckRequiredCalibrationList;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<ChuckGantryDto>(chuckParamList.Single(t => t.CalibrationClassName == nameof(CalibrationChuckGantry)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<ChuckCenterObjDto>(chuckParamList.Single(t => t.CalibrationClassName == nameof(CalibrationCenterObj)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<ChuckPrealignerObjDto>(chuckParamList.Single(t => t.CalibrationClassName == nameof(CalibrationPrealignerObj)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<ChuckStageMapDto>(chuckParamList.Single(t => t.CalibrationClassName == nameof(CalibrationChuckStageMap)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<ChuckGlobalScaleErrorDto>(chuckParamList.Single(t => t.CalibrationClassName == nameof(CalibrationChuckGlobalScaleError)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<ChuckRotateScaleErrorDto>(chuckParamList.Single(t => t.CalibrationClassName == nameof(CalibrationChuckRotateScaleError)).IsRequired, cancellationToken) == false) return false;

                var laserParamList = RequiredCalibrationParam.LaserRequiredCalibrationList;
                if (calibrationCacheProvider.TrySetIsRequiredSelfCheck<LaserAutoFocusDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserAutoFocus)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserAodDelayItemDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserAodDelayItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserXTCCalibrationItemDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserXTCCalibrationItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserPixelSizeItemDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserPixelSizeItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserLineCentricityItemDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserLineCentricityItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserIlluminationProfileItemDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserIlluminationProfileItem)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserOpticalPowerDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserOpticalPower)).IsRequired, cancellationToken) == false) return false;
                if (calibrationCacheProvider.TrySetArrayIsRequiredSelfCheck<LaserXYAstigmatismCalibrationItemDto>(laserParamList.Single(t => t.CalibrationClassName == nameof(CalibrationLaserXYAstigmatismItem)).IsRequired, cancellationToken) == false) return false;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Save Result or Db File Failed!");
                return false;
            }
        }
    }

    private bool ReflectWcfObjToObservableObj()
    {
        try
        {
            RequiredCalibrationParam = new SettingRequiredCalibrationParam();
            var reflectObjPropertyInfos = typeof(SettingRequiredCalibrationParam).GetProperties();
            var wcfObjPropertyInfos = typeof(CalibrationObj).GetProperties();
            wcfObjPropertyInfos.ForEach(t =>
            {
                var parentPropertyDescribe = t.GetCustomAttribute<DescriptionAttribute>()!.Description;
                var parentReflectProperty = reflectObjPropertyInfos.Where(t => t.CustomAttributes.Any(t => t.AttributeType.Equals(typeof(DescriptionAttribute))))
                    .Single(t => t.GetCustomAttribute<DescriptionAttribute>()!.Description.Equals(parentPropertyDescribe));
                var childWcfValue = t.GetValue(_calibrationObj)!;
                var childWcfPropertyInfos = childWcfValue.GetType().GetProperties();
                var reflectValue = parentReflectProperty.GetValue(RequiredCalibrationParam) as ObservableCollection<RequiredCalibrationParam>;
                var cacheReflectValue = parentReflectProperty.GetValue(CacheRequiredCalibrationParam) as ObservableCollection<RequiredCalibrationParam>;

                childWcfPropertyInfos.ForEach(t =>
                {
                    var childPropertyDescribe = t.GetCustomAttribute<DescriptionAttribute>()!.Description;
                    var requiredCalibrationParam = new RequiredCalibrationParam() { CalibrationName = childPropertyDescribe, CalibrationClassName = t.PropertyType.Name.Replace("[]", string.Empty) };
                    var cacheReflectValueResult = cacheReflectValue!.SingleOrDefault(t => t.CalibrationName == childPropertyDescribe);
                    if (cacheReflectValueResult is not null)
                        requiredCalibrationParam.IsRequired = cacheReflectValueResult.IsRequired;

                    synchronizationContextProvider.Send(() => reflectValue!.Add(requiredCalibrationParam));
                });

                ObjectHelper.SetPropertyValue(parentReflectProperty, parentReflectProperty.Name, reflectValue);
            });
            return true;
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Reflect wcfObj to cuga required calibration param obj failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Reflect wcfObj to cuga required calibration param obj failed!");
            return false;
        }
    }

    static bool SetCalibrationItemIsRequired(object childCalibrationObj, bool isRequired)
    {
        try
        {
            if (childCalibrationObj is System.Collections.ICollection)
            {
                var childCalibrationObjList = ObjectConvertToList<CalibrationBase>(childCalibrationObj);
                childCalibrationObjList.ForEach(t => t.IsRequiredSelfCheck = isRequired);
            }
            else
            {
                if (childCalibrationObj.GetType().IsSubclassOf(typeof(CalibrationBase)) == false)
                    return false;
                (childCalibrationObj as CalibrationBase)!.IsRequiredSelfCheck = isRequired;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static List<T> ObjectConvertToList<T>(object listObj)
    {
        var result = new List<T>();
        if (listObj.GetType().IsGenericType == false)
            return result;
        if (listObj is System.Collections.ICollection list)
        {
            if (list.Count > 0)
            {
                foreach (var item in list)
                {
                    result.Add((T)item);
                }
            }
        }
        else
            return result;

        return result;
    }
}