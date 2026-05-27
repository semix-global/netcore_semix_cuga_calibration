using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Version;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationVersionFactory), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationVersionFactory(
    ICacheProvider cacheProvider,
    ApplicationCookie applicationCookie) : ICalibrationVersionFactory
{
    public CalibrationVersionDTO.VersionInfo? CalibrationDTOItemsConvertToVersionInfo(Type type, long? id = null)
    {
        var dtos = (id is null
            ? cacheProvider.GetOrDefaultArray(type)
            : cacheProvider.GetArray(type, id.Value)) as CalibrationDTOBase[];

        var dto = dtos?.FirstOrDefault();
        if (dto is null) return null;

        return new CalibrationVersionDTO.VersionInfo
        {
            Id = dto.Id,
            Version = SQLiteHelper.GetTableInfo(type).Version,
            TypeFullName = type.AssemblyQualifiedName ?? string.Empty
        };
    }

    public CalibrationVersionDTO.VersionInfo? CalibrationDTOConvertToVersionInfo(Type type, long? id = null)
    {
        if ((id is null
                ? cacheProvider.GetOrDefault(type)
                : cacheProvider.Get(type, id.Value)) is not CalibrationDTOBase dto) return null;

        return new CalibrationVersionDTO.VersionInfo
        {
            Id = dto.Id,
            Version = SQLiteHelper.GetTableInfo(type).Version,
            TypeFullName = type.AssemblyQualifiedName ?? string.Empty
        };
    }

    public CalibrationVersionDTO CreateInstanceFromCurrentDatabase(string filePath)
    {
        var calibrationVersionDTO = new CalibrationVersionDTO
        {
            ResultFilePath = filePath,
            CalibrationVersionInfos =
            [
                .. applicationCookie.CalibrationMenu.GetAllChildren()
                    .Select(tt => tt.Entry.IsArray
                        ? CalibrationDTOItemsConvertToVersionInfo(tt.Entry.DTOType)
                        : CalibrationDTOConvertToVersionInfo(tt.Entry.DTOType))
                    .Where(t => t is not null)
                    .Select(tt => tt!)
            ]
        };

        return calibrationVersionDTO;
    }
}