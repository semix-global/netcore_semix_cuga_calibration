using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Models.Models.Setting.CalibrationRelationConfig;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationRelationService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class CalibrationRelationServiceImpl(
    IApplicationCookieService applicationCookieService,
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie,
    ILogger<CalibrationRelationServiceImpl> logger) : ICalibrationRelationService
{
    private bool _isValidateRelationConfigConsistency;

    public bool DependenciesValidate(Type viewModelType)
    {
        if (applicationCookie.SysUser.IsAdmin) return true;

        var entry = ApplicationCookie.CalibrationViewModelEntries[viewModelType];

        Guard.IsNotNull(entry);
        List<bool> results = [];
        foreach (var dependency in entry.Dependencies.Where(t => t.IsUsed))
        {
            Guard.IsNotNull(dependency.TypeInstance);
            var result = true;
            if (dependency.IsArray)
            {
                var dtos = Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase[]>(applicationCookieService.GetArrayOrDefault(dependency.TypeInstance, false));
                if (dtos.All(t => t.IsOk) == false)
                {
                    logger.LogError("Dependency validation failed for {dependency}!", dependency.Description);
                    result = false;
                }
            }
            else
            {
                var dto = Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase>(applicationCookieService.GetOrDefault(dependency.TypeInstance, false));
                if (dto.IsOk == false)
                {
                    logger.LogError("Dependency validation failed for {dependency}!", dependency.Description);
                    result = false;
                }
            }

            results.Add(result);
        }

        return results.All(r => r);
    }

    public bool RefreshRelationConfigCookies(out string message)
    {
        message = string.Empty;

        ValidateRelationConfigConsistency();
        if (calibrationSetting.SettingCalibrationRelationParams.Count == 0)
        {
            message = "Please reset relation config in the file-setting page!";
            return false;
        }

        var relationParams = calibrationSetting.SettingCalibrationRelationParams
            .SelectMany(t => t.GetAllChildren())
            .ToList();

        foreach (var param in relationParams)
        {
            var ownDependencyNode = FindConfigInTrees(param.Item.DependencyRelationConfigs, param.SysMenu.Id);
            if (ownDependencyNode is null) continue;

            var entry = ApplicationCookie.CalibrationViewModelEntries.Values
                .FirstOrDefault(e => e.DTOType.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false) == ownDependencyNode.Item.AssemblyQualifiedName);

            if (entry is null) continue;

            entry.Dependencies = GetAllCheckedItems(param.Item.DependencyRelationConfigs).Select(t => t.Item).ToList();
        }

        return true;
    }

    #region Private

    /// <summary>
    /// 校验数据库存储的数据结构和实时entry结构是否一致，不一致默认清空缓存，提示重新在setting页面中配置
    /// </summary>
    private void ValidateRelationConfigConsistency()
    {
        if (_isValidateRelationConfigConsistency) return;
        var relationParams = calibrationSetting.SettingCalibrationRelationParams
            .SelectMany(t => t.GetAllChildren())
            .ToList();

        var entries = ApplicationCookie.CalibrationViewModelEntries.Values.ToList();

        if (relationParams.Count != entries.Count)
        {
            ResetCache();
            return;
        }

        var entryTypeNames = new HashSet<string>(entries
            .Select(e => e.DTOType.GetAssemblyQualifiedName(isIncludeVersion: false, isIncludeCulture: false, isIncludePublicKeyToken: false)));

        foreach (var param in relationParams)
        {
            var ownDependencyNode = FindConfigInTrees(param.Item.DependencyRelationConfigs, param.SysMenu.Id);
            var paramTypeName = ownDependencyNode?.Item.AssemblyQualifiedName;
            if (string.IsNullOrEmpty(paramTypeName))
            {
                ResetCache();
                return;
            }

            if (entryTypeNames.Contains(paramTypeName!) == false)
            {
                ResetCache();
                return;
            }
        }

        _isValidateRelationConfigConsistency = true;
        return;

        void ResetCache()
        {
            calibrationSetting.SettingCalibrationRelationParams = [];
            applicationCookieService.Set(calibrationSetting, false, CancellationToken.None);
            calibrationSetting.AdaptIn(calibrationSetting);

            logger.LogError("Calibration entries structure is changed! Please reset relation config in the file-setting page!");
        }
    }

    private static IReadOnlyList<SettingCalibrationRelationConfig> GetAllCheckedItems(IReadOnlyList<SettingCalibrationRelationConfig> roots)
    {
        var result = new List<SettingCalibrationRelationConfig>();
        foreach (var root in roots)
        {
            CollectChecked(root);
        }

        return result;

        void CollectChecked(SettingCalibrationRelationConfig node)
        {
            if (node.Item.IsUsed) result.Add(node);
            foreach (var child in node.Children) CollectChecked(child);
        }
    }

    private static SettingCalibrationRelationConfig? FindConfigInTrees(IReadOnlyList<SettingCalibrationRelationConfig> roots, long sysMenuId)
    {
        foreach (var root in roots)
        {
            var found = FindRecursive(root, sysMenuId);
            if (found is not null) return found;
        }

        return null;

        SettingCalibrationRelationConfig? FindRecursive(SettingCalibrationRelationConfig node, long targetId)
        {
            if (node.SysMenu.Id == targetId) return node;
            foreach (var child in node.Children)
            {
                var result = FindRecursive(child, targetId);
                if (result is not null) return result;
            }

            return null;
        }
    }

    #endregion
}