using CommunityToolkit.Diagnostics;
using Core.Models.Models;
using Core.Wcf.Models;
using Net.Utilities.Mapper.Interfaces;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace Core.Models.Helper;

public static class CalibrationReflectionHelper
{
    /// <summary>
    /// 静态缓存,避免多次反射
    /// </summary>
    private static IReadOnlyList<CalibrationCategory> _historyCaches = [];

    public record CalibrationCategory(string Description, Type WcfCategoryType, IReadOnlyList<CalibrationCategoryItem> Items);

    public sealed record CalibrationCategoryItem(Type WcfModelType, Type CalibrationDtoType, MethodInfo CalibrationDtoToWcfModelMethodInfo, bool IsArray, string Description);

    public static IReadOnlyList<CalibrationCategory> GetCalibrationDescriptionList()
    {
        if (_historyCaches.Count != 0) return _historyCaches;
        var resultList = new List<CalibrationCategory>();

        foreach (var fatherPropertyInfo in typeof(CalibrationObj).GetProperties())
        {
            var items = new List<CalibrationCategoryItem>();
            var calibrationCategory = new CalibrationCategory(Guard.IsNotNullAndReturn(fatherPropertyInfo.GetCustomAttribute<DescriptionAttribute>()).Description, fatherPropertyInfo.PropertyType, items);

            foreach (var property in fatherPropertyInfo.PropertyType.GetProperties())
            {
                var isArray = typeof(IEnumerable).IsAssignableFrom(property.PropertyType);

                var childCalibrationWcfType = Guard.IsNotNullAndReturn(isArray ? property.PropertyType.GetElementType() : property.PropertyType);
                var childCalibrationDtoInfo = WcfModelTypeToCalibrationDtoType(childCalibrationWcfType);

                items.Add(new CalibrationCategoryItem(childCalibrationWcfType, childCalibrationDtoInfo.dtoType, childCalibrationDtoInfo.MethodInfo, isArray, Guard.IsNotNullAndReturn(childCalibrationDtoInfo.dtoType.Namespace)));
            }

            resultList.Add(calibrationCategory);
        }

        _historyCaches = [.. resultList];

        return resultList;
    }

    public static (Type dtoType, MethodInfo MethodInfo) WcfModelTypeToCalibrationDtoType(Type wcfObjType)
    {
        // 查找程序集中所有类型，筛选出实现IAdaptTo接口，且接口参数为wcfObjType的类型，返回wcfObjType对应的dto类型
        var assemblyTypes = typeof(CalibrationDtoBase).Assembly.GetTypes();
        var adaptToInterfaceType = typeof(IAdaptTo<>);
        var targetDtoInfo = assemblyTypes.Select(t =>
        {
            var interfaceTypes = t.GetInterfaces();
            // 获取该泛型接口的类型参数
            var dtoInterfaceTypes = interfaceTypes.Where(o => o.IsGenericType && o.GetGenericTypeDefinition() == adaptToInterfaceType);
            var matchInterface = dtoInterfaceTypes.SingleOrDefault(type => type.GetGenericArguments().SingleOrDefault(paramType => paramType == wcfObjType) is not null);
            if (matchInterface is null) return (null, null);

            var methodInfo = matchInterface.GetMethods().Single(methodInfo => methodInfo.ReturnType == wcfObjType);
            return (dtoType: t, methodInfo);
        }).Single(t => t.dtoType is not null);

        return (Guard.IsNotNullAndReturn(targetDtoInfo.dtoType), Guard.IsNotNullAndReturn(targetDtoInfo.methodInfo));
    }
}