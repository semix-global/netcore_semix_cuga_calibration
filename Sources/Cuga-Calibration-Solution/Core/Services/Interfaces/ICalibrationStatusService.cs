using Core.Models.Models;
using Local.SQL.Cache.Providers.Services.Interfaces;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationStatusService
{
    T GetCalibration<T>() where T : CalibrationDTOBase, new();

    T[] GetCalibrations<T>() where T : CalibrationDTOBase, new();

    bool GetCalibrationDtoIsOKStatus<T>(out T calibrationDto, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new();

    bool GetCalibrationDtoItemsIsOKStatus<T>(out T[] calibrationDtoItems, out string errorMessage) where T : CalibrationDTOBase, ICacheItem, new();
}