using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Core.Recipe.Models;
using Core.Recipe.Models.Wafer.ReticleMask;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Documents;
using Net.Utilities.WaferMap.WPF.Drawables;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationRecipeService
{
    CalibrationRecipeDTO GetCorrectWaferMapByOffset(CalibrationRecipeDTO calibrationRecipeDTO, bool isAutoAlignment);

    bool GetMicroscopeReticleMaskInfo(ReticleMarkDto reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? opticsMagType, out ReticleMarkItemDto maskInfo);

    bool GetChuckReticleMaskInfo(ReticleMarkDto reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? productivityInformation, out ReticleMarkItemDto maskInfo);

    bool GetLaserReticleMaskMachineInfo(ReticleMarkDto reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? productivityInformation, out ReticleMarkItemDto maskInfo);

    /// <summary>
    /// 获取指定Die对应的Mask明场位置
    /// </summary>
    /// <param name="waferMapCanvasDocument"></param>
    /// <param name="waferMapDie"></param>
    /// <param name="maskDto"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    bool GetDieMaskBrightFieldPosition(WaferMapCanvasDocument waferMapCanvasDocument, WaferMapDie waferMapDie, ReticleMarkItemDto maskDto, out Point position);

    /// <summary>
    /// 获取指定Reticle对应的Mask明场位置
    /// </summary>
    /// <param name="waferMapCanvasDocument"></param>
    /// <param name="waferMapReticle"></param>
    /// <param name="maskDto"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    bool GetReticleMaskBrightFieldPosition(WaferMapCanvasDocument waferMapCanvasDocument, WaferMapReticle waferMapReticle, ReticleMarkItemDto maskDto, out Point position);
}