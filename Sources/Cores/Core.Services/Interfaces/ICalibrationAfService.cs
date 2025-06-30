using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

/// <summary>
/// AutoFocus自动聚焦服务
/// </summary>
public interface ICalibrationAfService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 明场启用(工作模式)/禁用(ECS模式)
    /// </summary>
    /// <param name="enable">是否启用</param>
    /// <returns>启用是否成功</returns>
    SxExecuteRet<bool> ToggleBrightFieldEnable(bool enable);

    /// <summary>
    /// 暗场启用(工作模式)/禁用(NSC模式)
    /// </summary>
    /// <param name="enable">是否启用</param>
    /// <returns>启用是否成功</returns>
    SxExecuteRet<bool> ToggleDarkFieldEnable(bool enable);

    /// <summary>
    /// 切换CalChip模式
    /// </summary>
    /// <param name="calChipSiteModelEnum">CalChip模式</param>
    /// <returns>切换是否成功</returns>
    SxExecuteRet<bool> ToggleCalChipSiteModelEnum(CalChipSiteModelEnum calChipSiteModelEnum);

    /// <summary>
    /// 获取ECS当前值
    /// </summary>
    /// <returns>ECS当前值</returns>
    SxExecuteRet<double> GetSensorEcsValue();

    /// <summary>
    /// 获取平均ECS当前值
    /// </summary>
    /// <returns>ECS当前值</returns>
    SxExecuteRet<double> GetSensorAverageEcsValue();

    /// <summary>
    /// 获取是否是复查状态
    /// </summary>
    /// <returns>是否是复查状态, Ecs值</returns>
    SxExecuteRet<(bool IsReview, double CurrentEcsValue)> GetSensorIsReviewValue();

    /// <summary>
    /// 移动ECS
    /// </summary>
    /// <param name="ecs">ecs值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorEcsValue(double ecs);

    /// <summary>
    /// 移动显微镜镜头
    /// </summary>
    /// <param name="microscopeMagnificationEnum">显微镜镜头</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorMicroscopeObjValue(MicroscopeMagnificationEnum microscopeMagnificationEnum);

    /// <summary>
    /// 测试NSC曲线，得到曲线上下限，判断曲线上下限是否大于0.25
    /// </summary>
    /// <returns>测试NSC曲线，得到曲线上下限，判断曲线上下限是否大于0.25</returns>
    SxExecuteRet<bool> GetSensorNscCurveIsOk();

    /// <summary>
    /// 读取F N的值
    /// <param name="isA">是A路还是B路</param>
    /// </summary>
    /// <returns>F N的值</returns>
    SxExecuteRet<(double F, double N)> GetSensorFnValue(bool isA);

    /// <summary>
    /// 读取电流的值
    /// <param name="isA">是A路还是B路</param>
    /// </summary>
    /// <returns>电流的值</returns>
    SxExecuteRet<double> GetSensorCurrentValue(bool isA);

    /// <summary>
    /// 设置电流的值
    /// <param name="isA">是A路还是B路</param>
    /// <param name="current">电流的值</param>
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorCurrentValue(bool isA, double current);

    /// <summary>
    /// 获取Nsc补偿系数
    /// </summary>
    /// <returns>偏置NSC原始数据, 归一化增益</returns>
    SxExecuteRet<(double Offset, double Gain)> GetSensorNscCompensationCoefficient();

    /// <summary>
    /// 设置Nsc补偿系数
    /// <param name="offset">偏置NSC原始数据</param>
    /// <param name="gain">归一化增益</param>
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorNscCompensationCoefficient(double offset, double gain);

    /// <summary>
    /// 获取传感器: TracesBuffer error的Buffer值
    /// </summary>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TraceBuffer error当前值</returns>
    SxExecuteRet<List<double>> GetSensorAfErrorTraceBufferList(TimeSpan timeSpan);

    /// <summary>
    /// 获取传感器: TraceBuffer Nsc的Buffer值
    /// </summary>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TraceBuffer error当前值</returns>
    SxExecuteRet<List<double>> GetSensorNscTraceBufferList(TimeSpan timeSpan);

    /// <summary>
    /// 获取传感器: TracesBuffer ECS NSC Lvdt 的Buffer值
    /// </summary>
    /// <param name="startEcs">起始Ecs</param>
    /// <param name="endEcs">结束Ecs</param>
    /// <param name="speedEcs">速度Ecs</param>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TraceBuffer ECS NSC Lvdt当前值</returns>
    SxExecuteRet<List<(double Ecs, double Nsc, double Lvdt)>> GetNscCompensationCoefficientTraceBufferList(double startEcs, double endEcs, double speedEcs, TimeSpan timeSpan);

    #region 自动聚焦下发参数

    /// <summary>
    /// 设置明场chuck中心的机械位置
    /// </summary>
    /// <param name="position">明场chuck中心的机械位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorBrightFieldChuckCenterMachinePositionValue(Point position);

    /// <summary>
    /// 设置明场倍镜的清晰度最好的标准Ecs值
    /// </summary>
    /// <param name="microscopeMagnificationEnum">显微镜倍率</param>
    /// <param name="standardEcsValue">清晰度最好的标准Ecs值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorBrightFieldChuckStandardEcsValue(MicroscopeMagnificationEnum microscopeMagnificationEnum, double standardEcsValue);

    /// <summary>
    /// 设置明场耳朵中心的机械位置
    /// </summary>
    /// <param name="calChipSiteModelEnum">耳朵</param>
    /// <param name="position">明场chuck中心的机械位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorBrightFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position);

    /// <summary>
    /// 设置明场耳朵的清晰度最好的标准Ecs值(5X)
    /// </summary>
    /// <param name="calChipSiteModelEnum">耳朵</param>
    /// <param name="standardEcsValue">清晰度最好的标准Ecs值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorBrightFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue);

    /// <summary>
    /// 设置暗场chuck中心的机械位置
    /// </summary>
    /// <param name="position">暗场chuck中心的机械位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorDarkFieldChuckCenterMachinePositionValue(Point position);

    /// <summary>
    /// 设置暗场耳朵中心的机械位置
    /// </summary>
    /// <param name="calChipSiteModelEnum">耳朵</param>
    /// <param name="position">暗场chuck中心的机械位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorDarkFieldCalChipCenterMachinePositionValue(CalChipSiteModelEnum calChipSiteModelEnum, Point position);

    /// <summary>
    /// 设置暗场Chuck清晰度最好的标准Ecs值
    /// </summary>
    /// <param name="standardEcsValue">清晰度最好的标准Ecs值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorDarkFieldChuckStandardEcsValue(double standardEcsValue);

    /// <summary>
    /// 设置暗场耳朵的清晰度最好的标准Ecs值
    /// </summary>
    /// <param name="calChipSiteModelEnum">耳朵</param>
    /// <param name="standardEcsValue">清晰度最好的标准Ecs值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorDarkFieldCalChipStandardEcsValue(CalChipSiteModelEnum calChipSiteModelEnum, double standardEcsValue);

    /// <summary>
    /// 设置暗场自动聚焦电机的位置, 会等待电机到位(影响自动聚焦值)
    /// </summary>
    /// <param name="value">自动聚焦电机的位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDarkFieldAutoFocusMotorAbsoluteValue(double value);

    #endregion 自动聚焦下发参数

    #region RTFC

    /// <summary>
    /// Chip DSW自动聚焦
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<(double Ecs, double Score)> CalChipDswAfRtfc(Point position);

    /// <summary>
    /// Chip Haze自动聚焦
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<(double Ecs, double Score)> CalChipHazeAfRtfc(Point position);

    /// <summary>
    /// Chuck自动聚焦
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<(double Ecs, double Height)> ChuckAfRtfc(Point position);

    /// <summary>
    /// Nsc 诊断
    /// </summary>
    /// <returns>Ecs-Nsc traceBuffer</returns>
    SxExecuteRet<(Point[] tracebuffer, double k)> NscDiagnosis();

    #endregion RTFC
}