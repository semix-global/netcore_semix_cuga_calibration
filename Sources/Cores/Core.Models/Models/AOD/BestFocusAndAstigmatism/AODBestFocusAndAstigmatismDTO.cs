using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

public partial class AODBestFocusAndAstigmatismDTO : CalibrationDtoBase, ICloneable<AODBestFocusAndAstigmatismDTO>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private OpticsApodizationModeEnum _apodizationModeEnum;

    [ObservableProperty]
    private ObservableCollection<AODBestFocusAndAstigmatismDTOItem> _items = [];

    [ObservableProperty]
    private AODBestFocusAndAstigmatismDTOItem _resultDTO = new();

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitPoints = [];

    partial void OnFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public AODBestFocusAndAstigmatismDTO()
    {
        var customGrid = new CustomGrid();
        ScatterPlotControl.Configure(customGrid, 3,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 2, colSpan: 2));
            });

        ScatterPlotControl.SetTitle(0, "(Y Focus)(Y: mm/MHz - X: Y Focus(ecs))");
        ScatterPlotControl.SetTitle(1, "(X Focus)(Y: mm/MHz - X: X Focus(ecs))");
        ScatterPlotControl.SetTitle(2, "(XY Focus Offset)(Y: mm/MHz - X: XY Focus Offset(ecs))");
    }

    partial void OnItemsChanged(ObservableCollection<AODBestFocusAndAstigmatismDTOItem>? oldValue,
        ObservableCollection<AODBestFocusAndAstigmatismDTOItem> newValue)
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


    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);
            ScatterPlotControl.Clear(1);
            ScatterPlotControl.Clear(2);

            if (Items.Count == 0) return;

            var yScatterLines = ScatterPlotControl.GetOrAddScatterLines(0, 2);
            yScatterLines[0].Update(
                "OriginPoints",
                [
                    ..Items.Select(t =>
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

            ScatterPlotControl.GetOrAddScatterLine(
                1,
                "OriginPoints",
                [
                    ..Items.Select(t =>
                        new Point
                        (
                            t.BestFocus.BestXStrehlRatioECS,
                            1 / t.SpectralDensity
                        )
                    )
                ],
                1,
                new Range(0, Items.Count - 1));

            ScatterPlotControl.GetOrAddScatterLine(
                2,
                "OriginPoints",
                [
                    ..Items.Select(t =>
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
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public object ToFlatnessHtmlAnonymous() => new
    {
        ProductivityInformation,
        ApodizationModeEnum,
        AstigmatismBestSpectralDensity = ResultDTO.SpectralDensity,
        AstigmatismBestChirpAODWaveformParam = new HtmlQuote(ResultDTO.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        Plot = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
    };

    public AODBestFocusAndAstigmatismDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        ApodizationModeEnum = ApodizationModeEnum,
        Items = [.. Items.Select(t => t.Clone())],
        ResultDTO = ResultDTO.Clone(),
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
    private double _spectralDensity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XYBestFocusOffsetEcs))]
    private BestFocus _bestFocus = new();

    public double XYBestFocusOffsetEcs => BestFocus.BestXStrehlRatioECS - BestFocus.BestYStrehlRatioECS;

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

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
            new HtmlContainer([.. BestFocus.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
        YStrehlRatioScatterPlotControl =
            new HtmlContainer([.. BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
    };

    public AODBestFocusAndAstigmatismDTOItem Clone() => new()
    {
        SpectralDensity = SpectralDensity,
        GenerateChirpAODWaveformParam = GenerateChirpAODWaveformParam.Clone(),
        ChirpAODWaveformProfiles = [.. ChirpAODWaveformProfiles.Select(t => t.Clone())],
        BestFocus = BestFocus.Clone()
    };
}