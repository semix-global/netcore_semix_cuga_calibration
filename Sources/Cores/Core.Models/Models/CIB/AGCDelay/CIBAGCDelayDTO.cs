using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using System.ComponentModel;
using CommunityToolkit.Diagnostics;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.AGCDelay;

[CacheVersion("1.0.1")]
public sealed partial class CIBAGCDelayDTO : CalibrationDTOBase<CIBAGCDelayDTO>, IAdaptTo<CalibrationLaserCIBAGCDelayItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double Coefficient { get; set; } = -1;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial Point[] LaserLightInformationPMTVoltageValuePoints { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AODUniformityDTO.WindowItem StartWindowItem { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AODUniformityDTO.WindowItem StopWindowItem { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    public bool IsReverse => StartWindowItem.HorizontalProjectMinPixel > StopWindowItem.HorizontalProjectMinPixel;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ForwardAndReverseScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    public partial IReadOnlyList<CIBAGCDelayDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ConcurrentDictionary<CIBInformation, double> TargetPixelValues { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ConcurrentDictionary<CIBInformation, IScatterPlotControl> ScatterPlotControls { get; set; } = [];

    #region Partial Method

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnLaserLightInformationPMTVoltageValuePointsChanged(Point[] value) => RefreshPlot();

    partial void OnStartWindowItemChanged(AODUniformityDTO.WindowItem oldValue, AODUniformityDTO.WindowItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        OnPropertyChanged(nameof(IsReverse));
        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsReverse));
            RefreshForwardAndReversePlot();
        }
    }

