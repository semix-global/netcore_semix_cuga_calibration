using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Drawables;

namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationRecipeService
{
    /// <summary>
    ///  补偿WaferMap
    /// </summary>
    /// <param name="waferDto"></param>
    /// <param name="isAutoAlignment"></param>
    void GetCorrectWaferMapByOffset(WaferDTO waferDto, bool isAutoAlignment);

    /// <summary>
    /// 获取当前机械坐标所在的 Die
    /// </summary>
    WaferMapDie GetCurrentWaferMapDie(WaferDTO waferDto, Point machinePosition);

    /// <summary>
    /// 获取当前机械坐标所在的 Reticle
    /// </summary>
    WaferMapReticle GetCurrentWaferMapReticle(WaferDTO waferDto, Point machinePosition);

    /// <summary>
    /// WaferMapDie转机械坐标
    /// </summary>
    /// <param name="waferDto"></param>
    /// <param name="waferMapDie"></param>
    /// <param name="position"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    bool GetWaferMapDieMachinePosition<T>(WaferDTO waferDto, WaferMapDie<T> waferMapDie, out Point position) where T : Net.Utilities.Graphics.Primitives.Medias.Layer, new();

    /// <summary>
    /// 获取指定Die/Reticle对应的Mask明场机械坐标位置
    /// </summary>
    /// <param name="waferDto"></param>
    /// <param name="waferMapDie"></param>
    /// <param name="mask"></param>
    /// <param name="isReticle">True:Reticle模式,False:Die模式</param>
    /// <param name="position"></param>
    /// <returns></returns>
    bool GetMaskMachinePosition(WaferDTO waferDto, WaferMapDie waferMapDie, ReticleMarkDTOItem mask, bool isReticle, out Point position);

    bool GetMicroscopeReticleMaskInfo(ReticleMarkDTO reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? opticsMagType, out ReticleMarkDTOItem maskInfo);

    bool GetChuckReticleMaskInfo(ReticleMarkDTO reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? opticsMagType, out ReticleMarkDTOItem maskInfo);

    bool GetLaserReticleMaskMachineInfo(ReticleMarkDTO reticleMarkDto, WaferMaskTypeEnum waferMaskType, MicroscopeLensInformation? microscopeLensInformation, ProductivityInformation? opticsMagType, out ReticleMarkDTOItem maskInfo);

}