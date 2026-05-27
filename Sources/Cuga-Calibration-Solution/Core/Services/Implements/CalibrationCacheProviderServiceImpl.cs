using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Version;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using Core.Utilities;
using Core.Utilities.SourceGenerators;
using Core.Wcf.Models;
using Core.Wcf.Models.Ads;
using Core.Wcf.Models.Chuck;
using Core.Wcf.Models.Fourier;
using Core.Wcf.Models.Laser;
using Core.Wcf.Models.Microscope;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Serializations;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
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
using System.Reflection;
using System.Text;

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
    IApplicationCookieService applicationCookieService,
    RecipeCookie recipeCookie,
    CalibrationSetting calibrationSetting) : ICalibrationCacheProvider
{
    public async Task<bool> TrySaveAsync(CalibrationVersionDTO calibrationVersionDTO, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var calibrationObj = new CalibrationObj
                {
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    CalibrationAdsObj = new CalibrationAdsObj(),
                    CalibrationMicroscopeObj = new CalibrationMicroscopeObj(),
                    CalibrationChuckObj = new CalibrationChuckObj(),
                    CalibrationLaserObj = new CalibrationLaserObj(),
                    CalibrationPupilFourierObj = new CalibrationPupilFourierObj()
                };

                // 使用 CalibrationMenu 作为数据源
                var calibrationMenu = applicationCookie.CalibrationMenu;
                var wcfObjProperties = calibrationObj.GetType().GetProperties();

                // 递归处理所有校准菜单节点
                ProcessCalibrationMenuNode(calibrationMenu, calibrationObj, wcfObjProperties, cancellationToken);

                cacheProvider.Set(calibrationVersionDTO, CancellationToken.None);

                FileHelper.SerializeOperate(calibrationObj, Path.Combine(Path.Combine(options.Value.AppHomeDirectory, "CalibrationResult"), calibrationVersionDTO.ResultFilePath));

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

        void ProcessCalibrationMenuNode(CalibrationMenu node, CalibrationObj calibrationObj, PropertyInfo[] wcfObjProperties, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 递归处理子节点
            foreach (var child in node.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 如果是叶子节点（Menu 类型）且有有效的 Entry
                if (child.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu && child.Entry != CalibrationViewModelEntry.Default)
                {
                    ProcessCalibrationEntry(child, calibrationObj, wcfObjProperties, cancellationToken);
                }

                // 递归处理子节点
                ProcessCalibrationMenuNode(child, calibrationObj, wcfObjProperties, cancellationToken);
            }
        }

        void ProcessCalibrationEntry(CalibrationMenu menuNode, CalibrationObj calibrationObj, PropertyInfo[] wcfObjProperties, CancellationToken cancellationToken)
        {
            try
            {
                var entry = menuNode.Entry;

                // 获取对应的父级节点（类别）名称
                var parentCategoryName = menuNode.SysMenu.Parent?.Name ?? string.Empty;

                // 查找对应的必需校准配置
                var parentCalibrationRequiredCache = calibrationSetting.SettingRequiredCalibrationParamList
                    .FirstOrDefault(t => t.SysMenu.Name == parentCategoryName);

                if (parentCalibrationRequiredCache is null)
                {
                    logger.LogWarning("Required calibration cache not found for category: {Category}", parentCategoryName);
                    return;
                }

                // 获取 DTO 类型信息
                var calibrationDtoType = entry.DTOType;
                var wcfModelType = entry.AdaptToCUGAType;

                if (wcfModelType == null)
                {
                    logger.LogWarning("WCF model type is null for: {EntryName}", entry.Name);
                    return;
                }

                // 找到对应的 WCF 类别属性
                var wcfCategoryPropertyInfo = wcfObjProperties.FirstOrDefault(t => t.GetValue(calibrationObj)?.GetType().GetProperties()
                    .Any(p => p.PropertyType == wcfModelType || p.PropertyType == wcfModelType.MakeArrayType()) == true);

                if (wcfCategoryPropertyInfo == null)
                {
                    logger.LogWarning("WCF category property not found for: {WcfModelType}", wcfModelType.Name);
                    return;
                }

                var versionInfo = calibrationVersionDTO.GetVersionInfo(calibrationDtoType);
                var version = SQLiteHelper.GetTableInfo(calibrationDtoType).Version;

                var childCalibrationRequiredCache = parentCalibrationRequiredCache.GetAllChildren().SingleOrDefault(t => t.CategoryItem.TypeInstance == calibrationDtoType);
                var isRequired = childCalibrationRequiredCache?.CategoryItem.IsRequired ?? false;

                // 获取转换方法
                var toWcfMethod = CalibrationReflectionHelper.WcfModelTypeToCalibrationDtoType(wcfModelType).MethodInfo;
                if (toWcfMethod == null)
                {
                    logger.LogWarning("ToCuga/ToWcf method not found for: {DtoType}", calibrationDtoType.Name);
                    return;
                }

                var childWcfCategoryPropertyInfo = wcfCategoryPropertyInfo.PropertyType.GetProperties().SingleOrDefault(t => (entry.IsArray ? t.PropertyType.GetElementType() : t.PropertyType) == entry.AdaptToCUGAType);
                if (childWcfCategoryPropertyInfo == null)
                {
                    logger.LogWarning("WCF child array property not found for: {WcfModelType}", wcfModelType.Name);
                    return;
                }

                if (entry.IsArray)
                {
                    var dtoItems = versionInfo is null
                        ? applicationCookieService.GetCalibrations(calibrationDtoType, cancellationToken)
                        : cacheProvider.GetArray(calibrationDtoType, versionInfo.Id, cancellationToken);
                    if (dtoItems is null || dtoItems.Length == 0)
                        dtoItems = [Guard.IsNotNullAndReturn(Activator.CreateInstance(calibrationDtoType))];

                    var wcfItems = dtoItems.Select(t =>
                    {
                        var value = Guard.IsNotNullAndReturn(toWcfMethod.Invoke(t, null));
                        Guard.IsNotNullAndReturn(value.GetType().GetProperty(nameof(CalibrationBase.IsRequiredCalibrate))).SetValue(value, isRequired);
                        Guard.IsNotNullAndReturn(value.GetType().GetProperty(nameof(CalibrationBase.Version))).SetValue(value, version);
                        return value;
                    }).ToArray();

                    var values = ObjectHelper.ConvertToArray(wcfItems, wcfModelType);
                    childWcfCategoryPropertyInfo.SetValue(wcfCategoryPropertyInfo.GetValue(calibrationObj), values);
                }
                else
                {
                    var dto = (versionInfo is null
                        ? applicationCookieService.GetCalibration(calibrationDtoType, cancellationToken)
                        : cacheProvider.Get(calibrationDtoType, versionInfo.Id, cancellationToken)) ?? Guard.IsNotNullAndReturn(Activator.CreateInstance(calibrationDtoType));

                    var wcfModel = toWcfMethod.Invoke(dto, null);
                    if (wcfModel is not null)
                    {
                        Guard.IsNotNullAndReturn(wcfModel.GetType().GetProperty(nameof(CalibrationBase.IsRequiredCalibrate))).SetValue(wcfModel, isRequired);
                        Guard.IsNotNullAndReturn(wcfModel.GetType().GetProperty(nameof(CalibrationBase.Version))).SetValue(wcfModel, version);
                        childWcfCategoryPropertyInfo.SetValue(wcfCategoryPropertyInfo.GetValue(calibrationObj), wcfModel);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process calibration entry: {EntryName}", menuNode.Entry.Name);
                ThrowHelper.ThrowArgumentException($"Failed to process calibration entry: {menuNode.Entry.Name}", ex);
            }
        }
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
                foreach (var cacheItem in CugaCalibrationSolutionCacheCollector.DefaultCaches.Concat(CoreRecipeCacheCollector.DefaultCaches))
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
                        foreach (var cacheItem in CugaCalibrationSolutionCacheCollector.RecipeCaches.Concat(CoreRecipeCacheCollector.RecipeCaches))
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
                    [nameof(CalibrationDTOBase.CreatedUserName)] = applicationCookie.SysUser.UserName,
                    [nameof(CugaCalibrationSolutionCacheCollector.DefaultCaches)] = JObject.FromObject(defaultCaches, PrivateSetterContractResolver.Serializer),
                    [nameof(CugaCalibrationSolutionCacheCollector.RecipeCaches)] = JObject.FromObject(recipesCaches, PrivateSetterContractResolver.Serializer)
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
                var defaultCaches = Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(importData[nameof(CugaCalibrationSolutionCacheCollector.DefaultCaches)]);
                foreach (var cacheItem in CugaCalibrationSolutionCacheCollector.DefaultCaches.Concat(CoreRecipeCacheCollector.DefaultCaches))
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
                var recipesCaches = Guard.IsNotNullAndAssignableToTypeAndReturn<JObject>(importData[nameof(CugaCalibrationSolutionCacheCollector.RecipeCaches)]);

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

                            foreach (var cacheItem in CugaCalibrationSolutionCacheCollector.RecipeCaches.Concat(CoreRecipeCacheCollector.RecipeCaches))
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

                var finalMessage = isOverallSuccess ? "Import completed successfully And Restart Application" : "Import completed with errors And Restart Application";
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