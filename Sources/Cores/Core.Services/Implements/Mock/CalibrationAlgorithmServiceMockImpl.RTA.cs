using Core.Models.Models.Common.DarkField;
using Core.Services.Interfaces;

namespace Core.Services.Implements.Mock;

public sealed partial class CalibrationAlgorithmServiceMockImpl
{
    public ICalibrationAlgorithmService.RTAScanSample[] GetRTAScanSamples(
        DarkFieldRawScanImageDTO rawScanImage,
        ICalibrationAlgorithmService.StageMapConfigDTO stageMapConfig,
        bool isForward,
        bool isRemoveLinear,
        Guid htmlLogUniqueId) => _calibrationAlgorithmServiceImpl.GetRTAScanSamples(
        rawScanImage,
        stageMapConfig,
        isForward,
        isRemoveLinear,
        htmlLogUniqueId);
}