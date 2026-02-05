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
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
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
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models;
using Core.Models.Models.Common.Recipe.Info;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Helpers.Extensions;
using Newtonsoft.Json.Linq;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationCacheProvider), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationCacheProviderServiceImpl(
    IOptions<ApplicationSetting> options,
    ICacheProvider cacheProvider,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheDatabaseProvider recipeCacheDatabaseProvider,
    ISysRecipeInformationService sysRecipeInformationService,
    ILogger<CalibrationCacheProviderServiceImpl> logger,
    IDialogWindowProvider dialogWindowProvider,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting) : ICalibrationCacheProvider
{
    private readonly string _saveResultDirectory = Path.Combine(options.Value.AppHomeDirectory, "CalibrationResult");

    public async Task<bool> TrySaveAsync(string? filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
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
                    cancellationToken.ThrowIfCancellationRequested();

                    var parentCalibrationRequiredCache = calibrationSetting.SettingRequiredCalibrationParamList.Single(t => t.Description == calibrationCategory.Description);
                    var wcfCategoryPropertyInfo = wcfObjProperties.Single(t => t.PropertyType == calibrationCategory.WcfCategoryType);
                    foreach (var calibrationCategoryItem in calibrationCategory.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

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
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Save Result Canceled");

                    return false;
                }

                logger.LogError(ex, "Save Result Failed");

                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> TryExportAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var defaultCaches = new JObject();
                foreach (var cacheItem in CacheCollector.DefaultCaches)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var data = cacheItem.IsArray
                        ? cacheProvider.GetOrDefaultArray(cacheItem.Type)
                        : cacheProvider.GetOrDefault(cacheItem.Type) ?? Activator.CreateInstance(cacheItem.Type);

                    Guard.IsNotNull(data);

                    var jToken = JToken.FromObject(data, PrivateSetterContractResolver.PrivateSetterAndReplaceJsonSerializer);
                    RemoveMetadata(jToken);

                    defaultCaches[cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)] = jToken;
                }

                var recipesCaches = new JObject();

                var originalRecipeDBPath = applicationCookie.CalibrationRecipeDto?.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource;
                Guard.IsNotNullOrEmpty(originalRecipeDBPath);
                try
                {
                    var recipes = await sysRecipeInformationService.GetAllAsync(cancellationToken);

                    foreach (var recipeInfo in recipes.OrderBy(t => t.RecipeDbName))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        recipeCacheDatabaseProvider.ChangeDatabase(recipeInfo.RecipeNosqlRecipeDbDataSource, cancellationToken);

                        var recipeCaches = new JObject();
                        foreach (var cacheItem in CacheCollector.RecipeCaches)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var data = cacheItem.IsArray
                                ? recipeCacheProvider.GetOrDefaultArray(cacheItem.Type)
                                : recipeCacheProvider.GetOrDefault(cacheItem.Type) ?? Activator.CreateInstance(cacheItem.Type);

                            Guard.IsNotNull(data);

                            var jToken = JToken.FromObject(data, PrivateSetterContractResolver.PrivateSetterAndReplaceJsonSerializer);
                            RemoveMetadata(jToken);

                            recipeCaches[cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)] = jToken;
                        }

                        recipesCaches[recipeInfo.RecipeDbName] = recipeCaches;
                    }
                }
                finally
                {
                    recipeCacheDatabaseProvider.ChangeDatabase(originalRecipeDBPath, cancellationToken);
                }

                var exportData = new JObject
                {
                    // [nameof(ICacheItem.CreatedTime)] = DateTime.Now,
                    [nameof(CalibrationDtoBase.CreatedUserName)] = applicationCookie.SysUser.UserName,
                    [nameof(CacheCollector.DefaultCaches)] = JObject.FromObject(defaultCaches, PrivateSetterContractResolver.PrivateSetterAndReplaceJsonSerializer),
                    [nameof(CacheCollector.RecipeCaches)] = JObject.FromObject(recipesCaches, PrivateSetterContractResolver.PrivateSetterAndReplaceJsonSerializer)
                };

                FileHelper.SerializeOperate(exportData, filePath);

                return true;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Export Cache Canceled");

                    return false;
                }

                logger.LogError(ex, "Export Cache Failed");

                return false;
            }
        }, cancellationToken);

        void RemoveMetadata(JToken jToken)
        {
            switch (jToken)
            {
                case JArray array:
                    foreach (var item in array) RemoveMetadata(item);

                    break;
                case JObject obj:
                    obj.Remove(nameof(ICacheItem.Id));
                    obj.Remove(nameof(ICacheItem.Expiration));
                    obj.Remove(nameof(ICacheItem.CreatedTime));
                    obj.Remove(nameof(ICacheItem.ModifiedTime));
                    obj.Remove(nameof(ICacheItem.IsDeleted));
                    obj.Remove(nameof(ObservableValidator.HasErrors));
                    obj.Remove(nameof(CalibrationDtoBase.CreatedUserId));

                    break;
            }
        }
    }

    public async Task<bool> TryImportAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var importData = JObject.Parse(File.ReadAllText(filePath));

                var defaultCaches = GuardUtils.IsNotNullAndAssignableToType<JObject>(importData[nameof(CacheCollector.DefaultCaches)]);
                foreach (var cacheItem in CacheCollector.DefaultCaches)
                {
                    if (defaultCaches.TryGetValue(cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false), out var jToken) == false
                        || jToken.Type == JTokenType.Null) continue;

                    var targetType = cacheItem.IsArray ? cacheItem.Type.MakeArrayType() : cacheItem.Type;
                    var data = jToken.ToObject(targetType, PrivateSetterContractResolver.PrivateSetterAndReplaceJsonSerializer);
                    Guard.IsNotNull(data);

                    if (cacheItem.IsArray) cacheProvider.SetArray(ObjectHelper.ConvertToArray(data, cacheItem.Type).Cast<object>().ToArray(), cacheItem.Type, cancellationToken);
                    else cacheProvider.Set(data, cacheItem.Type, cancellationToken);
                }

                var recipesCaches = GuardUtils.IsNotNullAndAssignableToType<JObject>(importData[nameof(CacheCollector.RecipeCaches)]);

                var originalRecipeDBPath = applicationCookie.CalibrationRecipeDto?.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource;
                Guard.IsNotNullOrEmpty(originalRecipeDBPath);
                try
                {
                    foreach (var (recipeName, recipeCachesToken) in recipesCaches)
                    {
                        Guard.IsNotNull(recipeName);
                        var recipeCaches = GuardUtils.IsNotNullAndAssignableToType<JObject>(recipeCachesToken);

                        var recipe = (await sysRecipeInformationService.GetByConditionAsync(new SysRecipeInformationDto { RecipeDbName = recipeName }, cancellationToken)).FirstOrDefault();

                        if (recipe is null)
                        {
                            recipe = new SysRecipeInformationDto
                            {
                                RecipeDbName = recipeName,
                                DescribeInformation = $"Imported on {DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}",
                                RecipeNosqlRecipeDbDataSource = Path.Combine(options.Value.NosqlDbDataSourceDirectory, recipeName, new CalibrationRecipeInfoDto().RecipeDbName)
                            };

                            await sysRecipeInformationService.InsertAsync(recipe, cancellationToken);
                        }

                        recipeCacheDatabaseProvider.ChangeDatabase(recipe.RecipeNosqlRecipeDbDataSource, cancellationToken);

                        foreach (var cacheItem in CacheCollector.RecipeCaches)
                        {
                            if (recipeCaches.TryGetValue(cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false), out var jToken) == false
                                || jToken.Type == JTokenType.Null) continue;

                            var targetType = cacheItem.IsArray ? cacheItem.Type.MakeArrayType() : cacheItem.Type;
                            var data = jToken.ToObject(targetType, PrivateSetterContractResolver.PrivateSetterAndReplaceJsonSerializer);
                            Guard.IsNotNull(data);

                            if (cacheItem.IsArray)
                            {
                                recipeCacheProvider.SetArray(ObjectHelper.ConvertToArray(data, cacheItem.Type).Cast<object>().ToArray(), cacheItem.Type, cancellationToken);
                            }
                            else
                            {
                                recipeCacheProvider.Set(data, cacheItem.Type, cancellationToken);
                            }
                        }
                    }
                }
                finally
                {
                    recipeCacheDatabaseProvider.ChangeDatabase(originalRecipeDBPath, cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Import Cache Canceled");

                    return false;
                }

                logger.LogError(ex, "Import Cache Failed");

                return false;
            }
        }, cancellationToken);
    }

    public bool InvokeSave(Func<Action<ICacheItem>, bool> func, string name, CancellationToken token)
    {
        while (true)
        {
            if (token.IsCancellationRequested) return false;

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