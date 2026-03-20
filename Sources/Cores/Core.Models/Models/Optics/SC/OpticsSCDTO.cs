using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Core.Models.Models.Common.DarkField;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Optics.SC;

public sealed partial class OpticsSCDTO : CalibrationDtoBase, ICloneable<OpticsSCDTO>, IAdaptTo<CalibrationOpticsSC>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private IReadOnlyList<OpticsSCDTOItem> _items = [];


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SCMotorAbsoluteValueL1), nameof(SCMotorAbsoluteValueL3))]
    private OpticsSCDTOItem? _maxItem;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double SCMotorAbsoluteValueL1 => MaxItem?.SCMotorAbsoluteValueL3 ?? 0;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double SCMotorAbsoluteValueL3 => MaxItem?.SCMotorAbsoluteValueL3 ?? 0;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

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
        ScatterPlotControl.SetTitle("SC(Y: Strehl Ratio - X: mm)");
    }

    private void RefreshPlot()
    {
        try
        {
            if (Items.Count <= 0) return;

            ScatterPlotControl.GetOrAddScatterLine(
                string.Empty,
                [.. Items.Select(t => new Point(t.SCMotorAbsoluteValueL1, t.BestFocus.BestYStrehlRatioPoint.Y))],
                Constants.Category10.GetColor(0));

            MaxItem = Items.Maxima(t => t.BestFocus.BestYStrehlRatioPoint.Y).First();
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public OpticsSCDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        XZItems = [.. XZItems.Select(t => t.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitSCPoints = [.. FitSCPoints],
        XZSlope = XZSlope,
        XZIntercept = XZIntercept,
        XZRSquared = XZRSquared,
        XZFitSCPoints = [.. XZFitSCPoints],
        SCMotorRatio = SCMotorRatio,
        MinSCMotorAbsoluteValue = MinSCMotorAbsoluteValue,
        MaxSCMotorAbsoluteValue = MaxSCMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsSC AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        Slope = XZSlope,
        MinSCMotorAbsoluteValue = MinSCMotorAbsoluteValue,
        MaxSCMotorAbsoluteValue = MaxSCMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class OpticsSCDTOItem : ObservableObject, ICloneable<OpticsSCDTOItem>
{
    [ObservableProperty]
    private double _sCMotorAbsoluteValueL1;

    [ObservableProperty]
    private double _sCMotorAbsoluteValueL3;

    [ObservableProperty]
    private BestFocus _bestFocus = new();

    public OpticsSCDTOItem Clone() => new()
    {
        SCMotorAbsoluteValueL1 = SCMotorAbsoluteValueL1,
        SCMotorAbsoluteValueL3 = SCMotorAbsoluteValueL3,
        BestFocus = BestFocus.Clone()
    };
}