using Core.Models.Models;
using Local.SQL.Cache.Providers.Services.Interfaces;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationStatusService
{
    bool GetCalibrationDtoIsOKStatus<T>(out T calibrationDto, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new();

    bool GetCalibrationDtoItemsIsOKStatus<T>(out T[] calibrationDtoItems, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new();
}