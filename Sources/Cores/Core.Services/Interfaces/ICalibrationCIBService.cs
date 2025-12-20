using Core.Models.Enums.Optics;
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
    /// <param name="cibInformation">CIB信息</param>
    /// <param name="digitalGainPlusMultiplicativeFactors">数码增益+缩放系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetLightMatching(CIBInformation cibInformation, double digitalGainPlusMultiplicativeFactors);

    /// <summary>
    /// 读取所有CIB的PMT数据
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="productivityInformation">产率</param>
    /// <param name="stageCoordinateSystemEnum">位置坐标系</param>
    /// <param name="position">什么位置</param>
    /// <param name="cibInformations">CIB列表</param>
    /// <param name="imageWidth">图片宽度</param>
    /// <param name="isAutoFocus">是否自动聚焦</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CIB对应的PMT数据</returns>
    Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDto>>> GetPMTValuesAsync(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isAutoFocus,
        CancellationToken cancellationToken);
}