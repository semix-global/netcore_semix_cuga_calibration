using Core.Models.Models.Common.Version;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationVersionFactory
{
    CalibrationVersionDTO.VersionInfo? CalibrationDTOItemsConvertToVersionInfo(Type type, long? id = null);

    CalibrationVersionDTO.VersionInfo? CalibrationDTOConvertToVersionInfo(Type type, long? id = null);

    CalibrationVersionDTO CreateInstanceFromCurrentDatabase(string filePath);
}