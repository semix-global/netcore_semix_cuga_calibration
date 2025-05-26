using Core.Models.Enums.EFEM;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.EFEM;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Stage;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;

namespace Core.Services.Implements.WCF;

// ReSharper disable once InconsistentNaming
[IOCAppService(ServiceType = typeof(ICalibrationEFEMService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationEFEMServiceImpl : BaseService<ICgCalibrationService>, ICalibrationEFEMService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<bool> LoadFoup(EFEMStationEnum stationEnum)
    {
        var sxExecuteRetState = Invoke(() => Service!.GetLoadPortSTA(stationEnum.ToESxStation()));
        if (sxExecuteRetState.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetState.Msg, false);
        if (sxExecuteRetState.Anything == ESxFoupState.Opened) return SxExecuteRetHelper.CreateSuccess(true); // 已经加载Foup盒了

        var sxExecuteRet = Invoke(() => Service!.LoadFoup(stationEnum.ToESxStation(), false /*不读FOUP盒的ID*/));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> UnLoadFoup(EFEMStationEnum stationEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.UnLoadFoup(stationEnum.ToESxStation()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> LoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.LoadWafer(item.StationEnum.ToESxStation(), item.SlotId.ToString(), angleEnum.ToEfemAngleEnum(), false));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> PreAlignerVerifyLoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum, Point offsetPoint, double offsetAngle)
    {
        var sxExecuteRet = Invoke(() => Service!.PreAlignerVerifyLoadWafer(item.StationEnum.ToESxStation(), item.SlotId.ToString(), angleEnum.ToEfemAngleEnum(), new CgPoint(offsetPoint.X, offsetPoint.Y), offsetAngle));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> UnLoadWafer(EFEMFoupItem item)
    {
        var sxExecuteRet = Invoke(() => Service!.UnloadWaferFormChuck(item.StationEnum.ToESxStation(), item.SlotId.ToString()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<EFEMFoupItem>> GetMapData(EFEMStationEnum stationEnum)
    {
        // 1-上手臂，2-下手臂，3-校准台，4-chuck台
        var sxExecuteRetState = Invoke(() => Service!.GetWaferInfo(4));
        if (sxExecuteRetState.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<EFEMFoupItem>>(sxExecuteRetState.Msg, []);
        var sxWaferInfoDto = sxExecuteRetState.Anything;

        List<EFEMFoupItem> efemFoupItems = [];
        var sxExecuteRet = Invoke(() => Service!.GetMapData(stationEnum.ToESxStation()));
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, efemFoupItems);

        var charArray = sxExecuteRet.Anything.ToCharArray();
        efemFoupItems.AddRange(charArray.Select((t, i) =>
        {
            var efemFoupItem = new EFEMFoupItem
            {
                StationEnum = stationEnum,
                SlotId = i + 1,
                IsHasWafer = t switch
                {
                    '1' => true,
                    '0' => false,
                    _ => false
                }
            };
            if (sxWaferInfoDto.OriginStation == stationEnum.ToESxStation() && int.TryParse(sxWaferInfoDto.SlotId, out var slotId) && slotId == efemFoupItem.SlotId) efemFoupItem.IsLoadWafer = true;

            return efemFoupItem;
        }));

        if (efemFoupItems.Count != 25) return SxExecuteRetHelper.CreateError<List<EFEMFoupItem>>("GetMapData is empty", []);

        return SxExecuteRetHelper.CreateSuccess(efemFoupItems);
    }
}