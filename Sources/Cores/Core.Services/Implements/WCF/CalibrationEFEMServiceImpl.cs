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
using Net.Utilities.Models.Geometries;
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

    public SxExecuteRet<bool> IsChuckLoadedWafer() => Invoke(() => Service!.WaferOnChuck());

    public SxExecuteRet<bool> SyncFoupStatus(EFEMStationEnum stationEnum)
    {
        var sxExecuteRetState = Invoke(() => Service!.GetLoadPortSTA(stationEnum.ToESxStation()));
        if (sxExecuteRetState.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetState.Msg, false);
        return sxExecuteRetState.Anything switch
        {
            ESxFoupState.Opened => SxExecuteRetHelper.CreateSuccess(true),
            ESxFoupState.Exsit or ESxFoupState.Empty => SxExecuteRetHelper.CreateSuccess(false),
            ESxFoupState.Unknown => SxExecuteRetHelper.CreateError("EFEM is Abnormal! Foup status is unknown!", false),
            _ => SxExecuteRetHelper.CreateError($"Unsupported foup state: {sxExecuteRetState.Anything}", false)
        };
    }

    public SxExecuteRet<bool> LoadFoup(EFEMStationEnum stationEnum)
    {
        var foupStatusRet = SyncFoupStatus(stationEnum);
        if (foupStatusRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(foupStatusRet.Msg, false);

        // 已处于load状态
        if (foupStatusRet.Anything) return SxExecuteRetHelper.CreateSuccess(true);

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
        var sxExecuteRet = Invoke(() => Service!.PreAlignerVerify(item.StationEnum.ToESxStation(), item.SlotId.ToString(), angleEnum.ToEfemAngleEnum(), new CgPoint(offsetPoint.X, offsetPoint.Y), offsetAngle));

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
        // 必须处于load状态才能mapping；未 Load 返回空集合，状态未知/通信失败按错误返回
        var foupStatusRet = SyncFoupStatus(stationEnum);
        if (foupStatusRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<EFEMFoupItem>>(foupStatusRet.Msg, []);
        if (foupStatusRet.Anything == false) return SxExecuteRetHelper.CreateSuccess<List<EFEMFoupItem>>([]);

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