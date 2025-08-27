using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationStageService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    #region 运动

    /// <summary>
    /// 启用/禁用物理摇杆
    /// </summary>
    /// <param name="enable">是否启用</param>
    /// <returns>启用是否成功</returns>
    SxExecuteRet<bool> ToggleEnableJoystick(bool enable);

    /// <summary>
    /// 设置平移台速度
    /// </summary>
    /// <param name="stageSpeedEnum">速度</param>
    /// <param name="opticsMagTypeEnum">暗场Mag</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSpeed(StageSpeedEnum stageSpeedEnum, OpticsMagTypeEnum opticsMagTypeEnum);

    /// <summary>
    /// 设置X轴速度数值
    /// </summary>
    /// <param name="speedValue"></param>
    /// <returns></returns>
    SxExecuteRet<bool> SetXSpeedValue(double speedValue);

    /// <summary>
    /// 设置Y轴速度数值
    /// </summary>
    /// <param name="speedValue"></param>
    /// <returns></returns>
    SxExecuteRet<bool> SetYSpeedValue(double speedValue);

    #region 旋转

    /// <summary>
    /// 获取Chuck的当前旋转角度
    /// </summary>
    /// <returns>当前旋转角度</returns>
    SxExecuteRet<double> GetMachineStageTheta();

    /// <summary>
    /// 旋转Chuck的相对角度位置
    /// </summary>
    /// <param name="degrees">相对的旋转角度</param>
    /// <returns>旋转是否成功</returns>
    SxExecuteRet<bool> MoveRelativeStageTheta(double degrees);

    /// <summary>degrees
    /// 旋转Chuck到绝对角度位置
    /// </summary>
    /// <param name="degrees">绝对的旋转角度</param>
    /// <returns>旋转是否成功</returns>
    SxExecuteRet<bool> SetAbsoluteStageTheta(double degrees);

    #endregion 旋转

    #region 相对运动

    /// <summary>
    /// 相对移动平台(步进的形式)
    /// </summary>
    /// <param name="dir">方向</param>
    /// <param name="step">步进距离</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> MoveRelativeStageXy(StageDirectionTypeEnum dir, double step);

    #endregion 相对运动

    #region 明场运动

    /// <summary>
    /// 获取平台明场当前位置
    /// </summary>
    /// <returns>明场当前位置</returns>
    SxExecuteRet<Point> GetBrightFieldStagePosition();

    /// <summary>
    /// 明场移动平台(步进的形式)
    /// </summary>
    /// <param name="point">点</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetBrightFieldAbsoluteStageXy(Point point);

    #endregion 明场运动

    #region 暗场运动

    /// <summary>
    /// 获取平台暗场当前位置
    /// </summary>
    /// <returns>暗场当前位置</returns>
    SxExecuteRet<Point> GetDarkFieldStagePosition();

    /// <summary>
    /// 暗场移动平台(步进的形式)
    /// </summary>
    /// <param name="point">点</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDarkFieldAbsoluteStageXy(Point point);

    #endregion 暗场运动

    #region 机械移动

    /// <summary>
    /// 获取平台机械当前位置
    /// </summary>
    /// <returns>机械当前位置</returns>
    SxExecuteRet<Point> GetMachineStagePosition();

    /// <summary>
    /// 机械移动平台(步进的形式)
    /// </summary>
    /// <param name="point">点</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetMachineAbsoluteStageXy(Point point);

    /// <summary>
    /// 获取机械方向
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<(double XDirection, double YDirection)> GetMachineDirection();

    /// <summary>
    /// Y轴初始化
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> InitYAxis();

    /// <summary>
    /// 初始化后Y Offset校准到初始正交姿态
    /// </summary>
    /// <returns>BeforeOffset：摆动前 AfterOffset：摆动后</returns>
    SxExecuteRet<(double BeforeOffset, double AfterOffset)> OriginYOffsetCalibration();

    #endregion 机械移动

    /// <summary>
    /// 明场转机械坐标
    /// </summary>
    /// <param name="point">明场坐标</param>
    /// <returns>机械坐标</returns>
    SxExecuteRet<Point> BrightFieldToMachinePosition(Point point);

    /// <summary>
    /// 暗场转机械坐标
    /// </summary>
    /// <param name="point">暗场坐标</param>
    /// <returns>机械坐标</returns>
    SxExecuteRet<Point> DarkFieldToMachinePosition(Point point);

    /// <summary>
    /// 机械转明场坐标
    /// </summary>
    /// <param name="point">机械坐标</param>
    /// <returns>明场坐标</returns>
    SxExecuteRet<Point> MachineToBrightFieldPosition(Point point);

    /// <summary>
    /// 机械转暗场坐标
    /// </summary>
    /// <param name="point">机械坐标</param>
    /// <returns>暗场坐标</returns>
    SxExecuteRet<Point> MachineToDarkFieldPosition(Point point);

    #endregion 运动

    #region 晶圆对准P8

    /// <summary>
    /// 查找晶圆中心(P8) 自动模式
    /// </summary>
    /// <param name="offsetThreshold">晶圆中心坐标与Chuck Center坐标的偏移量的阈值</param>
    /// <returns>多次尝试后 符合阈值的情况下晶圆中心坐标与Chuck Center坐标的累计偏移量</returns>
    SxExecuteRet<Point> FindWaferCenterByAutomatic(int offsetThreshold = 100);

    /// <summary>
    /// 查找晶圆中心(P8) 手动模式
    /// </summary>
    /// <param name="bitmapMemoryBytes">查找晶圆中心返回的8张图片</param>
    /// <param name="offset">上一次晶圆中心的偏移量</param>
    /// <param name="waferEdgeOffsets">手动标记的晶圆边缘图片的像素坐标偏移量</param>
    /// <returns></returns>
    SxExecuteRet<Point> FindWaferCenterByManually(out List<byte[]> bitmapMemoryBytes, Point offset, List<Point>? waferEdgeOffsets = null);

    #endregion 晶圆对准P8

    #region 晶圆对准P5

    /*
    对准步骤：
    1.切换低倍镜，比如5x。
    2.手动MarkSite1 移动平移台，找有特征的图案 调用MarkAlignSite1(Size size) ，size模板大小，比如256x256 设定模板大小 ，返回值记为LowSite1。
    3.手动MarkSite2 移动平移台，标记第二步相同特征的图案，调用MarkAlignSite2(C2MSiteDTO site) site传入MarkAlignSite1的返回值LowSite1。返回值记为LowSite2。
    4.切换高倍镜，比如50x。
    5.手动MarkSite1 移动平移台，手动调整stage位置，标记特征的图案，可以和第二步相同，也可以不同，调用MarkAlignSite1(Size size) ，size模板大小，比如256x256 设定模板大小 ，返回值记为HighSite1。
    6.手动MarkSite1 移动平移台，手动调整stage位置，标记第五步相同的特征的图案，调用MarkAlignSite2(C2MSiteDTO site) site传入MarkAlignSite1的返回值HighSite1。返回值记为HighSite2。
    7.执行晶圆对准，如果调用Alignment(),C2MAlignResult.Degrees得到最终的偏移角度θ 内部会执行三次，自我补偿校验，所以直接得到最终的offset θ 。
    8.如果调用AlignmentVerify()，不进行自我补偿校验，直接得到offset θ 值。
    */

    /// <summary>
    /// 设置标记点1
    /// </summary>
    /// <param name="algorithmTemplateSizeEnum">标记的模板尺寸大小</param>
    /// <param name="algorithmTemplateTypeEnum">算法匹配类型</param>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    /// <returns>标记点1的坐标和模板</returns>
    SxExecuteRet<AlignmentSiteDto> MarkAlignSite1(
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    );

    /// <summary>
    /// 设置标记点2
    /// </summary>
    /// <param name="site">MarkAlignSite1的返回值</param>
    /// <returns>标记点2的坐标和模板</returns>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    SxExecuteRet<AlignmentSiteDto> MarkAlignSite2(AlignmentSiteDto site, AlgorithmWaferTypeEnum algorithmWaferTypeEnum);

    /// <summary>
    /// 晶圆对准
    /// </summary>
    /// <param name="lowSite1">低倍镜下手动设置标记点1</param>
    /// <param name="lowSite2">低倍镜下手动设置标记点2</param>
    /// <param name="highSite1">高倍镜下手动设置标记点1</param>
    /// <param name="highSite2">高倍镜下手动设置标记点2</param>
    /// <param name="highMicroscopeLensInformation"></param>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    /// <param name="lowMicroscopeLensInformation"></param>
    /// <returns>晶圆的偏移角度和4个标记点的坐标</returns>
    SxExecuteRet<AlignmentResultDto> Alignment(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    );

    /// <summary>
    /// 晶圆对准 验证角度不校正
    /// </summary>
    /// <param name="lowSite1">低倍镜下手动设置标记点1</param>
    /// <param name="lowSite2">低倍镜下手动设置标记点2</param>
    /// <param name="highSite1">高倍镜下手动设置标记点1</param>
    /// <param name="highSite2">高倍镜下手动设置标记点2</param>
    /// <param name="lowMicroscopeLensInformation">对准使用的低倍镜</param>
    /// <param name="highMicroscopeLensInformation">对准使用的高倍镜</param>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    /// <returns>晶圆的偏移角度和4个标记点的坐标</returns>
    SxExecuteRet<AlignmentResultDto> AlignmentVerify(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    );

    #endregion 晶圆对准P5

    #region 暗场晶圆对准P5

    /// <summary>
    /// 设置标记点1
    /// </summary>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="algorithmTemplateSizeEnum">标记的模板尺寸大小</param>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    /// <returns>暗场标记点1的坐标和模板</returns>
    SxExecuteRet<AlignmentSiteDto> MarkAlignSite1DarkField(
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    );

    /// <summary>
    /// 设置标记点2
    /// </summary>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="site">MarkAlignSite1的返回值</param>
    /// <returns>暗场标记点2的坐标和模板</returns>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    SxExecuteRet<AlignmentSiteDto> MarkAlignSite2DarkField(
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        AlignmentSiteDto site,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum);

    /// <summary>
    /// 晶圆对准
    /// </summary>
    /// <param name="brightFieldLowSite1">低倍镜下手动设置标记点1</param>
    /// <param name="brightFieldLowSite2">低倍镜下手动设置标记点2</param>
    /// <param name="darkFieldHighSite1">暗场标记点1的坐标和模板</param>
    /// <param name="darkFieldHighSite2">暗场标记点2的坐标和模板</param>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="lowMicroscopeLensInformation">对准使用的低倍镜</param>
    /// <param name="algorithmWaferTypeEnum">晶圆类型</param>
    /// <returns>晶圆的偏移角度和4个标记点的坐标</returns>
    SxExecuteRet<AlignmentResultDto> AlignmentDarkField(
        AlignmentSiteDto brightFieldLowSite1,
        AlignmentSiteDto brightFieldLowSite2,
        AlignmentSiteDto darkFieldHighSite1,
        AlignmentSiteDto darkFieldHighSite2,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    );

    #endregion 暗场晶圆对准P5

    #region 设置校准

    /// <summary>
    /// 设置gantry
    /// </summary>
    /// <param name="gantryOffset">gantry offset</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGantryOffset(double gantryOffset);

    /// <summary>
    /// 补偿矩阵开关
    /// </summary>
    /// <param name="enable">是否启用</param>
    /// <returns>启用是否成功</returns>
    SxExecuteRet<bool> ToggleEnableStageMap(bool enable);

    /// <summary>
    /// 设置StageMap
    /// </summary>
    /// <param name="stageMapDto">stage map</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetStageMap(StageMapDto stageMapDto);

    /// <summary>
    /// 设置明场中心的机械位置
    /// </summary>
    /// <param name="position">新的明场中心Stage物理坐标</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetBrightFieldCenterMachinePositionValue(Point position);

    /// <summary>
    /// 设置暗场中心的机械位置
    /// </summary>
    /// <param name="position">新的明场中心Stage物理坐标</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDarkFieldCenterMachinePositionValue(Point position);

    /// <summary>
    /// 设置全局比例误差系数
    /// </summary>
    /// <param name="xScale">X轴比例系数</param>
    /// <param name="yScale">Y轴比例系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGlobalScaleErrorCoefficient(double xScale, double yScale);

    /// <summary>
    /// 设置旋转比例误差系数
    /// </summary>
    /// <param name="tScale">T轴比例系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetRotateScaleErrorCoefficient(double tScale);

    #region Stage物理坐标

    /// <summary>
    /// 获取当前明场中心的Stage物理坐标
    /// </summary>
    /// <returns>明场中心的Stage物理坐标</returns>
    SxExecuteRet<Point> GetBrightFieldCenterMachinePositionValue();

    /// <summary>
    /// 获取当前在上下料位置时的Stage物理坐标
    /// </summary>
    /// <returns>上下料位置的Stage物理坐标</returns>
    SxExecuteRet<Point> GetEfemLoadWaferMachineStagePosition();

    /// <summary>
    /// 获取当前在上下料位置时的Chuck的起始旋转角度
    /// </summary>
    /// <returns>Chuck的起始旋转角度</returns>
    SxExecuteRet<double> GetEfemLoadWaferMachineStageTheta();

    #endregion Stage物理坐标

    #endregion 设置校准
}