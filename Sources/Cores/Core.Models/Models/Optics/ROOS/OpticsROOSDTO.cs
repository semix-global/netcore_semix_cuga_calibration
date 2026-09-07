using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Optics.ROOS;

[CacheVersion("1.0.0")]
public sealed partial class OpticsROOSDTO : CalibrationDTOBase<OpticsROOSDTO>, ICloneable<OpticsROOSDTO>, IAdaptTo<CalibrationOpticsROOS>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double Slope { get; set; }

    [ObservableProperty]
    public partial double Intercept { get; set; }

    [ObservableProperty]
    public partial double RSquared { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Point> FitPoints { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<Point> IllegalPoints { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<OpticsROOSDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial OpticsROOSDTOItem ResultDTOItem { get; set; } = new();

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public OpticsROOSDTO()
    {
        PlotDataSource.Configure(new Columns());

        PlotDataSource.SetTitle(0, "Y Pixel Height(Y: Pixel - X: ROOSPos)");
    }

    partial void OnItemsChanged(IReadOnlyList<OpticsROOSDTOItem>? oldValue, IReadOnlyList<OpticsROOSDTOItem> newValue)
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

    partial void OnFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnIllegalPointsChanged(IReadOnlyList<Point> oldValue, IReadOnlyList<Point> newValue) => RefreshPlot();

    private void RefreshPlot()
    {
        if (Items.Count == 0) return;

        PlotDataSource.Clear(0);

        PlotDataSource.GetOrAddScatterLine(
            0,
            "Origin",
            [.. Items.Select(t => new Point(t.ROOSPos, t.CropImageYPixelHeight))],
            0,
            new Range(0, Items.Count - 1));

        if (FitPoints.Count > 0)
            PlotDataSource.GetOrAddScatterLine(
                0,
                $"Fit Curve: y = {Slope:0.######}x + {Intercept:0.######} r^2 = {RSquared:0.######})",
                FitPoints,
                Constants.Category10.GetColor(1));

        if (IllegalPoints.Count > 0)
        {
            var scatterMarkers = PlotDataSource.GetOrAddScatterMarkers(
                0,
                "Illegal Points",
                [.. IllegalPoints],
                color: Constants.Category10.GetColor(3),
                MarkerShape.Asterisk);
            scatterMarkers.MarkerSize = 50;
        }

        PlotDataSource.AutoScaleRefresh();
    }

    public override OpticsROOSDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        FitPoints = [.. FitPoints],
        IllegalPoints = [.. IllegalPoints],
        Items = [.. Items.Select(t => t.Clone())],
        ResultDTOItem = ResultDTOItem.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsROOS AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        OpticsMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        ROOSPos = ResultDTOItem.ROOSPos,
        StartImageYPixel = ResultDTOItem.StartImageYPixel,
        EndImageYPixel = ResultDTOItem.EndImageYPixel,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };
}

public sealed partial class OpticsROOSDTOItem : ObservableCacheBase, ICloneable<OpticsROOSDTOItem>
{
    [ObservableProperty]
    public partial double ROOSPos { get; set; }

    [ObservableProperty]
    public partial double StartImageYPixel { get; set; }

    [ObservableProperty]
    public partial double EndImageYPixel { get; set; }

    public double CropImageYPixelHeight => EndImageYPixel - StartImageYPixel;

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DrawFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginFilePath { get; set; } = string.Empty;

    public void ConvertYPixelToNearestEvenPrecise()
    {
        var imageYPixelHeight = CropImageYPixelHeight;
        StartImageYPixel = ConvertDoubleToNearestEvenPrecise(StartImageYPixel);
        EndImageYPixel = ConvertDoubleToNearestEvenPrecise(StartImageYPixel + imageYPixelHeight);
        return;

        double ConvertDoubleToNearestEvenPrecise(double value)
        {
            // 获取整数部分
            var integerPart = (int)value;

            // 如果整数部分已经是偶数，直接返回
            if (integerPart % 2 == 0)
                return integerPart;

            // 对于奇数，找到两个相邻的候选偶数
            var candidate1 = integerPart - 1; // 比奇数小的偶数
            var candidate2 = integerPart + 1; // 比奇数大的偶数

            // 计算原数与两个候选偶数的绝对差值
            var diff1 = Math.Abs(value - candidate1);
            var diff2 = Math.Abs(value - candidate2);

            // 返回差值较小的那个偶数
            // 如果差值相等（例如数字正好在两个偶数中间），返回较小的
            return diff1 <= diff2 ? candidate1 : candidate2;
        }
    }

    public OpticsROOSDTOItem Clone() => new()
    {
        ROOSPos = ROOSPos,
        StartImageYPixel = StartImageYPixel,
        EndImageYPixel = EndImageYPixel,
        FilePath = FilePath,
        DrawFilePath = DrawFilePath,
        OriginFilePath = OriginFilePath
    };
}