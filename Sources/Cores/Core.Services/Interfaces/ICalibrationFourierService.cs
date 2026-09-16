using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using C2MFFRangeModel = Core.Models.Models.Common.Fourier.C2MFFRangeModel;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;

namespace Core.Services.Interfaces;

public interface ICalibrationFourierService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 通道回零
    /// </summary>
    /// <param name="channelId">1(channel 1), 2(channel 2), 3(channel 3)</param>
    SxExecuteRet<bool> Home(int channelId);

    /// <summary>
    /// 设置通道1或2挡杆位置
    /// </summary>
    /// <param name="channelId">1(channel 1), 2(channel 2)</param>
    /// <param name="rodPositions">挡杆位置，下标为杆号</param>
    SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions);

    /// <summary>
    /// 获取傅里叶相机的图片
    /// </summary>
    /// <param name="productivityInformation">生产率信息</param>
    /// <param name="laserLightInformation">激光光强</param>
    /// <param name="dfPosition">暗场位置</param>
    /// <param name="scanLength">扫描长度</param>
    /// <param name="channelId">1(channel 1), 2(channel 2), 3(channel 3)</param>
    /// <returns>傅里叶相机的图片</returns>
    SxExecuteRet<BitmapImage> GetImage(
        ProductivityInformation productivityInformation,
        LaserLightInformation laserLightInformation,
        Point dfPosition,
        double scanLength,
        int channelId);

    /// <summary>
    /// 获取傅里叶相机的配置
    /// </summary>  
    /// <returns>傅里叶相机的配置</returns>
    SxExecuteRet<C2MFFRangeModel> GetFourierConfig();

    SxExecuteRet<bool> FF_Move_CH3X(int rpos, double lpos, double ppos);

    SxExecuteRet<bool> FF_Move_CH3Y(int rpos, double lpos);

    /// <summary>
    /// 获取旋转电机MARK实时位置，只有CH3有（X和Y都有）
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    SxExecuteRet<double> GetFFRACT(CgFFCHEnum ch);

    /// <summary>
    /// 获取挡杆也就是位移电机实时位置，只有CH3有（X和Y都有）
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    SxExecuteRet<double> GetFFLACT(CgFFCHEnum ch);

    /// <summary>
    /// 获取推杆目标位置 只有CH3X有（只有X方向有）
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    SxExecuteRet<double> GetFFPACT(CgFFCHEnum ch);

    SxExecuteRet<bool> SetFFRPOS_CH3(FFCH ch, double pos);

    SxExecuteRet<bool> SetFFLPOS_CH3(FFCH ch, double pos);

    SxExecuteRet<bool> SetFFPPOS_CH3(FFCH ch, double pos);
}