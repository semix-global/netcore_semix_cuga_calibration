using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using Core.Utilities;
using Core.Wcf.Models;
using Core.Wcf.Models.Ads;
using Core.Wcf.Models.Chuck;
using Core.Wcf.Models.Fourier;
using Core.Wcf.Models.Laser;
using Core.Wcf.Models.Microscope;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Serializations;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using Local.SQL.Cache.Providers.Serializations;

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
    RecipeCookie recipeCookie,
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
                    CalibrationLaserObj = new CalibrationLaserObj(),
                    CalibrationPupilFourierObj = new CalibrationPupilFourierObj()
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
                                dtoItems = [Guard.IsNotNullAndReturn(Activator.CreateInstance(calibrationCategoryItem.CalibrationDtoType))];
                            var wcfItems = dtoItems.Select(t =>
                            {
                                var value = Guard.IsNotNullAndReturn(calibrationCategoryItem.CalibrationDtoToWcfModelMethodInfo.Invoke(t, null));
                                Guard.IsNotNullAndReturn(value.GetType().GetProperty(nameof(CalibrationBase.IsRequiredCalibrate))).SetValue(value, childCalibrationRequiredCache.IsRequired);
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
                                Guard.IsNotNullAndReturn(wcfModel.GetType().GetProperty(nameof(CalibrationBase.IsRequiredCalibrate))).SetValue(wcfModel, childCalibrationRequiredCache.IsRequired);
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

    public async Task<(bool IsSuccess, string Message)> TryExportAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            var defaultCaches = new JObject();
            var recipesCaches = new JObject();
            var messageBuilder = new StringBuilder();
            var isOverallSuccess = true;

            try
            {
                messageBuilder.AppendLine("=== Default Cache Export ===");
                foreach (var cacheItem in CacheCollector.DefaultCaches)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var data = cacheItem.IsArray
                            ? cacheProvider.GetOrDefaultArray(cacheItem.Type)
                            : cacheProvider.GetOrDefault(cacheItem.Type) ?? Activator.CreateInstance(cacheItem.Type);

                        Guard.IsNotNull(data);

                        defaultCaches[cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)] = JToken.FromObject(data, IgnoreCacheItemPropertiesContractResolver.Serializer);

                        var count = cacheItem.IsArray ? ((Array)data).Length : 1;
                        messageBuilder.AppendLine($"  [Success] {cacheItem.Type.Name} ({count} items)");
                    }
                    catch (Exception ex)
                    {
                        isOverallSuccess = false;
                        messageBuilder.AppendLine($"  [Failed] {cacheItem.Type.Name}: {ex.Message}");
                        logger.LogError(ex, "Failed to export default cache: {@TypeName}", cacheItem.Type.Name);
                    }
                }

                messageBuilder.AppendLine();

                var originalRecipeDBPath = recipeCookie.SysRecipeInformationDTO.RecipeNosqlRecipeDbDataSource;
                Guard.IsNotNullOrEmpty(originalRecipeDBPath);
                try
                {
                    var recipes = await sysRecipeInformationService.GetAllAsync(cancellationToken);

                    foreach (var recipeInfo in recipes.OrderBy(t => t.RecipeDbName))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        messageBuilder.AppendLine($"=== Recipe: {recipeInfo.RecipeDbName} ===");

                        recipeCacheDatabaseProvider.ChangeDatabase(recipeInfo.RecipeNosqlRecipeDbDataSource, cancellationToken);

                        var recipeCaches = new JObject();
                        foreach (var cacheItem in CacheCollector.RecipeCaches)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                var data = cacheItem.IsArray
                                    ? recipeCacheProvider.GetOrDefaultArray(cacheItem.Type)
                                    : recipeCacheProvider.GetOrDefault(cacheItem.Type) ?? Activator.CreateInstance(cacheItem.Type);

                                Guard.IsNotNull(data);

                                recipeCaches[cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)] = JToken.FromObject(data, IgnoreCacheItemPropertiesContractResolver.Serializer);

                                var count = cacheItem.IsArray ? ((Array)data).Length : 1;
                                messageBuilder.AppendLine($"  [Success] {cacheItem.Type.Name} ({count} items)");
                            }
                            catch (Exception ex)
                            {
                                isOverallSuccess = false;
                                messageBuilder.AppendLine($"  [Failed] {cacheItem.Type.Name}: {ex.Message}");
                                logger.LogError(ex, "Failed to export recipe cache: {@RecipeName} - {@TypeName}", recipeInfo.RecipeDbName, cacheItem.Type.Name);
                            }
                        }

                        recipesCaches[recipeInfo.RecipeDbName] = recipeCaches;
                        messageBuilder.AppendLine();
                    }
                }
                finally
                {
                    recipeCacheDatabaseProvider.ChangeDatabase(originalRecipeDBPath, cancellationToken);
                }

                var exportData = new JObject
                {
                    [nameof(ICacheItem.CreatedTime)] = DateTime.Now,
                    [nameof(CalibrationDtoBase.CreatedUserName)] = applicationCookie.SysUser.UserName,
                    [nameof(CacheCollector.DefaultCaches)] = JObject.FromObject(defaultCaches, PrivateSetterContractResolver.Serializer),
                    [nameof(CacheCollector.RecipeCaches)] = JObject.FromObject(recipesCaches, PrivateSetterContractResolver.Serializer)
                };

                FileHelper.SerializeOperate(exportData, filePath);

                var finalMessage = isOverallSuccess ? "Export completed successfully" : "Export completed with errors";
                messageBuilder.Insert(0, finalMessage + Environment.NewLine + Environment.NewLine);

                return (isOverallSuccess, messageBuilder.ToString());
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Export Cache Canceled");
                    return (false, "Export canceled");
                }

                logger.LogError(ex, "Export Cache Failed");
                return (false, $"Export failed: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<(bool IsSuccess, string Message)> TryImportAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            var messageBuilder = new StringBuilder();
            var isOverallSuccess = true;

            try
            {
                var importData = JObject.Parse(File.ReadAllText(filePath));

                // Import Default Caches
                messageBuilder.AppendLine("=== Default Cache Import ===");
                var defaultCaches = Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(importData[nameof(CacheCollector.DefaultCaches)]);
                foreach (var cacheItem in CacheCollector.DefaultCaches)
                {
                    try
                    {
                        if (defaultCaches.TryGetValue(cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false), out var jToken) == false
                            || jToken.Type == JTokenType.Null)
                        {
                            isOverallSuccess = false;
                            messageBuilder.AppendLine($"  [Failed] {cacheItem.Type.Name}: Data not found in file");
                            continue;
                        }

                        var targetType = cacheItem.IsArray ? cacheItem.Type.MakeArrayType() : cacheItem.Type;
                        var data = jToken.ToObject(targetType, PrivateSetterContractResolver.Serializer);
                        Guard.IsNotNull(data);

                        int count;
                        if (cacheItem.IsArray)
                        {
                            var array = ObjectHelper.ConvertToArray(data, cacheItem.Type).Cast<object>().ToArray();
                            cacheProvider.SetArray(cacheItem.Type, array, cancellationToken);
                            count = array.Length;
                        }
                        else
                        {
                            cacheProvider.Set(cacheItem.Type, data, cancellationToken);
                            count = 1;
                        }

                        messageBuilder.AppendLine($"  [Success] {cacheItem.Type.Name} ({count} items)");
                    }
                    catch (Exception ex)
                    {
                        isOverallSuccess = false;
                        messageBuilder.AppendLine($"  [Failed] {cacheItem.Type.Name}: {ex.Message}");
                        logger.LogWarning(ex, "Failed to import default cache: {@TypeName}", cacheItem.Type.Name);
                    }
                }

                messageBuilder.AppendLine();

                // Import Recipe Caches
                var recipesCaches = Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(importData[nameof(CacheCollector.RecipeCaches)]);

                var originalRecipeDBPath = recipeCookie.SysRecipeInformationDTO.RecipeNosqlRecipeDbDataSource;
                Guard.IsNotNullOrEmpty(originalRecipeDBPath);
                try
                {
                    var sysRecipeInformationList = await sysRecipeInformationService
                        .GetAllAsync(cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var (recipeName, recipeCachesToken) in recipesCaches)
                    {
                        messageBuilder.AppendLine($"=== Recipe: {recipeName} ===");
                        try
                        {
                            Guard.IsNotNull(recipeName);
                            var recipeCaches = Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(recipeCachesToken);


                            var recipe = sysRecipeInformationList.SingleOrDefault(t => t.RecipeDbName == recipeName);

                            var isNewRecipe = false;
                            if (recipe is null)
                            {
                                recipe = new SysRecipeInformationDTO
                                {
                                    RecipeDbName = recipeName,
                                    DescribeInformation = $"Imported on {DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}",
                                    RecipeNosqlRecipeDbDataSource = SQLiteHelper.GetConnectionString(Path.Combine(options.Value.NosqlDbDataSourceDirectory, recipeName, options.Value.RecipeDBName))
                                };

                                await sysRecipeInformationService.CreatAsync(recipe, cancellationToken);
                                isNewRecipe = true;
                            }

                            recipeCacheDatabaseProvider.ChangeDatabase(recipe.RecipeNosqlRecipeDbDataSource, cancellationToken);

                            foreach (var cacheItem in CacheCollector.RecipeCaches)
                            {
                                try
                                {
                                    if (recipeCaches.TryGetValue(cacheItem.Type.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false), out var jToken) == false
                                        || jToken.Type == JTokenType.Null)
                                    {
                                        continue;
                                    }

                                    var targetType = cacheItem.IsArray ? cacheItem.Type.MakeArrayType() : cacheItem.Type;
                                    var data = jToken.ToObject(targetType, PrivateSetterContractResolver.Serializer);
                                    Guard.IsNotNull(data);

                                    int count;
                                    if (cacheItem.IsArray)
                                    {
                                        var array = ObjectHelper.ConvertToArray(data, cacheItem.Type).Cast<object>().ToArray();
                                        recipeCacheProvider.SetArray(cacheItem.Type, array, cancellationToken);
                                        count = array.Length;
                                    }
                                    else
                                    {
                                        recipeCacheProvider.Set(cacheItem.Type, data, cancellationToken);
                                        count = 1;
                                    }

                                    var newInfo = isNewRecipe ? " [NEW]" : "";
                                    messageBuilder.AppendLine($"  [Success] {cacheItem.Type.Name} ({count} items){newInfo}");
                                }
                                catch (Exception ex)
                                {
                                    isOverallSuccess = false;
                                    messageBuilder.AppendLine($"  [Failed] {cacheItem.Type.Name}: {ex.Message}");
                                    logger.LogError(ex, "Failed to import recipe cache: {@RecipeName} - {@TypeName}", recipeName, cacheItem.Type.Name);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            isOverallSuccess = false;
                            messageBuilder.AppendLine($"  [Failed] Recipe Setup: {ex.Message}");
                            logger.LogError(ex, "Failed to import recipe: {@RecipeName}", recipeName);
                        }

                        messageBuilder.AppendLine();
                    }
                }
                finally
                {
                    recipeCacheDatabaseProvider.ChangeDatabase(originalRecipeDBPath, cancellationToken);
                }

                var finalMessage = isOverallSuccess ? "Import completed successfully" : "Import completed with errors";
                messageBuilder.Insert(0, finalMessage + Environment.NewLine + Environment.NewLine);

                return (isOverallSuccess, messageBuilder.ToString());
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Import Cache Canceled");
                    return (false, "Import canceled");
                }

                logger.LogError(ex, "Import Cache Failed");
                return (false, $"Import failed: {ex.Message}");
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