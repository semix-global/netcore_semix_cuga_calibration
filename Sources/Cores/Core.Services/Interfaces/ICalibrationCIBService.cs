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
    /// 切换自动增益 Auto Gain Control
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否自动增益</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable);

    /// <summary>
    /// 切换Log反差模式
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="cibProfileModeEnum">数据显示模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum);

    /// <summary>
    /// 切换所有PMT L0K
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否自动L0k</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable);

    /// <summary>
    /// 设置增益
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="gain">增益</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain);

    /// <summary>
    /// 切换Mark模式
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否Mark模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableMarkMode(IReadOnlyList<CIBInformation> cibInformations, bool enable);

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
    /// 获取Gain实测关系
    /// </summary>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="startGain">开始增益</param>
    /// <param name="stepGain">增益步进</param>
    /// <param name="stopGain">停止增益</param>
    /// <returns>实测值</returns>
    SxExecuteRet<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain);

    /// <summary>
    /// 读取所有CIB的图片
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="position">中心位置</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="imageWidth">图片宽度</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="isAutoFocus">是否自动聚焦</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken);

    /// <summary>
    /// 读取所有CIB的图片
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="startPosition">起点位置</param>
    /// <param name="endPosition">终点位置</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="isAutoFocus">是否自动聚焦</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken);

    /// <summary>
    /// 读取所有CIB的图片
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="startPosition">起点位置</param>
    /// <param name="endPosition">终点位置</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="startECS">ECS起点</param>
    /// <param name="stopECS">ECS终点</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的图片</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        double startECS,
        double stopECS,
        bool isForward,
        CancellationToken cancellationToken);
}