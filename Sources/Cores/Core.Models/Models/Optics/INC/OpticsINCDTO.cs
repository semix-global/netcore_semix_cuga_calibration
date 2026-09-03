using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.Extensions;
using ScottPlot;
using System.ComponentModel;
using Net.Utilities.ScottPlot.Helper;

namespace Core.Models.Models.Optics.INC;

[CacheVersion("1.0.1")]
public sealed partial class OpticsINCDTO : CalibrationDTOBase<OpticsINCDTO>, IAdaptTo<CalibrationOpticsINC>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<OpticsINCDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> SmoothPoints { get; set; } = [];

    [ObservableProperty]
    public partial double? MaxItemINCMotorAbsoluteValue { get; set; }

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<OpticsINCDTOItem> oldValue, IReadOnlyList<OpticsINCDTOItem> newValue)
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

    partial void OnSmoothPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnMaxItemINCMotorAbsoluteValueChanged(double? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsINCDTO()
    {
        PlotDataSource.SetTitle("INC(Y: PMT Value - X: °)");
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines = PlotDataSource.GetOrAddScatterLines((Items.Count > 0 ? 1 : 0) + (SmoothPoints.Count > 0 ? 1 : 0));
            var xLines = PlotDataSource.GetOrAddXLines(MaxItemINCMotorAbsoluteValue is not null ? 1 : 0);

            scatterLines.ElementAtOrDefault(0)?.Update(
                "INC",
                [.. Items.Select(t => new Point(t.INCMotorAbsoluteValue, t.PMTValue))],
                Constants.Category10.GetColor(0));

            scatterLines.ElementAtOrDefault(1)?.Update(
                "Smooth",
                SmoothPoints,
                Constants.Category10.GetColor(1));

            xLines.ElementAtOrDefault(0)?.Update("Max", Guard.IsNotNullAndReturn(MaxItemINCMotorAbsoluteValue), Colors.Red);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override OpticsINCDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        SmoothPoints = [.. SmoothPoints],
        MaxItemINCMotorAbsoluteValue = MaxItemINCMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationOpticsINC AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        INCMotorAbsoluteValue = MaxItemINCMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class OpticsINCDTOItem : ObservableObject, ICloneable<OpticsINCDTOItem>
{
    [ObservableProperty]
    public partial double INCMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double PMTValue { get; set; }

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    public OpticsINCDTOItem Clone() => new()
    {
        INCMotorAbsoluteValue = INCMotorAbsoluteValue,
        PMTValue = PMTValue,
        RawImageFilePath = RawImageFilePath,
        ImageFilePath = ImageFilePath
    };
}