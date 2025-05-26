using Core.Models.Enums.EFEM;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.EFEM;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Stage;
using Cuga.Interface.Calibration;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Semix.CoreLib;
using Semix.GRPC.DTO;

namespace Core.Services.Implements.GRPC;

// ReSharper disable once InconsistentNaming
[IOCAppService(ServiceType = typeof(ICalibrationEFEMService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationEFEMServiceImpl : BaseService<ICgCalibMWHService>, ICalibrationEFEMService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<bool> LoadFoup(EFEMStationEnum stationEnum)
    {
        var sxExecuteRetState = Invoke(() => Service?.GetLoadPortSTA(new SxParamObj<ESxStation>(stationEnum.ToESxStation())));
        if (sxExecuteRetState.IsSuccess) return SxExecuteRetHelper.CreateSuccess(true); // 已经加载Foup盒了

        var sxExecuteRet = Invoke(() => Service?.LoadFoup(new SxParamObj<(ESxStation station, bool isReadId)>((stationEnum.ToESxStation(), false /*不读FOUP盒的ID*/))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> UnLoadFoup(EFEMStationEnum stationEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.UnLoadFoup(new SxParamObj<ESxStation>(stationEnum.ToESxStation())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> LoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.LoadWafer(new SxParamObj<(ESxStation station, string slotId, int angle, bool isReadid)>((item.StationEnum.ToESxStation(), item.SlotId.ToString(), angleEnum.ToEfemAngleEnum(), false))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> PreAlignerVerifyLoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum, Point offsetPoint, double offsetAngle)
    {
        var sxExecuteRet = Invoke(() => Service?.PreAlignerVerifyLoadWafer(new SxParamObj<(ESxStation station, string slotId, int angle, CgPoint xyOffset, double chuckAngleOffset)>((item.StationEnum.ToESxStation(), item.SlotId.ToString(), angleEnum.ToEfemAngleEnum(), new CgPoint(offsetPoint.X, offsetPoint.Y), offsetAngle))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> UnLoadWafer(EFEMFoupItem item)
    {
        var sxExecuteRet = Invoke(() => Service?.UnLoadWafer(new SxParamObj<(ESxStation station, string slotId)>((item.StationEnum.ToESxStation(), item.SlotId.ToString()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<EFEMFoupItem>> GetMapData(EFEMStationEnum stationEnum)
    {
        // 1-上手臂，2-下手臂，3-校准台，4-chuck台
        var sxExecuteRetState = Invoke(() => Service?.GetWaferInfo(new SxParamObj<int>(4)));
        if (sxExecuteRetState.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<EFEMFoupItem>>(sxExecuteRetState.Msg, []);
        var sxWaferInfoDto = sxExecuteRetState.Anything;

        List<EFEMFoupItem> efemFoupItems = [];
        var sxExecuteRet = Invoke(() => Service?.GetMapData(new SxParamObj<ESxStation>(stationEnum.ToESxStation())));
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