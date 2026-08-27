using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using System.ComponentModel;


namespace Core.Models.Models.Optics.CollectPolarization;

[CacheVersion("1.0.1")]
public sealed partial class CollectPolarizationDTO : CalibrationDTOBase<CollectPolarizationDTO>, IAdaptTo<CalibrationCollectionPolarizationItem>
{
    [ObservableProperty]
    public partial OpticsPolarizationModeEnum OpticsPolarizationModeEnum { get; set; } = OpticsPolarizationModeEnum.P;

    [ObservableProperty]
    public partial OpticsCollectorPolarizationModeEnum OpticsCollectorPolarizationMode { get; set; } = OpticsCollectorPolarizationModeEnum.N;

    [ObservableProperty]
    public partial int ChannelId { get; set; } = -1;

    [ObservableProperty]
    public partial double NDFRotaryMotorPosition { get; set; }

    partial void OnNDFRotaryMotorPositionChanged(double oldValue, double newValue) => RefreshPlot();

    partial void OnGrayValueChanged(double oldValue, double newValue) => RefreshPlot();

    [ObservableProperty]
    public partial double GrayValue { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<CollectPolarizationDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial Point[] FitPoints { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();


#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<CollectPolarizationDTOItem>? oldValue,
        IReadOnlyList<CollectPolarizationDTOItem> newValue)
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

    partial void OnFitPointsChanged(Point[] oldValue, Point[] newValue) => RefreshPlot();

    public CollectPolarizationDTO()
    {
        ScatterPlotControl.Configure();

        ScatterPlotControl.SetTitle(0, "Relation (Y: Gray Value - X: NDF Rotary Pos(°))");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);

            if (Items.Count == 0) return;

            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(0, 2);

            Point[] points = [.. Items.Select(t => new Point(t.NDFRotaryMotorPosition, t.GrayValue)).OrderBy(t => t.X)];
            scatterLines[0].Update(
                $"Channel {ChannelId}",
                points,
                Constants.Turbo.GetColor(0));

            if (NDFRotaryMotorPosition == 0 || GrayValue == 0) return;
            var scatterMarkers = ScatterPlotControl.GetOrAddScatterMarkers(
                0,
                "Result Point",
                [new Point(NDFRotaryMotorPosition, GrayValue)],
                color: Colors.Red,
                MarkerShape.Asterisk);
            scatterMarkers.MarkerSize = 50;

            if (FitPoints.Length > 0)
                scatterLines[1].Update(
                    $"Channel {ChannelId} Fit",
                    FitPoints,
                    Constants.Turbo.GetColor(1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override CollectPolarizationDTO Clone() => new()
    {
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        OpticsCollectorPolarizationMode = OpticsCollectorPolarizationMode,
        ChannelId = ChannelId,
        NDFRotaryMotorPosition = NDFRotaryMotorPosition,
        GrayValue = GrayValue,
        Items = [.. Items.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationCollectionPolarizationItem AdaptTo() => new()
    {
        CgNDFTypeEnum = OpticsCollectorPolarizationMode.ToCgNDFTypeEnum(),
        ChannelId = ChannelId,
        NDFMotorPosition = NDFRotaryMotorPosition,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public object ToFlatnessHtmlAnonymous() => new
    {
        ChannelId,
        IsCalibrated,
        NDFRotaryMotorPosition,
        GrayValue,
        Analysis = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
        Images = new HtmlExpand("Images", new HtmlContainer([
            ..Items.Select(t =>
                new HtmlBullet(new
                {
                    MotorPos = t.NDFRotaryMotorPosition,
                    Image = new HtmlImage(t.ImageFilePath, $"Pos:{t.NDFRotaryMotorPosition},Gray:{t.GrayValue}")
                }))
        ]))
    };
}

public sealed partial class CollectPolarizationDTOItem : ObservableObject, ICloneable<CollectPolarizationDTOItem>
{
    [ObservableProperty]
    public partial double NDFRotaryMotorPosition { get; set; }

    [ObservableProperty]
    public partial double GrayValue { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;


    public CollectPolarizationDTOItem Clone() => new()
    {
        NDFRotaryMotorPosition = NDFRotaryMotorPosition,
        GrayValue = GrayValue,
        ImageFilePath = ImageFilePath
    };
}