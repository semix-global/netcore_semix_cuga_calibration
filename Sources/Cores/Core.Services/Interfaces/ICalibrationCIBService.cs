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
    /// 切换自动增益<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="enable">是否自动增益</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableAutoGainControl(IReadOnlyList<CIBInformation> cibInformations, bool enable);

    /// <summary>
    /// 切换Log反差模式<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="cibInformations">CIB信息列表</param>
    /// <param name="cibProfileModeEnum">数据显示模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum);

    /// <summary>
    /// 切换所有PMT L0K<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
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
    /// 切换Mark模式<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
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
    /// <param name="maxLogGain">LogGain 最大值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain);

    /// <summary>
    /// 设置LightMatching
    /// </summary>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="digitalGainPlusMultiplicativeFactors">数码增益+缩放系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors);

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
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDto>>> GetPMTImagesAsync(
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
    Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDto>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken);
}