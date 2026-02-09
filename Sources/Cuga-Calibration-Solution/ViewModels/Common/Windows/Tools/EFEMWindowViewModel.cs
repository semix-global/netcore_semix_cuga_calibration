using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.EFEM;
using Core.Models.Models.Common.EFEM;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
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
    private bool _isEnable = true;

    [ObservableProperty]
    private EFEMAngleEnum _angleEnum = EFEMAngleEnum.Down;

    [ObservableProperty]
    private EFEMFoupItem? _selectedFoupItem;

    [ObservableProperty]
    private bool _isPrealigner;

    [ObservableProperty]
    private bool _prealignerIsOk;

    [ObservableProperty]
    private Point _offsetPoint;

    [ObservableProperty]
    private double _offsetAngle;

    /// <summary>
    /// 花篮载具1
    /// </summary>
    public ObservableCollection<EFEMFoupItem> EFEMFoupLoadPort1 { get; } = [.. Enumerable.Range(0, 25).Select(i => new EFEMFoupItem { StationEnum = EFEMStationEnum.P1, SlotId = 25 - i, IsHasWafer = false })];

    /// <summary>
    /// 花篮载具2
    /// </summary>
    public ObservableCollection<EFEMFoupItem> EFEMFoupLoadPort2 { get; } = [.. Enumerable.Range(0, 25).Select(i => new EFEMFoupItem { StationEnum = EFEMStationEnum.P2, SlotId = 25 - i, IsHasWafer = false })];

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
            efemViewModel.UnLoadFoup(stationEnum);

            foreach (var item in stationEnum == EFEMStationEnum.P1 ? EFEMFoupLoadPort1 : EFEMFoupLoadPort2)
                item.IsHasWafer = false;
        });
    }

    [RelayCommand]
    private Task GetMapDataAsync(EFEMStationEnum efemStationEnum)
    {
        return InvokeAsync(() => { GetMapData(efemStationEnum); });
    }

    [RelayCommand]
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
        });
    }

    [RelayCommand]
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
        var result = efemViewModel.GetMapData(efemStationEnum);

        foreach (var efemFoupItem in result)
        {
            var foupItem = efemStationEnum == EFEMStationEnum.P1
                ? EFEMFoupLoadPort1.SingleOrDefault(t => t.SlotId == efemFoupItem.SlotId && t.StationEnum == efemFoupItem.StationEnum)
                : EFEMFoupLoadPort2.SingleOrDefault(t => t.SlotId == efemFoupItem.SlotId && t.StationEnum == efemFoupItem.StationEnum);
            if (foupItem is null) continue;
            foupItem.IsHasWafer = efemFoupItem.IsHasWafer;
            foupItem.IsLoadWafer = efemFoupItem.IsLoadWafer;
        }
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