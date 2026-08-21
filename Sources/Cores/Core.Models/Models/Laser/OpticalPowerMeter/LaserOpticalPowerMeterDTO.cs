using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.ComponentModel;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

[CacheVersion("1.0.0")]
public sealed partial class LaserOpticalPowerMeterDTO : CalibrationDTOBase<LaserOpticalPowerMeterDTO>, IAdaptTo<CalibrationLaserOpticalPower>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double MaxCoefficient { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<LaserOpticalPowerMeterDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial double MaxMeasurePower { get; set; }

    [ObservableProperty]
    public partial Point MaxMeasurePowerPosition { get; set; } = Point.Origin;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<LaserOpticalPowerMeterDTOItem> oldValue, IReadOnlyList<LaserOpticalPowerMeterDTOItem> newValue)
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

    public LaserOpticalPowerMeterDTO()
    {
        PlotDataSource.SetTitle("Map(Y: um - X: um - Z: mW)");
    }

    private void RefreshPlot()
    {
        try
        {
            var heatmaps = PlotDataSource.GetOrAddHeatmaps(Items.Count > 0 ? 1 : 0);

            heatmaps.ElementAtOrDefault(0)?.Update(
                "Measure Power",
                [.. Items.Select(t => new Point3D(t.MeasurePosition.X, t.MeasurePosition.Y, t.MeasurePower))],
                "00.00000",
                isHighlightMax: true);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override LaserOpticalPowerMeterDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        MaxCoefficient = MaxCoefficient,
        Items = [.. Items.Select(x => x.Clone())],
        MaxMeasurePower = MaxMeasurePower,
        MaxMeasurePowerPosition = MaxMeasurePowerPosition,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserOpticalPower AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Coefficient = MaxCoefficient,
        MeasureMaxPower = MaxMeasurePower,
        MeasureMaxPowerPosition = MaxMeasurePowerPosition.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class LaserOpticalPowerMeterDTOItem : ObservableObject, ICloneable<LaserOpticalPowerMeterDTOItem>
{
    [ObservableProperty]
    public partial Point MeasurePosition { get; set; }

    [ObservableProperty]
    public partial double MeasurePower { get; set; }

    public LaserOpticalPowerMeterDTOItem Clone() => new()
    {
        MeasurePosition = MeasurePosition,
        MeasurePower = MeasurePower
    };
}