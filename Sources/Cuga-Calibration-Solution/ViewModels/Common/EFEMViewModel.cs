using Core.Models.Enums.EFEM;
using Core.Models.Exceptions;
using Core.Models.Models.Common.EFEM;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

// ReSharper disable once InconsistentNaming
[IOCAppService(ServiceType = typeof(EFEMViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class EFEMViewModel(ICalibrationEFEMService calibrationEfemService) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationEfemService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public void LoadFoup(EFEMStationEnum stationEnum)
    {
        var ret = calibrationEfemService.LoadFoup(stationEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void UnLoadFoup(EFEMStationEnum stationEnum)
    {
        var ret = calibrationEfemService.UnLoadFoup(stationEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void LoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum)
    {
        var ret = calibrationEfemService.LoadWafer(item, angleEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void PreAlignerVerifyLoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum, Point offsetPoint, double offsetAngle)
    {
        var ret = calibrationEfemService.PreAlignerVerifyLoadWafer(item, angleEnum, offsetPoint, offsetAngle);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void UnLoadWafer(EFEMFoupItem item)
    {
        var ret = calibrationEfemService.UnLoadWafer(item);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public List<EFEMFoupItem> GetMapData(EFEMStationEnum stationEnum)
    {
        var ret = calibrationEfemService.GetMapData(stationEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    # endregion 服务
}