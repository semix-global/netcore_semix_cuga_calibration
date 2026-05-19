using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using System.ComponentModel;

namespace Core.Models.Models.AOD.Delay;

[CacheVersion("1.0.0")]
public sealed partial class AODDelayDTO : CalibrationDTOBase<AODDelayDTO>, IAdaptTo<CalibrationLaserAodDelayItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<AODDelayDTOItem> Items { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrescanAODDelay), nameof(ChirpAODDelay))]
    public partial AODDelayDTOItem? MaxItem { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public double PrescanAODDelay => MaxItem is not null && MaxItem.AODDelay <= 0 ? Math.Abs(MaxItem.AODDelay) : 0d;

    [Newtonsoft.Json.JsonIgnore]
    public double ChirpAODDelay => MaxItem is not null && MaxItem.AODDelay >= 0 ? Math.Abs(MaxItem.AODDelay) : 0d;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<AODDelayDTOItem>? oldValue, IReadOnlyList<AODDelayDTOItem> newValue)
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

    partial void OnMaxItemChanged(AODDelayDTOItem? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public AODDelayDTO()
    {
        ScatterPlotControl.SetTitle("AOD Delay(Y: PMT Value - X: sa)");
    }

    private void RefreshPlot()
    {
        try
        {
            if (Items.Count <= 0) return;

            ScatterPlotControl.GetOrAddScatterLine(
                "AOD Delay",
                [.. Items.Select(t => new Point(t.AODDelay, t.PMTValue))],
                Constants.Category10.GetColor(0));

            MaxItem = Items.Maxima(t => t.PMTValue).First();
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    #region Mapper

    public override AODDelayDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = [.. Items.Select(t => t.Clone())],
        MaxItem = MaxItem?.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAodDelayItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        PrescanAodDelayTime = PrescanAODDelay,
        ChirpAodDelayTime = ChirpAODDelay,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class AODDelayDTOItem : ObservableObject, ICloneable<AODDelayDTOItem>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrescanAODDelay), nameof(ChirpAODDelay))]
    public partial double AODDelay { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public double PrescanAODDelay => AODDelay <= 0 ? Math.Abs(AODDelay) : 0d;

    [Newtonsoft.Json.JsonIgnore]
    public double ChirpAODDelay => AODDelay >= 0 ? Math.Abs(AODDelay) : 0d;

    [ObservableProperty]
    public partial double PMTValue { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    public AODDelayDTOItem Clone() => new()
    {
        AODDelay = AODDelay,
        PMTValue = PMTValue,
        ImageFilePath = ImageFilePath,
        RawImageFilePath = RawImageFilePath
    };
}