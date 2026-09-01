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
    /// 获取傅里叶相机的图片
    /// </summary>
    /// <param name="channelId">通道ID</param>
    /// <returns>傅里叶相机的图片</returns>
    SxExecuteRet<BitmapImage> GetFourierImage(int channelId);

    /// <summary>
    /// 获取傅里叶相机的图片
    /// </summary>
    /// <param name="id">通道ID</param>
    /// <param name="param">光强</param>
    /// <param name="pos">晶圆位置</param>
    /// <returns>傅里叶相机的图片</returns>
    SxExecuteRet<BitmapImage> GetFFReviewImgForTrigger(int id, ProductivityInformation productivityInformation, double level, Point pos, int width = 800);

    /// <summary>
    /// 获取傅里叶相机的配置
    /// </summary>  
    /// <returns>傅里叶相机的配置</returns>
    SxExecuteRet<C2MFFRangeModel> GetFourierConfig();

    SxExecuteRet<bool> FF_Move_CH12(FFCH channelId, List<(int rodnumber, double rodpos)> rodpostions);

    SxExecuteRet<bool> FF_Move_CH3X(int rpos, double lpos, double ppos);

    SxExecuteRet<bool> FF_Move_CH3Y(int rpos, double lpos);

    SxExecuteRet<bool> SetFFHome(FFCH ch);

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