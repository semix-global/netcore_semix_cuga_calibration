using Core.Models.Enums.EFEM;
using Core.Models.Exceptions;
using Core.Models.Helper;
using Core.Models.Models.Common.EFEM;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

// ReSharper disable once InconsistentNaming
[IOCAppService(ServiceType = typeof(ICalibrationEFEMService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationEFEMServiceMockImpl : ICalibrationEFEMService
{
    private static bool IsLp1Loaded;
    private static bool IsLp2Loaded;

    private static readonly List<EFEMFoupItem> Lp1 = [.. Enumerable.Range(0, 25).Select(i => new EFEMFoupItem { StationEnum = EFEMStationEnum.P1, SlotId = 25 - i, IsHasWafer = Random.Shared.NextDouble() > 0.5 })];
    private static readonly List<EFEMFoupItem> Lp2 = [.. Enumerable.Range(0, 25).Select(i => new EFEMFoupItem { StationEnum = EFEMStationEnum.P2, SlotId = 25 - i, IsHasWafer = Random.Shared.NextDouble() > 0.5 })];

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> IsChuckLoadedWafer()
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> LoadFoup(EFEMStationEnum stationEnum)
    {
        Thread.Sleep(2000);

        switch (stationEnum)
        {
            case EFEMStationEnum.P1:
                IsLp1Loaded = true;
                break;
            case EFEMStationEnum.P2:
                IsLp2Loaded = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stationEnum), stationEnum, null);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> UnLoadFoup(EFEMStationEnum stationEnum)
    {
        Thread.Sleep(100);

        switch (stationEnum)
        {
            case EFEMStationEnum.P1:
                IsLp1Loaded = false;
                break;
            case EFEMStationEnum.P2:
                IsLp2Loaded = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stationEnum), stationEnum, null);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> LoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum)
    {
        Thread.Sleep(100);
        var lp = item.StationEnum == EFEMStationEnum.P1 ? Lp1 : Lp2;
        var foupItem = lp.Single(t => t.SlotId == item.SlotId);
        foupItem.IsHasWafer = false;
        foupItem.IsLoadWafer = true;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> PreAlignerVerifyLoadWafer(EFEMFoupItem item, EFEMAngleEnum angleEnum, Point offsetPoint, double offsetAngle)
    {
        Thread.Sleep(100);
        var lp = item.StationEnum == EFEMStationEnum.P1 ? Lp1 : Lp2;
        var foupItem = lp.Single(t => t.SlotId == item.SlotId);
        foupItem.IsHasWafer = false;
        foupItem.IsLoadWafer = true;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> UnLoadWafer(EFEMFoupItem item)
    {
        Thread.Sleep(100);
        var lp = item.StationEnum == EFEMStationEnum.P1 ? Lp1 : Lp2;
        var foupItem = lp.Single(t => t.SlotId == item.SlotId);
        foupItem.IsHasWafer = true;
        foupItem.IsLoadWafer = false;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<EFEMFoupItem>> GetMapData(EFEMStationEnum stationEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(stationEnum == EFEMStationEnum.P1
            ? IsLp1Loaded
                ? Lp1
                : throw new CugaException("Currently in unload state, can't mapping!")
            : IsLp2Loaded
                ? Lp2
                : throw new CugaException("Currently in unload state, can't mapping!"));
    }
}