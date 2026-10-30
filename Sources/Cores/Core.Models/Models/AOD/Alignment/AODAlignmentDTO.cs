using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot.MultiplotLayouts;

namespace Core.Models.Models.AOD.Alignment;

[CacheVersion("1.0.0")]
public sealed partial class AODAlignmentDTO : CalibrationDTOBase<AODAlignmentDTO>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<AODAlignmentChannel> ChannelItems { get; set; } = [];

    [ObservableProperty]
    public partial double Slope { get; set; }

    [ObservableProperty]
    public partial double Intercept { get; set; }
    
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];
    
    // ReSharper disable UnusedParameterInPartialMethod
    
    partial void OnSlopeChanged(double value) => RefreshPlot();

    partial void OnInterceptChanged(double value) => RefreshPlot();

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
            var channels = ChannelItems.OrderBy(channel => channel.CIBInformation.ChannelId).ToArray();
            var projectionCount = channels.Sum(channel => channel.Items.Count(item => item.ImageHorizontalProjects.Length > 0));
            var alignmentCount = channels.Sum(channel => (HasAlignmentPoints(channel) ? 1 : 0) + (channel.FitAlignmentPoints.Count > 0 ? 1 : 0));

            var projectionLines = PlotDataSource.GetOrAddScatterLines(0, projectionCount);
            var alignmentLines = PlotDataSource.GetOrAddScatterLines(1, alignmentCount);

            var projectionIndex = 0;
            var alignmentIndex = 0;
            foreach (var (channelIndex, channel) in channels.Index())
            {
                foreach (var item in channel.Items.OrderBy(item => item.PrescanFrequency))
                {
                    if (item.ImageHorizontalProjects.Length <= 0) continue;

                    projectionLines.ElementAtOrDefault(projectionIndex)?.Update(
                        $"{channel.CIBInformation} {item.PrescanFrequency:0.###}(MHz)",
                        item.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item)).ToArray(),
                        Constants.Category10.GetColor(channelIndex));
                    projectionIndex++;
                }

                var alignmentPoints = channel.Items
                    .OrderBy(item => item.PrescanFrequency)
                    .Select(item => item.PeakPosition is { } peak && double.IsFinite(peak)
                        ? new Point(item.PrescanFrequency, peak)
                        : item.PeakInteger is { } pixel
                            ? new Point(item.PrescanFrequency, pixel)
                            : (Point?)null)
                    .Where(point => point.HasValue)
                    .Select(point => point!.Value)
                    .ToArray();

                if (alignmentPoints.Length > 0)
                {
                    alignmentLines.ElementAtOrDefault(alignmentIndex)?.Update(
                        $"{channel.CIBInformation}",
                        alignmentPoints,
                        Constants.Category10.GetColor(channelIndex));
                    alignmentIndex++;
                }

                if (channel.FitAlignmentPoints.Count > 0)
                {
                    alignmentLines.ElementAtOrDefault(alignmentIndex)?.Update(
                        $"{channel.CIBInformation} Fit: y = {channel.Slope:0.######}x + {channel.Intercept:0.######} r^2 = {channel.RSquared:0.######}",
                        channel.FitAlignmentPoints.ToArray(),
                        Constants.Category10.GetColor(channelIndex));
                    alignmentIndex++;
                }
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }

        return;

        static bool HasAlignmentPoints(AODAlignmentChannel channel) => channel.Items.Any(item => item.PeakPosition is { } peak && double.IsFinite(peak) || item.PeakInteger is not null);
    }

    public override AODAlignmentDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        ChannelItems = [.. ChannelItems.Select(x => x.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        GeneratePrescanAODWaveformParam = GeneratePrescanAODWaveformParam.Clone(),
        PrescanAODWaveformResultFilePath = PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = [.. PrescanAODWaveformProfiles.Select(t => t.Clone())],
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
    public partial double[] ImageHorizontalProjects { get; set; } = [];

    [ObservableProperty]
    public partial int? PeakInteger { get; set; }

    [ObservableProperty]
    public partial double? PeakPosition { get; set; }

    [ObservableProperty]
    public partial bool IsPeakValid { get; set; }

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
        PeakInteger = PeakInteger,
        PeakPosition = PeakPosition,
        IsPeakValid = IsPeakValid,
        RawImageFilePath = RawImageFilePath,
        ImageFilePath = ImageFilePath
    };
}