    partial void OnStopWindowItemChanged(AODUniformityDTO.WindowItem oldValue, AODUniformityDTO.WindowItem newValue)
    {
        oldValue.PropertyChanged -= ItemOnPropertyChanged;

        newValue.PropertyChanged -= ItemOnPropertyChanged;
        newValue.PropertyChanged += ItemOnPropertyChanged;

        OnPropertyChanged(nameof(IsReverse));
        RefreshForwardAndReversePlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsReverse));
            RefreshForwardAndReversePlot();
        }
    }

    partial void OnItemsChanged(IReadOnlyList<CIBAGCDelayDTOItem> oldValue, IReadOnlyList<CIBAGCDelayDTOItem> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlots();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlots();
    }

    partial void OnTargetPixelValuesChanged(ConcurrentDictionary<CIBInformation, double> value) => RefreshPlots();

    // ReSharper restore UnusedParameterInPartialMethod

    #endregion

    public CIBAGCDelayDTO()
    {
        ScatterPlotControl.SetTitle("Laser Light Information(Y: PMT Value(Voltage) - X: Coefficient)");

        ForwardAndReverseScatterPlotControl.Configure(new Columns(), 2);

        ForwardAndReverseScatterPlotControl.SetTitle(0, "Window(Y: Coefficient - X: sa)");
        ForwardAndReverseScatterPlotControl.SetTitle(1, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
    }

    public CIBAGCDelayDTO(IReadOnlyList<CIBInformation> cibInformations) : this()
    {
        ScatterPlotControls = new ConcurrentDictionary<CIBInformation, IScatterPlotControl>(cibInformations.Select(t => new KeyValuePair<CIBInformation, IScatterPlotControl>(t, GetScatterPlotControl())));
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(1);

            scatterLines[0].Update(
                string.Empty,
                LaserLightInformationPMTVoltageValuePoints,
                Constants.Category10.GetColor(0));
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    private void RefreshForwardAndReversePlot()
    {
        try
        {
            Refresh(StartWindowItem, "Start", Colors.Blue, Colors.DarkBlue);
            Refresh(StopWindowItem, "Stop", Colors.Red, Colors.DarkRed);
        }
        finally
        {
            ForwardAndReverseScatterPlotControl.AutoScaleRefresh();
        }

        return;

        void Refresh(AODUniformityDTO.WindowItem windowItem, string title, Color primaryColor, Color secondaryColor)
        {
            if (windowItem.Window.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    0,
                    title,
                    [.. windowItem.Window.ToPoints()],
                    primaryColor);

            if (windowItem.ImageHorizontalProjects.Count > 0)
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    title,
                    [.. windowItem.ImageHorizontalProjects.ToPoints()],
                    primaryColor);

            if (windowItem.SmoothImageHorizontalProjects.Count > 0)
            {
                ForwardAndReverseScatterPlotControl.GetOrAddScatterLine(
                    1,
                    $"{title} Smooth",
                    [.. windowItem.SmoothImageHorizontalProjects.ToPoints()],
                    secondaryColor);

                ForwardAndReverseScatterPlotControl.GetOrAddXLine(
                    1,
                    $"{title} Smooth Min Pixel",
                    windowItem.HorizontalProjectMinPixel,
                    secondaryColor);
            }
        }
    }

    private void RefreshPlots()
    {
        foreach (var item in Items)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(item.CIBInformation, _ => GetScatterPlotControl());

            try
            {
                var information = scatterPlotControl.GetTitle().Split(['=', '>'], StringSplitOptions.RemoveEmptyEntries);
                scatterPlotControl.SetTitle($"{information[0].Trim()} => {nameof(item.Delay)}: {item.Delay:0.###}, Delay(0) Error: {item.ZeroDelayError:0.###}");

                var scatterLines = scatterPlotControl.GetOrAddScatterLines(item.Items.Count);
                var xLines = scatterPlotControl.GetOrAddXLines(item.Items.Count + 1);

                foreach (var (index, itemItemData) in item.Items.Index())
                {
                    var color = Constants.Turbo.GetColor(index, new Range(0, item.Items.Count - 1));
                    scatterLines[index].Update(
                        $"{index + 1} => Current: {itemItemData.HorizontalProjectMinPixel:0.###} Error: {itemItemData.Error:0.###}",
                        [.. itemItemData.ImageHorizontalProjects.ToPoints()],
                        color);

                    xLines[index + 1].Update(
                        $"{index + 1} => Error: {itemItemData.Error:0.###}",
                        itemItemData.HorizontalProjectMinPixel,
                        color);

                    scatterLines[index].IsVisible = xLines[index + 1].IsVisible = index == item.Items.Count - 1;
                }

                var tryGetSingle = TargetPixelValues.TryGetSingle(t => t.Key == item.CIBInformation, out var targetPMTValueKvp);
                xLines[0].Update(
                    $"Target: {targetPMTValueKvp.Value:0.###}",
                    targetPMTValueKvp.Value,
                    Colors.Red);
                xLines[0].IsVisible = tryGetSingle;
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }
    }

    private static IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        scatterPlotControl.SetTitle("Horizontal Projects(Y: PMT Value(Voltage) - X: px)");

        return scatterPlotControl;
    }

    #region Mapper

    public override CIBAGCDelayDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Coefficient = Coefficient,
        Items = [.. Items.Select(t => t.Clone())],
        TargetPixelValues = new ConcurrentDictionary<CIBInformation, double>(TargetPixelValues.Select(t => new KeyValuePair<CIBInformation, double>(t.Key.Clone(), t.Value))),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserCIBAGCDelayItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Items = [.. Items.Select(t => t.AdaptTo())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class CIBAGCDelayDTOItem : ObservableObject, ICloneable<CIBAGCDelayDTOItem>, IAdaptTo<CalibrationLaserCIBAGCDelayItem.Item>
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Item> Items { get; set; } = [];

    [ObservableProperty]
    public partial double ZeroDelayError { get; set; }

    [ObservableProperty]
    public partial double Delay { get; set; }

    partial void OnItemsChanged(IReadOnlyList<Item> oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(Items));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Items));
    }

    #region Mapper

    public CIBAGCDelayDTOItem Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        ZeroDelayError = ZeroDelayError,
        Delay = Delay
    };

    public CalibrationLaserCIBAGCDelayItem.Item AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        ChannelId = CIBInformation.ChannelId,
        Delay = Delay
    };

    #endregion Mapper

    public sealed partial class Item : ObservableObject, ICloneable<Item>
    {
        [ObservableProperty]
        public partial IReadOnlyList<double> ImageHorizontalProjects { get; set; } = [];

        [ObservableProperty]
        public partial int HorizontalProjectMinPixel { get; set; }

        [ObservableProperty]
        public partial string RawImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ImageFilePath { get; set; } = string.Empty;

        [ObservableProperty]
        public partial double Error { get; set; }

        [ObservableProperty]
        public partial bool IsOk { get; set; }

        public Item Clone() => new()
        {
            ImageHorizontalProjects = [.. ImageHorizontalProjects],
            HorizontalProjectMinPixel = HorizontalProjectMinPixel,
            RawImageFilePath = RawImageFilePath,
            ImageFilePath = ImageFilePath,
            Error = Error,
            IsOk = IsOk
        };

        public void CalculateHorizontalProjectMinPixel(ProductivityInformation productivityInformation, int markerLengthPixel)
        {
            var imageHorizontalProjects = ImageHorizontalProjects
                .ToArray()
                .AsSpan()[productivityInformation.OriginYPixelsStartIndex..(productivityInformation.OriginYPixelsEndIndex + 1)]
                .ToArray();

            Guard.IsEqualTo(imageHorizontalProjects.Length, productivityInformation.YPixels);

            var (indexes, _) = Extremumor.FindMinima(imageHorizontalProjects.ToPoints());

            var centerIndex = indexes.OrderBy(t => imageHorizontalProjects[t]).First();

            var temps = Generate.LinearRangeInt32(centerIndex - markerLengthPixel, centerIndex + markerLengthPixel)
                .Where(t => t >= 0 && t < imageHorizontalProjects.Length)
                .Select(t => new Point(t, imageHorizontalProjects[t]))
                .ToArray();

            var targetValue = temps.Min(t => t.Y) + (temps.Max(t => t.Y) - temps.Min(t => t.Y)) * 1d / 4d;
            var changedList = temps.Where(t => t.Y < targetValue).ToList();

            var startIndex = Convert.ToInt32(changedList[0].X);
            var endIndex = Convert.ToInt32(changedList[^1].X);

            HorizontalProjectMinPixel = productivityInformation.OriginYPixelsStartIndex + (startIndex + endIndex) / 2;
        }
    }
}