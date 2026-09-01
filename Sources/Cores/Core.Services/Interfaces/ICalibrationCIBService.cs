using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationCIBService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获取CIB信息列表
    /// </summary>
    /// <returns>CIB信息列表</returns>
    SxExecuteRet<IReadOnlyList<CIBInformation>> GetCIBInformations();

    /// <summary>
    /// 获取AGC状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <returns>是否启用</returns>
    SxExecuteRet<IReadOnlyList<bool>> GetAGC(IReadOnlyList<CIBInformation> cibInformations);

    /// <summary>
    /// 设置AGC状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否启用AGC</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable);

    /// <summary>
    /// 获取ProfileMode状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <returns>CIBProfileModeEnum</returns>
    SxExecuteRet<IReadOnlyList<CIBProfileModeEnum>> GetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations);

    /// <summary>
    /// 设置ProfileMode
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="cibProfileModeEnum">数据显示模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum);

    /// <summary>
    /// 获取L0K状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <returns>是否启用</returns>
    SxExecuteRet<IReadOnlyList<bool>> GetL0K(IReadOnlyList<CIBInformation> cibInformations);

    /// <summary>
    /// 设置L0K状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否启用L0K</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable);

    /// <summary>
    /// 获取Marker状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <returns>是否启用</returns>
    SxExecuteRet<IReadOnlyList<bool>> GetMarker(IReadOnlyList<CIBInformation> cibInformations);

    /// <summary>
    /// 设置Marker状态
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否启用Marker</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetMarker(IReadOnlyList<CIBInformation> cibInformations, bool enable);

    /// <summary>
    /// 设置增益
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="gain">增益</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain);

    /// <summary>
    /// 设置MMD
    /// </summary>
    /// <param name="cibInformation">CIB信息</param>
    /// <param name="logGainMul128U12Bits">LogGain * 128 [0, 4095]</param>
    /// <param name="gainS16Bits">GainS16Bit [-2^15, 2^15-1]</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits);

    /// <summary>
    /// 设置LightMatching
    /// </summary>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="digitalGainPlusMultiplicativeFactors">数码增益+缩放系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors);

    /// <summary>
    /// 设置照明文件
    /// </summary>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="illuminationProfiles">照明文件</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetIlluminationProfile(IReadOnlyList<CIBInformation> cibInformations, IReadOnlyList<double> illuminationProfiles);

    /// <summary>
    /// 获取延迟
    /// </summary>
    /// <param name="cibInformations">CIB列表</param>
    /// <returns>返回延迟</returns>
    SxExecuteRet<IReadOnlyList<CIBDelayDTO>> GetDelays(IReadOnlyList<CIBInformation> cibInformations);

    /// <summary>
    /// 设置延迟
    /// </summary>
    /// <param name="delays">延迟</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDelays(IReadOnlyList<CIBDelayDTO> delays);

    /// <summary>
    /// 实时下发XPixelSize 
    /// </summary>
    /// <param name="productivityInformation"></param>
    /// <param name="xPixelSize"></param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetXPixelSize(ProductivityInformation productivityInformation, double xPixelSize);

    /// <summary>
    /// 获取Gain实测关系
    /// </summary>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="startGain">开始增益</param>
    /// <param name="stepGain">增益步进</param>
    /// <param name="stopGain">停止增益</param>
    /// <returns>实测值</returns>
    SxExecuteRet<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain);

    /// <summary>
    /// 下发所有calchip模式的RTFC参数
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGlobalRTFCParams(ProductivityInformation productivityInformation);

    /// <summary>
    /// 读取所有CIB的图片: X 采[单位置]短图
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="centerPosition">中心位置</param>
    /// <param name="imageWidth">图片宽度</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="isAutoFocus">是否自动聚焦</param>
    /// <param name="isKeepRawImageCIBProfileModeEnum">是否返回原图(跳过转换为线性图)</param>
    /// <param name="isCustomAFParam">是否自定义AF参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        int imageWidth,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        bool isKeepRawImageCIBProfileModeEnum,
        bool isCustomAFParam,
        CancellationToken cancellationToken);

    /// <summary>
    /// 读取所有CIB的图片: X 采[多位置]短图
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="centerPositions">多个中心位置</param>
    /// <param name="imageWidth">图片宽度</param>
    /// <param name="cibInformation">CIB</param>
    /// <param name="isAutoFocus">是否自动聚焦</param>
    /// <param name="isKeepRawImageCIBProfileModeEnum">是否返回原图(跳过转换为线性图)</param>
    /// <param name="isCustomAFParam">是否自定义AF参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>多个中心位置对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        IReadOnlyList<Point> centerPositions,
        int imageWidth,
        CIBInformation cibInformation,
        bool isAutoFocus,
        bool isKeepRawImageCIBProfileModeEnum,
        bool isCustomAFParam,
        CancellationToken cancellationToken);

    /// <summary>
    /// 读取所有CIB的图片: X 采[单位置]长图
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="startPosition">起点位置</param>
    /// <param name="stopPosition">终点位置</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="isAutoFocus">是否自动聚焦</param>
    /// <param name="isKeepRawImageCIBProfileModeEnum">是否返回原图(跳过转换为线性图)</param>
    /// <param name="isCustomAFParam">是否自定义AF参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        bool isKeepRawImageCIBProfileModeEnum,
        bool isCustomAFParam,
        CancellationToken cancellationToken);

    /// <summary>
    /// 读取所有CIB的图片: X/Z 同步采[单位置]短图
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="startPosition">起点位置</param>
    /// <param name="stopPosition">终点位置</param>
    /// <param name="startECS">ECS起点</param>
    /// <param name="stopECS">ECS终点</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="isKeepRawImageCIBProfileModeEnum">是否返回原图(跳过转换为线性图)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        double startECS,
        double stopECS,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isKeepRawImageCIBProfileModeEnum,
        CancellationToken cancellationToken);

    /// <summary>
    /// 自动聚焦
    /// </summary>
    /// <param name="calChipSiteModelEnum">CalChip模式</param>
    /// <param name="productivityInformation">产率</param>
    /// <param name="cibInformation">CIB</param>
    /// <param name="centerMachinePosition">机械位置 null表示用cuga配置值</param>
    /// <param name="laserLightInformation">光强 null表示用cuga配置值</param>
    /// <returns>RTFC返回ECS、电机值、是否是AF伺服(True: AF / False: Relay)</returns>
    SxExecuteRet<(double ECS, double Motor, bool isAFServo)> RuntimeAFCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        ProductivityInformation productivityInformation,
        CIBInformation cibInformation,
        Point? centerMachinePosition = null,
        LaserLightInformation? laserLightInformation = null);
}