using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Utilities;
using Core.Wcf.Models;
using Core.Wcf.Models.Ads;
using Core.Wcf.Models.Chuck;
using Core.Wcf.Models.Laser;
using Core.Wcf.Models.Microscope;
using CugaCalibration.Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.IO;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationCacheProvider), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationCacheProviderServiceImpl(
    IOptions<ApplicationSetting> options,
    ICacheProvider cacheProvider,
    ILogger<CalibrationCacheProviderServiceImpl> logger,
    IDialogWindowProvider dialogWindowProvider,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting) : ICalibrationCacheProvider
{
    private readonly string _saveResultDirectory = Path.Combine(options.Value.AppHomeDirectory, "CalibrationResult");

    public bool TrySave(string? filePath = null)
    {
        try
        {
            var calibrationObj = new CalibrationObj
            {
                CalibrationAdsObj = new CalibrationAdsObj(),
                CalibrationMicroscopeObj = new CalibrationMicroscopeObj(),
                CalibrationChuckObj = new CalibrationChuckObj(),
                CalibrationLaserObj = new CalibrationLaserObj()
            };

            var calibrationCategoryList = CalibrationReflectionHelper.GetCalibrationDescriptionList();
            var wcfObjProperties = calibrationObj.GetType().GetProperties();
            var calibrationBase = new CalibrationBase();
            foreach (var calibrationCategory in calibrationCategoryList)
            {
                var parentCalibrationRequiredCache = calibrationSetting.SettingRequiredCalibrationParamList.Single(t => t.Description == calibrationCategory.Description);
                var wcfCategoryPropertyInfo = wcfObjProperties.Single(t => t.PropertyType == calibrationCategory.WcfCategoryType);
                foreach (var calibrationCategoryItem in calibrationCategory.Items)
                {
                    var childCalibrationRequiredCache = parentCalibrationRequiredCache.CategoryItems.Single(t => t.TypeInstance == calibrationCategoryItem.CalibrationDtoType);
                    if (calibrationCategoryItem.IsArray)
                    {
                        var childWcfCategoryPropertyInfo = wcfCategoryPropertyInfo.PropertyType.GetProperties().Single(t => t.PropertyType.GetElementType() == calibrationCategoryItem.WcfModelType);
                        var dtoItems = cacheProvider.GetArray(calibrationCategoryItem.CalibrationDtoType);
                        var wcfItems = dtoItems?.Select(t =>
                        {
                            var value = GuardUtils.IsNotNullAndReturn(calibrationCategoryItem.CalibrationDtoToWcfModelMethodInfo.Invoke(t, null));
                            GuardUtils.IsNotNullAndReturn(value.GetType().GetProperty(nameof(calibrationBase.IsRequiredSelfCheck))).SetValue(value, childCalibrationRequiredCache.IsRequired);
                            return value;
                        }).ToArray();
                        if (wcfItems is not null && wcfItems.Length > 0)
                        {
                            var values = ObjectHelper.ConvertToArray(wcfItems, calibrationCategoryItem.WcfModelType);
                            childWcfCategoryPropertyInfo.SetValue(wcfCategoryPropertyInfo.GetValue(calibrationObj), values);
                        }
                    }
                    else
                    {
                        var childWcfCategoryPropertyInfo = wcfCategoryPropertyInfo.PropertyType.GetProperties().Single(t => t.PropertyType == calibrationCategoryItem.WcfModelType);
                        var dto = cacheProvider.Get(calibrationCategoryItem.CalibrationDtoType);
                        var wcfModel =dto is not null? calibrationCategoryItem.CalibrationDtoToWcfModelMethodInfo.Invoke(dto, null):null;
                        if (wcfModel is not null)
                        {
                            GuardUtils.IsNotNullAndReturn(wcfModel.GetType().GetProperty(nameof(calibrationBase.IsRequiredSelfCheck))).SetValue(wcfModel, childCalibrationRequiredCache.IsRequired);
                            childWcfCategoryPropertyInfo.SetValue(wcfCategoryPropertyInfo.GetValue(calibrationObj), wcfModel);
                        }
                    }
                }

                wcfCategoryPropertyInfo.SetValue(calibrationObj, wcfCategoryPropertyInfo.GetValue(calibrationObj));
            }

            FileHelper.SerializeOperate(calibrationObj, Path.Combine(_saveResultDirectory, filePath ?? $"Result_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.dat"));

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save calibration result failed");
            return false;
        }
    }

    // todo:delete
    public bool TrySet<T>(T dto, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        return InvokeSave(update =>
        {
            update(dto);
            cacheProvider.Set(dto, cancellationToken);
            return true;
        }, typeof(T).Name);
    }

    // todo:delete
    public bool TrySetArray<T>(T[] dtoList, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        return InvokeSave(update =>
        {
            foreach (var dto in dtoList) update(dto);

            cacheProvider.SetArray(dtoList, cancellationToken);
            return true;
        }, typeof(T).Name);
    }

    public bool TrySetDisable<T>(CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var calibrationDtoBase = cacheProvider.GetOrDefault<T>();

        calibrationDtoBase.IsCalibrated = calibrationDtoBase.IsVerified = false;

        return TrySet(calibrationDtoBase, cancellationToken);
    }

    public bool TrySetArrayDisable<T>(CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var caches = cacheProvider.GetOrDefaultArray<T>();

        foreach (var calibrationDtoBase in caches)
        {
            calibrationDtoBase.IsCalibrated = calibrationDtoBase.IsVerified = false;
        }

        return TrySetArray(caches, cancellationToken);
    }

    public bool TrySetIsRequiredSelfCheck<T>(bool isRequiredSelfCheck, CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var calibrationDtoBase = cacheProvider.GetOrDefault<T>();

        if (calibrationDtoBase.IsRequiredSelfCheck == isRequiredSelfCheck) // 避免重复写入
            return true;

        calibrationDtoBase.IsRequiredSelfCheck = isRequiredSelfCheck;

        return TrySet(calibrationDtoBase, cancellationToken);
    }

    public bool TrySetArrayIsRequiredSelfCheck<T>(bool isRequiredSelfCheck, CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var caches = cacheProvider.GetOrDefaultArray<T>();

        if (caches.All(t => t.IsRequiredSelfCheck == isRequiredSelfCheck)) // 避免重复写入
            return true;

        foreach (var calibrationDtoBase in caches)
        {
            calibrationDtoBase.IsRequiredSelfCheck = isRequiredSelfCheck;
        }

        return TrySetArray(caches, cancellationToken);
    }

    private bool InvokeSave(Func<Action<ICacheItem>, bool> func, string name)
    {
        while (true)
        {
            if (func(Update)) return true;

            dialogWindowProvider.TryShowDialog($"Save {name} Failed!", out var dialogResultEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);

            if (dialogResultEnum == DialogResultEnum.Retry) continue;

            return false;
        }

        void Update(ICacheItem cacheItem)
        {
            cacheItem.Id = 0;
            cacheItem.CreatedTime = DateTime.Now;
            cacheItem.IsDeleted = false;

            if (cacheItem is not IEntityAdd entity) return;

            entity.CreatedUserId = applicationCookie.SysUser.Id;
            entity.CreatedUserName = applicationCookie.SysUser.UserName;
        }
    }
}