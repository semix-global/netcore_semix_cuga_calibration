using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using System.Collections.Concurrent;
using System.ComponentModel;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Helper;
using Range = ScottPlot.Range;

namespace Core.Models.Models.CIB.AGCDelay;

public sealed partial class CIBAGCDelayDTO : CalibrationDtoBase, ICloneable<CIBAGCDelayDTO>, IAdaptTo<CalibrationLaserCIBAGCDelayItem>
{
    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial Point[] LaserLightInformationPMTVoltageValuePoints { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    public partial IReadOnlyList<CIBAGCDelayDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial ConcurrentBag<KeyValuePair<CIBInformation, double>> TargetPixelValues { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial ConcurrentBag<KeyValuePair<CIBInformation, IScatterPlotControl>> ScatterPlotControls { get; set; } = [];

    #region Partial Method

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnLaserLightInformationPMTVoltageValuePointsChanged(Point[] value) => RefreshPlot();

    partial void OnItemsChanged(IReadOnlyList<CIBAGCDelayDTOItem>? oldValue, IReadOnlyList<CIBAGCDelayDTOItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlots();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlots();
    }

    partial void OnTargetPixelValuesChanged(ConcurrentBag<KeyValuePair<CIBInformation, double>> value) => RefreshPlots();

    // ReSharper restore UnusedParameterInPartialMethod

    #endregion

    public CIBAGCDelayDTO()
    {
        ScatterPlotControl.SetTitle("Laser Light Information(Y: PMT Value(Voltage) - X: Coefficient)");
    }

    public CIBAGCDelayDTO(IReadOnlyList<CIBInformation> cibInformations) : this()
    {
        ScatterPlotControls = [.. cibInformations.Select(t => new KeyValuePair<CIBInformation, IScatterPlotControl>(t, GetScatterPlotControl()))];
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

    private void RefreshPlots()
    {
        foreach (var item in Items)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(item.CIBInformation, new Lazy<IScatterPlotControl>(GetScatterPlotControl));

            scatterPlotControl.Clear(0);

            try
            {
                var scatterLines = scatterPlotControl.GetOrAddScatterLines(item.Items.Count);
                var xLines = scatterPlotControl.GetOrAddXLines(item.Items.Count + 1);

                foreach (var (index, itemItemData) in item.Items.Index())
                {
                    var color = Constants.Turbo.GetColor(index, new Range(0, item.Items.Count - 1));
                    scatterLines[index].Update(
                        $"{index + 1}: Delay: {item.Delay:0.###}",
                        [.. itemItemData.ImageHorizontalProjects.ToPoints()],
                        color);

                    xLines[index + 1].Update(
                        $"{index + 1}: Error: {itemItemData.Error:0.###}",
                        itemItemData.HorizontalProjectMinPixel,
                        color);

                    scatterLines[index].IsVisible = xLines[index + 1].IsVisible = index == item.Items.Count - 1;
                }

                xLines[0].Update(TargetPixelValues.TryGetSingle(t => t.Key == item.CIBInformation, out var targetPMTValueKvp)
                        ? "Target"
                        : string.Empty,
                    targetPMTValueKvp.Value,
                    Colors.Red);
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

    public CIBAGCDelayDTO Clone() => new()
    {
        LaserLightInformation = LaserLightInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        TargetPixelValues = [.. TargetPixelValues],
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
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IReadOnlyList<Item> Items { get; set; } = [];

    [ObservableProperty]
    public partial double Delay { get; set; }

    partial void OnItemsChanged(IReadOnlyList<Item>? oldValue, IReadOnlyList<Item> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

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
            IsOk = IsOk,
        };

        public void CalculateHorizontalProjectMinPixel()
        {
            var targetValue = ImageHorizontalProjects.Min() + (ImageHorizontalProjects.Max() - ImageHorizontalProjects.Min()) * 2d / 3d;
            var changedList = ImageHorizontalProjects.ToPoints().Where(t => t.Y < targetValue).ToList();

            var startIndex = Convert.ToInt32(changedList[0].X);
            var endIndex = Convert.ToInt32(changedList[^1].X);

            HorizontalProjectMinPixel = (startIndex + endIndex) / 2;
        }
    }
}