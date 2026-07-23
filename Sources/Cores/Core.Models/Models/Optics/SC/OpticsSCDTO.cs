using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using System.ComponentModel;
using Net.Utilities.Helpers.Extensions;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;

namespace Core.Models.Models.Optics.SC;

[CacheVersion("1.0.0")]
public sealed partial class OpticsSCDTO : CalibrationDTOBase<OpticsSCDTO>, IAdaptTo<CalibrationOpticsSC>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<OpticsSCDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SCMotorAbsoluteValueL1), nameof(SCMotorAbsoluteValueL3), nameof(Lambda))]
    public partial OpticsSCDTOItem? MaxItem { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public double Lambda => MaxItem?.Lambda ?? 0;

    [Newtonsoft.Json.JsonIgnore]
    public double SCMotorAbsoluteValueL1 => MaxItem?.SCMotorAbsoluteValueL3 ?? 0;

    [Newtonsoft.Json.JsonIgnore]
    public double SCMotorAbsoluteValueL3 => MaxItem?.SCMotorAbsoluteValueL3 ?? 0;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<OpticsSCDTOItem>? oldValue, IReadOnlyList<OpticsSCDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsSCDTO()
    {
        ScatterPlotControl.SetTitle("SC(Y: Strehl Ratio - X: λ)");
    }

    private void RefreshPlot()
    {
        try
        {
            if (Items.Count <= 0) return;

            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(3);

            scatterLines[0].Update(
                "Y Strehl Ratio",
                [.. Items.Select(t => new Point(t.Lambda, t.BestFocus.BestYStrehlRatioPoint.Y))],
                Constants.Category10.GetColor(0));
            scatterLines[1].Update(
                "X Strehl Ratio",
                [.. Items.Select(t => new Point(t.Lambda, t.BestFocus.BestXStrehlRatioPoint.Y))],
                Constants.Category10.GetColor(1));
            scatterLines[2].Update(
                "Spot Area Percent Mean",
                [.. Items.Select(t => new Point(t.Lambda, t.BestFocus.SpotAreaPercentMean))],
                Constants.Category10.GetColor(2));

            MaxItem = Items.Minima(t => t.BestFocus.SpotAreaPercentMean).First();
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override OpticsSCDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        MaxItem = MaxItem?.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsSC AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        SCMotorAbsoluteValueL1 = SCMotorAbsoluteValueL1,
        SCMotorAbsoluteValueL3 = SCMotorAbsoluteValueL3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class OpticsSCDTOItem : ObservableObject, ICloneable<OpticsSCDTOItem>
{
    [ObservableProperty]
    public partial double Lambda { get; set; }

    [ObservableProperty]
    public partial double SCMotorAbsoluteValueL1 { get; set; }

    [ObservableProperty]
    public partial double SCMotorAbsoluteValueL3 { get; set; }

    [ObservableProperty]
    public partial BestFocus BestFocus { get; set; } = new();

    public OpticsSCDTOItem Clone() => new()
    {
        Lambda = Lambda,
        SCMotorAbsoluteValueL1 = SCMotorAbsoluteValueL1,
        SCMotorAbsoluteValueL3 = SCMotorAbsoluteValueL3,
        BestFocus = BestFocus.Clone()
    };
}