using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Documents;
using Net.Utilities.WaferMap.WPF.Drawables;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationRecipeService
{
    WaferMapCanvasDocument GetWaferMapCanvasDocument();

    ReticleMarkDto GetReticleMark();

    bool GetWaferMapOffset(out Point offset);

    bool GetCorrectWaferMapByOffset(bool isAutoAlignment);

    bool GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo);

    bool GetChuckReticleMaskInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, out ReticleMarkItemDto maskInfo);

    bool GetLaserReticleMaskMachineInfo(WaferMaskTypeEnum waferMaskType, MicroscopeMagnificationEnum? magnificationType, OpticsMagTypeEnum? opticsMagType, StageSpeedEnum? stageSpeedEnum, out ReticleMarkItemDto maskInfo);

    /// <summary>
    /// 获取指定Die对应的Mask明场位置
    /// </summary>
    /// <param name="waferMapDie"></param>
    /// <param name="maskDto"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    bool GetDieMaskBrightFieldPosition(WaferMapDie waferMapDie, ReticleMarkItemDto maskDto, out Point position);

    /// <summary>
    /// 获取指定Reticle对应的Mask明场位置
    /// </summary>
    /// <param name="waferMapReticle"></param>
    /// <param name="maskDto"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    bool GetReticleMaskBrightFieldPosition(WaferMapReticle waferMapReticle, ReticleMarkItemDto maskDto, out Point position);

}