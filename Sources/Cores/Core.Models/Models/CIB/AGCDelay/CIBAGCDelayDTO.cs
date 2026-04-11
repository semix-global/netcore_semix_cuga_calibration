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

namespace Core.Models.Models.CIB.AGCDelay;

public sealed partial class CIBAGCDelayDTO : CalibrationDtoBase, ICloneable<CIBAGCDelayDTO>, IAdaptTo<CalibrationLaserCIBAGCDelayItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

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

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnItemsChanged(IReadOnlyList<CIBAGCDelayDTOItem>? oldValue, IReadOnlyList<CIBAGCDelayDTOItem> newValue)
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

    partial void OnTargetPixelValuesChanged(ConcurrentBag<KeyValuePair<CIBInformation, double>> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public CIBAGCDelayDTO()
    {
    }

    public CIBAGCDelayDTO(IReadOnlyList<CIBInformation> cibInformations)
    {
        ScatterPlotControls = [.. cibInformations.Select(t => new KeyValuePair<CIBInformation, IScatterPlotControl>(t, GetScatterPlotControl()))];
    }

    private void RefreshPlot()
    {
        foreach (var item in Items)
        {
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(item.CIBInformation, new Lazy<IScatterPlotControl>(GetScatterPlotControl));

            scatterPlotControl.Clear(0);
            scatterPlotControl.Clear(1);

            try
            {
                if (TargetPixelValues.TryGetSingle(t => t.Key == item.CIBInformation, out var targetPMTValueKvp))
                {
                    scatterPlotControl.GetOrAddXLine(1, "Target", targetPMTValueKvp.Value, Colors.Red);
                }
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

        scatterPlotControl.SetTitle("Window(Y: PMT Value - X: px)");

        return scatterPlotControl;
    }

    #region Mapper

    public CIBAGCDelayDTO Clone() => new()
    {
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
        public partial double Error { get; set; }

        [ObservableProperty]
        public partial bool IsOk { get; set; }

        public Item Clone() => new()
        {
            Error = Error,
            IsOk = IsOk
        };
    }
}