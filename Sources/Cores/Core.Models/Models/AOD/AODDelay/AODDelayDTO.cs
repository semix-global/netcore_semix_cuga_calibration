using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Models.AOD.AODDelay;

public sealed partial class AODDelayDTO : CalibrationDtoBase, ICloneable<AODDelayDTO>, IAdaptTo<CalibrationLaserAodDelayItem>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private IReadOnlyList<AODDelayDTOItem> _items = [];

    [ObservableProperty]
    private AODDelayDTOItem? _maxItem;

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

    public AODDelayDTO Clone() => new()
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
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        PrescanAodDelayTime = MaxItem?.PrescanAODDelay ?? 0d,
        ChirpAodDelayTime = MaxItem?.ChirpAODDelay ?? 0d,
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
    private double _aODDelay;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double PrescanAODDelay => AODDelay <= 0 ? Math.Abs(AODDelay) : 0d;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public double ChirpAODDelay => AODDelay >= 0 ? Math.Abs(AODDelay) : 0d;

    [ObservableProperty]
    private double _pMTValue;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    public AODDelayDTOItem Clone() => new()
    {
        AODDelay = AODDelay,
        PMTValue = PMTValue,
        ImageFilePath = ImageFilePath,
        RawImageFilePath = RawImageFilePath
    };
}