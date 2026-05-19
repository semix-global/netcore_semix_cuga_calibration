using Core.Models.Extensions;
using Core.Models.Models;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationStatusService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationStatusServiceImpl(ICacheProvider cacheProvider) : ICalibrationStatusService
{
    public bool GetCalibrationDtoIsOKStatus<T>(out T calibrationDto, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new()
    {
        try
        {
            calibrationDto = cacheProvider.GetOrDefault<T>();
            var type = calibrationDto.GetType();
            errorMessage = nameof(type.Name);

            var extensionType = typeof(CoreWcfModelsExtension);
            // 获取方法信息
            var methodInfo = extensionType.GetMethods()
                .FirstOrDefault(m => m.Name == nameof(CoreWcfModelsExtension.IsOk) && m.GetParameters()[0].ParameterType == type);

            if (methodInfo is null)
            {
                errorMessage = "IsOk Extension Method not found!";
                return false;
            }

            // 调用方法
            var parameters = new object[] { calibrationDto, null! };
            var result = methodInfo.Invoke(null, parameters);
            errorMessage = parameters[1].ToString();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            calibrationDto = null!;
            return false;
        }
    }

    public bool GetCalibrationDtoItemsIsOKStatus<T>(out T[] calibrationDtoItems, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new()
    {
        try
        {
            calibrationDtoItems = cacheProvider.GetOrDefaultArray<T>();
            var type = calibrationDtoItems.GetType();
            errorMessage = nameof(type.Name);

            var extensionType = typeof(CoreWcfModelsExtension);

            // 获取方法信息
            var methodInfo = extensionType.GetMethods()
                .FirstOrDefault(m => m.Name == nameof(CoreWcfModelsExtension.IsOk) && m.GetParameters()[0].ParameterType == type);

            if (methodInfo is null)
            {
                errorMessage = "IsOk Extension Method not found!";
                return false;
            }

            // 调用方法
            var parameters = new object[] { calibrationDtoItems, null! };
            var result = methodInfo.Invoke(null, parameters);
            errorMessage = parameters[1].ToString();
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            calibrationDtoItems = null!;
            errorMessage = ex.Message;
            return false;
        }
    }
}