public sealed partial class AODAlignmentChannel : ObservableObject, ICloneable<AODAlignmentChannel>
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

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

    [ObservableProperty]
    public partial int LeftRemovedCount { get; set; }

    [ObservableProperty]
    public partial int RightRemovedCount { get; set; }

    [ObservableProperty]
    public partial int KeptPointCount { get; set; }

    [ObservableProperty]
    public partial double FitStartFrequency { get; set; }

    [ObservableProperty]
    public partial double FitStopFrequency { get; set; }

    [ObservableProperty]
    public partial string FitStatus { get; set; } = string.Empty;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<AODAlignmentDTOItem> oldValue, IReadOnlyList<AODAlignmentDTOItem> newValue)=> RefreshPlot();

    partial void OnSlopeChanged(double value) => RefreshPlot();

    partial void OnInterceptChanged(double value) => RefreshPlot();

    partial void OnRSquaredChanged(double value) => RefreshPlot();

    partial void OnFitAlignmentPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public AODAlignmentChannel()
    {
        PlotDataSource.Configure(new Columns(), 2);

        PlotDataSource.SetTitle(0, "Projection Y(Y: PMT Value - X: px)");
        PlotDataSource.SetTitle(1, "Alignment(Y: px - X: MHz)");
    }

    private void RefreshPlot()
    {
        try
        {
            var items = Items.OrderBy(item => item.PrescanFrequency).ToArray();
            var projectionCount = items.Count(item => item.ImageHorizontalProjects.Length > 0);
            var alignmentPoints = items
                .Select(item => item.PeakPosition is { } peak && double.IsFinite(peak)
                    ? new Point(item.PrescanFrequency, peak)
                    : item.PeakInteger is { } pixel
                        ? new Point(item.PrescanFrequency, pixel)
                        : (Point?)null)
                .Where(point => point.HasValue)
                .Select(point => point!.Value)
                .ToArray();
            var alignmentCount = (alignmentPoints.Length > 0 ? 1 : 0) + (FitAlignmentPoints.Count > 0 ? 1 : 0);

            var projectionLines = PlotDataSource.GetOrAddScatterLines(0, projectionCount);
            var alignmentLines = PlotDataSource.GetOrAddScatterLines(1, alignmentCount);

            var projectionIndex = 0;
            foreach (var (index, item) in items.Index())
            {
                if (item.ImageHorizontalProjects.Length <= 0) continue;

                projectionLines.ElementAtOrDefault(projectionIndex)?.Update(
                    $"{item.PrescanFrequency:0.###}(MHz)",
                    item.ImageHorizontalProjects.Index().Select(t => new Point(t.Index, t.Item)).ToArray(),
                    Constants.Category10.GetColor(index));
                projectionIndex++;
            }

            var alignmentIndex = 0;
            if (alignmentPoints.Length > 0)
            {
                alignmentLines.ElementAtOrDefault(alignmentIndex)?.Update(
                    $"{CIBInformation}",
                    alignmentPoints,
                    Constants.Category10.GetColor(0));
                alignmentIndex++;
            }

            if (FitAlignmentPoints.Count > 0)
            {
                PlotDataSource.SetTitle(0, $"CH{CIBInformation.ChannelId}Projection Y(Y: PMT Value - X: px)");
                PlotDataSource.SetTitle(1, $"CH{CIBInformation.ChannelId}Alignment(Y: px - X: MHz)");

                alignmentLines.ElementAtOrDefault(alignmentIndex)?.Update(
                    $"Fit: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######}",
                    FitAlignmentPoints.ToArray(),
                    Constants.Category10.GetColor(1));
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public AODAlignmentChannel Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(item => item.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitAlignmentPoints = [.. FitAlignmentPoints],
        LeftRemovedCount = LeftRemovedCount,
        RightRemovedCount = RightRemovedCount,
        KeptPointCount = KeptPointCount,
        FitStartFrequency = FitStartFrequency,
        FitStopFrequency = FitStopFrequency,
        FitStatus = FitStatus
    };
}