using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.EFEM;

namespace Core.Models.Models.Common.EFEM;

/// <summary>
/// EFEM中FOUP盒里面层数项目: "EFEM" 通常代表 "Equipment Front End Module"，意为 "设备前端模块", FOUP 是用于半导体晶圆的封装和传输的标准化载体
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed partial class EFEMFoupItem : ObservableObject
{
    /// <summary>
    /// 站点
    /// </summary>
    [ObservableProperty]
    private EFEMStationEnum _stationEnum;

    /// <summary>
    /// 层数 1 - 25
    /// </summary>
    [ObservableProperty]
    private int _slotId;

    /// <summary>
    /// 是否有料
    /// </summary>
    [ObservableProperty]
    private bool _isHasWafer;

    /// <summary>
    /// 是否上料
    /// </summary>
    [ObservableProperty]
    private bool _isLoadWafer;
}