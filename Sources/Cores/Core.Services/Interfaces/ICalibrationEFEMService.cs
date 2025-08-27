using Core.Models.Enums.EFEM;
using Core.Models.Models.Common.EFEM;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationEFEMService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// Chuck是否晶圆
    /// </summary>
    /// <returns>是否有晶圆</returns>
    SxExecuteRet<bool> IsChuckLoadedWafer();

    /// <summary>
    /// 载入FOUP盒: FOUP 是用于半导体晶圆的封装和传输的标准化载体
    /// </summary>
    /// <param name="stationEnum">站点</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> LoadFoup(EFEMStationEnum stationEnum);

    /// <summary>
    /// 卸载Foup盒: FOUP 是用于半导体晶圆的封装和传输的标准化载体
    /// </summary>
    /// <param name="stationEnum">站点</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> UnLoadFoup(EFEMStationEnum stationEnum);

    /// <summary>
    /// 上料
    /// </summary>
    /// <param name="item">FOUP层数信息</param>
    /// <param name="angleEnum">角度</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> LoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum);

    /// <summary>
    /// Prealigner校准结果补偿后 上料验证
    /// </summary>
    /// <param name="item">FOUP层数信息</param>
    /// <param name="angleEnum">角度</param>
    /// <param name="offsetPoint">晶圆中心坐标偏移量</param>
    /// <param name="offsetAngle">晶圆角度偏移量</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> PreAlignerVerifyLoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum, Point offsetPoint, double offsetAngle);

    /// <summary>
    /// 下料
    /// </summary>
    /// <param name="item">FOUP层数信息</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> UnLoadWafer(EFEMFoupItem item);

    /// <summary>
    /// 获取FOUP盒层数数据
    /// </summary>
    /// <param name="stationEnum">站点</param>
    /// <returns>层数数据</returns>
    SxExecuteRet<List<EFEMFoupItem>> GetMapData(EFEMStationEnum stationEnum);
}