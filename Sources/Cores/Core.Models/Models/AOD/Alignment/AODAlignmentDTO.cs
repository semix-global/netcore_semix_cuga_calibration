using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot.MultiplotLayouts;
using Constants = Net.Utilities.ScottPlot.WPF.Helper.Constants;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.Alignment;

public sealed partial class AODAlignmentDTO : CalibrationDtoBase, ICloneable<AODAlignmentDTO>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDTOItem> _items = [];

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitAlignmentPoints = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<AODAlignmentDTOItem>? oldValue, IReadOnlyList<AODAlignmentDTOItem> newValue)
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

    partial void OnSlopeChanged(double value) => RefreshPlot();

    partial void OnInterceptChanged(double value) => RefreshPlot();

    partial void OnRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitAlignmentPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public AODAlignmentDTO()
    {
        ScatterPlotControl.Configure(new Columns(), 2);

        ScatterPlotControl.SetTitle(0, "Projection Y(Y: PMT Value - X: px)");
        ScatterPlotControl.SetTitle(1, "Alignment(Y: px - X: MHz)");
    }

    private void RefreshPlot()
    {
        try
        {
            ScatterPlotControl.Clear(0);
            ScatterPlotControl.Clear(1);

            var isNeedRefreshes = new bool[Items.Count];

            foreach (var (index, item) in Items.Index())
            {
                if (item.ImageHorizontalProjects.Count <= 0) continue;

                ScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"{item.PrescanFrequency:0.###}(MHz)",
                    [.. item.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item))],
                    index,
                    new Range(0, Items.Count - 1));

                item.ImageHorizontalProjectsMaxPixel = Vector<double>.Build.DenseOfEnumerable(item.ImageHorizontalProjects).MaximumIndex();

                isNeedRefreshes[index] = true;
            }

            if (isNeedRefreshes.All(b => b))
            {
                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Alignment",
                    [.. Items.Select(t => new Point(t.PrescanFrequency, GuardUtils.IsNotNullAndReturn(t.ImageHorizontalProjectsMaxPixel)))],
                    Constants.Category10.GetColor(0));
            }

            if (FitAlignmentPoints.Count > 0)
                ScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"Fit Curve: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######})",
                    FitAlignmentPoints,
                    Constants.Category10.GetColor(1));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public AODAlignmentDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
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

public sealed partial class AODAlignmentDTOItem : ObservableCacheBase, ICloneable<AODAlignmentDTOItem>
{
    [ObservableProperty]
    private double _prescanFrequency;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private IReadOnlyList<double> _imageHorizontalProjects = [];

    [ObservableProperty]
    private int? _imageHorizontalProjectsMaxPixel;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    public AODAlignmentDTOItem Clone() => new()
    {
        PrescanFrequency = PrescanFrequency,
        PrescanAODWaveformResultFilePath = PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = [.. PrescanAODWaveformProfiles.Select(t => t.Clone())],
        ImageHorizontalProjects = [.. ImageHorizontalProjects],
        ImageHorizontalProjectsMaxPixel = ImageHorizontalProjectsMaxPixel,
        RawImageFilePath = RawImageFilePath,
        ImageFilePath = ImageFilePath
    };
}