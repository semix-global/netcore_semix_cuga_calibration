using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.Alignment;

[CacheVersion("1.0.0")]
public sealed partial class AODAlignmentDTO : CalibrationDTOBase<AODAlignmentDTO>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<AODAlignmentDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial double Slope { get; set; }

    [ObservableProperty]
    public partial double Intercept { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> FitAlignmentPoints { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<AODAlignmentDTOItem> oldValue, IReadOnlyList<AODAlignmentDTOItem> newValue)
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

    partial void OnSlopeChanged(double value) => RefreshPlot();

    partial void OnInterceptChanged(double value) => RefreshPlot();

    partial void OnRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitAlignmentPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public AODAlignmentDTO()
    {
        PlotDataSource.Configure(new Columns(), 2);

        PlotDataSource.SetTitle(0, "Projection Y(Y: PMT Value - X: px)");
        PlotDataSource.SetTitle(1, "Alignment(Y: px - X: MHz)");
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.Clear(0);
            PlotDataSource.Clear(1);

            var isNeedRefreshes = new bool[Items.Count];

            foreach (var (index, item) in Items.Index())
            {
                if (item.ImageHorizontalProjects.Count <= 0) continue;

                PlotDataSource.GetOrAddScatterLine(
                    0,
                    $"{item.PrescanFrequency:0.###}(MHz)",
                    [.. item.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    index,
                    new Range(0, Items.Count - 1));

                item.ProjectMaxPixel = Vector<double>.Build.DenseOfEnumerable(item.ImageHorizontalProjects).MaximumIndex();

                isNeedRefreshes[index] = true;
            }

            if (isNeedRefreshes.All(b => b))
            {
                PlotDataSource.GetOrAddScatterLine(
                    1,
                    "Alignment",
                    [.. Items.Select(t => new Point(t.PrescanFrequency, Guard.IsNotNullAndReturn(t.ProjectMaxPixel)))],
                    Constants.Category10.GetColor(0));
            }

            if (FitAlignmentPoints.Count > 0)
                PlotDataSource.GetOrAddScatterLine(
                    1,
                    $"Fit Curve: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######}",
                    FitAlignmentPoints,
                    Constants.Category10.GetColor(1));
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public override AODAlignmentDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(x => x.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitAlignmentPoints = [.. FitAlignmentPoints],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AODAlignmentDTOItem : ObservableObject, ICloneable<AODAlignmentDTOItem>
{
    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> ImageHorizontalProjects { get; set; } = [];

    [ObservableProperty]
    public partial int? ProjectMaxPixel { get; set; }

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    public AODAlignmentDTOItem Clone() => new()
    {
        PrescanFrequency = PrescanFrequency,
        PrescanAODWaveformResultFilePath = PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = [.. PrescanAODWaveformProfiles.Select(t => t.Clone())],
        ImageHorizontalProjects = [.. ImageHorizontalProjects],
        ProjectMaxPixel = ProjectMaxPixel,
        RawImageFilePath = RawImageFilePath,
        ImageFilePath = ImageFilePath
    };
}