using Core.Models.Helper;
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
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Helpers.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationCacheProvider), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationCacheProviderServiceImpl(
    IOptions<ApplicationSetting> options,
    ICacheProvider cacheProvider,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
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
                        if (dtoItems is null || dtoItems.Length == 0)
                            dtoItems = [GuardUtils.IsNotNullAndReturn(Activator.CreateInstance(calibrationCategoryItem.CalibrationDtoType))];
                        var wcfItems = dtoItems.Select(t =>
                        {
                            var value = GuardUtils.IsNotNullAndReturn(calibrationCategoryItem.CalibrationDtoToWcfModelMethodInfo.Invoke(t, null));
                            GuardUtils.IsNotNullAndReturn(value.GetType().GetProperty(nameof(CalibrationBase.IsRequiredCalibrate))).SetValue(value, childCalibrationRequiredCache.IsRequired);
                            return value;
                        }).ToArray();

                        var values = ObjectHelper.ConvertToArray(wcfItems, calibrationCategoryItem.WcfModelType);
                        childWcfCategoryPropertyInfo.SetValue(wcfCategoryPropertyInfo.GetValue(calibrationObj), values);
                    }
                    else
                    {
                        var childWcfCategoryPropertyInfo = wcfCategoryPropertyInfo.PropertyType.GetProperties().Single(t => t.PropertyType == calibrationCategoryItem.WcfModelType);
                        var dto = cacheProvider.Get(calibrationCategoryItem.CalibrationDtoType);
                        var wcfModel = dto is not null ? calibrationCategoryItem.CalibrationDtoToWcfModelMethodInfo.Invoke(dto, null) : null;
                        if (wcfModel is not null)
                        {
                            GuardUtils.IsNotNullAndReturn(wcfModel.GetType().GetProperty(nameof(CalibrationBase.IsRequiredCalibrate))).SetValue(wcfModel, childCalibrationRequiredCache.IsRequired);
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
            logger.LogError(ex, "Save result failed");
            return false;
        }
    }

    public bool TryExport(string filePath)
    {
        try
        {
            var defaultJObject = new JObject();
            foreach (var cacheItem in CacheCollector.DefaultCaches)
            {
                var data = cacheItem.IsArray
                    ? cacheProvider.GetOrDefaultArray(cacheItem.Type)
                    : cacheProvider.GetOrDefault(cacheItem.Type);

                defaultJObject[cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)] = data is not null
                    ? JToken.FromObject(data)
                    : JValue.CreateNull();
            }

            var recipeJObject = new JObject();
            foreach (var cacheItem in CacheCollector.RecipeCaches)
            {
                var data = cacheItem.IsArray
                    ? recipeCacheProvider.GetOrDefaultArray(cacheItem.Type)
                    : recipeCacheProvider.GetOrDefault(cacheItem.Type);

                recipeJObject[cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)] = data is not null
                    ? JToken.FromObject(data)
                    : JValue.CreateNull();
            }

            var exportJObject = new JObject
            {
                [nameof(CacheCollector.DefaultCaches)] = defaultJObject,
                [nameof(CacheCollector.RecipeCaches)] = recipeJObject
            };

            File.WriteAllText(filePath, exportJObject.ToString(Formatting.Indented));

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export cache failed");

            return false;
        }
    }

    public bool TryImport(string filePath)
    {
        try
        {
            var importJObject = JObject.Parse(File.ReadAllText(filePath));

            if (importJObject.TryGetValue(nameof(CacheCollector.DefaultCaches), out var defaultJToken) && defaultJToken is JObject defaultJObject)
            {
                foreach (var cacheItem in CacheCollector.DefaultCaches)
                {
                    if (defaultJObject.TryGetValue(cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false), out var jToken) == false || jToken.Type == JTokenType.Null) continue;

                    var targetType = cacheItem.IsArray ? cacheItem.Type.MakeArrayType() : cacheItem.Type;

                    var data = jToken.ToObject(targetType);

                    Guard.IsNotNull(data);
                    try
                    {
                        if (cacheItem.IsArray)
                        {
                            cacheProvider.SetArray(ObjectHelper.ConvertToArray(data, cacheItem.Type).Cast<object>().ToArray(), cacheItem.Type, CancellationToken.None);
                        }
                        else
                        {
                            cacheProvider.Set(data, cacheItem.Type, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Import cache failed");

                        return false;
                    }
                }
            }

            if (importJObject.TryGetValue(nameof(CacheCollector.RecipeCaches), out var recipeJToken) && recipeJToken is JObject recipeJObject)
            {
                foreach (var cacheItem in CacheCollector.RecipeCaches)
                {
                    if (recipeJObject.TryGetValue(cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false), out var jToken) == false || jToken.Type == JTokenType.Null) continue;

                    var targetType = cacheItem.IsArray ? cacheItem.Type.MakeArrayType() : cacheItem.Type;

                    var data = jToken.ToObject(targetType);

                    Guard.IsNotNull(data);

                    if (cacheItem.IsArray)
                    {
                        recipeCacheProvider.SetArray(ObjectHelper.ConvertToArray(data, cacheItem.Type).Cast<object>().ToArray(), cacheItem.Type, CancellationToken.None);
                    }
                    else
                    {
                        recipeCacheProvider.Set(data, cacheItem.Type, CancellationToken.None);
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import cache failed");

            return false;
        }
    }

    public bool InvokeSave(Func<Action<ICacheItem>, bool> func, string name)
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