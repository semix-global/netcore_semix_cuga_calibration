using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

[CacheVersion("1.0.0")]
public partial class AODBestFocusAndAstigmatismDTO : CalibrationDTOBase<AODBestFocusAndAstigmatismDTO>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial OpticsApodizationModeEnum ApodizationModeEnum { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AODBestFocusAndAstigmatismDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial AODBestFocusAndAstigmatismDTOItem ResultDTO { get; set; } = new();

    [ObservableProperty]
    public partial double Slope { get; set; }

    [ObservableProperty]
    public partial double Intercept { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitPoints { get; set; } = [];

    partial void OnFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public AODBestFocusAndAstigmatismDTO()
    {
        var customGrid = new CustomGrid();
        PlotDataSource.Configure(customGrid, 3,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2, colSpan: 2));
            });

        PlotDataSource.SetTitle(0, "(Y Focus)(Y: mm/MHz - X: Y Focus(ecs))");
        PlotDataSource.SetTitle(1, "(X Focus)(Y: mm/MHz - X: X Focus(ecs))");
        PlotDataSource.SetTitle(2, "(XY Focus Offset)(Y: mm/MHz - X: XY Focus Offset(ecs))");
    }

    partial void OnItemsChanged(ObservableCollection<AODBestFocusAndAstigmatismDTOItem> oldValue, ObservableCollection<AODBestFocusAndAstigmatismDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }


    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear(0);
            PlotDataSource.Clear(1);
            PlotDataSource.Clear(2);

            if (Items.Count == 0) return;

            var yScatterLines = PlotDataSource.GetOrAddScatterLines(0, 2);
            yScatterLines[0].Update(
                "OriginPoints",
                [
                    .. Items.Select(t =>
                        new Point
                        (
                            t.BestFocus.BestYStrehlRatioECS,
                            1 / t.SpectralDensity
                        )
                    )
                ],
                Constants.Turbo.GetColor(0));

            if (FitPoints.Count != 0)
            {
                yScatterLines[1].Update(
                    $"{PolynomialCurve.ToString1(Slope, Intercept, RSquared, "0.######")}",
                    FitPoints,
                    Constants.Turbo.GetColor(1));
            }

            PlotDataSource.GetOrAddScatterLine(
                1,
                "OriginPoints",
                [
                    .. Items.Select(t =>
                        new Point
                        (
                            t.BestFocus.BestXStrehlRatioECS,
                            1 / t.SpectralDensity
                        )
                    )
                ],
                1,
                new Range(0, Items.Count - 1));

            PlotDataSource.GetOrAddScatterLine(
                2,
                "OriginPoints",
                [
                    .. Items.Select(t =>
                        new Point
                        (
                            t.XYBestFocusOffsetEcs,
                            1 / t.SpectralDensity
                        )
                    )
                ],
                2,
                new Range(0, Items.Count - 1));
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public object ToFlatnessHtmlAnonymous() => new
    {
        ProductivityInformation,
        ApodizationModeEnum,
        AstigmatismBestSpectralDensity = ResultDTO.SpectralDensity,
        AstigmatismBestChirpAODWaveformParam = new HtmlQuote(ResultDTO.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        Plot = new HtmlContainer([.. PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
    };

    public override AODBestFocusAndAstigmatismDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        ApodizationModeEnum = ApodizationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        ResultDTO = ResultDTO.Clone(),
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitPoints = [.. FitPoints],
        Id = Id,
        Expiration = Expiration,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };
}

public partial class AODBestFocusAndAstigmatismDTOItem : ObservableObject, ICloneable<AODBestFocusAndAstigmatismDTOItem>
{
    [ObservableProperty]
    public partial double SpectralDensity { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XYBestFocusOffsetEcs))]
    public partial BestFocus BestFocus { get; set; } = new();

    public double XYBestFocusOffsetEcs => BestFocus.BestXStrehlRatioECS - BestFocus.BestYStrehlRatioECS;

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];

    public object ToFlatnessHtmlAnonymous() => new
    {
        SpectralDensity,
        ChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        ChirpAODWaveformProfiles =
            new HtmlTable([.. ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
        BestXStrehlRatio = new Point(BestFocus.BestXStrehlRatioECS, BestFocus.BestXStrehlRatioPoint.Y),
        BestYStrehlRatio = new Point(BestFocus.BestYStrehlRatioECS, BestFocus.BestYStrehlRatioPoint.Y),
        XYBestFocusOffsetEcs,
        BestFocus.RawImageFilePath,
        XStrehlRatioScatterPlotControl =
            new HtmlContainer([.. BestFocus.XStrehlRatioPlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
        YStrehlRatioScatterPlotControl =
            new HtmlContainer([.. BestFocus.YStrehlRatioPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
    };

    public AODBestFocusAndAstigmatismDTOItem Clone() => new()
    {
        SpectralDensity = SpectralDensity,
        GenerateChirpAODWaveformParam = GenerateChirpAODWaveformParam.Clone(),
        ChirpAODWaveformProfiles = [.. ChirpAODWaveformProfiles.Select(t => t.Clone())],
        BestFocus = BestFocus.Clone()
    };
}