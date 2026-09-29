using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.EFEM;
using Core.Models.Models.Common.EFEM;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

/// <summary>
/// "EFEM" 通常代表 "Equipment Front End Module"，意为 "设备前端模块"
/// </summary>
// ReSharper disable InconsistentNaming
[IOCAppService(ServiceType = typeof(EFEMWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class EFEMWindowViewModel(
    EFEMViewModel efemViewModel,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider,
    ILogger<EFEMWindowViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    public partial bool IsEnable { get; set; } = true;

    [ObservableProperty]
    public partial EFEMAngleEnum AngleEnum { get; set; } = EFEMAngleEnum.Down;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadWaferCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnLoadWaferCommand))]
    public partial EFEMFoupItem? SelectedFoupItem { get; set; }

    /// <summary>
    /// FOUP1 是否处于 Load 状态
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadWaferCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnLoadWaferCommand))]
    public partial bool IsFoupLoadPort1Loaded { get; set; }

    /// <summary>
    /// FOUP2 是否处于 Load 状态
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadWaferCommand))]
    [NotifyCanExecuteChangedFor(nameof(UnLoadWaferCommand))]
    public partial bool IsFoupLoadPort2Loaded { get; set; }

    [ObservableProperty]
    public partial bool IsPrealigner { get; set; }

    [ObservableProperty]
    public partial bool PrealignerIsOk { get; set; }

    [ObservableProperty]
    public partial Point OffsetPoint { get; set; }

    [ObservableProperty]
    public partial double OffsetAngle { get; set; }

    /// <summary>
    /// 卸盒残片槽位记录: 卸盒时若 Chuck 上有该盒晶圆, 必须重新装盒后将残片卸回, 已上料标记跨装盒周期保留
    /// </summary>
    private EFEMFoupItem? _residualWaferItem;

    /// <summary>
    /// 花篮载具1
    /// </summary>
    public ObservableCollection<EFEMFoupItem> EFEMFoupLoadPort1 { get; } = [.. Enumerable.Range(0, 25).Select(i => new EFEMFoupItem { StationEnum = EFEMStationEnum.P1, SlotId = 25 - i, IsHasWafer = false })];

    /// <summary>
    /// 花篮载具2
    /// </summary>
    public ObservableCollection<EFEMFoupItem> EFEMFoupLoadPort2 { get; } = [.. Enumerable.Range(0, 25).Select(i => new EFEMFoupItem { StationEnum = EFEMStationEnum.P2, SlotId = 25 - i, IsHasWafer = false })];

    [RelayCommand]
    private Task LoadedAsync()
    {
        return InvokeAsync(() =>
        {
            SyncFoupLoadedStatus();

            foreach (var efemStationEnum in EnumHelper.Enums<EFEMStationEnum>())
            {
                RefreshFoupMapData(efemStationEnum);
            }
        });
    }

    [RelayCommand]
    private Task LoadFoupAsync(EFEMStationEnum stationEnum)
    {
        return InvokeAsync(() =>
        {
            efemViewModel.LoadFoup(stationEnum);
            GetMapData(stationEnum);
        });
    }

    [RelayCommand]
    private Task UnLoadFoupAsync(EFEMStationEnum stationEnum)
    {
        return InvokeAsync(() =>
        {
            // 卸盒时若 Chuck 上有该盒晶圆, 记录残片槽位: 重新装盒后必须先卸回残片才能继续操作
            var residualItem = (stationEnum == EFEMStationEnum.P1 ? EFEMFoupLoadPort1 : EFEMFoupLoadPort2).FirstOrDefault(t => t.IsLoadWafer);
            if (residualItem is not null) _residualWaferItem = residualItem;

            efemViewModel.UnLoadFoup(stationEnum);
            GetMapData(stationEnum);
        });
    }

    [RelayCommand]
    private Task GetMapDataAsync(EFEMStationEnum efemStationEnum)
    {
        return InvokeAsync(() => { GetMapData(efemStationEnum); });
    }

    /// <summary>
    /// 防呆: 选中的槽位所在的 FOUP 处于 Load 状态时才允许晶圆上下料
    /// </summary>
    private bool CanWafer => SelectedFoupItem is not null
                             && (SelectedFoupItem.StationEnum == EFEMStationEnum.P1 ? IsFoupLoadPort1Loaded : IsFoupLoadPort2Loaded);

    /// <summary>
    /// 防呆: 两个 FOUP 中任一槽位晶圆在 Chuck 上 (含残片) 时禁止上料 (单片系统)
    /// </summary>
    private bool CanLoadWafer => CanWafer
                                 && EFEMFoupLoadPort1.Any(t => t.IsLoadWafer) == false
                                 && EFEMFoupLoadPort2.Any(t => t.IsLoadWafer) == false;

    [RelayCommand(CanExecute = nameof(CanLoadWafer))]
    private Task LoadWaferAsync()
    {
        return InvokeAsync(() =>
        {
            PrealignerIsOk = false;
            if (SelectedFoupItem is null || SelectedFoupItem.IsHasWafer == false
                                         || EFEMFoupLoadPort1.Any(t => t.IsLoadWafer)
                                         || EFEMFoupLoadPort2.Any(t => t.IsLoadWafer))
            {
                dialogWindowProvider.ShowDialog("Please select a wafer item", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (IsPrealigner)
            {
                if (OffsetPoint != Point.Origin || OffsetAngle != 0) efemViewModel.PreAlignerVerifyLoadWafer(SelectedFoupItem, AngleEnum, OffsetPoint, OffsetAngle);
                else efemViewModel.LoadWafer(SelectedFoupItem, AngleEnum);
                PrealignerIsOk = true;
                IsPrealigner = false;
                OffsetPoint = Point.Origin;
                OffsetAngle = 0;
            }
            else efemViewModel.LoadWafer(SelectedFoupItem, AngleEnum);

            SelectedFoupItem.IsLoadWafer = true;
            GetMapData(SelectedFoupItem.StationEnum);
            dialogWindowProvider.TryShowDialog("Information", "Load wafer success!", out _);

            Close();
        });
    }

    [RelayCommand(CanExecute = nameof(CanWafer))]
    private Task UnLoadWaferAsync()
    {
        return InvokeAsync(() =>
        {
            if (SelectedFoupItem is null || SelectedFoupItem.IsHasWafer || SelectedFoupItem.IsLoadWafer == false)
            {
                dialogWindowProvider.ShowDialog("Please select a wafer item", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            efemViewModel.UnLoadWafer(SelectedFoupItem);

            // 残片已卸回, 解除保护
            if (ReferenceEquals(_residualWaferItem, SelectedFoupItem)) _residualWaferItem = null;

            SelectedFoupItem.IsLoadWafer = false;
            GetMapData(SelectedFoupItem.StationEnum);
        });
    }

    [RelayCommand(CanExecute = nameof(IsEnable))]
    private void Close()
    {
        CloseView(true);
    }

    private void GetMapData(EFEMStationEnum efemStationEnum)
    {
        SyncFoupLoadedStatus();
        RefreshFoupMapData(efemStationEnum);
    }

    /// <summary>
    /// 同步两个 FOUP 站位的装载状态 (两个 FOUP 同时存在, 一并同步避免单侧状态过时)
    /// </summary>
    private void SyncFoupLoadedStatus()
    {
        var isPort1Loaded = efemViewModel.SyncFoupStatus(EFEMStationEnum.P1);
        var isPort2Loaded = efemViewModel.SyncFoupStatus(EFEMStationEnum.P2);

        // CanExecuteChanged 必须在 UI 线程触发, 否则上下料按钮的启停状态不生效 (与 IsEnable 同模式)
        contextProvider.Send(() =>
        {
            IsFoupLoadPort1Loaded = isPort1Loaded;
            IsFoupLoadPort2Loaded = isPort2Loaded;
        });
    }

    /// <summary>
    /// 刷新指定站位 FOUP 的槽位映射数据
    /// </summary>
    private void RefreshFoupMapData(EFEMStationEnum efemStationEnum)
    {
        var result = efemViewModel.GetMapData(efemStationEnum);
        var port = efemStationEnum == EFEMStationEnum.P1 ? EFEMFoupLoadPort1 : EFEMFoupLoadPort2;

        if (result.Count == 0)
        {
            foreach (var item in port)
            {
                item.IsHasWafer = false;
                item.IsLoadWafer = false;
            }
        }
        else
        {
            foreach (var efemFoupItem in result)
            {
                var foupItem = port.SingleOrDefault(t => t.SlotId == efemFoupItem.SlotId && t.StationEnum == efemFoupItem.StationEnum);
                if (foupItem is null) continue;
                foupItem.IsHasWafer = efemFoupItem.IsHasWafer;
                foupItem.IsLoadWafer = efemFoupItem.IsLoadWafer;
            }
        }

        // 卸盒残片保护: 残片槽位的已上料标记跨装盒周期保留, 直到残片卸回 (重新装盒后才能卸片)
        if (_residualWaferItem is not null && _residualWaferItem.StationEnum == efemStationEnum)
            _residualWaferItem.IsLoadWafer = true;

        // 槽位状态已刷新 (含残片保护): 通知 Load 命令重估 Chuck 占用 (任一 FOUP 槽位在片即占用); CanExecuteChanged 须在 UI 线程触发
        contextProvider.Send(() => LoadWaferCommand.NotifyCanExecuteChanged());
    }

    private Task InvokeAsync(Action action)
    {
        return Task.Run(() =>
        {
            try
            {
                contextProvider.Send(() => IsEnable = false);
                action();
            }
            catch (Exception e)
            {
                logger.LogError(e, "{@Name}: Connecting Failed", nameof(EFEMWindowViewModel));
            }
            finally
            {
                contextProvider.Send(() => IsEnable = true);
            }
        });
    }
}