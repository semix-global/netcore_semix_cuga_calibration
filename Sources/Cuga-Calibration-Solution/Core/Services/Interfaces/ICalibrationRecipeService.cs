using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Common.Recipe.Wafer.WaferMap;
using Net.Utilities.Models;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationRecipeService
{
    WaferMapDataDto GetWaferMapData();

    WaferMapDto GetWaferMap();

    ReticleMarkDto GetReticleMark();

    bool GetWaferMapOffset(out Point offset);

    bool GetCorrectWaferMapByOffset(bool isAutoAlignment);

    bool GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo);

    bool GetChuckReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo);

    bool GetLaserReticleMaskMachineInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, StageSpeedEnum? stageSpeedEnum, out ReticleMarkItemDto maskInfo);

    bool GetReticleMaskBrightFieldPosition(WaferMapDieItemDto reticleCellDto, ReticleMarkItemDto maskDto, out Point position);
}